// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage A-lite: the one scope model both "Set U-value" and "Set glazing" use - the counts, the choice labels, the
    /// sentence, the "only the selected" availability - and that it is pinned (what Apply touches is fixed when the view-model
    /// is created, whatever the selection does afterwards) and read-only on the model.
    /// </summary>
    public class ThermalScopeTests
    {
        private static List<Guid> Guids(int count) => Enumerable.Range(0, count).Select(x => Guid.NewGuid()).ToList();

        [Fact]
        public void TheSelectedElements_AreThoseThatUseTheConstruction_AndTheRestAreCountedAsOther()
        {
            List<Guid> using_ = Guids(12);
            List<Guid> selected = using_.Take(2).Concat(Guids(1)).Concat(new[] { using_[0] }).ToList(); // 2 using, 1 not, 1 duplicate

            ThermalScope scope = new ThermalScope("SIM_EXT_SLD", "panel", "panels", using_, selected);

            Assert.Equal(12, scope.UsingCount);
            Assert.Equal(2, scope.SelectedCount);
            Assert.Equal(1, scope.OtherSelectedCount);
            Assert.Equal(using_.Take(2), scope.SelectedGuids);
            Assert.Equal(ThermalApplyScope.AllUsing, scope.Scope);
        }

        [Fact]
        public void TheLabels_CarryTheCounts()
        {
            List<Guid> using_ = Guids(12);

            ThermalScope some = new ThermalScope("SIM_EXT_SLD", "panel", "panels", using_, using_.Take(3));
            Assert.Equal("All 12 panels using it", some.AllLabel);
            Assert.Equal("Only the 3 selected", some.SelectedLabel);
            Assert.True(some.SelectedAvailable);
            Assert.Null(some.SelectedUnavailableReason);

            ThermalScope one = new ThermalScope("SIM_EXT_SLD", "panel", "panels", using_.Take(1), null);
            Assert.Equal("All 1 panel using it", one.AllLabel);
        }

        [Fact]
        public void OnlyTheSelected_IsUnavailable_WithAReason_WhenNoSelectedElementUsesTheConstruction()
        {
            ThermalScope scope = new ThermalScope("SIM_EXT_SLD", "panel", "panels", Guids(12), Guids(2));

            Assert.False(scope.SelectedAvailable);
            Assert.Equal("Only the selected", scope.SelectedLabel);
            Assert.Equal("No selected panel uses SIM_EXT_SLD.", scope.SelectedUnavailableReason);
            Assert.Equal(2, scope.OtherSelectedCount);
        }

        [Fact]
        public void TheSentence_IsOneBuilder_ForPanelsAndApertures()
        {
            List<Guid> panels = Guids(12);
            List<Guid> apertures = Guids(20);

            ThermalScope scope_Panels = new ThermalScope("SIM_EXT_SLD", "panel", "panels", panels, panels.Take(3));
            ThermalScope scope_Apertures = new ThermalScope("GLZ", "aperture", "apertures", apertures, apertures.Take(2));

            Assert.Equal("Applies to 12 panels using SIM_EXT_SLD (3 selected).", scope_Panels.Text(ThermalApplyScope.AllUsing, "-"));
            Assert.Equal("Applies to 20 apertures using GLZ (2 selected).", scope_Apertures.Text(ThermalApplyScope.AllUsing, "-"));
            Assert.Equal("Applies to 3 selected panels of the 12 using SIM_EXT_SLD.", scope_Panels.Text(ThermalApplyScope.SelectedOnly, "-"));
            Assert.Equal("Applies to 2 selected apertures of the 20 using GLZ.", scope_Apertures.Text(ThermalApplyScope.SelectedOnly, "-"));
            Assert.Equal("the caller's sentence", scope_Panels.Text(ThermalApplyScope.DontAssign, "the caller's sentence"));

            ThermalScope none = new ThermalScope("SIM_EXT_SLD", "panel", "panels", Guids(0), null);
            Assert.Equal("Nobody.", none.Text(ThermalApplyScope.AllUsing, "-", "Nobody."));
            Assert.Equal("Applies to 0 panels using SIM_EXT_SLD.", none.Text(ThermalApplyScope.AllUsing, "-"));
        }

        [Fact]
        public void TheBasis_IsTheSelectedElements_OnlyForTheSelectedScope()
        {
            List<Guid> using_ = Guids(12);
            ThermalScope scope = new ThermalScope("X", "panel", "panels", using_, using_.Take(3));

            Assert.Equal(12, scope.BasisGuids(ThermalApplyScope.AllUsing).Count);
            Assert.Equal(12, scope.BasisGuids(ThermalApplyScope.DontAssign).Count);
            Assert.Equal(3, scope.BasisGuids(ThermalApplyScope.SelectedOnly).Count);

            // With nothing selected "only the selected" has no basis of its own: the elements using the construction stay the basis.
            Assert.Equal(12, new ThermalScope("X", "panel", "panels", using_, null).BasisGuids(ThermalApplyScope.SelectedOnly).Count);
        }

        // ---- Pinned: the scope is fixed when the view-model is created ---------------------------------------------

        [Fact]
        public async Task TheUValueScope_IsPinned_AChangedSelectionAfterwardsDoesNotChangeWhatApplyTouches()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            List<Guid> all = UValueFixture.PanelGuids(analyticalModel, construction);
            List<Guid> selected = all.Take(3).ToList();
            UValueViewModel viewModel = new UValueViewModel(analyticalModel, construction.Guid, selected, new ImmediateUValueEvaluator());
            await viewModel.InitializeAsync();
            viewModel.TargetText = "0.3";
            await viewModel.LastEvaluationTask;
            viewModel.ApplyScope = ThermalApplyScope.SelectedOnly;

            // The caller's selection changes (a click in the 3D view, the list is edited in place): the pinned scope does not.
            selected.Clear();
            selected.AddRange(all.Skip(5).Take(2));

            Assert.Equal(3, viewModel.SelectedPanelsCount);
            SetUValueRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);
            Assert.Equal(all.Take(3), request.SelectedPanelGuids);
            Assert.Equal(ThermalApplyScope.SelectedOnly, request.Scope);
            Assert.Equal("Applies to 3 selected panels of the 12 using SIM_EXT_SLD.", viewModel.ScopeText);
        }

        [Fact]
        public async Task TheGlazingScope_IsPinned_AChangedSelectionAfterwardsDoesNotChangeWhatApplyTouches()
        {
            AnalyticalModel analyticalModel = GlazingFixture.Model();
            List<Guid> all = GlazingFixture.ApertureGuids(analyticalModel, GlazingFixture.CurrentGuid);
            List<Guid> selected = all.Take(2).ToList();
            GlazingViewModel viewModel = GlazingFixture.ViewModel(analyticalModel, selected);
            await viewModel.InitializeAsync();
            viewModel.ApplyScope = ThermalApplyScope.SelectedOnly;
            viewModel.SelectedGuid = GlazingFixture.BetterGuid;

            selected.Clear();
            selected.AddRange(all.Skip(10).Take(5));

            Assert.Equal(2, viewModel.SelectedAperturesCount);
            SetGlazingRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);
            Assert.Equal(all.Take(2), request.SelectedApertureGuids);
            Assert.Equal(ThermalApplyScope.SelectedOnly, request.Scope);
            Assert.Equal("Applies to 2 selected apertures of the 20 using GLZ.", viewModel.ScopeText);
        }

        // ---- Both view-models share it -----------------------------------------------------------------------------

        [Fact]
        public async Task BothViewModels_ExposeTheSameScopeModel_AndTheSameChoice()
        {
            AnalyticalModel panels = UValueFixture.Model(out Construction construction);
            UValueViewModel uValue = new UValueViewModel(panels, construction.Guid, UValueFixture.PanelGuids(panels, construction).Take(3), new ImmediateUValueEvaluator());
            GlazingViewModel glazing = GlazingFixture.ViewModel(GlazingFixture.Model(), GlazingFixture.ApertureGuids(GlazingFixture.Model(), GlazingFixture.CurrentGuid).Take(3));
            await glazing.InitializeAsync();

            Assert.IsType<ThermalScope>(uValue.Scope);
            Assert.IsType<ThermalScope>(glazing.Scope);

            foreach (ThermalApplyScope choice in new[] { ThermalApplyScope.SelectedOnly, ThermalApplyScope.DontAssign, ThermalApplyScope.AllUsing })
            {
                uValue.ApplyScope = choice;
                glazing.ApplyScope = choice;
                Assert.Equal(choice, uValue.Scope.Scope);
                Assert.Equal(choice, glazing.Scope.Scope);
            }
        }

        // ---- Read-only ---------------------------------------------------------------------------------------------

        [Fact]
        public async Task TheViewModels_NeverWriteTheModel_WhateverTheScopeAndOptions()
        {
            AnalyticalModel analyticalModel = UValueFixture.Model(out Construction construction);
            string before = analyticalModel.ToJsonObject().ToJsonString();
            UValueViewModel uValue = new UValueViewModel(analyticalModel, construction.Guid, UValueFixture.PanelGuids(analyticalModel, construction).Take(3), new ImmediateUValueEvaluator());
            await uValue.InitializeAsync();
            uValue.TargetText = "0.3";
            await uValue.LastEvaluationTask;
            uValue.ApplyScope = ThermalApplyScope.SelectedOnly;
            uValue.ApplyScope = ThermalApplyScope.DontAssign;
            uValue.ApplyScope = ThermalApplyScope.AllUsing;
            uValue.KeepName = true;
            uValue.KeepName = false;
            Assert.NotNull(uValue.CreateRequest());
            uValue.Dispose();
            Assert.Equal(before, analyticalModel.ToJsonObject().ToJsonString());

            AnalyticalModel glazingModel = GlazingFixture.Model();
            string before_Glazing = glazingModel.ToJsonObject().ToJsonString();
            GlazingViewModel glazing = GlazingFixture.ViewModel(glazingModel, GlazingFixture.ApertureGuids(glazingModel, GlazingFixture.CurrentGuid).Take(2));
            await glazing.InitializeAsync();
            glazing.SelectedGuid = GlazingFixture.BetterGuid;
            glazing.ApplyScope = ThermalApplyScope.SelectedOnly;
            glazing.ApplyScope = ThermalApplyScope.DontAssign;
            await glazing.AddSourceAsync(GlazingFixture.LoadedGood());
            Assert.NotNull(glazing.CreateRequest());
            glazing.Dispose();
            Assert.Equal(before_Glazing, glazingModel.ToJsonObject().ToJsonString());
        }
    }
}
