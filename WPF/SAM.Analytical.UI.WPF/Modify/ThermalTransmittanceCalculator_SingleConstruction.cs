using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;

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

            List<IAnalyticalObject> analyticalObjects = new List<IAnalyticalObject> ();

            constructionManager.Constructions?.ForEach(x => analyticalObjects.Add(x));

            System.Func<IAnalyticalObject, string> func_Text = new System.Func<IAnalyticalObject, string>(x => 
            { 
                return x is SAMObject ? ((SAMObject)x).Name :"???"; 
            });

            IAnalyticalObject analyticalObject = null;
            using (ComboBoxForm<IAnalyticalObject> comboBoxForm = new ComboBoxForm<IAnalyticalObject>("Select Construction", analyticalObjects, func_Text))
            {
                if (comboBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                analyticalObject = comboBoxForm.SelectedItem;
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

            ConstructionCalculationDataWindow constructionCalculationDataWindow = new ConstructionCalculationDataWindow();
            constructionCalculationDataWindow.ConstructionManager = constructionManager;
            constructionCalculationDataWindow.ConstructionCalculationData = constructionCalculationData;

            bool? dialogResult = constructionCalculationDataWindow.ShowDialog();
            if(dialogResult != true)
            {
                return;
            }

            constructionCalculationData = constructionCalculationDataWindow.ConstructionCalculationData;
            if(constructionCalculationData == null)
            {
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);

            ProgressBarWindowManager progressBarWindowManager = progressBarWindowManager = new ProgressBarWindowManager();
            progressBarWindowManager.Show("Calculate", "Calculating...");
            List<IThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults = thermalTransmittanceCalculator.Calculate(new List<IThermalTransmittanceCalculationData>() { constructionCalculationData});
            progressBarWindowManager.Close();

            if (thermalTransmittanceCalculationResults != null)
            {
                foreach (IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult in thermalTransmittanceCalculationResults)
                {

                    constructionManager.Update(thermalTransmittanceCalculationResult);
                }

                analyticalModel = Analytical.Query.UpdateConstructions(analyticalModel, constructionManager);
                analyticalModel = Analytical.Query.UpdateApertureConstructions(analyticalModel, constructionManager);
                Tas.Modify.UpdateThermalParameters(analyticalModel);
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new FullModification());
        }
    }
}