// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

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
        /// <see cref="UI.Modify.PrintRoomDataSheets(UIAnalyticalModel, IWin32Window)"/>, with its stages shown in the
        /// SAM progress-dialog pattern (documentation/ProgressDialogPattern.md) instead of the "Print RDS" window.
        /// The work holds the application's thread in Excel, so the window is a <see cref="PartOProgressHost"/> on its
        /// own thread and keeps painting. Excel reports no progress and the work cannot stop part-way, so the bar is
        /// indeterminate and there is no Cancel; the note says both. The window opens only once the work starts - the
        /// early returns (no folder, no template, no Spaces) open nothing, as before.
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

            PartOProgressHost? partOProgressHost = null;
            try
            {
                UI.Modify.PrintRoomDataSheets(analyticalModel, directory, owner, index =>
                {
                    partOProgressHost ??= new PartOProgressHost(
                        "Print Room Data Sheets",
                        string.Format(CultureInfo.CurrentCulture, "{0:N0} {1} → {2}", spaceCount, spaceCount == 1 ? "Space" : "Spaces", directory),
                        PrintRoomDataSheetsStageNames,
                        cancellable: false,
                        title: "Print RDS");

                    partOProgressHost.Start(index);
                });

                partOProgressHost?.State.Complete();
            }
            finally
            {
                partOProgressHost?.Dispose();
            }
        }
    }
}
