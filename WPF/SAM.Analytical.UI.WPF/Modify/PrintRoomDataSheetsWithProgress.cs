// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// The stages of Print Room Data Sheets as its progress window names them, one per stage that
        /// <see cref="UI.Modify.PrintRoomDataSheets(AnalyticalModel, string, IWin32Window, System.Action{int})"/>
        /// announces (<see cref="UI.Modify.PrintRoomDataSheetsStages"/>).
        /// </summary>
        internal static readonly IReadOnlyList<string> PrintRoomDataSheetsStageNames = ["Collect the room data", "Write the data to the RDS workbook", "Print the room data sheets (Excel)", "Finish"];

        /// <summary>
        /// The ribbon's Print Room Data Sheets: the same work as
        /// <see cref="UI.Modify.PrintRoomDataSheets(UIAnalyticalModel, IWin32Window)"/>, with its stages drawn in the
        /// SAM progress-dialog style (documentation/ProgressDialogPattern.md) instead of the plain "Print RDS" window.
        /// <para>
        /// Only the look changes. The window is driven exactly as the "Print RDS" <c>SAM.Core.Windows.WPF.ProgressWindow</c>
        /// it replaces: created on the calling (UI) thread when the first stage starts, shown modelessly with no owner,
        /// not topmost, not in the taskbar; on each stage it is activated and the dispatcher is pumped once so it
        /// repaints; it closes when the work returns or throws. Excel reports no progress and the work cannot stop
        /// part-way, so there is no percentage and no Cancel, and the note says both.
        /// </para>
        /// </summary>
        public static void PrintRoomDataSheetsWithProgress(this UIAnalyticalModel? uIAnalyticalModel, IWin32Window? owner = null)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            string? directory = uIAnalyticalModel!.Path;
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directory = System.IO.Path.GetDirectoryName(directory);
            }

            int spaceCount = analyticalModel.GetSpaces()?.Count ?? 0;

            PartOProgressState partOProgressState = new(PrintRoomDataSheetsStageNames);
            PartOProgressWindow? partOProgressWindow = null;
            try
            {
                UI.Modify.PrintRoomDataSheets(analyticalModel, directory, owner, index =>
                {
                    if (partOProgressWindow == null)
                    {
                        partOProgressWindow = PrintRoomDataSheetsWindow(partOProgressState, spaceCount, directory);
                        partOProgressWindow.Show();
                    }

                    partOProgressState.Start(index);
                    partOProgressWindow.Render();
                    partOProgressWindow.Activate();

                    DoEvents();
                });
            }
            finally
            {
                partOProgressWindow?.Close();
            }
        }

        /// <summary>
        /// The Print RDS progress window, not yet shown: the shared progress style, with the replaced window's
        /// title and window behaviour (no owner, not topmost, not in the taskbar, centred on screen) and no Cancel.
        /// </summary>
        internal static PartOProgressWindow PrintRoomDataSheetsWindow(PartOProgressState partOProgressState, int spaceCount, string? directory)
        {
            return new PartOProgressWindow()
            {
                Title = "Print RDS",
                Topmost = false,
                ShowInTaskbar = false,
                Heading = "Print Room Data Sheets",
                Subheading = string.Format(CultureInfo.CurrentCulture, "{0:N0} {1} → {2}", spaceCount, spaceCount == 1 ? "Space" : "Spaces", directory),
                Cancellable = false,
                State = partOProgressState,
            };
        }

        /// <summary>
        /// Pumps the dispatcher down to Background priority so the window repaints between steps - what the replaced
        /// <c>ProgressWindow.Update</c> does.
        /// </summary>
        private static void DoEvents()
        {
            DispatcherFrame dispatcherFrame = new();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(x =>
                {
                    ((DispatcherFrame)x).Continue = false;
                    return null;
                }),
                dispatcherFrame);
            Dispatcher.PushFrame(dispatcherFrame);
        }
    }
}
