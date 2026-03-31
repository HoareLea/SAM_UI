// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void CopyViewSettingsCamera(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return;
            }

            IViewSettings viewSettings_Destination = uIGeometrySettings.GetViewSettings(guid);
            if (viewSettings_Destination == null)
            {
                return;
            }

            IEnumerable<IViewSettings>? viewSettings_Compatible = null;
            if (viewSettings_Destination is TwoDimensionalViewSettings)
            {
                viewSettings_Compatible = uIGeometrySettings.GetViewSettings<TwoDimensionalViewSettings>().Cast<IViewSettings>();
            }
            else if (viewSettings_Destination is ThreeDimensionalViewSettings)
            {
                viewSettings_Compatible = uIGeometrySettings.GetViewSettings<ThreeDimensionalViewSettings>().Cast<IViewSettings>();
            }

            if(viewSettings_Compatible is null || !viewSettings_Compatible.Any())
            {
                return;
            }

            IViewSettings? viewSettings_Source = null;
            using (Core.Windows.Forms.ComboBoxForm<IViewSettings> textBoxForm = new ("Copy View", viewSettings_Compatible, x => x.Name))
            {
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                viewSettings_Source = textBoxForm.SelectedItem;
            }

            if(viewSettings_Source is null)
            {
                return;
            }


            if (viewSettings_Source is TwoDimensionalViewSettings twoDimensionalViewSettings_Source && viewSettings_Destination is TwoDimensionalViewSettings twoDimensionalViewSettings_Destination)
            {
                twoDimensionalViewSettings_Destination.Camera = twoDimensionalViewSettings_Source.Camera;
            }
            else if (viewSettings_Source is ThreeDimensionalViewSettings threeDimensionalViewSettings_Source && viewSettings_Destination is ThreeDimensionalViewSettings threeDimensionalViewSettings_Destination)
            {
                threeDimensionalViewSettings_Destination.Camera = threeDimensionalViewSettings_Source.Camera;
            }
            else
            {
                return;
            }


            if (!uIGeometrySettings.AddViewSettings(viewSettings_Destination))
            {
                return;
            };

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel?.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings_Destination, false, true));
        }
    }
}