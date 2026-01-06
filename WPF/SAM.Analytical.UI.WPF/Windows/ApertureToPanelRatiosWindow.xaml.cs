// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Windows;


namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureToPanelRatiosWindow.xaml
    /// </summary>
    public partial class ApertureToPanelRatiosWindow : System.Windows.Window
    {
        public ApertureToPanelRatiosWindow()
        {
            InitializeComponent();
        }

        public ApertureToPanelRatios ApertureToPanelRatios
        {
            get
            {
                return ApertureToPanelRatiosControl_Main.ApertureToPanelRatios;
            }

            set
            {
                ApertureToPanelRatiosControl_Main.ApertureToPanelRatios = value;
            }
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
