using SAM.Analytical.Tas;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void ThermalTransmittanceCalculator(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            ConstructionManager constructionManager = analyticalModel.ConstructionManager;
            if (constructionManager == null)
            {
                return;
            }

            ThermalTransmittanceCalculatorWindow thermalTransmittanceCalculatorWindow = new ThermalTransmittanceCalculatorWindow();
            thermalTransmittanceCalculatorWindow.ConstructionManager = constructionManager;

            bool? dialogResult = thermalTransmittanceCalculatorWindow.ShowDialog();
            if(dialogResult != true)
            {
                return;
            }

            List<LayerThicknessCalculationData> layerThicknessCalculationDatas = thermalTransmittanceCalculatorWindow.LayerThicknessCalculationDatas;
            if(layerThicknessCalculationDatas == null || layerThicknessCalculationDatas.Count == 0)
            {
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);

            ProgressBarWindowManager progressBarWindowManager = progressBarWindowManager = new ProgressBarWindowManager();
            progressBarWindowManager.Show("Calculate", "Calculating...");
            List<LayerThicknessCalculationResult> layerThicknessCalculationResults =  thermalTransmittanceCalculator.Calculate(layerThicknessCalculationDatas);
            progressBarWindowManager.Close();

            if (layerThicknessCalculationResults != null)
            {
                LayerThicknessCalculationResultsWindow layerThicknessCalculationResultsWindow = new LayerThicknessCalculationResultsWindow();
                layerThicknessCalculationResultsWindow.ConstructionManager = constructionManager;
                layerThicknessCalculationResultsWindow.LayerThicknessCalculationResults = layerThicknessCalculationResults;
                dialogResult = layerThicknessCalculationResultsWindow.ShowDialog();
                if(dialogResult != true)
                {
                    return;
                }

                layerThicknessCalculationResults = layerThicknessCalculationResultsWindow.LayerThicknessCalculationResults;
                if(layerThicknessCalculationResults == null || layerThicknessCalculationResults.Count == 0)
                {
                    return;
                }

                foreach (LayerThicknessCalculationResult layerThicknessCalculationResult in layerThicknessCalculationResults)
                {

                    constructionManager.Update(layerThicknessCalculationResult);
                }

                analyticalModel = Analytical.Query.UpdateConstructionsByConstructionManager(analyticalModel, constructionManager);
                Tas.Modify.UpdateThermalParameters(analyticalModel);
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}