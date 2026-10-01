// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What Tas TCD calculates for one glazing system (an aperture construction): the pane's Ug, g and light
    /// transmittance, and, for a system with frame layers, the frame's Uf. NaN where there is no value.
    /// </summary>
    public sealed class GlazingValues
    {
        public GlazingValues(double ug, double g, double lightTransmittance, double uf)
        {
            Ug = ug;
            G = g;
            LightTransmittance = lightTransmittance;
            Uf = uf;
        }

        /// <summary>Thermal transmittance of the pane (centre of glass) [W/m²K].</summary>
        public double Ug { get; }

        /// <summary>Total solar energy transmittance (g-value), 0-1.</summary>
        public double G { get; }

        /// <summary>Light transmittance, 0-1.</summary>
        public double LightTransmittance { get; }

        /// <summary>Thermal transmittance of the frame [W/m²K]; NaN for a system without frame layers.</summary>
        public double Uf { get; }
    }
}
