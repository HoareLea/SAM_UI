// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Core.UI.WPF
{
    public class FilterAddingEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
        public Type Type { get; }
        public IUIFilter UIFilter { get; set; } = null;
        
        public FilterAddingEventArgs(Type type)
        {
            Type = type;
        }
    }
}
