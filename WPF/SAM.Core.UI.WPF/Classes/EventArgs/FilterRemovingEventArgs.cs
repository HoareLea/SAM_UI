// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.UI.WPF
{
    public class FilterRemovingEventArgs : EventArgs
    {
        public IFilterControl FilterControl { get; } = null;
        
        public FilterRemovingEventArgs(IFilterControl filterControl)
        {
            FilterControl = filterControl;
        }
    }
}
