// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>Pane share of the approximation used when the apertures' areas are not available.</summary>
        public const double GlazingApproximatePaneShare = 0.8;

        /// <summary>
        /// The overall U-value (Uw) of the apertures if they had <paramref name="candidate"/> [W/m²K]:
        /// (Ug·Apane + Uf·Aframe) / (Apane + Aframe), summed over the apertures' ACTUAL pane and frame areas with the
        /// candidate applied (the pane/frame split follows the candidate's frame layers and width). A candidate without
        /// frame layers has no frame area, so its Uw is its Ug. When the areas are not available (no geometry) and the
        /// candidate has a frame, the fixed 80 % pane / 20 % frame approximation is used and
        /// <paramref name="basis"/> says so, so it is never presented as the area-weighted value. Without the values
        /// needed the result is NaN with basis <see cref="GlazingUwBasis.None"/>.
        /// </summary>
        public static double GlazingOverallThermalTransmittance(IEnumerable<Aperture> apertures, ApertureConstruction candidate, double ug, double uf, out GlazingUwBasis basis)
        {
            basis = GlazingUwBasis.None;
            if (candidate == null || double.IsNaN(ug))
            {
                return double.NaN;
            }

            if (!candidate.HasFrameConstructionLayers())
            {
                basis = GlazingUwBasis.Area;
                return ug;
            }

            if (double.IsNaN(uf))
            {
                return double.NaN;
            }

            double area_Pane = 0;
            double area_Frame = 0;
            bool areas = false;
            if (apertures != null)
            {
                areas = true;
                foreach (Aperture aperture in apertures)
                {
                    if (aperture == null)
                    {
                        continue;
                    }

                    Aperture aperture_Candidate = new Aperture(aperture, candidate);
                    double pane = aperture_Candidate.GetPaneArea();
                    double frame = aperture_Candidate.GetFrameArea();
                    if (double.IsNaN(pane) || double.IsNaN(frame))
                    {
                        areas = false;
                        break;
                    }

                    area_Pane += pane;
                    area_Frame += frame;
                }
            }

            if (areas && area_Pane + area_Frame > 0)
            {
                basis = GlazingUwBasis.Area;
                return (ug * area_Pane + uf * area_Frame) / (area_Pane + area_Frame);
            }

            basis = GlazingUwBasis.Approximate;
            return GlazingApproximatePaneShare * ug + (1 - GlazingApproximatePaneShare) * uf;
        }
    }
}
