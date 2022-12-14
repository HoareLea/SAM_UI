using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static bool Editable(ViewSettings viewSettings)
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

            return true;
        }
    }
}