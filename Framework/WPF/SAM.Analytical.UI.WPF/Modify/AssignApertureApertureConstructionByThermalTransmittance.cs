using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignApertureApertureConstructionByThermalTransmittance(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Aperture> apertures)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null || apertures == null || apertures.Count() == 0)
            {
                return;
            }

            ConstructionManager constructionManager = analyticalModel.ConstructionManager;
            if (constructionManager == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                adjacencyCluster = new AdjacencyCluster();
            }


            List<Aperture> apertures_Temp = new List<Aperture>();
            foreach (Aperture aperture in apertures)
            {
                if (aperture == null)
                {
                    continue;
                }

                Aperture aperture_Temp = adjacencyCluster.GetAperture(aperture.Guid);
                if (aperture_Temp == null)
                {
                    continue;
                }

                apertures_Temp.Add(aperture_Temp);
            }

            if (apertures_Temp == null || apertures_Temp.Count == 0)
            {
                return;
            }

            ProgressBarWindowManager progressBarWindowManager = new ProgressBarWindowManager();
            progressBarWindowManager.Show("Calculate", "Calculating...");

            ThermalTransmittanceCalculator thermalTransmittanceCalculator = new ThermalTransmittanceCalculator(constructionManager);
            List<ThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults = thermalTransmittanceCalculator.Calculate(constructionManager?.ApertureConstructions?.ConvertAll(x => x.Guid));

            progressBarWindowManager.Close();

            ThermalTransmittanceCalculationResultWindow thermalTransmittanceCalculationResultWindow = new ThermalTransmittanceCalculationResultWindow();
            thermalTransmittanceCalculationResultWindow.ConstructionManager = constructionManager;
            thermalTransmittanceCalculationResultWindow.ThermalTransmittanceCalculationResults = thermalTransmittanceCalculationResults;

            if (thermalTransmittanceCalculationResultWindow.ShowDialog() != true)
            {
                return;
            }

            ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = thermalTransmittanceCalculationResultWindow.ThermalTransmittanceCalculationResult;
            if (thermalTransmittanceCalculationResult == null)
            {
                return;
            }

            if (!Core.Query.TryConvert(thermalTransmittanceCalculationResult.Reference, out System.Guid guid))
            {
                return;
            }

            ApertureConstruction apertureConstruction = constructionManager.ApertureConstructions?.Find(x => x.Guid == guid);
            if (apertureConstruction == null)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            foreach (Aperture aperture in apertures_Temp)
            {
                Panel panel = adjacencyCluster.GetPanel(aperture);
                if (panel == null)
                {
                    continue;
                }

                panel = Analytical.Create.Panel(panel);

                Aperture aperture_New = new Aperture(aperture, apertureConstruction);

                panel.RemoveAperture(aperture.Guid);

                panel.AddAperture(aperture_New);

                adjacencyCluster.AddObject(panel);
                if (sAMObjects.Find(x => x.Guid == panel.Guid) == null)
                {
                    sAMObjects.Add(panel);
                }
            }

            uIAnalyticalModel.SetJSAMObject(new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary), new AnalyticalModelModification(sAMObjects));
        }
    }
}