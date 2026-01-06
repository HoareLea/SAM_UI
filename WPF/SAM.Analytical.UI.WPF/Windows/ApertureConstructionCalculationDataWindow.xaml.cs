// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureConstructionCalculationDataWindow.xaml
    /// </summary>
    public partial class ApertureConstructionCalculationDataWindow : System.Windows.Window
    {
        public ApertureConstructionCalculationDataWindow()
        {
            InitializeComponent();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return ApertureConstructionCalculationDataControl_Main.ConstructionManager;
            }

            set
            {
                ApertureConstructionCalculationDataControl_Main.ConstructionManager = value;
            }
        }

        public ApertureConstructionCalculationData ApertureConstructionCalculationData
        {
            get
            {
                return ApertureConstructionCalculationDataControl_Main.ApertureConstructionCalculationData;
            }

            set
            {
                ApertureConstructionCalculationDataControl_Main.ApertureConstructionCalculationData = value;
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            ApertureConstructionCalculationData apertureConstructionCalculationData = ApertureConstructionCalculationData;
            if (apertureConstructionCalculationData == null)
            {
                MessageBox.Show("Could not collect data.");
                return;
            }

            if (string.IsNullOrWhiteSpace(apertureConstructionCalculationData.ApertureConstructionName))
            {
                MessageBox.Show("Provide construction name.");
                return;
            }

            if (double.IsNaN(apertureConstructionCalculationData.PaneThermalTransmittance) && double.IsNaN(apertureConstructionCalculationData.FrameThermalTransmittance))
            {
                MessageBox.Show("Provide pane and/or frame thermal transmittance.");
                return;
            }

            DialogResult = true;

            Close();
        }
    }
}
