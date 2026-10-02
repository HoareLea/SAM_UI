// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where an alternative comes from: made by the thickness calculation, already in the model, or from a library / loaded source.</summary>
    public enum ConstructionAlternativeKind
    {
        /// <summary>The thickness variant the existing U-value calculation generates (a new construction, not in the model yet).</summary>
        Generated,

        /// <summary>A construction already in the model.</summary>
        Model,

        /// <summary>A construction of the default library (outside the model until Apply).</summary>
        Library,

        /// <summary>A construction of a source loaded for the panel (outside the model until Apply).</summary>
        Loaded,
    }

    /// <summary>
    /// One line of the alternatives list under an opaque row: the generated variant or one existing construction, with its U-value on the
    /// row's heat-flow basis and what choosing it does. A plain object: the list shows it, the view-model decides.
    /// </summary>
    public sealed class ConstructionAlternativeRow
    {
        internal ConstructionAlternativeRow(ConstructionAlternativeKind kind, Guid? guid, string name, double thermalTransmittance, double target, string sourceLabel, string shortId, string buildUp, int usedBy, IReadOnlyList<string> warnings, string blockReason, string statusOverride, ConstructionCandidate candidate)
        {
            Kind = kind;
            Guid = guid;
            Name = name;
            ThermalTransmittance = thermalTransmittance;
            Target = target;
            SourceLabel = sourceLabel;
            ShortId = shortId;
            BuildUp = buildUp;
            UsedBy = usedBy;
            Warnings = warnings ?? new List<string>();
            BlockReason = blockReason;
            StatusOverride = statusOverride;
            Candidate = candidate;
        }

        public ConstructionAlternativeKind Kind { get; }

        /// <summary>The existing construction's Guid (its identity); null for the generated variant.</summary>
        public Guid? Guid { get; }

        public bool IsGenerated => Kind == ConstructionAlternativeKind.Generated;

        public string Name { get; }

        /// <summary>U-value [W/m²K] on the row's heat-flow basis; NaN when not calculated.</summary>
        public double ThermalTransmittance { get; }

        public double Target { get; }

        public string SourceLabel { get; }

        public string ShortId { get; }

        public string BuildUp { get; }

        /// <summary>How many panels of the model use it (0 for anything not in the model).</summary>
        public int UsedBy { get; }

        /// <summary>Compatibility notes that do not block (another panel group, a name that will get a suffix, materials that will be added).</summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>Why it cannot be chosen (a material missing or different from the model's, not reachable); null when nothing blocks it.</summary>
        public string BlockReason { get; }

        internal string StatusOverride { get; }

        internal ConstructionCandidate Candidate { get; }

        /// <summary>True when the construction was made for another panel group than the panels it would be given to (ordered after those that were not).</summary>
        public bool MadeForOtherGroup { get; internal set; }

        public bool CanApply => BlockReason == null && !double.IsNaN(ThermalTransmittance);

        /// <summary>Target minus U [W/m²K]: positive is better than the target.</summary>
        public double Margin => Target - ThermalTransmittance;

        public bool Meets => !double.IsNaN(ThermalTransmittance) && Margin >= -0.0005;

        /// <summary>"Generated" / "Existing model" / "Library".</summary>
        public string KindText
        {
            get
            {
                switch (Kind)
                {
                    case ConstructionAlternativeKind.Generated:
                        return "Generated";

                    case ConstructionAlternativeKind.Model:
                        return "Existing model";

                    case ConstructionAlternativeKind.Loaded:
                        return string.IsNullOrEmpty(SourceLabel) ? "Source" : SourceLabel;

                    default:
                        return "Library";
                }
            }
        }

        public string NameText => Name;

        public string UText => double.IsNaN(ThermalTransmittance) ? "U –" : string.Format(CultureInfo.CurrentCulture, "U {0:0.000}", ThermalTransmittance);

        /// <summary>"Meets the target by 0.030", "0.020 above the target", or the reason it has no U-value.</summary>
        public string StatusText
        {
            get
            {
                if (StatusOverride != null)
                {
                    return StatusOverride;
                }

                if (double.IsNaN(ThermalTransmittance) || double.IsNaN(Target))
                {
                    return "not calculated";
                }

                double margin = Margin;
                if (Math.Abs(margin) < 0.0005)
                {
                    return "meets the target";
                }

                return margin > 0
                    ? string.Format(CultureInfo.CurrentCulture, "meets the target by {0:0.000}", margin)
                    : string.Format(CultureInfo.CurrentCulture, "{0:0.000} above the target", -margin);
            }
        }

        /// <summary>"Generated · new" / "Existing model · used by 12 panels" / "Library · adds 2 materials".</summary>
        public string OriginText
        {
            get
            {
                switch (Kind)
                {
                    case ConstructionAlternativeKind.Generated:
                        return "Generated · a new construction from the thickness calculation";

                    case ConstructionAlternativeKind.Model:
                        return UsedBy == 0 ? "Existing model · not used by any panel" : string.Format(CultureInfo.CurrentCulture, "Existing model · used by {0} {1}", UsedBy, UsedBy == 1 ? "panel" : "panels");

                    default:
                        return string.Format(CultureInfo.CurrentCulture, "{0} · not in the model yet", KindText);
                }
            }
        }

        /// <summary>The first warning or the block reason for the line (the full list is in the tooltip).</summary>
        public string WarningText => BlockReason != null ? "⚠ " + BlockReason : Warnings.Count == 0 ? string.Empty : "⚠ " + Warnings[0];

        public string Tooltip
        {
            get
            {
                List<string> lines = new List<string> { Name + (ShortId == null ? string.Empty : " (…" + ShortId + ")") };
                if (!string.IsNullOrEmpty(BuildUp))
                {
                    lines.Add(BuildUp);
                }

                lines.Add(OriginText);
                if (BlockReason != null)
                {
                    lines.Add(BlockReason);
                }

                lines.AddRange(Warnings);
                return string.Join(Environment.NewLine, lines);
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} · {1} · {2}", Name, UText, KindText);
        }
    }
}
