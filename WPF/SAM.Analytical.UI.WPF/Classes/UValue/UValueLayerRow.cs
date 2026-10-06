// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One construction layer in the "Set U-value" before/after table (thicknesses in mm).</summary>
    public sealed class UValueLayerRow
    {
        public UValueLayerRow(int index, string name, double thicknessBefore, double thicknessAfter, bool adjusted)
        {
            Index = index;
            Name = name;
            ThicknessBefore = thicknessBefore;
            ThicknessAfter = thicknessAfter;
            Adjusted = adjusted;
        }

        /// <summary>0-based layer index, outside to inside as stored in the construction.</summary>
        public int Index { get; }

        /// <summary>The layer's material name.</summary>
        public string Name { get; }

        /// <summary>Thickness now [mm].</summary>
        public double ThicknessBefore { get; }

        /// <summary>Thickness after Apply [mm] (rounded to 1 mm as applied); equals <see cref="ThicknessBefore"/> for fixed layers.</summary>
        public double ThicknessAfter { get; }

        /// <summary>True for the layer that is varied.</summary>
        public bool Adjusted { get; }
    }
}
