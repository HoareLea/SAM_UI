// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// "Export Space reports...": opens <see cref="SpaceReportPdfBatchWindow"/> for the model, with the scope
        /// defaulting to <paramref name="spaces"/> when some are selected and to All Spaces otherwise. The one-Space
        /// commands (<see cref="CreateSpaceReportPdf(UIAnalyticalModel?, IEnumerable{Space}?, SpaceReportPdf, System.Windows.Window?)"/>)
        /// are unchanged.
        /// </summary>
        public static void ExportSpaceReportPdfs(this UIAnalyticalModel? uIAnalyticalModel, IEnumerable<Space>? spaces, System.Windows.Window? owner = null)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                string text = "Open an analytical model first.";
                if (owner == null)
                {
                    MessageBox.Show(text, "Export Space reports", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(owner, text, "Export Space reports", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return;
            }

            SpaceReportPdfBatchWindow spaceReportPdfBatchWindow = new SpaceReportPdfBatchWindow(analyticalModel, spaces, SpaceReportPdfBatchDirectory(uIAnalyticalModel?.Path));
            if (owner != null)
            {
                spaceReportPdfBatchWindow.Owner = owner;
            }
            else
            {
                spaceReportPdfBatchWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            spaceReportPdfBatchWindow.ShowDialog();
        }

        /// <summary>
        /// The suggested batch folder: "&lt;model name&gt; Space reports" beside the saved model, so thousands of PDFs
        /// do not land loose next to it. Null for an unsaved model: the user chooses.
        /// </summary>
        internal static string? SpaceReportPdfBatchDirectory(string? modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return null;
            }

            try
            {
                string? directory = Path.GetDirectoryName(modelPath);
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    return null;
                }

                return Path.Combine(directory, Path.GetFileNameWithoutExtension(modelPath) + " Space reports");
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
