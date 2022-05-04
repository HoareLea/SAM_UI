using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EnergySimulation(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            AnalyticalModel analyticalModel_Temp = Query.Simulate(analyticalModel, uIAnalyticalModel.Path);
            if (analyticalModel_Temp == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel_Temp;
        }
    }
}