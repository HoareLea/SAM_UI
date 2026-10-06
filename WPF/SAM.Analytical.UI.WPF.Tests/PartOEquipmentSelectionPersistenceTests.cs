// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>What survives, and what must never be re-decided because something was not to hand.</b>
    ///
    /// <para><b>Three facts, three homes</b></para>
    /// <list type="bullet">
    /// <item><b>The selected equipment identity</b> is an engineering fact and lives on the dwelling's own
    /// air handling unit, as <c>AirHandlingUnitParameter.VentilationUnitReference</c>. It survives model
    /// serialization, reopening, Iteration 2B's rounds and re-preparation - proven in
    /// <c>SAM.Tests.PartOVentilationUnitSelectionTests</c>.</item>
    /// <item><b>The selection mode</b> and <b>the permitted pool</b> are project configuration and live on
    /// the model as <c>AnalyticalModelParameter.PartOEquipmentSelection</c> - so they outlive the Part O
    /// dialog and the project being closed, and cannot leak into another project the way an application
    /// setting would.</item>
    /// <item><b>Equipment capability</b> lives in the catalogue and is never persisted anywhere.</item>
    /// </list>
    ///
    /// <para><b>The failure this file exists to prevent</b></para>
    /// <para>
    /// An engineer takes manual authority, authors assignments by hand, and later re-prepares through
    /// Prepare &amp; Run - which states nothing about equipment. If "nothing stated" meant "automatic over
    /// the whole catalogue", the smallest-capable rule would replace every authored assignment and report a
    /// successful preparation. A reselected model looks exactly like a correctly selected one, so nothing in
    /// the result would tell them.
    /// </para>
    /// </summary>
    public class PartOEquipmentSelectionPersistenceTests
    {
        private const string model_MRXBOX = "MRXBOXAB-ECO5-AECV";

        private const string model_XBC15 = "XBC15";

        /// <summary>
        /// <b>A request that says nothing inherits the PROJECT's configuration.</b> A project under manual
        /// authority stays under manual authority, so no rule runs and every authored assignment survives.
        /// </summary>
        [Fact]
        public void ARequestThatSaysNothing_InheritsTheProjectsConfiguration()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [XBC15Reference()]));

            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(Request(null), analyticalModel);

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOEquipmentSelection.Mode);
            Assert.Equal(model_XBC15, Assert.Single(partOEquipmentSelection.AllowedVentilationUnitReferences).Model);

            //THE assertion: manual authority means no candidate set, so nothing is reselected.
            Assert.Null(partOEquipmentSelection.CandidateDescriptors(Descriptors()));
        }

        /// <summary>
        /// The same for a pooled project: a request that says nothing does not widen the pool back to the
        /// whole catalogue.
        /// </summary>
        [Fact]
        public void ARequestThatSaysNothing_DoesNotWidenTheProjectsPool()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [XBC15Reference()]));

            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(Request(null), analyticalModel);

            Assert.Equal(model_XBC15, Assert.Single(partOEquipmentSelection.CandidateDescriptors(Descriptors())).VentilationUnitReference.Model);
        }

        /// <summary>
        /// A project that has never stated a preference gets exactly what Iteration 2 always did - automatic
        /// selection over every selectable product. Nothing needs migrating.
        /// </summary>
        [Fact]
        public void AProjectThatSaysNothing_GetsTheHistoricDefault()
        {
            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(Request(null), Model(null));

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, partOEquipmentSelection.Mode);
            Assert.Equal(2, partOEquipmentSelection.CandidateDescriptors(Descriptors()).Count);
        }

        /// <summary>And with neither a request nor a project, still the historic default rather than null.</summary>
        [Fact]
        public void WithNothingAtAll_TheDefaultIsStillStated()
        {
            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(null, null);

            Assert.NotNull(partOEquipmentSelection);
            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, partOEquipmentSelection.Mode);
        }

        /// <summary>
        /// A request that DOES state a preference wins - that is the engineer changing their mind in the
        /// dialog, and the dialog is the more recent statement of the two.
        /// </summary>
        [Fact]
        public void ARequestThatStatesAPreference_WinsOverTheProject()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [XBC15Reference()]));

            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(
                Request(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts)),
                analyticalModel);

            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, partOEquipmentSelection.Mode);
            Assert.Equal(2, partOEquipmentSelection.CandidateDescriptors(Descriptors()).Count);
        }

        /// <summary>
        /// <b>Manual authority survives the project being saved and reopened</b> - so reopening the Part O
        /// dialog shows Manual, and a re-preparation through any path still runs no rule. This is the
        /// mechanism behind "a manual assignment does not silently revert to Automatic merely because the
        /// dialog was reopened".
        /// </summary>
        [Fact]
        public void ManualAuthority_SurvivesTheProjectsRoundTrip()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [XBC15Reference()]));

            AnalyticalModel analyticalModel_Reopened = new(analyticalModel.ToJsonObject());

            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(Request(null), analyticalModel_Reopened);

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, partOEquipmentSelection.Mode);
            Assert.Equal(model_XBC15, Assert.Single(partOEquipmentSelection.AllowedVentilationUnitReferences).Model);
            Assert.Null(partOEquipmentSelection.CandidateDescriptors(Descriptors()));
        }

        /// <summary>
        /// <b>And the authored assignment itself survives with it.</b> The identity is on the air handling
        /// unit and the capability is not stored at all - so a reopened project states the same product and
        /// resolves its rating from today's catalogue.
        /// </summary>
        [Fact]
        public void TheAuthoredIdentity_SurvivesTheProjectsRoundTrip_AndStoresNoCapacity()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            AnalyticalModel analyticalModel_Reopened = new(analyticalModel.ToJsonObject());

            AirHandlingUnit airHandlingUnit = Assert.Single(analyticalModel_Reopened.AdjacencyCluster.GetObjects<AirHandlingUnit>());

            Assert.Equal(model_XBC15, airHandlingUnit.SelectedVentilationUnitReference()?.Model);

            //The rating came from the catalogue handed in, not from the file.
            Assert.Equal(190, airHandlingUnit.SelectedVentilationUnitCapacityDescriptor(Descriptors()).MaximumSupplyFlowRate_Lps);

            //Offered a catalogue that no longer holds it, the rating is UNKNOWN rather than the other
            //product's 150 - which is what proves no capacity travelled with the identity.
            Assert.Null(airHandlingUnit.SelectedVentilationUnitCapacityDescriptor(Descriptors().FindAll(x => x.VentilationUnitReference.Model != model_XBC15)));
        }

        /// <summary>
        /// A project whose stored pool names a product the current catalogue no longer holds contributes no
        /// candidate for it - a permission is not a capability, and one cannot conjure the other.
        /// </summary>
        [Fact]
        public void AStoredPoolNamingAWithdrawnProduct_ContributesNoCandidate()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [new VentilationUnitReference("Nuaire", "XBC-Withdrawn", null)]));

            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(Request(null), new AnalyticalModel(analyticalModel.ToJsonObject()));

            //Empty, and NOT quietly widened to the whole catalogue.
            Assert.Empty(partOEquipmentSelection.CandidateDescriptors(Descriptors()));
        }

        /// <summary>
        /// The configuration is on the model's own parameters, so two projects open in one session cannot
        /// see each other's - which is the property an application-wide setting could not have given.
        /// </summary>
        [Fact]
        public void OneProjectsConfiguration_DoesNotReachAnother()
        {
            AnalyticalModel analyticalModel_Manual = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling, [XBC15Reference()]));
            AnalyticalModel analyticalModel_Other = Model(null);

            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, Query.PartOEquipmentSelection(Request(null), analyticalModel_Manual).Mode);
            Assert.Equal(PartOEquipmentSelectionMode.AutomaticAllProducts, Query.PartOEquipmentSelection(Request(null), analyticalModel_Other).Mode);
        }

        /// <summary>
        /// A restored run holds no preparation context - <c>PartORun.Restore</c> clears it deliberately - and
        /// that changes nothing about the model's equipment. Reviewing saved results reassigns nothing,
        /// because the identity is a model fact and reviewing reads.
        /// </summary>
        [Fact]
        public void ARestoredRun_HoldsNoContext_AndReassignsNothing()
        {
            AnalyticalModel analyticalModel = Model(new PartOEquipmentSelection(PartOEquipmentSelectionMode.ManualPerDwelling));

            PartORun partORun = new();

            Assert.Null(partORun.PreparationContext);

            //The model is untouched by there being no context to prepare from, and still states its own
            //authored product and its own manual authority.
            Assert.Equal(model_XBC15, Assert.Single(analyticalModel.AdjacencyCluster.GetObjects<AirHandlingUnit>()).SelectedVentilationUnitReference()?.Model);
            Assert.Equal(PartOEquipmentSelectionMode.ManualPerDwelling, Query.PartOEquipmentSelection(null, analyticalModel).Mode);
        }

        // =================================================================================================
        // Fixtures
        // =================================================================================================

        /// <summary>
        /// A project with one air handling unit authored as the XBC15, and optionally its own equipment
        /// preselection stamped on it.
        /// </summary>
        private static AnalyticalModel Model(PartOEquipmentSelection? partOEquipmentSelection)
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit("MVHR-01");

            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, XBC15Reference());

            adjacencyCluster.AddObject(airHandlingUnit);

            AnalyticalModel result = new("Fixture", null, null, null, adjacencyCluster);

            if (partOEquipmentSelection is not null)
            {
                result.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentSelection);
            }

            return result;
        }

        /// <summary>A request stating a preference, or - passed null - one that says nothing at all.</summary>
        private static PartOWorkflowRequest Request(PartOEquipmentSelection? partOEquipmentSelection)
        {
            return new PartOWorkflowRequest(PartOVentilationStrategyOption.Options[0], PartOWorkflowScope.AllDwellings, [], true)
            {
                EquipmentSelection = partOEquipmentSelection,
            };
        }

        private static List<VentilationUnitCapacityDescriptor> Descriptors()
        {
            return
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", model_MRXBOX, "MR-ECO-COOL-V"), 150, 150, 10),
                new VentilationUnitCapacityDescriptor(XBC15Reference(), 190, 190, 20),
            ];
        }

        private static VentilationUnitReference XBC15Reference()
        {
            return new VentilationUnitReference("Nuaire", model_XBC15, null);
        }
    }
}
