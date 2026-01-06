// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;
using SAM.Core;
using SAM.Core.Attributes;

namespace SAM.Analytical.UI.WPF
{
    [AssociatedTypes(typeof(Setting)), Description("Analytical Setting Parameter")]
    public enum AnalyticalSettingParameter
    {
        [ParameterProperties("Simulate Options", "Simulate Options"), SAMObjectParameterValue(typeof(SimulateOptions))] SimulateOptions,
    }
}
