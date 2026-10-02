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
                    alternatives = new ConstructionAlternatives(analyticalModel, uValue, session.Services.ConstructionEvaluator, session.Services.ConstructionCache, session.Services.ConstructionLibrary, session.Services.Sources);
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
