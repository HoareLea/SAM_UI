using SAM.Geometry.UI;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {       
        public static CurveAppearance CurveAppearance(this Panel panel)
        {
            if (panel == null)
            {
                return null;
            }

            return new CurveAppearance(System.Windows.Media.Color.FromRgb(105, 105, 105), 0.04);
        }

        public static CurveAppearance CurveAppearance(this Panel panel, ViewSettings viewSettings)
        {
            CurveAppearance result = viewSettings.GetAppearances<CurveAppearance>(panel)?.FirstOrDefault();
            if (result == null)
            {
                result = CurveAppearance(panel);
            }

            return result;
        }
    }
}