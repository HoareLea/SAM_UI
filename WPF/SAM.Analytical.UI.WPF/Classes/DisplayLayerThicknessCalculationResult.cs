using SAM.Analytical.Tas;

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

        public string MaterialName
        {
            get
            {
                return GetMaterial()?.Name;
            }
        }

        public double InitialThermalTransmittance
        {
            get
            {
                return layerThicknessCalculationResult.InitialThermalTransmittance;
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

        public double? InitialThickness
        {
            get
            {
                ConstructionLayer constructionLayer = layerThicknessCalculationResult?.ConstructionLayer(constructionManager);
                if(constructionLayer == null)
                {
                    return null;
                }

                return constructionLayer.Thickness;
            }
        }

        public double? Thickness
        {
            get
            {
                return double.IsNaN(layerThicknessCalculationResult.Thickness) ? (double?)null : Core.Query.Round(layerThicknessCalculationResult.Thickness, Core.Tolerance.MacroDistance);
            }
        }

        public double? InitialConstructionThickness
        {
            get
            {
                Construction construction = layerThicknessCalculationResult?.Construction(constructionManager);
                if(construction == null)
                {
                    return null;
                }

                return construction.GetThickness();
            }
        }

        public double? ConstructionThickness
        {
            get
            {
                Construction construction = layerThicknessCalculationResult?.Construction(constructionManager);
                if (construction == null)
                {
                    return null;
                }



                return construction.GetThickness() - InitialThickness + Thickness;
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