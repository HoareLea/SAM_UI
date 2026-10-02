// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-3: the Glazing System Builder view-model (no WPF type, no model): seeding from a system (outside → inside, gas layers as gaps, the frame
    /// and its width), the edits (add / replace / remove / move / reverse a pane, gaps, frame, intended use), the check and Save's gating, the
    /// performance with stale answers dropped, and Save as predefined / Cancel against a temporary user library. Tas is the E0-1 stand-in.
    /// </summary>
    public sealed class GlazingBuilderViewModelTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;

        public GlazingBuilderViewModelTests()
        {
            library = BuilderFixture.Library(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception)
            {
            }
        }

        private static string F(double value) => value.ToString("0.00", CultureInfo.CurrentCulture);

        private string Hash() => File.Exists(library.Path) ? System.Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(library.Path))) : "absent";

        private static string Kinds(GlazingBuilderViewModel viewModel) => string.Join(",", viewModel.Layers.Select(x => x.IsPane ? "P" : "G"));

        private static void Settle(GlazingBuilderViewModel viewModel) => BuilderUiFixture.Settle(viewModel);

        // ---- Seeding ------------------------------------------------------------------------------------------------

        [Fact]
        public void A_system_seeds_the_draft_outside_to_inside_with_gas_layers_as_gaps_and_its_intended_use()
        {
            // SAM stores INSIDE -> OUTSIDE: the tinted pane is the INSIDE one here.
            ApertureConstruction seed = new ApertureConstruction(BuilderUiFixture.SeedGuid, "TINT_IN", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(BuilderFixture.Tint, 0.006), new ConstructionLayer(BuilderUiFixture.GasName, 0.016), new ConstructionLayer(BuilderFixture.Clear, 0.004) },
                new List<ConstructionLayer>() { new ConstructionLayer(BuilderFixture.FrameMaterial, 0.07) });
            seed.SetValue(ApertureConstructionParameter.DefaultPanelType, "Roof");

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, seed: seed))
            {
                Assert.Equal("P,G,P", Kinds(viewModel));
                Assert.Equal(BuilderFixture.Clear, ((DraftPane)viewModel.Draft.Layers[0]).OriginalName);
                Assert.Equal(DefaultGasType.Air, ((DraftGap)viewModel.Draft.Layers[1]).GasType);
                Assert.Equal(0.016, ((DraftGap)viewModel.Draft.Layers[1]).Thickness, 6);
                Assert.Equal(BuilderFixture.Tint, ((DraftPane)viewModel.Draft.Layers[2]).OriginalName);
                Assert.Equal(PanelType.Roof, viewModel.SelectedIntendedUse.Value);
                Assert.Equal(PanelType.Roof, viewModel.Draft.IntendedPanelType);

                // Composing the draft again gives the seed's SAM order (inside first): the one reversal point.
                GlazingComposition composition = BuilderFixture.Compose(viewModel.Draft);
                Assert.Equal(new[] { BuilderFixture.Tint, BuilderFixture.Clear }, composition.ApertureConstruction.PaneConstructionLayers.Where(x => x.Name == BuilderFixture.Tint || x.Name == BuilderFixture.Clear).Select(x => x.Name));
                Assert.Equal(seed.Guid, viewModel.Draft.BasedOnGuid);
                Assert.NotEqual(seed.Guid, viewModel.Draft.EvaluationGuid);
            }
        }

        [Fact]
        public void Seeding_from_a_system_the_Builder_made_keeps_where_its_panes_really_came_from()
        {
            ApertureConstruction saved = library.Save(BuilderFixture.Double("Made earlier")).Saved;
            GlazingSource user = GlazingSource.FromUserLibrary(library);

            GlazingBuilderOptions options = BuilderUiFixture.Options(library, new FakeDraftTas(), null, user.GetApertureConstructions(ApertureType.Window).Single(x => x.Guid == saved.Guid), true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model));
            options.SeedSource = user;
            using (GlazingBuilderViewModel viewModel = new GlazingBuilderViewModel(options))
            {
                // The panes came from the IGDB-shaped file, not from "My glazing systems" (which only holds them now).
                Assert.All(viewModel.Draft.Panes, x => Assert.Equal(BuilderFixture.Source, x.SourceLabel));
                Assert.All(viewModel.Draft.Panes, x => Assert.Equal(BuilderFixture.Source, x.SourceFileName));
                Assert.Equal("Made earlier (copy)", viewModel.Name);
            }
        }

        [Fact]
        public void The_default_name_is_the_seed_name_with_copy_and_stays_unique_in_my_glazing_systems()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.Equal("SEED_GLZ (copy)", viewModel.Name);
                Assert.StartsWith("New · based on SEED_GLZ · not saved", viewModel.StatusText);
                Assert.True(viewModel.CanSave);
            }

            Assert.True(library.Save(BuilderUiFixture.Builder(library).Draft).Succeeded);

            using (GlazingBuilderViewModel second = BuilderUiFixture.Builder(library))
            {
                Assert.Equal("SEED_GLZ (copy 2)", second.Name);
                Assert.True(second.CanSave);
            }
        }

        [Fact]
        public void A_frame_without_a_stored_width_proposes_its_depth_and_says_so_and_a_stored_width_is_kept()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.True(viewModel.HasFrame);
                Assert.Equal("70", viewModel.FrameWidthText);
                Assert.Equal(0.07, viewModel.Draft.Frame.Width, 6);
                Assert.Contains("depth", viewModel.FrameNote);

                viewModel.FrameWidthText = "55";
                Assert.Equal(0.055, viewModel.Draft.Frame.Width, 6);
                Assert.DoesNotContain("depth", viewModel.FrameNote);
            }

            using (GlazingBuilderViewModel stored = BuilderUiFixture.Builder(library, seed: BuilderUiFixture.Seed(frameWidth: 0.06)))
            {
                Assert.Equal("60", stored.FrameWidthText);
                Assert.DoesNotContain("depth", stored.FrameNote);
            }
        }

        [Fact]
        public void Without_a_seed_the_draft_is_empty_cannot_be_saved_and_asks_for_panes()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, withSeed: false))
            {
                Assert.Empty(viewModel.Layers);
                Assert.False(viewModel.HasFrame);
                Assert.False(viewModel.CanSave);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.NoPanes));
                Assert.Equal(GlazingBuilderPerformanceState.NotCalculated, viewModel.PerformanceState);
                Assert.Equal("New · not saved", viewModel.StatusText);
            }
        }

        // ---- Editing -----------------------------------------------------------------------------------------------

        [Fact]
        public void Adding_a_pane_after_a_pane_inserts_a_gap_first_and_selects_the_new_pane()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                Assert.True(viewModel.AddPane());

                // Nothing was selected: at the INSIDE end, after a pane, so a gap (a copy of the last gap) comes first.
                Assert.Equal("P,G,P,G,P", Kinds(viewModel));
                DraftGap added = (DraftGap)viewModel.Draft.Layers[3];
                Assert.Equal(DefaultGasType.Air, added.GasType);
                Assert.Equal(0.012, added.Thickness, 6);
                Assert.Equal(BuilderFixture.LowE, viewModel.SelectedLayer.Pane.OriginalName);
                Assert.Equal(4, viewModel.SelectedLayer.Index);

                // After the first layer: pane | new gap | new pane | the old gap | pane.
                viewModel.SelectedLayer = viewModel.Layers[0];
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.Tint);
                Assert.True(viewModel.AddPane());
                Assert.Equal("P,G,P,G,P,G,P", Kinds(viewModel));
                Assert.Equal(BuilderFixture.Tint, viewModel.Draft.Layers[2] is DraftPane pane ? pane.OriginalName : null);
            }
        }

        [Fact]
        public void Choosing_a_pane_in_the_browser_announces_the_pane_commands()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                List<string> raised = new List<string>();
                viewModel.PropertyChanged += (sender, e) => raised.Add(e.PropertyName);

                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                Assert.Contains(nameof(GlazingBuilderViewModel.CanAddPane), raised);
                Assert.Contains(nameof(GlazingBuilderViewModel.CanReplacePane), raised);
                Assert.True(viewModel.CanAddPane);
                Assert.False(viewModel.CanReplacePane);

                viewModel.SelectedLayer = viewModel.Layers[0];
                Assert.True(viewModel.CanReplacePane);
            }
        }

        [Fact]
        public void Adding_a_pane_without_a_chosen_pane_does_nothing()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.False(viewModel.CanAddPane);
                Assert.False(viewModel.AddPane());
                Assert.Equal("P,G,P", Kinds(viewModel));
            }
        }

        [Fact]
        public void Replacing_a_pane_keeps_the_stack_and_never_replaces_a_gap()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                viewModel.SelectedLayer = viewModel.Layers[1];
                Assert.False(viewModel.CanReplacePane);
                Assert.False(viewModel.ReplacePane());

                viewModel.SelectedLayer = viewModel.Layers[2];
                Assert.True(viewModel.CanReplacePane);
                Assert.True(viewModel.ReplacePane());

                Assert.Equal("P,G,P", Kinds(viewModel));
                Assert.Equal(BuilderFixture.LowE, viewModel.SelectedLayer.Pane.OriginalName);
                Assert.Equal(2, viewModel.SelectedLayer.Index);
                Assert.Equal(BuilderFixture.Clear, viewModel.Layers[0].Pane.OriginalName);
            }
        }

        [Fact]
        public void Gaps_are_added_removed_and_edited_in_place_without_rebuilding_the_list()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                GlazingBuilderLayerRow gap = viewModel.Layers[1];
                viewModel.SelectedLayer = gap;
                Assert.True(viewModel.AddGap());
                Assert.Equal("P,G,G,P", Kinds(viewModel));
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.GapOnGap));
                Assert.Equal(2, viewModel.SelectedLayer.Index);

                Assert.True(viewModel.RemoveSelected());
                Assert.Equal("P,G,P", Kinds(viewModel));

                GlazingBuilderLayerRow row = viewModel.Layers[1];
                GlazingGasOption argon = row.GasOptions.Single(x => x.Value == DefaultGasType.Argon);
                row.SelectedGas = argon;
                row.WidthText = "16";

                // The row is the same object (the user's focus survives); the draft has the edit; the gap's heat transfer is shown.
                Assert.Same(row, viewModel.Layers[1]);
                Assert.Equal(DefaultGasType.Argon, ((DraftGap)viewModel.Draft.Layers[1]).GasType);
                Assert.Equal(0.016, ((DraftGap)viewModel.Draft.Layers[1]).Thickness, 6);
                Assert.Contains("heat transfer", row.Detail);
                Assert.Contains("W/m²K", row.Detail);
            }
        }

        [Fact]
        public void A_gap_width_that_is_not_a_number_is_a_blocking_error_on_that_layer()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Layers[1].WidthText = "wide";

                Assert.False(viewModel.CanSave);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.ThicknessMissing));
                Assert.True(viewModel.Layers[1].HasIssue);
                Assert.Contains("Error", viewModel.Layers[1].IssueText);
                Assert.Contains("Error", viewModel.Layers[1].AutomationName);

                // A finding selects the layer it concerns.
                viewModel.SelectedLayer = null;
                viewModel.SelectIssue(viewModel.Issues.First(x => x.LayerIndex == 1));
                Assert.Same(viewModel.Layers[1], viewModel.SelectedLayer);

                viewModel.Layers[1].WidthText = "14";
                Assert.True(viewModel.CanSave);
            }
        }

        [Fact]
        public void Layers_move_one_place_at_a_time_and_stop_at_the_ends()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                viewModel.AddPane();

                // Outside → inside: clear | gap | clear | gap | low-e. Move the low-e pane outwards twice.
                Assert.Equal(new[] { BuilderFixture.Clear, BuilderFixture.Clear, BuilderFixture.LowE }, viewModel.Draft.Panes.Select(x => x.OriginalName));
                viewModel.SelectedLayer = viewModel.Layers[4];
                Assert.False(viewModel.CanMoveDown);
                Assert.True(viewModel.CanMoveUp);

                Assert.True(viewModel.MoveSelected(-1));
                Assert.True(viewModel.MoveSelected(-1));
                Assert.Equal(2, viewModel.SelectedLayer.Index);
                Assert.Equal(new[] { BuilderFixture.Clear, BuilderFixture.LowE, BuilderFixture.Clear }, viewModel.Draft.Panes.Select(x => x.OriginalName));

                viewModel.SelectedLayer = viewModel.Layers[0];
                Assert.False(viewModel.MoveSelected(-1));
                Assert.Equal(0, viewModel.SelectedLayer.Index);
            }
        }

        [Fact]
        public void Reversing_a_pane_swaps_its_faces_in_the_composed_system_and_again_undoes_it()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas))
            {
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                viewModel.SelectedLayer = viewModel.Layers[2];
                viewModel.ReplacePane();
                Settle(viewModel);

                Assert.True(viewModel.CanReverse);
                Assert.DoesNotContain("reversed", viewModel.SelectedLayer.Detail);

                Assert.True(viewModel.ToggleReverse());
                Settle(viewModel);
                Assert.True(viewModel.SelectedLayer.Pane.Reversed);
                Assert.Contains("reversed", viewModel.SelectedLayer.Detail);
                Assert.Contains("0.84 / 0.025", viewModel.SelectedLayer.Detail);
                Assert.Contains(BuilderFixture.LowE + " Reversed", tas.Requests.Last());

                int calls = tas.Calls;
                Assert.True(viewModel.ToggleReverse());
                Settle(viewModel);
                Assert.False(viewModel.SelectedLayer.Pane.Reversed);
                Assert.DoesNotContain("reversed", viewModel.SelectedLayer.Detail);

                // The build-up is one Tas already answered (the content is the key, not the history): no new call.
                Assert.Equal(calls, tas.Calls);

                // Not a glass pane (a gap): nothing to reverse.
                viewModel.SelectedLayer = viewModel.Layers[1];
                Assert.False(viewModel.CanReverse);
                Assert.False(viewModel.ToggleReverse());
            }
        }

        [Fact]
        public void The_intended_use_decides_the_gap_orientation_and_a_seeded_other_use_is_kept_as_an_option()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, seed: BuilderUiFixture.Seed(PanelType.CurtainWall)))
            {
                Assert.Equal(PanelType.CurtainWall, viewModel.SelectedIntendedUse.Value);
                Assert.Contains(viewModel.IntendedUses, x => x.Value == PanelType.CurtainWall);
                Assert.Contains(viewModel.IntendedUses, x => x.Value == PanelType.Roof);
                Assert.Contains(viewModel.IntendedUses, x => x.Value == PanelType.Undefined);

                string vertical = viewModel.Layers[1].Detail;
                viewModel.SelectedIntendedUse = viewModel.IntendedUses.Single(x => x.Value == PanelType.Roof);
                Assert.Equal(PanelType.Roof, viewModel.Draft.IntendedPanelType);
                Assert.NotEqual(vertical, viewModel.Layers[1].Detail);

                viewModel.SelectedIntendedUse = viewModel.IntendedUses.Single(x => x.Value == PanelType.Undefined);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.NoIntendedUse));
                Assert.True(viewModel.CanSave);
            }
        }

        [Fact]
        public void The_frame_is_none_or_copied_with_an_explicit_width()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.Equal(2, viewModel.FrameChoices.Count);
                Assert.True(viewModel.FrameChoices[0].IsNone);
                Assert.Contains("SEED_GLZ", viewModel.SelectedFrame.Label);

                viewModel.SelectedFrame = viewModel.FrameChoices[0];
                Assert.False(viewModel.HasFrame);
                Assert.True(viewModel.Draft.Frame.IsNone);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.Frameless));
                Assert.Equal("no frame", viewModel.UfText);
                Assert.Contains("No frame", viewModel.FrameNote);

                viewModel.SelectedFrame = viewModel.FrameChoices[1];
                Assert.True(viewModel.HasFrame);
                Assert.False(viewModel.Draft.Frame.IsNone);
                Assert.Equal("70", viewModel.FrameWidthText);

                viewModel.FrameWidthText = string.Empty;
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.FrameWidthMissing));
                Assert.True(viewModel.CanSave);
            }
        }

        // ---- Performance -------------------------------------------------------------------------------------------

        [Fact]
        public void The_performance_shows_Ug_g_LT_and_Uf_with_their_labels_and_never_calls_a_stored_value_Uw()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas))
            {
                Assert.Equal(GlazingBuilderPerformanceState.Calculated, viewModel.PerformanceState);
                Assert.Equal(1, tas.Calls);
                Assert.Equal(F(5.7 / 2) + " W/m²K", viewModel.UgText);
                Assert.Equal(F(0.5), viewModel.GText);
                Assert.Equal(F(0.7), viewModel.LightTransmittanceText);
                Assert.Equal(F(1.8) + " W/m²K", viewModel.UfText);
                Assert.False(viewModel.PerformanceIsStale);
                Assert.Equal(string.Empty, viewModel.PerformanceText);
            }
        }

        [Fact]
        public void The_reference_Uw_is_an_example_with_its_window_and_frame_width_and_no_spacer_psi()
        {
            double ug = 1.2;
            double uf = 2.0;
            double width = 0.07;
            double pane = (1.23 - 2 * width) * (1.48 - 2 * width);
            double frame = 1.23 * 1.48 - pane;

            Assert.Equal((ug * pane + uf * frame) / (pane + frame), GlazingReferenceWindow.Uw(ug, uf, width), 9);
            Assert.True(double.IsNaN(GlazingReferenceWindow.Uw(ug, double.NaN, width)));
            Assert.True(double.IsNaN(GlazingReferenceWindow.Uw(ug, uf, double.NaN)));
            Assert.True(double.IsNaN(GlazingReferenceWindow.Uw(ug, uf, 0.7)));

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.StartsWith("Uw example", viewModel.ReferenceUwText);
                Assert.Contains("1.23 × 1.48 m", viewModel.ReferenceUwText);
                Assert.Contains("frame width 70 mm", viewModel.ReferenceUwText);
                Assert.Contains("no spacer Ψ", viewModel.ReferenceUwText);

                // Between Ug and Uf, as an area-weighted value is.
                double uw = GlazingReferenceWindow.Uw(5.7 / 2, 1.8, 0.07);
                Assert.Contains(F(uw), viewModel.ReferenceUwText);

                viewModel.FrameWidthText = string.Empty;
                Assert.Contains("enter a frame width", viewModel.ReferenceUwText);

                viewModel.SelectedFrame = viewModel.FrameChoices[0];
                Assert.Contains("no frame (= Ug)", viewModel.ReferenceUwText);
            }
        }

        [Fact]
        public void An_answer_for_an_older_build_up_is_never_shown_and_the_newest_wins_in_any_order()
        {
            // Tas answers each request when the test says, with a Ug the test chooses.
            ScriptedTas tas = new ScriptedTas();
            GlazingBuilderOptions options = BuilderUiFixture.Options(library, null, null, null, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model));
            options.Evaluator = new DraftGlazingEvaluator(tas, TimeSpan.Zero, null, BuilderFixture.Options());
            using (GlazingBuilderViewModel viewModel = new GlazingBuilderViewModel(options))
            {
                tas.Answer(0, 2.0);
                Assert.True(viewModel.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10)));
                Assert.Equal(F(2.0) + " W/m²K", viewModel.UgText);

                // Edit A (a wider gap), then edit B (a wider one still): both are asked of Tas; the first answer is for a build-up that is gone.
                viewModel.Layers[1].WidthText = "14";
                Task first = viewModel.LastEvaluationTask;
                viewModel.Layers[1].WidthText = "16";
                Task newest = viewModel.LastEvaluationTask;

                Assert.Equal(3, tas.Calls);
                Assert.Equal(GlazingBuilderPerformanceState.Calculating, viewModel.PerformanceState);
                Assert.True(viewModel.PerformanceIsStale);
                Assert.Contains("Calculating", viewModel.PerformanceText);
                Assert.Equal(F(2.0) + " W/m²K", viewModel.UgText);

                // The newest answers first, the older one LAST: it must not replace what is shown.
                tas.Answer(2, 3.3);
                Assert.True(newest.Wait(TimeSpan.FromSeconds(10)));
                Assert.Equal(F(3.3) + " W/m²K", viewModel.UgText);
                Assert.False(viewModel.PerformanceIsStale);

                tas.Answer(1, 9.9);
                Assert.True(first.Wait(TimeSpan.FromSeconds(10)));
                Assert.Equal(F(3.3) + " W/m²K", viewModel.UgText);
                Assert.Equal(GlazingBuilderPerformanceState.Calculated, viewModel.PerformanceState);
            }
        }

        // Tas answering request n when told to: for stale-answer tests.
        private sealed class ScriptedTas : IGlazingEvaluator
        {
            private readonly List<TaskCompletionSource<double>> answers = new List<TaskCompletionSource<double>>();

            public int Calls { get { lock (answers) { return answers.Count; } } }

            public void Answer(int index, double ug)
            {
                lock (answers)
                {
                    answers[index].TrySetResult(ug);
                }
            }

            public Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, System.Threading.CancellationToken cancellationToken)
            {
                Guid guid = request.Batches.Single().Guids.Single();
                TaskCompletionSource<double> source = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (answers)
                {
                    answers.Add(source);
                }

                return source.Task.ContinueWith(t => new GlazingEvaluation(new Dictionary<Guid, GlazingValues>() { [guid] = new GlazingValues(t.Result, 0.5, 0.7, 1.8) }, 1, null));
            }
        }

        [Fact]
        public void A_failed_calculation_says_why_shows_no_values_and_does_not_block_saving()
        {
            FakeDraftTas tas = new FakeDraftTas() { Throw = true };
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas))
            {
                Assert.Equal(GlazingBuilderPerformanceState.NotCalculated, viewModel.PerformanceState);
                Assert.StartsWith("Not calculated:", viewModel.PerformanceText);
                Assert.Equal("–", viewModel.UgText);
                Assert.Equal(string.Empty, viewModel.ReferenceUwText);
                Assert.True(viewModel.CanSave);
            }
        }

        [Fact]
        public void A_build_up_with_errors_asks_nothing_of_Tas()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas))
            {
                int calls = tas.Calls;
                viewModel.Layers[1].WidthText = "wide";
                Settle(viewModel);

                Assert.Equal(calls, tas.Calls);
                Assert.Equal(GlazingBuilderPerformanceState.NotCalculated, viewModel.PerformanceState);
                Assert.Contains("correct the errors", viewModel.PerformanceText);
            }
        }

        [Fact]
        public void Changing_only_the_name_does_not_ask_Tas_again()
        {
            FakeDraftTas tas = new FakeDraftTas();
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas))
            {
                int calls = tas.Calls;
                viewModel.Name = "Something else";
                viewModel.Name = "Something else again";
                Settle(viewModel);

                Assert.Equal(calls, tas.Calls);
                Assert.Equal(GlazingBuilderPerformanceState.Calculated, viewModel.PerformanceState);
            }
        }

        // ---- Check and Save gating ---------------------------------------------------------------------------------

        [Fact]
        public void Save_is_disabled_by_blocking_errors_and_enabled_by_warnings()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.True(viewModel.CanSave);
                Assert.StartsWith("✓", viewModel.ValidationSummary);

                viewModel.Name = "   ";
                Assert.False(viewModel.CanSave);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.NameRequired));
                Assert.StartsWith("✕ 1 error", viewModel.ValidationSummary);
                Assert.Contains(viewModel.Issues, x => x.Text.StartsWith("✕ Error:"));

                viewModel.Name = "Fine";
                Assert.True(viewModel.CanSave);

                // A warning (no intended use) does not block.
                viewModel.SelectedIntendedUse = viewModel.IntendedUses.Single(x => x.Value == PanelType.Undefined);
                Assert.True(viewModel.CanSave);
                Assert.StartsWith("⚠", viewModel.ValidationSummary);

                // A gas at the edge (the last pane removed) is an error.
                viewModel.SelectedLayer = viewModel.Layers[2];
                viewModel.RemoveSelected();
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.GasAtEdge));
                Assert.False(viewModel.CanSave);
            }
        }

        [Fact]
        public void A_name_already_in_my_glazing_systems_blocks_Save_in_any_case_and_a_new_one_frees_it()
        {
            Assert.True(library.Save(BuilderFixture.Double("Taken")).Succeeded);

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Name = "taken ";
                Assert.False(viewModel.CanSave);
                Assert.True(viewModel.Validation.Has(GlazingDraftIssueCodes.DuplicateName));

                viewModel.Name = "Not taken";
                Assert.True(viewModel.CanSave);
            }
        }

        [Fact]
        public void Without_a_library_there_is_nowhere_to_save()
        {
            using (GlazingBuilderViewModel viewModel = new GlazingBuilderViewModel(BuilderUiFixture.Options(null, new FakeDraftTas(), null, null, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model))))
            {
                Assert.False(viewModel.CanSave);
                Assert.False(viewModel.SaveAsync().Result);
            }
        }

        // ---- Save and Cancel ---------------------------------------------------------------------------------------

        [Fact]
        public void Editing_previewing_and_checking_write_nothing_anywhere_and_Cancel_leaves_everything_as_it_was()
        {
            string before = Hash();
            int changed = 0;
            library.Changed += (sender, e) => changed++;

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                viewModel.AddPane();
                viewModel.ToggleReverse();
                viewModel.AddGap();
                viewModel.Name = "Never saved";
                viewModel.SelectedFrame = viewModel.FrameChoices[0];
                Settle(viewModel);
            }

            Assert.Equal(before, Hash());
            Assert.False(File.Exists(library.Path));
            Assert.False(File.Exists(library.BackupPath));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void Save_as_predefined_writes_one_new_system_raises_Changed_and_Saved_once_and_the_draft_is_unchanged()
        {
            int changed = 0;
            int saved = 0;
            library.Changed += (sender, e) => changed++;

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Saved += (sender, e) => saved++;
                viewModel.Panes.SelectedEntry = BuilderUiFixture.Pane(viewModel, BuilderFixture.LowE);
                viewModel.SelectedLayer = viewModel.Layers[2];
                viewModel.ReplacePane();
                viewModel.Name = "E0 Double";
                Settle(viewModel);
                string text = viewModel.UgText;

                Assert.True(viewModel.SaveAsync().Result);

                Assert.Equal(1, changed);
                Assert.Equal(1, saved);
                Assert.NotNull(viewModel.SavedSystem);
                Assert.Equal("E0 Double", viewModel.SavedSystem.Name);
                Assert.NotEqual(viewModel.Draft.EvaluationGuid, viewModel.SavedSystem.Guid);
                Assert.True(viewModel.SaveResult.Succeeded);
                Assert.False(viewModel.IsSaving);
                Assert.False(viewModel.CanSave);
                Assert.Null(viewModel.SaveError);
                Assert.Equal("Saved to My glazing systems as E0 Double.", viewModel.StatusText);

                UserGlazingLibraryContent content = library.Read();
                ApertureConstruction system = Assert.Single(content.Systems);
                Assert.Equal(viewModel.SavedSystem.Guid, system.Guid);
                Assert.Equal(PanelType.WallExternal.ToString(), system.TryGetValue(ApertureConstructionParameter.DefaultPanelType, out string panelType) ? panelType : null);
                Assert.True(system.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double width));
                Assert.Equal(0.07, width, 6);

                // Embedded materials: Tas and Apply can use the system alone. Provenance keeps the Tas values the draft had.
                Dictionary<string, IMaterial> materials = GlazingSource.FromUserLibrary(library).GetMaterials();
                Assert.All(system.PaneConstructionLayers.Concat(system.FrameConstructionLayers), x => Assert.True(materials.ContainsKey(x.Name), x.Name));
                Assert.Equal(text, viewModel.UgText);
                Assert.NotNull(system.GetParameterSet(GlazingBuilderProvenance.ParameterSetName));
            }
        }

        [Fact]
        public void A_second_Save_after_a_success_is_refused_and_writes_nothing()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                Assert.True(viewModel.SaveAsync().Result);
                string after = Hash();

                Assert.False(viewModel.SaveAsync().Result);
                Assert.Equal(after, Hash());
                Assert.Single(library.Read().Systems);
            }
        }

        [Fact]
        public void A_Save_that_fails_keeps_everything_says_why_and_can_be_tried_again()
        {
            int changed = 0;
            library.Changed += (sender, e) => changed++;
            Directory.CreateDirectory(Path.GetDirectoryName(library.Path));
            File.WriteAllText(library.Path, "{ this is not a library");
            string before = Hash();

            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                int saved = 0;
                viewModel.Saved += (sender, e) => saved++;

                Assert.False(viewModel.SaveAsync().Result);

                Assert.Null(viewModel.SavedSystem);
                Assert.Equal(0, saved);
                Assert.Equal(0, changed);
                Assert.False(string.IsNullOrWhiteSpace(viewModel.SaveError));
                Assert.False(viewModel.IsSaving);
                Assert.Equal(before, Hash());
                Assert.Equal("{ this is not a library", File.ReadAllText(library.Path));

                // The file is repaired (removed): the same Builder saves.
                File.Delete(library.Path);
                Assert.True(viewModel.CanSave);
                Assert.True(viewModel.SaveAsync().Result);
                Assert.Equal(1, saved);
                Assert.Equal(1, changed);
            }
        }

        [Fact]
        public void A_name_taken_by_another_window_meanwhile_is_refused_at_Save_with_the_check_in_the_list()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                viewModel.Name = "Race";
                Assert.True(viewModel.CanSave);

                // Another SAM window saves "Race" after this Builder read the names.
                Assert.True(BuilderFixture.Library(directory).Save(BuilderFixture.Double("Race")).Succeeded);

                Assert.False(viewModel.SaveAsync().Result);
                Assert.Contains("Race", viewModel.SaveError);
                Assert.Null(viewModel.SavedSystem);
                Assert.Single(library.Read().Systems);
            }
        }

        [Fact]
        public void Edits_are_ignored_while_the_system_is_being_saved()
        {
            using (GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library))
            {
                // Another SAM window holds the library's lock: this Save waits for it, so the Builder is saving for a moment.
                Task<bool> save;
                using (new FileStream(library.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    save = viewModel.SaveAsync();
                    Assert.True(viewModel.IsSaving);
                    Assert.False(viewModel.CanSave);

                    string name = viewModel.Name;
                    int layers = viewModel.Layers.Count;
                    viewModel.Name = "changed meanwhile";
                    Assert.False(viewModel.AddGap());
                    Assert.False(viewModel.RemoveSelected());
                    viewModel.FrameWidthText = "99";
                    viewModel.SelectedFrame = viewModel.FrameChoices[0];
                    Assert.Equal(name, viewModel.Name);
                    Assert.Equal(layers, viewModel.Layers.Count);
                    Assert.True(viewModel.HasFrame);
                    Assert.False(save.IsCompleted);
                }

                Assert.True(save.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(save.Result);
                Assert.False(viewModel.IsSaving);
                Assert.Equal("SEED_GLZ (copy)", viewModel.SavedSystem.Name);
            }
        }

        [Fact]
        public void A_disposed_Builder_ignores_late_answers_and_disposing_twice_is_fine()
        {
            FakeDraftTas tas = new FakeDraftTas();
            GlazingBuilderViewModel viewModel = BuilderUiFixture.Builder(library, tas);
            tas.Hold();
            viewModel.Layers[1].WidthText = "14";
            Task pending = viewModel.LastEvaluationTask;

            viewModel.Dispose();
            viewModel.Dispose();
            tas.Release();

            Assert.True(pending.Wait(TimeSpan.FromSeconds(10)));
            Assert.False(File.Exists(library.Path));
        }

        [Fact]
        public void The_Builder_holds_no_model_so_it_cannot_change_one()
        {
            // Structural, like the E0-1 scan (which now covers the Builder view-model, rows, pane browser and options as well).
            Assert.Empty(BuilderSurface.ModelReferences());
            Assert.Contains(BuilderSurface.Types(), x => x == typeof(GlazingBuilderViewModel));
            Assert.Contains(BuilderSurface.Types(), x => x == typeof(GlazingPaneBrowser));
        }
    }
}
