// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// U-value PR3: <c>Modify.SetGlazing</c>. One Apply is one Undo step (one <c>SetJSAMObject</c>, one snapshot); only
    /// the chosen system and the materials the model lacks enter the model; the apertures are re-assigned per scope with
    /// their parameters refreshed; a failure changes nothing.
    /// </summary>
    public class SetGlazingTests
    {
        // What the Tas calculation of the chosen system answers (Ug 1.10, g 0.50, light 0.70 ...).
        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static async Task<SetGlazingRequest> Request(AnalyticalModel analyticalModel, Guid systemGuid, GlazingApplyScope scope = GlazingApplyScope.AllApertures, IEnumerable<Guid> selected = null)
        {
            GlazingViewModel viewModel = GlazingFixture.ViewModel(analyticalModel, selected);
            await viewModel.InitializeAsync();
            viewModel.ApplyScope = scope;
            viewModel.SelectedGuid = systemGuid;
            SetGlazingRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);
            return request;
        }

        private static AnalyticalModel Apply(AnalyticalModel analyticalModel, SetGlazingRequest request, out SetGlazingResult result)
        {
            return Modify.SetGlazing(analyticalModel, request, Tas(request), out result);
        }

        // ---- What changes --------------------------------------------------------------------------------------

        [Fact]
        public async Task EveryApertureUsingTheCurrentSystem_GetsTheChosenOne()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(20);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(20, result.ApertureCount);
            List<Aperture> apertures = changed.AdjacencyCluster.GetApertures();
            Assert.Equal(20, apertures.Count);
            Assert.All(apertures, x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
            Assert.Equal(20, result.PanelGuids.Count);
        }

        [Fact]
        public async Task TheApertureKeepsItsIdentityAndGeometry_OnlyTheSystemChanges()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            Aperture before = analyticalModel.AdjacencyCluster.GetApertures().First();
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult _);

            Aperture after = changed.AdjacencyCluster.GetAperture(before.Guid);
            Assert.NotNull(after);
            Assert.Equal(before.GetArea(), after.GetArea(), 6);
        }

        [Fact]
        public async Task OnlyTheChosenSystemsMissingMaterialsEnterTheModel()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(4);
            int materialsBefore = analyticalModel.MaterialLibrary.Count;
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Equal(new[] { GlazingFixture.LowE }, result.MaterialNamesAdded.ToArray());
            Assert.Equal(materialsBefore + 1, changed.MaterialLibrary.Count);
            Assert.NotNull(changed.MaterialLibrary.GetMaterial(GlazingFixture.LowE));
            // The library's other materials (and the other candidates') are not copied.
            Assert.Null(changed.MaterialLibrary.GetMaterial("Mystery"));
        }

        [Fact]
        public async Task TheChosenSystem_EntersTheModelUnderAUniqueName_AndTheSourceStaysStored()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(4);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            // "GLZ" is taken by the current system: the chosen one (also called GLZ) becomes "GLZ 2".
            Assert.True(result.ApertureConstructionAdded);
            Assert.Equal("GLZ 2", result.ApertureConstruction.Name);
            Assert.Equal(GlazingFixture.BetterGuid, result.ApertureConstruction.Guid);

            List<ApertureConstruction> systems = changed.AdjacencyCluster.GetApertureConstructions();
            Assert.Contains(systems, x => x.Guid == GlazingFixture.BetterGuid && x.Name == "GLZ 2");

            // No silent deletes: the system the apertures left stays in the model (unused), once.
            Assert.Single(systems.Where(x => x.Guid == GlazingFixture.CurrentGuid));
            Assert.Empty(changed.AdjacencyCluster.GetApertures(systems.First(x => x.Guid == GlazingFixture.CurrentGuid)));
        }

        [Fact]
        public async Task ASystemTheModelAlreadyHas_IsNotAddedAgain()
        {
            ApertureConstruction other = GlazingFixture.System(new Guid("a0000000-0000-4000-8000-0000000000aa"), "Other", ApertureType.Window, GlazingFixture.Clear);
            AnalyticalModel analyticalModel = GlazingFixture.Model(5, other, 2);
            SetGlazingRequest request = await Request(analyticalModel, other.Guid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.False(result.ApertureConstructionAdded);
            Assert.Empty(result.MaterialNamesAdded);
            Assert.Single(changed.AdjacencyCluster.GetApertureConstructions().Where(x => x.Guid == other.Guid));
            Assert.Equal(7, changed.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == other.Guid));
        }

        [Fact]
        public async Task AperturesOnOtherSystems_AreNotTouched()
        {
            ApertureConstruction other = GlazingFixture.System(new Guid("a0000000-0000-4000-8000-0000000000aa"), "Other", ApertureType.Window, GlazingFixture.Clear);
            AnalyticalModel analyticalModel = GlazingFixture.Model(5, other, 3);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Equal(5, result.ApertureCount);
            Assert.Equal(3, changed.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == other.Guid));
        }

        // ---- Scope ---------------------------------------------------------------------------------------------

        [Fact]
        public async Task SelectedApertures_AreTheOnlyOnesChanged()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(10);
            List<Guid> selected = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid).Take(3).ToList();
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid, GlazingApplyScope.SelectedApertures, selected);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Equal(3, result.ApertureCount);
            Assert.Equal(selected.OrderBy(x => x), result.ApertureGuids.OrderBy(x => x));
            Assert.Equal(3, changed.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.BetterGuid));
            Assert.Equal(7, changed.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.CurrentGuid));
        }

        [Fact]
        public async Task DontAssign_AddsTheSystemAndItsMaterials_ButChangesNoAperture()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(6);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid, GlazingApplyScope.DontAssign);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(0, result.ApertureCount);
            Assert.All(changed.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.CurrentGuid, x.TypeGuid));
            Assert.Contains(changed.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == GlazingFixture.BetterGuid);
            Assert.NotNull(changed.MaterialLibrary.GetMaterial(GlazingFixture.LowE));
        }

        [Fact]
        public async Task SelectedScope_WithNoSelectedApertureUsingTheSystem_IsRefused()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(6);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);
            request.Scope = GlazingApplyScope.SelectedApertures;
            request.SelectedApertureGuids = new[] { Guid.NewGuid() };

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Null(changed);
            Assert.False(result.Succeeded);
            Assert.Equal("None of the selected apertures uses GLZ.", result.Error);
        }

        // ---- Parameters ----------------------------------------------------------------------------------------

        [Fact]
        public async Task TheApertureParameters_AreRefreshedFromTheTasCalculationOfTheChosenSystem()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult _);

            foreach (Aperture aperture in changed.AdjacencyCluster.GetApertures())
            {
                Assert.True(aperture.TryGetValue(ApertureParameter.ThermalTransmittance, out double u));
                Assert.Equal(1.10, u);
                Assert.True(aperture.TryGetValue(ApertureParameter.TotalSolarEnergyTransmittance, out double g));
                Assert.Equal(0.50, g);
                Assert.True(aperture.TryGetValue(ApertureParameter.LightTransmittance, out double light));
                Assert.Equal(0.70, light);
                Assert.True(aperture.TryGetValue(ApertureParameter.DirectSolarEnergyAbsorptance, out double absorptance));
                Assert.Equal(0.30, absorptance);
                Assert.True(aperture.TryGetValue(ApertureParameter.PilkingtonShadingShortWavelengthCoefficient, out double pilkington));
                Assert.Equal(0.60, pilkington);
            }
        }

        [Fact]
        public async Task WithoutATasResult_TheComparisonValuesAreUsed()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(2);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            AnalyticalModel changed = Modify.SetGlazing(analyticalModel, request, null, out SetGlazingResult result);

            Assert.True(result.Succeeded, result.Error);
            Aperture aperture = changed.AdjacencyCluster.GetApertures().First();
            Assert.True(aperture.TryGetValue(ApertureParameter.ThermalTransmittance, out double u));
            Assert.Equal(1.10, u);
            Assert.True(aperture.TryGetValue(ApertureParameter.TotalSolarEnergyTransmittance, out double g));
            Assert.Equal(0.50, g);
        }

        // ---- Refusals ------------------------------------------------------------------------------------------

        [Fact]
        public async Task ASystemWhoseMaterialIsNotInTheModelNorSupplied_IsRefused_AndNothingChanges()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);
            request.MaterialsToAdd = new List<IMaterial>();

            AnalyticalModel changed = Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Null(changed);
            Assert.Equal("Material LowE6 of GLZ is not in the Material Library.", result.Error);
        }

        [Fact]
        public void TheCurrentSystemMustStillBeInTheModel()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            SetGlazingRequest request = new SetGlazingRequest() { SourceApertureConstructionGuid = Guid.NewGuid(), ApertureConstruction = GlazingFixture.Current() };

            AnalyticalModel changed = Modify.SetGlazing(analyticalModel, request, null, out SetGlazingResult result);

            Assert.Null(changed);
            Assert.Equal("The current aperture construction is no longer in the model.", result.Error);
        }

        [Fact]
        public void NothingToApplyIsRefused()
        {
            Assert.Null(Modify.SetGlazing((AnalyticalModel)null, new SetGlazingRequest(), null, out SetGlazingResult result_1));
            Assert.False(result_1.Succeeded);
            Assert.Null(Modify.SetGlazing(GlazingFixture.Model(1), null, null, out SetGlazingResult result_2));
            Assert.False(result_2.Succeeded);
        }

        [Fact]
        public async Task TheSourceModelIsNotModified()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(4);
            string before = analyticalModel.ToJsonObject().ToJsonString();
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            Apply(analyticalModel, request, out SetGlazingResult _);

            Assert.Equal(before, analyticalModel.ToJsonObject().ToJsonString());
        }

        // ---- One Apply, one Undo -------------------------------------------------------------------------------

        [Fact]
        public async Task Apply_CallsSetJSAMObjectOnce_AddingExactlyOneUndoSnapshot()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(8);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            int historyChanged = 0;
            int calculations = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => historyChanged++;
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);

            SetGlazingResult result = Modify.SetGlazing(uIAnalyticalModel, request, x => { calculations++; return Tas(x); });

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(1, modified);
            Assert.Equal(1, historyChanged);
            Assert.Equal(1, calculations);
            Assert.True(uIAnalyticalModel.CanUndo);
            Assert.False(uIAnalyticalModel.CanRedo);
            Assert.Equal(8, uIAnalyticalModel.JSAMObject.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.BetterGuid));

            // The restore itself runs off the UI thread (UIJSAMObject.Undo); that one Undo restores every aperture and leaves
            // no new system and no orphan material is checked in the real app (PR record).
        }

        [Fact]
        public async Task FailedApply_DoesNotTouchTheModelOrTheUndoHistory()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int modified = 0;
            uIAnalyticalModel.Modified += (sender, e) => modified++;
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);
            request.MaterialsToAdd = new List<IMaterial>();

            SetGlazingResult result = Modify.SetGlazing(uIAnalyticalModel, request, Tas);

            Assert.False(result.Succeeded);
            Assert.Equal(0, modified);
            Assert.False(uIAnalyticalModel.CanUndo);
        }

        [Fact]
        public async Task DontAssign_NeedsNoTasCalculation()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(3);
            UIAnalyticalModel uIAnalyticalModel = new UIAnalyticalModel(analyticalModel);
            int calculations = 0;
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid, GlazingApplyScope.DontAssign);

            SetGlazingResult result = Modify.SetGlazing(uIAnalyticalModel, request, x => { calculations++; return Tas(x); });

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(0, calculations);
        }

        [Fact]
        public async Task Result_CarriesWhatTheReportNeeds()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model(5);
            SetGlazingRequest request = await Request(analyticalModel, GlazingFixture.BetterGuid);
            request.TargetUw = 1.25;

            Apply(analyticalModel, request, out SetGlazingResult result);

            Assert.Equal(GlazingApplyScope.AllApertures, result.Scope);
            Assert.Equal(GlazingFixture.CurrentGuid, result.SourceApertureConstruction.Guid);
            Assert.Equal(1.40, result.OldValues.Ug);
            Assert.Equal(1.10, result.Values.Ug);
            Assert.Equal(1.25, result.TargetUw);
            Assert.Equal(GlazingUwBasis.Area, result.UwBasis);
            Assert.True(result.NewUw < result.OldUw);
            Assert.True((DateTime.Now - result.AppliedAt).TotalMinutes < 1);
        }
    }
}
