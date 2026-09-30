// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Tas;
using SAM.Analytical.UI;
using SAM.Core;
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
    /// <b>A prepared model carries the previous run's result link, and nothing trusts it.</b>
    ///
    /// <para><b>What acceptance found (30 Sep 2026)</b></para>
    /// <para>
    /// Preparation copies the open model, and after a run the open model IS that run's output. So Iteration 1b's
    /// <c>.prepared.sam</c> carried Iteration 1a's <c>SimulationResultProvenance</c> (its <c>Path_TSD</c> naming
    /// <c>Iteration1a\tas\….tsd</c>) and 1a's overheating scenarios, byte for byte, and Iteration 2's carried 1b's.
    /// Leftover state, not engineering input: the prepared model was never simulated as that record says.
    /// </para>
    ///
    /// <para><b>Why it is left in place</b></para>
    /// <para>
    /// Every reader either replaces it or checks it and refuses. <c>RunPartOSimulation</c> stamps the run's own
    /// scenarios and provenance on the workflow's model. <c>PartORun</c> keeps the scenarios itself, never reading
    /// them off the prepared model. The resume (<c>.partorun.json</c> + <c>.prepared.sam</c>) reads only the
    /// prepared model's cluster, zones and systems, bound by <c>SimulationResultProvenance.Fingerprint</c>, which
    /// leaves both parameters out. <c>PartORun.Restore</c> accepts a record only where the results file, the
    /// design fingerprint and the scenario fingerprint all still match.
    /// </para>
    /// <para>
    /// These tests drive that production path, with TAS replaced at its one seam (<see cref="PartOWorkflowRunner"/>),
    /// from a model that carries a previous case's link exactly as the acceptance files did. They require that the
    /// link is never trusted - not that it survives: a later clean-up that strips it keeps them green.
    /// </para>
    /// </summary>
    public class PartOPreparedModelCarriedLinkTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOCarriedLink_" + Guid.NewGuid().ToString("N"));

        private readonly string directory_Previous;

        private readonly string directory_Next;

        public PartOPreparedModelCarriedLinkTests()
        {
            directory_Previous = Path.Combine(directory, "Iteration1a", "tas");
            directory_Next = Path.Combine(directory, "Iteration1b", "tas");

            Directory.CreateDirectory(directory_Previous);
            Directory.CreateDirectory(directory_Next);
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

        [Fact]
        public void A_completed_run_is_linked_to_its_own_results_whatever_the_prepared_model_carried()
        {
            Case @case = Prepare();

            AnalyticalModel analyticalModel_Workflow = Run(@case, 365, out string path_TSD);

            //The run's own results, in its own folder, under the scenarios it was prepared with.
            Assert.True(@case.PartORun.Complete(analyticalModel_Workflow, path_TSD, Context(365), out string refusal), refusal);
            Assert.Equal(path_TSD, Provenance(analyticalModel_Workflow)!.Path_TSD);
            Assert.StartsWith(directory_Next, path_TSD, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(ScenarioTexts(@case.Scenarios_Next), ScenarioTexts(Scenarios(analyticalModel_Workflow)));

            //Nothing was written into the previous case's folder.
            Assert.Equal(new[] { @case.Path_TSD_Previous }, Directory.GetFiles(directory_Previous));

            //And the previous results are not needed by anything below: a link that were consumed would fail here.
            File.Delete(@case.Path_TSD_Previous);

            //What a later session reopens - the saved run model, its sidecar and the prepared model.
            string path_Model = Query.Path_PartORunModel(path_TSD);
            AnalyticalModel analyticalModel_Saved = Read(path_Model);

            PartORun partORun = new();
            Assert.True(partORun.Restore(analyticalModel_Saved, path_Model, out refusal), refusal);
            Assert.Equal(path_TSD, partORun.Path_TSD);
            Assert.Equal(ScenarioTexts(@case.Scenarios_Next), ScenarioTexts(partORun.OverheatingScenarios));
            Assert.True(partORun.CanResumeIteration3, partORun.ResumeRefusal);

            //Whatever the resumed prepared model carries, it names no result of this run and none is taken from it...
            AnalyticalModel analyticalModel_Prepared = partORun.AnalyticalModel_Prepared;
            Assert.NotEqual(path_TSD, Provenance(analyticalModel_Prepared)?.Path_TSD);

            //...and it is in no identity the resume or Iteration 3 is bound by: the fingerprint is the same without it.
            AnalyticalModel analyticalModel_Stripped = new(analyticalModel_Prepared);
            analyticalModel_Stripped.RemoveValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance);
            analyticalModel_Stripped.RemoveValue(Analytical.AnalyticalModelParameter.OverheatingScenarios);
            Assert.Equal(SimulationResultProvenance.Fingerprint(analyticalModel_Stripped), SimulationResultProvenance.Fingerprint(analyticalModel_Prepared));
            Assert.Equal(PartORunResume.Read(PartORunResume.Path_Resume(path_TSD))!.Fingerprint_PreparedModel, SimulationResultProvenance.Fingerprint(analyticalModel_Stripped));
        }

        /// <summary>
        /// Opening the <c>.prepared.sam</c> itself is never read as a result - not the previous case's, whose link it
        /// may carry, and not this run's.
        /// </summary>
        [Fact]
        public void The_prepared_model_opened_on_its_own_is_never_paired_with_the_previous_results()
        {
            Case @case = Prepare();

            Run(@case, 365, out string path_TSD);

            string path_Prepared = PartORunResume.Path_PreparedModel(path_TSD);
            AnalyticalModel analyticalModel_Prepared = Read(path_Prepared);

            PartORun partORun = new();
            Assert.False(partORun.Restore(analyticalModel_Prepared, path_Prepared, out string _));
            Assert.NotEqual(PartORunState.WorkflowCompleted, partORun.State);
            Assert.Null(partORun.Path_TSD);
        }

        /// <summary>
        /// A run that does not complete (not a full year) writes no record and stamps no link of its own. Whatever
        /// the model it returns carries - the one Simulate adopted as the open model before PR-4 - names none of this run's
        /// results, and reopened it is refused, never paired with the previous case's.
        /// </summary>
        [Fact]
        public void A_run_that_does_not_complete_leaves_the_carried_link_refused_on_reopen()
        {
            Case @case = Prepare();

            AnalyticalModel analyticalModel_Workflow = Run(@case, 1, out string path_TSD);

            Assert.NotEqual(path_TSD, Provenance(analyticalModel_Workflow)?.Path_TSD);
            Assert.Equal(ScenarioTexts(@case.Scenarios_Next), ScenarioTexts(Scenarios(analyticalModel_Workflow)));
            Assert.False(File.Exists(Query.Path_PartORunModel(path_TSD)));
            Assert.False(File.Exists(PartORunResume.Path_PreparedModel(path_TSD)));

            //What a Save of the open model and a later Open would give.
            string path_Model = Path.Combine(directory_Next, "Saved.sam");
            Assert.True(Core.Convert.ToFile(analyticalModel_Workflow, path_Model, SAMFileType.SAM));

            PartORun partORun = new();
            Assert.False(partORun.Restore(Read(path_Model), path_Model, out string _));
            Assert.NotEqual(PartORunState.WorkflowCompleted, partORun.State);
            Assert.Null(partORun.Path_TSD);
        }

        // -----------------------------------------------------------------------------------------------

        private sealed class Case
        {
            public PartORun PartORun { get; init; } = null!;

            public AnalyticalModel AnalyticalModel_Prepared { get; init; } = null!;

            public List<OverheatingScenario> Scenarios_Next { get; init; } = null!;

            public string Path_TSD_Previous { get; init; } = null!;
        }

        /// <summary>
        /// The previous case's output - its scenarios and its provenance to its own results - and the next case
        /// prepared from it: a copy with a changed design that still carries both, as SAM's preparation leaves them.
        /// </summary>
        private Case Prepare()
        {
            AnalyticalModel analyticalModel_Previous = Model();

            string path_TSD_Previous = Path.Combine(directory_Previous, "Flat1.tsd");
            File.WriteAllText(path_TSD_Previous, "previous results");

            analyticalModel_Previous.SetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>(Scenarios()));
            analyticalModel_Previous.SetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(analyticalModel_Previous, path_TSD_Previous));
            Assert.True(Provenance(analyticalModel_Previous)!.IsComplete);

            //Preparation: a copy, with the design changed.
            AdjacencyCluster adjacencyCluster = analyticalModel_Previous.AdjacencyCluster;
            Space space = new(adjacencyCluster.GetSpaces().Single());
            space.SetValue(SpaceParameter.SupplyAirFlow, 0.024);
            adjacencyCluster.AddObject(space);
            AnalyticalModel analyticalModel_Prepared = new(analyticalModel_Previous, adjacencyCluster);

            Assert.Equal(path_TSD_Previous, Provenance(analyticalModel_Prepared)!.Path_TSD);

            List<OverheatingScenario> scenarios_Next = Scenarios();
            Zone zone = analyticalModel_Prepared.AdjacencyCluster.GetObjects<Zone>().Single();

            PartORun partORun = new();
            Assert.True(partORun.Prepare(analyticalModel_Prepared, scenarios_Next, new PartOPreparationContext(PartOIteration.BasePassive, [zone], [], null)));

            //The fixture is one the Part O pre-simulation gate lets through, so the run below is really simulated.
            PartOPreSimulationCheck partOPreSimulationCheck = PartOPreSimulationCheck.Gate(partORun, analyticalModel_Prepared);
            Assert.True(partOPreSimulationCheck is null || partOPreSimulationCheck.IsValid, partOPreSimulationCheck is null ? null : string.Join("\n", partOPreSimulationCheck.Errors.Select(x => x.Text)));

            return new Case { PartORun = partORun, AnalyticalModel_Prepared = analyticalModel_Prepared, Scenarios_Next = scenarios_Next, Path_TSD_Previous = path_TSD_Previous };
        }

        private PartOSimulationContext Context(int simulateTo)
        {
            return new PartOSimulationContext(directory_Next, "Flat1", null, SolarCalculationMethod.TAS, 1, simulateTo);
        }

        /// <summary>The production simulation path, TAS replaced by a runner that writes this run's results.</summary>
        private AnalyticalModel Run(Case @case, int simulateTo, out string path_TSD)
        {
            AnalyticalModel result = Modify.RunPartOSimulation(
                @case.AnalyticalModel_Prepared,
                Context(simulateTo),
                "Flat1",
                @case.PartORun,
                CancellationToken.None,
                out _,
                out path_TSD,
                out bool cancelled,
                out _,
                out _,
                out string refusal,
                null,
                (AnalyticalModel model, WorkflowSettings workflowSettings, CancellationToken _, out bool cancelled_Workflow) =>
                {
                    cancelled_Workflow = false;
                    File.WriteAllText(Path.ChangeExtension(workflowSettings.Path_TBD, "tsd"), string.Format("results - {0}", Guid.NewGuid()));
                    return model;
                });

            Assert.True(refusal == null, refusal);
            Assert.False(cancelled);
            Assert.NotNull(result);

            return result;
        }

        private static AnalyticalModel Read(string path)
        {
            List<AnalyticalModel>? analyticalModels = Core.Convert.ToSAM<AnalyticalModel>(path);
            Assert.NotNull(analyticalModels);

            return Assert.Single(analyticalModels!);
        }

        private static SimulationResultProvenance? Provenance(AnalyticalModel analyticalModel)
        {
            return analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance, out SimulationResultProvenance simulationResultProvenance) ? simulationResultProvenance : null;
        }

        private static List<OverheatingScenario> Scenarios(AnalyticalModel analyticalModel)
        {
            return analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.OverheatingScenarios, out SAMCollection<OverheatingScenario> collection) && collection is not null ? [.. collection] : [];
        }

        private static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }

        /// <summary>A scenario set as comparable text - each scenario's own JSON, in a fixed order.</summary>
        private static List<string> ScenarioTexts(IEnumerable<OverheatingScenario> overheatingScenarios)
        {
            return [.. overheatingScenarios.Select(x => x.ToJsonObject().ToJsonString()).OrderBy(x => x, StringComparer.Ordinal)];
        }

        /// <summary>One dwelling, one space, one wall - the shape <c>PartORunModelGrowthTests</c> simulates, with the area and fabric the pre-simulation gate asks for.</summary>
        private static AnalyticalModel Model()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space = new("Bedroom 1", new Point3D(5, 5, 1.5))
            {
                InternalCondition = new InternalCondition("Double Bedroom - Bedroom 1"),
            };
            space.SetValue(SpaceParameter.SupplyAirFlow, 0.012);
            space.SetValue(SpaceParameter.Area, 25.0);
            space.SetValue(SpaceParameter.Volume, 62.5);

            Face3D face3D = new(new Polygon3D([new Point3D(0, 0, 0), new Point3D(10, 0, 0), new Point3D(10, 0, 3), new Point3D(0, 0, 3)]));
            Panel panel = Analytical.Create.Panel(new Construction(Guid.NewGuid(), "External Wall", [new ConstructionLayer("Concrete", 0.2)]), PanelType.WallExternal, face3D);

            Zone zone = new("Flat 1");

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddObject(zone);
            adjacencyCluster.AddRelation(space, panel);
            adjacencyCluster.AddRelation(zone, space);

            MaterialLibrary materialLibrary = new("Materials");
            materialLibrary.Add(new OpaqueMaterial(Guid.NewGuid(), "Concrete", "Concrete", "Fixture", 2.3, 2300, 1000));

            return new AnalyticalModel("Flat1", null, null, null, adjacencyCluster, materialLibrary, new ProfileLibrary("Profiles"));
        }
    }
}
