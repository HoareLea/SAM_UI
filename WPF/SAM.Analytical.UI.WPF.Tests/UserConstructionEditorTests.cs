// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR4: the classic Constructions editor (<c>Modify.EditConstructions</c> / <c>ConstructionLibraryWindow</c>, retained as it is) gains <b>Save to My
    /// constructions…</b> for the selected authored or imported opaque construction. The editor's own button raises an event (shown only while a host
    /// handles it, like "Set U-value…") and stays open and unchanged; the host's handler (<c>Modify.SaveToMyConstructions</c>) asks for a name and saves a
    /// NEW construction - new Guid, materials embedded, provenance "Constructions editor" - to My constructions. Neither the editor's library nor any
    /// model is touched, so there is no Undo step; a construction that cannot be saved says why instead of asking for a name.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserConstructionEditorTests : IDisposable
    {
        private readonly string directory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary library;

        public UserConstructionEditorTests()
        {
            library = UserConstructionFixture.Library(directory);
        }

        public void Dispose()
        {
            UserConstructionFixture.Delete(directory);
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        // ---- The host's handler ---------------------------------------------------------------------------------------------

        [Fact]
        public void Saving_an_authored_construction_asks_for_a_name_and_saves_a_new_one_with_the_editor_provenance_and_its_materials()
        {
            Construction authored = UserConstructionFixture.Wall("Authored", 0.13);
            authored.SetValue(ConstructionParameter.DefaultPanelType, "Wall");
            List<string> reports = new List<string>();
            UserConstructionSaveSubject asked = null;
            Func<string, string> rule = null;

            UserConstructionSaveResult result = Modify.SaveToMyConstructions(authored, UserConstructionFixture.Materials(), "Project X", library, (subject, problem) => { asked = subject; rule = problem; return "  My authored wall "; }, reports.Add);

            Assert.True(result.Succeeded, result.Error);
            Assert.Equal("My authored wall", result.Saved.Name);
            Assert.NotEqual(authored.Guid, result.Saved.Guid);
            Assert.Equal(new[] { "Saved 'My authored wall' to My constructions." }, reports);
            Assert.Equal("the construction Authored", asked.Description);
            Assert.Equal("Authored", asked.SuggestedName);
            Assert.Null(rule("free"));
            Assert.NotNull(rule(""));

            Construction saved = library.Read().Constructions.Single();
            UserConstructionProvenance provenance = UserConstructionProvenance.FromConstruction(saved);
            Assert.Equal(UserConstructionOrigin.ConstructionEditor, provenance.SavedFrom);
            Assert.Equal("Authored", provenance.BasedOnName);
            Assert.Equal(authored.Guid, provenance.BasedOnGuid);
            Assert.Equal("Project X", provenance.OriginModelName);
            Assert.True(double.IsNaN(provenance.ThermalTransmittance));
            Assert.True(saved.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelType));
            Assert.Equal("Wall", panelType);
            Assert.Equal(4, library.Read().ConstructionManager.MaterialLibrary.GetMaterials().Count);
        }

        [Fact]
        public void A_cancelled_name_a_transparent_construction_and_a_missing_library_save_nothing_and_a_taken_name_is_refused_by_the_rule()
        {
            List<string> reports = new List<string>();

            Assert.Null(Modify.SaveToMyConstructions(UserConstructionFixture.Wall("W"), UserConstructionFixture.Materials(), "M", library, (subject, problem) => null, reports.Add));
            Assert.Empty(reports);
            Assert.False(File.Exists(library.Path));

            Construction glass = UserConstructionFixture.Glass(out MaterialLibrary glassMaterials);
            int prompts = 0;
            Assert.Null(Modify.SaveToMyConstructions(glass, glassMaterials, "M", library, (subject, problem) => { prompts++; return "x"; }, reports.Add));
            Assert.Contains("only opaque constructions", Assert.Single(reports));
            Assert.Equal(0, prompts);
            Assert.False(File.Exists(library.Path));

            reports.Clear();
            Assert.Null(Modify.SaveToMyConstructions(UserConstructionFixture.Wall("W"), UserConstructionFixture.Materials(), "M", null, (subject, problem) => "x", reports.Add));
            Assert.Contains("no My constructions library", Assert.Single(reports));

            reports.Clear();
            Assert.True(Modify.SaveToMyConstructions(UserConstructionFixture.Wall("W"), UserConstructionFixture.Materials(), "M", library, (subject, problem) => "Taken", reports.Add).Succeeded);
            Func<string, string> rule = null;
            Modify.SaveToMyConstructions(UserConstructionFixture.Wall("W"), UserConstructionFixture.Materials(), "M", library, (subject, problem) => { rule = problem; return null; }, reports.Add);
            Assert.Contains("already in My constructions", rule(" taken "));

            // The suggestion for a second save of the same construction is made unique.
            UserConstructionSaveSubject suggested = null;
            Modify.SaveToMyConstructions(UserConstructionFixture.Wall("Taken"), UserConstructionFixture.Materials(), "M", library, (subject, problem) => { suggested = subject; return null; }, reports.Add);
            Assert.Equal("Taken 2", suggested.SuggestedName);

            // A save the library refuses after the prompt (the name was taken meanwhile) is reported and nothing is added.
            reports.Clear();
            UserConstructionSaveResult refused = Modify.SaveToMyConstructions(UserConstructionFixture.Wall("W"), UserConstructionFixture.Materials(), "M", library, (subject, problem) => "TAKEN", reports.Add);
            Assert.False(refused.Succeeded);
            Assert.Contains("already in My constructions", Assert.Single(reports));
            Assert.Single(library.Read().Constructions);
        }

        // ---- The classic window ----------------------------------------------------------------------------------------------

        private static ConstructionLibraryWindow Show(Construction[] constructions, MaterialLibrary materials)
        {
            ConstructionLibrary constructionLibrary = new ConstructionLibrary("Model");
            foreach (Construction construction in constructions)
            {
                constructionLibrary.Add(construction);
            }

            ConstructionLibraryWindow window = new ConstructionLibraryWindow(materials, constructionLibrary) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            return window;
        }

        private static Button SaveButton(ConstructionLibraryWindow window) => (Button)window.FindName("Button_SaveToMyConstructions");

        [WpfFact]
        public void The_button_is_shown_only_while_a_host_handles_it_enabled_for_one_selected_construction_and_raises_the_event_without_changing_the_window()
        {
            Construction a = UserConstructionFixture.Wall("A", 0.13);
            Construction b = UserConstructionFixture.Wall("B", 0.2);
            ConstructionLibraryWindow window = Show(new[] { a, b }, UserConstructionFixture.Materials());
            try
            {
                Button button = SaveButton(window);
                Assert.Equal("Save to My constructions...", button.Content);
                Assert.Equal(Visibility.Collapsed, button.Visibility);

                List<SaveToMyConstructionsRequestedEventArgs> raised = new List<SaveToMyConstructionsRequestedEventArgs>();
                EventHandler<SaveToMyConstructionsRequestedEventArgs> handler = (sender, e) => raised.Add(e);
                window.SaveToMyConstructionsRequested += handler;
                Flush();
                Assert.Equal(Visibility.Visible, button.Visibility);
                Assert.False(button.IsEnabled);   // nothing selected

                DataGrid grid = (DataGrid)window.FindName("DataGrid_Constructions");
                grid.SelectedItem = grid.Items[0];
                Flush();
                Assert.True(button.IsEnabled);

                string before = window.ConstructionLibrary.ToJsonObject().ToJsonString();
                ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
                Flush();

                SaveToMyConstructionsRequestedEventArgs e = Assert.Single(raised);
                Assert.Equal("A", e.Construction.Name);
                Assert.Equal(a.Guid, e.Construction.Guid);
                Assert.NotNull(e.MaterialLibrary.GetMaterial(UValueFixture.Wool));
                Assert.True(window.IsVisible);   // the window stays open
                Assert.Equal(before, window.ConstructionLibrary.ToJsonObject().ToJsonString());

                // Two selected: not one construction, so not offered.
                window.MultiSelect = true;
                grid.SelectAll();
                Flush();
                Assert.False(button.IsEnabled);

                window.SaveToMyConstructionsRequested -= handler;
                Flush();
                Assert.Equal(Visibility.Collapsed, button.Visibility);
            }
            finally
            {
                window.Close();
            }
        }

        [WpfFact]
        public void The_editor_hands_the_selected_construction_to_the_host_which_saves_it_and_the_editors_own_library_is_not_changed()
        {
            Construction a = UserConstructionFixture.Wall("A", 0.13);
            ConstructionLibraryWindow window = Show(new[] { a }, UserConstructionFixture.Materials());
            try
            {
                List<string> reports = new List<string>();
                window.SaveToMyConstructionsRequested += (sender, e) =>
                {
                    e.Handled = true;
                    Modify.SaveToMyConstructions(e.Construction, e.MaterialLibrary, "Project", library, (subject, problem) => "A for later", reports.Add);
                };

                DataGrid grid = (DataGrid)window.FindName("DataGrid_Constructions");
                grid.SelectedItem = grid.Items[0];
                Flush();
                string before = window.ConstructionLibrary.ToJsonObject().ToJsonString();
                ((IInvokeProvider)new ButtonAutomationPeer(SaveButton(window)).GetPattern(PatternInterface.Invoke)).Invoke();
                Flush();

                Assert.Equal(new[] { "Saved 'A for later' to My constructions." }, reports);
                Assert.Equal("A for later", library.Read().Constructions.Single().Name);
                Assert.NotEqual(a.Guid, library.Read().Constructions.Single().Guid);
                Assert.Equal(before, window.ConstructionLibrary.ToJsonObject().ToJsonString());
                Assert.Single(window.GetConstructions(false));
            }
            finally
            {
                window.Close();
            }
        }
    }
}
