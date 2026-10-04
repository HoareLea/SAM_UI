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
    /// One layer of a <see cref="DraftFrame"/>: a material (a copy) and a thickness. The material is kept with where it came from (a label and a
    /// file NAME, never a path) so a saved system can say so; a layer whose material is not available is kept by name so the problem can be shown.
    /// </summary>
    public sealed class DraftFrameLayer
    {
        public DraftFrameLayer(IMaterial material, double thickness, string sourceLabel = null, string sourceFileName = null)
        {
            Material = material?.Clone();
            Name = material?.Name;
            Thickness = double.IsNaN(thickness) && material != null && material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness) ? defaultThickness : thickness;
            SourceLabel = DraftPane.FileNameOnly(sourceLabel);
            SourceFileName = DraftPane.FileNameOnly(sourceFileName);
        }

        /// <summary>A layer whose material is not available: kept by name so the check can name it.</summary>
        public static DraftFrameLayer Missing(string name, double thickness, string sourceLabel = null)
        {
            DraftFrameLayer result = new DraftFrameLayer(null, thickness, sourceLabel);
            result.Name = name;
            return result;
        }

        /// <summary>The material (a copy); null when missing.</summary>
        public IMaterial Material { get; }

        /// <summary>The material's name in its source.</summary>
        public string Name { get; private set; }

        /// <summary>[m]; NaN when none is entered (an error the check lists).</summary>
        public double Thickness { get; set; }

        /// <summary>Where the material came from, as shown to the user (the source's label or file name).</summary>
        public string SourceLabel { get; }

        /// <summary>The source file's NAME (no folder).</summary>
        public string SourceFileName { get; }

        public string DisplayName => Material is Material material && !string.IsNullOrWhiteSpace(material.DisplayName) ? material.DisplayName : Name;

        internal DraftFrameLayer Copy() => WithSource(SourceLabel, SourceFileName);

        internal DraftFrameLayer WithSource(string sourceLabel, string sourceFileName)
        {
            DraftFrameLayer result = new DraftFrameLayer(Material, Thickness, sourceLabel, sourceFileName);
            result.Name = Name;
            return result;
        }

        internal string Signature => (Material?.Guid.ToString() ?? Name ?? string.Empty) + "=" + Thickness.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The frame of a draft: none, or its layers (a material and a thickness each, in the order SAM stores them) and an explicit face width. The layers
    /// are either copied from an existing complete system (<see cref="CopyFrom"/>) and then free to be edited, or authored in the Builder by adding
    /// layers to a frame that has none. The Builder does not invent frame layers from a declared Uf, and the frame's additional heat transfer is NOT
    /// editable: it is carried with a copied frame (and shown read-only), and an authored frame has none.
    /// </summary>
    public sealed class DraftFrame
    {
        private readonly List<DraftFrameLayer> layers;
        private readonly string originalSignature;
        private bool authored;
        private bool editedMark;
        private double additionalHeatTransfer;
        private string copiedFromName;
        private Guid? copiedFromGuid;

        private DraftFrame(IEnumerable<DraftFrameLayer> layers, double width, double additionalHeatTransfer, string copiedFromName, Guid? copiedFromGuid, string sourceLabel, bool authored, bool edited)
        {
            this.layers = (layers ?? new DraftFrameLayer[0]).Where(x => x != null).Select(x => x.Copy()).ToList();
            Width = width;
            this.additionalHeatTransfer = additionalHeatTransfer;
            this.copiedFromName = copiedFromName;
            this.copiedFromGuid = copiedFromGuid;
            SourceLabel = DraftPane.FileNameOnly(sourceLabel);
            this.authored = authored;
            editedMark = edited;
            originalSignature = Signature();
        }

        public static DraftFrame None()
        {
            return new DraftFrame(null, double.NaN, double.NaN, null, null, null, false, false);
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

            List<DraftFrameLayer> frameLayers = new List<DraftFrameLayer>();
            foreach (ConstructionLayer layer in system.FrameConstructionLayers.Where(x => x != null))
            {
                IMaterial material = layer.Name == null ? null : materials?.GetMaterial(layer.Name);
                frameLayers.Add(material != null ? new DraftFrameLayer(material, layer.Thickness, sourceLabel) : DraftFrameLayer.Missing(layer.Name, layer.Thickness, sourceLabel));
            }

            double width = system.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out double value) && !double.IsNaN(value) && value > 0 ? value : double.NaN;
            double additionalHeatTransfer = system.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double additional) ? additional : double.NaN;

            return new DraftFrame(frameLayers, width, additionalHeatTransfer, system.Name, system.Guid, sourceLabel, false, false);
        }

        /// <summary>The same frame (layers, materials, width, additional heat transfer) recorded as copied from another system - for a system the Builder saved, whose frame came from somewhere else.</summary>
        /// <param name="edited">True when the saved system's frame was edited after the copy (its provenance says so).</param>
        internal DraftFrame WithOrigin(string copiedFromName, Guid? copiedFromGuid, bool edited = false)
        {
            return IsNone ? this : new DraftFrame(layers, Width, AdditionalHeatTransfer, copiedFromName, copiedFromGuid, SourceLabel, false, edited || IsEdited);
        }

        /// <summary>The same frame recorded as authored in the Builder (it was never copied) - for a system the Builder saved with a frame of its own.</summary>
        internal DraftFrame AsAuthored()
        {
            return IsNone ? this : new DraftFrame(layers, Width, double.NaN, null, null, SourceLabel, true, false);
        }

        /// <summary>The layers' own source labels / file names, as a saved system recorded them (by position); a layer without a record keeps its own.</summary>
        internal DraftFrame WithLayerSources(IReadOnlyList<GlazingBuilderFrameRecord> records)
        {
            if (IsNone || records == null || records.Count == 0)
            {
                return this;
            }

            List<DraftFrameLayer> result = new List<DraftFrameLayer>();
            for (int i = 0; i < layers.Count; i++)
            {
                GlazingBuilderFrameRecord record = records.FirstOrDefault(x => x.Position == i + 1);
                DraftFrameLayer layer = layers[i];
                if (record == null || string.IsNullOrWhiteSpace(record.SourceLabel) && string.IsNullOrWhiteSpace(record.SourceFile))
                {
                    result.Add(layer);
                    continue;
                }

                result.Add(layer.WithSource(string.IsNullOrWhiteSpace(record.SourceLabel) ? layer.SourceLabel : record.SourceLabel, string.IsNullOrWhiteSpace(record.SourceFile) ? layer.SourceFileName : record.SourceFile));
            }

            return new DraftFrame(result, Width, AdditionalHeatTransfer, copiedFromName, copiedFromGuid, SourceLabel, authored, editedMark);
        }

        public bool IsNone => layers.Count == 0;

        /// <summary>True for a frame built in the Builder: layers added to a frame that had none, not copied from a system.</summary>
        public bool IsAuthored => !IsNone && authored;

        /// <summary>True for a copied frame whose layers (materials or thicknesses) were changed after the copy, or that was saved so.</summary>
        public bool IsEdited => !IsNone && !authored && (editedMark || Signature() != originalSignature);

        /// <summary>The frame's layers, each with its material and thickness, in the order SAM stores them.</summary>
        public IReadOnlyList<DraftFrameLayer> EditableLayers => layers;

        /// <summary>The frame layers as SAM stores them (copies), in the same order.</summary>
        public IReadOnlyList<ConstructionLayer> Layers => layers.Select(x => new ConstructionLayer(x.Name, x.Thickness)).ToList();

        /// <summary>The materials of the frame layers (copies), one for each name.</summary>
        public IReadOnlyList<IMaterial> Materials => layers.Where(x => x.Material != null).GroupBy(x => x.Name).Select(x => x.First().Material.Clone()).ToList();

        /// <summary>The depth of the frame: the sum of its layers' thicknesses [m]; NaN while one has none.</summary>
        public double Depth => layers.Count == 0 || layers.Any(x => double.IsNaN(x.Thickness)) ? double.NaN : layers.Sum(x => x.Thickness);

        /// <summary>Frame face width [m], written as the system's Default Frame Width; NaN when not entered.</summary>
        public double Width { get; set; }

        /// <summary>True when a width was typed that is not a positive number (the check lists it as an error; <see cref="Width"/> is NaN then).</summary>
        public bool WidthInvalid { get; set; }

        /// <summary>
        /// Frame additional heat transfer [%], READ-ONLY: carried with a copied frame (and applied by Tas on top of the frame layers); NaN when the
        /// frame has none, which is always so for an authored frame.
        /// </summary>
        public double AdditionalHeatTransfer => additionalHeatTransfer;

        public string CopiedFromName => copiedFromName;

        public Guid? CopiedFromGuid => copiedFromGuid;

        public string SourceLabel { get; }

        /// <summary>Adds a layer of <paramref name="material"/> (its default thickness when <paramref name="thickness"/> is NaN) at <paramref name="index"/>, or at the end. A frame with no layers becomes an authored one.</summary>
        public DraftFrameLayer AddLayer(IMaterial material, double thickness = double.NaN, string sourceLabel = null, string sourceFileName = null, int? index = null)
        {
            if (material == null)
            {
                return null;
            }

            if (layers.Count == 0)
            {
                // A frame with no layers starts a frame of its own.
                authored = true;
                editedMark = false;
                additionalHeatTransfer = double.NaN;
                copiedFromName = null;
                copiedFromGuid = null;
            }

            DraftFrameLayer layer = new DraftFrameLayer(material, thickness, sourceLabel, sourceFileName);
            layers.Insert(index.HasValue ? Math.Max(0, Math.Min(index.Value, layers.Count)) : layers.Count, layer);
            return layer;
        }

        /// <summary>Gives the layer at <paramref name="index"/> another material (its thickness stays); returns the new layer.</summary>
        public DraftFrameLayer ReplaceMaterial(int index, IMaterial material, string sourceLabel = null, string sourceFileName = null)
        {
            if (material == null || index < 0 || index >= layers.Count)
            {
                return null;
            }

            DraftFrameLayer layer = new DraftFrameLayer(material, layers[index].Thickness, sourceLabel, sourceFileName);
            layers[index] = layer;
            return layer;
        }

        /// <summary>Removes the layer at <paramref name="index"/>. Removing the last one leaves no frame, and the copy / additional heat transfer with it.</summary>
        public bool RemoveLayerAt(int index)
        {
            if (index < 0 || index >= layers.Count)
            {
                return false;
            }

            layers.RemoveAt(index);
            if (layers.Count == 0)
            {
                authored = false;
                editedMark = false;
                additionalHeatTransfer = double.NaN;
                copiedFromName = null;
                copiedFromGuid = null;
                Width = double.NaN;
                WidthInvalid = false;
            }

            return true;
        }

        /// <summary>Moves the layer at <paramref name="from"/> to <paramref name="to"/>.</summary>
        public bool MoveLayer(int from, int to)
        {
            if (from < 0 || from >= layers.Count || to < 0 || to >= layers.Count || from == to)
            {
                return false;
            }

            DraftFrameLayer layer = layers[from];
            layers.RemoveAt(from);
            layers.Insert(to, layer);
            return true;
        }

        private string Signature()
        {
            return string.Join("|", layers.Select(x => x.Signature));
        }
    }
}
