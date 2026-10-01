// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One line of the Thermal Performance panel (read-only): the elements of one kind that share a construction, with the
    /// performance stored on them and how many there are. Identity is the construction Guid; the name is only shown.
    /// </summary>
    public sealed class ThermalPerformanceRow
    {
        internal ThermalPerformanceRow(bool aperture, string type, Guid constructionGuid, string constructionName, string performanceText, int selectedCount, int usedByCount, int elementCount, double area, IReadOnlyList<Guid> highlightGuids, ThermalPerformanceMode mode)
        {
            IsAperture = aperture;
            Type = type;
            ConstructionGuid = constructionGuid;
            ConstructionName = constructionName;
            PerformanceText = performanceText;
            SelectedCount = selectedCount;
            UsedByCount = usedByCount;
            ElementCount = elementCount;
            Area = area;
            HighlightGuids = highlightGuids ?? new List<Guid>();
            Mode = mode;
        }

        /// <summary>True for an aperture construction (windows, doors), false for a panel construction.</summary>
        public bool IsAperture { get; }

        /// <summary>The panel type(s) / aperture type(s) of the elements in the row, e.g. "WallExternal".</summary>
        public string Type { get; }

        public Guid ConstructionGuid { get; }

        public string ConstructionName { get; }

        /// <summary>The stored performance, e.g. "U 0.260" or "U 1.243 · g 0.40 · LT 0.80"; "not calculated" or "varies" where the model says so.</summary>
        public string PerformanceText { get; }

        /// <summary>Selected elements in the row (<see cref="ThermalPerformanceMode.Selection"/>); 0 in the whole-envelope mode.</summary>
        public int SelectedCount { get; }

        /// <summary>Every element of this kind that uses the construction in the model (the number Set U-value / Set glazing show).</summary>
        public int UsedByCount { get; }

        /// <summary>The elements in the row: the selected ones, or the envelope ones in the whole-envelope mode.</summary>
        public int ElementCount { get; }

        /// <summary>Gross area of the elements in the row [m²] (whole-envelope mode); NaN when it is not known.</summary>
        public double Area { get; }

        /// <summary>
        /// The elements a click on the row highlights: every element using the construction in the Selection mode ("select all N"),
        /// the envelope elements of the row in the whole-envelope mode.
        /// </summary>
        public IReadOnlyList<Guid> HighlightGuids { get; }

        public ThermalPerformanceMode Mode { get; }

        public string Title => string.Format(CultureInfo.CurrentCulture, "{0} ({1})", IsAperture ? "Aperture" : "Panel", Type);

        /// <summary>"12 use it (3 selected)" in the Selection mode, "12 elements · 48.0 m²" in the whole-envelope mode.</summary>
        public string Detail
        {
            get
            {
                if (Mode == ThermalPerformanceMode.Selection)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} use it ({1} selected)", UsedByCount, SelectedCount);
                }

                string elements = string.Format(CultureInfo.CurrentCulture, "{0} {1}", ElementCount, ElementCount == 1 ? "element" : "elements");
                return double.IsNaN(Area) ? elements : string.Format(CultureInfo.CurrentCulture, "{0} · {1:0.0} m²", elements, Area);
            }
        }
    }
}
