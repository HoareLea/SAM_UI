// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>A heading of the Thermal Performance panel (Walls, Roofs, Floors, Windows, Doors ...) and its rows.</summary>
    public sealed class ThermalPerformanceGroup
    {
        internal ThermalPerformanceGroup(string title, IReadOnlyList<ThermalPerformanceRow> rows)
        {
            Title = title;
            Rows = rows;
        }

        public string Title { get; }

        public IReadOnlyList<ThermalPerformanceRow> Rows { get; }
    }
}
