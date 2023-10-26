using HoneybeeSchema.Energy;
using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public class DisplayLayerThicknessCalculationResult
    {
        private ConstructionManager constructionManager;
        private LayerThicknessCalculationResult layerThicknessCalculationResult;

        public bool Selected { get; set; } = true;

        public DisplayLayerThicknessCalculationResult(ConstructionManager constructionManager, LayerThicknessCalculationResult layerThicknessCalculationResult)
        {
            this.layerThicknessCalculationResult = layerThicknessCalculationResult;
            this.constructionManager = constructionManager;
        }

        public string ConstructionName
        {
            get
            {
                return layerThicknessCalculationResult.ConstructionName;
            }
        }

        public int LayerIndex
        {
            get 
            {
                return layerThicknessCalculationResult.LayerIndex;
            }
        }

        public double ThermalTransmittance
        {
            get
            {
                return layerThicknessCalculationResult.ThermalTransmittance;
            }
        }

        public double CalculatedThermalTransmittance
        {
            get
            {
                return layerThicknessCalculationResult.CalculatedThermalTransmittance;
            }
        }

        public string MaterialName
        {
            get
            {
                return GetMaterial()?.Name;
            }
        }

        public double? Thickness
        {
            get
            {
                return double.IsNaN(layerThicknessCalculationResult.Thickness) ? (double?)null : SAM.Core.Query.Round(layerThicknessCalculationResult.Thickness, SAM.Core.Tolerance.MacroDistance);
            }
        }

        public Core.IMaterial GetMaterial()
        {
            return Tas.Query.Material(layerThicknessCalculationResult, constructionManager);
        }

        public LayerThicknessCalculationResult LayerThicknessCalculationResult
        {
            get
            {
                return layerThicknessCalculationResult;
            }
        }
    }
}