using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void ThermalTransmittanceCalculator_SingleConstruction(this UIAnalyticalModel uIAnalyticalModel)
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

            SelectConstructionWindow selectConstructionWindow = new SelectConstructionWindow();
            selectConstructionWindow.ConstructionManager = constructionManager;
            if(selectConstructionWindow.ShowDialog() != true)
            {
                return;
            }

            IAnalyticalObject analyticalObject = selectConstructionWindow.GetSelectedAnalyticalObject();
            if(analyticalObject == null)
            {
                return;
            }

            IConstructionCalculationData constructionCalculationData = null;
            if (analyticalObject is Construction)
            {
                LayerThicknessCalculationData layerThicknessCalculationData = Tas.Create.LayerThicknessCalculationData((Construction)analyticalObject, constructionManager.MaterialLibrary);
                if(layerThicknessCalculationData != null)
                {
                    layerThicknessCalculationData.ThermalTransmittance = double.NaN;
                    constructionCalculationData = layerThicknessCalculationData;
                }
            }

            if(constructionCalculationData == null)
            {
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);

            bool @continue = true;

            IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = null;

            while (@continue)
            {
                @continue = false;

                ConstructionCalculationDataWindow constructionCalculationDataWindow = new ConstructionCalculationDataWindow();
                constructionCalculationDataWindow.ConstructionManager = constructionManager;
                constructionCalculationDataWindow.ConstructionCalculationData = constructionCalculationData;

                bool? dialogResult = constructionCalculationDataWindow.ShowDialog();
                if (dialogResult != true)
                {
                    return;
                }

                constructionCalculationData = constructionCalculationDataWindow.ConstructionCalculationData;
                if (constructionCalculationData == null)
                {
                    return;
                }

                ProgressBarWindowManager progressBarWindowManager = progressBarWindowManager = new ProgressBarWindowManager();
                progressBarWindowManager.Show("Calculate", "Calculating...");

                thermalTransmittanceCalculationResult = thermalTransmittanceCalculator.Calculate(constructionCalculationData);
                progressBarWindowManager.Close();

                if (thermalTransmittanceCalculationResult == null)
                {
                    MessageBox.Show("Could not calculate construction for given criteria.");
                    @continue = true;
                    continue;
                }

                if(thermalTransmittanceCalculationResult is ConstructionCalculationResult)
                {
                    if(double.IsNaN(((ConstructionCalculationResult)thermalTransmittanceCalculationResult).CalculatedThermalTransmittance))
                    {
                        MessageBox.Show("Could not calculate construction for given criteria.");
                        @continue = true;
                        continue;
                    }
                }
                else if (thermalTransmittanceCalculationResult is LayerThicknessCalculationResult)
                {
                    if (double.IsNaN(((LayerThicknessCalculationResult)thermalTransmittanceCalculationResult).CalculatedThermalTransmittance))
                    {
                        MessageBox.Show("Could not calculate construction for given criteria.");
                        @continue = true;
                        continue;
                    }
                }

                ConstructionCalculationResultWindow constructionCalculationResultWindow = new ConstructionCalculationResultWindow();
                constructionCalculationResultWindow.ConstructionManager = constructionManager;
                constructionCalculationResultWindow.ConstructionCalculationResult = thermalTransmittanceCalculationResult as IConstructionCalculationResult;

                dialogResult = constructionCalculationResultWindow.ShowDialog();
                if (dialogResult != true)
                {
                    @continue = true;
                    continue;
                }
            }

            if(thermalTransmittanceCalculationResult == null)
            {
                return;
            }

            constructionManager.Update(thermalTransmittanceCalculationResult);

            analyticalModel = Analytical.Query.UpdateConstructions(analyticalModel, constructionManager);
            analyticalModel = Analytical.Query.UpdateApertureConstructions(analyticalModel, constructionManager);
            Tas.Modify.UpdateThermalParameters(analyticalModel);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}