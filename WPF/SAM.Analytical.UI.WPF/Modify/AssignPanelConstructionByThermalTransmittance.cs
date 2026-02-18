using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;
using static SAM.Analytical.Glazing;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignPanelConstructionByThermalTransmittance(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Panel> panels)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || panels == null || panels.Count() == 0)
            {
                return;
            }

            ConstructionManager constructionManager = analyticalModel.ConstructionManager;
            if (constructionManager == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }


            List<Panel> panels_Temp = new List<Panel>();
            foreach (Panel panel in panels)
            {
                if(panel == null)
                {
                    continue;
                }

                Panel panel_Temp = adjacencyCluster.GetObject<Panel>(panel.Guid);
                if(panel_Temp == null)
                {
                    continue;
                }

                panels_Temp.Add(panel_Temp);
            }

            if(panels_Temp == null || panels_Temp.Count == 0)
            {
                return;
            }

            ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager();
            progressBarWindowManager.Show("Calculate", "Calculating...");

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<ThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults = thermalTransmittanceCalculator.Calculate(constructionManager?.Constructions?.ConvertAll(x => x.Guid));

            progressBarWindowManager.Close();

            ThermalTransmittanceCalculationResultWindow thermalTransmittanceCalculationResultWindow = new ThermalTransmittanceCalculationResultWindow();
            thermalTransmittanceCalculationResultWindow.ConstructionManager = constructionManager;
            thermalTransmittanceCalculationResultWindow.ThermalTransmittanceCalculationResults = thermalTransmittanceCalculationResults;

            if (thermalTransmittanceCalculationResultWindow.ShowDialog() != true)
            {
                return;
            }

            ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = thermalTransmittanceCalculationResultWindow.ThermalTransmittanceCalculationResult;
            if(thermalTransmittanceCalculationResult == null)
            {
                return;
            }

            if(!Core.Query.TryConvert(thermalTransmittanceCalculationResult.Reference, out System.Guid guid))
            {
                return;
            }

            Construction construction = constructionManager.Constructions?.Find(x => x.Guid == guid);
            if(construction == null)
            {
                return;
            }

            List<Panel> panels_Transparent = analyticalModel.TransparentPanels();

            foreach (Panel panel_Temp in panels_Temp)
            {
                Panel panel_New  = Analytical.Create.Panel(panel_Temp, construction);

                bool transparent = construction.Transparent(constructionManager?.MaterialLibrary);
                if (transparent)
                {
                    panel_New.SetValue(PanelParameter.ThermalTransmittance, Core.Query.Round(thermalTransmittanceCalculationResult.GetTransparentThermalTransmittance(), Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.DirectSolarEnergyAbsorptance, Core.Query.Round(thermalTransmittanceCalculationResult.DirectSolarEnergyAbosrtptance, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.DirectSolarEnergyReflectance, Core.Query.Round(thermalTransmittanceCalculationResult.DirectSolarEnergyReflectance, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.DirectSolarEnergyTransmittance, Core.Query.Round(thermalTransmittanceCalculationResult.DirectSolarEnergyTransmittance, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.LightReflectance, Core.Query.Round(thermalTransmittanceCalculationResult.LightReflectance, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.LightTransmittance, Core.Query.Round(thermalTransmittanceCalculationResult.LightTransmittance, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.PilkingtonShadingLongWavelengthCoefficient, Core.Query.Round(thermalTransmittanceCalculationResult.PilkingtonLongWavelengthCoefficient, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.PilkingtonShadingShortWavelengthCoefficient, Core.Query.Round(thermalTransmittanceCalculationResult.PilkingtonShortWavelengthCoefficient, Tolerance.MacroDistance));
                    panel_New.SetValue(PanelParameter.TotalSolarEnergyTransmittance, Core.Query.Round(thermalTransmittanceCalculationResult.TotalSolarEnergyTransmittance, Tolerance.MacroDistance));
                    if(panel_New.PanelType == PanelType.WallExternal)
                    {
                        panel_New = Analytical.Create.Panel(panel_New, PanelType.CurtainWall);
                    }
                }
                else
                {
                    panel_New.SetValue(PanelParameter.ThermalTransmittance, Core.Query.Round(thermalTransmittanceCalculationResult.GetThermalTransmittance(panel_New.PanelType), Tolerance.MacroDistance));
                    if (panel_New.PanelType == PanelType.CurtainWall)
                    {
                        panel_New = Analytical.Create.Panel(panel_New, PanelType.WallExternal);
                    }

                }
                adjacencyCluster.AddObject(panel_New);
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(panels_Temp.ConvertAll(x => (SAMObject)x)));
        }
    }
}