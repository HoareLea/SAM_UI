// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR3: Glazing Builder 2 - EDIT a saved system with round-trip fidelity and Save and replace. Opening a saved system to edit starts from a
    /// copy that reproduces it: the same layers, materials, gap coefficients, frame and values (a pane saved reversed reopens as the original pane with
    /// Reverse on), under its own name; Save as new needs another name; Save and replace saves the result as a new system (new Guid) and archives the
    /// one that was opened, in one transaction. The seed is never changed; a copy made from the candidate list ("New system based on this…") is still a
    /// copy. The Builder holds no model. These run as the app does (STA thread, WPF dispatcher; <c>[WpfFact]</c>) over the Tas stand-in and a temporary
    /// library file.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class GlazingBuilderEditTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private int changed;

        public GlazingBuilderEditTests()
        {
            library = BuilderFixture.Library(directory);
            library.Changed += (sender, e) => changed++;
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

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static async Task Pump(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                await Task.Delay(10);
                Flush();
            }
        }

        private static async Task Settle(GlazingBuilderViewModel builder)
        {
            Task evaluation = builder.LastEvaluationTask;
            Task panes = builder.Panes.LastWork;
            await Pump(() => evaluation.IsCompleted && panes.IsCompleted, "the Builder to settle");
            await evaluation;
            await panes;
        }

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            changed = 0;
            return result.Saved;
        }

        // The Builder on a saved system of the user library, as My library → Open in Builder… opens it.
        private async Task<GlazingBuilderViewModel> Open(ApertureConstruction saved, bool edit = true, FakeDraftTas tas = null)
        {
            GlazingSource user = GlazingSource.FromUserLibrary(library);
            ApertureConstruction seed = user.GetApertureConstructions(ApertureType.Window).Single(x => x.Guid == saved.Guid);
            GlazingBuilderOptions options = BuilderUiFixture.Options(library, tas ?? new FakeDraftTas(), null, seed, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model));
            options.SeedSource = user;
            options.EditSeed = edit;
            GlazingBuilderViewModel result = new GlazingBuilderViewModel(options);
            await Settle(result);
            return result;
        }

        // A draft with everything round-trip has to carry: a reversed pane, three panes, two gaps, a frame with a width and both additional heat transfers (the frame's, 10 %, comes with the copied frame).
        private static GlazingSystemDraft Rich(string name)
        {
            GlazingSystemDraft result = BuilderFixture.Triple(name);
            ((DraftPane)result.Layers[4]).Reversed = true;                  // the inside low-e pane, coating the other way round
            result.Frame.Width = 0.05;
            result.PaneAdditionalHeatTransfer = 5;
            return result;
        }

        private static string Json(ApertureConstruction system) => system.ToJsonObject().ToJsonString();

        // ---- Round-trip identity ----------------------------------------------------------------------------------------

        [WpfFact]
        public async Task Opening_a_saved_system_and_saving_it_unchanged_gives_the_same_layers_materials_coefficients_frame_and_values()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            ConstructionManager before = UserLibraryFile.Parse(File.ReadAllText(library.Path), out _);
            int materials_Before = before.MaterialLibrary.GetMaterials().Count;
            ApertureConstruction original = library.Read().Systems.Single(x => x.Guid == saved.Guid);

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                // The reversed pane reopens as the pane it was made from, Reverse on; the others are as they were.
                DraftPane inside = (DraftPane)builder.Draft.Layers[4];
                Assert.True(inside.Reversed);
                Assert.Equal(BuilderFixture.LowE, inside.OriginalName);
                Assert.All(new[] { 0, 2 }, i => Assert.False(((DraftPane)builder.Draft.Layers[i]).Reversed));
                Assert.Equal("P,G,P,G,P", string.Join(",", builder.Layers.Select(x => x.IsPane ? "P" : "G")));

                builder.Name = "Rich, saved again";
                Assert.True(await builder.SaveAsync(), builder.SaveError);
                ApertureConstruction again = library.Read().Systems.Single(x => x.Guid == builder.SavedSystem.Guid);

                // Layers (inside first, as SAM stores them): the same material names and thicknesses, pane and frame.
                Assert.NotEqual(original.Guid, again.Guid);
                Assert.Equal(original.PaneConstructionLayers.Select(x => (x.Name, x.Thickness)), again.PaneConstructionLayers.Select(x => (x.Name, x.Thickness)));
                Assert.Equal(original.FrameConstructionLayers.Select(x => (x.Name, x.Thickness)), again.FrameConstructionLayers.Select(x => (x.Name, x.Thickness)));

                // Every parameter the Builder writes is the same: use, description, frame width, both additional heat transfers (the frame's, 10 %, comes with the copied frame).
                Assert.Equal(original.GetValue<string>(ApertureConstructionParameter.DefaultPanelType), again.GetValue<string>(ApertureConstructionParameter.DefaultPanelType));
                Assert.Equal(original.GetValue<string>(ApertureConstructionParameter.Description), again.GetValue<string>(ApertureConstructionParameter.Description));
                Assert.Equal(0.05, again.GetValue<double>(ApertureConstructionParameter.DefaultFrameWidth), 9);
                Assert.Equal(5, again.GetValue<double>(ApertureConstructionParameter.PaneAdditionalHeatTransfer), 9);
                Assert.Equal(10, again.GetValue<double>(ApertureConstructionParameter.FrameAdditionalHeatTransfer), 9);

                // No material was added or renamed: the reversed pane is the very same "<name> Reversed" material (identity, not a lookalike).
                Assert.Empty(builder.SaveResult.AddedMaterials);
                Assert.Empty(builder.SaveResult.RenamedMaterials);
                Assert.Equal(materials_Before, library.Read().ConstructionManager.MaterialLibrary.GetMaterials().Count);
                Assert.Contains(BuilderFixture.LowE + " Reversed", again.PaneConstructionLayers.Select(x => x.Name));

                // The provenance records the same build (panes incl. Reversed, gap coefficients, frame), and what it was based on.
                GlazingBuilderProvenance p0 = GlazingBuilderProvenance.FromApertureConstruction(original);
                GlazingBuilderProvenance p1 = GlazingBuilderProvenance.FromApertureConstruction(again);
                Assert.Equal(p0.Panes.Select(x => (x.Position, x.Material, x.OriginalName, x.Reversed, x.Thickness)), p1.Panes.Select(x => (x.Position, x.Material, x.OriginalName, x.Reversed, x.Thickness)));
                Assert.Equal(p0.Gaps.Select(x => (x.Position, x.Gas, x.Thickness, x.HeatTransferCoefficient, x.TiltDegrees, x.Material)), p1.Gaps.Select(x => (x.Position, x.Gas, x.Thickness, x.HeatTransferCoefficient, x.TiltDegrees, x.Material)));
                Assert.Equal(p0.FrameWidth, p1.FrameWidth, 9);
                Assert.Equal(p0.IntendedPanelType, p1.IntendedPanelType);
                Assert.Equal(original.Guid, p1.BasedOnGuid);
                Assert.Null(p1.SupersedesGuid);
            }

            // The system that was opened is exactly as it was.
            Assert.Equal(Json(original), Json(library.Read().Systems.Single(x => x.Guid == saved.Guid)));
        }

        [WpfFact]
        public async Task A_reopened_reversed_pane_can_be_un_reversed_and_composes_the_original_material()
        {
            ApertureConstruction saved = Save(Rich("Rich"));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                builder.SelectedLayer = builder.Layers[4];
                Assert.True(builder.CanReverse);
                Assert.True(builder.ToggleReverse());

                GlazingComposition composition = BuilderFixture.Compose(builder.Draft);
                Assert.False(((DraftPane)builder.Draft.Layers[4]).Reversed);
                Assert.Contains(BuilderFixture.LowE, composition.ApertureConstruction.PaneConstructionLayers.Select(x => x.Name));
                Assert.DoesNotContain(BuilderFixture.LowE + " Reversed", composition.ApertureConstruction.PaneConstructionLayers.Select(x => x.Name));
            }
        }

        [Fact]
        public void Unreverse_undoes_exactly_what_Reverse_made_and_refuses_anything_else()
        {
            TransparentMaterial original = BuilderFixture.LowEPane();
            TransparentMaterial reversed = Query.Reverse(original);

            TransparentMaterial undone = Query.Unreverse(reversed);

            Assert.NotNull(undone);
            Assert.Equal(original.Name, undone.Name);
            Assert.True(MaterialIdentity.Same(original, undone));
            Assert.True(MaterialIdentity.Same(reversed, Query.Reverse(undone)));

            // Not made by the Builder: IGDB's own "… Reversed" entry has a different name pattern in its display name; a pane without the suffix is not a reversal.
            Assert.Null(Query.Unreverse(BuilderFixture.LowEPane()));
            Assert.Null(Query.Unreverse(BuilderFixture.ClearPane()));
            TransparentMaterial lookalike = BuilderFixture.LowEPaneReversedEntry();
            TransparentMaterial lookalikeUndone = Query.Unreverse(lookalike);
            Assert.True(lookalikeUndone == null || MaterialIdentity.Same(Query.Reverse(lookalikeUndone), lookalike));
        }

        [WpfFact]
        public async Task A_copy_made_without_edit_mode_still_reverses_back_to_the_original_pane_but_offers_no_replace()
        {
            ApertureConstruction saved = Save(Rich("Rich"));

            using (GlazingBuilderViewModel builder = await Open(saved, edit: false))
            {
                Assert.True(((DraftPane)builder.Draft.Layers[4]).Reversed);                  // fidelity applies to every copy of a Builder system
                Assert.False(builder.IsEditing);
                Assert.Equal("Rich (copy)", builder.Name);
                Assert.Equal("New · based on Rich · not saved", builder.StatusText);
                Assert.Equal("Save as predefined", builder.SaveButtonText);
                Assert.Equal(string.Empty, builder.SaveAndReplaceText);
                Assert.False(builder.CanSaveAndReplace);
                Assert.False(await builder.SaveAndReplaceAsync());
                Assert.Equal(1, library.Read().Systems.Count);
            }
        }

        // ---- Edit mode --------------------------------------------------------------------------------------------------

        [WpfFact]
        public async Task Editing_starts_from_a_copy_under_the_systems_own_name_that_can_only_be_replaced_until_it_is_renamed()
        {
            ApertureConstruction saved = Save(Rich("Rich"));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                Assert.True(builder.IsEditing);
                Assert.Equal("Rich", builder.EditedName);
                Assert.Equal("Rich", builder.Name);
                Assert.Equal("Editing a copy of Rich · saving creates a new system", builder.StatusText);
                Assert.Equal("Save as new", builder.SaveButtonText);
                Assert.Equal("Save and replace Rich", builder.SaveAndReplaceText);

                // The check shown is the one replacing is held to: the edited system's own name is free, so there is nothing wrong with it.
                Assert.False(builder.Validation.HasErrors);
                Assert.Equal("✓ Ready to save.", builder.ValidationSummary);
                Assert.True(builder.CanSaveAndReplace);

                // Save as new needs another name, and says so.
                Assert.False(builder.CanSave);
                Assert.Contains("give it a name other than 'Rich'", builder.SaveAsNewHint);

                builder.Name = "Rich 2";
                Assert.True(builder.CanSave);
                Assert.True(builder.CanSaveAndReplace);
                Assert.Equal(string.Empty, builder.SaveAsNewHint);
            }
        }

        [WpfFact]
        public async Task Another_systems_name_blocks_both_saves_while_editing()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            Save(BuilderFixture.Double("Taken"));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                // The Builder read the names when it opened, so "Taken" is known.
                builder.Name = " taken ";

                Assert.False(builder.CanSave);
                Assert.False(builder.CanSaveAndReplace);
                Assert.True(builder.Validation.Has(GlazingDraftIssueCodes.DuplicateName));
                Assert.Contains("is already in My glazing systems", string.Join(" ", builder.Issues.Select(x => x.Text)));
            }
        }

        [WpfFact]
        public async Task Edit_mode_is_ignored_when_the_library_no_longer_holds_the_system_or_is_not_the_one_given()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            GlazingSource user = GlazingSource.FromUserLibrary(library);
            ApertureConstruction seed = user.GetApertureConstructions(ApertureType.Window).Single();
            Assert.True(library.Remove(saved.Guid).Succeeded);                                // gone before the Builder opens

            GlazingBuilderOptions options = BuilderUiFixture.Options(library, new FakeDraftTas(), null, seed, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model));
            options.SeedSource = user;
            options.EditSeed = true;
            using (GlazingBuilderViewModel builder = new GlazingBuilderViewModel(options))
            {
                await Settle(builder);
                Assert.False(builder.IsEditing);
                Assert.Equal("Rich (copy)", builder.Name);
                Assert.False(builder.CanSaveAndReplace);
            }

            // A seed that is not in the library at all (a model system, a default-library system) is a plain copy even when asked to edit.
            options = BuilderUiFixture.Options(library, new FakeDraftTas(), null, null, true, BuilderUiFixture.PaneSource(kind: GlazingSourceKind.Model));
            options.EditSeed = true;
            using (GlazingBuilderViewModel builder = new GlazingBuilderViewModel(options))
            {
                await Settle(builder);
                Assert.False(builder.IsEditing);
                Assert.Equal("SEED_GLZ (copy)", builder.Name);
            }
        }

        // ---- Save and replace -----------------------------------------------------------------------------------------------

        [WpfFact]
        public async Task Save_and_replace_keeps_the_name_adds_a_new_guid_archives_the_old_system_and_announces_the_change_once()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            Save(BuilderFixture.Double("Other"));
            string oldJson = Json(library.Read().Systems.Single(x => x.Guid == saved.Guid));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                int savedEvents = 0;
                builder.Saved += (sender, e) => savedEvents++;
                builder.Layers[1].WidthText = "20";                             // a real edit: the first gap is 20 mm
                await Settle(builder);

                Assert.True(await builder.SaveAndReplaceAsync(), builder.SaveError);

                ApertureConstruction replacement = builder.SavedSystem;
                Assert.Equal("Rich", replacement.Name);
                Assert.NotEqual(saved.Guid, replacement.Guid);
                Assert.Equal(saved.Guid, builder.SaveResult.Replaced.Guid);
                Assert.Equal(1, savedEvents);
                Assert.Equal(1, changed);
                Assert.Equal("Saved to My glazing systems as Rich, replacing Rich (kept in the archive).", builder.StatusText);
                Assert.False(builder.CanSave);
                Assert.False(builder.CanSaveAndReplace);

                List<ApertureConstruction> main = library.Read().Systems;
                Assert.Equal(new[] { "Other", "Rich" }, main.Select(x => x.Name).OrderBy(x => x));
                Assert.DoesNotContain(main, x => x.Guid == saved.Guid);
                GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(main.Single(x => x.Guid == replacement.Guid));
                Assert.Equal(saved.Guid, provenance.SupersedesGuid);
                Assert.Equal(saved.Guid, provenance.BasedOnGuid);
                Assert.Equal(3, provenance.SchemaVersion);
                Assert.Equal(0.02, provenance.Gaps.Single(x => x.Position == 1).Thickness, 9);
            }

            ConstructionManager archive = UserLibraryFile.Parse(File.ReadAllText(library.ArchivePath), out _);
            Assert.Equal(oldJson, Json(archive.ApertureConstructions.Single(x => x.Guid == saved.Guid)));
        }

        [WpfFact]
        public async Task Save_and_replace_of_a_system_that_was_removed_meanwhile_fails_cleanly_and_save_as_new_still_works()
        {
            ApertureConstruction saved = Save(Rich("Rich"));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                Assert.True(BuilderFixture.Library(directory).Remove(saved.Guid).Succeeded);       // another window removes it
                string hash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path)));

                Assert.False(await builder.SaveAndReplaceAsync());

                Assert.Contains("no longer in My glazing systems", builder.SaveError);
                Assert.Equal(hash, System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path))));
                Assert.Null(builder.SavedSystem);
                Assert.Equal(0, changed);

                // The draft is still good: as a new system under another name it saves.
                builder.Name = "Rich again";
                Assert.True(await builder.SaveAsync(), builder.SaveError);
                Assert.Equal(new[] { "Rich again" }, library.Read().Systems.Select(x => x.Name));
            }
        }

        [WpfFact]
        public async Task A_failed_save_and_replace_leaves_the_library_as_it_was_and_the_builder_open_for_a_retry()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            library.BeforeWrite = path =>
            {
                if (path == library.ArchivePath)
                {
                    throw new IOException("archive disk full");
                }
            };

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                string hash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path)));

                Assert.False(await builder.SaveAndReplaceAsync());

                Assert.Contains("Nothing was saved or replaced", builder.SaveError);
                Assert.Equal(hash, System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path))));
                Assert.Null(builder.SavedSystem);
                Assert.True(builder.CanSaveAndReplace);                                                // can try again
                Assert.False(File.Exists(library.ArchivePath));

                library.BeforeWrite = null;
                Assert.True(await builder.SaveAndReplaceAsync(), builder.SaveError);
                Assert.Equal(new[] { builder.SavedSystem.Guid }, library.Read().Systems.Select(x => x.Guid));
            }
        }

        [WpfFact]
        public async Task A_replace_the_library_refuses_under_its_lock_lists_the_issues_of_the_check_that_is_shown()
        {
            ApertureConstruction saved = Save(Rich("Rich"));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                builder.Name = "Late";
                await Settle(builder);
                Assert.True(builder.CanSaveAndReplace);
                Assert.True(BuilderFixture.Library(directory).Save(BuilderFixture.Double("Late")).Succeeded);   // another window takes the name meanwhile

                Assert.False(await builder.SaveAndReplaceAsync());

                Assert.Contains("is already in My glazing systems", builder.SaveError);
                Assert.True(builder.Validation.Has(GlazingDraftIssueCodes.DuplicateName));
                Assert.Equal(builder.Validation.Issues.Count(), builder.Issues.Count);                       // the list shows the check the summary shows ...
                Assert.Contains("is already in My glazing systems", string.Join(" ", builder.Issues.Select(x => x.Text)));   // ... with the library's finding
                Assert.Null(builder.SavedSystem);
            }
        }

        [WpfFact]
        public async Task The_Builder_still_holds_no_model_and_the_saved_system_is_never_changed_by_opening_or_cancelling()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            string hash = System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path)));

            using (GlazingBuilderViewModel builder = await Open(saved))
            {
                builder.Name = "Edited but never saved";
                builder.Layers[1].WidthText = "8";
                await Settle(builder);
            }

            Assert.Equal(hash, System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(library.Path))));
            Assert.False(File.Exists(library.ArchivePath));
            Assert.Equal(0, changed);
            Assert.Empty(BuilderSurface.ModelReferences());
        }

        // ---- The window -------------------------------------------------------------------------------------------------

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (T descendant in Descendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private static T ById<T>(DependencyObject root, string id) where T : FrameworkElement
        {
            return Descendants<T>(root).FirstOrDefault(x => AutomationProperties.GetAutomationId(x) == id);
        }

        private static void Press(Button button)
        {
            Assert.True(button.IsEnabled, button.Content + " is disabled");
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();
        }

        [WpfFact]
        public async Task The_window_offers_Save_and_replace_only_while_editing_and_replacing_closes_it()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            GlazingBuilderViewModel builder = await Open(saved);
            GlazingSystemBuilderWindow window = new GlazingSystemBuilderWindow(builder) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            try
            {
                Button replace = ById<Button>(window, "button_SaveReplace");
                Button asNew = ById<Button>(window, "button_Save");
                Assert.True(replace.IsVisible);
                Assert.True(replace.IsEnabled);
                Assert.Equal("Save and replace Rich", replace.Content);
                Assert.Equal("Save as new", asNew.Content);
                Assert.False(asNew.IsEnabled);                                                    // the name is the edited system's
                Assert.True(ById<TextBlock>(window, "textBlock_SaveHint").Text.Contains("other than 'Rich'"));
                Assert.StartsWith("Editing a copy of Rich", ById<TextBlock>(window, "textBlock_Status").Text);

                Press(replace);
                await Pump(() => builder.SavedSystem != null, "the replacement to be saved");
                await Pump(() => !window.IsLoaded, "the window to close");

                Assert.Equal(new[] { "Rich" }, library.Read().Systems.Select(x => x.Name));
                Assert.NotEqual(saved.Guid, library.Read().Systems.Single().Guid);
                Assert.True(File.Exists(library.ArchivePath));
            }
            finally
            {
                if (window.IsLoaded)
                {
                    window.Close();
                }
            }
        }

        [WpfFact]
        public async Task A_plain_copy_shows_no_replace_button()
        {
            ApertureConstruction saved = Save(Rich("Rich"));
            GlazingBuilderViewModel builder = await Open(saved, edit: false);
            GlazingSystemBuilderWindow window = new GlazingSystemBuilderWindow(builder) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            try
            {
                Assert.False(ById<Button>(window, "button_SaveReplace").IsVisible);
                Assert.Equal("Save as predefined", ById<Button>(window, "button_Save").Content);
                Assert.True(ById<Button>(window, "button_Save").IsEnabled);
                Assert.Equal(string.Empty, ById<TextBlock>(window, "textBlock_SaveHint").Text);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
