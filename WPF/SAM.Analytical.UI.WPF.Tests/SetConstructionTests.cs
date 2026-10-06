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
    /// Thermal Stage D1: assigning an EXISTING construction to the panels of a row. The model-only core builds the changed model on a clone
    /// (the input is untouched); only the chosen construction and the materials the model lacks enter; the scope is explicit; the source stays
    /// as a stored object when its last panel moves away; the change composes with the other thermal changes into ONE commit and ONE Undo.
    /// </summary>
    public class SetConstructionTests
    {
        private sealed class Counters
        {
            public int Modified;
            public int HistoryChanged;
            public int ThermalParameters;
        }

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private static SetConstructionRequest Request(Construction source, Construction chosen, IEnumerable<IMaterial> materials = null, ThermalApplyScope scope = ThermalApplyScope.AllUsing, IEnumerable<Guid> selected = null)
        {
            return new SetConstructionRequest()
            {
                SourceConstructionGuid = source.Guid,
                Construction = chosen,
                MaterialsToAdd = materials ?? new List<IMaterial>(),
                Scope = scope,
                SelectedPanelGuids = selected,
                SourceLabel = "Test",
            };
        }

        private static string Json(AnalyticalModel model)
        {
            return model.ToJsonObject().ToJsonString();
        }

        private static List<Panel> PanelsOn(AnalyticalModel model, Guid constructionGuid)
        {
            return model.AdjacencyCluster.GetPanels().Where(x => x.TypeGuid == constructionGuid).ToList();
        }

        // ---- The core ----------------------------------------------------------------------------------------------

        [Fact]
        public void A_construction_already_in_the_model_is_assigned_to_every_panel_using_the_current_one_and_nothing_else_enters_the_model()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);
            string before = Json(model);

            AnalyticalModel changed = Modify.SetConstruction(model, Request(current, thick), out SetConstructionResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.NotNull(changed);
            Assert.Equal(before, Json(model));
            Assert.False(result.ConstructionAdded);
            Assert.Empty(result.MaterialNamesAdded);
            Assert.Equal(6, result.PanelCount);
            Assert.Equal(thick.Guid, result.Construction.Guid);
            Assert.Equal(7, PanelsOn(changed, thick.Guid).Count);               // its own panel + the 6
            Assert.Empty(PanelsOn(changed, current.Guid));
            Assert.Equal(changed.MaterialLibrary.GetMaterials().Count, model.MaterialLibrary.GetMaterials().Count);

            // The source stays in the model as a stored object: no silent delete.
            Assert.Contains(changed.AdjacencyCluster.GetConstructions(), x => x.Guid == current.Guid);
            // And the chosen construction is not duplicated.
            Assert.Single(changed.AdjacencyCluster.GetConstructions().Where(x => x.Guid == thick.Guid).Select(x => x.Name).Distinct());
        }

        [Fact]
        public void A_library_construction_enters_the_model_under_its_own_guid_with_only_the_materials_the_model_lacks()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);
            GlazingSource library = AlternativesFixture.Library();
            Construction aerogel = library.GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
            IMaterial material = library.GetMaterials()[AlternativesFixture.Aerogel];

            AnalyticalModel changed = Modify.SetConstruction(model, Request(current, aerogel, new[] { material }), out SetConstructionResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.True(result.ConstructionAdded);
            Assert.Equal(new[] { AlternativesFixture.Aerogel }, result.MaterialNamesAdded.ToArray());
            Assert.NotNull(changed.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
            Assert.Equal(AlternativesFixture.LibraryAerogelGuid, result.Construction.Guid);
            Assert.Equal("LIB_AEROGEL", result.Construction.Name);
            Assert.Equal(6, PanelsOn(changed, AlternativesFixture.LibraryAerogelGuid).Count);

            // The materials the model already had were not added again.
            Assert.Equal(model.MaterialLibrary.GetMaterials().Count + 1, changed.MaterialLibrary.GetMaterials().Count);
            Assert.Null(model.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
        }

        [Fact]
        public void A_library_construction_that_shares_a_name_with_a_model_construction_is_added_under_a_numbered_name()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);
            Construction same_Name = AlternativesFixture.Library().GetConstructions().Single(x => x.Guid == AlternativesFixture.LibrarySameNameGuid);
            Assert.Equal(thick.Name, same_Name.Name);

            AnalyticalModel changed = Modify.SetConstruction(model, Request(current, same_Name), out SetConstructionResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal("MODEL_THICK 2", result.Construction.Name);
            Assert.Equal(AlternativesFixture.LibrarySameNameGuid, result.Construction.Guid);
            // The model's own MODEL_THICK is untouched and still on its panel.
            Assert.Single(PanelsOn(changed, thick.Guid));
            Assert.Equal(6, PanelsOn(changed, AlternativesFixture.LibrarySameNameGuid).Count);
        }

        [Fact]
        public void Only_the_selected_panels_change_when_the_scope_is_the_selection()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);
            List<Guid> panels = model.AdjacencyCluster.GetPanels(current).Select(x => x.Guid).ToList();

            AnalyticalModel changed = Modify.SetConstruction(model, Request(current, thick, scope: ThermalApplyScope.SelectedOnly, selected: panels.Take(2)), out SetConstructionResult result);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(2, result.PanelCount);
            Assert.Equal(4, PanelsOn(changed, current.Guid).Count);
            Assert.Equal(3, PanelsOn(changed, thick.Guid).Count);
            Assert.All(panels.Take(2), x => Assert.Equal(thick.Guid, changed.AdjacencyCluster.GetObject<Panel>(x).TypeGuid));
            Assert.All(panels.Skip(2), x => Assert.Equal(current.Guid, changed.AdjacencyCluster.GetObject<Panel>(x).TypeGuid));
        }

        [Fact]
        public void A_request_that_cannot_be_applied_is_refused_with_a_reason_and_changes_nothing()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out Construction medium, out _);
            string before = Json(model);
            List<Guid> panels = model.AdjacencyCluster.GetPanels(current).Select(x => x.Guid).ToList();

            // Only a construction the model has, a different one, an explicit scope, and the selection must use the construction.
            Assert.Null(Modify.SetConstruction(model, Request(current, current), out SetConstructionResult same));
            Assert.Contains("already", same.Error);

            Assert.Null(Modify.SetConstruction(model, Request(AlternativesFixture.Wall("GONE", 0.08), thick), out SetConstructionResult gone));
            Assert.Contains("no longer in the model", gone.Error);

            Assert.Null(Modify.SetConstruction(model, Request(current, thick, scope: ThermalApplyScope.DontAssign), out SetConstructionResult dontAssign));
            Assert.Contains("always assigned", dontAssign.Error);

            Assert.Null(Modify.SetConstruction(model, Request(current, thick, scope: ThermalApplyScope.SelectedOnly, selected: new[] { Guid.NewGuid() }), out SetConstructionResult none));
            Assert.Contains("None of the selected panels", none.Error);

            // A material the model lacks that the request did not bring.
            Construction aerogel = AlternativesFixture.Library().GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
            Assert.Null(Modify.SetConstruction(model, Request(current, aerogel), out SetConstructionResult material));
            Assert.Contains("Material Aerogel", material.Error);

            Assert.Null(Modify.SetConstruction(null, Request(current, thick), out SetConstructionResult nothing));
            Assert.False(nothing.Succeeded);

            Assert.Equal(before, Json(model));
            Assert.Equal(6, model.AdjacencyCluster.GetPanels(current).Count);
            Assert.NotNull(medium);
            Assert.Equal(6, panels.Count);
        }

        [Fact]
        public void The_UI_model_change_is_one_commit_one_undo_and_one_undo_restores_the_model()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);
            UIAnalyticalModel ui = new UIAnalyticalModel(model);
            Counters counters = new Counters();
            ui.Modified += (sender, e) => counters.Modified++;
            ui.HistoryChanged += (sender, e) => counters.HistoryChanged++;

            GlazingSource library = AlternativesFixture.Library();
            Construction aerogel = library.GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
            int refreshes = 0;

            SetConstructionResult result = Modify.SetConstruction(ui, Request(current, aerogel, new[] { library.GetMaterials()[AlternativesFixture.Aerogel] }), x => refreshes++);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal(1, counters.Modified);
            Assert.Equal(1, counters.HistoryChanged);
            Assert.Equal(1, refreshes);
            Assert.True(ui.CanUndo);
            Assert.NotNull(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));

            // One Undo restores the model (the restore runs off the calling thread, so wait for it).
            Assert.True(ui.Undo());
            for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetPanels(current).Count != 6; i++)
            {
                System.Threading.Thread.Sleep(50);
            }

            Assert.Equal(6, ui.JSAMObject.AdjacencyCluster.GetPanels(current).Count);
            Assert.Empty(PanelsOn(ui.JSAMObject, AlternativesFixture.LibraryAerogelGuid));
            Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
            Assert.DoesNotContain(ui.JSAMObject.AdjacencyCluster.GetConstructions(), x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
            Assert.False(ui.CanUndo);
        }

        [Fact]
        public void A_failed_UI_change_commits_nothing()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);
            UIAnalyticalModel ui = new UIAnalyticalModel(model);
            Counters counters = new Counters();
            ui.Modified += (sender, e) => counters.Modified++;

            SetConstructionResult result = Modify.SetConstruction(ui, Request(current, current), x => counters.ThermalParameters++);

            Assert.False(result.Succeeded);
            Assert.Equal(0, counters.Modified);
            Assert.Equal(0, counters.ThermalParameters);
            Assert.False(ui.CanUndo);
        }

        // ---- The change set ---------------------------------------------------------------------------------------

        [Fact]
        public void The_change_set_refuses_two_changes_to_one_construction_and_counts_the_new_kind()
        {
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out Construction thick, out _, out _);

            ThermalChangeSet set = new ThermalChangeSet().Add(Request(current, thick));
            Assert.Equal(1, set.Count);
            Assert.False(set.IsEmpty);
            Assert.Single(set.ConstructionRequests);

            // The generated variant and an existing construction for the SAME current construction would overwrite each other.
            set.Add(new SetUValueRequest() { ConstructionGuid = current.Guid, LayerIndex = 2, Thickness = 0.12 });
            Assert.Null(model.ProposeThermalChange(set, null, out ThermalChangeResult result));
            Assert.Equal("Two opaque changes are for the same construction.", result.Error);

            ThermalChangeSet twice = new ThermalChangeSet().Add(Request(current, thick)).Add(Request(current, thick));
            Assert.Null(model.ProposeThermalChange(twice, null, out ThermalChangeResult result_Twice));
            Assert.Equal("Two opaque changes are for the same construction.", result_Twice.Error);
        }

        [Fact]
        public void An_existing_construction_and_a_glazing_change_apply_together_as_one_commit_and_one_undo()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            string before = Json(ui.JSAMObject);
            Counters counters = new Counters();
            ui.Modified += (sender, e) => counters.Modified++;
            ui.HistoryChanged += (sender, e) => counters.HistoryChanged++;

            GlazingSource library = AlternativesFixture.Library();
            Construction chosen = library.GetConstructions().Single(x => x.Guid == AlternativesFixture.LibraryThickGuid);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(AlternativesFixture.Services()))
            {
                ui.Modified += (sender, e) => viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts), modelChanged: true);
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts));

                ThermalRowEditor wall = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.Wall.Name).Editor;
                wall.TargetText = "0.18";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                wall.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));

                // Typing a target offers the list and chooses nothing; the generated variant is still the row's change.
                Assert.True(wall.HasAlternatives);
                Assert.False(wall.AlternativeChosen);
                Assert.True(wall.AlternativeRows[0].IsGenerated);
                Assert.Equal(1, viewModel.Session.ChangeCount);

                wall.SelectedAlternative = wall.AlternativeRows.Single(x => x.Guid == AlternativesFixture.LibraryThickGuid);
                Assert.True(wall.AlternativeChosen);
                Assert.True(wall.HasRequest);
                Assert.Equal("✓", wall.PreviewGlyph);
                Assert.Contains("LIB_THICK (Library)", wall.PreviewText);
                Assert.Equal(1, viewModel.Session.ChangeCount);
                Assert.Equal(12, viewModel.Session.ElementCount);
                Assert.NotNull(viewModel.Session.Diff);

                ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == GlazingFixture.CurrentName).Editor;
                window.OpenChange();
                window.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                window.Glazing.SelectedGuid = GlazingFixture.BetterGuid;
                Assert.Equal(2, viewModel.Session.ChangeCount);

                ThermalChangeResult result = viewModel.Apply(set => Modify.ApplyThermalChange(ui, set, x => counters.ThermalParameters++, Tas));

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(1, counters.Modified);
                Assert.Equal(1, counters.HistoryChanged);
                Assert.Equal(1, counters.ThermalParameters);
                Assert.Single(result.ConstructionResults);
                Assert.Empty(result.UValueResults);
                Assert.Single(result.GlazingResults);
                Assert.Equal(12, result.ConstructionResults[0].PanelCount);
                Assert.Equal(12, ui.JSAMObject.AdjacencyCluster.GetPanels().Count(x => x.TypeGuid == AlternativesFixture.LibraryThickGuid));
                Assert.All(ui.JSAMObject.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.BetterGuid, x.TypeGuid));
                Assert.Contains("12 panels now LIB_THICK", result.Text);
                Assert.EndsWith("One Undo reverts it.", result.Text);

                // One Undo restores the whole change: the walls and the windows (the restore runs off the calling thread, so wait for it).
                Assert.True(ui.Undo());
                for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetPanels(parts.Wall).Count != 12; i++)
                {
                    System.Threading.Thread.Sleep(50);
                }

                Assert.Equal(12, ui.JSAMObject.AdjacencyCluster.GetPanels(parts.Wall).Count);
                Assert.Empty(PanelsOn(ui.JSAMObject, AlternativesFixture.LibraryThickGuid));
                Assert.All(ui.JSAMObject.AdjacencyCluster.GetApertures(), x => Assert.Equal(GlazingFixture.CurrentGuid, x.TypeGuid));
                Assert.False(ui.CanUndo);
            }

            Assert.NotNull(chosen);
        }

        [Fact]
        public void Discarding_after_choosing_an_existing_construction_leaves_the_model_untouched_and_ends_the_list()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(AlternativesFixture.Services()))
            {
                viewModel.Update(ui.JSAMObject, Select(ui.JSAMObject, parts));
                ThermalRowEditor wall = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.Wall.Name).Editor;
                wall.TargetText = "0.18";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                wall.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
                wall.SelectedAlternative = wall.AlternativeRows.Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
                Assert.True(wall.HasRequest);

                viewModel.Discard();

                Assert.False(viewModel.Session.IsPending);
                Assert.Null(wall.Alternatives);
                Assert.False(wall.HasAlternatives);
                Assert.Empty(wall.AlternativeRows);
                Assert.Equal(before, Json(ui.JSAMObject));
                Assert.False(ui.CanUndo);
                Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(AlternativesFixture.Aerogel));
            }
        }

        [Fact]
        public void A_row_that_is_only_looked_at_or_has_no_valid_target_asks_the_evaluator_nothing()
        {
            ThermalParts parts = ThermalFixture.Build();
            FakeConstructionUValueEvaluator evaluator = new FakeConstructionUValueEvaluator();

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(AlternativesFixture.Services(evaluator)))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts));
                ThermalRowEditor wall = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.Wall.Name).Editor;

                Assert.Null(wall.Alternatives);
                Assert.Equal(0, evaluator.Requests.Count);

                wall.TargetText = "abc";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Assert.Null(wall.Alternatives);
                Assert.Equal(0, evaluator.Requests.Count);

                wall.TargetText = "0.18";
                wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                wall.Alternatives.Idle().Wait(TimeSpan.FromSeconds(10));
                Assert.True(evaluator.Requests.Count > 0);
            }
        }

        private static List<SAMObject> Select(AnalyticalModel model, ThermalParts parts)
        {
            HashSet<Guid> panelGuids = new HashSet<Guid>(parts.WallPanels.Take(3));
            List<SAMObject> result = new List<SAMObject>();
            result.AddRange(model.AdjacencyCluster.GetPanels().Where(x => panelGuids.Contains(x.Guid)));
            result.AddRange(parts.Windows.Take(2).Select(x => (SAMObject)model.AdjacencyCluster.GetAperture(x)));
            return result;
        }
    }
}
