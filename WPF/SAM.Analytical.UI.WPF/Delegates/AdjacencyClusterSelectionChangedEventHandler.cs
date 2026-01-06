// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    public delegate void AdjacencyClusterSelectionChangedEventHandler<T>(object sender, AdjacencyClusterSelectionChangedEventArgs<T> e) where T: SAMObject;
}
