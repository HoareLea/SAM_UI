// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Iteration 3 window as a person actually meets it: at a window size, with buttons that get
    /// clicked, including in the states where there is nothing for a button to do.
    ///
    /// <para><b>What this is for</b></para>
    /// <para>
    /// Every one of these cases was reachable from the ordinary Iteration 3 workflow and none of them was
    /// covered. A control that throws when its result is absent is not a cosmetic problem: it is an
    /// unhandled exception in a WPF dispatcher, which takes the application down and loses the run the
    /// person was looking at. So every user-accessible control is clicked here in the state where it has
    /// least to work with, and the assertion is the same each time - <b>it refuses, it does not throw</b>.
    /// </para>
    ///
    /// <para><b>And the geometry</b></para>
    /// <para>
    /// The window asks for 1400x820, which does not fit a 1920x1080 desktop at 125% or 150% display
    /// scaling - and a window taller than the desktop cannot be resized back into view, because its title
    /// bar is the thing you would have to drag upwards. So the size is clamped, and the action bar is
    /// proved to be inside the window at the smallest size the window allows.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOIteration3WindowSmokeTests : IDisposable
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

        private static readonly Guid guid_Room = new("aaaaaaaa-0000-0000-0000-000000000001");

        private static readonly Guid guid_Dwelling = new("11111111-1111-1111-1111-111111111111");

        private static PartOIteration3Comparison Comparison()
        {
            List<PartOIteration3Room> rooms = [new PartOIteration3Room(guid_Room, "Bedroom 2", guid_Dwelling, "Flat 1")];

            Dictionary<Guid, double[]> series_A = new() { { guid_Room, [20.0, 20.0] } };
            Dictionary<Guid, double[]> series_B = new() { { guid_Room, [21.0, 21.0] } };

            List<PartOIteration3CriterionComparison> criteria =
            [
                new PartOIteration3CriterionComparison(guid_Room, "Bedroom 2", guid_Dwelling, "Flat 1", "TM59 Criterion A", true, 10, 32, TM59ComplianceStatus.Pass, 11, 32, TM59ComplianceStatus.Pass),
            ];

            return PartOIteration3Comparison.Create(rooms, series_A, series_B, criteria, out List<string> _);
        }

        /// <summary>A completed pairing, whose record path is inside this test's own directory.</summary>
        private PartOIteration3Result Result_Complete(bool assessments = true)
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            foreach (PartOIteration3Stage partOIteration3Stage in PartOIteration3Ledger.Order)
            {
                partOIteration3Ledger.Complete(partOIteration3Stage, "did its work");
            }

            PartOIteration3Record partOIteration3Record = new()
            {
                ProjectName_ReferenceA = "Flat",
                ProjectName_CandidateB = "Flat-It3B",
                Fingerprint_Scenario = "weather=CIBSE 2021 Leeds_TRY | solar=TAS | days 1-365",
                Count_AirSystem = 3,
            };

            partOIteration3Record.Adopt(partOIteration3Ledger);

            return new PartOIteration3Result(
                partOIteration3Ledger,
                partOIteration3Record,
                Comparison(),
                assessments ? new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Pass, null, null, null, null, null, "A report", null, 1) : null,
                assessments ? new PartOIteration3Assessment(true, null, TM59ComplianceStatus.Fail, null, null, null, null, null, "B report", null, 1) : null,
                null,
                null,
                Path.Combine(directory, "Flat-Iteration3.json"),
                false,
                ["a note"]);
        }

        private PartOIteration3Result Result_Refused()
        {
            PartOIteration3Ledger partOIteration3Ledger = new();

            partOIteration3Ledger.Complete(PartOIteration3Stage.Input, "ready");
            partOIteration3Ledger.Refuse(
                PartOIteration3Stage.Input,
                "This Iteration 3 pairing can no longer be shown.",
                ["Reference A's design state has changed since this Iteration 3 pairing was produced."]);

            PartOIteration3Record partOIteration3Record = new();

            partOIteration3Record.Adopt(partOIteration3Ledger);

            return new PartOIteration3Result(
                partOIteration3Ledger,
                partOIteration3Record,
                null,
                null,
                null,
                null,
                null,
                Path.Combine(directory, "Flat-Iteration3.json"),
                true,
                null);
        }

        private string WriteHistoricalReport()
        {
            string path = PartOIteration3Paths.Path_Report_ForRecord(Path.Combine(directory, "Flat-Iteration3.json"));

            File.WriteAllText(path, "the successful pairing, as it was");

            return path;
        }

        private static T Control<T>(PartOIteration3ResultWindow partOIteration3ResultWindow, string name)
            where T : FrameworkElement
        {
            return (T)partOIteration3ResultWindow.FindName(name);
        }

        private static void Click(PartOIteration3ResultWindow partOIteration3ResultWindow, string name)
        {
            Button button = Control<Button>(partOIteration3ResultWindow, name);

            //Raised rather than invoked through automation, because what is under test is the handler and
            //not WPF's routing - and a disabled button's handler is exactly the one worth proving safe.
            button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        //-------------------------------------------------------------------------------------------------
        //Geometry
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// The window never opens larger than the desktop it opens on, whatever the XAML asks for. At 150%
        /// scaling on a 1080p screen the work area is 688 device-independent pixels tall, and the
        /// unclamped 820 would put the action bar below the taskbar with no way to drag it back.
        /// </summary>
        [WpfFact]
        public void The_window_opens_inside_the_work_area()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new();

            Rect rect = SystemParameters.WorkArea;

            Assert.True(partOIteration3ResultWindow.Width <= rect.Width, "the window is wider than the work area");
            Assert.True(partOIteration3ResultWindow.Height <= rect.Height, "the window is taller than the work area");
        }

        /// <summary>
        /// At the smallest size the window allows, its whole content still fits inside the client area -
        /// which is the reason the summary sits in a bounded scroller, the refusal has a star row of its
        /// own, and the action bar wraps rather than running under the Close button.
        /// <para>
        /// The window is <b>shown</b> rather than measured in place: a <c>Window</c> lays its content out
        /// only once it has a presentation source, so <c>Measure</c>/<c>Arrange</c> on an unshown one
        /// reports every control as zero-sized and the assertion passes without testing anything. The
        /// client area is read from the OS for the same reason - it is the only number that says what the
        /// content actually had to fit into.
        /// </para>
        /// </summary>
        [WpfFact]
        public void The_whole_window_fits_its_client_area_at_the_minimum_size()
        {
            //A refusal with a great deal of text: the state that used to push the action bar off the
            //bottom of the window.
            PartOIteration3Ledger partOIteration3Ledger = new();

            List<string> reasons = [];
            for (int i = 0; i < 200; i++)
            {
                reasons.Add(string.Format("Reason {0}: a long refusal line that wraps over more than one line of the window at any sensible width.", i));
            }

            partOIteration3Ledger.Refuse(PartOIteration3Stage.Input, "This Iteration 3 pairing can no longer be shown.", reasons);

            PartOIteration3Record partOIteration3Record = new();
            partOIteration3Record.Adopt(partOIteration3Ledger);

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = new PartOIteration3Result(partOIteration3Ledger, partOIteration3Record, null, null, null, null, null, null, true, null),
            };

            partOIteration3ResultWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            partOIteration3ResultWindow.Left = 0;
            partOIteration3ResultWindow.Top = 0;
            partOIteration3ResultWindow.Width = partOIteration3ResultWindow.MinWidth;
            partOIteration3ResultWindow.Height = partOIteration3ResultWindow.MinHeight;

            try
            {
                partOIteration3ResultWindow.Show();
                partOIteration3ResultWindow.UpdateLayout();

                Size size_Client = Client(partOIteration3ResultWindow);
                Size size_Content = ((FrameworkElement)partOIteration3ResultWindow.Content).DesiredSize;

                Assert.True(
                    size_Content.Height <= size_Client.Height + 0.5,
                    string.Format("the content needs {0} of height and the client area is {1}", size_Content.Height, size_Client.Height));

                Assert.True(
                    size_Content.Width <= size_Client.Width + 0.5,
                    string.Format("the content needs {0} of width and the client area is {1}", size_Content.Width, size_Client.Width));

                //And the action bar is the part that matters: it is the last row, and it is inside.
                Button button_Close = Control<Button>(partOIteration3ResultWindow, "button_Close");

                Point point = button_Close.TranslatePoint(new Point(button_Close.ActualWidth, button_Close.ActualHeight), (FrameworkElement)partOIteration3ResultWindow.Content);

                Assert.True(point.Y > 0, "the window was not laid out, so this assertion would pass without testing anything");
                Assert.True(point.Y <= size_Client.Height + 0.5, string.Format("the Close button's bottom edge is at {0}, below the client area of {1}", point.Y, size_Client.Height));
                Assert.True(point.X <= size_Client.Width + 0.5, string.Format("the Close button's right edge is at {0}, outside the client area of {1}", point.X, size_Client.Width));
            }
            finally
            {
                partOIteration3ResultWindow.Close();
            }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>The window's client area in device-independent pixels, as the OS reports it.</summary>
        private static Size Client(System.Windows.Window window)
        {
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            if (handle == IntPtr.Zero || !GetClientRect(handle, out RECT rect))
            {
                return new Size(double.NaN, double.NaN);
            }

            System.Windows.Media.Matrix matrix = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;

            return new Size((rect.Right - rect.Left) * matrix.M11, (rect.Bottom - rect.Top) * matrix.M22);
        }

        //-------------------------------------------------------------------------------------------------
        //Clicking everything, in the states where there is least to click
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// Every action, on a refused pairing with no assessment, no TM59 report, no folder and no
        /// persisted report. Each one must refuse rather than throw.
        /// </summary>
        [WpfFact]
        public void Every_action_refuses_cleanly_on_a_refused_pairing_with_nothing_to_open()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Assert.False(Control<Button>(partOIteration3ResultWindow, "button_ReportA").IsEnabled);
            Assert.False(Control<Button>(partOIteration3ResultWindow, "button_ReportB").IsEnabled);
            Assert.False(Control<Button>(partOIteration3ResultWindow, "button_OpenReport").IsEnabled);

            foreach (string name in new[] { "button_ReportA", "button_ReportB", "button_OpenReport", "button_OpenFolder", "button_Close" })
            {
                Click(partOIteration3ResultWindow, name);
            }

            //Still usable after every refused action.
            Assert.NotNull(partOIteration3ResultWindow.Result);
            Assert.Empty(partOIteration3ResultWindow.Rows_Visible);
        }

        /// <summary>
        /// Repeating an action produces no second anything: the window reads, it does not run. Clicking
        /// the same control ten times leaves the same state it started in.
        /// </summary>
        [WpfFact]
        public void Repeated_clicks_change_nothing()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Complete(),
            };

            string text = partOIteration3ResultWindow.CopyAllText();

            for (int i = 0; i < 10; i++)
            {
                Click(partOIteration3ResultWindow, "button_OpenFolder");
                Click(partOIteration3ResultWindow, "button_OpenReport");
            }

            Assert.Equal(text, partOIteration3ResultWindow.CopyAllText());
            Assert.Single(partOIteration3ResultWindow.Rows_Visible);
        }

        /// <summary>
        /// Opening and closing the window repeatedly leaves nothing behind - the state a person reaches by
        /// reviewing, closing, and reviewing again.
        /// </summary>
        [WpfFact]
        public void The_window_can_be_opened_and_closed_repeatedly()
        {
            PartOIteration3Result partOIteration3Result = Result_Complete();

            for (int i = 0; i < 5; i++)
            {
                PartOIteration3ResultWindow partOIteration3ResultWindow = new()
                {
                    Result = partOIteration3Result,
                };

                Assert.Single(partOIteration3ResultWindow.Rows_Visible);

                Click(partOIteration3ResultWindow, "button_Close");

                partOIteration3ResultWindow.Close();
            }
        }

        /// <summary>
        /// A filter and a search, then clearing both, over a refused pairing where the controls are
        /// collapsed - the state in which a filter has no rows to filter.
        /// </summary>
        [WpfFact]
        public void Filtering_and_searching_a_refused_pairing_produces_no_rows_and_no_exception()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Control<CheckBox>(partOIteration3ResultWindow, "checkBox_Changed").IsChecked = true;
            Control<CheckBox>(partOIteration3ResultWindow, "checkBox_Failures").IsChecked = true;
            Control<TextBox>(partOIteration3ResultWindow, "textBox_Search").Text = "bedroom";

            Assert.Empty(partOIteration3ResultWindow.Rows_Visible);

            Control<CheckBox>(partOIteration3ResultWindow, "checkBox_Changed").IsChecked = false;
            Control<CheckBox>(partOIteration3ResultWindow, "checkBox_Failures").IsChecked = false;
            Control<TextBox>(partOIteration3ResultWindow, "textBox_Search").Text = string.Empty;

            Assert.Empty(partOIteration3ResultWindow.Rows_Visible);
        }

        //-------------------------------------------------------------------------------------------------
        //Historical versus current
        //-------------------------------------------------------------------------------------------------

        /// <summary>
        /// A refused review offers the last successful report - under a name and a tooltip that say it is
        /// historical - and says the same thing in the refusal text itself, where a person reading the
        /// refusal will actually meet it.
        /// </summary>
        [WpfFact]
        public void A_refusal_offers_the_last_successful_report_and_labels_it_historical()
        {
            string path = WriteHistoricalReport();

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Button button = Control<Button>(partOIteration3ResultWindow, "button_OpenReport");

            Assert.True(button.IsEnabled);
            Assert.Equal("Last successful report", button.Content);
            Assert.Contains("HISTORICAL", (string)button.ToolTip);
            Assert.Contains(path, (string)button.ToolTip);

            string text = Control<TextBlock>(partOIteration3ResultWindow, "textBlock_Refusal").Text;

            Assert.Contains("HISTORICAL", text);
            Assert.Contains("does NOT describe the analytical model in front of you", text);
            Assert.Contains(path, text);

            //And it is still a refusal: no Candidate B number is presented anywhere.
            Assert.Empty(partOIteration3ResultWindow.Rows_Visible);
            Assert.DoesNotContain("Bias B-A", partOIteration3ResultWindow.CopyAllText());
        }

        /// <summary>
        /// With no successful report on disk, a refusal offers nothing and claims nothing - no button, and
        /// no sentence about a file that is not there.
        /// </summary>
        [WpfFact]
        public void A_refusal_with_no_saved_report_offers_none_and_claims_none()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Assert.False(Control<Button>(partOIteration3ResultWindow, "button_OpenReport").IsEnabled);
            Assert.DoesNotContain("HISTORICAL", Control<TextBlock>(partOIteration3ResultWindow, "textBlock_Refusal").Text);
        }

        /// <summary>
        /// A completed review offers its own report under its own name - not "last successful", which
        /// would read as history for a result that is current.
        /// </summary>
        [WpfFact]
        public void A_completed_review_offers_its_own_report_under_its_own_name()
        {
            string path = WriteHistoricalReport();

            PartOIteration3Result partOIteration3Result = Result_Complete();

            partOIteration3Result.RecordReport(path, Path.ChangeExtension(path, "json"), null);

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = partOIteration3Result,
            };

            Button button = Control<Button>(partOIteration3ResultWindow, "button_OpenReport");

            Assert.True(button.IsEnabled);
            Assert.Equal("Comparison report", button.Content);
            Assert.DoesNotContain("HISTORICAL", (string)button.ToolTip);

            //And the notes name both files, so the path is on screen as well as behind a button.
            string notes = Control<TextBox>(partOIteration3ResultWindow, "textBox_Diagnostics").Text;

            Assert.Contains(path, notes);
        }

        /// <summary>
        /// A report that has been deleted since the window opened disables its own button rather than
        /// handing the shell a path that is not there.
        /// </summary>
        [WpfFact]
        public void A_report_deleted_after_the_window_opened_disables_its_button_on_the_click()
        {
            string path = WriteHistoricalReport();

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = Result_Refused(),
            };

            Button button = Control<Button>(partOIteration3ResultWindow, "button_OpenReport");

            Assert.True(button.IsEnabled);

            File.Delete(path);

            Click(partOIteration3ResultWindow, "button_OpenReport");

            Assert.False(button.IsEnabled);
            Assert.Equal("Last successful report", button.Content);
        }

        /// <summary>
        /// The report a window offers and the report the writer produces are the same file: both derive it
        /// from the pairing record, so a rename of one cannot silently orphan the other.
        /// </summary>
        [Fact]
        public void The_report_path_is_derived_from_the_pairing_record()
        {
            Assert.Equal(
                Path.Combine("C:\\out", "Flat-Iteration3-Review.txt"),
                PartOIteration3Paths.Path_Report_ForRecord(Path.Combine("C:\\out", "Flat-Iteration3.json")));

            Assert.Equal(
                Path.Combine("C:\\out", "Flat-Iteration3-Review.json"),
                PartOIteration3Paths.Path_Report_ForRecord(Path.Combine("C:\\out", "Flat-Iteration3.json"), "json"));

            Assert.Null(PartOIteration3Paths.Path_Report_ForRecord(null));
            Assert.Null(PartOIteration3Paths.Path_Report_ForRecord("   "));
        }

        /// <summary>
        /// The window renders what the report authority composes - so the copy on screen and the file on
        /// disk cannot drift apart.
        /// </summary>
        [WpfFact]
        public void The_window_renders_the_same_text_the_report_composes()
        {
            PartOIteration3Result partOIteration3Result = Result_Complete();

            PartOIteration3ResultWindow partOIteration3ResultWindow = new()
            {
                Result = partOIteration3Result,
            };

            Assert.Equal(PartOIteration3ReportText.Outcome(partOIteration3Result), Control<TextBlock>(partOIteration3ResultWindow, "textBlock_Outcome").Text);
            Assert.Equal(PartOIteration3ReportText.Summary(partOIteration3Result), Control<TextBlock>(partOIteration3ResultWindow, "textBlock_Summary").Text);
            Assert.Equal(PartOIteration3ReportText.Diagnostics(partOIteration3Result), Control<TextBox>(partOIteration3ResultWindow, "textBox_Diagnostics").Text);
        }

        /// <summary>
        /// A window with no result at all - the state a host reaches by constructing it and not setting
        /// one. Nothing renders, and nothing throws.
        /// </summary>
        [WpfFact]
        public void A_window_with_no_result_renders_nothing_and_survives_every_click()
        {
            PartOIteration3ResultWindow partOIteration3ResultWindow = new();

            foreach (string name in new[] { "button_ReportA", "button_ReportB", "button_OpenReport", "button_OpenFolder", "button_CopyAll", "button_Close" })
            {
                Click(partOIteration3ResultWindow, name);
            }

            Assert.Null(partOIteration3ResultWindow.Result);
        }
    }
}
