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

            if(jSAMObjects == null || jSAMObjects.Count == 0)
            {
                MessageBox.Show("No objects has been added");
                return;
            }

            MessageBox.Show(jSAMObjects.Count == 1 ? string.Format("{0} object has been added to the model", jSAMObjects.Count) : string.Format("{0} objects have been added to the model", jSAMObjects.Count));

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}