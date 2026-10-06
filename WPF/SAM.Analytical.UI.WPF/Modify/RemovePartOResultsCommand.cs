// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Results &gt; Part O &gt; Remove Results: a clean Part O baseline from a model that has been through Prepare
        /// &amp; Run, instead of hunting for an older <c>.sam</c>.
        /// <para>
        /// SAM removes the run output and the Part O preparation from a COPY (<c>Modify.RemovePartORunState</c>), and
        /// the copy is judged by the same validator Mixed Design asks (<c>Query.PartOBaselineFindings</c>) - see
        /// <see cref="PartORemoveResults"/>. The default action is <i>Save cleaned copy...</i> to a new file; the open
        /// model is never changed or overwritten, and no file is deleted from disk.
        /// </para>
        /// </summary>
        /// <param name="open">Opens the saved copy in this window, when the person asks for it.</param>
        public static void RemovePartOResults(this UIAnalyticalModel? uIAnalyticalModel, IWin32Window? owner, Func<string, bool>? open)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel is null)
            {
                return;
            }

            string? path_Model = uIAnalyticalModel!.Path;

            PartORemoveResults partORemoveResults = PartORemoveResults.Create(analyticalModel);

            PartORemoveResultsWindow partORemoveResultsWindow = new(partORemoveResults, path_Model)
            {
                ChooseFile = ChooseCleanedCopyFile,
            };

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partORemoveResultsWindow).Owner = owner.Handle;
            }

            if (partORemoveResultsWindow.ShowDialog() != true)
            {
                return;
            }

            string? path_Open = partORemoveResultsWindow.Path_Open;
            if (!string.IsNullOrWhiteSpace(path_Open))
            {
                open?.Invoke(path_Open!);
            }
        }

        private static string? ChooseCleanedCopyFile(string? path_Default)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Title = "Save cleaned copy",
                Filter = "SAM files (*.sam)|*.sam",
                FilterIndex = 1,
                DefaultExt = "sam",
                AddExtension = true,
                OverwritePrompt = true,
            };

            if (!string.IsNullOrWhiteSpace(path_Default))
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(path_Default);
                saveFileDialog.FileName = Path.GetFileName(path_Default);
            }

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }
    }
}
