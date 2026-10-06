// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Where the performance block of the Builder stands.</summary>
    public enum GlazingBuilderPerformanceState
    {
        /// <summary>Nothing asked yet.</summary>
        None,

        /// <summary>Tas is calculating the build-up as it is now; the values shown (if any) are the last ones and are greyed.</summary>
        Calculating,

        Calculated,

        /// <summary>No values: the build-up has errors, or Tas is unavailable / returned nothing. The reason is shown.</summary>
        NotCalculated,
    }

    /// <summary>
    /// The Glazing System Builder (Stage E0-3) without any WPF type: a TEMPORARY <see cref="GlazingSystemDraft"/> the user edits (panes, Air /
    /// Argon / Krypton gaps, a frame - none, copied from an existing system, or built from the materials of the sources, its layers editable - and an
    /// intended use), the pane browser, the draft's check and its
    /// performance, and Save as predefined into "My glazing systems".
    /// <list type="bullet">
    /// <item><b>No model.</b> It starts from snapshots (<see cref="GlazingBuilderOptions"/>) and has no analytical model, so opening it, editing, previewing,
    /// saving or cancelling cannot change one or add an Undo step. Save writes ONLY the user library (<see cref="UserGlazingLibrary.Save"/>, whose
    /// <see cref="UserGlazingLibrary.Changed"/> event refreshes the open candidate lists); it does not touch any list itself.</item>
    /// <item><b>Existing evaluation.</b> Ug / g / LT / Uf come from <see cref="DraftGlazingEvaluator"/> (the existing glazing calculation on a
    /// transient system); an answer that is not for the newest edit is dropped, never shown as current.</item>
    /// <item><b>Existing validation.</b> <see cref="Query.CheckGlazingDraft"/>; Save is disabled while it has errors.</item>
    /// </list>
    /// </summary>
    public sealed class GlazingBuilderViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GlazingBuilderOptions options;
        private readonly GlazingSystemDraft draft;
        private readonly DraftGlazingEvaluator evaluator;
        private readonly UserGlazingLibrary library;
        private readonly GlazingComposeOptions composeOptions;
        private readonly ObservableCollection<GlazingBuilderLayerRow> layers = new ObservableCollection<GlazingBuilderLayerRow>();
        private readonly List<GlazingFrameChoice> frameChoices = new List<GlazingFrameChoice>();
        private readonly ObservableCollection<GlazingBuilderFrameLayerRow> frameLayers = new ObservableCollection<GlazingBuilderFrameLayerRow>();
        private readonly List<GlazingFrameMaterialChoice> allFrameMaterials = new List<GlazingFrameMaterialChoice>();
        private readonly List<string> savedNames = new List<string>();
        private readonly List<KeyValuePair<Guid, string>> savedEntries = new List<KeyValuePair<Guid, string>>();
        private readonly SynchronizationContext context = SynchronizationContext.Current;

        private GlazingBuilderLayerRow selectedLayer;
        private GlazingFrameChoice selectedFrame;
        private GlazingBuilderFrameLayerRow selectedFrameLayer;
        private GlazingFrameMaterialChoice selectedFrameMaterial;
        private IReadOnlyList<GlazingFrameMaterialChoice> frameMaterials = new List<GlazingFrameMaterialChoice>();
        private string frameMaterialSearch = string.Empty;
        private bool ownFrame;
        private GlazingIntendedUse selectedUse;
        private string frameWidthText = string.Empty;
        private bool frameWidthInferred;
        private double inferredWidth = double.NaN;

        private GlazingComposition composition;
        private GlazingDraftValidation validation = new GlazingDraftValidation(null);
        private GlazingDraftValidation validationReplace = new GlazingDraftValidation(null);
        private Guid? editedGuid;
        private string editedName;
        private IReadOnlyList<GlazingBuilderIssueRow> issues = new List<GlazingBuilderIssueRow>();

        private long version;
        private GlazingBuilderPerformanceState performanceState = GlazingBuilderPerformanceState.None;
        private GlazingValues values;
        private string valuesKey;
        private string performanceReason;
        private bool disposed;
        private bool isSaving;
        private string saveError;
        private ApertureConstruction savedSystem;
        private bool changing;

        public GlazingBuilderViewModel(GlazingBuilderOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            library = options.Library;
            composeOptions = options.ComposeOptions;
            evaluator = options.Evaluator ?? new DraftGlazingEvaluator(null, null, null, options.ComposeOptions);

            ReadSavedNames();

            ApertureConstruction seed = options.Seed;
            draft = seed != null && options.SeedSource != null ? SeedDraft(seed, options.SeedSource) : new GlazingSystemDraft() { IntendedPanelType = PanelType.Undefined };

            // Editing a saved system: only when the library still holds it (it is replaced by Guid, under the library's lock).
            if (options.EditSeed && seed != null && library != null && savedEntries.Any(x => x.Key == seed.Guid))
            {
                editedGuid = seed.Guid;
                editedName = savedEntries.First(x => x.Key == seed.Guid).Value;
            }

            draft.Name = editedGuid != null ? editedName : UniqueName(seed == null ? "New glazing system" : seed.Name + " (copy)");

            IntendedUses = GlazingIntendedUse.Options(draft.IntendedPanelType);
            selectedUse = IntendedUses.First(x => x.Value == draft.IntendedPanelType);

            // Panes: the model's and the default library's, then the catalogue's remembered / added sources (shared with the Thermal Performance panel).
            Panes = new GlazingPaneBrowser(options.Sources, options.Catalog, options.PickSourceFile, options.SearchDebounce);

            // Add pane / Replace pane depend on the pane chosen in the browser.
            Panes.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(GlazingPaneBrowser.SelectedEntry))
                {
                    RaiseCommands();
                }
            };

            BuildFrameChoices();
            BuildFrameMaterials();
            frameWidthInferred = draft.Frame.IsNone || draft.Frame.IsAuthored ? false : InferFrameWidth(draft.Frame);
            frameWidthText = draft.Frame.IsNone || double.IsNaN(draft.Frame.Width) ? string.Empty : GlazingBuilderLayerRow.Millimetres(draft.Frame.Width);
            RebuildFrameLayers(null);

            Rebuild(null);
            Update(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raised once after a successful Save; <see cref="SavedSystem"/> is the system as saved. The host then selects it in the candidate list.</summary>
        public event EventHandler Saved;

        /// <summary>The temporary draft (the Builder's own; edit it through this view-model).</summary>
        public GlazingSystemDraft Draft => draft;

        public GlazingPaneBrowser Panes { get; }

        /// <summary>The build-up, OUTSIDE → INSIDE.</summary>
        public ObservableCollection<GlazingBuilderLayerRow> Layers => layers;

        public GlazingBuilderLayerRow SelectedLayer
        {
            get => selectedLayer;
            set
            {
                if (!ReferenceEquals(selectedLayer, value))
                {
                    selectedLayer = value;
                    Raise(nameof(SelectedLayer));
                    RaiseCommands();
                }
            }
        }

        // ---- Name, intended use, status -------------------------------------------------------------------------

        public string Name
        {
            get => draft.Name;
            set
            {
                if (isSaving || draft.Name == value)
                {
                    return;
                }

                draft.Name = value;
                Raise(nameof(Name));
                Update(false);
            }
        }

        public IReadOnlyList<GlazingIntendedUse> IntendedUses { get; }

        public GlazingIntendedUse SelectedIntendedUse
        {
            get => selectedUse;
            set
            {
                if (isSaving || value == null || ReferenceEquals(selectedUse, value))
                {
                    return;
                }

                selectedUse = value;
                draft.IntendedPanelType = value.Value;
                Raise(nameof(SelectedIntendedUse));
                Update(true);
            }
        }

        /// <summary>True when the Builder was opened on a saved system to edit it (see <see cref="GlazingBuilderOptions.EditSeed"/>): Save and replace is offered.</summary>
        public bool IsEditing => editedGuid != null;

        /// <summary>The name of the saved system being edited; null when not editing.</summary>
        public string EditedName => editedName;

        /// <summary>"New · based on SIM_EXT_GLZ · not saved" / "Editing a copy of X · saving creates a new system" / "Saved to My glazing systems".</summary>
        public string StatusText
        {
            get
            {
                if (savedSystem != null)
                {
                    return SaveResult?.Replaced != null
                        ? string.Format(CultureInfo.CurrentCulture, "Saved to My glazing systems as {0}, replacing {1} (kept in the archive).", savedSystem.Name, SaveResult.Replaced.Name)
                        : string.Format(CultureInfo.CurrentCulture, "Saved to My glazing systems as {0}.", savedSystem.Name);
                }

                if (IsEditing)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Editing a copy of {0} · saving creates a new system", editedName);
                }

                return draft.BasedOnName == null ? "New · not saved" : string.Format(CultureInfo.CurrentCulture, "New · based on {0} · not saved", draft.BasedOnName);
            }
        }

        // ---- Layers: commands ------------------------------------------------------------------------------------

        public bool CanAddPane => !isSaving && Panes.SelectedEntry != null;

        public bool CanReplacePane => !isSaving && Panes.SelectedEntry != null && selectedLayer != null && selectedLayer.IsPane;

        public bool CanRemove => !isSaving && selectedLayer != null;

        public bool CanMoveUp => !isSaving && selectedLayer != null && selectedLayer.Index > 0;

        public bool CanMoveDown => !isSaving && selectedLayer != null && selectedLayer.Index < layers.Count - 1;

        public bool CanReverse => !isSaving && selectedLayer != null && selectedLayer.IsPane && selectedLayer.Pane.Material is TransparentMaterial;

        /// <summary>
        /// Adds the pane chosen in the browser after the selected layer (at the INSIDE end when nothing is selected). A pane that would touch the pane
        /// before it gets a gap first (the previous gap's gas and width, else Argon 16 mm), so a stack stays pane | gap | pane. The new pane is selected.
        /// </summary>
        public bool AddPane(GlazingPaneEntry entry = null)
        {
            entry = entry ?? Panes.SelectedEntry;
            if (isSaving || entry == null)
            {
                return false;
            }

            int at = selectedLayer == null ? draft.Layers.Count : selectedLayer.Index + 1;
            List<DraftLayer> insert = new List<DraftLayer>();
            if (at > 0 && draft.Layers[at - 1] is DraftPane)
            {
                DraftGap previousGap = draft.Layers.OfType<DraftGap>().LastOrDefault();
                insert.Add(previousGap != null ? new DraftGap(previousGap.GasType, previousGap.Thickness) : new DraftGap(DefaultGasType.Argon, 0.016));
            }

            DraftPane pane = entry.ToDraftPane();
            insert.Add(pane);
            draft.Layers.InsertRange(at, insert);
            Changed(pane);
            return true;
        }

        /// <summary>Replaces the selected pane with the pane chosen in the browser (the new pane is not reversed).</summary>
        public bool ReplacePane(GlazingPaneEntry entry = null)
        {
            entry = entry ?? Panes.SelectedEntry;
            if (isSaving || entry == null || selectedLayer == null || !selectedLayer.IsPane)
            {
                return false;
            }

            DraftPane pane = entry.ToDraftPane();
            draft.Layers[selectedLayer.Index] = pane;
            Changed(pane);
            return true;
        }

        /// <summary>Adds a gap (Argon 16 mm, or the last gap's gas and width) after the selected layer, or at the INSIDE end.</summary>
        public bool AddGap()
        {
            if (isSaving)
            {
                return false;
            }

            DraftGap previousGap = draft.Layers.OfType<DraftGap>().LastOrDefault();
            DraftGap gap = previousGap != null ? new DraftGap(previousGap.GasType, previousGap.Thickness) : new DraftGap(DefaultGasType.Argon, 0.016);
            draft.Layers.Insert(selectedLayer == null ? draft.Layers.Count : selectedLayer.Index + 1, gap);
            Changed(gap);
            return true;
        }

        public bool RemoveSelected()
        {
            if (!CanRemove)
            {
                return false;
            }

            int index = selectedLayer.Index;
            draft.Layers.RemoveAt(index);
            DraftLayer next = draft.Layers.Count == 0 ? null : draft.Layers[Math.Min(index, draft.Layers.Count - 1)];
            Changed(next);
            return true;
        }

        /// <summary>Moves the selected layer one place outwards (-1) or inwards (+1).</summary>
        public bool MoveSelected(int delta)
        {
            if (isSaving || selectedLayer == null || delta == 0)
            {
                return false;
            }

            int from = selectedLayer.Index;
            int to = from + Math.Sign(delta);
            if (to < 0 || to >= draft.Layers.Count)
            {
                return false;
            }

            DraftLayer layer = draft.Layers[from];
            draft.Layers[from] = draft.Layers[to];
            draft.Layers[to] = layer;
            Changed(layer);
            return true;
        }

        /// <summary>Installs the selected pane the other way round (its outside- and inside-facing values swap: Gate 0 semantics of E0-1); again undoes it.</summary>
        public bool ToggleReverse()
        {
            if (!CanReverse)
            {
                return false;
            }

            DraftPane pane = selectedLayer.Pane;
            pane.Reversed = !pane.Reversed;
            Changed(pane);
            return true;
        }

        /// <summary>Selects the layer a finding concerns.</summary>
        public void SelectIssue(GlazingBuilderIssueRow issue)
        {
            if (issue?.LayerIndex != null && issue.LayerIndex.Value >= 0 && issue.LayerIndex.Value < layers.Count)
            {
                SelectedLayer = layers[issue.LayerIndex.Value];
            }
        }

        // ---- Frame ----------------------------------------------------------------------------------------------

        /// <summary>No frame, an own frame (built here), and the frames of the sources' systems to copy. A copied frame's layers can be edited like an own one's.</summary>
        public IReadOnlyList<GlazingFrameChoice> FrameChoices => frameChoices;

        public GlazingFrameChoice SelectedFrame
        {
            get => selectedFrame;
            set
            {
                if (isSaving || value == null || ReferenceEquals(selectedFrame, value))
                {
                    return;
                }

                selectedFrame = value;
                ownFrame = value.IsOwn;
                draft.Frame = value.IsNone || value.IsOwn ? DraftFrame.None() : DraftFrame.CopyFrom(value.System, value.Materials, value.SourceLabel);
                frameWidthInferred = !draft.Frame.IsNone && InferFrameWidth(draft.Frame);
                frameWidthText = draft.Frame.IsNone || double.IsNaN(draft.Frame.Width) ? string.Empty : GlazingBuilderLayerRow.Millimetres(draft.Frame.Width);
                RebuildFrameLayers(null);
                Raise(nameof(SelectedFrame));
                RaiseFrame();
                Update(true);
            }
        }

        public bool HasFrame => !draft.Frame.IsNone;

        /// <summary>True while the frame's layers can be worked on: a frame has layers, or an own frame was chosen (and is waiting for its first).</summary>
        public bool ShowFrameEditor => HasFrame || selectedFrame.IsOwn;

        /// <summary>The frame's layers in the order SAM stores them; a change edits the draft and recalculates.</summary>
        public ObservableCollection<GlazingBuilderFrameLayerRow> FrameLayers => frameLayers;

        public GlazingBuilderFrameLayerRow SelectedFrameLayer
        {
            get => selectedFrameLayer;
            set
            {
                if (!ReferenceEquals(selectedFrameLayer, value))
                {
                    selectedFrameLayer = value;
                    Raise(nameof(SelectedFrameLayer));
                    RaiseFrameCommands();
                }
            }
        }

        /// <summary>The solid materials a frame layer can be made of: those of the model, the libraries and the added sources, narrowed by <see cref="FrameMaterialSearchText"/>.</summary>
        public IReadOnlyList<GlazingFrameMaterialChoice> FrameMaterials => frameMaterials;

        public GlazingFrameMaterialChoice SelectedFrameMaterial
        {
            get => selectedFrameMaterial;
            set
            {
                if (!ReferenceEquals(selectedFrameMaterial, value))
                {
                    selectedFrameMaterial = value;
                    Raise(nameof(SelectedFrameMaterial));
                    RaiseFrameCommands();
                }
            }
        }

        /// <summary>Words to find in a material's name or source; empty lists them all.</summary>
        public string FrameMaterialSearchText
        {
            get => frameMaterialSearch;
            set
            {
                value = value ?? string.Empty;
                if (frameMaterialSearch == value)
                {
                    return;
                }

                frameMaterialSearch = value;
                FilterFrameMaterials();
                Raise(nameof(FrameMaterialSearchText));
            }
        }

        public string FrameMaterialCountText => frameMaterials.Count == allFrameMaterials.Count
            ? string.Format(CultureInfo.CurrentCulture, "{0} solid {1}", allFrameMaterials.Count, allFrameMaterials.Count == 1 ? "material" : "materials")
            : string.Format(CultureInfo.CurrentCulture, "{0} of {1} solid materials", frameMaterials.Count, allFrameMaterials.Count);

        public bool CanAddFrameLayer => !isSaving && selectedFrameMaterial != null;

        public bool CanReplaceFrameMaterial => !isSaving && selectedFrameMaterial != null && selectedFrameLayer != null;

        public bool CanRemoveFrameLayer => !isSaving && selectedFrameLayer != null;

        public bool CanMoveFrameLayerUp => !isSaving && selectedFrameLayer != null && selectedFrameLayer.Index > 0;

        public bool CanMoveFrameLayerDown => !isSaving && selectedFrameLayer != null && selectedFrameLayer.Index < frameLayers.Count - 1;

        /// <summary>
        /// Adds a layer of the chosen material (its default thickness, else 30 mm) after the selected frame layer, or at the end. On a frame with no layers
        /// this starts an own frame. The new layer is selected. Edits the draft only.
        /// </summary>
        public bool AddFrameLayer(GlazingFrameMaterialChoice choice = null)
        {
            choice = choice ?? selectedFrameMaterial;
            if (isSaving || choice == null)
            {
                return false;
            }

            if (draft.Frame.IsNone)
            {
                // The first layer of a frame that had none: the frame is an own one now.
                ownFrame = true;
                frameWidthInferred = false;
                frameWidthText = string.Empty;
                selectedFrame = frameChoices.First(x => x.IsOwn);
                Raise(nameof(SelectedFrame));
            }

            double thickness = choice.Material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness) && !double.IsNaN(defaultThickness) && defaultThickness > 0 ? defaultThickness : DefaultFrameLayerThickness;
            DraftFrameLayer layer = draft.Frame.AddLayer(choice.Material, thickness, choice.SourceLabel, choice.SourceFileName, selectedFrameLayer == null ? (int?)null : selectedFrameLayer.Index + 1);
            FrameChanged(layer);
            return true;
        }

        /// <summary>Gives the selected frame layer the chosen material; its thickness stays.</summary>
        public bool ReplaceFrameMaterial(GlazingFrameMaterialChoice choice = null)
        {
            choice = choice ?? selectedFrameMaterial;
            if (isSaving || choice == null || selectedFrameLayer == null)
            {
                return false;
            }

            DraftFrameLayer layer = draft.Frame.ReplaceMaterial(selectedFrameLayer.Index, choice.Material, choice.SourceLabel, choice.SourceFileName);
            FrameChanged(layer);
            return layer != null;
        }

        /// <summary>Removes the selected frame layer. Removing the last one leaves no frame.</summary>
        public bool RemoveFrameLayer()
        {
            if (!CanRemoveFrameLayer)
            {
                return false;
            }

            int index = selectedFrameLayer.Index;
            draft.Frame.RemoveLayerAt(index);
            DraftFrameLayer next = draft.Frame.EditableLayers.Count == 0 ? null : draft.Frame.EditableLayers[Math.Min(index, draft.Frame.EditableLayers.Count - 1)];
            FrameChanged(next);
            return true;
        }

        /// <summary>Moves the selected frame layer one place up (-1) or down (+1) the list.</summary>
        public bool MoveFrameLayer(int delta)
        {
            if (isSaving || selectedFrameLayer == null || delta == 0)
            {
                return false;
            }

            int from = selectedFrameLayer.Index;
            int to = from + Math.Sign(delta);
            if (!draft.Frame.MoveLayer(from, to))
            {
                return false;
            }

            FrameChanged(draft.Frame.EditableLayers[to]);
            return true;
        }

        /// <summary>Selects the frame layer a finding concerns.</summary>
        public void SelectFrameIssue(GlazingBuilderIssueRow issue)
        {
            int? number = issue?.Issue.FrameLayerNumber;
            if (number != null && number.Value >= 1 && number.Value <= frameLayers.Count)
            {
                SelectedFrameLayer = frameLayers[number.Value - 1];
            }
        }

        /// <summary>The frame's face width [mm] as typed (written as the system's Default Frame Width); empty when not entered.</summary>
        public string FrameWidthText
        {
            get => frameWidthText;
            set
            {
                value = value ?? string.Empty;
                if (isSaving || draft.Frame.IsNone || frameWidthText == value)
                {
                    return;
                }

                frameWidthText = value;
                frameWidthInferred = false;
                bool valid = GlazingBuilderLayerRow.TryMillimetres(value, out double metres) && metres > 0;
                draft.Frame.Width = valid ? metres : double.NaN;
                draft.Frame.WidthInvalid = !valid && !string.IsNullOrWhiteSpace(value);
                RaiseFrame();
                Update(true);
            }
        }

        /// <summary>Says what the frame is and where the shown width comes from.</summary>
        public string FrameNote
        {
            get
            {
                DraftFrame frame = draft.Frame;
                if (frame.IsNone)
                {
                    return selectedFrame.IsOwn
                        ? "Own frame: add its layers from the materials below. Without a frame the system is glass only (Uw = Ug)."
                        : "No frame: the system is glass only (Uw = Ug).";
                }

                string width = frameWidthInferred
                    ? string.Format(CultureInfo.CurrentCulture, "The frame stores no width; its depth ({0} mm) is proposed, which is what SAM uses when a system has none. Change it if the face is different.", GlazingBuilderLayerRow.Millimetres(inferredWidth))
                    : "The width is the face width of the frame.";

                if (frame.IsAuthored)
                {
                    return "An own frame: its layers are the ones listed. " + width;
                }

                return (frame.IsEdited ? "The layers of the frame copied from " + (frame.CopiedFromName ?? "another system") + " were edited. " : "The frame layers are copied as they are. ") + width;
            }
        }

        /// <summary>The depth of the frame - the sum of its layers' thicknesses - read-only; empty without a frame.</summary>
        public string FrameDepthText
        {
            get
            {
                if (draft.Frame.IsNone)
                {
                    return string.Empty;
                }

                double depth = draft.Frame.Depth;
                return double.IsNaN(depth) ? "Depth: – (a layer has no thickness)" : string.Format(CultureInfo.CurrentCulture, "Depth (sum of the layers): {0} mm", GlazingBuilderLayerRow.Millimetres(depth));
            }
        }

        /// <summary>
        /// The frame's additional heat transfer, READ-ONLY: "20 %" when the copied frame carries one, "none" otherwise (an own frame never has one),
        /// "no frame" without a frame. It is not an input: there is no way to type it here, and it makes no layers.
        /// </summary>
        public string FrameAdditionalHeatTransferText
        {
            get
            {
                DraftFrame frame = draft.Frame;
                if (frame.IsNone)
                {
                    return "no frame";
                }

                return double.IsNaN(frame.AdditionalHeatTransfer) ? "none" : frame.AdditionalHeatTransfer.ToString("0.##", CultureInfo.CurrentCulture) + " %";
            }
        }

        /// <summary>Where the additional heat transfer comes from and that it cannot be edited here.</summary>
        public string FrameAdditionalHeatTransferNote
        {
            get
            {
                DraftFrame frame = draft.Frame;
                if (frame.IsNone)
                {
                    return string.Empty;
                }

                if (double.IsNaN(frame.AdditionalHeatTransfer))
                {
                    return frame.IsAuthored
                        ? "Read-only. An own frame has none: only its layers carry heat."
                        : "Read-only. The copied frame carries none.";
                }

                return string.Format(CultureInfo.CurrentCulture, "Read-only. Carried with the frame copied from {0}; Tas applies it on top of the frame layers{1}.", frame.CopiedFromName ?? "another system", frame.IsEdited ? ", also after the layers were edited" : string.Empty);
            }
        }

        private const double DefaultFrameLayerThickness = 0.03;

        // ---- Validation ------------------------------------------------------------------------------------------

        /// <summary>The check as shown: while editing, the one Save and replace is held to (the edited system's own name is free); otherwise the check of a new system.</summary>
        public GlazingDraftValidation Validation => IsEditing ? validationReplace : validation;

        public IReadOnlyList<GlazingBuilderIssueRow> Issues => issues;

        /// <summary>"✓ Ready to save" / "✕ 1 error" / "⚠ 2 warnings": the words carry it, not the colour.</summary>
        public string ValidationSummary
        {
            get
            {
                GlazingDraftValidation shown = Validation;
                int errors = shown.Errors.Count();
                int warnings = shown.Warnings.Count();
                if (errors != 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "✕ {0} {1}{2}: it cannot be saved yet.", errors, errors == 1 ? "error" : "errors", warnings == 0 ? string.Empty : string.Format(CultureInfo.CurrentCulture, ", {0} {1}", warnings, warnings == 1 ? "warning" : "warnings"));
                }

                return warnings == 0 ? "✓ Ready to save." : string.Format(CultureInfo.CurrentCulture, "⚠ {0} {1}: it can be saved.", warnings, warnings == 1 ? "warning" : "warnings");
            }
        }

        // ---- Performance -----------------------------------------------------------------------------------------

        public GlazingBuilderPerformanceState PerformanceState => performanceState;

        /// <summary>True while the values shown are not for the build-up as it is now (they are greyed).</summary>
        public bool PerformanceIsStale => performanceState == GlazingBuilderPerformanceState.Calculating;

        /// <summary>"Calculating…", "Not calculated: …" or empty (the values speak).</summary>
        public string PerformanceText
        {
            get
            {
                switch (performanceState)
                {
                    case GlazingBuilderPerformanceState.Calculating:
                        return "Calculating… (the values below are the previous ones)";

                    case GlazingBuilderPerformanceState.NotCalculated:
                        return performanceReason ?? "Not calculated.";

                    case GlazingBuilderPerformanceState.None:
                        return "Not calculated yet.";

                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>The centre-of-pane U-value from Tas. It is NOT the overall window U-value (Uw), which is calculated per aperture when a system is applied.</summary>
        public string UgText => Value(values?.Ug, "0.00", "W/m²K");

        public string GText => Value(values?.G, "0.00", null);

        public string LightTransmittanceText => Value(values?.LightTransmittance, "0.00", null);

        /// <summary>Frame U-value (1-D, Tas, of the frame layers); "no frame" for a glass-only system.</summary>
        public string UfText => draft.Frame.IsNone ? "no frame" : Value(values?.Uf, "0.00", "W/m²K");

        /// <summary>The labelled example Uw (reference window, no spacer Ψ); empty when it cannot be given.</summary>
        public string ReferenceUwText
        {
            get
            {
                if (values == null)
                {
                    return string.Empty;
                }

                if (draft.Frame.IsNone)
                {
                    return GlazingReferenceWindow.Label(values.Ug, double.NaN, true);
                }

                double uw = GlazingReferenceWindow.Uw(values.Ug, values.Uf, draft.Frame.Width);
                return double.IsNaN(uw) ? "Uw example: enter a frame width to see it (reference window 1.23 × 1.48 m, no spacer Ψ)." : GlazingReferenceWindow.Label(uw, draft.Frame.Width, false);
            }
        }

        public bool HasValues => values != null;

        public bool HasPerformanceText => !string.IsNullOrEmpty(PerformanceText);

        // ---- Save ------------------------------------------------------------------------------------------------

        public bool IsSaving => isSaving;

        /// <summary>
        /// Save (as a NEW system) is possible while the check has no errors (a name, a pane stack Tas can calculate, a gas for every gap, …). While editing,
        /// the name must differ from the edited system's (use Save and replace to keep it).
        /// </summary>
        public bool CanSave => !isSaving && savedSystem == null && library != null && !validation.HasErrors;

        /// <summary>The first button: "Save as predefined", or "Save as new" while editing a saved system.</summary>
        public string SaveButtonText => IsEditing ? "Save as new" : "Save as predefined";

        /// <summary>The second button, only while editing: "Save and replace X".</summary>
        public string SaveAndReplaceText => IsEditing ? string.Format(CultureInfo.CurrentCulture, "Save and replace {0}", editedName) : string.Empty;

        /// <summary>Save and replace: the check (held to the rule that the edited system's own name is free) has no errors and the edited system was in the library when this opened.</summary>
        public bool CanSaveAndReplace => IsEditing && !isSaving && savedSystem == null && library != null && !validationReplace.HasErrors;

        /// <summary>Why Save as new is not possible although Save and replace is (the name is the edited system's); empty otherwise.</summary>
        public string SaveAsNewHint => IsEditing && savedSystem == null && !CanSave && CanSaveAndReplace
            ? string.Format(CultureInfo.CurrentCulture, "To save as a new system, give it a name other than '{0}'. Save and replace keeps the name and moves {0} to the archive.", editedName)
            : string.Empty;

        /// <summary>Why a Save failed (nothing was written); null otherwise.</summary>
        public string SaveError => saveError;

        /// <summary>The system as saved; null until a Save succeeded.</summary>
        public ApertureConstruction SavedSystem => savedSystem;

        /// <summary>The materials Save added to the library / the ones it renamed because the library had different ones of that name.</summary>
        public UserGlazingSaveResult SaveResult { get; private set; }

        /// <summary>The newest evaluation task, for tests.</summary>
        internal Task LastEvaluationTask { get; private set; } = Task.CompletedTask;

        /// <summary>
        /// Saves the draft as a NEW predefined system in "My glazing systems" (<see cref="UserGlazingLibrary.Save"/>, off the UI thread: new Guid, embedded
        /// materials, provenance, atomic write) and raises <see cref="Saved"/>. Nothing else is written: not the model, not a candidate list - the
        /// library's own <see cref="UserGlazingLibrary.Changed"/> event refreshes those. On failure nothing is written and <see cref="SaveError"/> says why.
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            return await SaveCoreAsync(null).ConfigureAwait(true);
        }

        /// <summary>
        /// Saves the draft as a NEW system and moves the system being edited to the archive in one transaction (<see cref="UserGlazingLibrary.SaveReplacing"/>):
        /// the new system has a new Guid, its provenance says it supersedes the old one, and the old name may be reused. Only while <see cref="IsEditing"/>.
        /// A failure leaves the library as it was - the edited system still active, the new one not saved (after a failed library write the edited system
        /// may also be in the archive; a retry is idempotent); <see cref="SaveError"/> says why.
        /// </summary>
        public async Task<bool> SaveAndReplaceAsync()
        {
            return CanSaveAndReplace && await SaveCoreAsync(editedGuid).ConfigureAwait(true);
        }

        private async Task<bool> SaveCoreAsync(Guid? replacing)
        {
            if (replacing == null ? !CanSave : !CanSaveAndReplace)
            {
                return false;
            }

            isSaving = true;
            saveError = null;
            RaiseSave();

            GlazingValues performance = CurrentPerformance();
            UserGlazingSaveResult result;
            try
            {
                result = await Task.Run(() => replacing == null ? library.Save(draft, performance) : library.SaveReplacing(draft, replacing.Value, performance)).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                result = null;
                saveError = "The system could not be saved: " + exception.Message;
            }

            isSaving = false;
            if (result != null)
            {
                SaveResult = result;
                if (result.Succeeded)
                {
                    savedSystem = result.Saved;
                    RaiseSave();
                    Raise(nameof(StatusText));
                    Saved?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                saveError = result.Error ?? "The system could not be saved.";
                if (result.Validation != null)
                {
                    if (replacing == null)
                    {
                        validation = result.Validation;
                    }
                    else
                    {
                        validationReplace = result.Validation;
                    }

                    issues = Validation.Issues.Select(x => new GlazingBuilderIssueRow(x)).ToList();
                    Raise(nameof(Validation));
                    Raise(nameof(Issues));
                    Raise(nameof(ValidationSummary));
                }
            }

            RaiseSave();
            return false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Interlocked.Increment(ref version);
            Panes.Dispose();
            evaluator.Dispose();
        }

        // ---- Seeding ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The draft of an existing complete system (OUTSIDE → INSIDE): panes with their materials, gas layers as gaps (the gas from the material),
        /// the frame copied, the intended use and the additional heat transfer. A layer whose material the source lacks is kept as a missing pane,
        /// which the check reports.
        /// </summary>
        internal static GlazingSystemDraft SeedDraft(ApertureConstruction system, GlazingSource source)
        {
            Dictionary<string, IMaterial> materials = source.GetMaterials();
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(system);
            GlazingSystemDraft result = new GlazingSystemDraft()
            {
                BasedOnName = system.Name,
                BasedOnGuid = system.Guid,
                IntendedPanelType = Analytical.Query.PanelType((object)system),
            };

            // A pane taken from a file is known by that file's name; the model's and the libraries' panes have no file.
            string fileName = source.Kind == GlazingSourceKind.Loaded ? source.Label : null;

            if (system.TryGetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, out double additional))
            {
                result.PaneAdditionalHeatTransfer = additional;
            }

            foreach (ConstructionLayer constructionLayer in GlazingLayerOrder.FromSam(system.PaneConstructionLayers ?? new List<ConstructionLayer>()))
            {
                if (constructionLayer == null)
                {
                    continue;
                }

                if (constructionLayer.Name != null && materials.TryGetValue(constructionLayer.Name, out IMaterial material))
                {
                    if (material is GasMaterial gasMaterial)
                    {
                        result.Layers.Add(new DraftGap(Analytical.Query.DefaultGasType(gasMaterial), constructionLayer.Thickness));
                    }
                    else
                    {
                        // A pane of a system the Builder made keeps where it really came from (its provenance), not "My glazing systems".
                        GlazingBuilderPaneRecord record = provenance?.Panes?.FirstOrDefault(x => string.Equals(x.Material, constructionLayer.Name, StringComparison.Ordinal));
                        string paneLabel = string.IsNullOrWhiteSpace(record?.SourceLabel) ? source.Label : record.SourceLabel;
                        string paneFile = string.IsNullOrWhiteSpace(record?.SourceFile) ? fileName : record.SourceFile;

                        // A pane the Builder saved reversed ("<name> Reversed") reopens as the pane it was made from with Reverse on, so composing it again
                        // gives the same material and a user can still undo the reversal. Only when that exactly reproduces the saved material.
                        TransparentMaterial original = record != null && record.Reversed && material is TransparentMaterial reversed ? Query.Unreverse(reversed) : null;
                        result.Layers.Add(original != null
                            ? new DraftPane(original, constructionLayer.Thickness, paneLabel, paneFile) { Reversed = true }
                            : new DraftPane(material, constructionLayer.Thickness, paneLabel, paneFile));
                    }
                }
                else
                {
                    result.Layers.Add(DraftPane.Missing(constructionLayer.Name, constructionLayer.Thickness, source.Label));
                }
            }

            result.Frame = DraftFrame.CopyFrom(system, source.ConstructionManager?.MaterialLibrary, source.Label);

            // A system the Builder saved keeps where its frame really came from (its provenance), not the saved system itself: opening it and saving it
            // again records, and describes, the same frame origin - a copy (edited or not), or a frame built here - and each layer's own source.
            if (!result.Frame.IsNone && provenance != null)
            {
                if (string.Equals(provenance.Frame, "Authored", StringComparison.OrdinalIgnoreCase))
                {
                    result.Frame = result.Frame.AsAuthored();
                }
                else if ((string.Equals(provenance.Frame, "Copied", StringComparison.OrdinalIgnoreCase) || string.Equals(provenance.Frame, "CopiedEdited", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(provenance.FrameCopiedFromName))
                {
                    result.Frame = result.Frame.WithOrigin(provenance.FrameCopiedFromName, provenance.FrameCopiedFromGuid, string.Equals(provenance.Frame, "CopiedEdited", StringComparison.OrdinalIgnoreCase));
                }

                result.Frame = result.Frame.WithLayerSources(provenance.FrameLayers);
            }

            return result;
        }

        // ---- Internals -------------------------------------------------------------------------------------------

        private void BuildFrameChoices()
        {
            frameChoices.Add(GlazingFrameChoice.None());
            frameChoices.Add(GlazingFrameChoice.Own());
            HashSet<string> signatures = new HashSet<string>() { string.Empty };

            List<(ApertureConstruction, GlazingSource)> systems = new List<(ApertureConstruction, GlazingSource)>();
            if (options.Seed != null && options.SeedSource != null)
            {
                systems.Add((options.Seed, options.SeedSource));
            }

            foreach (GlazingSource source in options.Sources ?? new List<GlazingSource>())
            {
                foreach (ApertureConstruction system in source?.GetApertureConstructions(ApertureType.Window) ?? new List<ApertureConstruction>())
                {
                    systems.Add((system, source));
                }
            }

            GlazingFrameChoice seedChoice = null;
            foreach ((ApertureConstruction system, GlazingSource source) in systems)
            {
                if (frameChoices.Count > 60 || !system.HasFrameConstructionLayers())
                {
                    continue;
                }

                GlazingFrameChoice choice = GlazingFrameChoice.Of(system, source);
                if (signatures.Add(choice.Signature))
                {
                    frameChoices.Add(choice);
                    if (seedChoice == null && options.Seed != null && ReferenceEquals(system, options.Seed))
                    {
                        seedChoice = choice;
                    }
                }
            }

            selectedFrame = draft.Frame.IsNone ? frameChoices[0] : draft.Frame.IsAuthored ? frameChoices.First(x => x.IsOwn) : seedChoice ?? frameChoices[0];
            ownFrame = selectedFrame.IsOwn;
        }

        // The solid materials of the model, the libraries and the added sources (and of the frame the draft starts with), once each by definition.
        private void BuildFrameMaterials()
        {
            HashSet<string> definitions = new HashSet<string>();
            List<GlazingFrameMaterialChoice> result = new List<GlazingFrameMaterialChoice>();

            void Add(IMaterial material, string sourceLabel, string sourceFileName)
            {
                if (material == null || Core.Query.MaterialType(material) != MaterialType.Opaque || string.IsNullOrWhiteSpace(material.Name))
                {
                    return;
                }

                if (definitions.Add(MaterialIdentity.Json(material)))
                {
                    result.Add(new GlazingFrameMaterialChoice(material, sourceLabel, sourceFileName));
                }
            }

            // The frame the draft starts with first (its materials may be in no source), then the sources in their order.
            foreach (DraftFrameLayer layer in draft.Frame.EditableLayers)
            {
                Add(layer.Material, layer.SourceLabel, layer.SourceFileName);
            }

            foreach (GlazingSource source in options.Sources ?? new List<GlazingSource>())
            {
                string fileName = source?.Kind == GlazingSourceKind.Loaded ? source.Label : null;
                foreach (IMaterial material in source?.GetMaterials().Values ?? Enumerable.Empty<IMaterial>())
                {
                    Add(material, source.Label, fileName);
                }
            }

            allFrameMaterials.Clear();
            allFrameMaterials.AddRange(result.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase));
            FilterFrameMaterials();
        }

        private void FilterFrameMaterials()
        {
            string[] words = (frameMaterialSearch ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            frameMaterials = words.Length == 0
                ? allFrameMaterials.ToList()
                : allFrameMaterials.Where(x => words.All(w => (x.DisplayName ?? string.Empty).IndexOf(w, StringComparison.CurrentCultureIgnoreCase) >= 0 || (x.Name ?? string.Empty).IndexOf(w, StringComparison.CurrentCultureIgnoreCase) >= 0 || (x.SourceLabel ?? string.Empty).IndexOf(w, StringComparison.CurrentCultureIgnoreCase) >= 0)).ToList();

            if (selectedFrameMaterial != null && !frameMaterials.Contains(selectedFrameMaterial))
            {
                selectedFrameMaterial = null;
                Raise(nameof(SelectedFrameMaterial));
            }

            Raise(nameof(FrameMaterials));
            Raise(nameof(FrameMaterialCountText));
            RaiseFrameCommands();
        }

        // The frame's layers were changed (added, replaced, removed, moved or a thickness typed): the choice follows the frame, the width proposed from the
        // depth follows the layers, the list is rebuilt around the layer to keep selected, then the draft is checked and recalculated.
        private void FrameChanged(DraftFrameLayer select)
        {
            if (draft.Frame.IsNone)
            {
                frameWidthInferred = false;
                frameWidthText = string.Empty;
                GlazingFrameChoice choice = ownFrame ? frameChoices.First(x => x.IsOwn) : frameChoices[0];
                if (!ReferenceEquals(selectedFrame, choice))
                {
                    selectedFrame = choice;
                    Raise(nameof(SelectedFrame));
                }
            }
            else if (draft.Frame.IsAuthored)
            {
                ownFrame = true;
            }

            RefreshInferredWidth();
            RebuildFrameLayers(select);
            RaiseFrame();
            Update(true);
        }

        // A thickness typed into a frame layer row.
        private void FrameRow_Edited(GlazingBuilderFrameLayerRow row)
        {
            if (!changing && !isSaving)
            {
                RefreshInferredWidth();
                RaiseFrame();
                Update(true);
            }
        }

        // While the shown width is the proposed one (the depth, for a frame that stores none), it follows the layers.
        private void RefreshInferredWidth()
        {
            if (!frameWidthInferred || draft.Frame.IsNone)
            {
                return;
            }

            double depth = draft.Frame.Depth;
            if (double.IsNaN(depth) || depth <= 0)
            {
                return;
            }

            inferredWidth = depth;
            draft.Frame.Width = depth;
            frameWidthText = GlazingBuilderLayerRow.Millimetres(depth);
        }

        private void RebuildFrameLayers(DraftFrameLayer select)
        {
            changing = true;
            try
            {
                frameLayers.Clear();
                IReadOnlyList<DraftFrameLayer> list = draft.Frame.EditableLayers;
                for (int i = 0; i < list.Count; i++)
                {
                    frameLayers.Add(new GlazingBuilderFrameLayerRow(list[i], i, FrameRow_Edited));
                }

                selectedFrameLayer = select == null ? null : frameLayers.FirstOrDefault(x => ReferenceEquals(x.Layer, select));
            }
            finally
            {
                changing = false;
            }

            Raise(nameof(SelectedFrameLayer));
            RaiseFrameCommands();
        }

        // The copied frame has no stored width: its depth is proposed (what SAM falls back to), and said so.
        private bool InferFrameWidth(DraftFrame frame)
        {
            if (!double.IsNaN(frame.Width) && frame.Width > 0)
            {
                return false;
            }

            double depth = frame.Layers.Where(x => x != null && !double.IsNaN(x.Thickness)).Sum(x => x.Thickness);
            if (depth <= 0)
            {
                return false;
            }

            inferredWidth = depth;
            frame.Width = depth;
            return true;
        }

        private void ReadSavedNames()
        {
            savedNames.Clear();
            savedEntries.Clear();
            try
            {
                UserGlazingLibraryContent content = library?.Read();
                if (content != null && content.State == UserGlazingLibraryState.Ready)
                {
                    savedNames.AddRange(content.Systems.Where(x => x != null).Select(x => x.Name));
                    savedEntries.AddRange(content.Systems.Where(x => x != null).Select(x => new KeyValuePair<Guid, string>(x.Guid, x.Name)));
                }
            }
            catch (Exception)
            {
                // Save checks the names again under the library's lock.
            }
        }

        private string UniqueName(string wanted)
        {
            string name = wanted;
            for (int i = 2; savedNames.Any(x => string.Equals(x?.Trim(), name, StringComparison.OrdinalIgnoreCase)) && i < 1000; i++)
            {
                name = wanted.EndsWith(" (copy)", StringComparison.Ordinal) ? wanted.Substring(0, wanted.Length - 1) + " " + i.ToString(CultureInfo.InvariantCulture) + ")" : wanted + " " + i.ToString(CultureInfo.InvariantCulture);
            }

            return name;
        }

        // A structural edit: the list is rebuilt around the layer to keep (or choose) selected, then checked and recalculated.
        private void Changed(DraftLayer select)
        {
            Rebuild(select);
            Update(true);
        }

        private void Rebuild(DraftLayer select)
        {
            changing = true;
            try
            {
                layers.Clear();
                int panes = 0;
                int gaps = 0;
                for (int i = 0; i < draft.Layers.Count; i++)
                {
                    DraftLayer layer = draft.Layers[i];
                    layers.Add(new GlazingBuilderLayerRow(layer, i, layer is DraftPane ? ++panes : ++gaps, Row_Edited));
                }

                selectedLayer = select == null ? null : layers.FirstOrDefault(x => ReferenceEquals(x.Layer, select));
            }
            finally
            {
                changing = false;
            }

            Raise(nameof(SelectedLayer));
            RaiseCommands();
        }

        // A gas or a width typed into a row.
        private void Row_Edited(GlazingBuilderLayerRow row)
        {
            if (!changing && !isSaving)
            {
                Update(true);
            }
        }

        // Check now (cheap, on the draft as it is) and ask for the performance; the name alone does not change the performance.
        private void Update(bool recalculate)
        {
            if (disposed)
            {
                return;
            }

            composition = draft.ComposeGlazingSystem(composeOptions);
            validation = draft.CheckGlazingDraft(composition, savedNames);
            validationReplace = IsEditing ? draft.CheckGlazingDraft(composition, savedEntries.Where(x => x.Key != editedGuid.Value).Select(x => x.Value)) : validation;
            GlazingDraftValidation shown = Validation;
            issues = shown.Issues.Select(x => new GlazingBuilderIssueRow(x)).ToList();

            int gap = 0;
            foreach (GlazingBuilderLayerRow row in layers)
            {
                double htc = double.NaN;
                if (row.IsGap)
                {
                    gap++;
                    GlazingBuilderGapRecord record = composition?.Provenance?.Gaps?.FirstOrDefault(x => x.Position == gap);
                    htc = record?.HeatTransferCoefficient ?? double.NaN;
                }

                row.Update(htc, shown.Issues.Where(x => x.LayerIndex == row.Index));
            }

            foreach (GlazingBuilderFrameLayerRow row in frameLayers)
            {
                row.Update(shown.Issues.Where(x => x.FrameLayerNumber == row.Index + 1));
            }

            Raise(nameof(Validation));
            Raise(nameof(Issues));
            Raise(nameof(ValidationSummary));
            Raise(nameof(StatusText));
            RaiseSave();
            RaiseFrame();

            if (recalculate)
            {
                LastEvaluationTask = EvaluateAsync();
            }
            else
            {
                Raise(nameof(ReferenceUwText));
            }
        }

        private async Task EvaluateAsync()
        {
            long mine = Interlocked.Increment(ref version);
            string key = composition?.ContentKey;
            performanceState = GlazingBuilderPerformanceState.Calculating;
            RaisePerformance();

            DraftGlazingEvaluation evaluation;
            try
            {
                // The draft is composed and checked at this call (a snapshot), before the first await.
                evaluation = await evaluator.EvaluateAsync(draft).ConfigureAwait(true);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                evaluation = null;
                if (!disposed && mine == Interlocked.Read(ref version))
                {
                    performanceState = GlazingBuilderPerformanceState.NotCalculated;
                    performanceReason = "Not calculated: " + exception.Message;
                    values = null;
                    RaisePerformance();
                }

                return;
            }

            // An answer for an older build-up is never shown.
            if (disposed || mine != Interlocked.Read(ref version) || evaluation.State == DraftGlazingEvaluationState.Superseded)
            {
                return;
            }

            if (evaluation.State == DraftGlazingEvaluationState.Calculated)
            {
                values = evaluation.Values;
                valuesKey = evaluation.ContentKey;
                performanceState = GlazingBuilderPerformanceState.Calculated;
                performanceReason = null;
            }
            else
            {
                values = null;
                valuesKey = null;
                performanceState = GlazingBuilderPerformanceState.NotCalculated;
                performanceReason = evaluation.Reason;
            }

            RaisePerformance();
        }

        // The values Tas gave THIS build-up (not an older one), for the saved system's provenance.
        private GlazingValues CurrentPerformance()
        {
            string key = draft.ComposeGlazingSystem(composeOptions)?.ContentKey;
            if (key == null)
            {
                return null;
            }

            if (values != null && valuesKey == key && performanceState == GlazingBuilderPerformanceState.Calculated)
            {
                return values;
            }

            return evaluator.Cache.TryGet(key, out GlazingValues cached) ? cached : null;
        }

        private static string Value(double? value, string format, string unit)
        {
            if (value == null || double.IsNaN(value.Value))
            {
                return "–";
            }

            return value.Value.ToString(format, CultureInfo.CurrentCulture) + (unit == null ? string.Empty : " " + unit);
        }

        private void RaiseCommands()
        {
            Raise(nameof(CanAddPane));
            Raise(nameof(CanReplacePane));
            Raise(nameof(CanRemove));
            Raise(nameof(CanMoveUp));
            Raise(nameof(CanMoveDown));
            Raise(nameof(CanReverse));
        }

        private void RaiseSave()
        {
            Raise(nameof(IsSaving));
            Raise(nameof(CanSave));
            Raise(nameof(CanSaveAndReplace));
            Raise(nameof(SaveAsNewHint));
            Raise(nameof(SaveError));
            Raise(nameof(SavedSystem));
            RaiseCommands();
            RaiseFrameCommands();
        }

        private void RaiseFrame()
        {
            Raise(nameof(HasFrame));
            Raise(nameof(ShowFrameEditor));
            Raise(nameof(FrameWidthText));
            Raise(nameof(FrameNote));
            Raise(nameof(FrameDepthText));
            Raise(nameof(FrameAdditionalHeatTransferText));
            Raise(nameof(FrameAdditionalHeatTransferNote));
            Raise(nameof(UfText));
        }

        private void RaiseFrameCommands()
        {
            Raise(nameof(CanAddFrameLayer));
            Raise(nameof(CanReplaceFrameMaterial));
            Raise(nameof(CanRemoveFrameLayer));
            Raise(nameof(CanMoveFrameLayerUp));
            Raise(nameof(CanMoveFrameLayerDown));
        }

        private void RaisePerformance()
        {
            Raise(nameof(PerformanceState));
            Raise(nameof(PerformanceIsStale));
            Raise(nameof(PerformanceText));
            Raise(nameof(HasPerformanceText));
            Raise(nameof(UgText));
            Raise(nameof(GText));
            Raise(nameof(LightTransmittanceText));
            Raise(nameof(UfText));
            Raise(nameof(ReferenceUwText));
            Raise(nameof(HasValues));
        }

        private void Raise(string name)
        {
            if (context == null || SynchronizationContext.Current == context)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return;
            }

            context.Post(_ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)), null);
        }
    }
}
