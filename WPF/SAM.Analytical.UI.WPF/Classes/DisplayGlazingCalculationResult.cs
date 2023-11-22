using SAM.Analytical.Tas;
using System;

namespace SAM.Analytical.UI.WPF
{
    public class DisplayGlazingCalculationResult
    {
        public ConstructionManager ConstructionManager { get; set; } = null;
        public GlazingCalculationData GlazingCalculationData { get; set; } = null;
        
        private GlazingCalculationResult glazingCalculationResult;

        public int? Index { get; set; } = null;

        public DisplayGlazingCalculationResult(GlazingCalculationResult glazingCalculationResult)
        {
            this.glazingCalculationResult = glazingCalculationResult;
        }

        public GlazingCalculationResult GlazingCalculationResult
        {
            get
            {
                return glazingCalculationResult;
            }
        }

        public Construction Construction
        {
            get
            {
                if (glazingCalculationResult == null || ConstructionManager == null)
                {
                    return null;
                }

                if (!Guid.TryParse(glazingCalculationResult.Reference, out Guid guid))
                {
                    return null;
                }

                return ConstructionManager.Constructions?.Find(x => x.Guid == guid);
            }
        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                if (glazingCalculationResult == null || ConstructionManager == null)
                {
                    return null;
                }

                if (!Guid.TryParse(glazingCalculationResult.Reference, out Guid guid))
                {
                    return null;
                }

                return ConstructionManager.ApertureConstructions?.Find(x => x.Guid == guid);
            }
        }

        public string ConstructionName
        {
            get
            {
                Construction construction = Construction;
                if(construction != null)
                {
                    return construction.Name;
                }

                ApertureConstruction apertureConstruction = ApertureConstruction;
                if(apertureConstruction != null)
                {
                    return apertureConstruction.Name;
                }

                return null;
            }
        }

        public string Description
        {
            get
            {
                Construction construction = Construction;
                if (construction != null)
                {
                    return construction.GetValue<string>(ConstructionParameter.Description);
                }

                ApertureConstruction apertureConstruction = ApertureConstruction;
                if (apertureConstruction != null)
                {
                    return apertureConstruction.GetValue<string>(ApertureConstructionParameter.Description);
                }

                return null;
            }
        }

        public double? TotalSolarEnergyTransmittance
        {
            get
            {
                return glazingCalculationResult == null || double.IsNaN(glazingCalculationResult.TotalSolarEnergyTransmittance) ? null : SAM.Core.Query.Round(glazingCalculationResult.TotalSolarEnergyTransmittance, 0.001);
            }
        }

        public double? LightTransmittance
        {
            get
            {
                return glazingCalculationResult == null || double.IsNaN(glazingCalculationResult.LightTransmittance) ? null : SAM.Core.Query.Round(glazingCalculationResult.LightTransmittance, 0.001);
            }
        }

        public double? ThermalTransmittance
        {
            get
            {
                return glazingCalculationResult == null || double.IsNaN(glazingCalculationResult.ThermalTransmittance) ? null : SAM.Core.Query.Round(glazingCalculationResult.ThermalTransmittance, 0.001);
            }
        }

        /// <summary>
        /// Thickness [mm]
        /// </summary>
        public double? Thickness
        {
            get
            {
                Construction construction = Construction;
                if(construction != null)
                {
                    double result = construction.GetThickness();
                    return double.IsNaN(result) ? null : SAM.Core.Query.Round(result * 1000, 1);
                }

                ApertureConstruction apertureConstruction = ApertureConstruction;
                if(apertureConstruction != null)
                {
                    double result = apertureConstruction.GetThickness(AperturePart.Pane);
                    return double.IsNaN(result) ? null : SAM.Core.Query.Round(result * 1000, 1);
                }

                return null;
            }
        }

        public Enum Type
        {
            get
            {
                Construction construction = Construction;
                if (construction != null)
                {
                    PanelType panelType = construction.PanelType();
                    return panelType == PanelType.Undefined ? null : panelType;
                }

                ApertureConstruction apertureConstruction = ApertureConstruction;
                if (apertureConstruction != null)
                {
                    return apertureConstruction.ApertureType;
                }

                return null;
            }
        }

        public string TypeName
        {
            get
            {
                Enum type = Type;
                if(type == null)
                {
                    return null;
                }

                return type.ToString();
            }
        }

        public PanelGroup? PanelGroup
        {
            get
            {
                Construction construction = Construction;
                if (construction != null)
                {
                    PanelType panelType = construction.PanelType();
                    return panelType == PanelType.Undefined ? null : panelType.PanelGroup();
                }

                ApertureConstruction apertureConstruction = ApertureConstruction;
                if (apertureConstruction != null)
                {
                    return Analytical.PanelGroup.Undefined;
                }

                return null;
            }
        }

        public Criteria? Criteria
        {
            get
            {
                return GetCriteria();
            }
        }

        private Criteria? GetCriteria()
        {
            if(GlazingCalculationData == null || glazingCalculationResult == null || ConstructionManager == null)
            {
                return null;
            }

            Criteria result = Query.Criteria(ConstructionManager, GlazingCalculationData, glazingCalculationResult);
            if(result == WPF.Criteria.Undefined)
            {
                return null;
            }

            return result;
        }

        public bool IsMain()
        {
            if(GlazingCalculationData == null || glazingCalculationResult == null)
            {
                return false;
            }

            if(!Guid.TryParse(glazingCalculationResult.Reference, out Guid guid))
            {
                return false;
            }

            return GlazingCalculationData.ConstructionGuid == guid;
        }
    }
}