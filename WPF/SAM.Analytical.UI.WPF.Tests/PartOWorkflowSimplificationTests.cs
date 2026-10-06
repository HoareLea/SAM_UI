// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Part O / TM59 workflow simplification (September 2026): per-method Iteration 3 results, the
    /// engineer-facing wording, the one progress window, the Hub's Simulation case and Iteration 3 panel, and
    /// the Iteration 3 comparison at the scale of a real project.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOWorkflowSimplificationTests : IDisposable
    {
        private readonly string directory = PartOIteration3Fixture.Directory_Temp();

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        //-------------------------------------------------------------------------------------------------
        //Per-method storage - never shown to an engineer, but it is what lets the methods coexist
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void Every_method_has_its_own_record_and_report_and_the_legacy_ones_are_still_named()
        {
            HashSet<string> paths = [];

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues(typeof(PartOIteration3BehaviourMode)))
            {
                PartOIteration3Paths partOIteration3Paths = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", partOIteration3BehaviourMode);

                Assert.True(paths.Add(partOIteration3Paths.Path_Record), partOIteration3Paths.Path_Record);
                Assert.True(paths.Add(partOIteration3Paths.Path_TPD), partOIteration3Paths.Path_TPD);

                string path_Report = PartOIteration3Paths.Path_Report_ForRecord(partOIteration3Paths.Path_Record);

                Assert.Equal(Path.ChangeExtension(partOIteration3Paths.Path_Record, null) + "-Review.txt", path_Report);
            }

            //A method's own record and its review in Iteration 3's reports folder beneath the results' root.
            Assert.Equal("C:\\out\\Iteration3\\reports\\Flat1-Iteration3-MG.json", PartOIteration3Paths.Path_Record_ForResults("C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.Equal("C:\\out\\Iteration3\\reports\\Flat1-Iteration3-MG-Review.txt", PartOIteration3Paths.Path_Report_ForRecord("C:\\out\\Iteration3\\reports\\Flat1-Iteration3-MG.json"));

            //One written beside the results before that folder existed is still named, and still reports beside itself.
            Assert.Equal("C:\\out\\Flat1-Iteration3-MG.json", PartOIteration3Paths.Path_Record_ForResults_Legacy("C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.Equal("C:\\out\\Flat1-Iteration3-MG-Review.txt", PartOIteration3Paths.Path_Report_ForRecord("C:\\out\\Flat1-Iteration3-MG.json"));

            //The legacy record and its report, exactly as before.
            Assert.Equal("C:\\out\\Flat1-Iteration3.json", PartOIteration3Paths.Path_Record_ForResults("C:\\out\\Flat1.tsd"));
            Assert.Equal("C:\\out\\Flat1-Iteration3-Review.txt", PartOIteration3Paths.Path_Report_ForRecord("C:\\out\\Flat1-Iteration3.json"));
        }

        [Fact]
        public void A_methods_own_record_that_names_another_method_is_not_that_methods_result()
        {
            string path_TSD = Path.Combine(directory, "Flat.tsd");

            PartOIteration3Record partOIteration3Record = new() { BehaviourMode = PartOIteration3BehaviourMode.Parity };
            partOIteration3Record.Adopt(new PartOIteration3Ledger());

            File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance), partOIteration3Record.ToString());

            PartOIteration3PairingStatus partOIteration3PairingStatus = Query.PartOIteration3PairingStatus(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            Assert.False(partOIteration3PairingStatus.Exists);
            Assert.Contains("another method", partOIteration3PairingStatus.Refusal_Read);
        }

        //-------------------------------------------------------------------------------------------------
        //Structured manufacturer guidance on the record
        //-------------------------------------------------------------------------------------------------

        private static PartOIteration3GuidanceEvidence Guidance(string name = "MVHR-01", double designSupply_Lps = 30)
        {
            return new PartOIteration3GuidanceEvidence(
                Guid.NewGuid(),
                name,
                "Nuaire / MRXBOXAB-ECO5-AECV",
                "MR-ECO-COOL-V",
                designSupply_Lps,
                designSupply_Lps,
                120,
                120,
                80,
                60,
                120,
                "Room stat",
                22,
                "exchanger then DX",
                0.62,
                14.5,
                13);
        }

        [Fact]
        public void The_guidance_evidence_round_trips_through_the_record_and_an_older_record_reads_as_none()
        {
            PartOIteration3Record partOIteration3Record = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance };
            partOIteration3Record.Add(Guidance());
            partOIteration3Record.Adopt(new PartOIteration3Ledger());

            PartOIteration3Record partOIteration3Record_Read = PartOIteration3Record.Parse(partOIteration3Record.ToString());

            PartOIteration3GuidanceEvidence partOIteration3GuidanceEvidence = Assert.Single(partOIteration3Record_Read.Guidance);

            Assert.Equal("MVHR-01", partOIteration3GuidanceEvidence.Name_AirHandlingUnit);
            Assert.Equal("MR-ECO-COOL-V", partOIteration3GuidanceEvidence.CoolingModuleModel);
            Assert.Equal(80, partOIteration3GuidanceEvidence.ElevatedAirFlow_Lps);
            Assert.Equal(13, partOIteration3GuidanceEvidence.MinimumSupplyTemperature_C);

            //A record written before the field existed.
            JsonObject jsonObject = JsonNode.Parse(partOIteration3Record.ToString()).AsObject();
            jsonObject.Remove("Guidance");

            Assert.Empty(PartOIteration3Record.Parse(jsonObject.ToJsonString()).Guidance);
        }

        [Fact]
        public void A_non_guidance_result_asks_for_no_guidance_and_a_changed_catalogue_is_never_shown_as_what_ran()
        {
            PartOIteration3Record partOIteration3Record_Parity = new() { BehaviourMode = PartOIteration3BehaviourMode.Parity };

            Assert.Empty(Query.PartOIteration3GuidanceEvidence(null, partOIteration3Record_Parity, null, out string source_Parity));
            Assert.Null(source_Parity);

            //An older guidance result with no fields, and a catalogue that is not the one it ran with.
            PartOIteration3Record partOIteration3Record_Old = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                Sha256_VentilationUnitCatalogue = new string('A', 64),
            };

            Assert.Empty(Query.PartOIteration3GuidanceEvidence(null, partOIteration3Record_Old, null, out string source_Old));
            Assert.Contains("changed", source_Old);

            //Recorded fields are used as recorded.
            PartOIteration3Record partOIteration3Record_New = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance };
            partOIteration3Record_New.Add(Guidance());

            Assert.Single(Query.PartOIteration3GuidanceEvidence(null, partOIteration3Record_New, null, out string source_New));
            Assert.Contains("recorded", source_New);
        }

        //-------------------------------------------------------------------------------------------------
        //The engineer-facing wording
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The internal names stay in code and files; none of them reaches an engineer. The product methods are
        /// offered first and the two validation methods are the ones behind Advanced.
        /// </summary>
        [Fact]
        public void The_methods_are_worded_for_an_engineer_and_the_validation_methods_are_advanced()
        {
            string[] internalTerms = ["A/B", "B0", "B4", "MG", "pairing", "Parity", "Candidate", "Reference A"];

            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues(typeof(PartOIteration3BehaviourMode)))
            {
                string text = Query.PartOIteration3MethodLabel(partOIteration3BehaviourMode) + " " + Query.PartOIteration3MethodExplanation(partOIteration3BehaviourMode);

                foreach (string term in internalTerms)
                {
                    Assert.DoesNotContain(term, text);
                }
            }

            Assert.Equal(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance, Query.PartOIteration3BehaviourModes[0]);
            Assert.Equal(PartOIteration3BehaviourMode.SelectedProduct, Query.PartOIteration3BehaviourModes[1]);

            Assert.True(Query.IsPartOIteration3ValidationMethod(PartOIteration3BehaviourMode.Parity));
            Assert.True(Query.IsPartOIteration3ValidationMethod(PartOIteration3BehaviourMode.SelectedProductCooling));
            Assert.False(Query.IsPartOIteration3ValidationMethod(PartOIteration3BehaviourMode.SelectedProduct));
            Assert.False(Query.IsPartOIteration3ValidationMethod(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
        }

        [Fact]
        public void The_reference_case_is_named_as_the_earlier_iteration()
        {
            PartORun partORun = new();

            Assert.True(partORun.Prepare(
                PartOIteration3Fixture.Model(PartOIteration3Fixture.Design(out List<Guid> guids, out List<Zone> zones)),
                PartOIteration3Fixture.Scenarios(),
                new PartOPreparationContext(Analytical.Enums.PartOIteration.BasePassive, zones, null, null),
                guids));

            Assert.Equal("Iteration 1a — baseline", Query.PartOIterationText(partORun));
        }

        //-------------------------------------------------------------------------------------------------
        //The one progress window
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void The_progress_state_is_honest_stages_and_durations_with_no_percentage()
        {
            DateTime now = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

            PartOProgressState partOProgressState = new(["One", "Two", "Three", "Four"], () => now);

            partOProgressState.Start(0);
            now = now.AddSeconds(42);

            //Starting stage 2 completes stage 0 and marks the stage nobody started as skipped.
            partOProgressState.Start(2);

            Assert.Equal(PartOProgressStageStatus.Completed, partOProgressState.Status(0));
            Assert.Equal(PartOProgressStageStatus.Skipped, partOProgressState.Status(1));
            Assert.Equal(PartOProgressStageStatus.Running, partOProgressState.Status(2));
            Assert.Equal(PartOProgressStageStatus.Pending, partOProgressState.Status(3));
            Assert.Equal(TimeSpan.FromSeconds(42), partOProgressState.Duration(0));

            now = now.AddMinutes(7).AddSeconds(48);

            Assert.Equal("7m 48s", PartOProgressState.Format(partOProgressState.Duration(2).Value));

            partOProgressState.Fail("stopped");

            Assert.Equal(PartOProgressStageStatus.Failed, partOProgressState.Status(2));
            Assert.Equal("8m 30s", PartOProgressState.Format(partOProgressState.Elapsed));

            //The clock stops when the operation ends.
            now = now.AddHours(1);
            Assert.Equal("8m 30s", PartOProgressState.Format(partOProgressState.Elapsed));

            List<string> lines = partOProgressState.Lines();
            Assert.Equal("✓ One · 42s", lines[0]);
            Assert.Equal("✕ Three · 7m 48s", lines[2]);
            Assert.Equal("○ Four", lines[3]);
            Assert.DoesNotContain(lines, x => x.Contains('%'));

            Assert.Equal("1h 03m", PartOProgressState.Format(TimeSpan.FromMinutes(63)));
        }

        [Fact]
        public void The_progress_host_is_the_ambient_one_while_it_lives_and_cancel_latches_its_token()
        {
            Assert.Null(PartOProgressHost.Current);

            using (PartOProgressHost partOProgressHost = new("Outer", null, ["a"], true, false))
            {
                Assert.Same(partOProgressHost, PartOProgressHost.Current);

                using (PartOProgressHost partOProgressHost_Inner = new("Inner", null, ["b"], true, false))
                {
                    Assert.Same(partOProgressHost_Inner, PartOProgressHost.Current);
                }

                Assert.Same(partOProgressHost, PartOProgressHost.Current);

                Assert.False(partOProgressHost.Token.IsCancellationRequested);

                partOProgressHost.Cancel();

                Assert.True(partOProgressHost.Token.IsCancellationRequested);

                //Hide and Show with no window are harmless.
                partOProgressHost.Hide();
                partOProgressHost.Show();
            }

            Assert.Null(PartOProgressHost.Current);
        }

        [Fact]
        public void Every_iteration_3_stage_belongs_to_one_phase_and_the_phases_are_in_ledger_order()
        {
            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in Enum.GetValues<PartOIteration3BehaviourMode>())
            {
                int phase_Previous = 0;

                foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
                {
                    int phase = Modify.PartOIteration3Phase(partOIteration3Stage, partOIteration3BehaviourMode);

                    Assert.InRange(phase, 0, Modify.PartOIteration3Phases(partOIteration3BehaviourMode).Count - 1);
                    Assert.True(phase >= phase_Previous);

                    phase_Previous = phase;
                }

                Assert.Equal(Modify.PartOIteration3Phases(partOIteration3BehaviourMode).Count - 1, phase_Previous);
            }
        }

        /// <summary>What the shown progress window says, read through UI Automation as an outside reader would.</summary>
        private static string ProgressText()
        {
            System.Windows.Automation.AutomationElement automationElement = System.Windows.Automation.AutomationElement.RootElement.FindFirst(
                System.Windows.Automation.TreeScope.Children,
                new System.Windows.Automation.AndCondition(
                    new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ProcessIdProperty, Environment.ProcessId),
                    new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.NameProperty, "Part O")));

            if (automationElement is null)
            {
                return null;
            }

            List<string> texts = [];
            foreach (System.Windows.Automation.AutomationElement automationElement_Text in automationElement.FindAll(System.Windows.Automation.TreeScope.Descendants, new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Text)))
            {
                texts.Add(automationElement_Text.Current.Name);
            }

            return string.Join(" | ", texts);
        }

        /// <summary>
        /// Prepare &amp; Run hides the progress window while the engineer reviews the prepared iteration, and shows
        /// it again for the simulation. It must come back with its content - stages, elapsed time - not as an
        /// empty frame.
        /// </summary>
        [WpfFact]
        public void The_progress_window_keeps_its_content_after_standing_aside_for_a_dialog()
        {
            using PartOProgressHost partOProgressHost = new("Prepare & Run", null, ["Prepare and review the iteration", "TAS simulation (full year)"]);

            Assert.True(partOProgressHost.IsShown, "no progress window could be shown on this desktop");

            partOProgressHost.Start(0);
            System.Threading.Thread.Sleep(1500);

            string before = ProgressText();
            Assert.Contains("Prepare and review the iteration", before);

            partOProgressHost.Hide();
            System.Threading.Thread.Sleep(500);
            partOProgressHost.Show();
            partOProgressHost.Start(1);
            System.Threading.Thread.Sleep(1500);

            string after = ProgressText();
            Assert.Contains("TAS simulation (full year)", after);
            Assert.Contains("Elapsed ", after);
        }

        [WpfFact]
        public void The_progress_window_shows_the_stages_the_detail_and_the_elapsed_time()
        {
            PartOProgressState partOProgressState = new(["Reference case", "TAS building simulation"]);
            partOProgressState.Start(1);
            partOProgressState.Detail = "Simulating Model";

            PartOProgressWindow partOProgressWindow = new()
            {
                Heading = "Iteration 3 — Selected product — manufacturer operating guidance",
                Subheading = "Reference case: Iteration 1a — baseline",
                State = partOProgressState,
            };

            ItemsControl itemsControl = (ItemsControl)partOProgressWindow.FindName("itemsControl_Stages");
            TextBlock textBlock_Detail = (TextBlock)partOProgressWindow.FindName("textBlock_Detail");
            TextBlock textBlock_Elapsed = (TextBlock)partOProgressWindow.FindName("textBlock_Elapsed");
            ProgressBar progressBar = (ProgressBar)partOProgressWindow.FindName("progressBar");

            Assert.Equal(2, itemsControl.Items.Count);
            Assert.Equal("Simulating Model", textBlock_Detail.Text);
            Assert.StartsWith("Elapsed ", textBlock_Elapsed.Text);

            //Indeterminate: TAS reports no fraction, so none is shown.
            Assert.True(progressBar.IsIndeterminate);

            partOProgressWindow.Close();
        }

        //-------------------------------------------------------------------------------------------------
        //The Simulation case - the Simulate dialog's three open inputs, now in the Hub
        //-------------------------------------------------------------------------------------------------

        [Fact]
        public void A_simulation_case_needs_weather_a_folder_and_a_solar_method()
        {
            Assert.Contains("weather", new PartOSimulationCase { OutputDirectory = "C:\\out" }.Refusal());
            Assert.Contains("output folder", new PartOSimulationCase { WeatherData = new Weather.WeatherData("w", "w", 51, 0, 0) }.Refusal());
            Assert.Null(new PartOSimulationCase { WeatherData = new Weather.WeatherData("w", "w", 51, 0, 0), OutputDirectory = "C:\\out" }.Refusal());
        }

        [Fact]
        public void The_first_simulation_case_is_what_the_simulate_dialog_would_have_opened_with()
        {
            AnalyticalModel analyticalModel = PartOIteration3Fixture.Model(new AdjacencyCluster(), "Flat");
            Weather.WeatherData weatherData = new("Leeds", "Leeds", 53.8, -1.5, 0);
            analyticalModel.SetValue(Analytical.AnalyticalModelParameter.WeatherData, weatherData);

            PartOSimulationCase partOSimulationCase = PartOSimulationCase.Create(analyticalModel, Path.Combine(directory, "Flat.sam"), null);

            Assert.Equal("Leeds", partOSimulationCase.WeatherData?.Name);
            Assert.Equal(directory, partOSimulationCase.OutputDirectory);
            Assert.Equal(SolarCalculationMethod.TAS, partOSimulationCase.SolarCalculationMethod);
        }

        [WpfFact]
        public void A_stated_simulation_case_that_cannot_run_blocks_prepare_and_run_and_says_why()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                SimulationCase = new PartOSimulationCase { OutputDirectory = directory },
            };

            partOWorkflowWindow.CompleteInitialisation();

            Assert.False(partOWorkflowWindow.CanRun);
            Assert.Contains("Simulation case", partOWorkflowWindow.BlockerDescription);
            Assert.Contains("weather", partOWorkflowWindow.SimulationCaseSummary);

            partOWorkflowWindow.Close();
        }

        //-------------------------------------------------------------------------------------------------
        //The Hub's Iteration 3 panel and its last-outcome line
        //-------------------------------------------------------------------------------------------------

        [WpfFact]
        public void The_iteration_3_panel_states_each_methods_result_and_offers_review_only_for_a_completed_one()
        {
            string path_TSD = Path.Combine(directory, "Flat.tsd");

            PartOIteration3Ledger partOIteration3Ledger_Complete = new();
            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                partOIteration3Ledger_Complete.Complete(partOIteration3Stage, "done");
            }

            PartOIteration3Record partOIteration3Record_Complete = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                Status_ReferenceA = TM59ComplianceStatus.Fail,
                Status_CandidateB = TM59ComplianceStatus.Fail,
                Ticks_Utc = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc).Ticks,
            };
            partOIteration3Record_Complete.Adopt(partOIteration3Ledger_Complete);

            PartOIteration3Ledger partOIteration3Ledger_Refused = new();
            partOIteration3Ledger_Refused.Complete(PartOIteration3Stage.Input, "ready");
            partOIteration3Ledger_Refused.Refuse(PartOIteration3Stage.ReferenceA, "no", ["no"]);

            PartOIteration3Record partOIteration3Record_Refused = new() { BehaviourMode = PartOIteration3BehaviourMode.SelectedProduct };
            partOIteration3Record_Refused.Adopt(partOIteration3Ledger_Refused);

            File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance), partOIteration3Record_Complete.ToString());
            File.WriteAllText(PartOIteration3Paths.Path_Record_ForResults(path_TSD, PartOIteration3BehaviourMode.SelectedProduct), partOIteration3Record_Refused.ToString());

            //Eligibility over those files: no run in this session, so nothing can be run - only reviewed.
            PartOIteration3Eligibility partOIteration3Eligibility = new(false, "No completed Part O run is available.", true, null, null, Query.PartOIteration3PairingStatuses(path_TSD));

            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                Iteration3Eligibility = partOIteration3Eligibility,
                Iteration3Mode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
            };

            partOWorkflowWindow.CompleteInitialisation();

            //A recorded result makes the panel relevant, although nothing can be run.
            Assert.True(partOWorkflowWindow.IsIteration3PanelVisible);

            Assert.StartsWith("✓ Result available", partOWorkflowWindow.Iteration3StatusText(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.Contains("reference FAIL / system FAIL", partOWorkflowWindow.Iteration3StatusText(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.StartsWith("Last attempt did not complete", partOWorkflowWindow.Iteration3StatusText(PartOIteration3BehaviourMode.SelectedProduct));
            Assert.Equal("Not run yet", partOWorkflowWindow.Iteration3StatusText(PartOIteration3BehaviourMode.Parity));

            Assert.True(partOWorkflowWindow.CanOpenIteration3Result);
            Assert.Equal("Open result", partOWorkflowWindow.Iteration3ReviewText);
            Assert.False(partOWorkflowWindow.CanRunIteration3);

            //The refused method offers its last attempt, never as the result.
            partOWorkflowWindow.Iteration3Mode = PartOIteration3BehaviourMode.SelectedProduct;

            Assert.True(partOWorkflowWindow.CanOpenIteration3Result);
            Assert.Equal("Show last attempt", partOWorkflowWindow.Iteration3ReviewText);

            //A method with nothing recorded offers nothing to open.
            partOWorkflowWindow.Iteration3Mode = PartOIteration3BehaviourMode.Parity;

            Assert.False(partOWorkflowWindow.CanOpenIteration3Result);

            //Exactly one method reads as chosen, whichever disclosure it sits under.
            foreach (PartOIteration3BehaviourMode partOIteration3BehaviourMode in new[] { PartOIteration3BehaviourMode.SelectedProductCooling, PartOIteration3BehaviourMode.SelectedProduct, PartOIteration3BehaviourMode.Parity, PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance })
            {
                partOWorkflowWindow.Iteration3Mode = partOIteration3BehaviourMode;

                Assert.Equal(1, partOWorkflowWindow.Iteration3CheckedCount);
            }

            partOWorkflowWindow.Close();
        }

        [WpfFact]
        public void A_completed_run_is_reported_inline_rather_than_by_a_message_box()
        {
            PartOWorkflowWindow partOWorkflowWindow = new()
            {
                LastOutcome = new PartOWorkflowOutcome(PartOWorkflowOutcomeKind.Success, "✓ Iteration 1a complete · TAS simulation 7m 48s · TM59 Fail"),
            };

            partOWorkflowWindow.CompleteInitialisation();

            Assert.Equal("✓ Iteration 1a complete · TAS simulation 7m 48s · TM59 Fail", partOWorkflowWindow.LastOutcomeText);

            partOWorkflowWindow.LastOutcome = null;

            Assert.Equal(string.Empty, partOWorkflowWindow.LastOutcomeText);

            partOWorkflowWindow.Close();
        }

        //-------------------------------------------------------------------------------------------------
        //The comparison at the scale of a real project
        //-------------------------------------------------------------------------------------------------

        /// <summary>A completed comparison over <paramref name="count_Dwelling"/> dwellings of five rooms, three criteria each.</summary>
        private PartOIteration3Result Result_Large(int count_Dwelling, out int count_Rows)
        {
            List<PartOIteration3Room> rooms = [];
            Dictionary<Guid, double[]> series_A = [];
            Dictionary<Guid, double[]> series_B = [];
            List<PartOIteration3CriterionComparison> criteria = [];

            string[] names = ["Living", "Kitchen", "Bedroom 1", "Bedroom 2", "Bathroom"];

            for (int d = 0; d < count_Dwelling; d++)
            {
                Guid guid_Dwelling = Guid.NewGuid();
                string name_Dwelling = string.Format("Flat {0:0000}", d + 1);

                for (int r = 0; r < names.Length; r++)
                {
                    Guid guid_Room = Guid.NewGuid();

                    rooms.Add(new PartOIteration3Room(guid_Room, names[r], guid_Dwelling, name_Dwelling));

                    double[] a = new double[24];
                    double[] b = new double[24];
                    for (int h = 0; h < 24; h++)
                    {
                        a[h] = 22 + (h % 6);
                        b[h] = a[h] + ((d + r) % 7) * 0.1;
                    }

                    series_A[guid_Room] = a;
                    series_B[guid_Room] = b;

                    foreach (string criterion in new[] { "TM59 Criterion A", "TM59 Criterion B", "TM59 Criterion C" })
                    {
                        bool fail_B = (d + r) % 11 == 0;

                        criteria.Add(new PartOIteration3CriterionComparison(guid_Room, names[r], guid_Dwelling, name_Dwelling, criterion, true, 10, 32, TM59ComplianceStatus.Pass, fail_B ? 40 : 12, 32, fail_B ? TM59ComplianceStatus.Fail : TM59ComplianceStatus.Pass));
                    }
                }
            }

            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(rooms, series_A, series_B, criteria, out List<string> refusals);

            Assert.NotNull(partOIteration3Comparison);
            Assert.Empty(refusals);

            count_Rows = criteria.Count;

            PartOIteration3Ledger partOIteration3Ledger = new();
            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                partOIteration3Ledger.Complete(partOIteration3Stage, "done");
            }

            PartOIteration3Record partOIteration3Record = new()
            {
                BehaviourMode = PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance,
                ProjectName_ReferenceA = "Block",
                ProjectName_CandidateB = "Block-It3BMG",
            };

            partOIteration3Record.Adopt(partOIteration3Ledger);

            return new PartOIteration3Result(
                partOIteration3Ledger,
                partOIteration3Record,
                partOIteration3Comparison,
                new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, null, null, null, null, null, "A report", null, rooms.Count),
                new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Fail, null, null, null, null, null, "B report", null, rooms.Count),
                null,
                null,
                Path.Combine(directory, "Block-Iteration3-MG.json"),
                false,
                ["a note"]);
        }

        private static List<T> Descendants<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            List<T> result = [];

            Stack<DependencyObject> stack = new();
            stack.Push(dependencyObject);

            while (stack.Count != 0)
            {
                DependencyObject current = stack.Pop();

                if (current is T t)
                {
                    result.Add(t);
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    stack.Push(VisualTreeHelper.GetChild(current, i));
                }
            }

            return result;
        }

        /// <summary>
        /// About five thousand rooms - a thousand dwellings, fifteen thousand criterion rows. The window must
        /// open with its grouped grid VIRTUALISED (WPF switches grouping virtualisation off unless asked), its
        /// dwelling groups COLLAPSED, and only a screenful of rows realised; and a filter that narrows the list
        /// to something readable opens its groups.
        /// </summary>
        [WpfFact]
        public void Five_thousand_rooms_open_as_collapsed_virtualised_dwelling_groups()
        {
            PartOIteration3Result partOIteration3Result = Result_Large(1000, out int count_Rows);

            Assert.Equal(15000, count_Rows);

            Stopwatch stopwatch = Stopwatch.StartNew();

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                ReferenceText = "Iteration 1a — baseline",
                Result = partOIteration3Result,
            };

            partOIteration3ResultWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            partOIteration3ResultWindow.Left = 0;
            partOIteration3ResultWindow.Top = 0;
            partOIteration3ResultWindow.Width = 1320;
            partOIteration3ResultWindow.Height = 860;

            try
            {
                partOIteration3ResultWindow.Show();
                partOIteration3ResultWindow.UpdateLayout();

                stopwatch.Stop();

                DataGrid dataGrid = (DataGrid)partOIteration3ResultWindow.FindName("dataGrid_Comparison");

                Assert.True(VirtualizingPanel.GetIsVirtualizingWhenGrouping(dataGrid));
                Assert.Equal(VirtualizationMode.Recycling, VirtualizingPanel.GetVirtualizationMode(dataGrid));

                Assert.Equal(count_Rows, partOIteration3ResultWindow.Rows_Visible.Count);
                Assert.False(partOIteration3ResultWindow.GroupsExpanded);

                List<DataGridRow> dataGridRows = Descendants<DataGridRow>(dataGrid);
                List<GroupItem> groupItems = Descendants<GroupItem>(dataGrid);

                //Collapsed groups realise no rows, and only a screenful of groups is realised at all.
                Assert.True(dataGridRows.Count < 100, string.Format("{0} rows were realised out of {1}", dataGridRows.Count, count_Rows));
                Assert.True(groupItems.Count > 0, "no dwelling group was realised, so the window was not laid out");
                Assert.True(groupItems.Count < 200, string.Format("{0} dwelling groups were realised out of 1000", groupItems.Count));

                //Every group closed.
                Assert.All(Descendants<Expander>(dataGrid), x => Assert.False(x.IsExpanded));

                //Narrowed to something readable: the groups open, and still only a screenful of rows exists.
                CheckBox checkBox_Failures = (CheckBox)partOIteration3ResultWindow.FindName("checkBox_Failures");
                TextBox textBox_Search = (TextBox)partOIteration3ResultWindow.FindName("textBox_Search");

                textBox_Search.Text = "flat 0011";
                checkBox_Failures.IsChecked = true;

                partOIteration3ResultWindow.UpdateLayout();

                Assert.True(partOIteration3ResultWindow.Rows_Visible.Count > 0);
                Assert.True(partOIteration3ResultWindow.Rows_Visible.Count <= PartOIteration3ResultWindow.Count_ExpandFiltered);
                Assert.True(partOIteration3ResultWindow.GroupsExpanded);
                Assert.True(Descendants<DataGridRow>(dataGrid).Count > 0);

                //A broad filter over thousands of rows keeps them collapsed.
                textBox_Search.Text = string.Empty;
                partOIteration3ResultWindow.UpdateLayout();

                Assert.True(partOIteration3ResultWindow.Rows_Visible.Count > PartOIteration3ResultWindow.Count_ExpandFiltered);
                Assert.False(partOIteration3ResultWindow.GroupsExpanded);

                //Largest differences is a flat list, largest first - still virtualised.
                checkBox_Failures.IsChecked = false;
                ((CheckBox)partOIteration3ResultWindow.FindName("checkBox_Largest")).IsChecked = true;
                partOIteration3ResultWindow.UpdateLayout();

                List<PartOIteration3Row> rows_Largest = partOIteration3ResultWindow.Rows_Visible;

                Assert.Equal(count_Rows, rows_Largest.Count);
                Assert.True(rows_Largest[0].MaximumAbsoluteDifference >= rows_Largest[^1].MaximumAbsoluteDifference);
                Assert.True(Descendants<DataGridRow>(dataGrid).Count < 100);
            }
            finally
            {
                partOIteration3ResultWindow.Close();
            }

            //A generous bound for a loaded CI machine; the point is minutes-not-seconds regressions.
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), string.Format("the window took {0} to open over 15 000 rows", stopwatch.Elapsed));
        }

        /// <summary>The summary is the headline, not a paragraph: verdicts and statistics are tiles.</summary>
        [WpfFact]
        public void The_comparison_leads_with_the_verdicts_and_statistics_in_engineer_terms()
        {
            PartOIteration3Result partOIteration3Result = Result_Large(2, out int _);

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                ReferenceText = "Iteration 1a — baseline",
                Guidance = [Guidance("MVHR-01", 30), Guidance("MVHR-02", 63)],
                GuidanceSource = "As resolved and recorded when this result was produced.",
                Result = partOIteration3Result,
            };

            string Text(string name) => ((TextBlock)partOIteration3ResultWindow.FindName(name)).Text;

            Assert.Equal("Iteration 3 comparison — Selected product — manufacturer operating guidance", Text("textBlock_Heading"));
            Assert.StartsWith("Reference case: Iteration 1a — baseline", Text("textBlock_Cases"));
            Assert.Equal("PASS", Text("textBlock_TileReference"));
            Assert.Equal("FAIL", Text("textBlock_TileSystem"));
            Assert.EndsWith(" K", Text("textBlock_TileBias"));
            Assert.Contains(" of ", Text("textBlock_TileChanged"));

            //The guidance as a card, one set of values for one product run the same way.
            ItemsControl itemsControl = (ItemsControl)partOIteration3ResultWindow.FindName("itemsControl_SystemFacts");
            List<KeyValuePair<string, string>> facts = itemsControl.ItemsSource.Cast<KeyValuePair<string, string>>().ToList();

            Assert.Equal("Nuaire / MRXBOXAB-ECO5-AECV + MR-ECO-COOL-V", facts.Single(x => x.Key == "Product").Value);
            Assert.Equal("22 °C", facts.Single(x => x.Key == "Setpoint").Value.Replace(',', '.'));
            Assert.Equal("30–63 l/s", facts.Single(x => x.Key == "Background (design) flow").Value);
            Assert.StartsWith("80 l/s", facts.Single(x => x.Key == "Cooling flow").Value);
            Assert.Equal("13 °C", facts.Single(x => x.Key == "Supply minimum").Value);
            Assert.StartsWith("Heat exchanger", facts.Single(x => x.Key == "Strategy").Value);

            //No internal term in the headline.
            foreach (string name in new[] { "textBlock_Heading", "textBlock_Cases" })
            {
                Assert.DoesNotContain("A/B", Text(name));
                Assert.DoesNotContain("Candidate", Text(name));
            }

            partOIteration3ResultWindow.Close();
        }
    }
}
