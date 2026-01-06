// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Core.Mollier.UI
{
    public enum DisplayPointType
    {
        [Description("Undefined")] Undefined,
        [Description("Default Point")] Default,
        [Description("Process Point")] Process,
        [Description("Cooling Saturation Point")] CoolingSaturation,
        [Description("Dew Point")] Dew,
        [Description("Room Point")] Room,

    }
}
