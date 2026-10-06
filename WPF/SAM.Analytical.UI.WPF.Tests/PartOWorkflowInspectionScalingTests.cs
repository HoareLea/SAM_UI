// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// What ONE Part O inspection asks of the model, counted.
    ///
    /// <para><b>Why this is asserted here and not with a stopwatch</b></para>
    /// <para>
    /// The inspection reads every room of the dwelling scope three times - once to place it, and once for
    /// each of the two Approved Document F directions - and each of those reads used to re-resolve the room
    /// against the model's whole space list, so inspecting a five thousand space project was quadratic. The
    /// fix is a single <c>PartFIndex</c> snapshot built in SAM and threaded through the inspection, and the
    /// property that has to hold is a <b>count</b>, not a duration: three resolutions per room in scope, two
    /// requirements per room in scope, and no second snapshot. Those numbers are the same on every machine,
    /// and they fail immediately if anybody moves a lookup back inside the loop.
    /// </para>
    ///
    /// <para><b>Nothing is cached in WPF, and this file would notice</b></para>
    /// <para>
    /// The counts below are per inspection. Two inspections of the same model cost exactly twice one - the
    /// inspection remembers nothing between calls, which is the workflow semantics PR #85 established and
    /// this change deliberately did not touch. A cache anywhere in the dialog would show up here as a second
    /// inspection costing nothing.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowInspectionScalingTests
    {
        /// <summary>
        /// A snapshot that reports what it was asked. It answers nothing differently: every method calls its
        /// base.
        /// </summary>
        private sealed class CountingPartFIndex : PartFIndex
        {
            internal int Resolutions;

            internal int Requirements;

            internal CountingPartFIndex(AdjacencyCluster adjacencyCluster)
                : base(adjacencyCluster)
            {
            }

            public override Space Space(Guid guid)
            {
                Resolutions++;

                return base.Space(guid);
            }

            public override double? PartFRequiredFlowRate_Lps(Space space, FlowClassification flowClassification)
            {
                Requirements++;

                return base.PartFRequiredFlowRate_Lps(space, flowClassification);
            }
        }

        /// <summary>
        /// One inspection asks a number of questions that is linear in the dwelling scope and independent of
        /// how large the rest of the model is.
        /// </summary>
        [Theory]
        [InlineData(60)]
        [InlineData(300)]
        [InlineData(1200)]
        public void OneInspection_AsksALinearNumberOfQuestionsOfOneSnapshot(int count)
        {
            AnalyticalModel analyticalModel = Model(count);

            CountingPartFIndex partFIndex = new(analyticalModel.AdjacencyCluster);

            PartOWorkflowInspection partOWorkflowInspection = Inspect(analyticalModel, partFIndex);

            //The stage really ran - a blocked or skipped stage would make the counts below meaningless.
            Assert.Equal(PartOWorkflowStageStatus.Ready, Stage(partOWorkflowInspection, PartOWorkflowStage.PartFRequirements).Status);

            //Supply and extract for each room of the scope, and nothing else.
            Assert.Equal(count * 2, partFIndex.Requirements);

            //One resolution to place each room, and one behind each requirement.
            Assert.Equal(count * 3, partFIndex.Resolutions);
        }

        /// <summary>
        /// Two inspections of the same model cost exactly twice one. The inspection remembers nothing
        /// between calls; a cache in the dialog would show as the second inspection asking less.
        /// </summary>
        [Fact]
        public void TwoInspections_CostExactlyTwiceOne()
        {
            AnalyticalModel analyticalModel = Model(300);

            CountingPartFIndex partFIndex = new(analyticalModel.AdjacencyCluster);

            Inspect(analyticalModel, partFIndex);

            int requirements = partFIndex.Requirements;
            int resolutions = partFIndex.Resolutions;

            Inspect(analyticalModel, partFIndex);

            Assert.Equal(requirements * 2, partFIndex.Requirements);
            Assert.Equal(resolutions * 2, partFIndex.Resolutions);
        }

        /// <summary>
        /// The snapshot changes no answer. An inspection given one reports exactly what an inspection that
        /// builds its own reports - same stage statuses, same detail sentences, same blockers.
        /// </summary>
        [Theory]
        [InlineData(60)]
        [InlineData(300)]
        public void ASuppliedSnapshot_ReportsExactlyWhatProductionReports(int count)
        {
            AnalyticalModel analyticalModel = Model(count);

            PartOWorkflowInspection partOWorkflowInspection_Production = Inspect(analyticalModel, null);
            PartOWorkflowInspection partOWorkflowInspection_Supplied = Inspect(analyticalModel, new CountingPartFIndex(analyticalModel.AdjacencyCluster));

            Assert.Equal(partOWorkflowInspection_Production.CanRun, partOWorkflowInspection_Supplied.CanRun);
            Assert.Equal(partOWorkflowInspection_Production.Blockers, partOWorkflowInspection_Supplied.Blockers);

            Assert.Equal(partOWorkflowInspection_Production.Stages.Count, partOWorkflowInspection_Supplied.Stages.Count);

            for (int i = 0; i < partOWorkflowInspection_Production.Stages.Count; i++)
            {
                Assert.Equal(partOWorkflowInspection_Production.Stages[i].Stage, partOWorkflowInspection_Supplied.Stages[i].Stage);
                Assert.Equal(partOWorkflowInspection_Production.Stages[i].Status, partOWorkflowInspection_Supplied.Stages[i].Status);
                Assert.Equal(partOWorkflowInspection_Production.Stages[i].Detail, partOWorkflowInspection_Supplied.Stages[i].Detail);
            }
        }

        /// <summary>
        /// The count the Approved Document F stage reports is the count the one-space query would produce -
        /// the authority, asked directly here rather than restated.
        /// </summary>
        [Fact]
        public void TheReportedRequirementCount_IsWhatTheOneSpaceQuerySays()
        {
            AnalyticalModel analyticalModel = Model(300);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            int count = 0;

            foreach (Space space in adjacencyCluster.GetSpaces())
            {
                double? supply = Analytical.Query.PartFRequiredFlowRate_Lps(adjacencyCluster, space, FlowClassification.Supply);
                double? extract = Analytical.Query.PartFRequiredFlowRate_Lps(adjacencyCluster, space, FlowClassification.Extract);

                if ((supply.HasValue && supply.Value > 0) || (extract.HasValue && extract.Value > 0))
                {
                    count++;
                }
            }

            string detail = Stage(Inspect(analyticalModel, null), PartOWorkflowStage.PartFRequirements).Detail;

            Assert.Contains(string.Format("{0} of 300 spaces", count), detail);
        }

        // ---- Fixture ------------------------------------------------------------------------------------

        /// <summary>
        /// A block of flats of <paramref name="count"/> rooms, three rooms to a dwelling: a bedroom with a
        /// continuous supply requirement, a kitchen with a continuous extract requirement, and a store with
        /// no Approved Document F data at all.
        /// </summary>
        private static AnalyticalModel Model(int count)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 0; i < count; i += 3)
            {
                Zone zone = new(string.Format("Flat {0}", i / 3));
                zone.SetValue(ZoneParameter.IsDwelling, true);
                zone.SetValue(ZoneParameter.ZoneCategory, "Flats");

                adjacencyCluster.AddObject(zone);

                Room(adjacencyCluster, zone, string.Format("Bedroom {0}", i), "TM59_Double Bedroom", PartFTerminalRole.Supply);
                Room(adjacencyCluster, zone, string.Format("Kitchen {0}", i + 1), "TM59_Kitchen", PartFTerminalRole.GeneralExtract);
                Room(adjacencyCluster, zone, string.Format("Store {0}", i + 2), "TM59_Store", PartFTerminalRole.Undefined);
            }

            return new AnalyticalModel(
                "Block",
                null,
                null,
                null,
                adjacencyCluster,
                new MaterialLibrary("Materials"),
                new ProfileLibrary("Profiles"));
        }

        private static void Room(AdjacencyCluster adjacencyCluster, Zone zone, string name, string internalConditionName, PartFTerminalRole partFTerminalRole)
        {
            Space space = new(name, null)
            {
                InternalCondition = new InternalCondition(internalConditionName),
            };

            if (partFTerminalRole != PartFTerminalRole.Undefined)
            {
                PartFSpaceData partFSpaceData = new();

                partFSpaceData.Terminals.Add(new PartFVentilationTerminalRequirement(name + " requirement", space.Guid, partFTerminalRole)
                {
                    ContinuousDesignFlowRate_Lps = 13,
                });

                space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);
            }

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddRelation(zone, space);
        }

        private static PartOWorkflowInspection Inspect(AnalyticalModel analyticalModel, PartFIndex partFIndex)
        {
            PartOWorkflowScenario partOWorkflowScenario = PartOWorkflowScenario.Scenarios.Find(x => x.Option.PartOIteration == PartOIteration.BasePassive && !x.SelectVentilationUnit);

            PartOWorkflowRequest partOWorkflowRequest = new(
                partOWorkflowScenario.Option,
                PartOWorkflowScope.AllDwellings,
                analyticalModel.GetZones(),
                partOWorkflowScenario.SelectVentilationUnit);

            return PartOWorkflowInspection.Inspect(
                analyticalModel,
                partOWorkflowRequest,
                null,
                new PartOWorkflowCapabilities { EquipmentAvailable = true },
                Analytical.Query.DefaultInternalConditionTextMap_TM59(),
                partFIndex);
        }

        private static PartOWorkflowStageState Stage(PartOWorkflowInspection partOWorkflowInspection, PartOWorkflowStage partOWorkflowStage)
        {
            foreach (PartOWorkflowStageState partOWorkflowStageState in partOWorkflowInspection.Stages)
            {
                if (partOWorkflowStageState.Stage == partOWorkflowStage)
                {
                    return partOWorkflowStageState;
                }
            }

            throw new Xunit.Sdk.XunitException(string.Format("The inspection reported no '{0}' stage.", partOWorkflowStage));
        }
    }
}
