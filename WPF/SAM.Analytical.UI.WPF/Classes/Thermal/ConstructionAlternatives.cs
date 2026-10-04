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
    public enum ConstructionAlternativesStatus
    {
        /// <summary>No target is typed yet, so there is nothing to compare against.</summary>
        Idle,

        /// <summary>The U-values of the existing constructions are being calculated.</summary>
        Calculating,

        /// <summary>The list is complete for the current target and basis.</summary>
        Ready,

        /// <summary>The existing constructions could not be calculated (Tas unavailable); the generated variant is unaffected.</summary>
        Failed,
    }

    /// <summary>
    /// The alternatives to the generated thickness variant for ONE opaque row, free of WPF types so it is unit-testable: <b>select wall →
    /// target U → alternatives</b>. Next to the variant the existing U-value calculation generates (<see cref="UValueViewModel"/>), it lists
    /// the EXISTING constructions that meet the target or come close - the model's own, and the default library's - each with its U-value
    /// on the row's heat-flow basis, where it comes from and what choosing it does. Choosing one replaces the generated variant as the row's
    /// change (<see cref="CreateRequest"/> builds a <see cref="SetConstructionRequest"/>); nothing here touches the model, and nothing is
    /// chosen for the user.
    /// <para>
    /// <b>No second calculation engine.</b> The U-values come from the existing Tas <c>ThermalTransmittanceCalculator</c> through
    /// <see cref="IConstructionUValueEvaluator"/>: ONE batch per pool, never one call per construction, and a <see cref="ConstructionUValueCache"/>
    /// keyed by content, so a redraw, another target or another row never asks again.
    /// </para>
    /// <para>
    /// <b>Identity</b> is the Guid (several constructions can share a name). Pools stay outside the model until Apply, and then only
    /// the chosen construction and the materials it lacks enter.
    /// </para>
    /// </summary>
    public sealed class ConstructionAlternatives : INotifyPropertyChanged, IDisposable
    {
        /// <summary>A construction "closely matches" when its U-value is at most this much above the target (10 %).</summary>
        public const double CloseFactor = 1.10;

        /// <summary>The list shows at most this many existing constructions.</summary>
        public const int MaxRows = 30;

        private const double Tolerance_U = 0.0005;

        private readonly AnalyticalModel analyticalModel;
        private readonly UValueViewModel uValue;
        private readonly IConstructionUValueEvaluator evaluator;
        private readonly ConstructionUValueCache cache;
        private readonly Func<GlazingSource> createLibrary;
        private readonly ThermalSourceCatalog catalog;
        private readonly UserConstructionLibrary userConstructions;
        private readonly SynchronizationContext userContext;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly Dictionary<(Guid, HeatFlowDirection, bool), string> keys = new Dictionary<(Guid, HeatFlowDirection, bool), string>();
        private readonly Dictionary<string, string> failures = new Dictionary<string, string>();
        private readonly HashSet<string> inFlight = new HashSet<string>();
        private readonly List<Task> tasks = new List<Task>();
        private List<ConstructionCandidate> candidates;
        private Dictionary<Guid, int> usedBy;
        private HashSet<string> modelNames;

        private IReadOnlyList<ConstructionAlternativeRow> rows = new List<ConstructionAlternativeRow>();
        private ConstructionAlternativesStatus status = ConstructionAlternativesStatus.Idle;
        private string statusMessage;
        private Guid? selectedGuid;
        private int notCalculated;
        private List<string> notes = new List<string>();
        private int disposed;
        private bool sourcesRequested;

        /// <param name="analyticalModel">The model (read only).</param>
        /// <param name="uValue">The row's U-value view-model: the target, the heat-flow basis, the scope and the generated variant.</param>
        /// <param name="evaluator">The batch U-value calculation (real or fake).</param>
        /// <param name="cache">The session's U-values of constructions as they are.</param>
        /// <param name="library">The default library as a source (created on first use); null for the model's constructions only.</param>
        /// <param name="catalog">The sources the user added, beyond the model and the default library; null for none. They are read when needed and the list follows them as they arrive.</param>
        /// <param name="userConstructions">
        /// "My constructions": its saved constructions come after the default library and before the added sources, read when the list is built and
        /// again whenever the library says it changed (on the thread this list was created on); null for none.
        /// </param>
        public ConstructionAlternatives(AnalyticalModel analyticalModel, UValueViewModel uValue, IConstructionUValueEvaluator evaluator, ConstructionUValueCache cache, Func<GlazingSource> library, ThermalSourceCatalog catalog = null, UserConstructionLibrary userConstructions = null)
        {
            this.analyticalModel = analyticalModel ?? throw new ArgumentNullException(nameof(analyticalModel));
            this.uValue = uValue ?? throw new ArgumentNullException(nameof(uValue));
            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            this.cache = cache ?? new ConstructionUValueCache();
            createLibrary = library;
            this.catalog = catalog;

            if (catalog != null)
            {
                catalog.SourcesChanged += Catalog_SourcesChanged;
            }

            // A Save / Rename / Remove may come from another thread: the list is refreshed on the thread it was created on (the panel's).
            this.userConstructions = userConstructions;
            if (userConstructions != null)
            {
                userContext = SynchronizationContext.Current;
                userConstructions.Changed += UserConstructions_Changed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ---- State ------------------------------------------------------------------------------------------------

        public ConstructionAlternativesStatus Status => status;

        public string StatusMessage => statusMessage;

        /// <summary>True while the existing constructions are being calculated.</summary>
        public bool IsBusy => status == ConstructionAlternativesStatus.Calculating;

        /// <summary>The list: the generated variant first, then the existing constructions that meet the target or are within 10 % of it.</summary>
        public IReadOnlyList<ConstructionAlternativeRow> Rows => rows;

        /// <summary>Notes about the sources of the list, e.g. why "My constructions" could not be used; empty when there are none.</summary>
        public IReadOnlyList<string> Notes => notes;

        /// <summary>The existing constructions in the list (not the generated variant).</summary>
        public int ExistingCount => rows.Count(x => !x.IsGenerated);

        /// <summary>E.g. "3 existing constructions meet U 0.180; 2 more are within 10 %." / "Calculating 42 existing constructions…".</summary>
        public string CountText
        {
            get
            {
                switch (status)
                {
                    case ConstructionAlternativesStatus.Idle:
                        return string.Empty;

                    case ConstructionAlternativesStatus.Calculating:
                        return catalog != null && catalog.IsLoading && inFlight.Count == 0
                            ? "Reading the added sources…"
                            : string.Format(CultureInfo.CurrentCulture, "Calculating the U-values of {0} existing constructions…", Math.Max(inFlight.Count, 1));

                    case ConstructionAlternativesStatus.Failed:
                        return statusMessage ?? "The existing constructions could not be calculated.";
                }

                int meets = rows.Count(x => !x.IsGenerated && x.Meets);
                int close = rows.Count(x => !x.IsGenerated && !x.Meets);
                string target = uValue.TargetThermalTransmittance.ToString("0.###", CultureInfo.CurrentCulture);

                if (meets == 0 && close == 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "No existing construction meets U {0} or is within 10 % of it.", target);
                }

                string text = string.Format(CultureInfo.CurrentCulture, "{0} existing {1} U {2}", meets, meets == 1 ? "construction meets" : "constructions meet", target);
                if (close > 0)
                {
                    text += string.Format(CultureInfo.CurrentCulture, "; {0} more {1} within 10 %", close, close == 1 ? "is" : "are");
                }

                if (notCalculated > 0)
                {
                    text += string.Format(CultureInfo.CurrentCulture, "; {0} could not be calculated", notCalculated);
                }

                return text + ".";
            }
        }

        // ---- Choice -----------------------------------------------------------------------------------------------

        /// <summary>The chosen existing construction's Guid; null while the generated variant is chosen (the default).</summary>
        public Guid? SelectedGuid
        {
            get => selectedGuid;
            set
            {
                if (value.HasValue && !rows.Any(x => x.Guid == value))
                {
                    return;
                }

                if (selectedGuid == value)
                {
                    return;
                }

                selectedGuid = value;
                Raise();
            }
        }

        /// <summary>The row the choice stands for (the generated variant when none is chosen).</summary>
        public ConstructionAlternativeRow SelectedRow
        {
            get => rows.FirstOrDefault(x => selectedGuid.HasValue ? x.Guid == selectedGuid : x.IsGenerated);
            set => SelectedGuid = value == null || value.IsGenerated ? (Guid?)null : value.Guid;
        }

        /// <summary>True while an existing construction is chosen: the row's change is its assignment, not the generated variant.</summary>
        public bool ExistingChosen => selectedGuid.HasValue && SelectedRow != null && !SelectedRow.IsGenerated;

        /// <summary>The compatibility notes of the chosen construction (another panel group, a name that gets a suffix, materials added).</summary>
        public IReadOnlyList<string> Warnings => ExistingChosen ? SelectedRow.Warnings : new List<string>();

        /// <summary>Why the chosen existing construction cannot be applied (a material, Keep name, the scope); null when nothing blocks it or none is chosen.</summary>
        public string ApplyBlockReason
        {
            get
            {
                if (!ExistingChosen)
                {
                    return null;
                }

                ConstructionAlternativeRow row = SelectedRow;
                if (row.BlockReason != null)
                {
                    return row.BlockReason;
                }

                if (uValue.KeepName)
                {
                    return "Keep name changes the generated construction itself; turn it off to assign an existing one.";
                }

                if (uValue.ApplyScope == ThermalApplyScope.DontAssign)
                {
                    return "An existing construction is always assigned: choose all the panels using the current construction, or the selected ones.";
                }

                if (uValue.ApplyScope == ThermalApplyScope.SelectedOnly && !uValue.Scope.SelectedAvailable)
                {
                    return uValue.Scope.SelectedUnavailableReason;
                }

                if (!row.CanApply)
                {
                    return "Its U-value is not calculated.";
                }

                return null;
            }
        }

        /// <summary>True only with an existing construction chosen, its U-value calculated and nothing blocking it.</summary>
        public bool ApplyEnabled => ExistingChosen && ApplyBlockReason == null && !IsBusy;

        /// <summary>The preview line of the chosen construction, e.g. "U 0.260 → 0.150 W/m²K · SIM_EXT_B (Existing model)".</summary>
        public string PreviewText
        {
            get
            {
                if (!ExistingChosen)
                {
                    return string.Empty;
                }

                ConstructionAlternativeRow row = SelectedRow;
                return string.Format(CultureInfo.CurrentCulture, "U {0} → {1} W/m²K · {2} ({3})", Format(uValue.CurrentThermalTransmittance), Format(row.ThermalTransmittance), row.Name, row.KindText);
            }
        }

        /// <summary>What happens to the constructions, e.g. "Assigns SIM_EXT_B to 12 panels; SIM_EXT_SLD stays unchanged."</summary>
        public string ResultText
        {
            get
            {
                if (!ExistingChosen)
                {
                    return string.Empty;
                }

                ConstructionAlternativeRow row = SelectedRow;
                int count = uValue.Scope.BasisGuids(uValue.ApplyScope == ThermalApplyScope.DontAssign ? ThermalApplyScope.AllUsing : uValue.ApplyScope).Count;
                string added = row.Kind == ConstructionAlternativeKind.Model ? string.Empty : " It is added to the model with the materials it lacks.";
                return string.Format(CultureInfo.CurrentCulture, "Assigns {0} to {1} {2}; {3} stays unchanged.{4}", row.Name, count, count == 1 ? "panel" : "panels", uValue.ConstructionName, added);
            }
        }

        // ---- Commands ---------------------------------------------------------------------------------------------

        /// <summary>The change to apply (the existing construction assigned), or null while <see cref="ApplyEnabled"/> is false.</summary>
        public SetConstructionRequest CreateRequest()
        {
            if (!ApplyEnabled)
            {
                return null;
            }

            ConstructionAlternativeRow row = SelectedRow;
            ConstructionCandidate candidate = row.Candidate;

            return new SetConstructionRequest()
            {
                SourceConstructionGuid = uValue.ConstructionGuid,
                Construction = new Construction(candidate.Construction),
                MaterialsToAdd = candidate.MaterialsToAdd.ToList(),
                Scope = uValue.ApplyScope,
                SelectedPanelGuids = uValue.Scope.SelectedGuids.ToList(),
                OldThermalTransmittance = uValue.CurrentThermalTransmittance,
                NewThermalTransmittance = row.ThermalTransmittance,
                TargetThermalTransmittance = uValue.TargetThermalTransmittance,
                HeatFlowDirection = uValue.HeatFlowDirection,
                SourceLabel = candidate.Source.Label,
                SourceKind = candidate.Kind,
                Notes = row.Warnings.ToList(),
            };
        }

        /// <summary>
        /// Brings the list up to date with the row's target, heat-flow basis and scope: builds it from what is cached and asks the evaluator,
        /// once per pool, for the U-values that are not. Cheap when everything is cached; call it whenever the row's inputs change.
        /// </summary>
        public void Refresh()
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                return;
            }

            double target = uValue.TargetThermalTransmittance;
            if (double.IsNaN(target))
            {
                rows = new List<ConstructionAlternativeRow>();
                selectedGuid = null;
                notCalculated = 0;
                status = ConstructionAlternativesStatus.Idle;
                statusMessage = null;
                Raise();
                return;
            }

            EnsureCandidates();
            if (catalog != null && !sourcesRequested)
            {
                // The remembered sources are read the first time a row needs candidates (not when the model opens); the list is rebuilt as each arrives.
                sourcesRequested = true;
                lock (tasks)
                {
                    tasks.Add(catalog.EnsureLoadedAsync());
                }
            }

            HeatFlowDirection direction = uValue.HeatFlowDirection;
            bool external = uValue.External;

            Dictionary<PanelGroup, int> groups = PanelGroups(uValue.ScopePanels);

            foreach (IGrouping<GlazingSource, ConstructionCandidate> group in candidates.GroupBy(x => x.Source))
            {
                List<ConstructionCandidate> missing = new List<ConstructionCandidate>();
                foreach (ConstructionCandidate candidate in group)
                {
                    string key = Key(candidate, direction, external);
                    if (key != null && !cache.Contains(key) && !inFlight.Contains(key) && !failures.ContainsKey(key))
                    {
                        missing.Add(candidate);
                    }
                }

                // A big source (hundreds of constructions) is asked for in chunks, the ones made for the panels' own group first, so the list fills in
                // as each chunk is calculated instead of waiting for the last: still one Tas run per chunk, in order, and every U-value cached.
                List<ConstructionCandidate> ordered = missing.OrderBy(x => Mismatched(x, groups, out _) == 0 ? 0 : 1).ToList();
                for (int index = 0; index < ordered.Count; index += TasConstructionUValueEvaluator.ChunkSize)
                {
                    Start(group.Key, ordered.GetRange(index, Math.Min(TasConstructionUValueEvaluator.ChunkSize, ordered.Count - index)), direction, external);
                }
            }

            Build();
        }

        /// <summary>Completes when every running batch has finished (tests await it).</summary>
        internal Task Idle()
        {
            lock (tasks)
            {
                return Task.WhenAll(tasks.ToArray());
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (catalog != null)
                {
                    catalog.SourcesChanged -= Catalog_SourcesChanged;
                }

                if (userConstructions != null)
                {
                    userConstructions.Changed -= UserConstructions_Changed;
                }

                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }

        // ---- Evaluation -------------------------------------------------------------------------------------------

        private void Start(GlazingSource source, List<ConstructionCandidate> missing, HeatFlowDirection direction, bool external)
        {
            List<string> keys_Batch = missing.Select(x => Key(x, direction, external)).ToList();
            foreach (string key in keys_Batch)
            {
                inFlight.Add(key);
            }

            ConstructionUValueRequest request = new ConstructionUValueRequest(missing.Select(x => x.Construction), source.ConstructionManager?.MaterialLibrary, direction, external);

            Task task = EvaluateAsync(request, missing, keys_Batch);
            lock (tasks)
            {
                tasks.Add(task);
            }
        }

        private async Task EvaluateAsync(ConstructionUValueRequest request, List<ConstructionCandidate> missing, List<string> keys_Batch)
        {
            IReadOnlyList<ConstructionUValue> results = null;
            string failure = null;
            try
            {
                results = await evaluator.EvaluateAsync(request, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Disposed: nobody wants it.
                return;
            }
            catch (Exception exception)
            {
                failure = exception.Message;
            }

            if (Volatile.Read(ref disposed) != 0)
            {
                return;
            }

            for (int i = 0; i < missing.Count; i++)
            {
                string key = keys_Batch[i];
                inFlight.Remove(key);

                ConstructionUValue result = results?.FirstOrDefault(x => x != null && x.Guid == missing[i].Guid);
                if (result != null && result.Calculated)
                {
                    cache.Set(key, result.ThermalTransmittance);
                }
                else
                {
                    failures[key] = result?.Message ?? failure ?? "Tas did not return a U-value for it.";
                }
            }

            Build();
        }

        // ---- The list ---------------------------------------------------------------------------------------------

        private void EnsureCandidates()
        {
            if (candidates != null)
            {
                return;
            }

            candidates = new List<ConstructionCandidate>();
            usedBy = new Dictionary<Guid, int>();

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            GlazingSource source_Model = GlazingSource.ConstructionsFromModel(analyticalModel);
            Dictionary<string, IMaterial> materials_Model = source_Model.GetMaterials();
            modelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            HashSet<Guid> guids = new HashSet<Guid>() { uValue.ConstructionGuid };
            List<Construction> constructions_Model = source_Model.GetConstructions();
            foreach (Construction construction in constructions_Model)
            {
                guids.Add(construction.Guid);
                if (construction.Name != null)
                {
                    modelNames.Add(construction.Name.Trim());
                }
            }

            foreach (Construction construction in constructions_Model)
            {
                if (construction.Guid == uValue.ConstructionGuid || !Opaque(construction, source_Model))
                {
                    continue;
                }

                usedBy[construction.Guid] = adjacencyCluster?.GetPanels(construction)?.Count ?? 0;
                candidates.Add(new ConstructionCandidate(construction, source_Model, materials_Model));
            }

            GlazingSource source_Library = null;
            try
            {
                source_Library = createLibrary?.Invoke();
            }
            catch (Exception)
            {
                // A library that cannot be read only means the model's own constructions are offered.
            }

            if (source_Library != null)
            {
                foreach (Construction construction in source_Library.GetConstructions())
                {
                    if (Opaque(construction, source_Library) && guids.Add(construction.Guid))
                    {
                        candidates.Add(new ConstructionCandidate(construction, source_Library, materials_Model));
                    }
                }
            }

            // "My constructions": after the default library, before the added sources. Read now (a small file; missing = empty, unreadable = a
            // note and the other sources keep working); the first of a Guid still wins, so a saved construction that is in the model already is the model's.
            notes = new List<string>();
            if (userConstructions != null)
            {
                GlazingSource source_User = GlazingSource.FromUserConstructions(userConstructions);
                if (!string.IsNullOrEmpty(source_User.Note))
                {
                    notes.Add(source_User.Note);
                }

                foreach (Construction construction in source_User.GetConstructions())
                {
                    if (Opaque(construction, source_User) && guids.Add(construction.Guid))
                    {
                        candidates.Add(new ConstructionCandidate(construction, source_User, materials_Model));
                    }
                }
            }

            // The sources the user added, in the order added: the first of a Guid wins (a construction in the model, the library, "My constructions"
            // or an earlier source is not offered a second time).
            foreach (GlazingSource source in catalog?.ReadySources ?? new List<GlazingSource>())
            {
                foreach (Construction construction in source.GetConstructions())
                {
                    if (Opaque(construction, source) && guids.Add(construction.Guid))
                    {
                        candidates.Add(new ConstructionCandidate(construction, source, materials_Model));
                    }
                }
            }
        }

        // Only an opaque construction is an alternative for an opaque row. A glazing system stored as a construction - transparent panes and gas,
        // no opaque layer, e.g. the default library's SIM_EXT_GLZ / SIM_INT_GLZ for curtain walls - has no opaque U-value (Tas answers 0 in the
        // opaque slots and gives its U-value as glazing), so it is never offered. A construction whose materials cannot all be found is not
        // excluded here: it is listed as not calculated, as before.
        private static bool Opaque(Construction construction, GlazingSource source)
        {
            MaterialType materialType = Analytical.Query.MaterialType(construction?.ConstructionLayers, source?.ConstructionManager?.MaterialLibrary);
            return materialType != Core.MaterialType.Transparent && materialType != Core.MaterialType.Gas;
        }

        // "My constructions" changed (a Save, Rename or Remove): the candidates are read again - on the thread the list was created on. The U-values
        // already calculated are cached by content, so only a construction not seen before is asked of Tas; a chosen construction that is gone
        // from the list drops back to the generated variant (Build).
        private void UserConstructions_Changed(object sender, EventArgs e)
        {
            SynchronizationContext context = userContext;
            if (context == null || context == SynchronizationContext.Current)
            {
                RefreshUserConstructions();
                return;
            }

            context.Post(_ => RefreshUserConstructions(), null);
        }

        private void RefreshUserConstructions()
        {
            if (Volatile.Read(ref disposed) != 0 || candidates == null)
            {
                return;
            }

            candidates = null;
            Refresh();
        }

        // A source arrived (or was removed): the candidates are rebuilt from the sources there are now.
        private void Catalog_SourcesChanged(object sender, EventArgs e)
        {
            if (Volatile.Read(ref disposed) != 0 || candidates == null)
            {
                return;
            }

            candidates = null;
            Refresh();
        }

        private string Key(ConstructionCandidate candidate, HeatFlowDirection direction, bool external)
        {
            (Guid, HeatFlowDirection, bool) id = (candidate.Guid, direction, external);
            if (!keys.TryGetValue(id, out string key))
            {
                key = ConstructionUValueCache.Key(candidate.Construction, candidate.Source.ConstructionManager?.MaterialLibrary, direction, external);
                keys[id] = key;
            }

            return key;
        }

        private void Build()
        {
            double target = uValue.TargetThermalTransmittance;
            if (double.IsNaN(target))
            {
                return;
            }

            HeatFlowDirection direction = uValue.HeatFlowDirection;
            bool external = uValue.External;
            Dictionary<PanelGroup, int> groups = PanelGroups(uValue.ScopePanels);
            int panelCount = uValue.ScopePanels.Count;

            List<ConstructionAlternativeRow> list = new List<ConstructionAlternativeRow>();

            // The generated variant: always first, selectable only when the thickness calculation reached the target.
            bool reached = uValue.Status == UValuePreviewStatus.Reached;
            list.Add(new ConstructionAlternativeRow(
                ConstructionAlternativeKind.Generated,
                null,
                uValue.NewConstructionName ?? uValue.ConstructionName + " (adjusted)",
                reached ? uValue.CalculatedThermalTransmittance : double.NaN,
                target,
                "Generated",
                null,
                reached ? string.Join(" / ", uValue.PreviewRows.Select(x => string.Format(CultureInfo.CurrentCulture, "{0:0.#} {1}", x.ThicknessAfter, x.Name))) : null,
                0,
                new List<string>(),
                reached ? uValue.ApplyBlockReason : null,
                reached ? null : (uValue.StatusMessage ?? "not calculated"),
                null));

            int calculating = 0;
            notCalculated = 0;
            List<ConstructionAlternativeRow> existing = new List<ConstructionAlternativeRow>();
            foreach (ConstructionCandidate candidate in candidates ?? new List<ConstructionCandidate>())
            {
                string key = Key(candidate, direction, external);
                double u = double.NaN;
                if (key == null || !cache.TryGet(key, out u) || !ConstructionUValue.Valid(u))
                {
                    if (key != null && inFlight.Contains(key))
                    {
                        calculating++;
                    }
                    else
                    {
                        notCalculated++;
                    }

                    continue;
                }

                if (u > target * CloseFactor + Tolerance_U)
                {
                    continue;
                }

                existing.Add(new ConstructionAlternativeRow(
                    Kind(candidate.Kind),
                    candidate.Guid,
                    candidate.Name,
                    u,
                    target,
                    candidate.Source.Label,
                    candidate.ShortId,
                    candidate.BuildUp,
                    usedBy.TryGetValue(candidate.Guid, out int used) ? used : 0,
                    CandidateWarnings(candidate, groups, panelCount),
                    candidate.MaterialIssue,
                    null,
                    candidate)
                {
                    MadeForOtherGroup = Mismatched(candidate, groups, out _) != 0,
                });
            }

            // Simple and predictable: the ones that meet the target first, those made for the panels' own group before the others, the closest to
            // the target first (the least over-insulated), then the near misses the same way; the model's own before a library's among equals.
            // Nothing is chosen for the user.
            list.AddRange(existing
                .OrderBy(x => x.Meets ? 0 : 1)
                .ThenBy(x => x.MadeForOtherGroup ? 1 : 0)
                .ThenBy(x => Math.Abs(x.Margin))
                .ThenBy(x => x.Kind == ConstructionAlternativeKind.Model ? 0 : 1)
                .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(MaxRows));

            rows = list;

            if (selectedGuid.HasValue && !rows.Any(x => x.Guid == selectedGuid))
            {
                // The choice is no longer in the list (another target or basis): back to the generated variant, never to another one.
                selectedGuid = null;
            }

            if (calculating > 0 || inFlight.Count > 0 || (catalog != null && catalog.IsLoading))
            {
                status = ConstructionAlternativesStatus.Calculating;
                statusMessage = null;
            }
            else if (existing.Count == 0 && notCalculated > 0 && candidates != null && candidates.Count == notCalculated)
            {
                status = ConstructionAlternativesStatus.Failed;
                statusMessage = failures.Values.FirstOrDefault() ?? "The existing constructions could not be calculated.";
            }
            else
            {
                status = ConstructionAlternativesStatus.Ready;
                statusMessage = null;
            }

            Raise();
        }

        private static ConstructionAlternativeKind Kind(GlazingSourceKind kind)
        {
            switch (kind)
            {
                case GlazingSourceKind.Model:
                    return ConstructionAlternativeKind.Model;

                case GlazingSourceKind.Library:
                    return ConstructionAlternativeKind.Library;

                case GlazingSourceKind.User:
                    return ConstructionAlternativeKind.User;

                default:
                    return ConstructionAlternativeKind.Loaded;
            }
        }

        // The panels in scope by panel group (those with no group at all are not counted: they cannot disagree with a construction).
        private static Dictionary<PanelGroup, int> PanelGroups(IEnumerable<Panel> panels)
        {
            Dictionary<PanelGroup, int> result = new Dictionary<PanelGroup, int>();
            foreach (Panel panel in panels)
            {
                PanelGroup panelGroup = panel.PanelType.PanelGroup();
                if (panelGroup != PanelGroup.Undefined)
                {
                    result[panelGroup] = result.TryGetValue(panelGroup, out int count) ? count + 1 : 1;
                }
            }

            return result;
        }

        // How many of the panels sit in another group than the one the candidate was made for (0 when it names none).
        private static int Mismatched(ConstructionCandidate candidate, Dictionary<PanelGroup, int> groups, out List<PanelGroup> groups_Other)
        {
            groups_Other = new List<PanelGroup>();

            PanelGroup panelGroup_Candidate = candidate.PanelType.PanelGroup();
            if (panelGroup_Candidate == PanelGroup.Undefined)
            {
                return 0;
            }

            int mismatched = 0;
            foreach (KeyValuePair<PanelGroup, int> pair in groups)
            {
                if (pair.Key != panelGroup_Candidate)
                {
                    mismatched += pair.Value;
                    groups_Other.Add(pair.Key);
                }
            }

            return mismatched;
        }

        // What a candidate is marked for before it is chosen: a Default Panel Type of another panel group than the panels in scope, a
        // name the model already has (it is added under a suffix), and the materials Apply adds.
        private List<string> CandidateWarnings(ConstructionCandidate candidate, Dictionary<PanelGroup, int> groups, int panelCount)
        {
            List<string> result = new List<string>();

            int mismatched = Mismatched(candidate, groups, out List<PanelGroup> groups_Other);
            if (mismatched > 0)
            {
                PanelType panelType_Candidate = candidate.PanelType;
                result.Add(string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} is made for {1} (Default Panel Type {2}), but {3} of the {4} {5} sit in {6}: it is not what the construction was made for.",
                    candidate.Name,
                    GroupName(panelType_Candidate.PanelGroup()),
                    panelType_Candidate,
                    mismatched,
                    panelCount,
                    panelCount == 1 ? "panel" : "panels",
                    string.Join(" and ", groups_Other.Select(GroupName))));
            }

            if (candidate.Kind != GlazingSourceKind.Model)
            {
                if (candidate.Name != null && modelNames != null && modelNames.Contains(candidate.Name.Trim()))
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "The model already has a construction named {0}; this one is added to the model under a numbered name.", candidate.Name));
                }

                if (candidate.MaterialsToAdd.Count != 0)
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "Adds {0} {1} to the model: {2}.", candidate.MaterialsToAdd.Count, candidate.MaterialsToAdd.Count == 1 ? "material" : "materials", string.Join(", ", candidate.MaterialsToAdd.Select(x => x.Name))));
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
                    return panelGroup.ToString().ToLowerInvariant();
            }
        }

        private static string Format(double value)
        {
            return double.IsNaN(value) ? "–" : value.ToString("0.000", CultureInfo.CurrentCulture);
        }

        private void Raise()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
