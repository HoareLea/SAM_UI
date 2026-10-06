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
    /// The state of the one-window "Set U-value" flow (opaque constructions), free of WPF types so it is
    /// unit-testable. The window binds to it; <c>Modify.SetUValue</c> applies <see cref="CreateRequest"/>.
    /// <para>
    /// <b>Identity.</b> The affected panels are those whose construction Guid matches
    /// (<c>AdjacencyCluster.GetPanels(Construction)</c>); names are only shown. Other constructions sharing the
    /// name are warned about, because the legacy post-step <c>UpdateConstructions</c> matches by name.
    /// </para>
    /// <para>
    /// <b>Stale results.</b> Every change of an input starts a new evaluation and cancels the previous one; a
    /// result that arrives for an older request is dropped, so the preview always matches the current inputs.
    /// </para>
    /// <para>
    /// <b>Apply</b> is enabled only while a reached result for the current inputs is shown.
    /// </para>
    /// </summary>
    public sealed class UValueViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>Default thickness range of the varied layer [m] (as <c>LayerThicknessCalculationData</c>).</summary>
        public const double DefaultMinThickness = 0.001;

        public const double DefaultMaxThickness = 1.0;

        // A target this close to the current U is "no change" (U-values are shown to 3 decimals).
        private const double NoChangeTolerance = 0.0005;

        private static readonly string[] derivedProperties = new[]
        {
            nameof(CurrentThermalTransmittance), nameof(TargetThermalTransmittance), nameof(Status), nameof(StatusMessage),
            nameof(IsBusy), nameof(Evaluation), nameof(CalculatedThickness), nameof(CalculatedThermalTransmittance), nameof(Margin),
            nameof(BestAchievableThermalTransmittance), nameof(MinThicknessThermalTransmittance), nameof(MaxThicknessThermalTransmittance),
            nameof(LastEvaluationMilliseconds), nameof(LayerIndex), nameof(LayerSentence), nameof(PreviewRows), nameof(HeatFlowBasis),
            nameof(HeatFlowDirection), nameof(External), nameof(ScopeText), nameof(ResultText), nameof(NewConstructionName),
            nameof(Warnings), nameof(ApplyBlockReason), nameof(ApplyEnabled), nameof(KeepName), nameof(KeepNameUnavailableReason),
            nameof(ScopeUnavailableReason), nameof(DontAssignUnavailableReason),
        };

        private readonly IUValueEvaluator evaluator;
        private readonly Construction construction;
        private readonly MaterialLibrary materialLibrary;
        private readonly List<Panel> panels_Using;
        private readonly ThermalScope scope;
        private readonly List<string> constructionNames;
        private readonly int sameNameCount;
        private readonly int automaticLayerIndex;

        private string targetText = string.Empty;
        private bool targetEdited;
        private double targetThermalTransmittance = double.NaN;
        private double currentThermalTransmittance = double.NaN;
        private int? layerIndexOverride;
        private double minThickness = DefaultMinThickness;
        private double maxThickness = DefaultMaxThickness;
        private HeatFlowDirection? heatFlowDirectionOverride;
        private UValueApplyMode applyMode = UValueApplyMode.NewConstruction;
        private UValueHeatFlowBasis heatFlowBasis;

        private UValueEvaluation evaluation;
        private UValuePreviewStatus status = UValuePreviewStatus.None;
        private string statusMessage;
        private bool busy;
        private long lastEvaluationMilliseconds = -1;

        // U at the range ends, kept across evaluations (a "no change" target clears the evaluation) for the inputs they were calculated for.
        private string rangeKey;
        private double rangeMinThicknessThermalTransmittance = double.NaN;
        private double rangeMaxThicknessThermalTransmittance = double.NaN;
        private int version;
        private CancellationTokenSource cancellationTokenSource;

        /// <param name="analyticalModel">The model (read only; the view-model keeps its own copies).</param>
        /// <param name="constructionGuid">The construction to change.</param>
        /// <param name="selectedPanelGuids">The selected panels; only those using the construction count.</param>
        /// <param name="evaluator">The U-value calculation (real or fake).</param>
        public UValueViewModel(AnalyticalModel analyticalModel, Guid constructionGuid, IEnumerable<Guid> selectedPanelGuids, IUValueEvaluator evaluator)
        {
            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster ?? throw new ArgumentNullException(nameof(analyticalModel));
            List<Construction> constructions = adjacencyCluster.GetConstructions() ?? new List<Construction>();

            construction = constructions.Find(x => x != null && x.Guid == constructionGuid) ?? throw new ArgumentException("The construction is not in the model.", nameof(constructionGuid));
            materialLibrary = analyticalModel.MaterialLibrary ?? new MaterialLibrary("Default MaterialLibrary");
            panels_Using = adjacencyCluster.GetPanels(construction) ?? new List<Panel>();

            scope = new ThermalScope(construction.Name, "panel", "panels", panels_Using.Select(x => x.Guid), selectedPanelGuids);

            constructionNames = constructions.Where(x => x != null).Select(x => x.Name).ToList();
            sameNameCount = constructions.Count(x => x != null && x.Guid != construction.Guid && x.Name == construction.Name);

            automaticLayerIndex = Tas.Query.AdjustableLayerIndex(construction, materialLibrary);
            heatFlowBasis = Query.UValueHeatFlowBasis(BasisPanels(), construction);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // ---- Source ---------------------------------------------------------------------------------------

        public Guid ConstructionGuid => construction.Guid;

        public string ConstructionName => construction.Name;

        /// <summary>The construction's layers as they are now (thicknesses in mm), for the layer override list.</summary>
        public IReadOnlyList<UValueLayerRow> Layers => construction.ConstructionLayers.Select((x, i) => new UValueLayerRow(i, x?.Name, Millimetres(x?.Thickness ?? double.NaN), Millimetres(x?.Thickness ?? double.NaN), i == automaticLayerIndex)).ToList();

        /// <summary>U-value of the construction as it is [W/m²K] on the current heat-flow basis; NaN until calculated.</summary>
        public double CurrentThermalTransmittance => currentThermalTransmittance;

        /// <summary>The construction to change, as it is in the model (the alternatives list excludes it and compares against it).</summary>
        internal Construction SourceConstruction => construction;

        /// <summary>The panels the change reaches under the current scope (the heat-flow basis and the panel-group check read them).</summary>
        internal IReadOnlyList<Panel> ScopePanels => BasisPanels().ToList();

        /// <summary>How many panels use the construction (by Guid).</summary>
        public int PanelsUsingCount => panels_Using.Count;

        /// <summary>How many of the selected panels use the construction.</summary>
        public int SelectedPanelsCount => scope.SelectedCount;

        public IReadOnlyList<Guid> SelectedPanelGuids => scope.SelectedGuids;

        /// <summary>The shared scope: the pinned panels, the choice and the labels (the window's "Changes" radios read it).</summary>
        public ThermalScope Scope => scope;

        // ---- Target ---------------------------------------------------------------------------------------

        /// <summary>The target as typed. Pre-filled with the current U-value until the user types.</summary>
        public string TargetText
        {
            get => targetText;
            set
            {
                targetEdited = true;
                SetTargetText(value);
            }
        }

        /// <summary>The parsed target [W/m²K]; NaN when the text is not a positive number.</summary>
        public double TargetThermalTransmittance => targetThermalTransmittance;

        // ---- Layer, range, heat flow ----------------------------------------------------------------------

        /// <summary>The layer chosen automatically (<c>Tas.Query.AdjustableLayerIndex</c>: never gas or glass); -1 if none.</summary>
        public int AutomaticLayerIndex => automaticLayerIndex;

        /// <summary>Advanced: vary this layer instead of the automatic one; null for automatic.</summary>
        public int? LayerIndexOverride
        {
            get => layerIndexOverride;
            set
            {
                if (layerIndexOverride == value)
                {
                    return;
                }

                layerIndexOverride = value;
                OnPropertyChanged(nameof(LayerIndexOverride));
                Restart();
            }
        }

        /// <summary>The layer that is varied.</summary>
        public int LayerIndex => layerIndexOverride ?? automaticLayerIndex;

        /// <summary>"&lt;layer&gt; &lt;t&gt; mm will be adjusted; other layers stay fixed." (null when no layer can be).</summary>
        public string LayerSentence
        {
            get
            {
                ConstructionLayer constructionLayer = Layer(LayerIndex);
                if (constructionLayer == null)
                {
                    return null;
                }

                return string.Format(CultureInfo.CurrentCulture, "{0} {1} mm will be adjusted; other layers stay fixed.", constructionLayer.Name, Millimetres(constructionLayer.Thickness).ToString("0.#", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>Advanced: minimum thickness of the varied layer [m].</summary>
        public double MinThickness
        {
            get => minThickness;
            set
            {
                if (minThickness == value)
                {
                    return;
                }

                minThickness = value;
                OnPropertyChanged(nameof(MinThickness));
                Restart();
            }
        }

        /// <summary>Advanced: maximum thickness of the varied layer [m].</summary>
        public double MaxThickness
        {
            get => maxThickness;
            set
            {
                if (maxThickness == value)
                {
                    return;
                }

                maxThickness = value;
                OnPropertyChanged(nameof(MaxThickness));
                Restart();
            }
        }

        /// <summary>Where the heat-flow direction comes from (affected panels, or the construction's Default Panel Type).</summary>
        public UValueHeatFlowBasis HeatFlowBasis => heatFlowBasis;

        /// <summary>Advanced: calculate for this heat-flow direction instead of the basis; null for automatic.</summary>
        public HeatFlowDirection? HeatFlowDirectionOverride
        {
            get => heatFlowDirectionOverride;
            set
            {
                if (heatFlowDirectionOverride == value)
                {
                    return;
                }

                heatFlowDirectionOverride = value;
                OnPropertyChanged(nameof(HeatFlowDirectionOverride));
                Restart();
            }
        }

        public HeatFlowDirection HeatFlowDirection => heatFlowDirectionOverride ?? heatFlowBasis.HeatFlowDirection;

        /// <summary>External surface resistances (true when the basis is undefined and a direction is chosen by hand).</summary>
        public bool External => heatFlowBasis.HeatFlowDirection == HeatFlowDirection.Undefined || heatFlowBasis.External;

        // ---- Apply options --------------------------------------------------------------------------------

        /// <summary>New construction (default) or <see cref="UValueApplyMode.ModifyInPlace"/>, which the user knows as "Keep name".</summary>
        public UValueApplyMode ApplyMode
        {
            get => applyMode;
            set
            {
                if (applyMode == value)
                {
                    return;
                }

                applyMode = value;
                OnPropertyChanged(nameof(ApplyMode));
                OnPropertyChanged(nameof(EffectiveScope));
                UpdateBasis();
            }
        }

        /// <summary>
        /// Keep name: the construction itself changes and keeps its name, so every panel using it changes. The same as
        /// <see cref="ApplyMode"/> being <see cref="UValueApplyMode.ModifyInPlace"/>; check <see cref="KeepNameUnavailableReason"/> first.
        /// </summary>
        public bool KeepName
        {
            get => applyMode == UValueApplyMode.ModifyInPlace;
            set => ApplyMode = value ? UValueApplyMode.ModifyInPlace : UValueApplyMode.NewConstruction;
        }

        /// <summary>
        /// Why "Keep name" cannot be chosen now (null while it can): another construction shares the name and would change
        /// too, or the scope is not "all the panels using it" (a name cannot be kept for only some of the panels).
        /// </summary>
        public string KeepNameUnavailableReason
        {
            get
            {
                if (sameNameCount > 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Keep name is unavailable: {0} other {1} also named {2} and would change too.", sameNameCount, sameNameCount == 1 ? "construction is" : "constructions are", construction.Name);
                }

                if (applyMode == UValueApplyMode.ModifyInPlace)
                {
                    return null;
                }

                switch (scope.Scope)
                {
                    case ThermalApplyScope.SelectedOnly:
                        return string.Format(CultureInfo.CurrentCulture, "Keep name changes every panel using {0}, so it cannot be combined with only the selected panels.", construction.Name);

                    case ThermalApplyScope.DontAssign:
                        return string.Format(CultureInfo.CurrentCulture, "Keep name changes {0} itself, so it cannot be combined with Don't assign.", construction.Name);

                    default:
                        return null;
                }
            }
        }

        /// <summary>Why "only the selected panels" cannot be chosen now (Keep name is on, or none is selected); null while it can.</summary>
        public string ScopeUnavailableReason => applyMode == UValueApplyMode.ModifyInPlace
            ? string.Format(CultureInfo.CurrentCulture, "Keep name changes every panel using {0}; turn it off to change only the selected panels.", construction.Name)
            : scope.SelectedUnavailableReason;

        /// <summary>Why "Don't assign" cannot be chosen now (Keep name is on); null while it can.</summary>
        public string DontAssignUnavailableReason => applyMode == UValueApplyMode.ModifyInPlace
            ? string.Format(CultureInfo.CurrentCulture, "Keep name changes {0} itself; turn it off to create a construction without assigning it.", construction.Name)
            : null;

        /// <summary>Which panels get the new construction. Ignored while Keep name is on (it always changes every panel using the construction).</summary>
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
                OnPropertyChanged(nameof(ApplyScope));
                OnPropertyChanged(nameof(EffectiveScope));
                UpdateBasis();
            }
        }

        /// <summary>The scope Apply uses (<see cref="ThermalApplyScope.AllUsing"/> while Keep name is on).</summary>
        public ThermalApplyScope EffectiveScope => applyMode == UValueApplyMode.ModifyInPlace ? ThermalApplyScope.AllUsing : scope.Scope;

        /// <summary>
        /// The name a new construction gets (e.g. "SIM_EXT_SLD U0.50"), from the achieved U; null until a reached result
        /// is shown, so no text names a construction for a U-value that cannot be applied.
        /// </summary>
        public string NewConstructionName => evaluation != null && evaluation.Reached
            ? Query.UValueConstructionName(construction.Name, evaluation.CalculatedThermalTransmittance, constructionNames)
            : null;

        /// <summary>The inline apply scope, e.g. "Applies to 40 panels using SIM_EXT_SLD (3 selected)."</summary>
        public string ScopeText
        {
            get
            {
                string name = construction.Name;
                int count = PanelsUsingCount;

                if (applyMode == UValueApplyMode.ModifyInPlace)
                {
                    string selected = SelectedPanelsCount > 0 ? string.Format(CultureInfo.CurrentCulture, " ({0} selected)", SelectedPanelsCount) : string.Empty;
                    return count == 0
                        ? string.Format(CultureInfo.CurrentCulture, "Keeps the name {0}; no panel uses it.", name)
                        : string.Format(CultureInfo.CurrentCulture, "Applies to all {0} {1} using {2}{3}, keeping the name.", count, Panels(count), name, selected);
                }

                return scope.Text(
                    scope.Scope,
                    string.Format(CultureInfo.CurrentCulture, "Creates {0} without assigning it to any panel.", NewConstructionName ?? "a new construction"),
                    string.Format(CultureInfo.CurrentCulture, "No panel uses {0}; only the new construction is created.", name));
            }
        }

        /// <summary>What happens to the constructions, e.g. "Creates SIM_EXT_SLD U0.50; SIM_EXT_SLD stays unchanged."</summary>
        public string ResultText => applyMode == UValueApplyMode.ModifyInPlace
            ? string.Format(CultureInfo.CurrentCulture, "Keeps the name {0}: the construction itself changes, so every panel using it changes.", construction.Name)
            : string.Format(CultureInfo.CurrentCulture, "Creates {0}; {1} stays unchanged.", NewConstructionName ?? "a new construction", construction.Name);

        /// <summary>Warning lines: mixed panel groups, other constructions sharing the name.</summary>
        public IReadOnlyList<string> Warnings
        {
            get
            {
                List<string> result = new List<string>();

                if (heatFlowBasis.Mixed && heatFlowDirectionOverride == null)
                {
                    string types = string.Join(", ", heatFlowBasis.PanelTypeCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Select(x => string.Format(CultureInfo.CurrentCulture, "{0} {1}", x.Value, x.Key)));
                    result.Add(string.Format(CultureInfo.CurrentCulture, "The affected panels are of mixed types ({0}); the U-value is calculated for {1} ({2} heat flow).", types, heatFlowBasis.PanelType, heatFlowBasis.HeatFlowDirection.ToString().ToLowerInvariant()));
                }

                if (sameNameCount > 0)
                {
                    result.Add(string.Format(CultureInfo.CurrentCulture, "{0} other {1} also named {2}; {3} not changed.", sameNameCount, sameNameCount == 1 ? "construction is" : "constructions are", construction.Name, sameNameCount == 1 ? "it is" : "they are"));
                }

                return result;
            }
        }

        // ---- Preview --------------------------------------------------------------------------------------

        public UValuePreviewStatus Status => status;

        /// <summary>The reason when the preview cannot be applied (input, unreachable target with the best U, Tas failure).</summary>
        public string StatusMessage => statusMessage;

        /// <summary>True while an evaluation for the current inputs is running.</summary>
        public bool IsBusy => busy;

        /// <summary>The evaluation the preview shows (for the current inputs only).</summary>
        public UValueEvaluation Evaluation => evaluation;

        /// <summary>Calculated thickness of the varied layer [m]; NaN unless reached.</summary>
        public double CalculatedThickness => evaluation != null && evaluation.Reached ? evaluation.Thickness : double.NaN;

        /// <summary>Achieved U-value [W/m²K]; NaN unless reached.</summary>
        public double CalculatedThermalTransmittance => evaluation != null && evaluation.Reached ? evaluation.CalculatedThermalTransmittance : double.NaN;

        /// <summary>Target minus achieved U [W/m²K]: positive is better than the target.</summary>
        public double Margin => targetThermalTransmittance - CalculatedThermalTransmittance;

        /// <summary>For an unreachable target: the closest U the thickness range allows.</summary>
        public double BestAchievableThermalTransmittance => evaluation?.BestAchievableThermalTransmittance ?? double.NaN;

        /// <summary>U-value with the varied layer at the minimum thickness (the highest reachable U).</summary>
        public double MinThicknessThermalTransmittance => rangeKey == RangeKey(LayerIndex, minThickness, maxThickness, HeatFlowDirection, External) ? rangeMinThicknessThermalTransmittance : double.NaN;

        /// <summary>U-value with the varied layer at the maximum thickness (the lowest reachable U).</summary>
        public double MaxThicknessThermalTransmittance => rangeKey == RangeKey(LayerIndex, minThickness, maxThickness, HeatFlowDirection, External) ? rangeMaxThicknessThermalTransmittance : double.NaN;

        /// <summary>How long the last evaluation took on the worker [ms]; -1 before the first.</summary>
        public long LastEvaluationMilliseconds => lastEvaluationMilliseconds;

        /// <summary>Before/after layer table; the after column equals before until a result is reached.</summary>
        public IReadOnlyList<UValueLayerRow> PreviewRows
        {
            get
            {
                double thickness = CalculatedThickness;
                int layerIndex = evaluation != null && evaluation.Reached ? evaluation.LayerIndex : LayerIndex;
                return construction.ConstructionLayers.Select((x, i) =>
                {
                    double before = Millimetres(x?.Thickness ?? double.NaN);
                    bool adjusted = i == layerIndex;
                    double after = adjusted && !double.IsNaN(thickness) ? Millimetres(Core.Query.Round(thickness, Tolerance.MacroDistance)) : before;
                    return new UValueLayerRow(i, x?.Name, before, after, adjusted);
                }).ToList();
            }
        }

        /// <summary>Why a reached result still cannot be applied (scope / name conflicts); null when nothing blocks.</summary>
        public string ApplyBlockReason
        {
            get
            {
                if (applyMode == UValueApplyMode.ModifyInPlace && sameNameCount > 0)
                {
                    return string.Format(CultureInfo.CurrentCulture, "Keep name is unavailable: other constructions are also named {0} and would change too. Create a new construction instead.", construction.Name);
                }

                if (EffectiveScope == ThermalApplyScope.SelectedOnly && !scope.SelectedAvailable)
                {
                    return scope.SelectedUnavailableReason;
                }

                return null;
            }
        }

        /// <summary>True only while a reached result for the current inputs is shown and nothing blocks it.</summary>
        public bool ApplyEnabled => status == UValuePreviewStatus.Reached && !busy && ApplyBlockReason == null;

        /// <summary>The running evaluation (tests await it).</summary>
        internal Task LastEvaluationTask { get; private set; } = Task.CompletedTask;

        // ---- Commands -------------------------------------------------------------------------------------

        /// <summary>Calculates the current U-value and pre-fills the target with it (unless the user typed already).</summary>
        public Task InitializeAsync()
        {
            Restart();
            return LastEvaluationTask;
        }

        /// <summary>The change to apply, or null while <see cref="ApplyEnabled"/> is false.</summary>
        public SetUValueRequest CreateRequest()
        {
            if (!ApplyEnabled)
            {
                return null;
            }

            return new SetUValueRequest()
            {
                ConstructionGuid = construction.Guid,
                LayerIndex = evaluation.LayerIndex,
                Thickness = evaluation.Thickness,
                InitialThermalTransmittance = evaluation.InitialThermalTransmittance,
                CalculatedThermalTransmittance = evaluation.CalculatedThermalTransmittance,
                TargetThermalTransmittance = targetThermalTransmittance,
                HeatFlowDirection = evaluation.Request.HeatFlowDirection,
                Mode = applyMode,
                Scope = EffectiveScope,
                SelectedPanelGuids = scope.SelectedGuids.ToList(),
                NewConstructionName = applyMode == UValueApplyMode.NewConstruction ? NewConstructionName : null,
            };
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        // ---- Evaluation -----------------------------------------------------------------------------------

        private void SetTargetText(string value)
        {
            value = value ?? string.Empty;
            if (targetText == value)
            {
                return;
            }

            targetText = value;
            targetThermalTransmittance = ParseThermalTransmittance(value);
            OnPropertyChanged(nameof(TargetText));
            Restart();
        }

        private void UpdateBasis()
        {
            UValueHeatFlowBasis basis = Query.UValueHeatFlowBasis(BasisPanels(), construction);
            bool changed = basis.HeatFlowDirection != heatFlowBasis.HeatFlowDirection || basis.External != heatFlowBasis.External;
            heatFlowBasis = basis;

            if (changed && heatFlowDirectionOverride == null)
            {
                Restart();
            }
            else
            {
                OnDerivedPropertiesChanged();
            }
        }

        // Starts an evaluation for the current inputs, superseding any running one.
        private void Restart()
        {
            LastEvaluationTask = EvaluateAsync();
        }

        private async Task EvaluateAsync()
        {
            int version_Current = ++version;

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            evaluation = null;

            bool probeOnly = double.IsNaN(targetThermalTransmittance);
            if (probeOnly && targetEdited)
            {
                SetState(UValuePreviewStatus.None, "Enter a target U-value in W/m²K.", false);
                if (!double.IsNaN(currentThermalTransmittance))
                {
                    return;
                }
            }
            else if (!probeOnly && !double.IsNaN(currentThermalTransmittance) && Math.Abs(targetThermalTransmittance - currentThermalTransmittance) < NoChangeTolerance)
            {
                SetState(UValuePreviewStatus.NoChange, "The target equals the current U-value; there is nothing to change.", false);
                return;
            }
            else if (!(minThickness > 0) || !(maxThickness > minThickness))
            {
                SetState(UValuePreviewStatus.Failed, "The thickness range must run from a positive minimum to a larger maximum.", false);
                return;
            }
            else
            {
                SetState(probeOnly ? UValuePreviewStatus.None : UValuePreviewStatus.Calculating, null, true);
            }

            UValueEvaluationRequest request = new UValueEvaluationRequest(construction, materialLibrary, LayerIndex, probeOnly ? double.NaN : targetThermalTransmittance, HeatFlowDirection, External, minThickness, maxThickness);

            UValueEvaluation result;
            try
            {
                result = await evaluator.EvaluateAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Superseded: the newer evaluation owns the state.
                return;
            }
            catch (Exception exception)
            {
                if (version_Current == version)
                {
                    SetState(UValuePreviewStatus.Failed, exception.Message, false);
                }

                return;
            }

            if (version_Current != version)
            {
                // Stale: an input changed while this ran.
                return;
            }

            Apply(result, probeOnly);
        }

        private void Apply(UValueEvaluation result, bool probeOnly)
        {
            lastEvaluationMilliseconds = result?.ElapsedMilliseconds ?? -1;

            if (result?.Request != null && !double.IsNaN(result.MinThicknessThermalTransmittance) && !double.IsNaN(result.MaxThicknessThermalTransmittance))
            {
                UValueEvaluationRequest request = result.Request;
                rangeKey = RangeKey(request.LayerIndex, request.MinThickness, request.MaxThickness, request.HeatFlowDirection, request.External);
                rangeMinThicknessThermalTransmittance = result.MinThicknessThermalTransmittance;
                rangeMaxThicknessThermalTransmittance = result.MaxThicknessThermalTransmittance;
            }

            if (result != null && !double.IsNaN(result.InitialThermalTransmittance))
            {
                currentThermalTransmittance = result.InitialThermalTransmittance;
            }

            if (result == null)
            {
                SetState(UValuePreviewStatus.Failed, "The U-value could not be calculated.", false);
                return;
            }

            if (result.Failure != UValueCalculationFailure.None && result.Failure != UValueCalculationFailure.Unreachable)
            {
                evaluation = result;
                SetState(UValuePreviewStatus.Failed, result.Message, false);
                return;
            }

            if (probeOnly)
            {
                evaluation = result;
                if (!targetEdited && !double.IsNaN(currentThermalTransmittance))
                {
                    // Pre-fill the target with the current U (shown selected, so typing replaces it).
                    SetTargetText(currentThermalTransmittance.ToString("0.###", CultureInfo.CurrentCulture));
                    return;
                }

                SetState(targetEdited ? status : UValuePreviewStatus.None, statusMessage, false);
                return;
            }

            evaluation = result;
            if (result.Reached)
            {
                SetState(UValuePreviewStatus.Reached, null, false);
            }
            else
            {
                SetState(UValuePreviewStatus.Unreachable, result.Message, false);
            }
        }

        private void SetState(UValuePreviewStatus status, string message, bool busy)
        {
            this.status = status;
            statusMessage = message;
            this.busy = busy;
            OnDerivedPropertiesChanged();
        }

        // ---- Helpers --------------------------------------------------------------------------------------

        private IEnumerable<Panel> BasisPanels()
        {
            HashSet<Guid> basis = new HashSet<Guid>(scope.BasisGuids(EffectiveScope));
            return panels_Using.Where(x => basis.Contains(x.Guid));
        }

        private ConstructionLayer Layer(int index)
        {
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            return constructionLayers != null && index >= 0 && index < constructionLayers.Count ? constructionLayers[index] : null;
        }

        private static string RangeKey(int layerIndex, double minThickness, double maxThickness, HeatFlowDirection heatFlowDirection, bool external)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1:R}|{2:R}|{3}|{4}", layerIndex, minThickness, maxThickness, heatFlowDirection, external);
        }

        private static double ParseThermalTransmittance(string text)
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

            return value > 0 && !double.IsInfinity(value) ? value : double.NaN;
        }

        private static double Millimetres(double metres)
        {
            return double.IsNaN(metres) ? double.NaN : Math.Round(metres * 1000, 3);
        }

        private static string Panels(int count)
        {
            return count == 1 ? "panel" : "panels";
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
