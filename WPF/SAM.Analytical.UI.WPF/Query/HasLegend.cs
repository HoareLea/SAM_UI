using SAM.Core.UI;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static bool HasLegend(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
        {
            ViewSettings viewSettings = uIAnalyticalModel.ViewSettings<ViewSettings>(guid);
            if(viewSettings == null)
            {
                return false;
            }

            Legend legend = viewSettings.Legend;
            if(legend == null)
            {
                return false;
            }

            return legend != null;
        }
    }
}
