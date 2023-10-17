using SAM.Architectural;
using SAM.Core;
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
                    else if (appearanceSettings is VentilationSystemAppearanceSettings)
                    {
                        values.Add("Ventilation System");
                    }
                    else if (appearanceSettings is HeatingSystemAppearanceSettings)
                    {
                        values.Add("Heating System");
                    }
                    else if (appearanceSettings is CoolingSystemAppearanceSettings)
                    {
                        values.Add("Cooling System");
                    }
                    else if (appearanceSettings is NCMDataAppearanceSettings)
                    {
                        values.Add("NCM");
                    }

                    appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                }

                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;
                    values.Add(parameterAppearanceSettings.ParameterName);
                }

                if (appearanceSettings is ComplexReferenceAppearanceSettings)
                {
                    IComplexReference complexReference = ((ComplexReferenceAppearanceSettings)appearanceSettings).ComplexReference;
                    if(complexReference != null)
                    {
                        values.Add(Core.Query.Text(complexReference));
                    }
                }
            }

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join(" ", values);
        }
    }
}
