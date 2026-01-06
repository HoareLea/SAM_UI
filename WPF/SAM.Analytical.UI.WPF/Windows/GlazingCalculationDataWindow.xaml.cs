// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for GlazingCalculationWindow.xaml
    /// </summary>
    public partial class GlazingCalculationDataWindow : System.Windows.Window
    {
        public GlazingCalculationDataWindow()
        {
            InitializeComponent();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return GlazingCalculationControl_Main.ConstructionManager;
            }

            set
            {
                GlazingCalculationControl_Main.ConstructionManager = value;
            }
        }

        public GlazingCalculationData GlazingCalculationData
        {
            get
            {
                return GlazingCalculationControl_Main.GlazingCalculationData;
            }

            set
            {
                GlazingCalculationControl_Main.GlazingCalculationData = value;
            }
        }
    }
}
