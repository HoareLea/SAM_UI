using SAM.Analytical.Tas;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Criteria Criteria(this ConstructionManager constructionManager, GlazingCalculationData glazingCalculationData, GlazingCalculationResult glazingCalculationResult, double tolerance = Tolerance.Distance)
        {
            if (glazingCalculationData == null || glazingCalculationResult == null || constructionManager == null)
            {
                return WPF.Criteria.Undefined;
            }

            List<bool> criterias = new List<bool>() { true, true, true };

            double totalSolarEnergyTransmittance = glazingCalculationData.TotalSolarEnergyTransmittance;
            double totalSolarEnergyTransmittance_Min = totalSolarEnergyTransmittance;
            double totalSolarEnergyTransmittance_Max = totalSolarEnergyTransmittance;

            if (!double.IsNaN(totalSolarEnergyTransmittance))
            {
                if (glazingCalculationData.TotalSolarEnergyTransmittanceRange != null)
                {
                    double totalSolarEnergyTransmittance_Min_Temp = glazingCalculationData.TotalSolarEnergyTransmittanceRange.Min;
                    if (!double.IsNaN(totalSolarEnergyTransmittance_Min_Temp))
                    {
                        totalSolarEnergyTransmittance_Min = totalSolarEnergyTransmittance_Min_Temp;
                    }

                    double totalSolarEnergyTransmittance_Max_Temp = glazingCalculationData.TotalSolarEnergyTransmittanceRange.Max;
                    if (!double.IsNaN(totalSolarEnergyTransmittance_Max_Temp))
                    {
                        totalSolarEnergyTransmittance_Max = totalSolarEnergyTransmittance_Max_Temp;
                    }
                }

                criterias[0] = false;
            }

            double lightTransmittance = glazingCalculationData.LightTransmittance;
            double lightTransmittance_Min = lightTransmittance;
            double lightTransmittance_Max = lightTransmittance;

            if(!double.IsNaN(lightTransmittance))
            {
                if (glazingCalculationData.LightTransmittanceRange != null)
                {
                    double lightTransmittance_Min_Temp = glazingCalculationData.LightTransmittanceRange.Min;
                    if (!double.IsNaN(lightTransmittance_Min_Temp))
                    {
                        lightTransmittance_Min = lightTransmittance_Min_Temp;
                    }

                    double lightTransmittance_Max_Temp = glazingCalculationData.LightTransmittanceRange.Max;
                    if (!double.IsNaN(lightTransmittance_Max_Temp))
                    {
                        lightTransmittance_Max = lightTransmittance_Max_Temp;
                    }
                }

                criterias[1] = false;
            }

            double thickness_Min = double.NaN;
            double thickness_Max = double.NaN;

            if(glazingCalculationData.ThicknessRange != null)
            {
                thickness_Min = glazingCalculationData.ThicknessRange.Min;
                thickness_Max = glazingCalculationData.ThicknessRange.Max;

                criterias[2] = false;
            }

            if (!double.IsNaN(totalSolarEnergyTransmittance_Min) && !double.IsNaN(totalSolarEnergyTransmittance_Max) && !double.IsNaN(glazingCalculationResult.TotalSolarEnergyTransmittance))
            {
                Range<double> range = new Range<double>(totalSolarEnergyTransmittance + totalSolarEnergyTransmittance_Min, totalSolarEnergyTransmittance + totalSolarEnergyTransmittance_Max);
                criterias[0] = range.In(glazingCalculationResult.TotalSolarEnergyTransmittance, tolerance);
            }

            if (!double.IsNaN(lightTransmittance_Min) && !double.IsNaN(lightTransmittance_Max) && !double.IsNaN(glazingCalculationResult.LightTransmittance))
            {
                Range<double> range = new Range<double>(lightTransmittance + lightTransmittance_Min, lightTransmittance + lightTransmittance_Max);
                criterias[1] = range.In(glazingCalculationResult.LightTransmittance, tolerance);
            }

            double thickness = double.NaN;
            if(System.Guid.TryParse(glazingCalculationResult.Reference, out System.Guid guid))
            {
                Construction construction = constructionManager.Constructions?.Find(x => x.Guid == guid);
                if(construction != null)
                {
                    thickness = construction.GetThickness();
                }
                else
                {
                    ApertureConstruction apertureConstruction = constructionManager.ApertureConstructions?.Find(x => x.Guid == guid);
                    if (apertureConstruction != null)
                    {
                        thickness = apertureConstruction.GetThickness(AperturePart.Pane);
                    }
                }
            }            

            if (!double.IsNaN(thickness_Min) && !double.IsNaN(thickness_Max) && !double.IsNaN(thickness))
            {
                Range<double> range = new Range<double>(thickness_Min, thickness_Max);
                criterias[2] = range.In(thickness, tolerance);
            }


            if(criterias.TrueForAll(x => x))
            {
                return WPF.Criteria.All;
            }

            if(criterias.TrueForAll(x => !x))
            {
                return WPF.Criteria.None;
            }

            return WPF.Criteria.NotAll;
        }
    }
}