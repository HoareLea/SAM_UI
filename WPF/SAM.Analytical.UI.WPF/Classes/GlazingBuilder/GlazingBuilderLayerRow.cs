// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One gas the Builder offers for a gap.</summary>
    public sealed class GlazingGasOption
    {
        internal GlazingGasOption(DefaultGasType value)
        {
            Value = value;
            Display = Core.Query.Description(value);
        }

        public DefaultGasType Value { get; }

        public string Display { get; }

        public override string ToString() => Display;
    }

    /// <summary>
    /// One line of the Builder's build-up list (OUTSIDE → INSIDE): a pane with its values, or a gap with its gas and width. It is a view of a
    /// <see cref="DraftLayer"/> of the Builder's own draft - editing a gap here edits that draft layer and nothing else.
    /// </summary>
    public sealed class GlazingBuilderLayerRow : INotifyPropertyChanged
    {
        private static readonly IReadOnlyList<GlazingGasOption> gasOptions = DraftGap.SupportedGasTypes.Select(x => new GlazingGasOption(x)).ToList();

        private readonly Action<GlazingBuilderLayerRow> edited;
        private string widthText;
        private string heatTransferText = string.Empty;
        private IReadOnlyList<GlazingDraftIssue> issues = new List<GlazingDraftIssue>();

        internal GlazingBuilderLayerRow(DraftLayer layer, int index, int number, Action<GlazingBuilderLayerRow> edited)
        {
            Layer = layer;
            Index = index;
            Number = number;
            this.edited = edited;
            widthText = layer is DraftGap gap ? Millimetres(gap.Thickness) : string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DraftLayer Layer { get; }

        /// <summary>The position in the build-up: 0 is the outermost layer.</summary>
        public int Index { get; }

        /// <summary>The number among the panes (or among the gaps), outside first: "Pane 2".</summary>
        public int Number { get; }

        public bool IsPane => Layer is DraftPane;

        public bool IsGap => Layer is DraftGap;

        public DraftPane Pane => Layer as DraftPane;

        public DraftGap Gap => Layer as DraftGap;

        public string Title => (IsPane ? "Pane " : "Gap ") + Number.ToString(CultureInfo.CurrentCulture);

        /// <summary>The pane's name as its source gives it ("Optifloat Clear 4mm"); for a gap the gas.</summary>
        public string Name => Pane != null ? Pane.DisplayName ?? Pane.OriginalName ?? string.Empty : Core.Query.Description(Gap.GasType);

        /// <summary>The values a person chooses a pane by; for a gap its heat transfer coefficient.</summary>
        public string Detail
        {
            get
            {
                if (Gap != null)
                {
                    return heatTransferText;
                }

                DraftPane pane = Pane;
                if (pane.Material == null)
                {
                    return "Its material is not available.";
                }

                if (pane.Material is TransparentMaterial material)
                {
                    // The faces as installed: a reversed pane shows the swapped emissivities (outside-facing first).
                    double external = Get(material, TransparentMaterialParameter.ExternalEmissivity);
                    double @internal = Get(material, TransparentMaterialParameter.InternalEmissivity);
                    if (pane.Reversed)
                    {
                        (external, @internal) = (@internal, external);
                    }

                    return string.Format(CultureInfo.CurrentCulture, "{0} mm · τsol {1} · LT {2} · ε {3} / {4}{5}{6}",
                        Millimetres(pane.Thickness), Format(Get(material, TransparentMaterialParameter.SolarTransmittance), "0.00"), Format(Get(material, TransparentMaterialParameter.LightTransmittance), "0.00"),
                        Format(external, "0.###"), Format(@internal, "0.###"), pane.Reversed ? " · reversed" : string.Empty, string.IsNullOrWhiteSpace(pane.SourceLabel) ? string.Empty : " · " + pane.SourceLabel);
                }

                return string.Format(CultureInfo.CurrentCulture, "{0} mm · not a glass pane", Millimetres(pane.Thickness));
            }
        }

        public bool Reversed => Pane?.Reversed ?? false;

        public IReadOnlyList<GlazingGasOption> GasOptions => gasOptions;

        /// <summary>The gap's gas; a change edits the draft and recalculates.</summary>
        public GlazingGasOption SelectedGas
        {
            get => Gap == null ? null : gasOptions.FirstOrDefault(x => x.Value == Gap.GasType);
            set
            {
                if (Gap != null && value != null && Gap.GasType != value.Value)
                {
                    Gap.GasType = value.Value;
                    Raise();
                    edited?.Invoke(this);
                }
            }
        }

        /// <summary>The gap's width [mm] as typed; empty or not a number means no width (an error the check lists).</summary>
        public string WidthText
        {
            get => widthText;
            set
            {
                value = value ?? string.Empty;
                if (Gap == null || widthText == value)
                {
                    return;
                }

                widthText = value;
                Gap.Thickness = TryMillimetres(value, out double metres) ? metres : double.NaN;
                Raise();
                edited?.Invoke(this);
            }
        }

        /// <summary>The check's findings for this layer, worst first; empty when there are none.</summary>
        public string IssueText => string.Join(" ", issues.Select(x => Glyph(x.Severity) + " " + x.Message));

        public bool HasIssue => issues.Count != 0;

        /// <summary>What a screen reader announces for the line: "Pane 1: Optifloat Clear 4mm, 4 mm …".</summary>
        public string AutomationName => Title + ": " + Name + (IsPane ? ", " : string.Empty) + (IsPane ? Detail : string.Format(CultureInfo.CurrentCulture, ", {0} mm{1}", widthText, string.IsNullOrEmpty(heatTransferText) ? string.Empty : ", " + heatTransferText)) + (HasIssue ? ". " + IssueText : string.Empty);

        public override string ToString() => AutomationName;

        // What the composed draft says about the layer (gap HTC) and what the check found.
        internal void Update(double heatTransferCoefficient, IEnumerable<GlazingDraftIssue> layerIssues)
        {
            heatTransferText = Gap == null || double.IsNaN(heatTransferCoefficient) ? string.Empty : string.Format(CultureInfo.CurrentCulture, "heat transfer {0:0.00} W/m²K", heatTransferCoefficient);
            issues = (layerIssues ?? Enumerable.Empty<GlazingDraftIssue>()).OrderByDescending(x => x.Severity).ToList();
            Raise();
        }

        internal void Refresh()
        {
            Raise();
        }

        internal static string Glyph(GlazingDraftIssueSeverity severity)
        {
            switch (severity)
            {
                case GlazingDraftIssueSeverity.Error:
                    return "✕ Error:";

                case GlazingDraftIssueSeverity.Warning:
                    return "⚠ Warning:";

                default:
                    return "ℹ Note:";
            }
        }

        internal static bool TryMillimetres(string text, out double metres)
        {
            metres = double.NaN;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if ((double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double mm) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out mm)) && !double.IsNaN(mm) && !double.IsInfinity(mm))
            {
                metres = mm / 1000;
                return true;
            }

            return false;
        }

        internal static string Millimetres(double metres)
        {
            return double.IsNaN(metres) ? string.Empty : Math.Round(metres * 1000, 2).ToString("0.##", CultureInfo.CurrentCulture);
        }

        private static double Get(TransparentMaterial material, TransparentMaterialParameter parameter)
        {
            return material.TryGetValue(parameter, out double value) ? value : double.NaN;
        }

        private static string Format(double value, string format)
        {
            return double.IsNaN(value) ? "–" : value.ToString(format, CultureInfo.CurrentCulture);
        }

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>One finding of the draft's check as a line of the Builder's validation list; choosing it selects the layer it concerns.</summary>
    public sealed class GlazingBuilderIssueRow
    {
        internal GlazingBuilderIssueRow(GlazingDraftIssue issue)
        {
            Issue = issue;
        }

        public GlazingDraftIssue Issue { get; }

        public GlazingDraftIssueSeverity Severity => Issue.Severity;

        public int? LayerIndex => Issue.LayerIndex;

        /// <summary>The finding with its severity in words, never colour alone.</summary>
        public string Text => GlazingBuilderLayerRow.Glyph(Issue.Severity) + " " + Issue.Message;

        public override string ToString() => Text;
    }
}
