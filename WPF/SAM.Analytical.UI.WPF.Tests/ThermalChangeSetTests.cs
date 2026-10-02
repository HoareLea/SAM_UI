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
    /// Thermal Stage C1 / C5: <see cref="ThermalChangeSet"/>. Opaque and glazing changes are composed from the existing
    /// <c>SetUValue</c> / <c>SetGlazing</c> cores on one chain of clones, the thermal-parameter refresh runs once, and
    /// <c>SetJSAMObject</c> is called exactly once - so one Apply is one Undo - or not at all. The proposed model that the check
    /// before Apply reads is built without Tas and without touching the current model, and the check reports only what the
    /// change would add.
    /// </summary>
    public class ThermalChangeSetTests
    {
        private static ThermalParts Build() => ThermalFixture.Build();

        private static SetUValueRequest Opaque(Construction construction, ThermalApplyScope scope = ThermalApplyScope.AllUsing, IEnumerable<Guid> selected = null) => ThermalFixture.Opaque(construction, scope, selected);

        private static async Task<SetGlazingRequest> Glazing(AnalyticalModel model, Guid systemGuid, ThermalApplyScope scope = ThermalApplyScope.AllUsing, IEnumerable<Guid> selected = null)
        {
            GlazingViewModel viewModel = GlazingFixture.ViewModel(model, selected);
            await viewModel.InitializeAsync();
            viewModel.ApplyScope = scope;
            viewModel.SelectedGuid = systemGuid;
            SetGlazingRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);
            return request;
        }

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static string Json(AnalyticalModel model)
        {
            return model.ToJsonObject().ToJsonString();
        }

        private sealed class Counters
        {
            public int Modified;
            public int HistoryChanged;
            public int ThermalParameters;
            public int Calculations;
        }

        private static ThermalChangeResult Apply(UIAnalyticalModel uIAnalyticalModel, ThermalChangeSet set, Counters counters)
        {
            uIAnalyticalModel.Modified += (sender, e) => counters.Modified++;
            uIAnalyticalModel.HistoryChanged += (sender, e) => counters.HistoryChanged++;

            return Modify.ApplyThermalChange(uIAnalyticalModel, set, x => counters.ThermalParameters++, x => { counters.Calculations++; return Tas(x); });
        }

        // ---- One change --------------------------------------------------------------------------------------

        [Fact]
        public void One_opaque_change_gives_every_panel_the_new_construction_in_one_commit()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(Opaque(fixture.Wall)), counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(12, result.PanelCount);
            Assert.Equal(0, result.ApertureCount);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.HistoryChanged);
            Assert.Equal(1, counters.ThermalParameters);
            Assert.Equal(0, counters.Calculations);
            Assert.True(ui.CanUndo);

            AnalyticalModel current = ui.JSAMObject;
            Assert.Equal(12, current.AdjacencyCluster.GetPanels(result.UValueResults[0].Construction).Count);
            Assert.NotNull(current.MaterialLibrary.GetMaterial(result.UValueResults[0].MaterialName));
        }

        [Fact]
        public async Task One_glazing_change_gives_every_aperture_the_chosen_system_in_one_commit_without_the_whole_model_refresh()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();
            SetGlazingRequest request = await Glazing(fixture.Model, GlazingFixture.BetterGuid);

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(request), counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(6, result.ApertureCount);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.HistoryChanged);
            Assert.Equal(0, counters.ThermalParameters);
            Assert.Equal(1, counters.Calculations);
            Assert.All(ui.JSAMObject.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
        }

        // ---- Several changes, one Apply ---------------------------------------------------------------------------

        [Fact]
        public async Task A_wall_and_a_window_change_together_in_one_commit_and_the_refresh_runs_once()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();
            ThermalChangeSet set = new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(await Glazing(fixture.Model, GlazingFixture.BetterGuid));

            ThermalChangeResult result = Apply(ui, set, counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(12, result.PanelCount);
            Assert.Equal(6, result.ApertureCount);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.HistoryChanged);
            Assert.Equal(1, counters.ThermalParameters);
            Assert.Equal(1, counters.Calculations);

            AnalyticalModel current = ui.JSAMObject;
            Assert.Equal(12, current.AdjacencyCluster.GetPanels(result.UValueResults[0].Construction).Count);
            Assert.All(current.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
            Assert.Contains("12 panels now", result.Text);
            Assert.Contains("6 apertures now", result.Text);
            Assert.EndsWith("One Undo reverts it.", result.Text);
        }

        [Fact]
        public void Two_walls_on_different_constructions_change_in_one_commit()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(Opaque(fixture.OtherWall)), counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(2, result.UValueResults.Count);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.ThermalParameters);
            Assert.NotEqual(result.UValueResults[0].Construction.Guid, result.UValueResults[1].Construction.Guid);
        }

        [Fact]
        public void Two_changes_to_the_same_construction_are_refused_and_nothing_is_committed()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();
            string before = Json(ui.JSAMObject);

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(Opaque(fixture.Wall)), counters);

            Assert.False(result.Succeeded);
            Assert.Equal(0, counters.Modified);
            Assert.Equal(0, counters.ThermalParameters);
            Assert.False(ui.CanUndo);
            Assert.Equal(before, Json(ui.JSAMObject));
        }

        [Fact]
        public async Task A_failing_change_commits_nothing_not_even_the_ones_before_it()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();
            string before = Json(ui.JSAMObject);

            // The glazing change is for "only the selected", but none of the selected apertures uses the system.
            SetGlazingRequest glazing = await Glazing(fixture.Model, GlazingFixture.BetterGuid, ThermalApplyScope.SelectedOnly, fixture.Windows.Take(2));
            glazing.SelectedApertureGuids = new List<Guid>() { Guid.NewGuid() };

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(glazing), counters);

            Assert.False(result.Succeeded);
            Assert.Equal(0, counters.Modified);
            Assert.Equal(0, counters.HistoryChanged);
            Assert.Equal(0, counters.ThermalParameters);
            Assert.False(ui.CanUndo);
            Assert.Equal(before, Json(ui.JSAMObject));
        }

        [Fact]
        public void An_empty_set_is_refused_and_commits_nothing()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet(), counters);

            Assert.False(result.Succeeded);
            Assert.Equal(0, counters.Modified);
            Assert.False(ui.CanUndo);
        }

        [Fact]
        public void Recalculating_the_stored_values_alone_is_one_commit_that_changes_no_element()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet() { RecalculateStoredValues = true }, counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.True(result.Recalculated);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.HistoryChanged);
            Assert.Equal(1, counters.ThermalParameters);
            Assert.Equal(0, result.PanelCount);
            Assert.Contains("recalculated", result.Text);
        }

        // ---- Scope is explicit -------------------------------------------------------------------------------------

        [Fact]
        public void Only_the_pinned_panels_change_whatever_is_selected_later()
        {
            ThermalParts fixture = Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(fixture.Model);
            Counters counters = new Counters();
            List<Guid> pinned = fixture.WallPanels.Take(3).ToList();

            ThermalChangeResult result = Apply(ui, new ThermalChangeSet().Add(Opaque(fixture.Wall, ThermalApplyScope.SelectedOnly, pinned)), counters);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(3, result.PanelCount);
            Assert.True(pinned.All(x => result.UValueResults[0].PanelGuids.Contains(x)));
            AnalyticalModel current = ui.JSAMObject;
            Assert.Equal(9, current.AdjacencyCluster.GetPanels(fixture.Wall).Count);
        }

        // ---- The proposed model, for the check before Apply ------------------------------------------------------

        [Fact]
        public async Task The_proposed_model_is_built_without_touching_the_current_one_and_without_tas()
        {
            ThermalParts fixture = Build();
            string before = Json(fixture.Model);
            ThermalChangeSet set = new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(await Glazing(fixture.Model, GlazingFixture.BetterGuid));

            AnalyticalModel proposed = Modify.ProposeThermalChange(fixture.Model, set, null, out ThermalChangeResult result);

            Assert.NotNull(proposed);
            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(before, Json(fixture.Model));
            Assert.Equal(12, proposed.AdjacencyCluster.GetPanels(result.UValueResults[0].Construction).Count);
            Assert.All(proposed.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
        }

        [Fact]
        public void A_change_that_adds_no_problem_has_no_new_warnings()
        {
            ThermalParts fixture = Build();
            ThermalChangeSet set = new ThermalChangeSet().Add(Opaque(fixture.Wall));
            string before = Json(fixture.Model);

            AnalyticalModel proposed = Modify.ProposeThermalChange(fixture.Model, set, null, out ThermalChangeResult result);
            ThermalCheckDiff diff = Query.ThermalCheckDiff(fixture.Model, proposed, result);

            Assert.True(diff.Passed, string.Join(Environment.NewLine, diff.Log.ToList().Select(x => x.Text)));
            Assert.Equal("No new warnings", diff.Text);
            Assert.Equal("✓", diff.Glyph);
            Assert.Equal(before, Json(fixture.Model));
        }

        [Fact]
        public async Task A_system_made_for_roofs_on_wall_windows_is_reported_as_new_before_apply()
        {
            ThermalParts fixture = Build();
            Guid roofGuid = new Guid("a0000000-0000-4000-8000-0000000000a1");
            GlazingViewModel viewModel = new GlazingViewModel(fixture.Model, GlazingFixture.CurrentGuid, null, new FakeGlazingEvaluator(), GlazingFixture.Library(GlazingFixture.RoofSystem(roofGuid)));
            await viewModel.InitializeAsync();
            viewModel.SelectedGuid = roofGuid;
            SetGlazingRequest request = viewModel.CreateRequest();
            Assert.NotNull(request);

            AnalyticalModel proposed = Modify.ProposeThermalChange(fixture.Model, new ThermalChangeSet().Add(request), null, out ThermalChangeResult result);
            ThermalCheckDiff diff = Query.ThermalCheckDiff(fixture.Model, proposed, result);

            Assert.False(diff.Passed);
            Assert.True(diff.NewWarnings > 0, diff.Text);
            Assert.Contains("new warning", diff.Text);
            Assert.Equal("⚠", diff.Glyph);
            Assert.NotEmpty(diff.Log.ToList());
        }

        [Fact]
        public async Task The_check_does_not_change_either_model()
        {
            ThermalParts fixture = Build();
            ThermalChangeSet set = new ThermalChangeSet().Add(Opaque(fixture.Wall)).Add(await Glazing(fixture.Model, GlazingFixture.BetterGuid));
            AnalyticalModel proposed = Modify.ProposeThermalChange(fixture.Model, set, null, out ThermalChangeResult result);
            string before_Current = Json(fixture.Model);
            string before_Proposed = Json(proposed);

            Query.ThermalCheckDiff(fixture.Model, proposed, result);

            Assert.Equal(before_Current, Json(fixture.Model));
            Assert.Equal(before_Proposed, Json(proposed));
        }
    }
}
