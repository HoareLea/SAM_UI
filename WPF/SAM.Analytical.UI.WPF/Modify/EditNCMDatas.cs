using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void EditNCMDatas(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            NCMDataWindow nCMDataWindow = new NCMDataWindow();
            bool? dialogResult = nCMDataWindow.ShowDialog();
            if(dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }
        }
    }
}