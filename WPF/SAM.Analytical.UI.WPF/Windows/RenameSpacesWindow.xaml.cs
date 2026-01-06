// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for RenameSpacesWindow.xaml
    /// </summary>
    public partial class RenameSpacesWindow : System.Windows.Window
    {
        public RenameSpacesWindow(UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            InitializeComponent();

            renameSpacesControl.UIAnalyticalModel = uIAnalyticalModel;
            renameSpacesControl.Spaces = spaces == null ? null : new List<Space>(spaces);
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            renameSpacesControl.Apply();

            DialogResult = true;
            Close();
        }

        private void button_Apply_Click(object sender, RoutedEventArgs e)
        {
            renameSpacesControl.Apply();
        }
    }
}
