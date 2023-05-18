using SAM.Core.UI;
using SAM.Geometry.UI;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static bool Editable<T>(ViewSettings viewSettings) where T : ValueAppearanceSettings
        {
            T valueAppearanceSettings = viewSettings?.GetValueAppearanceSettings<T>()?.FirstOrDefault();
            if(valueAppearanceSettings == null)
            {
                return true;
            }

            if (valueAppearanceSettings is SpaceAppearanceSettings)
            {
                SpaceAppearanceSettings spaceAppearanceSettings = (SpaceAppearanceSettings)(object)valueAppearanceSettings;
                if (spaceAppearanceSettings != null)
                {
                    IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
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
                        string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

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
            else if (valueAppearanceSettings is PanelAppearanceSettings)
            {
                PanelAppearanceSettings panelAppearanceSettings = (PanelAppearanceSettings)(object)valueAppearanceSettings;
                if (panelAppearanceSettings != null)
                {

                    IAppearanceSettings appearanceSettings = panelAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
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
                        string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

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
            else if (valueAppearanceSettings is ApertureAppearanceSettings)
            {
                ApertureAppearanceSettings apertureAppearanceSettings = (ApertureAppearanceSettings)(object)valueAppearanceSettings;
                if (apertureAppearanceSettings != null)
                {

                    IAppearanceSettings appearanceSettings = apertureAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
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
                        string parameterName = ((TypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;

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

            return true;
        }
    }
}