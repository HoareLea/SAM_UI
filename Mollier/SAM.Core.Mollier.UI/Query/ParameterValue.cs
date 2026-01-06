// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Mollier.UI.Controls;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {

        public static T ParameterValue<T>(FlowLayoutPanel flowLayoutPanel, ProcessParameterType processParameterType)
        {
            foreach(Control control in flowLayoutPanel.Controls)
            {
                if(typeof(T) == typeof(double))
                {
                    ParameterControl parameterControl = control as ParameterControl;
                    if (parameterControl == null)
                    {
                        continue;
                    }
                    ProcessParameterType processParameterType_Temp = parameterControl.ProcessParameterType;
                    if (processParameterType == processParameterType_Temp)
                    {
                        return (T)(object)parameterControl.Value;
                    }
                }
            }
            return default(T);
        }
    }
}
