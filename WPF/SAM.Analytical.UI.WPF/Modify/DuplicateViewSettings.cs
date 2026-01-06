// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void DuplicateViewSettings(this UIAnalyticalModel uIAnalyticalModel, System.Guid guid)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.UIGeometrySettings, out UIGeometrySettings uIGeometrySettings) || uIGeometrySettings == null)
            {
                return;
            }

            IViewSettings viewSettings = uIGeometrySettings.GetViewSettings(guid);
            if (viewSettings == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(viewSettings.Name) ? viewSettings.DefaultName() : viewSettings.Name;
            name = string.Format("New {0}", name);

            using (Core.Windows.Forms.TextBoxForm<string> textBoxForm = new Core.Windows.Forms.TextBoxForm<string>("Duplicate View", "Name"))
            {
                textBoxForm.Value = name;
                if (textBoxForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                name = textBoxForm.Value;
            }

            if (viewSettings is TwoDimensionalViewSettings)
            {
                viewSettings = new TwoDimensionalViewSettings(System.Guid.NewGuid(), name, (TwoDimensionalViewSettings)viewSettings);
            }
            else if (viewSettings is ThreeDimensionalViewSettings)
            {
                viewSettings = new ThreeDimensionalViewSettings(System.Guid.NewGuid(), name, (ThreeDimensionalViewSettings)viewSettings);
            }
            else
            {
                return;
            }


            if (!uIGeometrySettings.AddViewSettings(viewSettings))
            {
                return;
            };

            analyticalModel.SetValue(AnalyticalModelParameter.UIGeometrySettings, uIGeometrySettings);

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new ViewSettingsModification(viewSettings));
        }
    }
}
