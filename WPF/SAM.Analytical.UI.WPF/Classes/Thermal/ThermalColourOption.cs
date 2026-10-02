// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One choice of "Colour by": a thermal property of the elements a 3D view shows, as the Thermal Performance panel lists it. Each option
    /// is a <see cref="ParameterColouring"/> over a parameter that SAM STORES on the element (the same values the panel's rows show:
    /// "U 1.243 · g 0.40 · LT 0.80"); nothing is calculated to colour a view and nothing is written to the model.
    /// <para>
    /// The element types are kept apart on purpose, because the same quantity is stored with a different meaning on each:
    /// a <b>panel</b> stores the U-value of its construction (<see cref="PanelParameter.ThermalTransmittance"/>); a <b>window or door</b>
    /// stores the U-value of its aperture construction (<see cref="ApertureParameter.ThermalTransmittance"/>), which for a window is the
    /// glazing value Tas reports for the transparent construction (Ug), NOT the whole-window Uw with its frame - Uw is calculated on demand
    /// by the glazing workflow and is not stored on the element, so it cannot be shown spatially from the model; g and light transmittance
    /// are stored on windows only. See <c>documentation/Thermal-ColourSelector.md</c>.
    /// </para>
    /// </summary>
    public sealed class ThermalColourOption
    {
        private ThermalColourOption(string key, string label, string toolTip, ParameterColouring colouring)
        {
            Key = key;
            Label = label;
            ToolTip = toolTip;
            Colouring = colouring;
        }

        /// <summary>The "no colouring" choice: the view is shown as saved.</summary>
        public static ThermalColourOption Off { get; } = new ThermalColourOption("Off", "Off", "Show the view as saved.", null);

        public static ThermalColourOption PanelUValue { get; } = new ThermalColourOption(
            "PanelUValue",
            "U-value (panels)",
            "The stored U-value of walls, roofs, floors and other panels [W/m²K].",
            ParameterColouring.PanelThermalTransmittance());

        public static ThermalColourOption ApertureUValue { get; } = new ThermalColourOption(
            "ApertureUValue",
            "U-value (windows & doors)",
            "The stored U-value of windows and doors [W/m²K]. For a window this is the glazing value Tas stores (Ug), not the whole-window Uw with its frame.",
            ParameterColouring.For(typeof(Aperture), ApertureParameter.ThermalTransmittance, PaletteDefinitions.SamThermal, "U-value, windows & doors [W/m²K]"));

        public static ThermalColourOption GValue { get; } = new ThermalColourOption(
            "GValue",
            "g-value (windows)",
            "The stored total solar energy transmittance of windows [0-1]. Doors and opaque apertures have none.",
            ParameterColouring.For(typeof(Aperture), ApertureParameter.TotalSolarEnergyTransmittance, PaletteDefinitions.SamEnergy, "g-value [0-1]"));

        public static ThermalColourOption LightTransmittance { get; } = new ThermalColourOption(
            "LightTransmittance",
            "Light transmittance (windows)",
            "The stored light transmittance (LT) of windows [0-1]. Doors and opaque apertures have none.",
            ParameterColouring.For(typeof(Aperture), ApertureParameter.LightTransmittance, PaletteDefinitions.SamSpectrumAnalytical, "Light transmittance [0-1]"));

        /// <summary>Off, then the thermal properties the panel can show, in the order the selector lists them.</summary>
        public static IReadOnlyList<ThermalColourOption> All { get; } = new[] { Off, PanelUValue, ApertureUValue, GValue, LightTransmittance };

        /// <summary>The option that stands for <paramref name="colouring"/>; <see cref="Off"/> for null, null when it is not a thermal one.</summary>
        public static ThermalColourOption Of(ParameterColouring colouring)
        {
            return colouring == null ? Off : All.FirstOrDefault(x => colouring.Equals(x.Colouring));
        }

        public string Key { get; }

        /// <summary>What the selector shows (never a parameter or enum name).</summary>
        public string Label { get; }

        public string ToolTip { get; }

        /// <summary>What the view is coloured by; null for <see cref="Off"/>.</summary>
        public ParameterColouring Colouring { get; }

        /// <summary>The view shows what is coloured: the option is available when the view contains the elements of the type.</summary>
        public bool IsAvailableIn(Func<Type, bool> viewShows)
        {
            return Colouring == null || (viewShows != null && viewShows(Colouring.ElementType));
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
