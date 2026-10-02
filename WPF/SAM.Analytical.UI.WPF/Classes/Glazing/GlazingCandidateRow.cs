// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One line of the comparison table: a glazing system with its calculated values for the current scope.</summary>
    public sealed class GlazingCandidateRow
    {
        internal GlazingCandidateRow(GlazingCandidate candidate, GlazingValues values, bool transparent, double uw, GlazingUwBasis uwBasis, bool isCurrent, bool passes, double target, IReadOnlyList<GlazingRowWarning> warnings = null)
        {
            Warnings = warnings ?? new List<GlazingRowWarning>();
            Candidate = candidate;
            Values = values;
            Transparent = transparent;
            Uw = uw;
            UwBasis = uwBasis;
            IsCurrent = isCurrent;
            Passes = passes;
            Margin = double.IsNaN(target) || double.IsNaN(uw) ? double.NaN : target - uw;
        }

        public GlazingCandidate Candidate { get; }

        /// <summary>The calculated values; null when Tas could not calculate this system.</summary>
        public GlazingValues Values { get; }

        /// <summary>False for a system whose pane has no glass (a door): g and light transmittance do not apply.</summary>
        public bool Transparent { get; }

        public Guid Guid => Candidate.Guid;

        public string Name => Candidate.Name;

        /// <summary>The name with the current marker, as shown in the table.</summary>
        public string NameText => IsCurrent ? Candidate.Name + "  (current)" : Candidate.Name;

        public string Description => Candidate.Description;

        /// <summary>What the system is marked for before it is chosen (panel group, no frame, material); empty for the current system.</summary>
        public IReadOnlyList<GlazingRowWarning> Warnings { get; }

        public bool HasWarnings => Warnings.Count != 0;

        /// <summary>The markers as shown in the table's Check column, e.g. "⚠ made for roofs · ⚠ no frame"; empty when there are none.</summary>
        public string WarningText => string.Join(" · ", Warnings.Select(x => "⚠ " + x.ShortText));

        /// <summary>A tooltip that tells same-named systems apart: id, source, description, pane and frame.</summary>
        public string Tooltip => string.Join(Environment.NewLine, new[]
        {
            string.Format(CultureInfo.CurrentCulture, "{0}  [{1}]  from {2}", Candidate.Name, Candidate.ShortId, Candidate.Source.Label),
            string.IsNullOrWhiteSpace(Candidate.Description) ? null : Candidate.Description,
            "Pane: " + (string.IsNullOrEmpty(PaneBuildUp) ? "–" : PaneBuildUp),
            "Frame: " + (Candidate.HasFrame ? Candidate.FrameBuildUp : "none"),
            Candidate.MaterialIssue,
        }.Concat(Warnings.Where(x => !x.Blocks).Select(x => x.Text)).Where(x => x != null));

        public string SourceLabel => Candidate.Source.Label;

        public string PaneBuildUp => Candidate.PaneBuildUp;

        /// <summary>"frame: ..." or "no frame", so a system without frame layers is never mistaken for a framed one.</summary>
        public string FrameText => Candidate.HasFrame ? Candidate.FrameBuildUp : "no frame";

        public bool HasFrame => Candidate.HasFrame;

        public bool IsCurrent { get; }

        /// <summary>True when the system passes the filters (the current system is always shown, as the reference).</summary>
        public bool Passes { get; }

        /// <summary>The system can be applied (nothing about its materials blocks it).</summary>
        public bool CanApply => Candidate.MaterialIssue == null;

        /// <summary>Overall U-value [W/m²K], area-weighted when possible (see <see cref="UwBasis"/>); NaN when not available.</summary>
        public double Uw { get; }

        public GlazingUwBasis UwBasis { get; }

        /// <summary>Target Uw minus this Uw: positive is better than the target; NaN without a target.</summary>
        public double Margin { get; }

        public double Ug => Values?.Ug ?? double.NaN;

        public double Uf => Values?.Uf ?? double.NaN;

        public double G => Transparent ? Values?.G ?? double.NaN : double.NaN;

        public double LightTransmittance => Transparent ? Values?.LightTransmittance ?? double.NaN : double.NaN;

        public string UgText => Format(Ug, "0.00");

        public string UfText => HasFrame ? Format(Uf, "0.00") : "–";

        public string GText => Format(G, "0.00");

        public string LightTransmittanceText => Format(LightTransmittance, "0.00");

        /// <summary>Uw to two decimals; "≈" in front when it is the 80/20 approximation, so it is never read as area-weighted.</summary>
        public string UwText => double.IsNaN(Uw) ? "–" : (UwBasis == GlazingUwBasis.Approximate ? "≈" : string.Empty) + Uw.ToString("0.00", CultureInfo.CurrentCulture);

        public string MarginText => double.IsNaN(Margin) ? "–" : Margin.ToString("+0.00;-0.00;0.00", CultureInfo.CurrentCulture);

        /// <summary>The status in words and a glyph (never colour alone).</summary>
        public string StatusText
        {
            get
            {
                if (Values == null)
                {
                    return "✕ not calculated";
                }

                if (!CanApply)
                {
                    return "✕ cannot be used";
                }

                if (double.IsNaN(Margin))
                {
                    return IsCurrent ? "current" : "–";
                }

                return Margin >= 0 ? "✓ meets target" : "✕ above target";
            }
        }

        private static string Format(double value, string format)
        {
            return double.IsNaN(value) ? "–" : value.ToString(format, CultureInfo.CurrentCulture);
        }
    }
}
