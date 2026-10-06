// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Results &gt; Part O &gt; Remove Results: what SAM removes from a copy of the open model, what it leaves, and the
    /// Mixed Design baseline check of that copy - PASS or FAIL with every reason, never truncated. The one action is
    /// <i>Save cleaned copy...</i> to a new file; nothing here writes over the open model. Once saved, the check is the
    /// saved file's, and <i>Open cleaned copy</i> opens it.
    /// </summary>
    public partial class PartORemoveResultsWindow : System.Windows.Window
    {
        private readonly PartORemoveResults partORemoveResults;
        private readonly string? path_Model;
        private string? refusal;

        public PartORemoveResultsWindow(PartORemoveResults partORemoveResults, string? path_Model)
        {
            InitializeComponent();

            this.partORemoveResults = partORemoveResults;
            this.path_Model = path_Model;

            button_Save.Click += (s, e) => Save();
            button_Open.Click += (s, e) => DialogResult = true;

            Refresh();
        }

        /// <summary>
        /// Asks for the new file, given the offered path; null when the person cancels. The command supplies the save
        /// dialog; tests supply a path.
        /// </summary>
        public Func<string?, string?>? ChooseFile { get; set; }

        /// <summary>The saved copy to open, when the person chose <i>Open cleaned copy</i>.</summary>
        public string? Path_Open => DialogResult == true ? partORemoveResults.Path_Saved : null;

        internal void Save()
        {
            string? path = ChooseFile?.Invoke(PartORemoveResults.DefaultPath(path_Model));
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            refusal = partORemoveResults.Save(path, path_Model);

            Refresh();
        }

        private void Refresh()
        {
            bool saved = partORemoveResults.Path_Saved is not null;

            textBlock_Heading.Text = partORemoveResults.HasChanges
                ? "Remove Part O results and preparation from a copy of this model"
                : "Nothing to remove from this model";

            itemsControl_Removed.ItemsSource = partORemoveResults.Removed;
            textBlock_NothingRemoved.Visibility = partORemoveResults.HasChanges ? Visibility.Collapsed : Visibility.Visible;

            itemsControl_Kept.ItemsSource = partORemoveResults.Kept;
            stackPanel_Kept.Visibility = partORemoveResults.Kept.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            // ---- the check: SAM's validator, of the saved file once there is one ----------------------------------

            List<PartOMaterialisationRefusal> findings = partORemoveResults.Findings_Saved ?? partORemoveResults.Findings;
            string subject = saved ? "the saved copy" : partORemoveResults.HasChanges ? "the cleaned copy" : "this model";
            bool pass = partORemoveResults.Cleaned is not null && findings.Count == 0;

            textBlock_CheckGlyph.Text = pass ? "✓" : "✕";
            textBlock_CheckGlyph.Foreground = (Brush)FindResource(pass ? "PartO.Brush.Success" : "PartO.Brush.Danger");
            textBlock_Check.Text = pass
                ? string.Format("PASS — {0} is a clean Part O baseline, so Mixed Design accepts it.", subject)
                : string.Format("FAIL — Mixed Design still refuses {0}. SAM says:", subject);
            textBlock_Check.Foreground = textBlock_CheckGlyph.Foreground;
            itemsControl_Findings.ItemsSource = findings.Select(x => string.Format("[{0}] {1}", Core.Query.Description(x.Reason), x.Message)).ToList();

            // ---- saving -------------------------------------------------------------------------------------------

            if (refusal is not null)
            {
                textBlock_Saved.Text = refusal;
                textBlock_Saved.Foreground = (Brush)FindResource("PartO.Brush.Danger");
            }
            else
            {
                textBlock_Saved.Text = saved ? string.Format("Saved to {0}", partORemoveResults.Path_Saved) : string.Empty;
                textBlock_Saved.Foreground = (Brush)FindResource("PartO.Brush.Muted");
            }

            textBlock_Saved.Visibility = string.IsNullOrEmpty(textBlock_Saved.Text) ? Visibility.Collapsed : Visibility.Visible;

            button_Save.IsEnabled = partORemoveResults.HasChanges && !saved;
            button_Save.IsDefault = button_Save.IsEnabled;
            button_Save.ToolTip = !partORemoveResults.HasChanges
                ? "The model carries no Part O results or preparation, so there is no cleaned copy to save."
                : saved ? "The cleaned copy has been saved." : "Saves the cleaned copy as a new file. The open model's own file is never overwritten.";

            button_Open.Visibility = saved ? Visibility.Visible : Visibility.Collapsed;
            button_Open.IsDefault = saved;

            button_Close.Content = saved ? "Close" : "Cancel";
        }
    }
}
