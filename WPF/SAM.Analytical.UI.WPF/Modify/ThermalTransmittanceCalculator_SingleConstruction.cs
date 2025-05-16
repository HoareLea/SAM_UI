using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using System.Linq;
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

            IThermalTransmittanceCalculationData thermalTransmittanceCalculationData = null;
            if (analyticalObject is Construction)
            {
                LayerThicknessCalculationData layerThicknessCalculationData = Tas.Create.LayerThicknessCalculationData((Construction)analyticalObject, constructionManager.MaterialLibrary);
                if(layerThicknessCalculationData != null)
                {
                    layerThicknessCalculationData.ThermalTransmittance = double.NaN;
                    thermalTransmittanceCalculationData = layerThicknessCalculationData;
                }
            }
            else if(analyticalObject is ApertureConstruction)
            {
                ApertureConstructionCalculationData apertureConstructionCalculationData = Tas.Create.ApertureConstructionCalculationData((ApertureConstruction)analyticalObject, constructionManager);
                if (apertureConstructionCalculationData != null)
                {
                    apertureConstructionCalculationData.PaneThermalTransmittance = double.NaN;
                    apertureConstructionCalculationData.FrameThermalTransmittance = double.NaN;
                    thermalTransmittanceCalculationData = apertureConstructionCalculationData;
                }
            }

            if(thermalTransmittanceCalculationData == null)
            {
                return;
            }

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);

            bool @continue = true;

            IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = null;

            while (@continue)
            {
                @continue = false;

                bool? dialogResult = null;

                if (thermalTransmittanceCalculationData is IConstructionCalculationData)
                {
                    ConstructionCalculationDataWindow constructionCalculationDataWindow = new ConstructionCalculationDataWindow();
                    constructionCalculationDataWindow.ConstructionManager = constructionManager;
                    constructionCalculationDataWindow.ConstructionCalculationData = (IConstructionCalculationData)thermalTransmittanceCalculationData;

                    dialogResult = constructionCalculationDataWindow.ShowDialog();
                    if (dialogResult != true)
                    {
                        return;
                    }

                    thermalTransmittanceCalculationData = constructionCalculationDataWindow.ConstructionCalculationData;
                }
                else if(thermalTransmittanceCalculationData is ApertureConstructionCalculationData)
                {
                    ApertureConstructionCalculationDataWindow apertureConstructionCalculationDataWindow = new ApertureConstructionCalculationDataWindow();
                    apertureConstructionCalculationDataWindow.ConstructionManager = constructionManager;
                    apertureConstructionCalculationDataWindow.ApertureConstructionCalculationData = (ApertureConstructionCalculationData)thermalTransmittanceCalculationData;

                    dialogResult = apertureConstructionCalculationDataWindow.ShowDialog();
                    if (dialogResult != true)
                    {
                        return;
                    }

                    thermalTransmittanceCalculationData = apertureConstructionCalculationDataWindow.ApertureConstructionCalculationData;
                }
                else
                {
                    return;
                }

                if (thermalTransmittanceCalculationData == null)
                {
                    return;
                }

                ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager();
                progressBarWindowManager.Show("Calculate", "Calculating...");

                Log log = Tas.Create.Log(thermalTransmittanceCalculationData, constructionManager);
                if(log != null && log.Count() != 0)
                {
                    Log log_Error = log.Filter(LogRecordType.Error);
                    if(log_Error != null && log_Error.Count() != 0)
                    {
                        foreach(LogRecord logRecord in log)
                        {
                            if(string.IsNullOrWhiteSpace(logRecord?.Text))
                            {
                                continue;
                            }

                            progressBarWindowManager.Close();

                            MessageBox.Show(string.Format("Calculations interrupted!\n{0}", logRecord.Text));
                            break;
                        }

                        @continue = true;
                        continue;
                    }
                }

                thermalTransmittanceCalculationResult = thermalTransmittanceCalculator.Calculate(thermalTransmittanceCalculationData);
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
                constructionCalculationResultWindow.ThermalTransmittanceCalculationResult = thermalTransmittanceCalculationResult;

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