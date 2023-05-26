using SAM.Architectural;
using SAM.Core.UI;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System.Collections.Generic;

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

        public static string DefaultName(this Level level, double elevation, SpaceAppearanceSettings spaceAppearanceSettings)
        {
            List<string> values = new List<string>();

            if (level != null)
            {
                values.Add(level.Name);
            }

            if(!double.IsNaN(elevation))
            {
                values.Add(string.Format("[{0}m]", Core.Query.Round(elevation, Core.Tolerance.MacroDistance).ToString()));
            }

            if (spaceAppearanceSettings != null)
            {
                IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
           
                if (appearanceSettings is ITypeAppearanceSettings)
                {
                    if (appearanceSettings is ZoneAppearanceSettings)
                    {
                        ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)appearanceSettings;

                        values.Add(zoneAppearanceSettings.ZoneCategory);

                        values.Add("Zone");
                    }
                    else if (appearanceSettings is InternalConditionAppearanceSettings)
                    {
                        values.Add("IC");
                    }
                    else if (appearanceSettings is SpaceAppearanceSettings)
                    {
                        values.Add("Space");
                    }

                    appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                }

                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;
                    values.Add(parameterAppearanceSettings.ParameterName);
                }
            }

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join(" ", values);
        }
    }
}
