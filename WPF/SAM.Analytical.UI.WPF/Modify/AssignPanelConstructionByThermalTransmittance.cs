// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;

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

            panels_Temp.ForEach(x => adjacencyCluster.AddObject(Analytical.Create.Panel(x, construction)));

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(panels_Temp.ConvertAll(x => (SAMObject)x)));
        }
    }
}
