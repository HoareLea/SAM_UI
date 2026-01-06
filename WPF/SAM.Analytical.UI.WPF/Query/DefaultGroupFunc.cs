// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Func<Space, string> DefaultGroupFunc()
        {
            return new Func<Space, string>( x => { return null; });
        }
    }
}
