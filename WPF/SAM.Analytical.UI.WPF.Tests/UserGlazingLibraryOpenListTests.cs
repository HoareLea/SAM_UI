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
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// User-library PR1: a Rename or Remove through <see cref="UserGlazingLibrary"/> reaches the Change… list that is OPEN in the Thermal
    /// Performance panel by the library's own <see cref="UserGlazingLibrary.Changed"/>: a renamed system shows its new name and keeps its values (it
    /// is the same Guid, so nothing is calculated again), a removed one leaves the list and a choice of it is reset. Neither touches the model,
    /// its history or the Undo stack. As with the other panel integration tests the list belongs to the thread it was opened on, so these run as
    /// the app does - an STA thread with the WPF dispatcher (<c>[WpfFact]</c>) - and wait by pumping it, never by blocking it.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public sealed class UserGlazingLibraryOpenListTests : IDisposable
    {
        private readonly string directory = BuilderFixture.TempDirectory();
        private readonly UserGlazingLibrary library;
        private readonly FakeGlazingEvaluator evaluator = new FakeGlazingEvaluator();

        public UserGlazingLibraryOpenListTests()
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

        private ThermalEditServices Services()
        {
            return new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => evaluator, () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new FakeSourceReader().Catalog(), () => library);
        }

        private ApertureConstruction Save(GlazingSystemDraft draft)
        {
            UserGlazingSaveResult result = library.Save(draft, new GlazingValues(1.05, 0.52, 0.75, 1.8), new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(result.Succeeded, result.Error);
            return result.Saved;
        }

        private static async Task<ThermalRowEditor> OpenWindow(ThermalPerformanceViewModel viewModel, AnalyticalModel model, ThermalParts parts)
        {
            viewModel.Update(model, new List<SAMObject>() { model.AdjacencyCluster.GetAperture(parts.Windows[0]) });
            ThermalRowEditor window = viewModel.Groups.SelectMany(x => x.Rows).First(x => x.IsAperture).Editor;
            window.OpenChange();
            Task task = window.Glazing.LastEvaluationTask;
            await Pump(() => task.IsCompleted, "the list's calculation");
            await task;
            return window;
        }

        // Waits on the UI thread as the app would: the dispatcher keeps running what is posted to it (a blocking wait would starve it).
        private static async Task Pump(Func<bool> condition, string what)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), "Timed out waiting for " + what);
                await Task.Delay(10);
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }
        }

        private static string Json(AnalyticalModel model) => model.ToJsonObject().ToJsonString();

        [WpfFact]
        public async Task A_rename_reaches_the_open_list_which_shows_the_new_name_with_the_same_values_and_asks_tas_nothing_new()
        {
            ApertureConstruction saved = Save(BuilderFixture.Double("Old name"));
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = await OpenWindow(viewModel, ui.JSAMObject, parts);
                GlazingCandidateRow row = Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal("Old name", row.Name);
                string ug = row.UgText;
                int rows = window.Candidates.Count;
                int asked = evaluator.GuidsRequested;

                Assert.True(library.Rename(saved.Guid, "New name").Succeeded);
                await Pump(() => window.Candidates.Any(x => x.Guid == saved.Guid && x.Name == "New name"), "the renamed system to appear");

                row = Assert.Single(window.Candidates, x => x.Guid == saved.Guid);
                Assert.Equal(GlazingSourceKind.User, row.Candidate.Kind);
                Assert.NotNull(row.Values);
                Assert.Equal(ug, row.UgText);
                Assert.Equal(rows, window.Candidates.Count);
                Assert.Equal(asked, evaluator.GuidsRequested);

                // Choosing it still works, and a choice made before the rename survives it.
                window.SelectGlazing(saved.Guid);
                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);
                Assert.True(library.Rename(saved.Guid, "Newer name").Succeeded);
                await Pump(() => window.Candidates.Any(x => x.Guid == saved.Guid && x.Name == "Newer name"), "the second rename to appear");
                Assert.Equal(saved.Guid, window.SelectedCandidate?.Guid);
                Assert.Equal("Newer name", window.SelectedCandidate.Name);
            }

            Assert.Equal(before, Json(ui.JSAMObject));
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
        }

        [WpfFact]
        public async Task A_remove_reaches_the_open_list_which_drops_the_system_and_resets_a_choice_of_it()
        {
            ApertureConstruction doomed = Save(BuilderFixture.Double("Doomed"));
            ApertureConstruction stays = Save(BuilderFixture.Triple("Stays"));
            ThermalParts parts = ThermalFixture.Build();
            UIAnalyticalModel ui = new UIAnalyticalModel(parts.Model);
            int modified = 0;
            int history = 0;
            ui.Modified += (sender, e) => modified++;
            ui.HistoryChanged += (sender, e) => history++;
            string before = Json(ui.JSAMObject);

            using (ThermalPerformanceViewModel viewModel = new ThermalPerformanceViewModel(Services()))
            {
                ThermalRowEditor window = await OpenWindow(viewModel, ui.JSAMObject, parts);
                window.SelectGlazing(doomed.Guid);
                Assert.Equal(doomed.Guid, window.SelectedCandidate?.Guid);
                Assert.True(window.HasRequest);
                Assert.Equal(1, viewModel.Session.ChangeCount);

                Assert.True(library.Remove(doomed.Guid).Succeeded);
                await Pump(() => window.Candidates.All(x => x.Guid != doomed.Guid), "the removed system to leave the list");

                Assert.Single(window.Candidates, x => x.Guid == stays.Guid);
                Assert.Null(window.SelectedCandidate);
                Assert.False(window.HasRequest);
                Assert.Equal(0, viewModel.Session.ChangeCount);
                Assert.Single(window.Glazing.Sources, x => x.Kind == GlazingSourceKind.User);
            }

            Assert.Equal(before, Json(ui.JSAMObject));
            Assert.Equal(0, modified);
            Assert.Equal(0, history);
            Assert.False(ui.CanUndo);
        }
    }
}
