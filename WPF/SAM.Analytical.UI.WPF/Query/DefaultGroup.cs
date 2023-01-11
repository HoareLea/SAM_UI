using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static string DefaultGroup(this SpaceAppearanceSettings spaceAppearanceSettings)
        {
            List<string> values = new List<string>();

            if (spaceAppearanceSettings != null)
            {
                ParameterAppearanceSettings parameterAppearanceSettings = spaceAppearanceSettings?.ParameterAppearanceSettings<ParameterAppearanceSettings>();
                if (parameterAppearanceSettings != null)
                {
                    if (parameterAppearanceSettings is ZoneAppearanceSettings)
                    {
                        ZoneAppearanceSettings zoneAppearanceSettings = (ZoneAppearanceSettings)parameterAppearanceSettings;

                        values.Add(zoneAppearanceSettings.ZoneCategory);

                        values.Add("Zone");
                    }
                    else if (parameterAppearanceSettings is InternalConditionAppearanceSettings)
                    {
                        values.Add("IC");
                    }
                    else
                    {
                        values.Add("Space");
                    }

                    values.Add(parameterAppearanceSettings.ParameterName);
                }
            }

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join(" ", values);
        }
    }
}
