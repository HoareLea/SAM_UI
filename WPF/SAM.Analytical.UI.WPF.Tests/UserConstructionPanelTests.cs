// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// PR4 in the Thermal Performance panel as a person meets it: <b>Save to My constructions…</b> on an opaque row saves whichever construction applies -
    /// the generated variant (before, or without, an Apply), the chosen alternative, or the current construction - as a NEW construction after asking for
    /// a name, from the button or from an alternative's context menu. Saving is never a change of the model: it starts no edit, pins no scope, adds no
    /// pending change (a pending one stays exactly as it was), changes no model JSON and adds no Undo step; Rename and Remove of what was saved do not either.
    /// Choosing a saved construction and pressing Apply is the existing construction change: one model change, one Undo, back to the baseline. The real
    /// XAML runs over the fake calculations and temporary libraries; nothing reads the user's files.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserConstructionPanelTests : IDisposable
    {
        private readonly string directory = UserConstructionFixture.TempDirectory();
        private readonly UserConstructionLibrary library;

        public UserConstructionPanelTests()
        {
            library = UserConstructionFixture.Library(directory);
        }

        public void Dispose()
        {
            UserConstructionFixture.Delete(directory);
        }

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private sealed class Host : IDisposable
        {
            public ThermalParts Parts;
            public UIAnalyticalModel Ui;
            public ThermalPerformanceControl Control;
            public System.Windows.Window Window;
            public string Before;
            public int Modified;
            public int History;
            public List<Guid> Selected = new List<Guid>();
            public List<UserConstructionSaveSubject> Prompted = new List<UserConstructionSaveSubject>();
            public List<Func<string, string>> Rules = new List<Func<string, string>>();

            public ThermalRowEditor Wall => Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.ConstructionGuid == Parts.Wall.Guid).Editor;

            public ThermalRowEditor Windows => Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;

            public void Dispose()
            {
                Window.Close();
            }
        }

        private Host Open(string name = "My name", bool selectWindow = true)
        {
            Host host = new Host() { Parts = ThermalFixture.Build() };
            host.Ui = new UIAnalyticalModel(host.Parts.Model);
            host.Ui.Modified += (sender, e) => host.Modified++;
            host.Ui.HistoryChanged += (sender, e) => host.History++;
            host.Before = Json(host.Ui.JSAMObject);

            host.Control = new ThermalPerformanceControl(UserConstructionFixture.Services(library));
            host.Control.Applier = set => Modify.ApplyThermalChange(host.Ui, set, x => { }, Tas);
            host.Control.PromptConstructionName = (subject, rule) =>
            {
                host.Prompted.Add(subject);
                host.Rules.Add(rule);
                return name;
            };

            host.Selected.AddRange(new[] { 0, 1, 2 }.Select(x => host.Parts.WallPanels[x]));
            if (selectWindow)
            {
                host.Selected.Add(host.Parts.Windows[0]);
            }

            host.Ui.Modified += (sender, e) => Refresh(host, true);
            host.Window = new System.Windows.Window { Content = host.Control, Left = 0, Top = 0, Width = 380, Height = 900, ShowActivated = false };
            host.Window.Show();
            Refresh(host, false);
            return host;
        }

        private static void Refresh(Host host, bool modelChanged)
        {
            AnalyticalModel model = host.Ui.JSAMObject;
            List<SAMObject> selected = new List<SAMObject>();
            selected.AddRange(model.AdjacencyCluster.GetPanels().Where(x => host.Selected.Contains(x.Guid)));
            selected.AddRange(model.AdjacencyCluster.GetApertures().Where(x => host.Selected.Contains(x.Guid)));
            host.Control.Update(model, selected, modelChanged);
            Flush();
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

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

        private static T ById<T>(DependencyObject root, string id, object dataContext = null) where T : FrameworkElement
        {
            return Descendants<T>(root).FirstOrDefault(x => AutomationProperties.GetAutomationId(x) == id && (dataContext == null || ReferenceEquals(x.DataContext, dataContext)));
        }

        private static void Press(Button button)
        {
            Assert.True(button.IsEnabled, button.Content + " is disabled");
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();
        }

        private static void Type(Host host, string target)
        {
            TextBox textBox = Descendants<TextBox>(host.Control).First(x => AutomationProperties.GetAutomationId(x) == "textBox_Target" && AutomationProperties.GetName(x) == "Target U " + host.Parts.Wall.Name);
            textBox.Text = target;
            Flush();
            host.Wall.UValue.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            host.Wall.Alternatives?.Idle().Wait(TimeSpan.FromSeconds(10));
            Flush();
        }

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        private static void AssertModelUntouched(Host host)
        {
            Assert.Equal(host.Before, Json(host.Ui.JSAMObject));
            Assert.Equal(0, host.Modified);
            Assert.Equal(0, host.History);
            Assert.False(host.Ui.CanUndo);
        }

        private Construction Saved(string name)
        {
            return library.Read().Constructions.Single(x => x.Name == name);
        }

        // ---- The button ------------------------------------------------------------------------------------------------------

        [WpfFact]
        public void The_button_is_on_an_opaque_row_only_and_saving_the_current_construction_asks_for_a_name_starts_no_edit_and_changes_nothing_in_the_model()
        {
            using (Host host = Open("Current copy"))
            {
                Button button = ById<Button>(host.Control, "button_SaveConstruction", host.Wall);
                Assert.NotNull(button);
                Assert.True(button.IsVisible);
                Assert.Equal("Save to My constructions…", button.Content);
                Assert.Contains("Saves the current construction SIM_EXT_SLD to My constructions", button.ToolTip.ToString());
                Assert.Contains("The model is not changed", button.ToolTip.ToString());
                Assert.False(ById<Button>(host.Control, "button_SaveConstruction", host.Windows)?.IsVisible ?? false);

                Assert.False(host.Wall.IsEdited);
                Press(button);

                // A name was asked for, for the current construction, with the library's rule and a suggestion.
                UserConstructionSaveSubject subject = Assert.Single(host.Prompted);
                Assert.Contains("the current construction SIM_EXT_SLD", subject.Description);
                Assert.Equal("SIM_EXT_SLD", subject.SuggestedName);
                Assert.NotNull(host.Rules[0](string.Empty));
                Assert.Null(host.Rules[0]("anything else"));

                Construction saved = Saved("Current copy");
                Assert.NotEqual(host.Parts.Wall.Guid, saved.Guid);
                Assert.Equal(host.Parts.Wall.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness), saved.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness));
                UserConstructionProvenance provenance = UserConstructionProvenance.FromConstruction(saved);
                Assert.Equal(UserConstructionOrigin.Model, provenance.SavedFrom);
                Assert.Equal("SIM_EXT_SLD", provenance.BasedOnName);
                Assert.Equal(host.Parts.Wall.Guid, provenance.BasedOnGuid);
                Assert.Equal("Mixed", provenance.OriginModelName);
                Assert.Equal("Horizontal", provenance.HeatFlowDirection);
                Assert.Contains("WallExternal", provenance.HeatFlowBasis);
                Assert.Equal(4, library.Read().ConstructionManager.MaterialLibrary.GetMaterials().Count);

                Assert.Equal("Saved 'Current copy' to My constructions.", ById<TextBlock>(host.Control, "textBlock_UserConstructionMessage", host.Wall).Text);

                // Not an edit and not a pending change.
                Assert.False(host.Wall.IsEdited);
                Assert.False(host.Control.ViewModel.Session.IsPending);
                Assert.Equal(0, host.Control.ViewModel.Session.ChangeCount);
                Assert.NotEqual(Visibility.Visible, ((StackPanel)host.Control.FindName("stackPanel_Pending")).Visibility);
                AssertModelUntouched(host);
            }
        }

        [WpfFact]
        public void Cancelling_the_name_saves_nothing_and_a_construction_that_cannot_be_saved_says_why_without_asking_for_a_name()
        {
            using (Host host = Open())
            {
                host.Control.PromptConstructionName = (subject, rule) => null;
                Press(ById<Button>(host.Control, "button_SaveConstruction", host.Wall));
                Assert.False(File.Exists(library.Path));
                Assert.False(host.Wall.HasUserConstructionMessage);

                // An opaque row of a construction whose material is not in the library cannot be saved: the reason is shown and no name is asked for.
                int prompts = 0;
                host.Control.PromptConstructionName = (subject, rule) => { prompts++; return "x"; };
                UserConstructionSaveSubject missing = new UserConstructionSaveSubject(host.Parts.Wall, new MaterialLibrary("Empty"), "the wall", "Wall", new UserConstructionProvenance());
                Assert.Contains("is not in the material library", missing.Rejection);
                host.Wall.SaveToMyConstructions(missing, "x");
                Assert.Contains("is not in the material library", host.Wall.UserConstructionMessage);
                Assert.Equal(0, prompts);
                Assert.False(File.Exists(library.Path));
                AssertModelUntouched(host);
            }
        }

        [WpfFact]
        public void The_name_rule_is_the_librarys_and_a_taken_name_is_refused_with_nothing_written()
        {
            using (Host host = Open("Taken"))
            {
                Press(ById<Button>(host.Control, "button_SaveConstruction", host.Wall));
                Assert.Equal("Taken", Saved("Taken").Name);

                Assert.Contains("already in My constructions", host.Wall.MyConstructionsNameProblem(" TAKEN "));
                Assert.Contains("needs a name", host.Wall.MyConstructionsNameProblem("  "));
                Assert.Null(host.Wall.MyConstructionsNameProblem("Free"));

                // The suggestion is made unique among the saved ones.
                UserConstructionSaveSubject subject = host.Wall.CreateSaveSubject();
                Assert.Equal("SIM_EXT_SLD", subject.SuggestedName);
                library.Save(host.Parts.Wall, host.Parts.Model.MaterialLibrary, "SIM_EXT_SLD", UserConstructionFixture.Provenance());
                Assert.Equal("SIM_EXT_SLD 2", host.Wall.CreateSaveSubject().SuggestedName);

                // A save that the library refuses (a name taken since the prompt) says so in the row.
                UserConstructionSaveResult refused = host.Wall.SaveToMyConstructions(subject, "taken");
                Assert.False(refused.Succeeded);
                Assert.Contains("already in My constructions", host.Wall.UserConstructionMessage);
                Assert.Equal(2, library.Read().Constructions.Count);
                AssertModelUntouched(host);
            }
        }

        // ---- The generated variant ------------------------------------------------------------------------------------------

        [WpfFact]
        public void The_generated_variant_is_saved_before_Apply_from_the_same_query_without_touching_the_pending_change_and_Apply_still_works()
        {
            using (Host host = Open("Variant 30"))
            {
                Type(host, "0.30");
                Assert.True(host.Wall.HasRequest);
                Assert.Equal(1, host.Control.ViewModel.Session.ChangeCount);
                Assert.Contains("Saves the generated variant", host.Wall.SaveToMyConstructionsText);
                string beforeSave = Json(host.Ui.JSAMObject);

                Press(ById<Button>(host.Control, "button_SaveConstruction", host.Wall));

                UserConstructionSaveSubject subject = Assert.Single(host.Prompted);
                Assert.Contains("the generated variant SIM_EXT_SLD U0.30", subject.Description);
                Assert.Equal("SIM_EXT_SLD U0.30", subject.SuggestedName);

                // The saved construction is what the query makes for this evaluation (and what Apply will make).
                Construction saved = Saved("Variant 30");
                UValueEvaluation evaluation = host.Wall.UValue.Evaluation;
                ProposedConstructionResult proposed = Query.ProposedConstruction(host.Parts.Wall, host.Parts.Model.MaterialLibrary, evaluation.LayerIndex, evaluation.Thickness, UValueApplyMode.NewConstruction, null, evaluation.CalculatedThermalTransmittance, host.Parts.Model.AdjacencyCluster.GetConstructions());
                Assert.Equal(proposed.Construction.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness), saved.ConstructionLayers.Select(x => x.Name + "|" + x.Thickness));
                Assert.Equal(0.067, saved.ConstructionLayers[UValueFixture.WoolIndex].Thickness, 6);
                Assert.Equal("I01_Mineral Wool_0.067m", saved.ConstructionLayers[UValueFixture.WoolIndex].Name);
                Assert.NotNull(library.Read().ConstructionManager.MaterialLibrary.GetMaterial("I01_Mineral Wool_0.067m"));
                Assert.NotEqual(proposed.Construction.Guid, saved.Guid);

                UserConstructionProvenance provenance = UserConstructionProvenance.FromConstruction(saved);
                Assert.Equal(UserConstructionOrigin.GeneratedVariant, provenance.SavedFrom);
                Assert.Equal(host.Parts.Wall.Guid, provenance.BasedOnGuid);
                Assert.Equal(0.3, provenance.TargetThermalTransmittance, 6);
                Assert.Equal(evaluation.CalculatedThermalTransmittance, provenance.ThermalTransmittance, 6);
                Assert.Contains("layer 3", provenance.Route);
                Assert.Contains("Horizontal", provenance.HeatFlowBasis);

                // Nothing about the model or the pending change moved; the variant was never applied.
                Assert.Equal(beforeSave, Json(host.Ui.JSAMObject));
                Assert.True(host.Wall.HasRequest);
                Assert.Equal(1, host.Control.ViewModel.Session.ChangeCount);
                Assert.True(ById<Button>(host.Control, "button_Apply").IsEnabled);
                AssertModelUntouched(host);

                // Apply is still the one change, with its own new construction (the saved one is a separate copy).
                Press(ById<Button>(host.Control, "button_Apply"));
                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.History);
                Assert.DoesNotContain(host.Ui.JSAMObject.AdjacencyCluster.GetConstructions(), x => x.Guid == saved.Guid);
                AdjacencyCluster applied = host.Ui.JSAMObject.AdjacencyCluster;
                Assert.Equal(12, applied.GetPanels().Count(x => applied.GetConstructions().Any(c => c.Guid == x.TypeGuid && c.Name == "SIM_EXT_SLD U0.30")));
            }
        }

        [WpfFact]
        public void A_generated_variant_that_was_not_reached_is_not_saved_the_current_construction_is_offered_instead()
        {
            using (Host host = Open())
            {
                Type(host, "0.0001");   // below what the thickness range can reach
                Assert.False(host.Wall.UValue.ApplyEnabled);

                UserConstructionSaveSubject subject = host.Wall.CreateSaveSubject();
                Assert.True(subject.IsAvailable);
                Assert.Contains("the current construction", subject.Description);
                Assert.Equal(UserConstructionOrigin.Model, subject.Provenance.SavedFrom);

                UserConstructionSaveSubject generated = host.Wall.CreateSaveSubject(host.Wall.AlternativeRows.Single(x => x.IsGenerated));
                Assert.False(generated.IsAvailable);
                Assert.Contains("no U-value yet", generated.Error);
                Assert.False(File.Exists(library.Path));
            }
        }

        // ---- An alternative ------------------------------------------------------------------------------------------------

        [WpfFact]
        public void A_chosen_alternative_is_saved_as_a_new_construction_with_its_provenance_the_choice_stays_and_the_list_shows_the_copy_as_My_constructions()
        {
            using (Host host = Open("LIB_THICK copy"))
            {
                Type(host, "0.18");
                ConstructionAlternativeRow chosen = host.Wall.AlternativeRows.Single(x => x.Guid == AlternativesFixture.LibraryThickGuid);
                host.Wall.SelectedAlternative = chosen;
                Flush();
                Assert.True(host.Wall.AlternativeChosen);
                Assert.Contains("Saves LIB_THICK (Library) to My constructions", host.Wall.SaveToMyConstructionsText);

                Press(ById<Button>(host.Control, "button_SaveConstruction", host.Wall));

                Construction saved = Saved("LIB_THICK copy");
                Assert.NotEqual(AlternativesFixture.LibraryThickGuid, saved.Guid);
                UserConstructionProvenance provenance = UserConstructionProvenance.FromConstruction(saved);
                Assert.Equal(UserConstructionOrigin.DefaultLibrary, provenance.SavedFrom);
                Assert.Equal("LIB_THICK", provenance.BasedOnName);
                Assert.Equal(AlternativesFixture.LibraryThickGuid, provenance.BasedOnGuid);
                Assert.Equal(UValueFixture.U(0.13), provenance.ThermalTransmittance, 6);
                Assert.Contains("thermal transmittance", provenance.Route);

                // The choice is as it was; the list followed the library and offers the copy as My constructions.
                Assert.True(host.Wall.AlternativeChosen);
                Assert.Equal(AlternativesFixture.LibraryThickGuid, host.Wall.SelectedAlternative.Guid);
                Assert.Equal(1, host.Control.ViewModel.Session.ChangeCount);
                ConstructionAlternativeRow copy = Assert.Single(host.Wall.AlternativeRows, x => x.Guid == saved.Guid);
                Assert.Equal(ConstructionAlternativeKind.User, copy.Kind);
                Assert.Equal("My constructions", copy.KindText);
                AssertModelUntouched(host);
            }
        }

        [WpfFact]
        public void The_context_menu_of_an_alternative_saves_that_construction_without_choosing_it_and_a_right_click_does_not_select_it()
        {
            using (Host host = Open("From the menu"))
            {
                Type(host, "0.18");
                ListBox list = ById<ListBox>(host.Control, "listBox_Alternatives", host.Wall);
                ConstructionAlternativeRow row = host.Wall.AlternativeRows.Single(x => x.Guid == AlternativesFixture.LibraryAerogelGuid);
                Assert.True(host.Wall.SelectedAlternative.IsGenerated);

                list.ScrollIntoView(row);
                list.UpdateLayout();
                Flush();
                ListBoxItem item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(row);
                Assert.NotNull(item);
                item.ApplyTemplate();
                item.UpdateLayout();
                Flush();
                StackPanel content = Descendants<StackPanel>(host.Control).First(x => ReferenceEquals(x.DataContext, row) && x.ContextMenu != null);

                // The press that opens the menu does not choose the alternative.
                TextBlock text = Descendants<TextBlock>(host.Control).First(x => ReferenceEquals(x.DataContext, row));
                MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = text };
                text.RaiseEvent(args);
                args.RoutedEvent = Mouse.MouseDownEvent;
                text.RaiseEvent(args);
                Flush();
                Assert.True(host.Wall.SelectedAlternative.IsGenerated);
                Assert.False(host.Wall.AlternativeChosen);

                content.ContextMenu.PlacementTarget = content;
                content.ContextMenu.IsOpen = true;
                Flush();
                MenuItem menuItem = content.ContextMenu.Items.OfType<MenuItem>().Single(x => AutomationProperties.GetAutomationId(x) == "menuItem_SaveAlternative");
                Assert.Equal("Save to My constructions…", menuItem.Header);
                menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Flush();

                UserConstructionSaveSubject subject = Assert.Single(host.Prompted);
                Assert.Contains("the construction LIB_AEROGEL", subject.Description);
                Construction saved = Saved("From the menu");
                Assert.Contains("Aerogel", saved.ConstructionLayers.Select(x => x.Name));
                Assert.NotNull(library.Read().ConstructionManager.MaterialLibrary.GetMaterial("Aerogel"));
                Assert.Equal(AlternativesFixture.LibraryAerogelGuid, UserConstructionProvenance.FromConstruction(saved).BasedOnGuid);

                // The row's choice is untouched: still the generated variant, which is still the pending change.
                Assert.True(host.Wall.SelectedAlternative.IsGenerated);
                Assert.False(host.Wall.AlternativeChosen);
                Assert.Equal(1, host.Control.ViewModel.Session.ChangeCount);
                AssertModelUntouched(host);
            }
        }

        // ---- Library changes never touch the model ----------------------------------------------------------------------------

        [WpfFact]
        public void Saving_renaming_and_removing_with_the_panel_open_change_no_model_json_and_add_no_undo_and_the_list_follows()
        {
            using (Host host = Open("Round trip"))
            {
                Type(host, "0.18");
                Press(ById<Button>(host.Control, "button_SaveConstruction", host.Wall));
                Construction saved = Saved("Round trip");
                AssertModelUntouched(host);

                Assert.True(library.Rename(saved.Guid, "Renamed").Succeeded);
                Flush();
                Assert.Contains(host.Wall.AlternativeRows, x => x.Guid == saved.Guid && x.Name == "Renamed");
                AssertModelUntouched(host);

                Assert.True(library.Remove(saved.Guid).Succeeded);
                Flush();
                Assert.DoesNotContain(host.Wall.AlternativeRows, x => x.Guid == saved.Guid);
                AssertModelUntouched(host);
            }
        }

        [WpfFact]
        public void Choosing_a_saved_construction_and_pressing_Apply_is_one_model_change_and_one_Undo_restores_the_baseline_and_the_report_says_My_constructions()
        {
            Construction mine = library.Save(UserConstructionFixture.Wall("Source", 0.125), UserConstructionFixture.Materials(), "MY_WALL", UserConstructionFixture.Provenance(UserConstructionOrigin.Model, "Source"), new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc)).Saved;

            using (Host host = Open(selectWindow: false))
            {
                Type(host, "0.18");
                ConstructionAlternativeRow row = host.Wall.AlternativeRows.Single(x => x.Guid == mine.Guid);
                Assert.Equal(ConstructionAlternativeKind.User, row.Kind);
                host.Wall.SelectedAlternative = row;
                Flush();
                Assert.True(host.Wall.HasRequest);
                Assert.Contains("MY_WALL (My constructions)", ById<TextBlock>(host.Control, "textBlock_Preview", host.Wall).Text);
                Assert.Contains("It is added to the model with the materials it lacks.", host.Wall.ResultText);

                Press(ById<Button>(host.Control, "button_Apply"));

                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.History);
                Assert.True(host.Ui.CanUndo);
                AnalyticalModel changed = host.Ui.JSAMObject;
                Assert.Equal(12, changed.AdjacencyCluster.GetPanels().Count(x => x.TypeGuid == mine.Guid));
                Construction applied = changed.AdjacencyCluster.GetConstructions().Single(x => x.Guid == mine.Guid);
                Assert.Equal("MY_WALL", applied.Name);
                Assert.NotNull(UserConstructionProvenance.FromConstruction(applied));

                host.Ui.Undo();
                for (int i = 0; i < 200 && Json(host.Ui.JSAMObject) != host.Before; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    Flush();
                }

                // Back to the baseline: the saved construction is not in the model any more, nor its materials, and every panel has its original construction.
                AnalyticalModel restored = host.Ui.JSAMObject;
                AnalyticalModel baseline = host.Parts.Model;
                Assert.Equal(0, restored.AdjacencyCluster.GetPanels().Count(x => x.TypeGuid == mine.Guid));
                Assert.DoesNotContain(restored.AdjacencyCluster.GetConstructions(), x => x.Guid == mine.Guid);
                Assert.Equal(baseline.AdjacencyCluster.GetPanels().Select(x => x.Guid + "|" + x.TypeGuid).OrderBy(x => x), restored.AdjacencyCluster.GetPanels().Select(x => x.Guid + "|" + x.TypeGuid).OrderBy(x => x));
                Assert.Equal(baseline.MaterialLibrary.GetMaterials().Select(x => x.Name).OrderBy(x => x), restored.MaterialLibrary.GetMaterials().Select(x => x.Name).OrderBy(x => x));
                Assert.False(host.Ui.CanUndo);
            }
        }

        // ---- Reports -------------------------------------------------------------------------------------------------------

        [Fact]
        public void The_construction_change_report_names_My_constructions_with_the_guid_and_a_model_or_library_one_keeps_its_wording()
        {
            Construction mine = UserConstructionFixture.Wall("MY_WALL", 0.125);
            AnalyticalModel model = AlternativesFixture.Model(out Construction current, out _, out _, out _);

            string Report(GlazingSourceKind kind, string label)
            {
                SetConstructionRequest request = new SetConstructionRequest()
                {
                    SourceConstructionGuid = current.Guid,
                    Construction = new Construction(mine),
                    MaterialsToAdd = new List<IMaterial>(),
                    Scope = ThermalApplyScope.AllUsing,
                    OldThermalTransmittance = 0.26,
                    NewThermalTransmittance = 0.177,
                    TargetThermalTransmittance = 0.18,
                    HeatFlowDirection = HeatFlowDirection.Horizontal,
                    SourceLabel = label,
                    SourceKind = kind,
                };

                AnalyticalModel changed = Modify.SetConstruction(model, request, out SetConstructionResult result);
                Assert.True(result.Succeeded, result.Error);
                return Query.ConstructionChangeReportText(result, Query.ConstructionCheckSummary(changed, result), null);
            }

            string user = Report(GlazingSourceKind.User, "My constructions");
            Assert.Contains("Source:       My constructions (My constructions)", user);
            Assert.Contains("Guid " + mine.Guid, user);

            Assert.Contains("Default library (default library)", Report(GlazingSourceKind.Library, "Default library"));
            Assert.Contains("(existing model construction)", Report(GlazingSourceKind.Model, "Model"));
            Assert.Contains("database.tcd (added source)", Report(GlazingSourceKind.Loaded, "database.tcd"));
        }
    }
}
