// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for LayerThicknessCalculationResultsWindow.xaml
    /// </summary>
    public partial class LayerThicknessCalculationResultsWindow : System.Windows.Window
    {
        public ConstructionManager ConstructionManager { get; set; }

        public LayerThicknessCalculationResultsWindow()
        {
            InitializeComponent();

            DataGridColumn_Select.Binding = new Binding("Selected");
            DataGridColumn_Construction.Binding = new Binding("ConstructionName");
            DataGridColumn_Material.Binding = new Binding("MaterialName");
            DataGridColumn_IntialMaterialThickness.Binding = new Binding("InitialThickness");
            DataGridColumn_CalculatedMaterialThickness.Binding = new Binding("Thickness");
            DataGridColumn_IntialConstructionThickness.Binding = new Binding("InitialConstructionThickness");
            DataGridColumn_CalculatedConstructionThickness.Binding = new Binding("ConstructionThickness");
            DataGridColumn_InitialThermalTransmittance.Binding = new Binding("InitialThermalTransmittance");
            DataGridColumn_ThermalTransmittance.Binding = new Binding("ThermalTransmittance");
            DataGridColumn_CalculatedThermalTransmittance.Binding = new Binding("CalculatedThermalTransmittance");
        }

        public List<LayerThicknessCalculationResult> LayerThicknessCalculationResults
        {
            set
            {
                SetLayerThicknessCalculationResults(value);
            }

            get
            {
                return GetLayerThicknessCalculationResults();
            }
        }

        private List<LayerThicknessCalculationResult> GetLayerThicknessCalculationResults()
        {
            IEnumerable enumerable = DataGrid_Main.ItemsSource;
            if(enumerable == null)
            {
                return null;
            }

            List<LayerThicknessCalculationResult> result = new List<LayerThicknessCalculationResult>();
            foreach(object @object in enumerable)
            {
                DisplayLayerThicknessCalculationResult displayLayerThicknessCalculationResult = @object as DisplayLayerThicknessCalculationResult;
                if(displayLayerThicknessCalculationResult == null)
                {
                    continue;
                }

                if(!displayLayerThicknessCalculationResult.Selected)
                {
                    continue;
                }

                result.Add(displayLayerThicknessCalculationResult.LayerThicknessCalculationResult);
            }

            return result;
        }

        private void SetLayerThicknessCalculationResults(IEnumerable<LayerThicknessCalculationResult> layerThicknessCalculationResults)
        {
            if(layerThicknessCalculationResults == null)
            {
                DataGrid_Main.ItemsSource = null;
                return;
            }

            List<DisplayLayerThicknessCalculationResult> displayLayerThicknessCalculationResults = new List<DisplayLayerThicknessCalculationResult>();
            foreach(LayerThicknessCalculationResult layerThicknessCalculationResult in layerThicknessCalculationResults)
            {
                displayLayerThicknessCalculationResults.Add(new DisplayLayerThicknessCalculationResult(ConstructionManager, layerThicknessCalculationResult));
            }

            DataGrid_Main.ItemsSource = displayLayerThicknessCalculationResults;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
