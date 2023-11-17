using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void CalculateGlazing(this UIAnalyticalModel uIAnalyticalModel)
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

            GlazingCalculationWindow glazingCalculationWindow = new GlazingCalculationWindow();
            glazingCalculationWindow.ConstructionManager = constructionManager;
            glazingCalculationWindow.GlazingCalculationData = Tas.Create.GlazingCalculationData(constructionManager);

            bool? result = glazingCalculationWindow.ShowDialog();
            if(result == null || !result.HasValue || !result.Value)
            {
                return;
            }

            GlazingCalculationData glazingCalculationData = glazingCalculationWindow.GlazingCalculationData;
            if(glazingCalculationData == null)
            {
                return;
            }
               
        }
    }
}