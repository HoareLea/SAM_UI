// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One choice of an override list (layer, heat-flow direction); a null <see cref="Value"/> means "automatic".</summary>
    public sealed class ThermalOption
    {
        public ThermalOption(object value, string display)
        {
            Value = value;
            Display = display;
        }

        public object Value { get; }

        public string Display { get; }

        public override string ToString() => Display;
    }

    /// <summary>
    /// The editing state of ONE row of the Thermal Performance panel: a construction and the elements of the row, with the
    /// existing row view-model behind it - <see cref="UValueViewModel"/> for an opaque construction (target U, live preview,
    /// scope, Keep name, layer / range / heat-flow overrides) or <see cref="GlazingViewModel"/> for a glazing system (the list
    /// of complete systems, target Uw, scope, warnings). It adds no calculation of its own and no WPF type: it only presents
    /// what the view-models say as bindable text and hands their request to the <see cref="ThermalEditSession"/>.
    /// <para>
    /// The view-model is created on the first edit (typing a target, choosing Change...), not when the row is shown, so a panel
    /// that is only looked at calls no Tas. Its scope is pinned from the elements of the row at that moment.
    /// </para>
    /// </summary>
    public sealed class ThermalRowEditor : INotifyPropertyChanged, IDisposable
    {
        private readonly ThermalEditSession session;
        private readonly AnalyticalModel analyticalModel;
        private readonly IReadOnlyList<Guid> selectedGuids;

        private UValueViewModel uValue;
        private ConstructionAlternatives alternatives;
        private GlazingViewModel glazing;
        private readonly HashSet<GlazingSource> glazingSources = new HashSet<GlazingSource>();
        private UserGlazingLibrary userGlazing;
        private SynchronizationContext glazingContext;
        private string minThicknessText = (UValueViewModel.DefaultMinThickness * 1000).ToString("0.#", CultureInfo.CurrentCulture);
        private string maxThicknessText = (UValueViewModel.DefaultMaxThickness * 1000).ToString("0.#", CultureInfo.CurrentCulture);
        private bool changeOpen;
        private string userConstructionMessage = string.Empty;
        private bool userConstructionMessageIsError;

        internal ThermalRowEditor(ThermalEditSession session, ThermalPerformanceRow row, AnalyticalModel analyticalModel, IReadOnlyList<Guid> selectedGuids)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.analyticalModel = analyticalModel;
            this.selectedGuids = selectedGuids ?? new List<Guid>();
            Row = row ?? throw new ArgumentNullException(nameof(row));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ThermalPerformanceRow Row { get; }

        public bool IsAperture => Row.IsAperture;

        public UValueViewModel UValue => uValue;

        public GlazingViewModel Glazing => glazing;

        /// <summary>The existing constructions next to the generated variant (opaque rows, once a target is typed); null before.</summary>
        public ConstructionAlternatives Alternatives => alternatives;

        /// <summary>True once the user started an edit of this row: its scope is then pinned and the panel does not rebuild under it.</summary>
        public bool IsEdited => uValue != null || glazing != null;

        /// <summary>True while a calculation for the current inputs is running.</summary>
        public bool IsBusy => (uValue?.IsBusy ?? false) || (glazing?.IsBusy ?? false) || (AlternativeChosen && alternatives.IsBusy);

        /// <summary>True when the row can contribute a change to Apply: a reached target / a chosen, usable system.</summary>
        public bool HasRequest => AlternativeChosen ? alternatives.ApplyEnabled : (uValue?.ApplyEnabled ?? false) || (glazing?.ApplyEnabled ?? false);

        /// <summary>True while an existing construction is chosen instead of the generated variant: it is the row's change.</summary>
        public bool AlternativeChosen => alternatives != null && alternatives.ExistingChosen;

        /// <summary>Editing is possible only where the model is known; a row of a model that was replaced is read-only.</summary>
        public bool CanEdit => analyticalModel != null;

        // ---- Provenance of the stored value ----------------------------------------------------------------------

        /// <summary>True when the model stores no single value for the row: "Recalculate" is offered.</summary>
        public bool NeedsRecalculation => Row.StoredState != ThermalStoredState.Stored;

        /// <summary>
        /// Where the shown value comes from: stored on the model by Tas, not calculated, varying between elements - and, once a
        /// calculation of the row exists, whether the stored value agrees with it.
        /// </summary>
        public string ProvenanceText
        {
            get
            {
                switch (Row.StoredState)
                {
                    case ThermalStoredState.NotCalculated:
                        return "Not calculated: the model stores no value here.";

                    case ThermalStoredState.Varies:
                        return "The elements store different values.";
                }

                double calculated = uValue?.CurrentThermalTransmittance ?? double.NaN;
                if (!IsAperture && !double.IsNaN(calculated) && !double.IsNaN(Row.StoredThermalTransmittance) && Math.Abs(calculated - Row.StoredThermalTransmittance) >= 0.0005)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Stored U {0:0.000}, calculated now {1:0.000}: the stored value is out of date.", Row.StoredThermalTransmittance, calculated);
                }

                return "Stored on the model.";
            }
        }

        /// <summary>True when the stored value disagrees with the calculation of the current construction (so "Recalculate" matters even though a value is stored).</summary>
        public bool StoredIsOutOfDate => !IsAperture && uValue != null && !double.IsNaN(uValue.CurrentThermalTransmittance) && !double.IsNaN(Row.StoredThermalTransmittance) && Math.Abs(uValue.CurrentThermalTransmittance - Row.StoredThermalTransmittance) >= 0.0005;

        // ---- Opaque: target, preview ------------------------------------------------------------------------------

        /// <summary>The target U-value as typed [W/m²K]. Typing starts the edit; clearing the box ends it.</summary>
        public string TargetText
        {
            get => uValue?.TargetText ?? string.Empty;
            set
            {
                if (IsAperture || !CanEdit)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    if (uValue != null)
                    {
                        Clear();
                    }

                    return;
                }

                EnsureUValue();
                uValue.TargetText = value;
            }
        }

        /// <summary>The glyph of the preview: ✓ reached, ✕ not reachable / failed, ○ calculating, – nothing yet.</summary>
        public string PreviewGlyph
        {
            get
            {
                if (AlternativeChosen)
                {
                    return alternatives.ApplyBlockReason == null ? "✓" : "✕";
                }

                if (uValue != null)
                {
                    switch (uValue.Status)
                    {
                        case UValuePreviewStatus.Reached:
                            return "✓";

                        case UValuePreviewStatus.Unreachable:
                        case UValuePreviewStatus.Failed:
                            return "✕";

                        case UValuePreviewStatus.Calculating:
                            return "○";
                    }

                    return "–";
                }

                if (glazing != null)
                {
                    switch (glazing.Status)
                    {
                        case GlazingPreviewStatus.Ready:
                            return glazing.ProposedRow == null ? "–" : glazing.ApplyBlockReason == null ? "✓" : "✕";

                        case GlazingPreviewStatus.Calculating:
                            return "○";

                        default:
                            return "✕";
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// The preview line: "U 0.260 → 0.180 · Mineral wool 80 → 124 mm" (opaque, reached); the reason otherwise; for glazing the
        /// comparison "Uw 1.35 → 1.05 (Ug ..., g ..., LT ...)".
        /// </summary>
        public string PreviewText
        {
            get
            {
                if (AlternativeChosen)
                {
                    return alternatives.PreviewText;
                }

                if (uValue != null)
                {
                    switch (uValue.Status)
                    {
                        case UValuePreviewStatus.Reached:
                            UValueLayerRow adjusted = uValue.PreviewRows.FirstOrDefault(x => x.Adjusted);
                            return string.Format(CultureInfo.CurrentCulture, "U {0} → {1} W/m²K · {2} {3:0.#} → {4:0.#} mm", Format(uValue.CurrentThermalTransmittance), Format(uValue.CalculatedThermalTransmittance), adjusted?.Name, adjusted?.ThicknessBefore, adjusted?.ThicknessAfter);

                        case UValuePreviewStatus.Calculating:
                            return "Calculating…";

                        default:
                            return uValue.StatusMessage ?? (uValue.IsBusy ? "Calculating the current U-value…" : "Type a target U-value.");
                    }
                }

                if (glazing != null)
                {
                    if (glazing.Status == GlazingPreviewStatus.Calculating)
                    {
                        return "Calculating the systems…";
                    }

                    if (glazing.Status != GlazingPreviewStatus.Ready)
                    {
                        return glazing.StatusMessage ?? "The systems could not be calculated.";
                    }

                    return glazing.ProposedRow == null ? "Choose a system." : glazing.ChangeText;
                }

                return string.Empty;
            }
        }

        /// <summary>What happens to the constructions, e.g. "Creates SIM_EXT_SLD U0.18; SIM_EXT_SLD stays unchanged." (opaque, reached).</summary>
        public string ResultText => AlternativeChosen ? alternatives.ResultText : uValue != null && uValue.Status == UValuePreviewStatus.Reached ? uValue.ResultText : glazing != null && glazing.ProposedRow != null ? glazing.ResultText : string.Empty;

        /// <summary>The reason a reached / chosen change still cannot be applied; null when nothing blocks it.</summary>
        public string BlockReason => AlternativeChosen ? alternatives.ApplyBlockReason : uValue != null ? (uValue.Status == UValuePreviewStatus.Reached ? uValue.ApplyBlockReason : null) : glazing?.ApplyBlockReason;

        public IReadOnlyList<string> Warnings => AlternativeChosen ? alternatives.Warnings.Concat(uValue.Warnings).ToList() : uValue?.Warnings ?? glazing?.Warnings ?? new List<string>();

        // ---- Scope (pinned) ---------------------------------------------------------------------------------------

        public ThermalScope Scope => uValue?.Scope ?? glazing?.Scope;

        /// <summary>The inline scope sentence, e.g. "Applies to 12 panels using SIM_EXT_SLD (3 selected)."</summary>
        public string ScopeText => uValue?.ScopeText ?? glazing?.ScopeText ?? string.Empty;

        public string ScopeAllLabel => Scope?.AllLabel ?? string.Empty;

        public string ScopeSelectedLabel => Scope?.SelectedLabel ?? string.Empty;

        /// <summary>True for "all N using it". Choosing it re-targets the preview (the heat-flow basis may differ).</summary>
        public bool ScopeAll
        {
            get => (uValue?.ApplyScope ?? glazing?.ApplyScope ?? ThermalApplyScope.AllUsing) != ThermalApplyScope.SelectedOnly;
            set
            {
                if (value)
                {
                    SetScope(ThermalApplyScope.AllUsing);
                }
            }
        }

        /// <summary>True for "only the M selected".</summary>
        public bool ScopeSelected
        {
            get => (uValue?.ApplyScope ?? glazing?.ApplyScope ?? ThermalApplyScope.AllUsing) == ThermalApplyScope.SelectedOnly;
            set
            {
                if (value)
                {
                    SetScope(ThermalApplyScope.SelectedOnly);
                }
            }
        }

        public bool ScopeSelectedEnabled => uValue != null ? uValue.ScopeUnavailableReason == null : glazing != null && glazing.Scope.SelectedAvailable;

        /// <summary>Why "only the selected" cannot be chosen; null while it can (and while it is not worth explaining).</summary>
        public string ScopeReason => uValue != null ? uValue.ScopeUnavailableReason : glazing?.Scope.SelectedUnavailableReason;

        // ---- Opaque: Keep name and the per-row overrides (behind the "more" expander) ---------------------------

        public bool KeepName
        {
            get => uValue?.KeepName ?? false;
            set
            {
                if (uValue != null)
                {
                    uValue.KeepName = value;
                }
            }
        }

        public bool KeepNameEnabled => uValue != null && uValue.KeepNameUnavailableReason == null;

        public string KeepNameReason => uValue?.KeepNameUnavailableReason;

        public IReadOnlyList<ThermalOption> LayerOptions
        {
            get
            {
                List<ThermalOption> options = new List<ThermalOption>();
                if (uValue == null)
                {
                    return options;
                }

                UValueLayerRow automatic = uValue.Layers.FirstOrDefault(x => x.Index == uValue.AutomaticLayerIndex);
                options.Add(new ThermalOption(null, automatic == null ? "Automatic (none adjustable)" : string.Format(CultureInfo.CurrentCulture, "Automatic ({0})", automatic.Name)));
                options.AddRange(uValue.Layers.Select(x => new ThermalOption(x.Index, string.Format(CultureInfo.CurrentCulture, "{0}. {1}  {2:0.#} mm", x.Index + 1, x.Name, x.ThicknessBefore))));
                return options;
            }
        }

        public ThermalOption SelectedLayer
        {
            get => LayerOptions.FirstOrDefault(x => Equals(x.Value, uValue?.LayerIndexOverride));
            set
            {
                if (uValue != null && value != null)
                {
                    uValue.LayerIndexOverride = value.Value as int?;
                }
            }
        }

        public IReadOnlyList<ThermalOption> HeatFlowOptions { get; } = new List<ThermalOption>()
        {
            new ThermalOption(null, "Automatic"),
            new ThermalOption(HeatFlowDirection.Horizontal, "Horizontal (walls)"),
            new ThermalOption(HeatFlowDirection.Up, "Up (roofs)"),
            new ThermalOption(HeatFlowDirection.Down, "Down (floors)"),
        };

        public ThermalOption SelectedHeatFlow
        {
            get => HeatFlowOptions.FirstOrDefault(x => Equals(x.Value, uValue?.HeatFlowDirectionOverride));
            set
            {
                if (uValue != null && value != null)
                {
                    uValue.HeatFlowDirectionOverride = value.Value as HeatFlowDirection?;
                }
            }
        }

        public string MinThicknessText
        {
            get => minThicknessText;
            set
            {
                minThicknessText = value;
                if (uValue != null && TryMillimetres(value, out double mm))
                {
                    uValue.MinThickness = mm / 1000;
                }
            }
        }

        public string MaxThicknessText
        {
            get => maxThicknessText;
            set
            {
                maxThicknessText = value;
                if (uValue != null && TryMillimetres(value, out double mm))
                {
                    uValue.MaxThickness = mm / 1000;
                }
            }
        }

        // ---- Glazing: Change..., the list of complete systems ---------------------------------------------------

        /// <summary>True while the list of systems is open under the row.</summary>
        public bool ChangeOpen
        {
            get => changeOpen;
            private set
            {
                changeOpen = value;
                Raise();
            }
        }

        /// <summary>Opens the list of complete systems under the row (the existing view-model builds and calculates it).</summary>
        public void OpenChange()
        {
            if (!IsAperture || !CanEdit || glazing != null)
            {
                return;
            }

            // "My glazing systems" is read now (a small file; missing = empty, unreadable = a note) and joins the pool after the default library;
            // it is read again whenever the library says it changed, until the list closes.
            glazingContext = SynchronizationContext.Current;
            userGlazing = UserGlazingOrNull();
            if (userGlazing != null)
            {
                userGlazing.Changed += UserGlazing_Changed;
            }

            glazing = new GlazingViewModel(analyticalModel, Row.ConstructionGuid, SelectedForScope(), session.Services.GlazingEvaluator, session.Services.GlazingLibrary(), GlazingSource.FromUserLibrary(userGlazing));
            glazing.PropertyChanged += ViewModel_PropertyChanged;
            ChangeOpen = true;
            session.EditorChanged(this);
            _ = glazing.InitializeAsync();

            // The sources the user added join the pool: those already read now, the others as they arrive (read when first needed).
            glazingSources.Clear();
            ThermalSourceCatalog catalog = session.Services.Sources;
            catalog.SourcesChanged += Catalog_SourcesChanged;
            AddReadySources();
            _ = catalog.EnsureLoadedAsync();
        }

        // Adds the sources that are ready and not in the glazing pool yet (a source is added once; the first of a Guid wins in the pool).
        private void AddReadySources()
        {
            if (glazing == null)
            {
                return;
            }

            foreach (GlazingSource source in session.Services.Sources.ReadySources)
            {
                if (glazingSources.Add(source))
                {
                    _ = glazing.AddSourceAsync(source);
                }
            }
        }

        private void Catalog_SourcesChanged(object sender, EventArgs e)
        {
            AddReadySources();
        }

        /// <summary>
        /// Reads "My glazing systems" again into the open list (what <see cref="UserGlazingLibrary.Changed"/> does): a new system becomes a
        /// candidate, one already listed stays one row. With <paramref name="select"/>, that system is chosen as soon as it is in the list and shown
        /// even if the target would hide it (<see cref="GlazingViewModel.SelectWhenAvailable"/>). Reads only; no model is touched. Nothing happens
        /// while no list is open.
        /// </summary>
        public Task RefreshUserGlazingAsync(Guid? select = null)
        {
            if (glazing == null)
            {
                return Task.CompletedTask;
            }

            if (select != null)
            {
                glazing.SelectWhenAvailable(select.Value);
            }

            return glazing.SetUserSourceAsync(GlazingSource.FromUserLibrary(userGlazing));
        }

        /// <summary>Chooses the system <paramref name="guid"/> in the open list, now or once a source brings it (<see cref="GlazingViewModel.SelectWhenAvailable"/>).</summary>
        public void SelectGlazing(Guid guid)
        {
            glazing?.SelectWhenAvailable(guid);
        }

        // A Save may come from another thread (the Builder saving off the UI thread): the list is refreshed on the thread it was opened on.
        private void UserGlazing_Changed(object sender, EventArgs e)
        {
            SynchronizationContext context = glazingContext;
            if (context == null || context == SynchronizationContext.Current)
            {
                _ = RefreshUserGlazingAsync();
                return;
            }

            context.Post(_ => RefreshUserGlazingAsync(), null);
        }

        // The services' user library; a host where it cannot be created simply has no "My glazing systems".
        private UserGlazingLibrary UserGlazingOrNull()
        {
            try
            {
                return session.Services.UserGlazing;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>True while the list is open and "My glazing systems" is available: <c>Create new…</c> is offered (a system built there is saved to it).</summary>
        public bool CanCreateNew => glazing != null && changeOpen && userGlazing != null;

        /// <summary>
        /// Prepares the Glazing System Builder for <c>Create new…</c>: seeded from the chosen candidate, else the current system, with snapshots of
        /// the model's / default / user systems and materials as panes and frames, the panel's source catalogue, "My glazing systems" to save to and
        /// its own Tas calculation. The Builder holds NO model: it cannot read or change the analytical model, and opening, editing, previewing,
        /// saving or cancelling it adds no Undo step. When it saves, the library's <see cref="UserGlazingLibrary.Changed"/> event refreshes this
        /// open list and the new system is chosen (<see cref="SelectGlazing"/>) - nothing is added to the list by hand. The caller shows the
        /// window and disposes the view-model; null when no list is open.
        /// </summary>
        public GlazingBuilderViewModel CreateBuilder()
        {
            return CreateBuilder((glazing?.ProposedRow ?? glazing?.CurrentRow)?.Candidate);
        }

        /// <summary>
        /// The same, seeded from <paramref name="seed"/> (a candidate of the open list, e.g. the one right-clicked) instead of the chosen / current
        /// system: seeding does not choose the candidate, so the row's pending change is untouched. Null seed: as <see cref="CreateBuilder()"/> with no seed.
        /// With <paramref name="edit"/> and a system of "My glazing systems" the Builder opens in EDIT mode (<see cref="GlazingBuilderOptions.EditSeed"/>):
        /// Save and replace is offered besides Save as new.
        /// </summary>
        public GlazingBuilderViewModel CreateBuilder(GlazingCandidate seed, bool edit = false)
        {
            if (!CanCreateNew)
            {
                return null;
            }

            GlazingBuilderViewModel result = new GlazingBuilderViewModel(new GlazingBuilderOptions()
            {
                Seed = seed?.ApertureConstruction,
                SeedSource = seed?.Source,
                EditSeed = edit && seed?.Kind == GlazingSourceKind.User,
                Sources = glazing.Sources.Where(x => x.Kind == GlazingSourceKind.Model || x.Kind == GlazingSourceKind.Library || x.Kind == GlazingSourceKind.User).ToList(),
                Catalog = session.Services.Sources,
                Library = userGlazing,
                Evaluator = session.Services.CreateBuilderEvaluator(),
                ComposeOptions = session.Services.BuilderComposeOptions,
            });

            result.Saved += (sender, e) =>
            {
                if (result.SavedSystem != null)
                {
                    SelectGlazing(result.SavedSystem.Guid);
                }
            };

            return result;
        }

        /// <summary>Notes about the sources of the open list, e.g. why "My glazing systems" could not be used; empty when there are none.</summary>
        public string GlazingNotesText => glazing == null ? string.Empty : string.Join(Environment.NewLine, glazing.Notes);

        public bool HasGlazingNotes => !string.IsNullOrEmpty(GlazingNotesText);

        /// <summary>Closes the list and drops the choice: the row is not edited any more.</summary>
        public void CloseChange()
        {
            Clear();
        }

        public IReadOnlyList<GlazingCandidateRow> Candidates => glazing?.Rows ?? new List<GlazingCandidateRow>();

        public GlazingCandidateRow SelectedCandidate
        {
            get => glazing?.ProposedRow;
            set
            {
                if (glazing != null && value != null)
                {
                    glazing.SelectedGuid = value.Guid;
                }
            }
        }

        /// <summary>The filter: the largest overall U-value (Uw) wanted.</summary>
        public string GlazingTargetText
        {
            get => glazing?.TargetText ?? string.Empty;
            set
            {
                if (glazing != null)
                {
                    glazing.TargetText = value;
                }
            }
        }

        public string CandidateCountText => glazing?.CandidateCountText ?? string.Empty;

        // ---- Glazing: the filters and the order of the list (behind "Filters" in the open list; the shared view-model does the work) ----

        /// <summary>The smallest g-value wanted (0-1); empty for none.</summary>
        public string GlazingMinGText
        {
            get => glazing?.MinGText ?? string.Empty;
            set
            {
                if (glazing != null)
                {
                    glazing.MinGText = value;
                }
            }
        }

        /// <summary>The largest g-value wanted (0-1); empty for none.</summary>
        public string GlazingMaxGText
        {
            get => glazing?.MaxGText ?? string.Empty;
            set
            {
                if (glazing != null)
                {
                    glazing.MaxGText = value;
                }
            }
        }

        /// <summary>The smallest light transmittance wanted (0-1); empty for none.</summary>
        public string GlazingMinLightText
        {
            get => glazing?.MinLightText ?? string.Empty;
            set
            {
                if (glazing != null)
                {
                    glazing.MinLightText = value;
                }
            }
        }

        /// <summary>List the default library's systems (on by default).</summary>
        public bool GlazingIncludeLibrary
        {
            get => glazing?.IncludeLibrary ?? true;
            set
            {
                if (glazing != null)
                {
                    glazing.IncludeLibrary = value;
                }
            }
        }

        /// <summary>List the systems of the sources added with "Add source…" (on by default).</summary>
        public bool GlazingIncludeAdded
        {
            get => glazing?.IncludeLoaded ?? true;
            set
            {
                if (glazing != null)
                {
                    glazing.IncludeLoaded = value;
                }
            }
        }

        /// <summary>The orders the list can be shown in.</summary>
        public IReadOnlyList<ThermalOption> GlazingSortOptions { get; } = new List<ThermalOption>()
        {
            new ThermalOption(GlazingSortOrder.OverallU, "Uw, best first"),
            new ThermalOption(GlazingSortOrder.GLowest, "g, lowest first"),
            new ThermalOption(GlazingSortOrder.GHighest, "g, highest first"),
            new ThermalOption(GlazingSortOrder.LightHighest, "Light, highest first"),
            new ThermalOption(GlazingSortOrder.Name, "Name"),
        };

        public ThermalOption GlazingSelectedSort
        {
            get => GlazingSortOptions.FirstOrDefault(x => Equals(x.Value, glazing?.SortOrder ?? GlazingSortOrder.OverallU));
            set
            {
                if (glazing != null && value?.Value is GlazingSortOrder order)
                {
                    glazing.SortOrder = order;
                }
            }
        }

        /// <summary>
        /// The filters in force besides the target, in one line, e.g. "Filtered: g 0.30 - 0.50 · light ≥ 0.70 · without the default library"; empty
        /// when none. Shown under the count whether the filters are open or not, so a short list is never a mystery.
        /// </summary>
        public string GlazingFiltersText
        {
            get
            {
                if (glazing == null)
                {
                    return string.Empty;
                }

                List<string> parts = new List<string>();
                string minG = Number(glazing.MinGText);
                string maxG = Number(glazing.MaxGText);
                if (minG != null && maxG != null)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "g {0} – {1}", minG, maxG));
                }
                else if (minG != null)
                {
                    parts.Add("g ≥ " + minG);
                }
                else if (maxG != null)
                {
                    parts.Add("g ≤ " + maxG);
                }

                string minLight = Number(glazing.MinLightText);
                if (minLight != null)
                {
                    parts.Add("light ≥ " + minLight);
                }

                if (!glazing.IncludeLibrary)
                {
                    parts.Add("without the default library");
                }

                if (!glazing.IncludeLoaded)
                {
                    parts.Add("without the added sources");
                }

                return parts.Count == 0 ? string.Empty : "Filtered: " + string.Join(" · ", parts);
            }
        }

        public bool HasGlazingFilters => !string.IsNullOrEmpty(GlazingFiltersText);

        /// <summary>The chosen system against the target Uw, e.g. "Target Uw ≤ 1.20: ✓ Meets target (margin +0.10)"; empty without a target or a choice.</summary>
        public string GlazingComparisonText
        {
            get
            {
                GlazingCandidateRow proposed = glazing?.ProposedRow;
                if (proposed == null || double.IsNaN(glazing.Target))
                {
                    return string.Empty;
                }

                string text = string.Format(CultureInfo.CurrentCulture, "Target Uw ≤ {0:0.00}: {1}", glazing.Target, glazing.ComparisonStatus);
                return double.IsNaN(proposed.Margin) ? text : text + string.Format(CultureInfo.CurrentCulture, " (margin {0})", proposed.MarginText);
            }
        }

        // A filter box counts only when it holds a number the view-model uses (the same rule: a non-negative number).
        private static string Number(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out double value) && !double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return null;
            }

            return value >= 0 && !double.IsInfinity(value) ? value.ToString("0.00", CultureInfo.CurrentCulture) : null;
        }

        // ---- Commands ----------------------------------------------------------------------------------------------

        /// <summary>Adds this row's change to <paramref name="changeSet"/> when it has one; returns whether it did.</summary>
        internal bool AddTo(ThermalChangeSet changeSet)
        {
            if (AlternativeChosen)
            {
                SetConstructionRequest request_Construction = alternatives.CreateRequest();
                if (request_Construction != null)
                {
                    changeSet.Add(request_Construction);
                    return true;
                }

                return false;
            }

            if (uValue != null)
            {
                SetUValueRequest request = uValue.CreateRequest();
                if (request != null)
                {
                    changeSet.Add(request);
                    return true;
                }
            }

            if (glazing != null)
            {
                SetGlazingRequest request = glazing.CreateRequest();
                if (request != null)
                {
                    changeSet.Add(request);
                    return true;
                }
            }

            return false;
        }

        /// <summary>The number of elements the row's change reaches under its scope; 0 without a change.</summary>
        internal int ElementCount
        {
            get
            {
                if (AlternativeChosen)
                {
                    return alternatives.ApplyEnabled ? uValue.Scope.BasisGuids(uValue.ApplyScope).Count : 0;
                }

                if (uValue != null && uValue.ApplyEnabled)
                {
                    return uValue.EffectiveScope == ThermalApplyScope.DontAssign ? 0 : uValue.Scope.BasisGuids(uValue.EffectiveScope).Count;
                }

                if (glazing != null && glazing.ApplyEnabled)
                {
                    return glazing.Scope.Scope == ThermalApplyScope.DontAssign ? 0 : glazing.Scope.BasisGuids(glazing.Scope.Scope).Count;
                }

                return 0;
            }
        }

        /// <summary>Ends the edit: the view-model is dropped, so the row is read-only again and its scope is no longer pinned.</summary>
        internal void Clear()
        {
            DisposeViewModels();
            changeOpen = false;
            Raise();
            session.EditorChanged(this);
        }

        public void Dispose()
        {
            DisposeViewModels();
        }

        // ---- Helpers ---------------------------------------------------------------------------------------------

        // The scope is pinned when the edit starts: the selected elements of the row, in either mode (in Whole envelope the
        // selection does not decide which rows are shown, but "Only the M selected" is still available for the selected ones).
        private IReadOnlyList<Guid> SelectedForScope()
        {
            return Row.SelectedGuids;
        }

        private void EnsureUValue()
        {
            if (uValue != null)
            {
                return;
            }

            uValue = new UValueViewModel(analyticalModel, Row.ConstructionGuid, SelectedForScope(), session.Services.UValueEvaluator);
            uValue.PropertyChanged += ViewModel_PropertyChanged;

            if (TryMillimetres(minThicknessText, out double min))
            {
                uValue.MinThickness = min / 1000;
            }

            if (TryMillimetres(maxThicknessText, out double max))
            {
                uValue.MaxThickness = max / 1000;
            }
        }

        private void SetScope(ThermalApplyScope scope)
        {
            if (uValue != null)
            {
                uValue.ApplyScope = scope;
            }
            else if (glazing != null)
            {
                glazing.ApplyScope = scope;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // An opaque row with a target: the existing constructions are compared against it (created on the first valid target, so a row
            // that is only looked at, or whose target is not a number, asks nothing). The view-model announces every derived property after
            // each change of state, so the list follows one notification of that burst (Status is in every one), not each of them.
            if (sender == uValue && uValue != null && e.PropertyName == nameof(UValueViewModel.Status))
            {
                if (alternatives == null && CanEdit && !double.IsNaN(uValue.TargetThermalTransmittance))
                {
                    alternatives = new ConstructionAlternatives(analyticalModel, uValue, session.Services.ConstructionEvaluator, session.Services.ConstructionCache, session.Services.ConstructionLibrary, session.Services.Sources, UserConstructionsOrNull());
                    alternatives.PropertyChanged += Alternatives_PropertyChanged;
                }

                alternatives?.Refresh();
            }

            Raise();
            session.EditorChanged(this);
        }

        private void Alternatives_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Raise();
            session.EditorChanged(this);
        }

        // ---- Opaque: the existing constructions next to the generated variant -----------------------------------------

        /// <summary>The list under the target: the generated variant, then the existing constructions that meet it or come close.</summary>
        public IReadOnlyList<ConstructionAlternativeRow> AlternativeRows => alternatives?.Rows ?? new List<ConstructionAlternativeRow>();

        /// <summary>True once a target is typed and the list has something to show beside the generated variant, or is working on it.</summary>
        public bool HasAlternatives => alternatives != null && alternatives.Status != ConstructionAlternativesStatus.Idle;

        public string AlternativesCountText => alternatives?.CountText ?? string.Empty;

        /// <summary>The chosen line; the generated variant (the default) until the user chooses an existing construction.</summary>
        public ConstructionAlternativeRow SelectedAlternative
        {
            get => alternatives?.SelectedRow;
            set
            {
                if (alternatives != null && value != null)
                {
                    alternatives.SelectedRow = value;
                }
            }
        }

        /// <summary>Notes about the sources of the alternatives list, e.g. why "My constructions" could not be used; empty when there are none.</summary>
        public string AlternativesNotesText => alternatives == null ? string.Empty : string.Join(Environment.NewLine, alternatives.Notes);

        public bool HasAlternativesNotes => !string.IsNullOrEmpty(AlternativesNotesText);

        // ---- Opaque: Save to My constructions ----------------------------------------------------------------------------

        // The services' user construction library; a host where it cannot be created simply has no "My constructions".
        private UserConstructionLibrary UserConstructionsOrNull()
        {
            try
            {
                return session.Services.UserConstructions;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// True for an opaque row of a known model while "My constructions" is available: <c>Save to My constructions…</c> is offered. Saving needs no
        /// edit and starts none - it neither pins the row's scope nor becomes a pending change of the model.
        /// </summary>
        public bool CanSaveToMyConstructions => !IsAperture && CanEdit && UserConstructionsOrNull() != null;

        /// <summary>Which construction the button saves, in words: the chosen alternative, the generated variant, or the current construction.</summary>
        public string SaveToMyConstructionsText
        {
            get
            {
                ConstructionAlternativeRow row = alternatives?.SelectedRow;
                if (row != null && !row.IsGenerated)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Saves {0} ({1}) to My constructions, as a new construction. The model is not changed.", row.Name, row.KindText);
                }

                if (row != null && row.IsGenerated && uValue != null && uValue.Status == UValuePreviewStatus.Reached)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Saves the generated variant {0} to My constructions, without applying it. The model is not changed.", row.Name);
                }

                return string.Format(CultureInfo.CurrentCulture, "Saves the current construction {0} to My constructions, as a new construction. The model is not changed.", Row.ConstructionName);
            }
        }

        /// <summary>The result of the last save to My constructions (what was saved, or why not); empty before one.</summary>
        public string UserConstructionMessage => userConstructionMessage;

        public bool HasUserConstructionMessage => !string.IsNullOrEmpty(userConstructionMessage);

        /// <summary>True when <see cref="UserConstructionMessage"/> says why nothing was saved (shown as an error, not as a confirmation).</summary>
        public bool UserConstructionMessageIsError => userConstructionMessageIsError;

        /// <summary>
        /// What <c>Save to My constructions…</c> would save, built from copies (the model is only read): <paramref name="row"/> when given (an alternative of
        /// the list, right-clicked or chosen), otherwise the chosen alternative, else the generated variant once its target is reached, else the row's
        /// CURRENT construction. Opening it starts no edit, calls no Tas and changes nothing; <see cref="UserConstructionSaveSubject.Error"/> says why
        /// there is nothing to save.
        /// </summary>
        public UserConstructionSaveSubject CreateSaveSubject(ConstructionAlternativeRow row = null)
        {
            if (IsAperture || !CanEdit)
            {
                return new UserConstructionSaveSubject("Only an opaque construction can be saved to My constructions.");
            }

            ConstructionAlternativeRow target = row ?? alternatives?.SelectedRow;
            if (target != null && !target.IsGenerated)
            {
                return ExistingSubject(target);
            }

            if (target != null && target.IsGenerated && uValue != null && uValue.Status == UValuePreviewStatus.Reached)
            {
                return GeneratedSubject();
            }

            if (row != null)
            {
                return new UserConstructionSaveSubject("The generated variant has no U-value yet: type a target U-value the construction can reach.");
            }

            return CurrentSubject();
        }

        /// <summary>
        /// Saves <paramref name="subject"/> to "My constructions" under <paramref name="name"/> (a new Guid, the materials embedded, the provenance
        /// attached) and says what happened (<see cref="UserConstructionMessage"/>). The analytical model and its history are NOT touched: the row's
        /// pending change, if any, stays exactly as it was; the library's <c>Changed</c> event refreshes the open alternatives lists.
        /// </summary>
        public UserConstructionSaveResult SaveToMyConstructions(UserConstructionSaveSubject subject, string name)
        {
            UserConstructionSaveResult result = subject == null
                ? new UserConstructionSaveResult(null, null, null, "There is nothing to save.")
                : subject.Save(UserConstructionsOrNull(), name);

            userConstructionMessageIsError = !result.Succeeded;
            userConstructionMessage = result.Succeeded
                ? string.Format(CultureInfo.CurrentCulture, "Saved '{0}' to My constructions.", result.Saved.Name)
                : result.Error;

            Raise();
            return result;
        }

        /// <summary>The library's naming rule for a typed name (null when it can be used): not empty, not the name of another construction of My constructions.</summary>
        public string MyConstructionsNameProblem(string name)
        {
            UserConstructionLibrary library = UserConstructionsOrNull();
            if (library == null)
            {
                return "There is no My constructions library to save to.";
            }

            return UserConstructionLibrary.NameProblem(name, ExistingMyConstructionNames());
        }

        // The prompt's starting name: the construction's own name, made unique among the saved ones.
        private string UniqueMyConstructionName(string name)
        {
            return UserConstructionSaveSubject.UniqueName(name, ExistingMyConstructionNames());
        }

        private List<string> ExistingMyConstructionNames()
        {
            try
            {
                return UserConstructionsOrNull()?.Read().Constructions.Select(x => x.Name).ToList() ?? new List<string>();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        // An existing construction of the list (the model's, the default library's, My constructions', an added source's): saved as it is there.
        private UserConstructionSaveSubject ExistingSubject(ConstructionAlternativeRow row)
        {
            ConstructionCandidate candidate = row.Candidate;
            if (candidate == null)
            {
                return new UserConstructionSaveSubject("That alternative cannot be saved.");
            }

            UserConstructionOrigin origin;
            switch (candidate.Kind)
            {
                case GlazingSourceKind.Model:
                    origin = UserConstructionOrigin.Model;
                    break;

                case GlazingSourceKind.Library:
                    origin = UserConstructionOrigin.DefaultLibrary;
                    break;

                case GlazingSourceKind.User:
                    origin = UserConstructionOrigin.MyConstructions;
                    break;

                default:
                    origin = UserConstructionOrigin.AddedSource;
                    break;
            }

            UserConstructionProvenance provenance = new UserConstructionProvenance()
            {
                SavedFrom = origin,
                SavedFromSource = origin == UserConstructionOrigin.AddedSource ? candidate.Source.Label : null,
                BasedOnName = candidate.Name,
                BasedOnGuid = candidate.Guid,
                OriginModelName = analyticalModel?.Name,
                ThermalTransmittance = row.ThermalTransmittance,
                Route = "U-value of the construction as it is (Tas thermal transmittance)",
                Engine = UserConstructionProvenance.TasEngine,
            };

            ApplyBasis(provenance);

            return new UserConstructionSaveSubject(
                candidate.Construction,
                candidate.Source.ConstructionManager?.MaterialLibrary,
                string.Format(CultureInfo.CurrentCulture, "the construction {0} ({1}, {2})", candidate.Name, row.KindText, row.UText),
                UniqueMyConstructionName(candidate.Name),
                provenance);
        }

        // The generated thickness variant, made by the same pure query Apply uses; it exists only as this copy until the user applies it.
        private UserConstructionSaveSubject GeneratedSubject()
        {
            UValueEvaluation evaluation = uValue.Evaluation;
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (evaluation == null || !evaluation.Reached || adjacencyCluster == null)
            {
                return new UserConstructionSaveSubject("The generated variant has no U-value yet: type a target U-value the construction can reach.");
            }

            MaterialLibrary materials = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            ProposedConstructionResult proposed = Query.ProposedConstruction(uValue.SourceConstruction, materials, evaluation.LayerIndex, evaluation.Thickness, UValueApplyMode.NewConstruction, uValue.NewConstructionName, evaluation.CalculatedThermalTransmittance, adjacencyCluster.GetConstructions());
            if (!proposed.Succeeded)
            {
                return new UserConstructionSaveSubject(proposed.Error);
            }

            if (proposed.MaterialAdded)
            {
                materials.Add(proposed.Material);
            }

            UserConstructionProvenance provenance = new UserConstructionProvenance()
            {
                SavedFrom = UserConstructionOrigin.GeneratedVariant,
                BasedOnName = uValue.SourceConstruction.Name,
                BasedOnGuid = uValue.SourceConstruction.Guid,
                OriginModelName = analyticalModel.Name,
                ThermalTransmittance = evaluation.CalculatedThermalTransmittance,
                TargetThermalTransmittance = uValue.TargetThermalTransmittance,
                Route = string.Format(CultureInfo.CurrentCulture, "Thickness of layer {0} ({1}) solved for the target U-value (Tas layer thickness calculation)", evaluation.LayerIndex + 1, proposed.SourceMaterialName),
                Engine = UserConstructionProvenance.TasEngine,
            };

            ApplyBasis(provenance);

            return new UserConstructionSaveSubject(
                proposed.Construction,
                materials,
                string.Format(CultureInfo.CurrentCulture, "the generated variant {0} (U {1} W/m²K, made from {2})", proposed.Construction.Name, Format(evaluation.CalculatedThermalTransmittance), uValue.SourceConstruction.Name),
                UniqueMyConstructionName(proposed.Construction.Name),
                provenance);
        }

        // The row's construction as it is in the model now (a copy): no edit is needed, so its U-value is the calculated one when the row is being
        // edited, else the one the panels store.
        private UserConstructionSaveSubject CurrentSubject()
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            Construction construction = adjacencyCluster?.GetConstructions()?.Find(x => x != null && x.Guid == Row.ConstructionGuid);
            if (construction == null)
            {
                return new UserConstructionSaveSubject("The construction is no longer in the model.");
            }

            double u = uValue != null && !double.IsNaN(uValue.CurrentThermalTransmittance) ? uValue.CurrentThermalTransmittance : Row.StoredThermalTransmittance;

            UserConstructionProvenance provenance = new UserConstructionProvenance()
            {
                SavedFrom = UserConstructionOrigin.Model,
                BasedOnName = construction.Name,
                BasedOnGuid = construction.Guid,
                OriginModelName = analyticalModel.Name,
                ThermalTransmittance = u,
            };

            if (!double.IsNaN(u))
            {
                bool calculated = uValue != null && !double.IsNaN(uValue.CurrentThermalTransmittance);
                provenance.Route = calculated ? "U-value of the construction as it is (Tas thermal transmittance)" : "U-value stored on the model's panels (Tas)";
                provenance.Engine = UserConstructionProvenance.TasEngine;
            }

            ApplyBasis(provenance, construction);

            return new UserConstructionSaveSubject(
                construction,
                analyticalModel.MaterialLibrary,
                string.Format(CultureInfo.CurrentCulture, "the current construction {0}{1}", construction.Name, double.IsNaN(u) ? string.Empty : string.Format(CultureInfo.CurrentCulture, " (U {0} W/m²K)", Format(u))),
                UniqueMyConstructionName(construction.Name),
                provenance);
        }

        // The heat-flow basis of the U-value: the row's own while it is being edited, else the one the panels of the row give.
        private void ApplyBasis(UserConstructionProvenance provenance, Construction construction = null)
        {
            if (uValue != null)
            {
                provenance.HeatFlowDirection = uValue.HeatFlowDirection.ToString();
                provenance.HeatFlowBasis = UserConstructionSaveSubject.BasisText(uValue.HeatFlowBasis, uValue.HeatFlowDirection, uValue.External, uValue.HeatFlowDirectionOverride != null);
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            construction = construction ?? adjacencyCluster?.GetConstructions()?.Find(x => x != null && x.Guid == Row.ConstructionGuid);
            if (construction == null || adjacencyCluster == null)
            {
                return;
            }

            HashSet<Guid> elements = new HashSet<Guid>(Row.ElementGuids);
            List<Panel> panels = (adjacencyCluster.GetPanels(construction) ?? new List<Panel>()).Where(x => elements.Count == 0 || elements.Contains(x.Guid)).ToList();
            UValueHeatFlowBasis basis = Query.UValueHeatFlowBasis(panels, construction);
            provenance.HeatFlowDirection = basis.HeatFlowDirection.ToString();
            provenance.HeatFlowBasis = UserConstructionSaveSubject.BasisText(basis, basis.HeatFlowDirection, basis.External, false);
        }

        private void DisposeViewModels()
        {
            if (alternatives != null)
            {
                alternatives.PropertyChanged -= Alternatives_PropertyChanged;
                alternatives.Dispose();
                alternatives = null;
            }

            if (uValue != null)
            {
                uValue.PropertyChanged -= ViewModel_PropertyChanged;
                uValue.Dispose();
                uValue = null;
            }

            if (userGlazing != null)
            {
                userGlazing.Changed -= UserGlazing_Changed;
                userGlazing = null;
            }

            glazingContext = null;

            if (glazing != null)
            {
                session.Services.Sources.SourcesChanged -= Catalog_SourcesChanged;
                glazingSources.Clear();
                glazing.PropertyChanged -= ViewModel_PropertyChanged;
                glazing.Dispose();
                glazing = null;
            }
        }

        // Everything is derived from the view-models, so one notification for all properties is the honest one.
        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private static string Format(double value)
        {
            return double.IsNaN(value) ? "–" : value.ToString("0.000", CultureInfo.CurrentCulture);
        }

        private static bool TryMillimetres(string text, out double value)
        {
            return (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) && value > 0;
        }
    }
}
