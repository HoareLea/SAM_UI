// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One authoring issue of a Glazing System Builder draft. <see cref="LayerIndex"/> is the draft's own index (OUTSIDE → INSIDE) when the
    /// issue belongs to a pane or gap. Issues found by SAM's own checks (<c>Create.Log</c>) keep SAM's text and say so (<see cref="FromSam"/>).
    /// </summary>
    public sealed class GlazingDraftIssue
    {
        public GlazingDraftIssue(GlazingDraftIssueSeverity severity, string code, string message, int? layerIndex = null, bool fromSam = false)
            : this(severity, code, message, layerIndex, fromSam, null)
        {
        }

        public GlazingDraftIssue(GlazingDraftIssueSeverity severity, string code, string message, int? layerIndex, bool fromSam, int? frameLayerNumber)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            LayerIndex = layerIndex;
            FromSam = fromSam;
            FrameLayerNumber = frameLayerNumber;
        }

        /// <summary>1-based number of the frame layer (in the order SAM stores them) the issue belongs to; null for any other issue.</summary>
        public int? FrameLayerNumber { get; }

        public GlazingDraftIssueSeverity Severity { get; }

        /// <summary>A stable identifier of the rule (<see cref="GlazingDraftIssueCodes"/>).</summary>
        public string Code { get; }

        public string Message { get; }

        public int? LayerIndex { get; }

        public bool FromSam { get; }

        public override string ToString()
        {
            return string.Format("{0} {1}: {2}", Severity, Code, Message);
        }
    }

    /// <summary>The rule identifiers of <see cref="GlazingDraftIssue.Code"/>.</summary>
    public static class GlazingDraftIssueCodes
    {
        // From SAM's own checks (Create.Log on the composed system), severities as SAM gives them unless noted.
        public const string NoPanes = "NoPanes";                         // SAM warning, raised to an error here
        public const string EmptyLayerName = "EmptyLayerName";
        public const string Thickness = "Thickness";
        public const string MissingMaterial = "MissingMaterial";
        public const string GasAtEdge = "GasAtEdge";
        public const string GasNotRecognised = "GasNotRecognised";
        public const string MaterialProperty = "MaterialProperty";
        public const string Sam = "Sam";                                 // any other SAM record

        // Builder rules.
        public const string MissingPaneMaterial = "MissingPaneMaterial";
        public const string ThicknessMissing = "ThicknessMissing";
        public const string NotGlass = "NotGlass";
        public const string UnsupportedGas = "UnsupportedGas";
        public const string GasUnavailable = "GasUnavailable";
        public const string MissingFrameMaterial = "MissingFrameMaterial";
        public const string PaneOnPane = "PaneOnPane";
        public const string GapOnGap = "GapOnGap";
        public const string GapWidth = "GapWidth";
        public const string ManyPanes = "ManyPanes";
        public const string NoIntendedUse = "NoIntendedUse";
        public const string FrameWidthMissing = "FrameWidthMissing";
        public const string FrameWidthInvalid = "FrameWidthInvalid";
        public const string FrameLayerThickness = "FrameLayerThickness";
        public const string FrameMaterialNotSolid = "FrameMaterialNotSolid";
        public const string Frameless = "Frameless";
        public const string NameRequired = "NameRequired";
        public const string DuplicateName = "DuplicateName";
    }

    /// <summary>The result of <see cref="Query.CheckGlazingDraft"/>.</summary>
    public sealed class GlazingDraftValidation
    {
        public GlazingDraftValidation(IEnumerable<GlazingDraftIssue> issues)
        {
            Issues = (issues ?? Enumerable.Empty<GlazingDraftIssue>()).Where(x => x != null).OrderByDescending(x => x.Severity).ToList();
        }

        /// <summary>Errors first, then warnings, then information.</summary>
        public IReadOnlyList<GlazingDraftIssue> Issues { get; }

        public bool HasErrors => Issues.Any(x => x.Severity == GlazingDraftIssueSeverity.Error);

        public IEnumerable<GlazingDraftIssue> Errors => Issues.Where(x => x.Severity == GlazingDraftIssueSeverity.Error);

        public IEnumerable<GlazingDraftIssue> Warnings => Issues.Where(x => x.Severity == GlazingDraftIssueSeverity.Warning);

        public bool Has(string code)
        {
            return Issues.Any(x => x.Code == code);
        }
    }
}
