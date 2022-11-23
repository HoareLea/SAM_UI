using SAM.Core.UI.WPF;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static string Header(this IViewSettings viewSettings)
        {
            if(viewSettings == null)
            {
                return null;
            }

            if(viewSettings is ThreeDimensionalViewSettings)
            {
                return "3D View";
            }

            if(viewSettings is TwoDimensionalViewSettings)
            {
                TwoDimensionalViewSettings twoDimensionalViewSettings = (TwoDimensionalViewSettings)viewSettings;
                Plane plane = twoDimensionalViewSettings.Plane;
                if(plane != null)
                {
                    double elevation = SAM.Core.Query.Round(plane.Origin.Z, SAM.Core.Tolerance.MacroDistance);

                    return string.Format("Section View ({0}m)", elevation);
                }
            }

            return null;
        }
    }
}
