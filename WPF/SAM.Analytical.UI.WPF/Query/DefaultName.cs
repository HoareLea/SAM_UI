using SAM.Geometry.Spatial;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static string DefaultName(this IViewSettings viewSettings)
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
                    double elevation = Core.Query.Round(plane.Origin.Z, Core.Tolerance.MacroDistance);

                    return string.Format("Section View ({0}m)", elevation);
                }
            }

            return null;
        }
    }
}
