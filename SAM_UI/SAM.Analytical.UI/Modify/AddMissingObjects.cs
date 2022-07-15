using SAM.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void AddMissingObjects(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            analyticalModel = Windows.Query.AddMissingObjects(analyticalModel, new string[] { Analytical.Query.ResourcesDirectory() }, out List<IJSAMObject> jSAMObjects, owner);
            if (analyticalModel == null)
            {
                return;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}