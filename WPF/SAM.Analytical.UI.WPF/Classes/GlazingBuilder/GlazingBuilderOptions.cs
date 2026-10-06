// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Glazing System Builder is given to start from. It holds snapshots (a seed system with its materials, pools of panes and frames)
    /// and the shared services - never an analytical model: the Builder cannot read, change or Undo one.
    /// </summary>
    public sealed class GlazingBuilderOptions
    {
        /// <summary>The system the draft starts from (the chosen candidate, else the current system); null for an empty draft.</summary>
        public ApertureConstruction Seed { get; set; }

        /// <summary>
        /// True when <see cref="Seed"/> is a system saved in <see cref="Library"/> that the user opened to EDIT (My library → Open in Builder): the Builder
        /// then starts from a copy under the seed's own name, offers Save and replace (one transaction: the new system is saved and the seed is archived) as
        /// well as Save as new, and says "Editing a copy of …". The seed itself is never changed. Ignored when the library no longer holds the seed.
        /// </summary>
        public bool EditSeed { get; set; }

        /// <summary>The pool that holds the seed's materials (and gives the provenance label); null when there is no seed.</summary>
        public GlazingSource SeedSource { get; set; }

        /// <summary>
        /// Pools to take panes (the model's, the default library's) and frames to copy (from their window systems) from; "My glazing systems" may be
        /// among them for frames. The catalogue's remembered sources are added to the pane browser by <see cref="Catalog"/>.
        /// </summary>
        public IReadOnlyList<GlazingSource> Sources { get; set; } = new List<GlazingSource>();

        /// <summary>The Thermal Performance panel's source catalogue (shared: Add source… in the Builder lists the file there too); null for none.</summary>
        public ThermalSourceCatalog Catalog { get; set; }

        /// <summary>"My glazing systems": where Save writes. Required to save.</summary>
        public UserGlazingLibrary Library { get; set; }

        /// <summary>The performance calculation of the draft (the Builder owns and disposes it); null for a new Tas one.</summary>
        public DraftGlazingEvaluator Evaluator { get; set; }

        /// <summary>How drafts are composed for the checks (tests pass their gas definitions); null for SAM's default gases.</summary>
        public GlazingComposeOptions ComposeOptions { get; set; }

        /// <summary>Asks for the file of a new pane source (null when cancelled); the window supplies the open-file dialog.</summary>
        public Func<string> PickSourceFile { get; set; }

        /// <summary>How long typing in the pane search must settle before the list is filtered; null for the default, zero for at once.</summary>
        public TimeSpan? SearchDebounce { get; set; }
    }

    /// <summary>One choice of "intended use" (the Default Panel Type the saved system carries; it also decides the gap orientation).</summary>
    public sealed class GlazingIntendedUse
    {
        internal GlazingIntendedUse(PanelType value, string display)
        {
            Value = value;
            Display = display;
        }

        public PanelType Value { get; }

        public string Display { get; }

        public override string ToString() => Display;

        internal static List<GlazingIntendedUse> Options(PanelType also)
        {
            List<GlazingIntendedUse> result = new List<GlazingIntendedUse>()
            {
                new GlazingIntendedUse(PanelType.Undefined, "Not chosen"),
                new GlazingIntendedUse(PanelType.WallExternal, "External wall"),
                new GlazingIntendedUse(PanelType.Roof, "Roof (rooflight)"),
                new GlazingIntendedUse(PanelType.FloorExposed, "Exposed floor"),
            };

            // A seed made for another kind of panel keeps its use rather than losing it.
            if (result.All(x => x.Value != also))
            {
                result.Add(new GlazingIntendedUse(also, Core.Query.Description(also)));
            }

            return result;
        }
    }

    /// <summary>
    /// One choice of frame: no frame, an OWN frame (its layers are built in the Builder from the materials of the sources), or the frame of an existing
    /// complete system (its layers and materials are copied and may then be edited).
    /// </summary>
    public sealed class GlazingFrameChoice
    {
        internal GlazingFrameChoice(string label, ApertureConstruction system, MaterialLibrary materials, string sourceLabel)
        {
            Label = label;
            System = system;
            Materials = materials;
            SourceLabel = sourceLabel;
        }

        /// <summary>"None (glass only)" or "Frame of SIM_EXT_GLZ: 50 Timber (Model)".</summary>
        public string Label { get; }

        /// <summary>The system whose frame is copied; null for no frame.</summary>
        public ApertureConstruction System { get; }

        public MaterialLibrary Materials { get; }

        public string SourceLabel { get; }

        public bool IsNone => System == null && !IsOwn;

        /// <summary>The frame built in the Builder: it starts with no layers, which are added from the materials of the sources.</summary>
        public bool IsOwn { get; private set; }

        public override string ToString() => Label;

        internal static GlazingFrameChoice None() => new GlazingFrameChoice("None (glass only)", null, null, null);

        internal static GlazingFrameChoice Own() => new GlazingFrameChoice("Own frame (add the layers below)", null, null, null) { IsOwn = true };

        internal static GlazingFrameChoice Of(ApertureConstruction system, GlazingSource source)
        {
            string layers = string.Join(" + ", (system.FrameConstructionLayers ?? new List<ConstructionLayer>()).Where(x => x != null).Select(x => string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", x.Thickness * 1000, x.Name)));
            return new GlazingFrameChoice(string.Format(CultureInfo.CurrentCulture, "Frame of {0}: {1} ({2})", system.Name, layers, source?.Label), system, source?.ConstructionManager?.MaterialLibrary, source?.Label);
        }

        /// <summary>What makes two frames the same frame (their layers and the values copied with them), so a library full of one frame offers it once.</summary>
        internal string Signature
        {
            get
            {
                if (System == null)
                {
                    return string.Empty;
                }

                string layers = string.Join("|", (System.FrameConstructionLayers ?? new List<ConstructionLayer>()).Where(x => x != null).Select(x => x.Name + "=" + x.Thickness.ToString("R", CultureInfo.InvariantCulture)));
                double width = System.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double w) ? w : double.NaN;
                double additional = System.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double a) ? a : double.NaN;
                return layers + "#" + width.ToString("R", CultureInfo.InvariantCulture) + "#" + additional.ToString("R", CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>One solid material a frame layer can be made of, with where it comes from (a source's label; the file name when the source is a file).</summary>
    public sealed class GlazingFrameMaterialChoice
    {
        internal GlazingFrameMaterialChoice(IMaterial material, string sourceLabel, string sourceFileName)
        {
            Material = material;
            SourceLabel = sourceLabel;
            SourceFileName = sourceFileName;
            Name = material.Name;
            DisplayName = material is Material m && !string.IsNullOrWhiteSpace(m.DisplayName) ? m.DisplayName : material.Name;
            double conductivity = material is Material solid ? solid.ThermalConductivity : double.NaN;
            Detail = string.Format(CultureInfo.CurrentCulture, "{0}{1}", double.IsNaN(conductivity) ? string.Empty : string.Format(CultureInfo.CurrentCulture, "λ {0:0.###} W/mK", conductivity), string.IsNullOrWhiteSpace(sourceLabel) ? string.Empty : (double.IsNaN(conductivity) ? string.Empty : " · ") + sourceLabel);
        }

        public IMaterial Material { get; }

        public string Name { get; }

        public string DisplayName { get; }

        public string SourceLabel { get; }

        public string SourceFileName { get; }

        /// <summary>"λ 0.13 W/mK · Model".</summary>
        public string Detail { get; }

        /// <summary>The line of the picker: the name and the detail.</summary>
        public string Display => string.IsNullOrEmpty(Detail) ? DisplayName : DisplayName + " (" + Detail + ")";

        public override string ToString() => Display;
    }

    /// <summary>
    /// The reference window of the Builder's example Uw: 1.23 × 1.48 m (the standard window of EN ISO 10077-1), the draft's frame width all round,
    /// and NO spacer linear transmittance (Ψ). It is an example for comparing builds, labelled as such - never the U-value the model will get, which is
    /// calculated from each aperture's own areas when the system is applied.
    /// </summary>
    public static class GlazingReferenceWindow
    {
        public const double Width = 1.23;

        public const double Height = 1.48;

        /// <summary>(Ug·Apane + Uf·Aframe) / (Apane + Aframe) of the reference window; NaN without Ug, Uf or a frame width the window can hold.</summary>
        public static double Uw(double ug, double uf, double frameWidth)
        {
            if (double.IsNaN(ug) || double.IsNaN(uf) || double.IsNaN(frameWidth) || frameWidth <= 0 || 2 * frameWidth >= Math.Min(Width, Height))
            {
                return double.NaN;
            }

            double area_Pane = (Width - 2 * frameWidth) * (Height - 2 * frameWidth);
            double area_Frame = Width * Height - area_Pane;
            return (ug * area_Pane + uf * area_Frame) / (area_Pane + area_Frame);
        }

        /// <summary>The label of the example, e.g. "Uw example 1.31 W/m²K — 1.23 × 1.48 m, frame width 50 mm, no spacer Ψ".</summary>
        public static string Label(double uw, double frameWidth, bool frameless)
        {
            string window = string.Format(CultureInfo.CurrentCulture, "{0:0.00} × {1:0.00} m", Width, Height);
            return frameless
                ? string.Format(CultureInfo.CurrentCulture, "Uw example {0:0.00} W/m²K — {1}, no frame (= Ug)", uw, window)
                : string.Format(CultureInfo.CurrentCulture, "Uw example {0:0.00} W/m²K — {1}, frame width {2:0.#} mm, no spacer Ψ", uw, window, frameWidth * 1000);
        }
    }
}
