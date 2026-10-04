// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>Gap widths the EN 673 gas correlation is adopted for in the Builder [m]; outside it is a warning, not an error.</summary>
        public const double GlazingGapWidthMin = 0.004;

        public const double GlazingGapWidthMax = 0.030;

        /// <summary>More panes than this is allowed but warned about.</summary>
        public const int GlazingPaneCountWarning = 4;

        /// <summary>
        /// The authoring check of a Glazing System Builder draft. SAM's own rules are not re-implemented: <c>Create.Log</c> runs on the composed
        /// system (no pane layers, a layer without name, thickness ≤ 0, a material missing from the library, gas as the first / last pane layer,
        /// an unrecognised gas, pane material properties) and its records are listed with the draft layer they concern. The Builder adds what
        /// SAM does not check: a missing pane, a material that is not glass in the pane stack, gases not offered, two panes in contact, two
        /// gaps in a row, gap widths outside 4-30 mm, more than four panes, no intended use, a frame without a width, no frame (information), and for a
        /// frame's own layers: a material that is missing or a gas, a thickness that is missing or not positive, and a typed width that is not a positive number.
        /// With <paramref name="savedNames"/> (the names already in My glazing systems) it also checks what Save needs: a name, not taken.
        /// A failed Tas calculation is a status of the evaluation, never an issue here.
        /// </summary>
        public static GlazingDraftValidation CheckGlazingDraft(this GlazingSystemDraft draft, GlazingComposition composition = null, IEnumerable<string> savedNames = null)
        {
            if (draft == null)
            {
                return new GlazingDraftValidation(null);
            }

            composition = composition ?? draft.ComposeGlazingSystem();
            List<GlazingDraftIssue> issues = new List<GlazingDraftIssue>();

            // The draft indices of the layers that were composed (in order, outside -> inside).
            List<int> draftIndices = new List<int>();
            for (int i = 0; i < draft.Layers.Count; i++)
            {
                if (draft.Layers[i] is DraftPane || draft.Layers[i] is DraftGap)
                {
                    draftIndices.Add(i);
                }
            }

            int paneNumber = 0;
            int gapNumber = 0;
            DraftLayer previous = null;
            foreach (int i in draftIndices)
            {
                DraftLayer layer = draft.Layers[i];
                if (layer is DraftPane pane)
                {
                    paneNumber++;
                    if (pane.Material == null)
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.MissingPaneMaterial, i, "Pane {0}: its material '{1}' is not available.", paneNumber, pane.OriginalName ?? string.Empty));
                    }
                    else if (!(pane.Material is TransparentMaterial))
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.NotGlass, i, "Pane {0}: '{1}' is not a glass pane (a {2}).", paneNumber, pane.DisplayName, MaterialKind(pane.Material)));
                    }

                    if (double.IsNaN(pane.Thickness))
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.ThicknessMissing, i, "Pane {0}: it has no thickness.", paneNumber));
                    }

                    if (previous is DraftPane)
                    {
                        issues.Add(Warning(GlazingDraftIssueCodes.PaneOnPane, i, "Pane {0} touches the pane before it: there is no cavity between them.", paneNumber));
                    }
                }
                else if (layer is DraftGap gap)
                {
                    gapNumber++;
                    string gas = Core.Query.Description(gap.GasType);
                    if (gap.GasType == DefaultGasType.Undefined)
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.UnsupportedGas, i, "Gap {0}: no gas is chosen.", gapNumber));
                    }
                    else if (!DraftGap.SupportedGasTypes.Contains(gap.GasType))
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.UnsupportedGas, i, "Gap {0}: {1} is not offered; use air, argon or krypton.", gapNumber, gas));
                    }

                    if (double.IsNaN(gap.Thickness))
                    {
                        issues.Add(Error(GlazingDraftIssueCodes.ThicknessMissing, i, "Gap {0}: it has no width.", gapNumber));
                    }
                    else if (gap.Thickness > 0 && (Math.Round(gap.Thickness, 4) < GlazingGapWidthMin || Math.Round(gap.Thickness, 4) > GlazingGapWidthMax))
                    {
                        issues.Add(Warning(GlazingDraftIssueCodes.GapWidth, i, "Gap {0}: {1:0.#} mm is outside {2:0}-{3:0} mm, where the gas heat transfer correlation (EN 673) is used here.", gapNumber, gap.Thickness * 1000, GlazingGapWidthMin * 1000, GlazingGapWidthMax * 1000));
                    }

                    if (previous is DraftGap)
                    {
                        issues.Add(Warning(GlazingDraftIssueCodes.GapOnGap, i, "Gap {0} follows another gap: two gas layers in a row.", gapNumber));
                    }
                }

                previous = layer;
            }

            if (paneNumber > GlazingPaneCountWarning)
            {
                issues.Add(Warning(GlazingDraftIssueCodes.ManyPanes, null, "{0} panes: more than {1} is unusual; check the build-up.", paneNumber, GlazingPaneCountWarning));
            }

            if (draft.IntendedPanelType == PanelType.Undefined)
            {
                issues.Add(Warning(GlazingDraftIssueCodes.NoIntendedUse, null, "No intended use is chosen: the system will not say which panels it is made for, and its gaps are evaluated as vertical."));
            }

            DraftFrame frame = draft.Frame ?? DraftFrame.None();
            if (frame.IsNone)
            {
                issues.Add(new GlazingDraftIssue(GlazingDraftIssueSeverity.Info, GlazingDraftIssueCodes.Frameless, "No frame: the system is glass only (Uw = Ug)."));
            }
            else
            {
                int frameNumber = 0;
                foreach (DraftFrameLayer frameLayer in frame.EditableLayers)
                {
                    frameNumber++;
                    // A layer whose material is missing is SAM's own record ("Frame: ... does not contain Material"), which the check lists once.
                    if (frameLayer.Material is GasMaterial)
                    {
                        issues.Add(FrameLayerIssue(GlazingDraftIssueSeverity.Error, GlazingDraftIssueCodes.FrameMaterialNotSolid, frameNumber, "Frame layer {0}: '{1}' is a gas; a frame layer needs a solid material.", frameNumber, frameLayer.DisplayName));
                    }
                    else if (frameLayer.Material is TransparentMaterial)
                    {
                        issues.Add(FrameLayerIssue(GlazingDraftIssueSeverity.Warning, GlazingDraftIssueCodes.FrameMaterialNotSolid, frameNumber, "Frame layer {0}: '{1}' is a glass material; Tas will calculate the frame as glazing.", frameNumber, frameLayer.DisplayName));
                    }

                    if (double.IsNaN(frameLayer.Thickness))
                    {
                        issues.Add(FrameLayerIssue(GlazingDraftIssueSeverity.Error, GlazingDraftIssueCodes.FrameLayerThickness, frameNumber, "Frame layer {0}: it has no thickness.", frameNumber));
                    }
                    else if (frameLayer.Thickness <= 0)
                    {
                        issues.Add(FrameLayerIssue(GlazingDraftIssueSeverity.Error, GlazingDraftIssueCodes.FrameLayerThickness, frameNumber, "Frame layer {0}: its thickness must be more than 0.", frameNumber));
                    }
                }

                if (frame.WidthInvalid)
                {
                    issues.Add(Error(GlazingDraftIssueCodes.FrameWidthInvalid, null, "The frame width is not a positive number of millimetres."));
                }
                else if (double.IsNaN(frame.Width) || frame.Width <= 0)
                {
                    issues.Add(Warning(GlazingDraftIssueCodes.FrameWidthMissing, null, "The frame has no width: SAM would use the frame layers' depth as the frame width."));
                }
            }

            issues.AddRange(composition?.Issues ?? Enumerable.Empty<GlazingDraftIssue>());
            issues.AddRange(SamIssues(draft, composition, draftIndices));

            if (savedNames != null)
            {
                string name = draft.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    issues.Add(Error(GlazingDraftIssueCodes.NameRequired, null, "The system needs a name."));
                }
                else if (savedNames.Any(x => string.Equals(x?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(Error(GlazingDraftIssueCodes.DuplicateName, null, "A system named '{0}' is already in My glazing systems; choose another name.", name));
                }
            }

            // One problem, one error: a layer that already has an error of its own does not also list "not in the library".
            HashSet<int> layersWithErrors = new HashSet<int>(issues.Where(x => x.Severity == GlazingDraftIssueSeverity.Error && x.LayerIndex.HasValue && x.Code != GlazingDraftIssueCodes.MissingMaterial).Select(x => x.LayerIndex.Value));
            issues.RemoveAll(x => x.Code == GlazingDraftIssueCodes.MissingMaterial && x.LayerIndex.HasValue && layersWithErrors.Contains(x.LayerIndex.Value));

            return new GlazingDraftValidation(issues);
        }

        // SAM's Create.Log on the composed system: the pane stack and the frame are logged as two systems so each record is attributable.
        private static IEnumerable<GlazingDraftIssue> SamIssues(GlazingSystemDraft draft, GlazingComposition composition, List<int> draftIndices)
        {
            ApertureConstruction apertureConstruction = composition?.ApertureConstruction;
            MaterialLibrary materialLibrary = composition?.MaterialLibrary ?? new MaterialLibrary(string.Empty);
            if (apertureConstruction == null)
            {
                yield break;
            }

            List<ConstructionLayer> paneLayers = apertureConstruction.PaneConstructionLayers ?? new List<ConstructionLayer>();
            ApertureConstruction panes = new ApertureConstruction(apertureConstruction.Guid, apertureConstruction.Name, ApertureType.Window, paneLayers, null);
            foreach (LogRecord logRecord in Records(Analytical.Create.Log(panes), Analytical.Create.Log(panes, materialLibrary)))
            {
                if (logRecord.Text.Contains("Frame ConstructionLayers"))
                {
                    continue;
                }

                GlazingDraftIssue issue = FromSam(logRecord, null, index => DraftIndex(index, paneLayers.Count, draftIndices));
                if (issue != null)
                {
                    yield return issue;
                }
            }

            List<ConstructionLayer> frameLayers = apertureConstruction.FrameConstructionLayers;
            if (frameLayers != null && frameLayers.Count != 0)
            {
                ApertureConstruction frame = new ApertureConstruction(apertureConstruction.Guid, apertureConstruction.Name, ApertureType.Window, null, frameLayers);
                foreach (LogRecord logRecord in Records(Analytical.Create.Log(frame), Analytical.Create.Log(frame, materialLibrary)))
                {
                    if (logRecord.Text.Contains("Pane ConstructionLayers"))
                    {
                        continue;
                    }

                    // A frame layer's thickness is checked above, with the layer's number; SAM's record of the same problem would only repeat it.
                    GlazingDraftIssue issue = FromSam(logRecord, "Frame", null);
                    if (issue != null && issue.Code != GlazingDraftIssueCodes.Thickness)
                    {
                        yield return issue;
                    }
                }
            }

            // The pane materials' own properties (SAM's material check); frame materials come from an existing system and are not re-judged.
            HashSet<string> logged = new HashSet<string>();
            for (int i = 0; i < paneLayers.Count; i++)
            {
                IMaterial material = materialLibrary.GetMaterial(paneLayers[i]?.Name);
                if (material == null || material is GasMaterial || !logged.Add(material.Name))
                {
                    continue;
                }

                int? layerIndex = DraftIndex(i, paneLayers.Count, draftIndices);
                foreach (LogRecord logRecord in Records(Analytical.Create.Log(material)))
                {
                    GlazingDraftIssue issue = FromSam(logRecord, null, null);
                    if (issue != null)
                    {
                        yield return new GlazingDraftIssue(issue.Severity, GlazingDraftIssueCodes.MaterialProperty, issue.Message, layerIndex, true);
                    }
                }
            }
        }

        private static IEnumerable<LogRecord> Records(params Log[] logs)
        {
            return logs.Where(x => x != null).SelectMany(x => x);
        }

        private static readonly Regex LayerIndexRegex = new Regex(@"Construction Layer Index: (\d+)", RegexOptions.Compiled);

        private static GlazingDraftIssue FromSam(LogRecord logRecord, string prefix, Func<int, int?> layerIndex)
        {
            GlazingDraftIssueSeverity severity;
            switch (logRecord.LogRecordType)
            {
                case LogRecordType.Error:
                    severity = GlazingDraftIssueSeverity.Error;
                    break;
                case LogRecordType.Warning:
                    severity = GlazingDraftIssueSeverity.Warning;
                    break;
                default:
                    return null;
            }

            string text = logRecord.Text ?? string.Empty;
            string code = GlazingDraftIssueCodes.Sam;
            int? index = null;
            if (text.Contains("has no Pane ConstructionLayers"))
            {
                code = GlazingDraftIssueCodes.NoPanes;
                severity = GlazingDraftIssueSeverity.Error;
            }
            else if (text.Contains("layer with no name"))
            {
                code = GlazingDraftIssueCodes.EmptyLayerName;
            }
            else if (text.Contains("thickness equal or less than 0"))
            {
                code = GlazingDraftIssueCodes.Thickness;
            }
            else if (text.Contains("does not contain Material"))
            {
                code = GlazingDraftIssueCodes.MissingMaterial;
            }
            else if (text.Contains("shall not be gas type"))
            {
                code = GlazingDraftIssueCodes.GasAtEdge;
                if (layerIndex != null && text.StartsWith("First aperture construction pane layer"))
                {
                    index = layerIndex(0);
                }
                else if (layerIndex != null && text.StartsWith("Last aperture construction pane layer"))
                {
                    index = layerIndex(int.MaxValue);
                }
            }
            else if (text.Contains("recogionzed") || text.Contains("recognized"))
            {
                code = GlazingDraftIssueCodes.GasNotRecognised;
            }

            Match match = LayerIndexRegex.Match(text);
            if (index == null && layerIndex != null && match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int samIndex))
            {
                index = layerIndex(samIndex);
            }

            string message = string.IsNullOrEmpty(prefix) ? text : prefix + ": " + text;
            return new GlazingDraftIssue(severity, code, message, index, true);
        }

        // SAM index (inside -> outside) of a composed pane layer to the draft index; int.MaxValue = SAM's last layer.
        private static int? DraftIndex(int samIndex, int count, List<int> draftIndices)
        {
            if (count == 0)
            {
                return null;
            }

            int builderIndex = GlazingLayerOrder.SamIndex(samIndex == int.MaxValue ? count - 1 : samIndex, count);
            return builderIndex >= 0 && builderIndex < draftIndices.Count ? draftIndices[builderIndex] : (int?)null;
        }

        private static string MaterialKind(IMaterial material)
        {
            switch (material)
            {
                case GasMaterial _:
                    return "gas";
                case OpaqueMaterial _:
                    return "opaque material";
                default:
                    return material?.GetType().Name ?? "material";
            }
        }

        private static GlazingDraftIssue Error(string code, int? layerIndex, string format, params object[] values)
        {
            return new GlazingDraftIssue(GlazingDraftIssueSeverity.Error, code, string.Format(CultureInfo.CurrentCulture, format, values), layerIndex);
        }

        private static GlazingDraftIssue FrameLayerIssue(GlazingDraftIssueSeverity severity, string code, int frameLayerNumber, string format, params object[] values)
        {
            return new GlazingDraftIssue(severity, code, string.Format(CultureInfo.CurrentCulture, format, values), null, false, frameLayerNumber);
        }

        private static GlazingDraftIssue Warning(string code, int? layerIndex, string format, params object[] values)
        {
            return new GlazingDraftIssue(GlazingDraftIssueSeverity.Warning, code, string.Format(CultureInfo.CurrentCulture, format, values), layerIndex);
        }
    }
}
