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
                    ParameterAppearanceSettings parameterAppearanceSettings = spaceAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                    if (parameterAppearanceSettings != null)
                    {
                        if (parameterAppearanceSettings is InternalConditionAppearanceSettings)
                        {
                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return "Internal Conditions";
                            }
                            else
                            {
                                return parameterAppearanceSettings.ParameterName;
                            }
                        }
                        else if (parameterAppearanceSettings is ZoneAppearanceSettings)
                        {
                            ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)parameterAppearanceSettings;
                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return string.Format("{0} Zones", zoneAppearanceSettings.ZoneCategory);
                            }
                            else
                            {
                                return parameterAppearanceSettings.ParameterName;
                            }
                        }
                        else
                        {
                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return "Spaces";
                            }
                            else
                            {
                                return parameterAppearanceSettings.ParameterName;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}