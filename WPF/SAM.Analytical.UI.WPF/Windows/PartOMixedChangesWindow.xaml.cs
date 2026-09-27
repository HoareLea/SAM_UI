// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What <i>Apply suggestions</i> would change, shown before anything changes. Cancel is the default: a suggestion
    /// reaches the selection only by a deliberate click on Apply.
    /// </summary>
    public partial class PartOMixedChangesWindow : System.Windows.Window
    {
        public PartOMixedChangesWindow(List<PartOMixedSelectionChange> changes)
        {
            InitializeComponent();

            int count = changes?.Count ?? 0;

            textBlock_Heading.Text = string.Format("Apply screening suggestions to {0}?", UI.Query.PartOCount(count, "dwelling", "dwellings"));
            dataGrid_Changes.ItemsSource = changes;
            button_Apply.Content = string.Format("Apply {0}", UI.Query.PartOCount(count, "change", "changes"));
            button_Apply.Click += (s, e) => DialogResult = true;
        }
    }
}
