// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR6 (final UI refinement): what the completed My library workflows say. A result that says why nothing happened is an ERROR (not a
    /// confirmation) and a result does not outlive the entry it was about; the details pane tells a person what to do instead of staying blank; the
    /// build-up numbers say they are millimetres. Nothing here changes what is stored, archived or applied.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserLibraryAcceptanceCleanupTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary glazing;
        private readonly string constructionDirectory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary constructions;

        public UserLibraryAcceptanceCleanupTests()
        {
            glazing = BuilderFixture.Library(directory);
            constructions = UserConstructionFixture.Library(constructionDirectory);
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

            UserConstructionFixture.Delete(constructionDirectory);
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private ApertureConstruction SaveGlazing(string name)
        {
            UserGlazingSaveResult result = glazing.Save(BuilderFixture.Double(name), new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        private Construction SaveConstruction(string name)
        {
            UserConstructionSaveResult result = constructions.Save(UserConstructionFixture.Wall("Source"), UserConstructionFixture.Materials(), name, UserConstructionFixture.Provenance(), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        // ---- Glazing systems tab -----------------------------------------------------------------------------------------------

        [Fact]
        public void A_glazing_Remove_the_library_refuses_is_an_error_and_a_result_does_not_outlive_the_selection()
        {
            SaveGlazing("Alpha");
            SaveGlazing("Beta");
            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(glazing))
            {
                UserLibraryEntryRow alpha = viewModel.Rows.Single(x => x.Name == "Alpha");
                UserLibraryEntryRow beta = viewModel.Rows.Single(x => x.Name == "Beta");

                // A removal that went through is a confirmation.
                viewModel.SelectedRow = beta;
                Assert.True(viewModel.Remove(text => true));
                Assert.False(viewModel.MessageIsError);
                Assert.Contains("Removed 'Beta'", viewModel.Message);

                // The entry was taken out somewhere else: the library refuses and the message says so as an error.
                Assert.True(glazing.Remove(alpha.Guid).Succeeded);
                Assert.False(viewModel.Remove(text => true, alpha));
                Assert.True(viewModel.MessageIsError);
                Assert.False(string.IsNullOrEmpty(viewModel.Message));

                // Choosing another entry (here: after a new one is saved) leaves no result about the old one on screen.
                SaveGlazing("Gamma");
                Assert.True(viewModel.MessageIsError);
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Name == "Gamma");
                Assert.Equal(string.Empty, viewModel.Message);
                Assert.False(viewModel.MessageIsError);
            }
        }

        [Fact]
        public void The_glazing_details_tell_a_person_what_to_do_while_nothing_is_selected_and_say_the_numbers_are_millimetres()
        {
            using (UserLibraryViewModel viewModel = new UserLibraryViewModel(glazing))
            {
                // An empty library has its own empty state: no second hint.
                Assert.False(viewModel.ShowDetailsPlaceholder);
                Assert.Equal(string.Empty, viewModel.DetailsPlaceholderText);

                SaveGlazing("Alpha");
                Assert.True(viewModel.ShowDetailsPlaceholder);
                Assert.Contains("Select a system", viewModel.DetailsPlaceholderText);

                viewModel.SelectedRow = viewModel.Rows[0];
                Assert.False(viewModel.ShowDetailsPlaceholder);

                string details = viewModel.DetailsText;
                Assert.Contains("Pane (thickness in mm): ", details);
                Assert.Contains("Frame layers (thickness in mm): ", details);
                Assert.DoesNotContain(Environment.NewLine + "Frame: " + viewModel.SelectedRow.FrameText, details);
            }
        }

        // ---- Constructions tab -------------------------------------------------------------------------------------------------

        [Fact]
        public void A_construction_Remove_the_library_refuses_is_an_error_and_a_result_does_not_outlive_the_selection()
        {
            SaveConstruction("Alpha");
            SaveConstruction("Beta");
            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(constructions))
            {
                UserConstructionEntryRow alpha = viewModel.Rows.Single(x => x.Name == "Alpha");
                UserConstructionEntryRow beta = viewModel.Rows.Single(x => x.Name == "Beta");

                viewModel.SelectedRow = beta;
                Assert.True(viewModel.Remove(text => true));
                Assert.False(viewModel.MessageIsError);
                Assert.Contains("Removed 'Beta'", viewModel.Message);

                Assert.True(constructions.Remove(alpha.Guid).Succeeded);
                Assert.False(viewModel.Remove(text => true, alpha));
                Assert.True(viewModel.MessageIsError);
                Assert.False(string.IsNullOrEmpty(viewModel.Message));

                SaveConstruction("Gamma");
                viewModel.SelectedRow = viewModel.Rows.Single(x => x.Name == "Gamma");
                Assert.Equal(string.Empty, viewModel.Message);
                Assert.False(viewModel.MessageIsError);
            }
        }

        [Fact]
        public void The_construction_details_tell_a_person_what_to_do_while_nothing_is_selected_and_say_the_numbers_are_millimetres()
        {
            using (UserConstructionLibraryViewModel viewModel = new UserConstructionLibraryViewModel(constructions))
            {
                Assert.False(viewModel.ShowDetailsPlaceholder);

                SaveConstruction("Alpha");
                Assert.True(viewModel.ShowDetailsPlaceholder);
                Assert.Contains("Select a construction", viewModel.DetailsPlaceholderText);

                viewModel.SelectedRow = viewModel.Rows[0];
                Assert.False(viewModel.ShowDetailsPlaceholder);
                Assert.Contains("Build-up (thickness in mm): ", viewModel.DetailsText);
            }
        }

        // ---- The real window -----------------------------------------------------------------------------------------------------

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
            return Descendants<T>(root).FirstOrDefault(x => System.Windows.Automation.AutomationProperties.GetAutomationId(x) == id);
        }

        [WpfFact]
        public void The_window_shows_a_refused_result_as_an_error_and_a_hint_in_the_empty_details_pane_on_both_tabs()
        {
            SaveGlazing("Alpha");
            SaveGlazing("Beta");
            SaveConstruction("One");
            SaveConstruction("Two");
            UserLibraryViewModel viewModel = new UserLibraryViewModel(glazing) { Constructions = new UserConstructionLibraryViewModel(constructions) };
            UserLibraryWindow window = new UserLibraryWindow(viewModel) { Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();
            try
            {
                TextBlock danger = ById<TextBlock>(window, "textBlock_RenameError");
                Brush dangerBrush = danger.Foreground;
                TextBlock message = ById<TextBlock>(window, "textBlock_Message");
                TextBlock hint = ById<TextBlock>(window, "textBlock_DetailsPlaceholder");
                Assert.True(hint.IsVisible);
                Assert.Contains("Select a system", hint.Text);

                UserLibraryEntryRow alpha = viewModel.Rows.Single(x => x.Name == "Alpha");
                UserLibraryEntryRow beta = viewModel.Rows.Single(x => x.Name == "Beta");
                viewModel.SelectedRow = beta;
                Flush();
                Assert.False(hint.IsVisible);

                Assert.True(viewModel.Remove(text => true));
                Flush();
                Assert.NotEqual(((SolidColorBrush)dangerBrush).Color, ((SolidColorBrush)message.Foreground).Color);
                Assert.True(hint.IsVisible);

                Assert.True(glazing.Remove(alpha.Guid).Succeeded);
                Assert.False(viewModel.Remove(text => true, alpha));
                Flush();
                Assert.Equal(((SolidColorBrush)dangerBrush).Color, ((SolidColorBrush)message.Foreground).Color);
                Assert.Equal(viewModel.Message, message.Text);

                // The Constructions tab: the same.
                TabControl tabs = ById<TabControl>(window, "tabControl_Library");
                tabs.SelectedItem = tabs.Items[1];
                Flush();
                TextBlock constructionMessage = ById<TextBlock>(window, "textBlock_ConstructionsMessage");
                TextBlock constructionHint = ById<TextBlock>(window, "textBlock_ConstructionDetailsPlaceholder");
                Assert.True(constructionHint.IsVisible);
                Assert.Contains("Select a construction", constructionHint.Text);

                UserConstructionEntryRow one = viewModel.Constructions.Rows.Single(x => x.Name == "One");
                Assert.True(constructions.Remove(one.Guid).Succeeded);
                Assert.False(viewModel.Constructions.Remove(text => true, one));
                Flush();
                Assert.Equal(((SolidColorBrush)dangerBrush).Color, ((SolidColorBrush)constructionMessage.Foreground).Color);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
