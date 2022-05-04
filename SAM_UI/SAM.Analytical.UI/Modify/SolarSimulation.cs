using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void SolarSimulation(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            List<int> hoursOfYear = Analytical.Query.DefaultHoursOfYear();

            SolarCalculator.Modify.Simulate(analyticalModel, hoursOfYear.ConvertAll(x => new DateTime(2018, 1, 1).AddHours(x)), Core.Tolerance.MacroDistance, Core.Tolerance.MacroDistance, 0.012, Core.Tolerance.Distance);
            if (analyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}