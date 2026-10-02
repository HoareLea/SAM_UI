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
    /// <summary>
    /// The state of the one-window "Set glazing" flow, free of WPF types so it is unit-testable. The window binds to it;
    /// <c>Modify.SetGlazing</c> applies <see cref="CreateRequest"/>. Glazing is SELECTION, not calculation: the
    /// candidates are complete, real glazing systems (aperture constructions) from the model, the default library and files
    /// loaded for this window; their Ug / Uf / g / light transmittance are calculated once per pool by Tas, and the
    /// filters, the sort and the overall Uw work on those values. No pane property is ever created or edited.
    /// <para>
    /// <b>Identity.</b> The affected apertures are those whose aperture construction Guid matches; candidates are told
    /// apart by Guid (several can share a name), the name is only shown.
    /// </para>
    /// <para>
    /// <b>Window-local pool.</b> Nothing loaded here is written to the model until Apply, and then only the chosen system
    /// and the materials it lacks. Disposing the view-model without applying leaves the model untouched.
    /// </para>
    /// </summary>
    public sealed class GlazingViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly string[] derivedProperties = new[]
        {
            nameof(Rows), nameof(Status), nameof(StatusMessage), nameof(IsBusy), nameof(CandidateCountText), nameof(CurrentRow), nameof(ProposedRow),
            nameof(Target), nameof(Margin), nameof(ComparisonStatus), nameof(ChangeText), nameof(ScopeText), nameof(ResultText), nameof(Warnings),
            nameof(Notes), nameof(ApplyBlockReason), nameof(ApplyEnabled), nameof(SelectedGuid), nameof(LastEvaluationMilliseconds),
        };

        private readonly IGlazingEvaluator evaluator;
        private readonly AnalyticalModel analyticalModel;
        private readonly ApertureConstruction current;
        private readonly List<Aperture> apertures_Using;
        private readonly ThermalScope scope;
        private readonly Dictionary<Guid, PanelType> hostPanelTypes = new Dictionary<Guid, PanelType>();
        private readonly List<string> apertureConstructionNames;
        private readonly int sameNameCount;
        private readonly IReadOnlyDictionary<string, IMaterial> modelMaterials;

        private readonly List<GlazingSource> sources = new List<GlazingSource>();
        private readonly List<GlazingCandidate> candidates = new List<GlazingCandidate>();
        private readonly Dictionary<Guid, GlazingValues> values = new Dictionary<Guid, GlazingValues>();
        private readonly HashSet<Guid> evaluated = new HashSet<Guid>();
        private readonly Dictionary<Guid, KeyValuePair<double, GlazingUwBasis>> uwCache = new Dictionary<Guid, KeyValuePair<double, GlazingUwBasis>>();

        private string targetText = string.Empty;
        private string minGText = string.Empty;
        private string maxGText = string.Empty;
        private string minLightText = string.Empty;
        private bool includeLibrary = true;
        private bool includeLoaded = true;
        private GlazingSortOrder sortOrder = GlazingSortOrder.OverallU;
        private Guid? selectedGuid;
        private bool selectedByUser;
        private Guid? requestedGuid;
        private Guid? pinnedGuid;

        private GlazingPreviewStatus status = GlazingPreviewStatus.Calculating;
        private string statusMessage;
        private int pending;
        private long lastEvaluationMilliseconds = -1;
        private int version;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private IReadOnlyList<GlazingCandidateRow> rows;

        /// <param name="analyticalModel">The model (read only; the view-model keeps its own copies).</param>
        /// <param name="apertureConstructionGuid">The aperture construction whose apertures change.</param>
        /// <param name="selectedApertureGuids">The selected apertures; only those using the construction count.</param>
        /// <param name="evaluator">The glazing calculation (real or fake).</param>
        /// <param name="library">The default library as a source; null for none.</param>
        /// <param name="userSource">"My glazing systems" as a source (<see cref="GlazingSource.FromUserLibrary"/>); null for none.</param>
        public GlazingViewModel(AnalyticalModel analyticalModel, Guid apertureConstructionGuid, IEnumerable<Guid> selectedApertureGuids, IGlazingEvaluator evaluator, GlazingSource library, GlazingSource userSource = null)
        {
            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            this.analyticalModel = analyticalModel ?? throw new ArgumentNullException(nameof(analyticalModel));

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster ?? throw new ArgumentException("The model has no adjacency cluster.", nameof(analyticalModel));
            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions() ?? new List<ApertureConstruction>();

            current = apertureConstructions.Find(x => x != null && x.Guid == apertureConstructionGuid) ?? throw new ArgumentException("The aperture construction is not in the model.", nameof(apertureConstructionGuid));
            apertures_Using = adjacencyCluster.GetApertures(current) ?? new List<Aperture>();

            HashSet<Guid> using_Guids = new HashSet<Guid>(apertures_Using.Select(x => x.Guid));
            List<Guid> selected_All = (selectedApertureGuids ?? Enumerable.Empty<Guid>()).Distinct().ToList();
            scope = new ThermalScope(current.Name, "aperture", "apertures", apertures_Using.Select(x => x.Guid), selected_All);

            // The panel type of the panel carrying each affected aperture, read once: the pre-Apply panel-group warning
            // compares it with the candidate's Default Panel Type (the rule Edit > ModelCheck runs after the change).
            foreach (Panel panel in adjacencyCluster.GetPanels() ?? new List<Panel>())
            {
                foreach (Aperture aperture in panel?.Apertures ?? new List<Aperture>())
                {
                    if (aperture != null && using_Guids.Contains(aperture.Guid))
                    {
                        hostPanelTypes[aperture.Guid] = panel.PanelType;
                    }
                }
            }

            List<Aperture> apertures_Other = (adjacencyCluster.GetApertures() ?? new List<Aperture>()).Where(x => selected_All.Contains(x.Guid) && !using_Guids.Contains(x.Guid)).ToList();
            OtherSelectedCount = apertures_Other.Count;
            MixedApertureTypeCount = apertures_Other.Count(x => x.ApertureType != current.ApertureType);

            apertureConstructionNames = apertureConstructions.Where(x => x != null).Select(x => x.Name).ToList();
            sameNameCount = apertureConstructions.Where(x => x != null && x.Guid != current.Guid && x.Name == current.Name).Select(x => x.Guid).Distinct().Count();

            GlazingSource source_Model = GlazingSource.FromModel(analyticalModel);
            modelMaterials = source_Model.GetMaterials();

            sources.Add(source_Model);
            if (library != null)
            {
                sources.Add(library);
            }

            if (userSource != null)
            {
                Place(userSource);
            }

            Rebuild();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ---- Source ---------------------------------------------------------------------------------------

        public Guid ApertureConstructionGuid => current.Guid;

        public string ApertureConstructionName => current.Name;

        public ApertureType ApertureType => current.ApertureType;

        /// <summary>How many apertures use the current construction (by Guid).</summary>
        public int AperturesUsingCount => apertures_Using.Count;

        /// <summary>How many of the selected apertures use the current construction.</summary>
        public int SelectedAperturesCount => scope.SelectedCount;

        public IReadOnlyList<Guid> SelectedApertureGuids => scope.SelectedGuids;

        /// <summary>The shared scope: the pinned apertures, the choice and the labels (the window's "Changes" radios read it).</summary>
        public ThermalScope Scope => scope;

        /// <summary>The sources in the pool (model, default library, "My glazing systems", then the loaded ones), for the "N systems from ..." caption.</summary>
        public IReadOnlyList<GlazingSource> Sources => sources;

        // ---- Filters (Target) -----------------------------------------------------------------------------

        /// <summary>Target: the largest overall U-value (Uw) wanted [W/m²K]. Empty for no target.</summary>
        public string TargetText
        {
            get => targetText;
            set => Set(ref targetText, value);
        }

        /// <summary>The parsed target Uw; NaN when empty or not a positive number.</summary>
        public double Target => Parse(targetText);

        /// <summary>The smallest g-value wanted (0-1); empty for none.</summary>
        public string MinGText
        {
            get => minGText;
            set => Set(ref minGText, value);
        }

        public string MaxGText
        {
            get => maxGText;
            set => Set(ref maxGText, value);
        }

        /// <summary>The smallest light transmittance wanted (0-1); empty for none.</summary>
        public string MinLightText
        {
            get => minLightText;
            set => Set(ref minLightText, value);
        }

        // ---- Advanced -------------------------------------------------------------------------------------

        /// <summary>Advanced: include the default library's systems.</summary>
        public bool IncludeLibrary
        {
            get => includeLibrary;
            set => Set(ref includeLibrary, value);
        }

        /// <summary>Advanced: include the systems loaded with "Load more glazing...".</summary>
        public bool IncludeLoaded
        {
            get => includeLoaded;
            set => Set(ref includeLoaded, value);
        }

        /// <summary>The order of <see cref="Rows"/>; best overall U-value first by default. It changes nothing but the order.</summary>
        public GlazingSortOrder SortOrder
        {
            get => sortOrder;
            set
            {
                if (sortOrder == value)
                {
                    return;
                }

                sortOrder = value;
                OnPropertyChanged(nameof(SortOrder));
                Refresh();
            }
        }

        /// <summary>Which apertures get the system.</summary>
        public ThermalApplyScope ApplyScope
        {
            get => scope.Scope;
            set
            {
                if (scope.Scope == value)
                {
                    return;
                }

                scope.Scope = value;
                uwCache.Clear();
                OnPropertyChanged(nameof(ApplyScope));
                Refresh();
            }
        }

        // ---- Table ----------------------------------------------------------------------------------------

        /// <summary>The comparison table: systems passing the filters (the current one always), best overall U-value first.</summary>
        public IReadOnlyList<GlazingCandidateRow> Rows => rows;

        /// <summary>The row of the current system (the reference), or null.</summary>
        public GlazingCandidateRow CurrentRow => rows.FirstOrDefault(x => x.IsCurrent);

        /// <summary>The chosen system's row, or null.</summary>
        public GlazingCandidateRow ProposedRow => selectedGuid == null ? null : rows.FirstOrDefault(x => x.Guid == selectedGuid.Value);

        /// <summary>"Showing 14 of 171 systems." - how many pass the filters out of those in the pool.</summary>
        public string CandidateCountText
        {
            get
            {
                int total = PoolCandidates().Count;
                int shown = rows.Count(x => x.Passes);
                return string.Format(CultureInfo.CurrentCulture, "Showing {0} of {1} {2}.", shown, total, total == 1 ? "system" : "systems");
            }
        }

        /// <summary>The chosen system's Guid; set it from the table. A choice made by the user is kept until it is filtered out.</summary>
        public Guid? SelectedGuid
        {
            get => selectedGuid;
            set
            {
                if (selectedGuid == value)
                {
                    return;
                }

                // Another choice ends a pending or pinned "choose this system" (SelectWhenAvailable): the filters apply to every row again.
                requestedGuid = null;
                bool unpinned = pinnedGuid != null && pinnedGuid != value;
                if (unpinned)
                {
                    pinnedGuid = null;
                }

                selectedGuid = value;
                selectedByUser = value != null;
                if (unpinned)
                {
                    Refresh();
                    return;
                }

                OnDerivedPropertiesChanged();
            }
        }

        /// <summary>
        /// The system <see cref="SelectWhenAvailable"/> keeps in the table although the filters would hide it; null when none. It stays only
        /// while it is the choice.
        /// </summary>
        public Guid? PinnedGuid => pinnedGuid;

        /// <summary>
        /// Chooses the system <paramref name="guid"/>: now if it is in the pool, otherwise as soon as a source brings it (e.g. a system just saved
        /// to "My glazing systems", which arrives with <see cref="SetUserSourceAsync"/>). The chosen system is shown even when the target or the
        /// g / light filters would hide it, but only while it stays the choice: choosing another system (or none) ends that, and the filters apply
        /// to it again. Nothing else in the list changes; nothing is calculated or written.
        /// </summary>
        public void SelectWhenAvailable(Guid guid)
        {
            requestedGuid = guid;
            Refresh();
        }

        // ---- Comparison: Current | Proposed | Target | Margin | Status -------------------------------------

        /// <summary>Target minus the chosen system's Uw: positive is better than the target; NaN without both.</summary>
        public double Margin => ProposedRow?.Margin ?? double.NaN;

        /// <summary>"✓ Meets target", "✕ Above target by 0.12", or "–".</summary>
        public string ComparisonStatus
        {
            get
            {
                GlazingCandidateRow proposed = ProposedRow;
                if (proposed == null)
                {
                    return "–";
                }

                if (double.IsNaN(proposed.Margin))
                {
                    return double.IsNaN(Target) ? "–" : "Uw not available";
                }

                return proposed.Margin >= 0
                    ? "✓ Meets target"
                    : string.Format(CultureInfo.CurrentCulture, "✕ Above target by {0}", (-proposed.Margin).ToString("0.00", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>The physical change, e.g. "Ug 1.10 → 0.60 · g 0.62 → 0.45 · light 0.79 → 0.71 · pane and frame change."</summary>
        public string ChangeText
        {
            get
            {
                GlazingCandidateRow before = CurrentRow;
                GlazingCandidateRow after = ProposedRow;
                if (after == null || before == null)
                {
                    return null;
                }

                List<string> parts = new List<string>
                {
                    string.Format(CultureInfo.CurrentCulture, "Ug {0} → {1}", before.UgText, after.UgText),
                };

                if (before.Transparent || after.Transparent)
                {
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "g {0} → {1}", before.GText, after.GText));
                    parts.Add(string.Format(CultureInfo.CurrentCulture, "light {0} → {1}", before.LightTransmittanceText, after.LightTransmittanceText));
                }

                parts.Add(string.Format(CultureInfo.CurrentCulture, "Uw {0} → {1}", before.UwText, after.UwText));

                string pane = before.PaneBuildUp == after.PaneBuildUp ? "same pane build-up" : "pane build-up changes";
                string frame = before.FrameText == after.FrameText ? "same frame" : after.HasFrame ? "frame changes" : "no frame";
                return string.Join(" · ", parts) + string.Format(CultureInfo.CurrentCulture, " — {0}, {1}.", pane, frame);
            }
        }

        // ---- Apply scope ----------------------------------------------------------------------------------

        /// <summary>The inline apply scope, e.g. "Applies to 20 apertures using SIM_EXT_GLZ (3 selected)."</summary>
        public string ScopeText => scope.Text(scope.Scope, "Adds the chosen system to the model without assigning it to any aperture.");

        /// <summary>What happens to the model, e.g. "Adds SIM_EXT_GLZ 2 and 2 materials to the model; SIM_EXT_GLZ stays unchanged."</summary>
        public string ResultText
        {
            get
            {
                GlazingCandidateRow proposed = ProposedRow;
                if (proposed == null)
                {
                    return "Choose a glazing system from the table.";
                }

                GlazingCandidate candidate = proposed.Candidate;
                bool inModel = candidate.Kind == GlazingSourceKind.Model;
                string name = inModel ? candidate.Name : ModelName(candidate);
                int materials = candidate.MaterialsToAdd.Count;

                if (inModel)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Uses {0}, already in the model; {1} stays unchanged.", name, current.Name);
                }

                string materialsText = materials == 0 ? string.Empty : string.Format(CultureInfo.CurrentCulture, " and {0} {1}", materials, materials == 1 ? "material" : "materials");
                return string.Format(CultureInfo.CurrentCulture, "Adds {0}{1} to the model; {2} stays unchanged.", name, materialsText, current.Name);
            }
        }

        /// <summary>Warning lines: mixed aperture types, other aperture constructions sharing the name, blocked or frameless choices.</summary>
        public IReadOnlyList<string> Warnings
        {
            get
            {
                List<string> result = new List<string>();

                if (sameNameCount > 0)
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "{0} other aperture {1} also named {2}; {3} not changed.", sameNameCount, sameNameCount == 1 ? "construction is" : "constructions are", current.Name, sameNameCount == 1 ? "it is" : "they are"));
                }

                int otherSelected = OtherSelectedCount;
                if (otherSelected > 0)
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "{0} other selected {1} not use {2} and {3} not affected.", otherSelected, otherSelected == 1 ? "aperture does" : "apertures do", current.Name, otherSelected == 1 ? "is" : "are"));
                }

                if (MixedApertureTypeCount > 0)
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "{0} of them {1} of another type than {2}: only {3} systems are offered.", MixedApertureTypeCount, MixedApertureTypeCount == 1 ? "is" : "are", current.Name, current.ApertureType));
                }

                // The chosen system's own warnings (frameless, made for another panel group), the same markers its row
                // carries in the table; one that blocks Apply is shown as the block reason instead.
                GlazingCandidateRow proposed = ProposedRow;
                if (proposed != null)
                {
                    result.AddRange(proposed.Warnings.Where(x => !x.Blocks).Select(x => x.Text));
                }

                if (proposed != null && proposed.UwBasis == GlazingUwBasis.Approximate)
                {
                    result.Add("The apertures have no geometry to weigh by: Uw is approximate (80 % pane, 20 % frame).");
                }

                return result;
            }
        }

        /// <summary>Notes about sources (e.g. "This file contains 11,664 panes and no glazing systems.").</summary>
        public IReadOnlyList<string> Notes => sources.Where(x => !string.IsNullOrWhiteSpace(x.Note)).Select(x => x.Note).ToList();

        /// <summary>Selected apertures that use another construction (not affected).</summary>
        public int OtherSelectedCount { get; }

        /// <summary>Of those, the apertures of another aperture type than the current construction's (a different kind of system).</summary>
        public int MixedApertureTypeCount { get; }

        // ---- Status ---------------------------------------------------------------------------------------

        public GlazingPreviewStatus Status => status;

        /// <summary>The reason when the values could not be calculated.</summary>
        public string StatusMessage => statusMessage;

        /// <summary>True while a calculation is running.</summary>
        public bool IsBusy => pending > 0;

        /// <summary>How long the last calculation took on the worker [ms]; -1 before the first.</summary>
        public long LastEvaluationMilliseconds => lastEvaluationMilliseconds;

        /// <summary>Why a chosen system still cannot be applied; null when nothing blocks it.</summary>
        public string ApplyBlockReason
        {
            get
            {
                GlazingCandidateRow proposed = ProposedRow;
                if (proposed == null)
                {
                    return null;
                }

                if (proposed.IsCurrent)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} is the system the apertures use now; choose another.", current.Name);
                }

                if (proposed.Candidate.MaterialIssue != null)
                {
                    return proposed.Candidate.MaterialIssue;
                }

                if (proposed.Values == null)
                {
                    return "Tas could not calculate this system.";
                }

                if (scope.Scope == ThermalApplyScope.SelectedOnly && !scope.SelectedAvailable)
                {
                    return scope.SelectedUnavailableReason;
                }

                if (scope.Scope == ThermalApplyScope.AllUsing && AperturesUsingCount == 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "No aperture uses {0}.", current.Name);
                }

                return null;
            }
        }

        /// <summary>True only with a calculated, usable, different system chosen and nothing blocking it.</summary>
        public bool ApplyEnabled => status == GlazingPreviewStatus.Ready && !IsBusy && ProposedRow != null && ApplyBlockReason == null;

        /// <summary>The running calculation (tests await it).</summary>
        internal Task LastEvaluationTask { get; private set; } = Task.CompletedTask;

        // ---- Commands -------------------------------------------------------------------------------------

        /// <summary>Calculates the values of every system in the pool, once.</summary>
        public Task InitializeAsync()
        {
            LastEvaluationTask = EvaluateAsync();
            return LastEvaluationTask;
        }

        /// <summary>Adds the systems of a source loaded for this window to the pool and calculates just those.</summary>
        public Task AddSourceAsync(GlazingSource source)
        {
            if (source == null)
            {
                return Task.CompletedTask;
            }

            Place(source);
            Rebuild();
            OnDerivedPropertiesChanged();

            LastEvaluationTask = EvaluateAsync();
            return LastEvaluationTask;
        }

        /// <summary>
        /// Puts "My glazing systems" in the pool, or replaces the copy read earlier with <paramref name="source"/> (the library read again after it
        /// changed): after the default library, before the loaded sources. A system already listed from the library stays one row and keeps its
        /// values (saved systems are immutable and identified by Guid); a new one is added and calculated; one no longer in the library leaves the
        /// list. The model is not touched.
        /// </summary>
        public Task SetUserSourceAsync(GlazingSource source)
        {
            if (source == null)
            {
                return Task.CompletedTask;
            }

            GlazingSource previous = sources.Find(x => x.Kind == GlazingSourceKind.User);
            if (previous != null)
            {
                sources[sources.IndexOf(previous)] = source;
            }
            else
            {
                Place(source);
            }

            Rebuild(previous, source);
            OnDerivedPropertiesChanged();

            LastEvaluationTask = EvaluateAsync();
            return LastEvaluationTask;
        }

        /// <summary>The change to apply, or null while <see cref="ApplyEnabled"/> is false.</summary>
        public SetGlazingRequest CreateRequest()
        {
            if (!ApplyEnabled)
            {
                return null;
            }

            GlazingCandidateRow proposed = ProposedRow;
            GlazingCandidateRow before = CurrentRow;

            return new SetGlazingRequest()
            {
                SourceApertureConstructionGuid = current.Guid,
                ApertureConstruction = proposed.Candidate.ApertureConstruction,
                MaterialsToAdd = proposed.Candidate.MaterialsToAdd.ToList(),
                Scope = scope.Scope,
                SelectedApertureGuids = scope.SelectedGuids.ToList(),
                Values = proposed.Values,
                OldValues = before?.Values,
                OldUw = before?.Uw ?? double.NaN,
                NewUw = proposed.Uw,
                UwBasis = proposed.UwBasis,
                TargetUw = Target,
                Source = proposed.Candidate.Source,
            };
        }

        /// <summary>The name the chosen system gets in the model (a clash with another aperture construction's name gets a suffix).</summary>
        public string ModelName(GlazingCandidate candidate)
        {
            if (candidate.Kind == GlazingSourceKind.Model)
            {
                return candidate.Name;
            }

            string name = candidate.Name;
            string name_Unique = name;
            for (int index = 2; apertureConstructionNames.Any(x => string.Equals(x?.Trim(), name_Unique?.Trim(), StringComparison.OrdinalIgnoreCase)); index++)
            {
                name_Unique = string.Format(CultureInfo.CurrentCulture, "{0} {1}", name, index);
            }

            return name_Unique;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        // ---- Evaluation -----------------------------------------------------------------------------------

        private async Task EvaluateAsync()
        {
            int version_Current = ++version;

            List<GlazingEvaluationBatch> batches = new List<GlazingEvaluationBatch>();
            foreach (GlazingSource source in sources)
            {
                List<Guid> guids = candidates.Where(x => x.Source == source && !evaluated.Contains(x.Guid)).Select(x => x.Guid).ToList();
                if (guids.Count != 0)
                {
                    batches.Add(new GlazingEvaluationBatch(source, guids));
                }
            }

            if (batches.Count == 0)
            {
                Finish(GlazingPreviewStatus.Ready, null);
                return;
            }

            pending++;
            status = GlazingPreviewStatus.Calculating;
            statusMessage = null;
            OnDerivedPropertiesChanged();

            GlazingEvaluation result;
            try
            {
                result = await evaluator.EvaluateAsync(new GlazingEvaluationRequest(batches), cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                pending--;
                return;
            }
            catch (Exception exception)
            {
                pending--;
                Finish(GlazingPreviewStatus.Failed, exception.Message);
                return;
            }

            pending--;
            lastEvaluationMilliseconds = result?.ElapsedMilliseconds ?? -1;

            if (result == null)
            {
                Finish(GlazingPreviewStatus.Failed, "The glazing values could not be calculated.");
                return;
            }

            foreach (GlazingEvaluationBatch batch in batches)
            {
                foreach (Guid guid in batch.Guids)
                {
                    evaluated.Add(guid);
                    if (result.Values.TryGetValue(guid, out GlazingValues glazingValues))
                    {
                        values[guid] = glazingValues;
                    }
                }
            }

            if (result.Error != null && result.Values.Count == 0)
            {
                Finish(GlazingPreviewStatus.Failed, result.Error);
                return;
            }

            Finish(version_Current == version || pending == 0 ? GlazingPreviewStatus.Ready : GlazingPreviewStatus.Calculating, null);
        }

        private void Finish(GlazingPreviewStatus status, string message)
        {
            this.status = pending > 0 ? GlazingPreviewStatus.Calculating : status;
            statusMessage = message;
            Refresh();
        }

        // ---- Table ----------------------------------------------------------------------------------------

        // A source joins the pool at its rank (model, default library, "My glazing systems", loaded), after the sources of the same rank.
        private void Place(GlazingSource source)
        {
            int rank = GlazingSource.Rank(source.Kind);
            sources.Insert(sources.FindLastIndex(x => GlazingSource.Rank(x.Kind) <= rank) + 1, source);
        }

        // Rebuilds the pool's candidates from the sources: every Guid belongs to the first source (in pool order) that offers it. A candidate
        // already built for its source is kept with its values; one now offered by an earlier source is built afresh and calculated again.
        // replaced -> replacement: one source read again (the same immutable systems): its candidates are rebuilt on the new copy, values kept.
        private void Rebuild(GlazingSource replaced = null, GlazingSource replacement = null)
        {
            // Glass is replaced by glass and a solid door by a solid door: a system of the other kind is not a candidate
            // (one whose material cannot be resolved stays, shown as unusable, so the reason is visible).
            bool transparent_Current = current.Transparent(sources[0].ConstructionManager?.MaterialLibrary);

            Dictionary<Guid, GlazingCandidate> existing = candidates.ToDictionary(x => x.Guid);
            List<GlazingCandidate> rebuilt = new List<GlazingCandidate>();
            HashSet<Guid> guids = new HashSet<Guid>();
            HashSet<Guid> kept = new HashSet<Guid>();
            foreach (GlazingSource source in sources)
            {
                foreach (ApertureConstruction apertureConstruction in source.GetApertureConstructions(current.ApertureType))
                {
                    if (guids.Contains(apertureConstruction.Guid))
                    {
                        continue;
                    }

                    existing.TryGetValue(apertureConstruction.Guid, out GlazingCandidate candidate_Existing);
                    if (candidate_Existing != null && candidate_Existing.Source == source)
                    {
                        guids.Add(apertureConstruction.Guid);
                        kept.Add(apertureConstruction.Guid);
                        rebuilt.Add(candidate_Existing);
                        continue;
                    }

                    GlazingCandidate candidate = new GlazingCandidate(apertureConstruction, source, modelMaterials);
                    if (candidate.MaterialIssue == null && apertureConstruction.Transparent(source.ConstructionManager?.MaterialLibrary) != transparent_Current)
                    {
                        continue;
                    }

                    guids.Add(apertureConstruction.Guid);
                    rebuilt.Add(candidate);
                    if (candidate_Existing != null && replaced != null && candidate_Existing.Source == replaced && source == replacement)
                    {
                        kept.Add(apertureConstruction.Guid);
                    }
                }
            }

            // The current system must be in the pool even if the model-source listing lost it.
            if (!guids.Contains(current.Guid))
            {
                bool reuse = existing.TryGetValue(current.Guid, out GlazingCandidate candidate_Current) && candidate_Current.Source == sources[0];
                rebuilt.Insert(0, reuse ? candidate_Current : new GlazingCandidate(current, sources[0], modelMaterials));
                if (reuse)
                {
                    kept.Add(current.Guid);
                }
            }

            // A system that left the pool or now comes from another source is calculated again if it is listed.
            foreach (Guid guid in existing.Keys.Where(x => !kept.Contains(x)))
            {
                evaluated.Remove(guid);
                values.Remove(guid);
                uwCache.Remove(guid);
            }

            candidates.Clear();
            candidates.AddRange(rebuilt);

            Refresh();
        }

        private List<GlazingCandidate> PoolCandidates()
        {
            return candidates.Where(x => x.Guid == current.Guid || Included(x)).ToList();
        }

        private bool Included(GlazingCandidate candidate)
        {
            switch (candidate.Kind)
            {
                case GlazingSourceKind.Library:
                    return includeLibrary;

                case GlazingSourceKind.Loaded:
                    return includeLoaded;

                default:
                    return true;
            }
        }

        private void Refresh()
        {
            double target = Target;
            double minG = Parse(minGText);
            double maxG = Parse(maxGText);
            double minLight = Parse(minLightText);

            List<Aperture> apertures_Basis = BasisApertures();
            List<GlazingCandidateRow> rows_New = new List<GlazingCandidateRow>();
            foreach (GlazingCandidate candidate in PoolCandidates())
            {
                values.TryGetValue(candidate.Guid, out GlazingValues glazingValues);
                bool transparent = candidate.ApertureConstruction.Transparent(candidate.Source.ConstructionManager?.MaterialLibrary);
                bool isCurrent = candidate.Guid == current.Guid;

                double uw = double.NaN;
                GlazingUwBasis basis = GlazingUwBasis.None;
                if (glazingValues != null)
                {
                    if (!uwCache.TryGetValue(candidate.Guid, out KeyValuePair<double, GlazingUwBasis> cached))
                    {
                        uw = Query.GlazingOverallThermalTransmittance(apertures_Basis, candidate.ApertureConstruction, glazingValues.Ug, glazingValues.Uf, out basis);
                        uwCache[candidate.Guid] = new KeyValuePair<double, GlazingUwBasis>(uw, basis);
                    }
                    else
                    {
                        uw = cached.Key;
                        basis = cached.Value;
                    }
                }

                bool passes = glazingValues != null;
                if (passes && !double.IsNaN(target) && !double.IsNaN(uw))
                {
                    passes = uw <= target + 1e-9;
                }

                // A system without glass (a door) has no g or light transmittance: the g / light filters then say "no".
                if (passes)
                {
                    if (!double.IsNaN(minG) && !(glazingValues.G >= minG - 1e-9))
                    {
                        passes = false;
                    }

                    if (!double.IsNaN(maxG) && !(glazingValues.G <= maxG + 1e-9))
                    {
                        passes = false;
                    }

                    if (!double.IsNaN(minLight) && !(glazingValues.LightTransmittance >= minLight - 1e-9))
                    {
                        passes = false;
                    }
                }

                // Rows still being calculated are shown (values "–") so the table is never empty while Tas works.
                if (glazingValues == null && !evaluated.Contains(candidate.Guid))
                {
                    passes = true;
                }

                if (passes || isCurrent || candidate.Guid == pinnedGuid || candidate.Guid == requestedGuid)
                {
                    rows_New.Add(new GlazingCandidateRow(candidate, glazingValues, transparent, uw, basis, isCurrent, passes, target, RowWarnings(candidate, isCurrent, apertures_Basis)));
                }
            }

            GlazingSortOrder order = sortOrder;
            rows_New.Sort((x, y) => Compare(x, y, order));
            rows = rows_New;

            // A system asked for by Guid (SelectWhenAvailable) is chosen as soon as it is in the pool, and pinned: shown while it is the choice.
            if (requestedGuid != null && rows.Any(x => x.Guid == requestedGuid.Value))
            {
                selectedGuid = requestedGuid;
                selectedByUser = true;
                pinnedGuid = requestedGuid;
                requestedGuid = null;
            }

            // Keep the user's choice while it is shown; otherwise choose automatically, but only against a target.
            if (selectedGuid != null && !rows.Any(x => x.Guid == selectedGuid.Value && (x.Passes || x.Guid == pinnedGuid)))
            {
                selectedGuid = null;
                selectedByUser = false;
            }

            if (pinnedGuid != null && pinnedGuid != selectedGuid)
            {
                pinnedGuid = null;
            }

            // Choose automatically only against a target the current system does not meet already (then there is
            // nothing to fix and no choice is made for the user). The choice is the best Uw that meets it, whatever order is shown.
            GlazingCandidateRow row_Current = rows.FirstOrDefault(x => x.IsCurrent);
            bool currentMeets = row_Current != null && !double.IsNaN(row_Current.Margin) && row_Current.Margin >= 0;
            if (!selectedByUser)
            {
                List<GlazingCandidateRow> meeting = rows.Where(x => !x.IsCurrent && x.Passes && x.CanApply && x.Values != null && !double.IsNaN(x.Uw) && x.Margin >= 0).ToList();
                meeting.Sort((x, y) => Compare(x, y, GlazingSortOrder.OverallU));
                selectedGuid = double.IsNaN(target) || currentMeets ? null : meeting.Select(x => (Guid?)x.Guid).FirstOrDefault();
            }

            OnDerivedPropertiesChanged();
        }

        // In the chosen order (a value not calculated last), then best overall U-value first; the current system among its equals first.
        private static int Compare(GlazingCandidateRow x, GlazingCandidateRow y, GlazingSortOrder order)
        {
            int result;
            switch (order)
            {
                case GlazingSortOrder.GLowest:
                    result = Key(x.G).CompareTo(Key(y.G));
                    break;

                case GlazingSortOrder.GHighest:
                    result = KeyDescending(x.G).CompareTo(KeyDescending(y.G));
                    break;

                case GlazingSortOrder.LightHighest:
                    result = KeyDescending(x.LightTransmittance).CompareTo(KeyDescending(y.LightTransmittance));
                    break;

                case GlazingSortOrder.Name:
                    result = string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
                    break;

                default:
                    result = 0;
                    break;
            }

            if (result != 0)
            {
                return result;
            }

            result = Key(x.Uw).CompareTo(Key(y.Uw));
            if (result != 0)
            {
                return result;
            }

            result = Key(x.Ug).CompareTo(Key(y.Ug));
            if (result != 0)
            {
                return result;
            }

            result = y.IsCurrent.CompareTo(x.IsCurrent);
            return result != 0 ? result : string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        private static double Key(double value)
        {
            return double.IsNaN(value) ? double.MaxValue : value;
        }

        private static double KeyDescending(double value)
        {
            return double.IsNaN(value) ? double.MaxValue : -value;
        }

        // The apertures Uw is weighed over: the selected ones when the scope is "selected only", else every one using the construction.
        private List<Aperture> BasisApertures()
        {
            HashSet<Guid> basis = new HashSet<Guid>(scope.BasisGuids(scope.Scope));
            return apertures_Using.Where(x => basis.Contains(x.Guid)).ToList();
        }

        // What a candidate is marked for before it is chosen: a material that cannot be applied, no frame where the current
        // system has one, and a Default Panel Type of another panel group than the panels carrying the apertures in scope.
        // The current system is the reference and carries none.
        private List<GlazingRowWarning> RowWarnings(GlazingCandidate candidate, bool isCurrent, List<Aperture> apertures_Basis)
        {
            List<GlazingRowWarning> result = new List<GlazingRowWarning>();
            if (isCurrent)
            {
                return result;
            }

            if (candidate.MaterialIssue != null)
            {
                result.Add(new GlazingRowWarning(GlazingWarningKind.Material, candidate.MaterialDiffers ? "material differs from model" : "material missing", candidate.MaterialIssue, true));
            }

            if (!candidate.HasFrame && current.HasFrameConstructionLayers())
            {
                result.Add(new GlazingRowWarning(GlazingWarningKind.Frameless, "no frame", "The chosen system has no frame layers: the apertures lose their frame, so Uw equals Ug.", false));
            }

            PanelType panelType_Candidate = candidate.ApertureConstruction.PanelType();
            PanelGroup panelGroup_Candidate = panelType_Candidate.PanelGroup();
            if (panelGroup_Candidate != PanelGroup.Undefined)
            {
                List<PanelGroup> groups_Other = new List<PanelGroup>();
                int mismatched = 0;
                foreach (Aperture aperture in apertures_Basis)
                {
                    if (!hostPanelTypes.TryGetValue(aperture.Guid, out PanelType panelType_Host))
                    {
                        continue;
                    }

                    PanelGroup panelGroup_Host = panelType_Host.PanelGroup();
                    if (panelGroup_Host != PanelGroup.Undefined && panelGroup_Host != panelGroup_Candidate)
                    {
                        mismatched++;
                        if (!groups_Other.Contains(panelGroup_Host))
                        {
                            groups_Other.Add(panelGroup_Host);
                        }
                    }
                }

                if (mismatched > 0)
                {
                    string text = string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} is made for {1} (Default Panel Type {2}), but {3} of the {4} {5} sit in {6}: Edit > ModelCheck will warn about {7}.",
                        candidate.Name,
                        GroupName(panelGroup_Candidate),
                        panelType_Candidate,
                        mismatched,
                        apertures_Basis.Count,
                        Apertures(apertures_Basis.Count),
                        string.Join(" and ", groups_Other.Select(GroupName)),
                        mismatched == 1 ? "it" : "them");

                    result.Add(new GlazingRowWarning(GlazingWarningKind.PanelGroup, "made for " + GroupName(panelGroup_Candidate), text, false));
                }
            }

            return result;
        }

        private static string GroupName(PanelGroup panelGroup)
        {
            switch (panelGroup)
            {
                case PanelGroup.Floor:
                    return "floors";

                case PanelGroup.Roof:
                    return "roofs";

                case PanelGroup.Wall:
                    return "walls";

                default:
                    return "other panels";
            }
        }

        // ---- Helpers --------------------------------------------------------------------------------------

        private void Set(ref string field, string value)
        {
            value = value ?? string.Empty;
            if (field == value)
            {
                return;
            }

            field = value;
            Refresh();
        }

        private void Set(ref bool field, bool value)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Refresh();
        }

        private static double Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return double.NaN;
            }

            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out double value) &&
                !double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return double.NaN;
            }

            return value >= 0 && !double.IsInfinity(value) ? value : double.NaN;
        }

        private static string Apertures(int count)
        {
            return count == 1 ? "aperture" : "apertures";
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnDerivedPropertiesChanged()
        {
            foreach (string name in derivedProperties)
            {
                OnPropertyChanged(name);
            }
        }
    }
}
