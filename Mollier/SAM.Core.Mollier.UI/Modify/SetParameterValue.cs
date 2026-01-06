// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Mollier.UI.Controls;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {

        public static bool SetParameterValue(FlowLayoutPanel flowLayoutPanel, ProcessParameterType processParameterType, double value)
        {
            foreach(Control control in flowLayoutPanel.Controls)
            {
                ParameterControl parameterControl = control as ParameterControl;
                if (parameterControl == null)
                {
                    continue;
                }
                ProcessParameterType processParameterType_Temp = parameterControl.ProcessParameterType;
                if (processParameterType == processParameterType_Temp)
                {
                    parameterControl.Value = value;
                    return true;
                }
            }

            return false;
        }
    }
}
