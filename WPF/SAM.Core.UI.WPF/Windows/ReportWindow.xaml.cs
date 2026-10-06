// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Read-only, scrollable plain-text report viewer, for callers whose report text can be too long
    /// for a MessageBox (which has no scrollbar and is impractical past a few lines) - e.g. a model
    /// with thousands of spaces. Generic and presentation-only: it has no knowledge of what produced
    /// the text, so it carries none of the caller's domain or regulatory logic.
    /// </summary>
    public partial class ReportWindow : Window
    {
        public ReportWindow()
        {
            InitializeComponent();
        }

        public ReportWindow(string title, string text)
            : this()
        {
            if (!string.IsNullOrEmpty(title))
            {
                Title = title;
            }

            Text = text;
        }

        public string Text
        {
            get
            {
                return TextBox_Main.Text;
            }

            set
            {
                TextBox_Main.Text = value;
            }
        }

        private void Button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            string text = TextBox_Main.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (TrySetClipboardText(text))
            {
                return;
            }

            //Select the whole report and focus it, so Ctrl+C is one keystroke away - the TextBox's own
            //copy command is a different code path from the one that just failed.
            TextBox_Main.Focus();
            TextBox_Main.SelectAll();

            //Where the clipboard is blocked for the whole process rather than momentarily busy - a
            //virtual machine or remote-desktop session with clipboard sharing off, or a process running
            //at a different integrity level from the clipboard's owner - Ctrl+C will fail too. Saving to
            //a file does not touch the clipboard at all, so it always gets the report out.
            MessageBoxResult messageBoxResult = MessageBox.Show(
                this,
                "Could not copy the report to the clipboard - another application is holding it open.\n\nThe whole report has been selected, so you can press Ctrl+C to try copying it directly.\n\nSave the report to a file instead?",
                "Copy All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                SaveReport(text);
            }
        }

        private void SaveReport(string text)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Title = "Save Report",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = "Report.txt",
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                System.IO.File.WriteAllText(saveFileDialog.FileName, text);
            }
            catch (System.Exception exception) when (exception is System.IO.IOException || exception is System.UnauthorizedAccessException)
            {
                MessageBox.Show(this, "Could not save the report: " + exception.Message, "Save Report", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// The Windows clipboard is held open by other processes (clipboard managers, remote desktop
        /// and virtual-machine clipboard sharing, antivirus scanners) far more often than most apps
        /// expect, and the set then fails with CLIPBRD_E_CANT_OPEN. This application has no global
        /// unhandled-exception handler, so letting that escape from a button click crashes the whole
        /// app over a transient, retryable condition.
        /// <para>
        /// Three routes are tried, because a single retry loop over one API is demonstrably not enough:
        /// the WPF clipboard with a flush (survives this process exiting), then the WinForms clipboard
        /// (its own retry loop, and a different code path into OLE), then finally a set WITHOUT the
        /// flush - that data only lives as long as this process, but pasting it now is what the user
        /// actually asked for, and it is far better than failing.
        /// </para>
        /// <para>
        /// Every retry sleeps the UI thread, so the whole sequence is deliberately capped at roughly a
        /// quarter of a second. A longer, more determined retry was measurably worse: where the
        /// clipboard is blocked for the process rather than momentarily busy it never succeeds anyway,
        /// and the only thing the extra attempts bought was a frozen window before the same failure.
        /// </para>
        /// </summary>
        private static bool TrySetClipboardText(string text)
        {
            if (TrySetClipboardText_Wpf(text, copy: true, maxAttempts: 3, retryDelayMilliseconds: 40))
            {
                return true;
            }

            try
            {
                System.Windows.Forms.Clipboard.SetDataObject(text, true, 2, 40);
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
            }

            return TrySetClipboardText_Wpf(text, copy: false, maxAttempts: 2, retryDelayMilliseconds: 40);
        }

        private static bool TrySetClipboardText_Wpf(string text, bool copy, int maxAttempts, int retryDelayMilliseconds)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    DataObject dataObject = new DataObject();
                    dataObject.SetData(DataFormats.UnicodeText, text);
                    dataObject.SetData(DataFormats.Text, text);

                    Clipboard.SetDataObject(dataObject, copy);
                    return true;
                }
                //COMException derives from ExternalException, which is what the WinForms route throws;
                //catching the base covers both without letting an unrelated exception through.
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    if (attempt == maxAttempts)
                    {
                        return false;
                    }

                    System.Threading.Thread.Sleep(retryDelayMilliseconds);
                }
            }

            return false;
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
