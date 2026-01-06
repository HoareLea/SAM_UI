// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    [Description("Position")]
    public enum Position
    {
        [Description("Undefined")] Undefined,
        [Description("Prefix")] Prefix,
        [Description("Sufix")] Sufix,
    }
}
