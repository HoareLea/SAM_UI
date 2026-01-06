// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    [Description("Solar Calculation Method")]
    public enum SolarCalculationMethod
    {
        [Description("Undefined")] Undefined,
        [Description("None")] None,
        [Description("SAM")] SAM,
        [Description("TAS")] TAS,
    }
}
