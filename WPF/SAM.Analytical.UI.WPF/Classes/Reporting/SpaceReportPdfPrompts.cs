// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The user interaction of the one-Space report PDF command: the Save dialog, message boxes and opening the saved
    /// PDF. The command builds the real ones from its owner window (<see cref="Dialogs"/>); tests pass stand-ins so the
    /// workflow (refusal, cancel, failure, success, open) runs without a desktop.
    /// </summary>
    internal sealed class SpaceReportPdfPrompts
    {
        /// <summary>(dialog title, default file name, initial folder or null) → the chosen path, or null when cancelled.</summary>
        public Func<string, string, string?, string?> ChoosePath { get; init; } = (_, _, _) => null;

        /// <summary>(text, caption, buttons, image) → the button pressed.</summary>
        public Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> ShowMessage { get; init; } = (_, _, _, _) => MessageBoxResult.None;

        /// <summary>Opens the saved PDF in the default viewer; throws when it cannot.</summary>
        public Action<string> Open { get; init; } = _ => { };

        public static SpaceReportPdfPrompts Dialogs(System.Windows.Window? owner)
        {
            return new SpaceReportPdfPrompts()
            {
                ChoosePath = (title, fileName, directory) =>
                {
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = title,
                        Filter = "PDF files (*.pdf)|*.pdf",
                        DefaultExt = ".pdf",
                        AddExtension = true,
                        OverwritePrompt = true,
                        FileName = fileName,
                    };

                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        saveFileDialog.InitialDirectory = directory;
                    }

                    bool? dialogResult = owner == null ? saveFileDialog.ShowDialog() : saveFileDialog.ShowDialog(owner);
                    return dialogResult == true && !string.IsNullOrWhiteSpace(saveFileDialog.FileName) ? saveFileDialog.FileName : null;
                },

                ShowMessage = (text, caption, messageBoxButton, messageBoxImage) => owner == null
                    ? MessageBox.Show(text, caption, messageBoxButton, messageBoxImage)
                    : MessageBox.Show(owner, text, caption, messageBoxButton, messageBoxImage),

                Open = path => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }),
            };
        }
    }
}
