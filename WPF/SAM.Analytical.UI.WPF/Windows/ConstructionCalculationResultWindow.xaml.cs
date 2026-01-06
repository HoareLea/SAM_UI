// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConstructionCalculationResultWindow.xaml
    /// </summary>
    public partial class ConstructionCalculationResultWindow : System.Windows.Window
    {
        private ConstructionManager constructionManager;
        private IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult;

        public ConstructionCalculationResultWindow()
        {
            InitializeComponent();
        }

        private void Button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void Button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return constructionManager;
            }

            set
            {
                constructionManager = value;
                SetThermalTransmittanceCalculationResult(thermalTransmittanceCalculationResult);
            }
        }

        public IThermalTransmittanceCalculationResult ThermalTransmittanceCalculationResult
        {
            get
            {
                return thermalTransmittanceCalculationResult;
            }

            set
            {
                SetThermalTransmittanceCalculationResult(value);
            }
        }

        private void SetThermalTransmittanceCalculationResult(IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult)
        {
            DockPanel_Main.Children.Clear();

            if (thermalTransmittanceCalculationResult == null)
            {
                return;
            }

            if(thermalTransmittanceCalculationResult is LayerThicknessCalculationResult)
            {
                DockPanel_Main.Children.Add(new LayerThicknessCalculationResultControl() { ConstructionManager = constructionManager, LayerThicknessCalculationResult = (LayerThicknessCalculationResult)thermalTransmittanceCalculationResult });
            }
            else if(thermalTransmittanceCalculationResult is ConstructionCalculationResult)
            {
                DockPanel_Main.Children.Add(new ConstructionCalculationResultControl() { ConstructionManager = constructionManager, ConstructionCalculationResult = (ConstructionCalculationResult)thermalTransmittanceCalculationResult });
            }
            else if(thermalTransmittanceCalculationResult is ApertureConstructionCalculationResult)
            {
                DockPanel_Main.Children.Add(new ApertureConstructionCalculationResultControl() { ConstructionManager = constructionManager, ApertureConstructionCalculationResult = (ApertureConstructionCalculationResult)thermalTransmittanceCalculationResult });
            }
        }
    }
}
