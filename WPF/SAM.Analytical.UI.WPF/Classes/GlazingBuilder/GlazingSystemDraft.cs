// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A glazing system being authored in the Glazing System Builder (Stage E0): a name, an intended use, a stack of panes and gas
    /// gaps listed <b>OUTSIDE → INSIDE</b> (the order a user thinks in) and a frame. It is a plain editable object: any state is
    /// allowed while editing (two panes in contact, a gap at the edge, a missing pane), <see cref="Query.CheckGlazingDraft"/> lists what is
    /// wrong, and <see cref="Query.ComposeGlazingSystem"/> turns it into the complete <see cref="ApertureConstruction"/> SAM stores
    /// (INSIDE → OUTSIDE, see <see cref="GlazingLayerOrder"/>). The draft itself is never saved and never touches a model.
    /// </summary>
    public sealed class GlazingSystemDraft
    {
        public GlazingSystemDraft()
            : this(Guid.NewGuid())
        {
        }

        public GlazingSystemDraft(Guid evaluationGuid)
        {
            EvaluationGuid = evaluationGuid == Guid.Empty ? Guid.NewGuid() : evaluationGuid;
        }

        /// <summary>
        /// The Guid the composed system carries while it is a draft (Tas looks systems up by Guid). Stable for the whole Builder session so the
        /// same draft is the same system to the evaluator; never saved - saving gives the system a new Guid.
        /// </summary>
        public Guid EvaluationGuid { get; }

        public string Name { get; set; }

        /// <summary>What the system is made for (written as the construction's Default Panel Type); Undefined when not chosen.</summary>
        public PanelType IntendedPanelType { get; set; } = PanelType.Undefined;

        /// <summary>Panes and gaps, OUTSIDE first, INSIDE last.</summary>
        public List<DraftLayer> Layers { get; } = new List<DraftLayer>();

        public DraftFrame Frame { get; set; } = DraftFrame.None();

        /// <summary>Pane additional heat transfer [%] (copied from a seed system); NaN when not set.</summary>
        public double PaneAdditionalHeatTransfer { get; set; } = double.NaN;

        /// <summary>The system this draft started from (provenance only); null for a draft started from nothing.</summary>
        public string BasedOnName { get; set; }

        public Guid? BasedOnGuid { get; set; }

        public IEnumerable<DraftPane> Panes => Layers.OfType<DraftPane>();

        public IEnumerable<DraftGap> Gaps => Layers.OfType<DraftGap>();

        /// <summary>Appends layers on the INSIDE (the end of the outside → inside list).</summary>
        public GlazingSystemDraft Add(params DraftLayer[] layers)
        {
            foreach (DraftLayer layer in layers ?? new DraftLayer[0])
            {
                Layers.Add(layer);
            }

            return this;
        }
    }

    /// <summary>One layer of a <see cref="GlazingSystemDraft"/>: a <see cref="DraftPane"/> or a <see cref="DraftGap"/>.</summary>
    public abstract class DraftLayer
    {
        /// <summary>Thickness [m].</summary>
        public double Thickness { get; set; }
    }

    /// <summary>
    /// A pane in a draft: a snapshot of the pane material (normally a <see cref="TransparentMaterial"/> from a pane source such as an IGDB
    /// file) and where it came from. The source is recorded by LABEL and FILE NAME only - never an absolute path, which would travel into
    /// shared models with the system's provenance.
    /// </summary>
    public sealed class DraftPane : DraftLayer
    {
        /// <param name="material">The pane material; a copy is kept, so later changes to the source object do not reach the draft.</param>
        /// <param name="thickness">[m]; NaN takes the material's default thickness.</param>
        public DraftPane(IMaterial material, double thickness = double.NaN, string sourceLabel = null, string sourceFileName = null)
        {
            Material = material?.Clone();
            OriginalName = material?.Name;
            Thickness = double.IsNaN(thickness) && material != null && material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness) ? defaultThickness : thickness;
            SourceLabel = FileNameOnly(sourceLabel);
            SourceFileName = FileNameOnly(sourceFileName);
        }

        /// <summary>A pane whose material is not available (e.g. its source file is gone): kept by name so the problem can be shown.</summary>
        public static DraftPane Missing(string originalName, double thickness, string sourceLabel = null)
        {
            DraftPane result = new DraftPane(null, thickness, sourceLabel);
            result.OriginalName = originalName;
            return result;
        }

        /// <summary>The pane material (a copy); null when missing.</summary>
        public IMaterial Material { get; }

        /// <summary>The material's name in its source.</summary>
        public string OriginalName { get; private set; }

        /// <summary>
        /// True to install the pane the other way round: its External and Internal faces are swapped (the IGDB "… Reversed" convention,
        /// confirmed against real Tas in Stage E0-1 Gate 0). External always means the face towards the OUTSIDE of the built-up system.
        /// </summary>
        public bool Reversed { get; set; }

        /// <summary>Where the pane came from, as shown to the user (e.g. "International Glazing Database_v76-Pilkington.tcd").</summary>
        public string SourceLabel { get; }

        /// <summary>The source file's NAME (no folder).</summary>
        public string SourceFileName { get; }

        public string DisplayName => Material is Material material && !string.IsNullOrWhiteSpace(material.DisplayName) ? material.DisplayName : OriginalName;

        /// <summary>The material's category (e.g. "Material Root\Pilkington"), or empty.</summary>
        public string Category
        {
            get
            {
                if (Material is ParameterizedSAMObject parameterized && parameterized.TryGetValue(ParameterizedSAMObjectParameter.Category, out Category category) && category != null)
                {
                    return category.ToString("\\") ?? string.Empty;
                }

                return string.Empty;
            }
        }

        /// <summary>A file name from a label or path: the folder part is dropped so no machine path is ever recorded.</summary>
        internal static string FileNameOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            string trimmed = value.Trim();
            int index = trimmed.LastIndexOfAny(new[] { '\\', '/' });
            if (index >= 0)
            {
                trimmed = trimmed.Substring(index + 1);
            }

            // A bare drive ("C:name") cannot be a file name (Windows file names have no colon).
            index = trimmed.LastIndexOf(':');
            return (index >= 0 ? trimmed.Substring(index + 1) : trimmed).Trim();
        }
    }

    /// <summary>A gas gap in a draft: a gas type and a width. The gas material (with its heat transfer coefficient) is derived on compose.</summary>
    public sealed class DraftGap : DraftLayer
    {
        public DraftGap(DefaultGasType gasType, double thickness)
        {
            GasType = gasType;
            Thickness = thickness;
        }

        public DefaultGasType GasType { get; set; }

        /// <summary>The gases the Builder offers in E0 (the default data of the others is not trusted, see the Stage E0 plan §21).</summary>
        public static IReadOnlyList<DefaultGasType> SupportedGasTypes { get; } = new[] { DefaultGasType.Air, DefaultGasType.Argon, DefaultGasType.Krypton };
    }

    /// <summary>
    /// The frame of a draft: none, or the frame layers (and their materials) copied from an existing complete system, with an explicit,
    /// editable face width. The Builder never invents frame layers (no frame from a target Uf in E0). Frame layers are kept in the order the
    /// source system stores them: they are not part of the pane stack and are not reordered.
    /// </summary>
    public sealed class DraftFrame
    {
        private DraftFrame(IEnumerable<ConstructionLayer> layers, IEnumerable<IMaterial> materials, double width, double additionalHeatTransfer, string copiedFromName, Guid? copiedFromGuid, string sourceLabel)
        {
            Layers = (layers ?? new ConstructionLayer[0]).Where(x => x != null).Select(x => new ConstructionLayer(x)).ToList();
            Materials = (materials ?? new IMaterial[0]).Where(x => x != null).Select(x => x.Clone()).ToList();
            Width = width;
            AdditionalHeatTransfer = additionalHeatTransfer;
            CopiedFromName = copiedFromName;
            CopiedFromGuid = copiedFromGuid;
            SourceLabel = DraftPane.FileNameOnly(sourceLabel);
        }

        public static DraftFrame None()
        {
            return new DraftFrame(null, null, double.NaN, double.NaN, null, null, null);
        }

        /// <summary>
        /// The frame of <paramref name="system"/>: its frame layers, the materials they name (from <paramref name="materials"/>; a missing one is
        /// reported on compose) and its Frame Additional Heat Transfer. The width is the system's Default Frame Width when it has one, else NaN
        /// (to be entered: without it SAM falls back to the frame layers' depth as the face width).
        /// </summary>
        public static DraftFrame CopyFrom(ApertureConstruction system, MaterialLibrary materials, string sourceLabel = null)
        {
            if (system == null || !system.HasFrameConstructionLayers())
            {
                return None();
            }

            List<ConstructionLayer> layers = system.FrameConstructionLayers;
            List<IMaterial> frameMaterials = new List<IMaterial>();
            foreach (string name in layers.Where(x => x?.Name != null).Select(x => x.Name).Distinct())
            {
                IMaterial material = materials?.GetMaterial(name);
                if (material != null)
                {
                    frameMaterials.Add(material);
                }
            }

            double width = system.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double value) && !double.IsNaN(value) && value > 0 ? value : double.NaN;
            double additionalHeatTransfer = system.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double additional) ? additional : double.NaN;

            return new DraftFrame(layers, frameMaterials, width, additionalHeatTransfer, system.Name, system.Guid, sourceLabel);
        }

        public bool IsNone => Layers.Count == 0;

        /// <summary>The copied frame layers (copies), in the source system's order.</summary>
        public IReadOnlyList<ConstructionLayer> Layers { get; }

        /// <summary>The materials of the frame layers (copies).</summary>
        public IReadOnlyList<IMaterial> Materials { get; }

        /// <summary>Frame face width [m], written as the system's Default Frame Width; NaN when not entered.</summary>
        public double Width { get; set; }

        /// <summary>Frame additional heat transfer [%] copied from the source system; NaN when it has none.</summary>
        public double AdditionalHeatTransfer { get; }

        public string CopiedFromName { get; }

        public Guid? CopiedFromGuid { get; }

        public string SourceLabel { get; }
    }
}
