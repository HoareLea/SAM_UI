using SAM.Core;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static bool New(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if (uIAnalyticalModel == null)
            {
                uIAnalyticalModel = new UIAnalyticalModel();
            }

            if (uIAnalyticalModel.JSAMObject != null)
            {
                if (!uIAnalyticalModel.Close())
                {
                    return false;
                }
            }

            AnalyticalModel analyticalModel = null;
            using (Windows.Forms.NewAnalyticalModelForm newAnalyticalModelForm = new Windows.Forms.NewAnalyticalModelForm("New Project"))
            {
                if (newAnalyticalModelForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }

                analyticalModel = newAnalyticalModelForm.GetAnalyticalModel();
            }

            if(analyticalModel == null)
            {
                return false;
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
            return true;
        }
    }
}