// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>A model carried from one Part O TAS run into the next does not grow.</b>
    ///
    /// <para><b>What the 2B live run showed (26 Sep 2026)</b></para>
    /// <para>
    /// Every optimisation round prepares from the previous round's returned model and simulates it through
    /// <c>RunPartOSimulation</c>, whose ownership copy is <c>new AnalyticalModel(model, deepClone: true)</c>.
    /// The saved 3-dwelling model went from 172 KB to 3.7 MB over eleven runs (1.9 MB to 92 MB of JSON):
    /// the cluster's <c>DesignDay</c> records went 12, 26, 54, ... 28,670, 57,342 - <c>2n + 2</c> per run.
    /// </para>
    /// <para>
    /// The <c>2n</c> was the deep copy. A <c>DesignDay</c> has no Guid of its own, so the cluster re-keyed its
    /// clone and stored it BESIDE the original (fixed in SAM.Core). The <c>+2</c> was the workflow appending
    /// each run's design days instead of replacing them, which is also why the model still carried the London
    /// design days of a much earlier run next to its CIBSE Z1 ones (fixed in SAM_Tas,
    /// <c>Modify.ReplaceDesignDays</c>).
    /// </para>
    /// <para>
    /// Nothing sized or simulated from those records - the TBD is written from the run's own weather - so no
    /// TM59 result changed. The cost was size and time: exponential in the number of rounds.
    /// </para>
    /// <para>
    /// These drive the real <c>RunPartOSimulation</c> round after round, TAS replaced at its one seam
    /// (<see cref="PartOWorkflowRunner"/>) by a runner doing to the model what the workflow does to it here.
    /// </para>
    /// </summary>
    public class PartORunModelGrowthTests : IDisposable
    {
        private const int Rounds = 6;

        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartORunModelGrowthTests_" + Guid.NewGuid().ToString("N"));

        public PartORunModelGrowthTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                //Best effort: a temp folder left behind is not a test result.
            }
        }

        /// <summary>
        /// A TAS run that writes nothing back: only the ownership copy acts on the model. It must hand the
        /// next round the same objects it was given - before the SAM.Core fix, 12 design days became 768.
        /// </summary>
        [Fact]
        public void RepeatedRuns_DoNotMultiplyTheModelsRecords()
        {
            AnalyticalModel analyticalModel_Source = Model();
            Snapshot snapshot_Source = new(analyticalModel_Source);

            AnalyticalModel analyticalModel = analyticalModel_Source;
            for (int round = 1; round <= Rounds; round++)
            {
                analyticalModel = Run(analyticalModel, round, (model, _) => model);

                Snapshot snapshot = new(analyticalModel);
                Assert.True(snapshot_Source.DesignDays == snapshot.DesignDays, string.Format("Round {0}: the model carried {1} design days into the next round, not {2}.", round, snapshot.DesignDays, snapshot_Source.DesignDays));
                Assert.Equal(snapshot_Source.Objects, snapshot.Objects);
            }

            //And the caller's model is exactly as it was: the copy is the run's own.
            Assert.Equal(snapshot_Source.Objects, new Snapshot(analyticalModel_Source).Objects);
        }

        /// <summary>
        /// A run that records its design days as the workflow does. The model then carries exactly the last
        /// run's pair - none of the earlier rounds', and none of the London weather the model was first
        /// simulated on - and every design input the next round prepares from is unchanged.
        /// </summary>
        [Fact]
        public void RepeatedRuns_CarryOnlyTheCurrentRunsDesignDays_AndTheSameDesign()
        {
            AnalyticalModel analyticalModel_Source = Model();
            Snapshot snapshot_Source = new(analyticalModel_Source);

            AnalyticalModel analyticalModel = analyticalModel_Source;
            for (int round = 1; round <= Rounds; round++)
            {
                analyticalModel = Run(analyticalModel, round, (model, _) =>
                {
                    //As WorkflowCalculator does: the getter hands out a copy, so the model is rebuilt on it.
                    AdjacencyCluster adjacencyCluster = model.AdjacencyCluster;
                    adjacencyCluster.ReplaceDesignDays([new DesignDay("Z1 ANN CLG", 2018, 7, 1)], [new DesignDay("Z1 ANN HTG", 2018, 1, 1)]);
                    return new AnalyticalModel(model, adjacencyCluster);
                });

                List<DesignDay> designDays = analyticalModel.AdjacencyCluster.GetObjects<DesignDay>();
                Assert.Equal(["Z1 ANN CLG", "Z1 ANN HTG"], designDays.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));

                //The design the next round prepares from - identity, names, conditions, airflows, systems.
                Snapshot snapshot = new(analyticalModel);
                Assert.Equal(snapshot_Source.Design, snapshot.Design);
            }
        }

        // -----------------------------------------------------------------------------------------------

        private AnalyticalModel Run(AnalyticalModel analyticalModel, int round, Func<AnalyticalModel, WorkflowSettings, AnalyticalModel> workflow)
        {
            PartOSimulationContext partOSimulationContext = new(directory, "Flat1", null, SolarCalculationMethod.TAS, 1, 365);

            AnalyticalModel result = Modify.RunPartOSimulation(
                analyticalModel,
                partOSimulationContext,
                string.Format("Flat1-Opt{0:00}", round),
                null,
                CancellationToken.None,
                out _,
                out _,
                out bool cancelled,
                out _,
                out _,
                out string refusal,
                null,
                (AnalyticalModel model, WorkflowSettings workflowSettings, CancellationToken _, out bool cancelled_Workflow) =>
                {
                    cancelled_Workflow = false;
                    return workflow(model, workflowSettings);
                });

            Assert.True(refusal == null, refusal);
            Assert.False(cancelled);
            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// One dwelling, one space, one wall, a ventilation airflow - and the design days the live model
        /// carried: London from a much earlier run beside CIBSE Z1, several of each.
        /// </summary>
        private static AnalyticalModel Model()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space = new("Bedroom 1", new Point3D(5, 5, 1.5))
            {
                InternalCondition = new InternalCondition("Double Bedroom - Bedroom 1"),
            };
            space.SetValue(SpaceParameter.SupplyAirFlow, 0.012);

            Face3D face3D = new(new Polygon3D([new Point3D(0, 0, 0), new Point3D(10, 0, 0), new Point3D(10, 0, 3), new Point3D(0, 0, 3)]));
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "External Wall"), PanelType.WallExternal, face3D);

            Zone zone = new("Flat 1");

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddObject(zone);
            adjacencyCluster.AddRelation(space, panel);
            adjacencyCluster.AddRelation(zone, space);

            for (int i = 0; i < 4; i++)
            {
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("London ANN CLG 0% CONDS DB=>GRad", 2018, 7, 1), LoadType.Cooling));
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("London ANN HTG 100% CONDS DB", 2018, 1, 1), LoadType.Heating));
            }

            for (int i = 0; i < 2; i++)
            {
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("Z1 ANN CLG", 2018, 7, 1), LoadType.Cooling));
                adjacencyCluster.AddObject(new DesignDay(new DesignDay("Z1 ANN HTG", 2018, 1, 1), LoadType.Heating));
            }

            return new AnalyticalModel("Flat1", null, null, null, adjacencyCluster, new MaterialLibrary("Materials"), new ProfileLibrary("Profiles"));
        }

        /// <summary>What a round hands the next, reduced to what can be compared.</summary>
        private sealed class Snapshot
        {
            public Snapshot(AnalyticalModel analyticalModel)
            {
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

                DesignDays = adjacencyCluster.GetObjects<DesignDay>().Count;

                Objects = string.Join("; ", adjacencyCluster.GetObjects()
                    .GroupBy(x => x.GetType().Name)
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => string.Format("{0}={1}", x.Key, x.Count())));

                Design = string.Join("; ", adjacencyCluster.GetSpaces()
                    .OrderBy(x => x.Guid)
                    .Select(x => string.Format(
                        "{0}|{1}|{2}|{3}|panels={4}|zones={5}",
                        x.Guid,
                        x.Name,
                        x.InternalCondition?.Name,
                        x.TryGetValue(SpaceParameter.SupplyAirFlow, out double supplyAirFlow) ? supplyAirFlow : double.NaN,
                        string.Join(",", adjacencyCluster.GetPanels(x).Select(y => y.Guid).OrderBy(y => y)),
                        string.Join(",", adjacencyCluster.GetRelatedObjects<Zone>(x).Select(y => y.Name).OrderBy(y => y, StringComparer.Ordinal)))));
            }

            public int DesignDays { get; }

            public string Objects { get; }

            public string Design { get; }
        }
    }
}
