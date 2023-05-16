using SAM.Core.UI;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static string LegendName(ViewSettings viewSettings)
        {
            if (viewSettings is IAnalyticalViewSettings)
            {
                IAnalyticalViewSettings analyticalViewSettings = (IAnalyticalViewSettings)viewSettings;
                SpaceAppearanceSettings spaceAppearanceSettings = analyticalViewSettings.SpaceAppearanceSettings;
                if (spaceAppearanceSettings != null)
                {
                    IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetAppearanceSettings<IAppearanceSettings>();
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
                    else if(appearanceSettings is TypeAppearanceSettings)
                    {
                        string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;
                        
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
                        else if(appearanceSettings is ZoneAppearanceSettings)
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
                }
            }

            return null;
        }
    }
}