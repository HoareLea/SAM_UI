using SAM.Analytical.Tas;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void CalculateGlazing(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            ConstructionManager constructionManager = analyticalModel.ConstructionManager;
            if (constructionManager == null)
            {
                return;
            }

            GlazingCalculationDataWindow glazingCalculationDataWindow = new GlazingCalculationDataWindow();
            glazingCalculationDataWindow.ConstructionManager = constructionManager;
            glazingCalculationDataWindow.GlazingCalculationData = Tas.Create.GlazingCalculationData(constructionManager);

            bool? result = null;

            result = glazingCalculationDataWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            GlazingCalculationData glazingCalculationData = glazingCalculationDataWindow.GlazingCalculationData;
            if (glazingCalculationData == null)
            {
                return;
            }

            ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager();
            progressBarWindowManager.Show("Calculate", "Calculating...");

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<GlazingCalculationResult> glazingCalculationResults = thermalTransmittanceCalculator.CalculateGlazing();

            progressBarWindowManager.Close();

            GlazingCalculationResultWindow glazingCalculationResultWindow = new GlazingCalculationResultWindow();
            glazingCalculationResultWindow.ConstructionManager = constructionManager;
            glazingCalculationResultWindow.GlazingCalculationData = glazingCalculationData;
            glazingCalculationResultWindow.GlazingCalculationResults = glazingCalculationResults;

            result = glazingCalculationResultWindow.ShowDialog();
            if (result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            GlazingCalculationResult glazingCalculationResult = glazingCalculationResultWindow.GlazingCalculationResult;
            if (glazingCalculationResult == null)
            {
                return;
            }

            if (!Guid.TryParse(glazingCalculationResult.Reference, out Guid guid_Source))
            {
                return;
            }

            if (!Tas.Modify.Update(constructionManager, guid_Source, glazingCalculationData.ConstructionGuid, glazingCalculationResultWindow.Replace))
            {
                return;
            }

            analyticalModel = Analytical.Query.UpdateConstructions(analyticalModel, constructionManager);
            analyticalModel = Analytical.Query.UpdateApertureConstructions(analyticalModel, constructionManager);
            Tas.Modify.UpdateThermalParameters(analyticalModel);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}