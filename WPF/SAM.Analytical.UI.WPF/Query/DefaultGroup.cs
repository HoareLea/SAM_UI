// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
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

                    appearanceSettings = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>();
                }

                if (appearanceSettings is ParameterAppearanceSettings)
                {
                    string parameterName = ((ParameterAppearanceSettings)appearanceSettings)?.ParameterName;
                    values.Add(parameterName);
                }

                if (appearanceSettings is ComplexReferenceAppearanceSettings)
                {
                    IComplexReference complexReference = ((ComplexReferenceAppearanceSettings)appearanceSettings).ComplexReference;
                    if (complexReference != null)
                    {
                        values.Add(Core.Query.ShortText(complexReference));
                    }
                }
            }

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join(" ", values);
        }
    }
}
