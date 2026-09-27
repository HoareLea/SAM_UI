// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The mixed design at the target scale - 500 dwellings of 10 spaces, 5,000 spaces - with no TAS run. The matrix
    /// is one plain row per dwelling; the grid realises only what is visible, grouped or not.
    /// <para>
    /// Time bounds here are deliberately loose sanity limits for a CI machine, not budgets: the measured times are
    /// written to the test output. What must hold exactly is structural - rows per dwelling, never per space, and a
    /// realised-row count bounded by the viewport rather than by the project.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOMixedDesignScalingTests
    {
        private const int Count_Dwelling = 500;
        private const int SpacesPerDwelling = 10;

        private readonly ITestOutputHelper testOutputHelper;

        public PartOMixedDesignScalingTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Session_At5000Spaces_BuildsFiltersAssignsAndRefreshes()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(Count_Dwelling, corridor: true, spacesPerFlat: SpacesPerDwelling, category: "Core");
            Assert.Equal(Count_Dwelling * SpacesPerDwelling + 1, baseline.AdjacencyCluster.GetSpaces().Count);

            Stopwatch stopwatch = Stopwatch.StartNew();
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;
            TimeSpan open = stopwatch.Elapsed;

            //One row per dwelling - never per space.
            Assert.Equal(Count_Dwelling, partOMixedDesignSession.Rows.Count);

            stopwatch.Restart();
            List<PartOMixedDwellingRow> shown = [.. partOMixedDesignSession.Rows.Where(x => PartOMixedDesignSession.Matches(x, PartOMixedDwellingFilter.All, "Flat 1"))];
            TimeSpan filter = stopwatch.Elapsed;
            Assert.Equal(110, shown.Count); //Flat 10-19 and Flat 100-199 (names are zero-padded: "Flat 01")

            stopwatch.Restart();
            Assert.Null(partOMixedDesignSession.SetMvhr(partOMixedDesignSession.Rows, null));
            Assert.Null(partOMixedDesignSession.SetNatural(partOMixedDesignSession.Rows.Take(250)));
            TimeSpan assign = stopwatch.Elapsed;

            PartOScreeningEvidence evidence = PartOMixedDesignSessionTests.Evidence(partOMixedDesignSession, PartOScreeningStrategy.Natural, [.. partOMixedDesignSession.Rows.Select(x => (x, PartODwellingOutcome.Pass))]);

            stopwatch.Restart();
            partOMixedDesignSession.ApplyScreening([evidence]);
            TimeSpan screening = stopwatch.Elapsed;

            stopwatch.Restart();
            List<PartOMixedSelectionChange> changes = partOMixedDesignSession.SuggestionChanges(partOMixedDesignSession.Rows);
            TimeSpan suggestions = stopwatch.Elapsed;
            Assert.Equal(250, changes.Count);

            PartOMixedReadiness partOMixedReadiness = partOMixedDesignSession.Readiness();
            Assert.Equal(250, partOMixedReadiness.Natural);
            Assert.Equal(250, partOMixedReadiness.Mvhr);

            testOutputHelper.WriteLine("5,000 spaces / 500 dwellings: open {0:0} ms, filter {1:0} ms, two bulk assignments {2:0} ms, screening refresh {3:0} ms, suggestion changes {4:0} ms", open.TotalMilliseconds, filter.TotalMilliseconds, assign.TotalMilliseconds, screening.TotalMilliseconds, suggestions.TotalMilliseconds);

            Assert.True(open < TimeSpan.FromSeconds(30), open.ToString());
            Assert.True(assign < TimeSpan.FromSeconds(10), assign.ToString());
            Assert.True(screening < TimeSpan.FromSeconds(10), screening.ToString());
        }

        [Fact]
        public void Materialisation_At5000Spaces_HalfNaturalHalfMvhr_IsMeasured()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(Count_Dwelling, corridor: true, spacesPerFlat: SpacesPerDwelling);
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            partOMixedDesignSession.SetMvhr(partOMixedDesignSession.Rows, null);
            partOMixedDesignSession.SetNatural(partOMixedDesignSession.Rows.Where((x, i) => i % 2 == 0));

            Stopwatch stopwatch = Stopwatch.StartNew();
            PartOMaterialisation partOMaterialisation = partOMixedDesignSession.WithSelection().MaterialisePartODwellingStrategies();
            TimeSpan materialise = stopwatch.Elapsed;

            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);
            Assert.Equal(Count_Dwelling / 2, partOMaterialisation.VentilationSystems.Count);

            testOutputHelper.WriteLine("SAM mixed materialisation of 5,000 spaces (250 NV + 250 MVHR dwellings): {0:0.0} s", materialise.TotalSeconds);

            Assert.True(materialise < TimeSpan.FromMinutes(5), materialise.ToString());
        }

        [WpfFact]
        public void Window_At500Dwellings_RealisesOnlyVisibleRows_GroupedOrNot()
        {
            AnalyticalModel baseline = PartOMixedDesignFixture.Baseline(Count_Dwelling, corridor: true, spacesPerFlat: SpacesPerDwelling, category: "Core");
            PartOMixedDesignSession partOMixedDesignSession = new(baseline, null, null, null);
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            PartOMixedDesignWindow partOMixedDesignWindow = new()
            {
                Session = partOMixedDesignSession,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0,
            };

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                partOMixedDesignWindow.Show();
                partOMixedDesignWindow.UpdateLayout();
                TimeSpan show = stopwatch.Elapsed;

                int realised = Realised(partOMixedDesignWindow.Grid_Dwellings);
                Assert.True(realised > 0, "the grid was not laid out, so this test would pass without testing anything");
                Assert.True(realised < 100, string.Format("{0} rows realised of {1}", realised, Count_Dwelling));

                //Grouped: still virtualised.
                partOMixedDesignWindow.Grouped = true;
                partOMixedDesignWindow.UpdateLayout();
                int realised_Grouped = Realised(partOMixedDesignWindow.Grid_Dwellings);
                Assert.True(realised_Grouped < 100, string.Format("{0} rows realised of {1} when grouped", realised_Grouped, Count_Dwelling));

                //Search narrows what is shown; select all shown selects exactly those.
                partOMixedDesignWindow.Grouped = false;
                partOMixedDesignWindow.SearchText = "Flat 49";
                partOMixedDesignWindow.UpdateLayout();
                Assert.Equal(11, partOMixedDesignWindow.ShownRows.Count);

                partOMixedDesignWindow.Grid_Dwellings.SelectAll();
                Assert.Equal(11, partOMixedDesignWindow.SelectedRows.Count);

                //Scrolling to the end realises the end - and still only a viewport's worth.
                partOMixedDesignWindow.SearchText = string.Empty;
                partOMixedDesignWindow.UpdateLayout();
                partOMixedDesignWindow.Grid_Dwellings.ScrollIntoView(partOMixedDesignSession.Rows[^1]);
                partOMixedDesignWindow.UpdateLayout();
                Assert.True(Realised(partOMixedDesignWindow.Grid_Dwellings) < 100);

                testOutputHelper.WriteLine("Window with 500 dwellings shown in {0:0} ms; {1} rows realised ({2} grouped).", show.TotalMilliseconds, realised, realised_Grouped);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        [WpfFact]
        public void Window_ShowsTheFourAuthoritiesSeparately_AndOffersCoolingOnlyAsGated()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(3), null, null, null);
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            PartOMixedDesignWindow partOMixedDesignWindow = new() { Session = partOMixedDesignSession };

            try
            {
                partOMixedDesignWindow.Show();
                partOMixedDesignWindow.UpdateLayout();

                List<string> headers = [.. partOMixedDesignWindow.Grid_Dwellings.Columns.Select(x => x.Header?.ToString() ?? string.Empty)];

                Assert.Contains(headers, x => x.StartsWith("Screening") && x.Contains("Natural"));
                Assert.Contains("Suggested", headers);
                Assert.Contains("Selected (your design)", headers);
                Assert.Contains("Final TM59", headers);

                //Cooling is present only as gated: its checkbox cannot be ticked.
                CheckBox checkBox_Cooling = (CheckBox)partOMixedDesignWindow.FindName("checkBox_Cooling");
                Assert.False(checkBox_Cooling.IsEnabled);

                //No selection: Build is not offered, and says why.
                Button button_Build = (Button)partOMixedDesignWindow.FindName("button_Build");
                Assert.False(button_Build.IsEnabled);
            }
            finally
            {
                partOMixedDesignWindow.Close();
            }
        }

        private static int Realised(DependencyObject dependencyObject)
        {
            int result = dependencyObject is DataGridRow ? 1 : 0;

            int count = VisualTreeHelper.GetChildrenCount(dependencyObject);
            for (int i = 0; i < count; i++)
            {
                result += Realised(VisualTreeHelper.GetChild(dependencyObject, i));
            }

            return result;
        }
    }
}
