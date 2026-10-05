// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Part O UX pass 6 - final consistency.</b> Only what that pass found and changed: Iteration 3's verdict
    /// words, the TM59 window opened as a bare report, and the Iteration 2B result window's height (monitor-aware).
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOFinalConsistencyTests
    {
        /// <summary>
        /// Iteration 3 said "Pass" / "Fail" / "Undefined" where the Hub, the TM59 window and the 2B result say
        /// PASS / FAIL / NOT ASSESSED. One word per status now, and for a pass or a fail it is the TM59 window's.
        /// </summary>
        [Theory]
        [InlineData(TM59ComplianceStatus.Pass, "PASS")]
        [InlineData(TM59ComplianceStatus.Fail, "FAIL")]
        [InlineData(TM59ComplianceStatus.Undefined, "NOT ASSESSED")]
        [InlineData(TM59ComplianceStatus.NotApplicable, "NOT ASSESSED")]
        public void AnIteration3Status_IsWordedAsEveryOtherPartOVerdict(TM59ComplianceStatus tM59ComplianceStatus, string expected)
        {
            Assert.Equal(expected, Query.PartOVerdictText(tM59ComplianceStatus));
        }

        /// <summary>
        /// Iteration 3's "TM59 report" buttons open the shared TM59 window over the report text alone - the
        /// Iteration 3 assessment holds no summary to head it with, and none may be parsed out of the text. It
        /// used to show an empty verdict band and an empty facts box above the report. Nothing is drawn now
        /// until a summary is given; given one, even an unavailable one, the band is back.
        /// </summary>
        [WpfFact]
        public void AReportWithNoSummary_IsNotShownUnderAnEmptyVerdict()
        {
            PartOTM59ResultWindow partOTM59ResultWindow = new()
            {
                Report = "REPORT",
            };

            Assert.Equal(Visibility.Collapsed, partOTM59ResultWindow.border_Verdict.Visibility);
            Assert.Equal(Visibility.Collapsed, partOTM59ResultWindow.border_Facts.Visibility);

            partOTM59ResultWindow.ResultSummary = PartOTM59ResultSummary.Unavailable("gone", null);

            Assert.Equal(Visibility.Visible, partOTM59ResultWindow.border_Verdict.Visibility);
            Assert.Equal("TM59 assessment — UNAVAILABLE", partOTM59ResultWindow.VerdictHeading);

            //No run, so no facts - and no empty box for them.
            Assert.Equal(Visibility.Collapsed, partOTM59ResultWindow.border_Facts.Visibility);

            partOTM59ResultWindow.Close();
        }

        /// <summary>
        /// The 2B result window was the one Part O result with no height ceiling: opening Engineering detail
        /// (a fixed-height tab set) could push Copy All and Close below the taskbar. Its content now scrolls with
        /// the buttons outside the scroll, and its height is capped by the Hub's monitor-aware placement once the
        /// window exists - not from the primary monitor in the constructor, which on a
        /// shorter secondary monitor left the buttons off screen.
        /// </summary>
        [WpfFact]
        public void The2BResultWindow_KeepsItsButtonsOutsideTheScroll_AndTakesNoPrimaryMonitorCap()
        {
            PartOOptimisationResultWindow partOOptimisationResultWindow = new();

            //No ceiling until the window knows which monitor it is on.
            Assert.True(double.IsPositiveInfinity(partOOptimisationResultWindow.MaxHeight));

            Assert.True(InsideScrollViewer(partOOptimisationResultWindow.expander_Detail));
            Assert.False(InsideScrollViewer(partOOptimisationResultWindow.button_Close));
            Assert.False(InsideScrollViewer(partOOptimisationResultWindow.button_CopyAll));

            partOOptimisationResultWindow.Close();
        }

        /// <summary>
        /// The arithmetic the window relies on, for the reported case: a window with no ceiling of its own
        /// (+infinity, the WPF default) on a secondary monitor shorter than the primary and below it. The ceiling
        /// is 92% of THAT monitor, and the window is moved up inside it.
        /// </summary>
        [Fact]
        public void OnAShorterSecondaryMonitor_TheCeilingIsThatMonitors()
        {
            //Secondary working area y 1080..1800 (720 high); window opened at y 1500, 700 high, minimum 360.
            (double top, double minHeight, double maxHeight) = PartOWorkflowWindow.Placement(1500, 700, 360, double.PositiveInfinity, 1080, 1800);

            Assert.Equal(720 * 0.92, maxHeight, 3);
            Assert.Equal(360, minHeight);
            Assert.Equal(1800 - 720 * 0.92, top, 3);
        }

        /// <summary>
        /// The real window, opened low on its monitor with Engineering detail open: once rendered, its ceiling
        /// is that monitor's (whichever it is on), and Close is still inside the working area.
        /// </summary>
        [WpfFact]
        public void The2BResultWindow_OpenedLowWithTheDetailOpen_StaysOnItsMonitor()
        {
            System.Windows.Rect rect = SystemParameters.WorkArea;

            PartOOptimisationResultWindow partOOptimisationResultWindow = new()
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowActivated = false,
                Left = rect.Left + 10,
                Top = rect.Bottom - 150,
            };

            partOOptimisationResultWindow.expander_Detail.IsExpanded = true;

            partOOptimisationResultWindow.Show();
            partOOptimisationResultWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            System.IntPtr handle = new System.Windows.Interop.WindowInteropHelper(partOOptimisationResultWindow).Handle;
            System.Drawing.Rectangle workingArea = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
            System.Windows.Media.Matrix matrix = PresentationSource.FromVisual(partOOptimisationResultWindow).CompositionTarget.TransformFromDevice;
            double areaTop = matrix.Transform(new Point(workingArea.Left, workingArea.Top)).Y;
            double areaBottom = matrix.Transform(new Point(workingArea.Right, workingArea.Bottom)).Y;

            Assert.Equal((areaBottom - areaTop) * 0.92, partOOptimisationResultWindow.MaxHeight, 1);
            Assert.True(partOOptimisationResultWindow.Top + partOOptimisationResultWindow.ActualHeight <= areaBottom + 1, string.Format("bottom {0} is below the working area {1}", partOOptimisationResultWindow.Top + partOOptimisationResultWindow.ActualHeight, areaBottom));

            partOOptimisationResultWindow.Close();
        }

        private static bool InsideScrollViewer(DependencyObject dependencyObject)
        {
            for (DependencyObject? parent = LogicalTreeHelper.GetParent(dependencyObject); parent is not null; parent = LogicalTreeHelper.GetParent(parent))
            {
                if (parent is ScrollViewer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
