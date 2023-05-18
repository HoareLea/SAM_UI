using SAM.Core.UI;
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
                IAppearanceSettings appearanceSettings = spaceAppearanceSettings.GetValueAppearanceSettings<ValueAppearanceSettings>();
                if (appearanceSettings is TypeAppearanceSettings)
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

                    appearanceSettings = ((TypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>();
                }

                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    string parameterName = ((ParameterAppearanceSettings)appearanceSettings)?.ParameterName;
                    values.Add(parameterName);
                }
            }

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join(" ", values);
        }
    }
}
