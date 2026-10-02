// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage F1 (<c>documentation/Thermal-StageF-Final-Convergence.md</c>): the candidate-selection abilities that only the classic Set glazing window had - g / light
    /// filters, a comparison against the target, the source toggles - plus an order of the list, now in the glazing <c>Change…</c> list of the Thermal Performance panel.
    /// They are the shared <see cref="GlazingViewModel"/>'s own filters, so the tests assert behaviour: what is listed, what is chosen, that nothing reaches the model
    /// before Apply, and that Apply is still one change and one Undo.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalCandidateFilterTests
    {
        // ---- The order (view-model) ----------------------------------------------------------------------------------

        private static async Task<GlazingViewModel> Ready()
        {
            GlazingViewModel viewModel = GlazingFixture.ViewModel(GlazingFixture.Model());
            await viewModel.InitializeAsync();
            return viewModel;
        }

        [Fact]
        public async Task The_list_is_best_Uw_first_by_default_and_each_order_reorders_it_without_changing_what_is_listed()
        {
            GlazingViewModel viewModel = await Ready();
            Assert.Equal(GlazingSortOrder.OverallU, viewModel.SortOrder);
            Guid[] byUw = { GlazingFixture.MissingMaterialGuid, GlazingFixture.BetterGuid, GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid };
            Assert.Equal(byUw, viewModel.Rows.Select(x => x.Guid).ToArray());
            string count = viewModel.CandidateCountText;

            // g 0.50 / 0.60 / 0.62, light 0.70 / 0.78 / 0.80; the system whose material is missing is not known to be glass (no g, no light): last in those orders.
            viewModel.SortOrder = GlazingSortOrder.GLowest;
            Assert.Equal(new[] { GlazingFixture.BetterGuid, GlazingFixture.CurrentGuid, GlazingFixture.PaneOnlyGuid, GlazingFixture.MissingMaterialGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());

            viewModel.SortOrder = GlazingSortOrder.GHighest;
            Assert.Equal(new[] { GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid, GlazingFixture.BetterGuid, GlazingFixture.MissingMaterialGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());

            viewModel.SortOrder = GlazingSortOrder.LightHighest;
            Assert.Equal(new[] { GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid, GlazingFixture.BetterGuid, GlazingFixture.MissingMaterialGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());

            // Two systems are called "GLZ": the better Uw first.
            viewModel.SortOrder = GlazingSortOrder.Name;
            Assert.Equal(new[] { GlazingFixture.BetterGuid, GlazingFixture.CurrentGuid, GlazingFixture.MissingMaterialGuid, GlazingFixture.PaneOnlyGuid }, viewModel.Rows.Select(x => x.Guid).ToArray());
            Assert.Equal(count, viewModel.CandidateCountText);

            viewModel.SortOrder = GlazingSortOrder.OverallU;
            Assert.Equal(byUw, viewModel.Rows.Select(x => x.Guid).ToArray());
        }

        [Theory]
        [InlineData(GlazingSortOrder.OverallU)]
        [InlineData(GlazingSortOrder.GHighest)]
        [InlineData(GlazingSortOrder.LightHighest)]
        [InlineData(GlazingSortOrder.Name)]
        public async Task The_automatic_choice_against_a_target_is_the_best_Uw_that_meets_it_in_every_order(GlazingSortOrder order)
        {
            GlazingViewModel viewModel = await Ready();
            viewModel.SortOrder = order;

            viewModel.TargetText = "1.35";

            // The one with the missing material is better but cannot be applied: the better "GLZ" is chosen, whatever is listed first.
            Assert.Equal(GlazingFixture.BetterGuid, viewModel.SelectedGuid);
            Assert.True(viewModel.ApplyEnabled);
        }

        [Fact]
        public async Task A_choice_hidden_by_a_filter_is_dropped_and_nothing_is_applied()
        {
            GlazingViewModel viewModel = await Ready();
            viewModel.SelectedGuid = GlazingFixture.BetterGuid;
            Assert.True(viewModel.ApplyEnabled);

            viewModel.MinGText = "0.55";

            Assert.Null(viewModel.SelectedGuid);
            Assert.False(viewModel.ApplyEnabled);
            Assert.Null(viewModel.CreateRequest());
        }

        // ---- In the panel (the real XAML over the fake calculations) ----------------------------------------------------

        private static ThermalTransmittanceCalculationResult Tas(SetGlazingRequest request)
        {
            return new ThermalTransmittanceCalculationResult(request.ApertureConstruction.Guid, "Fake", 0.70, 0.20, 0.45, 0.25, 0.30, 0.50, 0.60, 0.30, new ThermalTransmittances(2, 2, 2, 2, 2, 2, 1.10));
        }

        private sealed class Host
        {
            public ThermalParts Parts;
            public UIAnalyticalModel Ui;
            public ThermalPerformanceControl Control;
            public System.Windows.Window Window;
            public ThermalSourceCatalog Catalog;
            public int Modified;
            public int HistoryChanged;
            public string Json;
            public string Snapshot;
        }

        // Two windows selected; the catalog holds one added source ("good.json", a better window system) when asked.
        private static Host Open(bool addedSource = false)
        {
            Host host = new Host() { Parts = ThermalFixture.Build() };
            host.Ui = new UIAnalyticalModel(host.Parts.Model);
            host.Ui.Modified += (sender, e) => host.Modified++;
            host.Ui.HistoryChanged += (sender, e) => host.HistoryChanged++;

            FakeSourceReader reader = new FakeSourceReader();
            string path = FakeSourceReader.Path("good.json");
            reader.Add(path, GlazingFixture.LoadedGood());
            host.Catalog = reader.Catalog(new InMemoryThermalSourceStore());
            if (addedSource)
            {
                host.Catalog.AddAsync(path).Wait(TimeSpan.FromSeconds(10));
            }

            host.Control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => host.Catalog));
            host.Control.Applier = set => Modify.ApplyThermalChange(host.Ui, set, x => { }, Tas);

            host.Window = new System.Windows.Window { Content = host.Control, Left = 0, Top = 0, Width = 380, Height = 1000, ShowActivated = false };
            host.Window.Show();

            AnalyticalModel model = host.Ui.JSAMObject;
            host.Control.Update(model, model.AdjacencyCluster.GetApertures().Where(x => host.Parts.Windows.Take(2).Contains(x.Guid)).Cast<SAMObject>().ToList());
            host.Json = model.ToJsonObject().ToJsonString();

            // What Undo restores: the model as its history snapshot stores it (SAM's JSON round trip, which drops NaN-valued parameters).
            host.Snapshot = new AnalyticalModel(model.ToJsonObject()).ToJsonObject().ToJsonString();
            Flush();
            return host;
        }

        private static ThermalRowEditor OpenChange(Host host)
        {
            ThermalRowEditor editor = host.Control.ViewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
            Press(Descendants<Button>(host.Control).First(x => Id(x) == "button_Change" && ReferenceEquals(x.DataContext, editor)));
            editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
            Flush();
            return editor;
        }

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        private static string Id(DependencyObject element)
        {
            return System.Windows.Automation.AutomationProperties.GetAutomationId(element);
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

        // The element of the open window row's editor (every row carries the same template).
        private static T Of<T>(Host host, ThermalRowEditor editor, string id) where T : FrameworkElement
        {
            return Descendants<T>(host.Control).FirstOrDefault(x => Id(x) == id && ReferenceEquals(x.DataContext, editor));
        }

        private static void Press(Button button)
        {
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();
        }

        private static void Type(TextBox textBox, string text)
        {
            textBox.Text = text;
            Flush();
        }

        private static void AssertModelUntouched(Host host)
        {
            Assert.Equal(0, host.Modified);
            Assert.Equal(0, host.HistoryChanged);
            Assert.False(host.Ui.CanUndo);
            Assert.Equal(host.Json, host.Ui.JSAMObject.ToJsonObject().ToJsonString());
        }

        [WpfFact]
        public void The_filters_are_folded_away_until_asked_for_so_the_open_list_stays_compact()
        {
            Host host = Open();
            try
            {
                ThermalRowEditor editor = OpenChange(host);

                ToggleButton toggle = Of<ToggleButton>(host, editor, "toggleButton_GlazingFilters");
                FrameworkElement filters = Of<Grid>(host, editor, "grid_GlazingFilters");
                Assert.NotNull(toggle);
                Assert.NotNull(filters);
                Assert.True(toggle.IsVisible);
                Assert.False(filters.IsVisible);
                Assert.True(Of<TextBox>(host, editor, "textBox_GlazingTarget").IsVisible);
                Assert.False(Of<TextBlock>(host, editor, "textBlock_GlazingFilters").IsVisible);

                toggle.IsChecked = true;
                Flush();

                Assert.True(filters.IsVisible);
                foreach (string id in new[] { "textBox_GlazingMinG", "textBox_GlazingMaxG", "textBox_GlazingMinLight" })
                {
                    Assert.True(Of<TextBox>(host, editor, id).IsVisible, id);
                }

                Assert.True(Of<ComboBox>(host, editor, "comboBox_GlazingSort").IsVisible);
                Assert.True(Of<CheckBox>(host, editor, "checkBox_GlazingIncludeLibrary").IsChecked);
                Assert.True(Of<CheckBox>(host, editor, "checkBox_GlazingIncludeAdded").IsChecked);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void G_and_light_filters_narrow_the_list_say_so_and_the_chosen_system_applies_as_one_change_and_one_Undo()
        {
            Host host = Open();
            try
            {
                ThermalRowEditor editor = OpenChange(host);
                Of<ToggleButton>(host, editor, "toggleButton_GlazingFilters").IsChecked = true;
                Flush();
                Assert.Equal("Showing 4 of 4 systems.", Of<TextBlock>(host, editor, "textBlock_CandidateCount").Text);

                // g 0.45 - 0.55 and light 0.65 or more: only the better "GLZ" (g 0.50, light 0.70) passes.
                Type(Of<TextBox>(host, editor, "textBox_GlazingMinG"), "0.45");
                Type(Of<TextBox>(host, editor, "textBox_GlazingMaxG"), "0.55");
                Type(Of<TextBox>(host, editor, "textBox_GlazingMinLight"), "0.65");

                Assert.Equal(new[] { GlazingFixture.BetterGuid }, editor.Candidates.Where(x => x.Passes).Select(x => x.Guid).ToArray());
                Assert.Equal("Showing 1 of 4 systems.", Of<TextBlock>(host, editor, "textBlock_CandidateCount").Text);
                TextBlock filtersText = Of<TextBlock>(host, editor, "textBlock_GlazingFilters");
                Assert.True(filtersText.IsVisible);
                Assert.Equal("Filtered: g 0.45 – 0.55 · light ≥ 0.65", filtersText.Text);

                // The list the person sees is the filtered one (the current system stays as the reference).
                ListBox list = Of<ListBox>(host, editor, "listBox_Candidates");
                Assert.Equal(editor.Candidates.Count, list.Items.Count);
                Assert.DoesNotContain(list.Items.Cast<GlazingCandidateRow>(), x => x.Guid == GlazingFixture.PaneOnlyGuid || x.Guid == GlazingFixture.MissingMaterialGuid);

                // A target: the comparison line; the best Uw that meets it is chosen.
                Type(Of<TextBox>(host, editor, "textBox_GlazingTarget"), "1.35");
                Assert.Equal(GlazingFixture.BetterGuid, editor.SelectedCandidate?.Guid);
                Assert.StartsWith("Target Uw ≤ 1.35: ✓ Meets target (margin +", Of<TextBlock>(host, editor, "textBlock_GlazingComparison").Text);
                AssertModelUntouched(host);

                Press(Descendants<Button>(host.Control).First(x => Id(x) == "button_Apply"));

                Assert.Equal(1, host.Modified);
                Assert.Equal(1, host.HistoryChanged);
                Assert.Equal(host.Parts.Windows.Count, host.Ui.JSAMObject.AdjacencyCluster.GetApertures().Count(x => x.TypeGuid == GlazingFixture.BetterGuid));

                // One Undo restores the model (the restore completes asynchronously).
                Assert.True(host.Ui.Undo());
                for (int i = 0; i < 200 && host.Ui.JSAMObject.AdjacencyCluster.GetApertures().Any(x => x.TypeGuid == GlazingFixture.BetterGuid); i++)
                {
                    Flush();
                    System.Threading.Thread.Sleep(25);
                }

                Assert.Equal(host.Snapshot, host.Ui.JSAMObject.ToJsonObject().ToJsonString());
                Assert.False(host.Ui.CanUndo);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void Sorting_in_the_panel_reorders_the_list_only()
        {
            Host host = Open();
            try
            {
                ThermalRowEditor editor = OpenChange(host);
                Of<ToggleButton>(host, editor, "toggleButton_GlazingFilters").IsChecked = true;
                Flush();
                ComboBox sort = Of<ComboBox>(host, editor, "comboBox_GlazingSort");
                Assert.Equal("Uw, best first", ((ThermalOption)sort.SelectedItem).Display);
                Assert.Equal(new[] { "Uw, best first", "g, lowest first", "g, highest first", "Light, highest first", "Name" }, sort.Items.Cast<ThermalOption>().Select(x => x.Display).ToArray());

                sort.SelectedItem = sort.Items.Cast<ThermalOption>().Single(x => Equals(x.Value, GlazingSortOrder.GHighest));
                Flush();

                ListBox list = Of<ListBox>(host, editor, "listBox_Candidates");
                Assert.Equal(new[] { GlazingFixture.PaneOnlyGuid, GlazingFixture.CurrentGuid, GlazingFixture.BetterGuid, GlazingFixture.MissingMaterialGuid }, list.Items.Cast<GlazingCandidateRow>().Select(x => x.Guid).ToArray());
                Assert.Equal("Showing 4 of 4 systems.", Of<TextBlock>(host, editor, "textBlock_CandidateCount").Text);
                Assert.Null(editor.SelectedCandidate);
                Assert.False(Of<TextBlock>(host, editor, "textBlock_GlazingFilters").IsVisible);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void The_source_toggles_hide_the_default_library_or_the_added_sources_and_a_hidden_choice_is_dropped()
        {
            Host host = Open(addedSource: true);
            try
            {
                ThermalRowEditor editor = OpenChange(host);
                editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                Flush();
                Of<ToggleButton>(host, editor, "toggleButton_GlazingFilters").IsChecked = true;
                Flush();
                Assert.Contains(editor.Candidates, x => x.Guid == GlazingFixture.LoadedGuid);
                Assert.Equal("Showing 5 of 5 systems.", Of<TextBlock>(host, editor, "textBlock_CandidateCount").Text);

                // Choose the added source's system, then hide the added sources: it leaves the list, the choice is dropped, nothing to apply.
                editor.SelectedCandidate = editor.Candidates.Single(x => x.Guid == GlazingFixture.LoadedGuid);
                Flush();
                Assert.True(editor.HasRequest);

                Of<CheckBox>(host, editor, "checkBox_GlazingIncludeAdded").IsChecked = false;
                Flush();
                Assert.DoesNotContain(editor.Candidates, x => x.Guid == GlazingFixture.LoadedGuid);
                Assert.Null(editor.SelectedCandidate);
                Assert.False(editor.HasRequest);
                Assert.False(Descendants<Button>(host.Control).First(x => Id(x) == "button_Apply").IsEnabled);
                Assert.Equal("Filtered: without the added sources", Of<TextBlock>(host, editor, "textBlock_GlazingFilters").Text);

                // Without the default library: only the model's own system (the current one, as the reference) is left.
                Of<CheckBox>(host, editor, "checkBox_GlazingIncludeLibrary").IsChecked = false;
                Flush();
                Assert.Equal(new[] { GlazingFixture.CurrentGuid }, editor.Candidates.Select(x => x.Guid).ToArray());
                Assert.Equal("Filtered: without the default library · without the added sources", Of<TextBlock>(host, editor, "textBlock_GlazingFilters").Text);

                Of<CheckBox>(host, editor, "checkBox_GlazingIncludeLibrary").IsChecked = true;
                Of<CheckBox>(host, editor, "checkBox_GlazingIncludeAdded").IsChecked = true;
                Flush();
                Assert.Equal(5, editor.Candidates.Count);
                Assert.False(Of<TextBlock>(host, editor, "textBlock_GlazingFilters").IsVisible);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [WpfFact]
        public void The_filters_belong_to_the_open_list_and_start_empty_when_it_is_opened_again()
        {
            Host host = Open();
            try
            {
                ThermalRowEditor editor = OpenChange(host);
                editor.GlazingMinGText = "0.55";
                editor.GlazingIncludeLibrary = false;
                editor.GlazingSelectedSort = editor.GlazingSortOptions.Single(x => Equals(x.Value, GlazingSortOrder.Name));
                Assert.True(editor.HasGlazingFilters);

                editor.CloseChange();
                Flush();
                Assert.Equal(string.Empty, editor.GlazingMinGText);
                Assert.Equal(string.Empty, editor.GlazingFiltersText);

                OpenChange(host);
                Assert.Equal(string.Empty, editor.GlazingMinGText);
                Assert.True(editor.GlazingIncludeLibrary);
                Assert.True(editor.GlazingIncludeAdded);
                Assert.Equal(GlazingSortOrder.OverallU, editor.GlazingSelectedSort.Value);
                Assert.Equal(4, editor.Candidates.Count);
                AssertModelUntouched(host);
            }
            finally
            {
                host.Window.Close();
            }
        }

        [Theory]
        [InlineData("abc", "", "")]
        [InlineData("-1", "", "")]
        [InlineData("0.3", "", "Filtered: g ≥ 0.30")]
        [InlineData("", "0.5", "Filtered: g ≤ 0.50")]
        public void The_filter_line_names_only_filters_that_are_in_force(string minG, string maxG, string expected)
        {
            ThermalParts parts = ThermalFixture.Build();
            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()))))
            {
                viewModel.Update(parts.Model, parts.Model.AdjacencyCluster.GetApertures().Where(x => parts.Windows.Contains(x.Guid)).Cast<SAMObject>().ToList());
                ThermalRowEditor editor = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
                Assert.Equal(string.Empty, editor.GlazingFiltersText);

                editor.OpenChange();
                editor.Glazing.LastEvaluationTask.Wait(TimeSpan.FromSeconds(10));
                editor.GlazingMinGText = minG;
                editor.GlazingMaxGText = maxG;

                Assert.Equal(expected, editor.GlazingFiltersText);
                Assert.Equal(expected.Length != 0, editor.HasGlazingFilters);
                Assert.Equal(string.Empty, editor.GlazingComparisonText);
            }
        }
    }
}
