using SAM.Analytical.Tas;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for LayerThicknessCalculationResultControl.xaml
    /// </summary>
    public partial class LayerThicknessCalculationResultControl : UserControl
    {
        private ConstructionManager constructionManager;
        private LayerThicknessCalculationResult layerThicknessCalculationResult;

        public LayerThicknessCalculationResultControl()
        {
            InitializeComponent();
        }

        public LayerThicknessCalculationResult LayerThicknessCalculationResult
        {
            get
            {
                return layerThicknessCalculationResult;
            }

            set
            {
                SetLayerThicknessCalculationResult(value);
            }
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
                SetLayerThicknessCalculationResult(layerThicknessCalculationResult);
            }
        }

        private void SetLayerThicknessCalculationResult(LayerThicknessCalculationResult layerThicknessCalculationResult)
        {
            this.layerThicknessCalculationResult = layerThicknessCalculationResult;
            if (layerThicknessCalculationResult != null)
            {
                Construction construction = constructionManager.GetConstructions(layerThicknessCalculationResult.ConstructionName)?.FirstOrDefault();
                List<ConstructionLayer> constructionLayers = construction?.ConstructionLayers;

                ConstructionLayer constructionLayer = null;
                IMaterial material = null;
                if(constructionLayers != null && layerThicknessCalculationResult.LayerIndex != -1 && constructionLayers.Count > layerThicknessCalculationResult.LayerIndex)
                {
                    constructionLayer = constructionLayers[layerThicknessCalculationResult.LayerIndex];
                    string name = constructionLayer?.Name;
                    if(!string.IsNullOrEmpty(name))
                    {
                        material = constructionManager.GetMaterial(name);
                    }
                }
                
                TextBox_ThermalTransmittance.Text = layerThicknessCalculationResult.ThermalTransmittance.ToString();
                TextBox_ConstructionName.Text = layerThicknessCalculationResult.ConstructionName;
                TextBox_MaterialName.Text = material?.Name;

                TextBox_InitialThermalTransmittance.Text = layerThicknessCalculationResult.InitialThermalTransmittance.ToString();
                TextBox_InitialThickness.Text = construction == null ? null : Core.Query.Round(construction.GetThickness(), Tolerance.MacroDistance).ToString();
                TextBox_MaterialInitialThickness.Text = constructionLayer == null ? null : constructionLayer.Thickness.ToString();

                TextBox_CalculatedThermalTransmittance.Text = Core.Query.Round(layerThicknessCalculationResult.CalculatedThermalTransmittance, Tolerance.MacroDistance).ToString();
                TextBox_CalculatedThickness.Text = construction == null || constructionLayer == null ? null : Core.Query.Round((construction.GetThickness() + layerThicknessCalculationResult.Thickness - constructionLayer.Thickness), Tolerance.MacroDistance).ToString();
                TextBox_MaterialCalculatedThickness.Text = Core.Query.Round(layerThicknessCalculationResult.Thickness, Tolerance.MacroDistance).ToString();
            }
        }
    }
}
