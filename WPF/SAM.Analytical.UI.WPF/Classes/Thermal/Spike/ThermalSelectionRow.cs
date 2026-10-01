// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// B0 docking spike (read-only): one line of the Thermal Performance panel - the selected elements of one kind that
    /// share a construction, e.g. "Panel (WallExternal) · SIM_EXT_SLD · 12 use it (3 selected)". No performance values yet.
    /// </summary>
    public sealed class ThermalSelectionRow
    {
        internal ThermalSelectionRow(string kind, string type, Guid constructionGuid, string constructionName, int selectedCount, int usedByCount)
        {
            Kind = kind;
            Type = type;
            ConstructionGuid = constructionGuid;
            ConstructionName = constructionName;
            SelectedCount = selectedCount;
            UsedByCount = usedByCount;
        }

        /// <summary>"Panel" or "Aperture".</summary>
        public string Kind { get; }

        /// <summary>The panel type(s) / aperture type(s) of the selected elements, e.g. "WallExternal".</summary>
        public string Type { get; }

        public Guid ConstructionGuid { get; }

        public string ConstructionName { get; }

        public int SelectedCount { get; }

        /// <summary>How many elements of this kind use the construction in the model.</summary>
        public int UsedByCount { get; }

        public string Title => string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Kind, Type);

        public string Detail => string.Format(CultureInfo.CurrentCulture, "{0} use it ({1} selected)", UsedByCount, SelectedCount);
    }
}
