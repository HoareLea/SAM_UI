using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static bool Editable<T>(ViewSettings viewSettings) where T : IAppearanceSettings
        {
            if (viewSettings is IAnalyticalViewSettings)
            {
                IAnalyticalViewSettings analyticalViewSettings = (IAnalyticalViewSettings)viewSettings;

                if(typeof(T).IsAssignableFrom(typeof(SpaceAppearanceSettings)))
                {
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
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else if (parameterAppearanceSettings is ZoneAppearanceSettings)
                            {
                                ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)parameterAppearanceSettings;
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                else if (typeof(T).IsAssignableFrom(typeof(PanelAppearanceSettings)))
                {
                    PanelAppearanceSettings panelAppearanceSettings = analyticalViewSettings.PanelAppearanceSettings;
                    if (panelAppearanceSettings != null)
                    {
                        ParameterAppearanceSettings parameterAppearanceSettings = panelAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                        if (parameterAppearanceSettings != null)
                        {
                            if (parameterAppearanceSettings is ConstructionAppearanceSettings)
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                else if(typeof(T).IsAssignableFrom(typeof(ApertureAppearanceSettings)))
                {
                    ApertureAppearanceSettings apertureAppearanceSettings = analyticalViewSettings.ApertureAppearanceSettings;
                    if (apertureAppearanceSettings != null)
                    {
                        ParameterAppearanceSettings parameterAppearanceSettings = apertureAppearanceSettings.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                        if (parameterAppearanceSettings != null)
                        {
                            if (parameterAppearanceSettings is ApertureConstructionAppearanceSettings)
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }


            }

            return true;
        }
    }
}