// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.UI.WPF
{
    public interface IFilterControl
    {
        IUIFilter UIFilter { get; }

        event FilterChangedEventHandler FilterChanged;

        event FilterRemovingEventHandler FilterRemoving;
    }
}
