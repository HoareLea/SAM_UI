// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Composes a Glazing System Builder draft into a complete window system: the ONE conversion from the Builder's OUTSIDE → INSIDE
        /// list to SAM's INSIDE → OUTSIDE <see cref="ApertureConstruction"/> (<see cref="GlazingLayerOrder"/>), with a material library holding
        /// only what the system names.
        /// <list type="bullet">
        /// <item>A pane is a layer of its material (a reversed pane: a derived material "&lt;name&gt; Reversed" with the External / Internal
        /// faces swapped - the IGDB convention, JSON-identical to IGDB's own reversed entries).</item>
        /// <item>A gap is an ordinary layer naming a <see cref="GasMaterial"/> derived from the gas's default definition, with the heat transfer
        /// coefficient of its width at the evaluation orientation of the intended use (EN 673, SAM <c>Query.HeatTransferCoefficient</c>), its
        /// Default Gas Type set explicitly, and SAM's own name pattern ("Argon_16mm_1.16W/m2K_90deg") so identical gaps are one material.</item>
        /// <item>The frame (copied layers + materials) is written as is, with Default Frame Width when entered.</item>
        /// <item>Default Panel Type = the intended use (enum name, read back by <c>Analytical.Query.PanelType</c>); Description = the build-up;
        /// the construction's own U / g / LT parameters are NEVER written (Tas values belong to the apertures, on Apply).</item>
        /// </list>
        /// Never fails: a missing material leaves the layer without one (<see cref="GlazingComposition.IsComplete"/> false), which
        /// <see cref="CheckGlazingDraft"/> reports and which is never calculated or saved.
        /// </summary>
        public static GlazingComposition ComposeGlazingSystem(this GlazingSystemDraft draft, GlazingComposeOptions options = null)
        {
            if (draft == null)
            {
                return null;
            }

            Func<DefaultGasType, GasMaterial> gasSource = options?.GasSource ?? (x => Analytical.Query.DefaultGasMaterial(x));
            GlazingGapOrientation orientation = GlazingGapOrientation.For(draft.IntendedPanelType);

            MaterialLibrary materialLibrary = new MaterialLibrary("Glazing System Builder");
            List<GlazingDraftIssue> issues = new List<GlazingDraftIssue>();
            Dictionary<string, string> sourceLabels = new Dictionary<string, string>();
            Dictionary<DefaultGasType, GasMaterial> gases = new Dictionary<DefaultGasType, GasMaterial>();

            GlazingBuilderProvenance provenance = new GlazingBuilderProvenance()
            {
                BasedOnName = draft.BasedOnName,
                BasedOnGuid = draft.BasedOnGuid,
                IntendedPanelType = draft.IntendedPanelType,
                GapEvaluationTiltDegrees = orientation.TiltDegrees,
                GapHeatTransferBasis = orientation.Basis,
            };

            // Builder order: index 0 = outside.
            List<ConstructionLayer> layers_OutsideToInside = new List<ConstructionLayer>();
            List<string> description = new List<string>();
            int paneNumber = 0;
            int gapNumber = 0;
            for (int i = 0; i < draft.Layers.Count; i++)
            {
                DraftLayer layer = draft.Layers[i];
                if (layer is DraftPane pane)
                {
                    paneNumber++;
                    string name = pane.OriginalName ?? string.Empty;
                    IMaterial material = pane.Material;
                    if (material != null)
                    {
                        if (pane.Reversed && material is TransparentMaterial transparentMaterial)
                        {
                            material = Reverse(transparentMaterial);
                        }

                        name = LibraryMaterialMerge.Add(materialLibrary, material, pane.SourceLabel) ?? name;
                        if (!string.IsNullOrWhiteSpace(pane.SourceLabel))
                        {
                            sourceLabels[name] = pane.SourceLabel;
                        }
                    }

                    layers_OutsideToInside.Add(new ConstructionLayer(name, pane.Thickness));
                    description.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} mm{2}", pane.DisplayName ?? name, Millimetres(pane.Thickness), pane.Reversed ? " (reversed)" : string.Empty));
                    provenance.Panes.Add(new GlazingBuilderPaneRecord()
                    {
                        Position = paneNumber,
                        Material = name,
                        OriginalName = pane.OriginalName,
                        DisplayName = pane.DisplayName,
                        Category = pane.Category,
                        SourceLabel = pane.SourceLabel,
                        SourceFile = pane.SourceFileName,
                        Reversed = pane.Reversed,
                        Thickness = pane.Thickness,
                    });
                }
                else if (layer is DraftGap gap)
                {
                    gapNumber++;
                    double thickness = double.IsNaN(gap.Thickness) ? double.NaN : Math.Round(gap.Thickness, 4);
                    string gasName = Core.Query.Description(gap.GasType);
                    string name = string.Format(CultureInfo.InvariantCulture, "{0} gap", gasName);
                    double heatTransferCoefficient = double.NaN;

                    if (!gases.TryGetValue(gap.GasType, out GasMaterial definition))
                    {
                        definition = gap.GasType == DefaultGasType.Undefined ? null : gasSource(gap.GasType);
                        gases[gap.GasType] = definition;
                    }

                    if (definition == null && gap.GasType != DefaultGasType.Undefined)
                    {
                        issues.Add(new GlazingDraftIssue(GlazingDraftIssueSeverity.Error, GlazingDraftIssueCodes.GasUnavailable, string.Format(CultureInfo.CurrentCulture, "Gap {0}: no definition of {1} is available, so its heat transfer cannot be derived.", gapNumber, gasName), i));
                    }
                    else if (definition != null && !double.IsNaN(thickness) && thickness > 0)
                    {
                        heatTransferCoefficient = Math.Round(Analytical.Query.HeatTransferCoefficient(definition, thickness, orientation.TiltRadians), 3);
                        if (!double.IsNaN(heatTransferCoefficient))
                        {
                            GasMaterial gasMaterial = GapMaterial(definition, gap.GasType, thickness, heatTransferCoefficient, orientation.TiltDegrees);
                            name = LibraryMaterialMerge.Add(materialLibrary, gasMaterial) ?? gasMaterial.Name;
                        }
                    }

                    layers_OutsideToInside.Add(new ConstructionLayer(name, thickness));
                    description.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} mm", gasName, Millimetres(thickness)));
                    provenance.Gaps.Add(new GlazingBuilderGapRecord()
                    {
                        Position = gapNumber,
                        Gas = gasName,
                        Thickness = thickness,
                        HeatTransferCoefficient = heatTransferCoefficient,
                        TiltDegrees = orientation.TiltDegrees,
                        Material = name,
                    });
                }
            }

            // Frame: copied layers (in the source system's order) and their materials.
            DraftFrame frame = draft.Frame ?? DraftFrame.None();
            List<ConstructionLayer> frameLayers = null;
            if (!frame.IsNone)
            {
                Dictionary<string, string> names = new Dictionary<string, string>();
                foreach (IMaterial material in frame.Materials)
                {
                    string name = LibraryMaterialMerge.Add(materialLibrary, material, frame.SourceLabel);
                    if (name != null)
                    {
                        names[material.Name] = name;
                        if (!string.IsNullOrWhiteSpace(frame.SourceLabel))
                        {
                            sourceLabels[name] = frame.SourceLabel;
                        }
                    }
                }

                frameLayers = frame.Layers.Select(x => new ConstructionLayer(x.Name != null && names.TryGetValue(x.Name, out string name) ? name : x.Name, x.Thickness)).ToList();
                provenance.Frame = "Copied";
                provenance.FrameCopiedFromName = frame.CopiedFromName;
                provenance.FrameCopiedFromGuid = frame.CopiedFromGuid;
                provenance.FrameWidth = frame.Width > 0 ? frame.Width : double.NaN;
            }

            string systemName = string.IsNullOrWhiteSpace(draft.Name) ? "Unnamed glazing system" : draft.Name.Trim();
            ApertureConstruction apertureConstruction = new ApertureConstruction(options?.Guid ?? draft.EvaluationGuid, systemName, ApertureType.Window, GlazingLayerOrder.ToSam(layers_OutsideToInside), frameLayers);

            if (draft.IntendedPanelType != PanelType.Undefined)
            {
                apertureConstruction.SetValue(ApertureConstructionParameter.DefaultPanelType, draft.IntendedPanelType.ToString());
            }

            string frameText = frame.IsNone
                ? "no frame"
                : string.Format(CultureInfo.InvariantCulture, "frame copied from {0}{1}", frame.CopiedFromName ?? "another system", frame.Width > 0 ? string.Format(CultureInfo.InvariantCulture, ", {0} mm wide", Millimetres(frame.Width)) : string.Empty);
            apertureConstruction.SetValue(ApertureConstructionParameter.Description, string.Format(CultureInfo.InvariantCulture, "Glazing System Builder. Outside to inside: {0}; {1}.", description.Count == 0 ? "(no layers)" : string.Join(" | ", description), frameText));

            if (!frame.IsNone && frame.Width > 0)
            {
                apertureConstruction.SetValue(ApertureConstructionParameter.DefaultFrameWidth, frame.Width);
            }

            if (!double.IsNaN(draft.PaneAdditionalHeatTransfer))
            {
                apertureConstruction.SetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, draft.PaneAdditionalHeatTransfer);
            }

            if (!frame.IsNone && !double.IsNaN(frame.AdditionalHeatTransfer))
            {
                apertureConstruction.SetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, frame.AdditionalHeatTransfer);
            }

            return new GlazingComposition(apertureConstruction, materialLibrary, issues, provenance, sourceLabels);
        }

        /// <summary>The gas material of one gap, named as SAM names derived gases ("Argon_16mm_1.16W/m2K_90deg"; fractional widths keep one decimal).</summary>
        internal static GasMaterial GapMaterial(GasMaterial definition, DefaultGasType gasType, double thickness, double heatTransferCoefficient, double tiltDegrees)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "{0}_{1}mm_{2}W/m2K_{3}deg", Core.Query.Description(gasType), Millimetres(thickness), heatTransferCoefficient.ToString("0.###", CultureInfo.InvariantCulture), Math.Round(tiltDegrees, 0).ToString("0", CultureInfo.InvariantCulture));

            GasMaterial result = Analytical.Create.GasMaterial(definition, name, name, name, thickness, heatTransferCoefficient);

            // Set on the RESULT: SAM's Create.GasMaterial(gas, ...) copies the type onto its source instead (Stage E0 plan §21).
            result?.SetValue(GasMaterialParameter.DefaultGasType, Core.Query.Description(gasType));
            return result;
        }

        /// <summary>
        /// A pane installed the other way round: External and Internal solar reflectance, light reflectance and emissivity swapped
        /// (transmittances and thermal properties are the same both ways). Named "&lt;name&gt; Reversed" as IGDB names its reversed entries.
        /// </summary>
        internal static TransparentMaterial Reverse(TransparentMaterial transparentMaterial)
        {
            string name = transparentMaterial.Name + " Reversed";
            string displayName = string.IsNullOrWhiteSpace(transparentMaterial.DisplayName) ? name : transparentMaterial.DisplayName + " Reversed";
            TransparentMaterial result = new TransparentMaterial(name, Guid.NewGuid(), transparentMaterial, displayName, transparentMaterial.Description);

            Swap(transparentMaterial, result, TransparentMaterialParameter.ExternalSolarReflectance, TransparentMaterialParameter.InternalSolarReflectance);
            Swap(transparentMaterial, result, TransparentMaterialParameter.ExternalLightReflectance, TransparentMaterialParameter.InternalLightReflectance);
            Swap(transparentMaterial, result, TransparentMaterialParameter.ExternalEmissivity, TransparentMaterialParameter.InternalEmissivity);
            return result;
        }

        private static void Swap(TransparentMaterial source, TransparentMaterial result, TransparentMaterialParameter external, TransparentMaterialParameter @internal)
        {
            bool hasExternal = source.TryGetValue(external, out double value_External);
            bool hasInternal = source.TryGetValue(@internal, out double value_Internal);

            // In place (the parameter order is kept), so the result is JSON-identical to IGDB's own reversed entry.
            if (hasInternal)
            {
                result.SetValue(external, value_Internal);
            }
            else
            {
                result.RemoveValue(external);
            }

            if (hasExternal)
            {
                result.SetValue(@internal, value_External);
            }
            else
            {
                result.RemoveValue(@internal);
            }
        }

        private static string Millimetres(double metres)
        {
            return double.IsNaN(metres) ? "?" : Math.Round(metres * 1000, 1).ToString("0.#", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The orientation the gap heat transfer coefficients of a draft are derived for. EN 673's correlation depends on the heat-flow direction:
    /// roof glazing is evaluated horizontal with heat flow up (0°), everything else at the vertical reference (90°, as walls). Floor glazing
    /// (heat flow down, which SAM's correlation does not cover) uses the vertical reference, the higher - conservative - conductance.
    /// </summary>
    internal sealed class GlazingGapOrientation
    {
        private GlazingGapOrientation(double tiltDegrees, string basis)
        {
            TiltDegrees = tiltDegrees;
            Basis = basis;
        }

        public double TiltDegrees { get; }

        public double TiltRadians => TiltDegrees * Math.PI / 180;

        public string Basis { get; }

        internal static GlazingGapOrientation For(PanelType panelType)
        {
            const string method = "EN 673 (SAM Query.HeatTransferCoefficient, temperature difference 15 K, mean 283 K)";
            switch (panelType == PanelType.Undefined ? PanelGroup.Undefined : Analytical.Query.PanelGroup(panelType))
            {
                case PanelGroup.Roof:
                    return new GlazingGapOrientation(0, method + "; horizontal, heat flow up (roof glazing)");
                case PanelGroup.Floor:
                    return new GlazingGapOrientation(90, method + "; vertical reference (floor glazing: heat flow down is not covered)");
                case PanelGroup.Undefined:
                    return new GlazingGapOrientation(90, method + "; vertical reference (no intended use chosen)");
                default:
                    return new GlazingGapOrientation(90, method + "; vertical (wall glazing)");
            }
        }
    }
}
