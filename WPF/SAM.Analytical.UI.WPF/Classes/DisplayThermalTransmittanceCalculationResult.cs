using SAM.Analytical.Tas;
using System;

namespace SAM.Analytical.UI.WPF
{
    public class DisplayThermalTransmittanceCalculationResult
    {
        public ConstructionManager ConstructionManager { get; set; } = null;
        
        private ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult;

        public DisplayThermalTransmittanceCalculationResult(ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult)
        {
            this.thermalTransmittanceCalculationResult = thermalTransmittanceCalculationResult;
        }

        public ThermalTransmittanceCalculationResult ThermalTransmittanceCalculationResult
        {
            get
            {
                return thermalTransmittanceCalculationResult;
            }
        }

        public Construction Construction
        {
            get
            {
                if (thermalTransmittanceCalculationResult == null || ConstructionManager == null)
                {
                    return null;
                }

                if (!Guid.TryParse(thermalTransmittanceCalculationResult.Reference, out Guid guid))
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
                if (thermalTransmittanceCalculationResult == null || ConstructionManager == null)
                {
                    return null;
                }

                if (!Guid.TryParse(thermalTransmittanceCalculationResult.Reference, out Guid guid))
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
                return thermalTransmittanceCalculationResult == null || double.IsNaN(thermalTransmittanceCalculationResult.TotalSolarEnergyTransmittance) ? null : SAM.Core.Query.Round(thermalTransmittanceCalculationResult.TotalSolarEnergyTransmittance, 0.001);
            }
        }

        public double? LightTransmittance
        {
            get
            {
                return thermalTransmittanceCalculationResult == null || double.IsNaN(thermalTransmittanceCalculationResult.LightTransmittance) ? null : SAM.Core.Query.Round(thermalTransmittanceCalculationResult.LightTransmittance, 0.001);
            }
        }

        public double? ThermalTransmittance
        {
            get
            {
                double? result = null;

                Construction construction = Construction;
                if (construction != null)
                {
                    bool transparent = construction.Transparent(ConstructionManager?.MaterialLibrary);
                    if(transparent)
                    {
                        result = thermalTransmittanceCalculationResult?.GetTransparentThermalTransmittance();
                    }
                    else
                    {
                        PanelType panelType = construction.PanelType();
                        if (panelType == PanelType.Undefined)
                        {
                            panelType = PanelType.WallExternal;
                        }

                        result = thermalTransmittanceCalculationResult?.GetThermalTransmittance(panelType);
                    } 
                }
                else
                {
                    ApertureConstruction apertureConstruction = ApertureConstruction;
                    if (apertureConstruction != null)
                    {
                        result = thermalTransmittanceCalculationResult?.GetTransparentThermalTransmittance();
                    }
                }

                if(result != null && result.HasValue)
                {
                    if(double.IsNaN(result.Value))
                    {
                        result = null;
                    }
                    else
                    {
                        result = Core.Query.Round(result.Value, 0.001);
                    }
                }

                return result;
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
    }
}