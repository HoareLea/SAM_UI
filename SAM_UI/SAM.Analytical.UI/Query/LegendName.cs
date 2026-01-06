using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.UI;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static string LegendName<T>(ViewSettings viewSettings) where T : IJSAMObject
        {
            if(viewSettings == null)
            {
                return null;
            }

            if(typeof(T) == typeof(Space))
            {
                return LegendName_Space(viewSettings);
            }

            if (typeof(T) == typeof(Panel))
            {
                return LegendName_Panel(viewSettings);
            }

            if (typeof(T) == typeof(Aperture))
            {
                return LegendName_Aperture(viewSettings);
            }

            return null;
        }

        public static string LegendName_Space(ViewSettings viewSettings)
        {
            SpaceAppearanceSettings spaceAppearanceSettings = viewSettings?.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();
            if (spaceAppearanceSettings == null)
            {
                return null;
            }

            IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
            if (appearanceSettings is ParameterAppearanceSettings)
            {
                ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                {
                    return "Spaces";
                }
                else
                {
                    return parameterAppearanceSettings.ParameterName;
                }
            }
            else if (appearanceSettings is ITypeAppearanceSettings)
            {
                string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                if (appearanceSettings is InternalConditionAppearanceSettings)
                {
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return "Internal Conditions";
                    }
                    else
                    {
                        return parameterName;
                    }
                }
                else if (appearanceSettings is ZoneAppearanceSettings)
                {
                    ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)appearanceSettings;
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return string.Format("{0} Zones", zoneAppearanceSettings.ZoneCategory);
                    }
                    else
                    {
                        return parameterName;
                    }
                }
            }
            else if (appearanceSettings is ComplexReferenceAppearanceSettings)
            {
                IComplexReference complexReference = ((ComplexReferenceAppearanceSettings)appearanceSettings).ComplexReference;
                return complexReference.Text();
            }

            return null;
        }

        public static string LegendName_Panel(ViewSettings viewSettings)
        {
            PanelAppearanceSettings panelAppearanceSettings = viewSettings?.GetValueAppearanceSettings<PanelAppearanceSettings>()?.FirstOrDefault();
            if (panelAppearanceSettings == null)
            {
                return null;
            }

            IAppearanceSettings appearanceSettings = panelAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
            if (appearanceSettings is ParameterAppearanceSettings)
            {
                ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                {
                    return "Panels";
                }
                else
                {
                    return parameterAppearanceSettings.ParameterName;
                }
            }
            else if (appearanceSettings is ITypeAppearanceSettings)
            {
                string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                if (appearanceSettings is ConstructionAppearanceSettings)
                {
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return "Constructions";
                    }
                    else
                    {
                        return parameterName;
                    }
                }

            }
            else if (appearanceSettings is BoundaryTypeAppearanceSettings)
            {
                return "Boundary Types";
            }

            return null;
        }

        public static string LegendName_Aperture(ViewSettings viewSettings)
        {
            ApertureAppearanceSettings apertureAppearanceSettings = viewSettings?.GetValueAppearanceSettings<ApertureAppearanceSettings>()?.FirstOrDefault();
            if (apertureAppearanceSettings == null)
            {
                return null;
            }

            IAppearanceSettings appearanceSettings = apertureAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
            if (appearanceSettings is ParameterAppearanceSettings)
            {
                ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                {
                    return "Apertures";
                }
                else
                {
                    return parameterAppearanceSettings.ParameterName;
                }
            }
            else if (appearanceSettings is ITypeAppearanceSettings)
            {
                string parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                if (appearanceSettings is ApertureConstructionAppearanceSettings)
                {
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return "Aperture Constructions";
                    }
                    else
                    {
                        return parameterName;
                    }
                }
                else if (appearanceSettings is PanelAppearanceSettings)
                {
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return "Panels";
                    }
                    else
                    {
                        return parameterName;
                    }
                }
                else if (appearanceSettings is OpeningPropertiesAppearanceSettings)
                {
                    if (parameterName == "Color" || parameterName == "Name")
                    {
                        return "Opening Properties";
                    }
                    else
                    {
                        return parameterName;
                    }
                }

            }

            return null;
        }
    }
}