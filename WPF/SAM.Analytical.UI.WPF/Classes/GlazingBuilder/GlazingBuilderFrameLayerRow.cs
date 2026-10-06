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
    /// <summary>
    /// One line of the Builder's frame layer list: a material and a thickness, in the order SAM stores the frame's layers. It is a view of a
    /// <see cref="DraftFrameLayer"/> of the Builder's own draft - editing the thickness here edits that draft layer and nothing else.
    /// </summary>
    public sealed class GlazingBuilderFrameLayerRow : INotifyPropertyChanged
    {
        private readonly Action<GlazingBuilderFrameLayerRow> edited;
        private string thicknessText;
        private IReadOnlyList<GlazingDraftIssue> issues = new List<GlazingDraftIssue>();

        internal GlazingBuilderFrameLayerRow(DraftFrameLayer layer, int index, Action<GlazingBuilderFrameLayerRow> edited)
        {
            Layer = layer;
            Index = index;
            this.edited = edited;
            thicknessText = GlazingBuilderLayerRow.Millimetres(layer.Thickness);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DraftFrameLayer Layer { get; }

        /// <summary>The position in the frame: 0 is the first layer in the order SAM stores them.</summary>
        public int Index { get; }

        /// <summary>"Layer 2".</summary>
        public string Title => "Layer " + (Index + 1).ToString(CultureInfo.CurrentCulture);

        /// <summary>The material's name as its source gives it.</summary>
        public string Name => Layer.DisplayName ?? Layer.Name ?? string.Empty;

        /// <summary>What a person chooses a frame material by: its conductivity and where it came from.</summary>
        public string Detail
        {
            get
            {
                if (Layer.Material == null)
                {
                    return "Its material is not available.";
                }

                double conductivity = Layer.Material is Material material ? material.ThermalConductivity : double.NaN;
                string source = string.IsNullOrWhiteSpace(Layer.SourceLabel) ? string.Empty : Layer.SourceLabel;
                string lambda = double.IsNaN(conductivity) ? string.Empty : string.Format(CultureInfo.CurrentCulture, "λ {0:0.###} W/mK", conductivity);
                return string.Join(" · ", new[] { lambda, source }.Where(x => !string.IsNullOrEmpty(x)));
            }
        }

        /// <summary>The layer's thickness [mm] as typed; empty or not a number means no thickness (an error the check lists).</summary>
        public string ThicknessText
        {
            get => thicknessText;
            set
            {
                value = value ?? string.Empty;
                if (thicknessText == value)
                {
                    return;
                }

                thicknessText = value;
                Layer.Thickness = GlazingBuilderLayerRow.TryMillimetres(value, out double metres) ? metres : double.NaN;
                Raise();
                edited?.Invoke(this);
            }
        }

        /// <summary>The check's findings for this layer, worst first; empty when there are none.</summary>
        public string IssueText => string.Join(" ", issues.Select(x => GlazingBuilderLayerRow.Glyph(x.Severity) + " " + x.Message));

        public bool HasIssue => issues.Count != 0;

        /// <summary>What a screen reader announces for the line: "Frame layer 1: Timber, 50 mm …".</summary>
        public string AutomationName => "Frame " + Title.ToLowerInvariant() + ": " + Name + ", " + thicknessText + " mm" + (HasIssue ? ". " + IssueText : string.Empty);

        public override string ToString() => AutomationName;

        internal void Update(IEnumerable<GlazingDraftIssue> layerIssues)
        {
            issues = (layerIssues ?? Enumerable.Empty<GlazingDraftIssue>()).OrderByDescending(x => x.Severity).ToList();
            Raise();
        }

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
