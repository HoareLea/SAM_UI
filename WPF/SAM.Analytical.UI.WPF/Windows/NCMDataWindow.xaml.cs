// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for NCMDataWindow.xaml
    /// </summary>
    public partial class NCMDataWindow : System.Windows.Window
    {
        public NCMDataWindow()
        {
            InitializeComponent();
        }

        public List<NCMData> NCMDatas
        {
            get
            {
                return NCMDataControl_Main.NCMDatas;
            }

            set
            {
                NCMDataControl_Main.NCMDatas = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
