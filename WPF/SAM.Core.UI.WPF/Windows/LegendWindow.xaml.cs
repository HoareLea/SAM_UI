// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for LegendWindow.xaml
    /// </summary>
    public partial class LegendWindow : Window
    {
        public LegendWindow()
        {
            InitializeComponent();
        }

        public LegendWindow(Legend legend)
        {
            InitializeComponent();

            Legend = legend;
        }

        public Legend Legend
        {
            get
            {
                return legendControl.Legend;
            }

            set
            {
                legendControl.Legend = value;
            }
        }

        public LegendItem UndefinedLegendItem
        {
            get
            {
                return legendControl.UndefinedLegendItem;
            }

            set
            {
                legendControl.UndefinedLegendItem = value;
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
