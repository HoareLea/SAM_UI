using SAM.Core.UI;
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
                        IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetAppearanceSettings<IAppearanceSettings>();
                        if(appearanceSettings is ParameterAppearanceSettings)
                        {
                            ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if(appearanceSettings is TypeAppearanceSettings)
                        {
                            string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                            if (appearanceSettings is InternalConditionAppearanceSettings)
                            {
                                if (parameterName == "Color" || parameterName == "Name")
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else if (appearanceSettings is ZoneAppearanceSettings)
                            {
                                if (parameterName == "Color" || parameterName == "Name")
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

                        IAppearanceSettings appearanceSettings = panelAppearanceSettings.GetAppearanceSettings<IAppearanceSettings>();
                        if (appearanceSettings is ParameterAppearanceSettings)
                        {
                            ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if (appearanceSettings is TypeAppearanceSettings)
                        {
                            string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                            if (appearanceSettings is ConstructionAppearanceSettings)
                            {
                                if (parameterName == "Color" || parameterName == "Name")
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

                        IAppearanceSettings appearanceSettings = apertureAppearanceSettings.GetAppearanceSettings<IAppearanceSettings>();
                        if (appearanceSettings is ParameterAppearanceSettings)
                        {
                            ParameterAppearanceSettings parameterAppearanceSettings = (ParameterAppearanceSettings)appearanceSettings;

                            if (parameterAppearanceSettings.ParameterName == "Color" || parameterAppearanceSettings.ParameterName == "Name")
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if (appearanceSettings is TypeAppearanceSettings)
                        {
                            string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

                            if (appearanceSettings is ApertureConstructionAppearanceSettings)
                            {
                                if (parameterName == "Color" || parameterName == "Name")
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