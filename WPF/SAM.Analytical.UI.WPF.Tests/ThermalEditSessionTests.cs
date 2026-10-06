// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Thermal Stage C2-C6: editing in the Thermal Performance panel. A row edit (typing a target, choosing a system) builds a
    /// proposal on clones and checks it before Apply; its scope is pinned; an outside model change discards it; Discard changes
    /// nothing; one Apply of any number of rows is one commit (one Undo); an imported glazing source enters the model only on
    /// Apply; a stored value that is missing, varying or out of date says so.
    /// </summary>
    public class ThermalEditSessionTests
    {
        private static ThermalEditServices Services()
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()));
        }

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private sealed class Counters
        {
            public int Modified;
            public int HistoryChanged;
            public int ThermalParameters;
        }

        // What the app does: the panel hands the set to Modify, with Tas replaced by stand-ins.
        private static Func<ThermalChangeSet, ThermalChangeResult> Applier(UIAnalyticalModel ui, Counters counters)
        {
            ui.Modified += (sender, e) => counters.Modified++;
            ui.HistoryChanged += (sender, e) => counters.HistoryChanged++;
            return set => Modify.ApplyThermalChange(ui, set, x => counters.ThermalParameters++, Tas);
        }

        private static List<SAMObject> Select(AnalyticalModel model, IEnumerable<Guid> panels, IEnumerable<Guid> apertures = null)
        {
            List<SAMObject> result = new List<SAMObject>();
            HashSet<Guid> panelGuids = new HashSet<Guid>(panels ?? Enumerable.Empty<Guid>());
            result.AddRange(model.AdjacencyCluster.GetPanels().Where(x => panelGuids.Contains(x.Guid)));
            foreach (Guid guid in apertures ?? Enumerable.Empty<Guid>())
            {
                result.Add(model.AdjacencyCluster.GetAperture(guid));
            }

            return result;
        }

        private static ThermalRowEditor Row(ThermalPerformanceViewModel viewModel, string constructionName)
        {
            return viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == constructionName).Editor;
        }

        private static ThermalRowEditor Type(ThermalRowEditor editor, string target)
        {
            editor.TargetText = target;
            editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            return editor;
        }

        private static ThermalRowEditor Choose(ThermalRowEditor editor, Guid systemGuid)
        {
            editor.OpenChange();
            editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            editor.Glazing.SelectedGuid = systemGuid;
            return editor;
        }

        private static string Json(AnalyticalModel model)
        {
            return model.ToJsonObject().ToJsonString();
        }

        private static AnalyticalModel WithStoredU(AnalyticalModel model, Guid constructionGuid, double? u)
        {
            AdjacencyCluster adjacencyCluster = model.AdjacencyCluster;
            foreach (Panel panel in adjacencyCluster.GetPanels().Where(x => x.TypeGuid == constructionGuid).ToList())
            {
                Panel copy = Analytical.Create.Panel(panel);
                if (u.HasValue)
                {
                    copy.SetValue(PanelParameter.ThermalTransmittance, u.Value);
                }

                adjacencyCluster.AddObject(copy);
            }

            return new AnalyticalModel(model, adjacencyCluster, model.MaterialLibrary, model.ProfileLibrary);
        }

        // ---- One opaque row: target, preview, check ---------------------------------------------------------------

        [Fact]
        public void Typing_a_target_on_a_selected_wall_previews_it_and_builds_one_change_with_no_new_warnings()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Row(viewModel, parts.Wall.Name);

                Assert.False(editor.IsEdited);
                Assert.False(viewModel.Session.IsPending);

                Type(editor, "0.30");

                Assert.True(editor.IsEdited);
                Assert.Equal("✓", editor.PreviewGlyph);
                Assert.Contains("→", editor.PreviewText);
                Assert.Contains("mm", editor.PreviewText);
                Assert.True(editor.HasRequest);
                Assert.Equal(1, viewModel.Session.ChangeCount);
                Assert.Equal(12, viewModel.Session.ElementCount);
                Assert.NotNull(viewModel.Session.Diff);
                Assert.True(viewModel.Session.Diff.Passed);
                Assert.Equal("✓ No new warnings", viewModel.Session.CheckText);
                Assert.True(viewModel.Session.CanApply);
            }
        }

        [Fact]
        public void Choosing_only_the_selected_changes_the_scope_to_the_selected_panels_inline()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");

                Assert.Equal("All 12 panels using it", editor.ScopeAllLabel);
                Assert.Equal("Only the 3 selected", editor.ScopeSelectedLabel);
                Assert.True(editor.ScopeSelectedEnabled);

                editor.ScopeSelected = true;
                editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

                Assert.Equal(3, viewModel.Session.ElementCount);
                Assert.Contains("3 selected panels of the 12", editor.ScopeText);
            }
        }

        [Fact]
        public void Nothing_is_calculated_or_pinned_until_a_row_is_edited()
        {
            ThermalParts parts = ThermalFixture.Build();
            ImmediateUValueEvaluator evaluator = new ImmediateUValueEvaluator();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(new ThermalEditServices(() => evaluator, () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()))))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));

                Assert.Empty(evaluator.Requests);
                Assert.False(viewModel.Session.IsPending);
                Assert.True(viewModel.ModeSwitchEnabled);
                Assert.Null(viewModel.Session.CheckText);
            }
        }

        [Fact]
        public void Clearing_the_target_ends_the_edit_and_unpins_the_panel()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");
                Assert.True(viewModel.Session.IsPending);

                editor.TargetText = string.Empty;

                Assert.False(editor.IsEdited);
                Assert.False(viewModel.Session.IsPending);
                Assert.Equal(0, viewModel.Session.ChangeCount);
            }
        }

        // ---- Pinned scope and invalidation -----------------------------------------------------------------------

        [Fact]
        public void While_a_row_is_edited_a_new_selection_and_a_mode_switch_change_nothing()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");
                IReadOnlyList<ThermalPerformanceGroup> groups = viewModel.Groups;
                string summary = viewModel.Summary;

                // Select other panels and switch the mode: the proposal and its scope stay as they were.
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Skip(5).Take(7)));
                viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;

                Assert.Same(groups, viewModel.Groups);
                Assert.Equal(summary, viewModel.Summary);
                Assert.Equal(ThermalPerformanceMode.Selection, viewModel.Mode);
                Assert.False(viewModel.ModeSwitchEnabled);
                Assert.Same(editor, Row(viewModel, parts.Wall.Name));
                Assert.Equal(3, editor.Scope.SelectedCount);
                Assert.True(viewModel.Session.CanApply);

                // The change set Apply would use still reaches exactly what the edit pinned.
                SetUValueRequest request = viewModel.Session.BuildChangeSet().UValueRequests.Single();
                Assert.Equal(parts.WallPanels.Take(3).OrderBy(x => x), request.SelectedPanelGuids.OrderBy(x => x));
            }
        }

        [Fact]
        public void A_model_change_from_outside_discards_the_pending_edit_and_says_why()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                Type(Row(viewModel, parts.Wall.Name), "0.30");
                Assert.True(viewModel.Session.IsPending);

                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)), modelChanged: true);

                Assert.False(viewModel.Session.IsPending);
                Assert.Equal(0, viewModel.Session.ChangeCount);
                Assert.Null(viewModel.Session.Diff);
                Assert.Contains("discarded", viewModel.Session.Notice);
                Assert.True(viewModel.ModeSwitchEnabled);
                Assert.False(Row(viewModel, parts.Wall.Name).IsEdited);
            }
        }

        [Fact]
        public void A_model_change_while_nothing_is_edited_just_refreshes_the_rows_without_a_notice()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));

                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(5)), modelChanged: true);

                Assert.Null(viewModel.Session.Notice);
                Assert.Equal(5, viewModel.Groups.SelectMany(x => x.Rows).Sum(x => x.SelectedCount) - 0);
            }
        }

        // ---- Cancel / Discard -------------------------------------------------------------------------------------

        [Fact]
        public void Discard_leaves_the_model_and_its_history_untouched()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            Counters counters = new Counters();
            Applier(ui, counters);
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts.WallPanels.Take(3), parts.Windows.Take(2)));
                Type(Row(viewModel, parts.Wall.Name), "0.30");
                Choose(Row(viewModel, GlazingFixture.CurrentName), GlazingFixture.BetterGuid);
                Assert.Equal(2, viewModel.Session.ChangeCount);

                viewModel.Discard();

                Assert.False(viewModel.Session.IsPending);
                Assert.Equal(0, viewModel.Session.ChangeCount);
                Assert.Equal(0, counters.Modified);
                Assert.Equal(0, counters.HistoryChanged);
                Assert.False(ui.CanUndo);
                Assert.Equal(before, Json(ui.JSAMObject));
            }
        }

        // ---- Apply: one commit, one Undo --------------------------------------------------------------------------

        [Fact]
        public void Apply_of_one_wall_row_commits_once_ends_the_edit_and_shows_the_new_construction()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            Counters counters = new Counters();
            Func<ThermalChangeSet, ThermalChangeResult> applier = Applier(ui, counters);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                // What the host does when the model is replaced: hands the panel the new model and the selection.
                List<Guid> selected = parts.WallPanels.Take(3).ToList();
                ui.Modified += (sender, e) => viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, selected), modelChanged: true);
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, selected));
                Type(Row(viewModel, parts.Wall.Name), "0.30");

                ThermalChangeResult result = viewModel.Apply(applier);

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, counters.Modified);
                Assert.Equal(1, counters.HistoryChanged);
                Assert.Equal(1, counters.ThermalParameters);
                Assert.True(ui.CanUndo);
                Assert.False(viewModel.Session.IsPending);
                Assert.Same(result, viewModel.Session.LastResult);
                Assert.EndsWith("One Undo reverts it.", viewModel.Session.LastResult.Text);

                // The rows were rebuilt from the new model: the walls now use the new construction.
                string newName = result.UValueResults[0].Construction.Name;
                Assert.Contains(viewModel.Groups.SelectMany(x => x.Rows), x => x.ConstructionName == newName);
            }
        }

        [Fact]
        public void A_wall_row_and_a_window_row_apply_together_as_one_commit()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            Counters counters = new Counters();
            Func<ThermalChangeSet, ThermalChangeResult> applier = Applier(ui, counters);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts.WallPanels.Take(3), parts.Windows.Take(2)));
                Type(Row(viewModel, parts.Wall.Name), "0.30");
                Choose(Row(viewModel, GlazingFixture.CurrentName), GlazingFixture.BetterGuid);

                Assert.Equal(2, viewModel.Session.ChangeCount);
                Assert.Equal(12 + 6, viewModel.Session.ElementCount);
                Assert.NotNull(viewModel.Session.Diff);

                ThermalChangeResult result = viewModel.Apply(applier);

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, counters.Modified);
                Assert.Equal(1, counters.HistoryChanged);
                Assert.Equal(1, counters.ThermalParameters);
                Assert.Equal(12, result.PanelCount);
                Assert.Equal(6, result.ApertureCount);
                Assert.All(ui.JSAMObject.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
            }
        }

        [Fact]
        public void A_failed_apply_keeps_what_was_typed_and_says_why()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");

                ThermalChangeResult result = viewModel.Apply(set => new ThermalChangeResult("Boom"));

                Assert.False(result.Succeeded);
                Assert.Equal("Boom", viewModel.Session.Notice);
                Assert.True(viewModel.Session.IsPending);
                Assert.Equal("0.30", editor.TargetText);
                Assert.Null(viewModel.Session.LastResult);
            }
        }

        // ---- Imported glazing sources enter the model only on Apply -------------------------------------------

        [Fact]
        public void A_system_from_a_library_enters_the_model_only_when_applied_and_not_when_discarded()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            Counters counters = new Counters();
            Func<ThermalChangeSet, ThermalChangeResult> applier = Applier(ui, counters);
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts.WallPanels.Take(1), parts.Windows.Take(2)));

                // Choosing a library system (not in the model) proposes it; the model has neither it nor its materials.
                Choose(Row(viewModel, GlazingFixture.CurrentName), GlazingFixture.BetterGuid);
                Assert.True(viewModel.Session.CanApply);
                Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == GlazingFixture.BetterGuid);
                Assert.Equal(before, Json(ui.JSAMObject));

                viewModel.Discard();
                Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == GlazingFixture.BetterGuid);
                Assert.Equal(before, Json(ui.JSAMObject));
                Assert.False(ui.CanUndo);

                // The same choice, applied: now it is in the model, as one commit.
                Choose(Row(viewModel, GlazingFixture.CurrentName), GlazingFixture.BetterGuid);
                Assert.True(viewModel.Apply(applier).Succeeded);
                Assert.Contains(ui.JSAMObject.AdjacencyCluster.GetApertureConstructions(), x => x.Guid == GlazingFixture.BetterGuid);
                Assert.Equal(1, counters.Modified);
            }
        }

        [Fact]
        public void A_candidate_warning_is_kept_on_the_row_and_the_check_before_apply_reports_it_as_new()
        {
            ThermalParts parts = ThermalFixture.Build();
            Guid roofGuid = new Guid("a0000000-0000-4000-8000-0000000000a1");
            ThermalEditServices services = new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(GlazingFixture.RoofSystem(roofGuid)), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()));

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(services))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(1), parts.Windows.Take(2)));
                ThermalRowEditor editor = Choose(Row(viewModel, GlazingFixture.CurrentName), roofGuid);

                Assert.Contains(editor.Candidates, x => x.Guid == roofGuid && x.HasWarnings);
                Assert.Contains(editor.Warnings, x => x.Contains("roof", StringComparison.OrdinalIgnoreCase) || x.Contains("panel", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(viewModel.Session.Diff);
                Assert.False(viewModel.Session.Diff.Passed);
                Assert.StartsWith("⚠", viewModel.Session.CheckText);
            }
        }

        // ---- Provenance of the stored value ----------------------------------------------------------------------

        [Fact]
        public void A_value_the_model_does_not_store_is_called_not_calculated_and_offers_recalculation()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Row(viewModel, parts.Wall.Name);

                Assert.Equal(ThermalStoredState.NotCalculated, editor.Row.StoredState);
                Assert.True(editor.NeedsRecalculation);
                Assert.StartsWith("Not calculated", editor.ProvenanceText);
            }
        }

        [Fact]
        public void A_stored_value_that_disagrees_with_the_calculation_is_flagged_out_of_date()
        {
            ThermalParts parts = ThermalFixture.Build();
            AnalyticalModel model = WithStoredU(parts.Model, parts.Wall.Guid, 0.40); // the construction calculates to 0.260
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(model, Select(model, parts.WallPanels.Take(3)));
                ThermalRowEditor editor = Row(viewModel, parts.Wall.Name);

                Assert.Equal(ThermalStoredState.Stored, editor.Row.StoredState);
                Assert.Equal(0.40, editor.Row.StoredThermalTransmittance, 3);
                Assert.Equal("Stored on the model.", editor.ProvenanceText);
                Assert.False(editor.StoredIsOutOfDate);

                Type(editor, "0.30");

                Assert.True(editor.StoredIsOutOfDate);
                Assert.Contains("out of date", editor.ProvenanceText);
            }
        }

        [Fact]
        public void Recalculating_the_stored_values_is_one_commit_and_needs_no_edit()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            Counters counters = new Counters();
            Func<ThermalChangeSet, ThermalChangeResult> applier = Applier(ui, counters);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts.WallPanels.Take(3)));

                ThermalChangeResult result = viewModel.Recalculate(applier);

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, counters.Modified);
                Assert.Equal(1, counters.HistoryChanged);
                Assert.Equal(1, counters.ThermalParameters);
                Assert.Contains("recalculated", viewModel.Session.LastResult.Text);
            }
        }

        // ---- Whole envelope ------------------------------------------------------------------------------------------

        [Fact]
        public void In_the_whole_envelope_a_row_edit_reaches_every_element_using_the_construction()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, null);
                viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");

                Assert.False(editor.ScopeSelectedEnabled);
                Assert.Equal(12, viewModel.Session.ElementCount);
                Assert.True(viewModel.Session.CanApply);
            }
        }
    }
}
