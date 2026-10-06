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
    /// The two controls of the Thermal Performance panel are independent: Selection / Whole envelope decides which cards are
    /// SHOWN; "All N using it / Only the M selected" decides which elements Apply will CHANGE. A panel selected in the 3D view
    /// must therefore be available as "Only the M selected" in the whole-envelope mode too (it used to be dropped there), and
    /// the scope stays pinned once a proposal exists.
    /// </summary>
    public class ThermalSelectedScopeTests
    {
        private static ThermalEditServices Services()
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()));
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

        private static ThermalPerformanceViewModel WholeEnvelope(AnalyticalModel model, IEnumerable<SAMObject> selected)
        {
            ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services());
            viewModel.Update(model, selected);
            viewModel.Mode = ThermalPerformanceMode.WholeEnvelope;
            return viewModel;
        }

        // 1
        [Fact]
        public void In_the_whole_envelope_one_selected_panel_makes_only_the_selected_available()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, parts.WallPanels.Take(1))))
            {
                ThermalRowEditor editor = Row(viewModel, parts.Wall.Name);
                Assert.Equal(1, editor.Row.SelectedCount);
                Assert.Contains("1 selected", editor.Row.Detail);

                Type(editor, "0.30");

                Assert.True(editor.ScopeSelectedEnabled);
                Assert.Equal("Only the 1 selected", editor.ScopeSelectedLabel);
                Assert.Equal("All 12 panels using it", editor.ScopeAllLabel);
                Assert.Null(editor.ScopeReason);

                // Which cards are shown does not depend on the selection: both wall constructions are still there.
                Assert.Contains(viewModel.Groups.SelectMany(x => x.Rows), x => x.ConstructionName == parts.OtherWall.Name);
            }
        }

        // 2
        [Fact]
        public void In_the_whole_envelope_two_selected_panels_of_one_construction_make_only_the_2_selected()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, parts.WallPanels.Skip(4).Take(2))))
            {
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");

                Assert.True(editor.ScopeSelectedEnabled);
                Assert.Equal("Only the 2 selected", editor.ScopeSelectedLabel);

                editor.ScopeSelected = true;
                editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

                Assert.Equal(2, viewModel.Session.ElementCount);
                Assert.Contains("2 selected panels of the 12", editor.ScopeText);
            }
        }

        // 3
        [Fact]
        public void Selected_panels_that_do_not_use_the_construction_of_the_row_do_not_count()
        {
            ThermalParts parts = ThermalFixture.Build();
            List<Guid> otherPanels = UValueFixture.PanelGuids(parts.Model, parts.OtherWall);
            Assert.NotEmpty(otherPanels);

            // One wall of each construction, and a window, are selected.
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, parts.WallPanels.Take(1).Concat(otherPanels.Take(1)), parts.Windows.Take(1))))
            {
                ThermalPerformanceRow wall = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.Wall.Name);
                ThermalPerformanceRow other = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionName == parts.OtherWall.Name);

                Assert.Equal(parts.WallPanels.Take(1), wall.SelectedGuids);
                Assert.Equal(otherPanels.Take(1), other.SelectedGuids);

                ThermalRowEditor editor = Type(wall.Editor, "0.30");
                Assert.Equal(1, editor.Scope.SelectedCount);
            }

            // Only a panel of the other construction is selected: nothing of this row is.
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, otherPanels.Take(1))))
            {
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");

                Assert.Equal(0, editor.Row.SelectedCount);
                Assert.False(editor.ScopeSelectedEnabled);
                Assert.Contains("No selected panel uses", editor.ScopeReason);
                Assert.Equal(1, Row(viewModel, parts.OtherWall.Name).Row.SelectedCount);
            }
        }

        // 4 and 5
        [Fact]
        public void Apply_with_the_selected_scope_changes_exactly_those_panels_and_one_undo_restores_them()
        {
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            ui.Modified += (sender, e) => modified++;
            Func<ThermalChangeSet, ThermalChangeResult> applier = set => Modify.ApplyThermalChange(ui, set, x => { }, null);
            List<Guid> chosen = parts.WallPanels.Skip(3).Take(2).ToList();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(ui.JSAMObject, Select(ui.JSAMObject, chosen)))
            {
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");
                editor.ScopeSelected = true;
                editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

                SetUValueRequest request = viewModel.Session.BuildChangeSet().UValueRequests.Single();
                Assert.Equal(ThermalApplyScope.SelectedOnly, request.Scope);
                Assert.Equal(chosen.OrderBy(x => x), request.SelectedPanelGuids.OrderBy(x => x));

                ThermalChangeResult result = viewModel.Apply(applier);

                Assert.True(result.Succeeded, result.Error);
                Assert.Equal(2, result.PanelCount);
                Assert.Equal(1, modified);

                // Exactly the chosen panels moved to the new construction; the other 10 kept theirs.
                AdjacencyCluster adjacencyCluster = ui.JSAMObject.AdjacencyCluster;
                Guid newGuid = result.UValueResults[0].Construction.Guid;
                Assert.Equal(chosen.OrderBy(x => x), adjacencyCluster.GetPanels().Where(x => x.Construction?.Guid == newGuid).Select(x => x.Guid).OrderBy(x => x));
                Assert.Equal(10, adjacencyCluster.GetPanels(parts.Wall).Count);

                // One Undo restores the model (the restore runs off the calling thread, so wait for it).
                Assert.True(ui.CanUndo);
                Assert.True(ui.Undo());
                for (int i = 0; i < 100 && ui.JSAMObject.AdjacencyCluster.GetPanels(parts.Wall).Count != 12; i++)
                {
                    System.Threading.Thread.Sleep(50);
                }

                // Every panel is back on the original construction, and the new construction and its material are gone.
                AdjacencyCluster restored = ui.JSAMObject.AdjacencyCluster;
                Assert.Equal(12, restored.GetPanels(parts.Wall).Count);
                Assert.DoesNotContain(restored.GetPanels(), x => x.Construction?.Guid == newGuid);
                Assert.Null(ui.JSAMObject.MaterialLibrary.GetMaterial(result.UValueResults[0].MaterialName));
                Assert.False(ui.CanUndo);
            }
        }

        // 6
        [Fact]
        public void In_the_selection_mode_one_selected_panel_exposes_its_editable_row_with_the_selected_scope()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(1)));

                ThermalPerformanceRow row = Assert.Single(viewModel.Groups.SelectMany(x => x.Rows));
                Assert.Equal(parts.Wall.Name, row.ConstructionName);
                Assert.True(row.Editor.CanEdit);
                Assert.Equal(1, row.SelectedCount);
                Assert.Equal(12, row.UsedByCount);

                Type(row.Editor, "0.30");

                Assert.True(row.Editor.ScopeSelectedEnabled);
                Assert.Equal("Only the 1 selected", row.Editor.ScopeSelectedLabel);
            }
        }

        // 7
        [Fact]
        public void Changing_the_selection_before_a_proposal_updates_the_selected_count_in_both_modes()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, parts.WallPanels.Take(1))))
            {
                Assert.Equal(1, Row(viewModel, parts.Wall.Name).Row.SelectedCount);

                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Take(3)));
                Assert.Equal(3, Row(viewModel, parts.Wall.Name).Row.SelectedCount);

                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");
                Assert.Equal("Only the 3 selected", editor.ScopeSelectedLabel);

                viewModel.Discard();
                viewModel.Update(parts.Model, Select(parts.Model, Enumerable.Empty<Guid>()));
                Assert.Equal(0, Row(viewModel, parts.Wall.Name).Row.SelectedCount);

                viewModel.Mode = ThermalPerformanceMode.Selection;
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Skip(2).Take(4)));
                Assert.Equal(4, Row(viewModel, parts.Wall.Name).Row.SelectedCount);
            }
        }

        // 8
        [Fact]
        public void Changing_the_selection_after_a_proposal_does_not_change_the_pinned_scope()
        {
            ThermalParts parts = ThermalFixture.Build();
            List<Guid> pinned = parts.WallPanels.Take(2).ToList();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, pinned)))
            {
                ThermalRowEditor editor = Type(Row(viewModel, parts.Wall.Name), "0.30");
                editor.ScopeSelected = true;
                editor.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Assert.True(viewModel.Session.IsPending);

                // The user clicks elsewhere in the view: other panels, then nothing.
                viewModel.Update(parts.Model, Select(parts.Model, parts.WallPanels.Skip(6).Take(5)));
                viewModel.Update(parts.Model, Select(parts.Model, Enumerable.Empty<Guid>()));

                Assert.Same(editor, Row(viewModel, parts.Wall.Name));
                Assert.Equal(2, editor.Scope.SelectedCount);
                Assert.Equal(2, viewModel.Session.ElementCount);
                SetUValueRequest request = viewModel.Session.BuildChangeSet().UValueRequests.Single();
                Assert.Equal(pinned.OrderBy(x => x), request.SelectedPanelGuids.OrderBy(x => x));

                // A genuine outside change of the model still discards the proposal.
                viewModel.Update(parts.Model, Select(parts.Model, pinned), modelChanged: true);
                Assert.False(viewModel.Session.IsPending);
                Assert.Contains("discarded", viewModel.Session.Notice);
            }
        }

        [Fact]
        public void In_the_whole_envelope_a_selected_window_makes_only_the_selected_available_for_the_glazing_row()
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = WholeEnvelope(parts.Model, Select(parts.Model, null, parts.Windows.Take(2))))
            {
                ThermalRowEditor editor = Row(viewModel, GlazingFixture.CurrentName);
                Assert.Equal(2, editor.Row.SelectedCount);

                editor.OpenChange();
                editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));

                Assert.True(editor.ScopeSelectedEnabled);
                Assert.Equal("Only the 2 selected", editor.ScopeSelectedLabel);
            }
        }
    }
}
