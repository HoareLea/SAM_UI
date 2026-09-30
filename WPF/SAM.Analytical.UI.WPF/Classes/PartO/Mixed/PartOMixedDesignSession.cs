// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>Which rows the matrix shows.</summary>
    public enum PartOMixedDwellingFilter
    {
        All,

        /// <summary>Rows with a refusal, a constraint conflict, no selection, or anything else to resolve before building.</summary>
        NeedsAttention,

        /// <summary>Rows the current final mixed run failed.</summary>
        FailingFinal,

        /// <summary>Rows with no selected strategy.</summary>
        NotSelected,

        /// <summary>Rows whose applicable suggestion differs from the selection.</summary>
        SuggestionDiffers,
    }

    /// <summary>One change an assignment or <i>Apply suggestions</i> would make - shown before it is made.</summary>
    public sealed class PartOMixedSelectionChange
    {
        internal PartOMixedSelectionChange(PartOMixedDwellingRow partOMixedDwellingRow, PartODwellingStrategy? from, PartODwellingStrategy to, string? reason)
        {
            Row = partOMixedDwellingRow;
            From = from is null ? null : new PartODwellingStrategy(from);
            To = new PartODwellingStrategy(to);
            Reason = reason;
        }

        public PartOMixedDwellingRow Row { get; }

        public PartODwellingStrategy? From { get; }

        public PartODwellingStrategy To { get; }

        public string? Reason { get; }

        public string Dwelling => Row.Name;

        public string FromText => UI.Query.PartODwellingStrategyText(From);

        public string ToText => UI.Query.PartODwellingStrategyText(To);
    }

    /// <summary>The concise summary shown before the mixed design is built.</summary>
    public sealed class PartOMixedReadiness
    {
        public int DwellingCount { get; internal set; }

        public int Natural { get; internal set; }

        public int Mvhr { get; internal set; }

        public int Optimised { get; internal set; }

        /// <summary>MVHR and Optimised MVHR dwellings with active cooling on - counted beside them, never as a strategy of its own.</summary>
        public int Cooled { get; internal set; }

        public int NotSelected { get; internal set; }

        public int NeedsAttention { get; internal set; }

        /// <summary>What stops a build outright - the baseline is not clean, nothing is selected, the selection is unsaved and cannot be saved.</summary>
        public List<string> Blockers { get; } = [];

        public bool CanBuild => Blockers.Count == 0 && DwellingCount != 0 && NotSelected == 0 && NeedsAttention == 0;

        public string Text
        {
            get
            {
                List<string> parts = [UI.Query.PartOCount(DwellingCount, "dwelling", "dwellings")];

                parts.Add(string.Format("{0} Natural", Natural));
                parts.Add(string.Format("{0} MVHR", Mvhr));

                if (Optimised != 0)
                {
                    parts.Add(string.Format("{0} Optimised MVHR", Optimised));
                }

                if (Cooled != 0)
                {
                    parts.Add(string.Format("{0} with active cooling (whole building on the TAS Systems route)", Cooled));
                }

                if (NotSelected != 0)
                {
                    parts.Add(string.Format("{0} not selected", NotSelected));
                }

                if (NeedsAttention != 0)
                {
                    parts.Add(string.Format("{0} need{1} attention", NeedsAttention, NeedsAttention == 1 ? "s" : string.Empty));
                }

                return string.Join(" · ", parts);
            }
        }
    }

    /// <summary>
    /// Everything the mixed Part O design window shows and edits, without the window - so every rule it applies is
    /// testable, and the window only binds.
    ///
    /// <para><b>What it owns, and what it only reads</b></para>
    /// <list type="bullet">
    /// <item><b>Reads</b> the open model as the baseline: its dwellings (SAM's own dwelling rule,
    /// <c>Query.PartFDwellingZones</c>), its persisted strategy set, SAM's clean-baseline findings, and the catalogue.
    /// It never writes the model; <see cref="WithSelection"/> hands back a copy for the caller to adopt when a person
    /// saves.</item>
    /// <item><b>Owns</b> a draft of the selection - edited by row and in bulk, compared with the saved set for
    /// <see cref="IsDirty"/> - and the project constraints.</item>
    /// <item><b>Holds</b> the screening and final-run evidence of <see cref="PartOMixedDesignState"/>, re-validated
    /// against the baseline every time it is refreshed.</item>
    /// </list>
    /// </summary>
    public class PartOMixedDesignSession
    {
        private readonly List<PartOMixedDwellingRow> rows = [];
        private readonly Dictionary<Guid, PartOMixedDwellingRow> dictionary_Row = [];
        private readonly List<PartOMaterialisationRefusal> refusals = [];
        private readonly Dictionary<PartOScreeningStrategy, string> screeningStale = [];

        private AnalyticalModel analyticalModel;
        private string? fingerprint_Design;
        private string? fingerprint_Catalogue;
        private string? finalStale;

        //SAM's own staleness answer for the final run against the baseline - digesting the whole model, so asked when
        //the baseline or the final run changes, never on every edit. See ValidateFinal.
        private string? finalStale_Baseline;

        /// <param name="ventilationUnitTemplates">
        /// The catalogue's product templates - each product's manufacturer guidance, which is a cooled dwelling's cooling.
        /// Offered to SAM with the descriptors; null or empty and SAM refuses any cooled dwelling.
        /// </param>
        public PartOMixedDesignSession(AnalyticalModel analyticalModel_Baseline, string? path_Model, List<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, PartOMixedDesignState? partOMixedDesignState, List<VentilationUnitTemplate>? ventilationUnitTemplates = null)
        {
            analyticalModel = analyticalModel_Baseline ?? throw new ArgumentNullException(nameof(analyticalModel_Baseline));
            Path_Model = path_Model;
            Descriptors = ventilationUnitCapacityDescriptors ?? [];
            Templates = ventilationUnitTemplates ?? [];
            State = partOMixedDesignState ?? new PartOMixedDesignState();

            //Offer products where there are any - the catalogue's or the project's test unit, the Iteration 2 terms.
            //The final run of a design materialised without them is the Iteration 1a terms; which one the last run
            //used is on its evidence.
            catalogueOffered = CatalogueHasProducts && (State.FinalRun?.CatalogueOffered ?? true);

            BuildRows();
            Rebase(analyticalModel_Baseline);

            OpenedOnCleanBaseline = IsCleanBaseline;
        }

        /// <summary>
        /// Whether the model this session was opened on was a clean baseline. Only such a session keeps its state
        /// beside the model (<c>Modify.WritePartOMixedDesignState</c>): a refused model can never be built from -
        /// it is a run output, and nothing is cleaned back - so a sidecar beside it would hold nothing a mixed design
        /// can use, and would only drop a file into another workflow's folder.
        /// </summary>
        public bool OpenedOnCleanBaseline { get; }

        /// <summary>The baseline the session reads - the open model as last adopted.</summary>
        public AnalyticalModel Baseline => analyticalModel;

        public string? Path_Model { get; }

        public List<VentilationUnitCapacityDescriptor> Descriptors { get; }

        /// <summary>The catalogue's product templates - the manufacturer guidance SAM reads a cooled dwelling's cooling from.</summary>
        public List<VentilationUnitTemplate> Templates { get; }

        /// <summary>
        /// The templates as the next build offers them: with the catalogue, never without it (a cooled product must be
        /// selected against the catalogue in the same call - SAM's rule).
        /// </summary>
        public List<VentilationUnitTemplate>? TemplatesOffered => catalogueOffered ? Templates : null;

        /// <summary>
        /// Whether any product can be offered: a selectable catalogue product, or the project's test ventilation unit
        /// where SAM's product-selection rule makes it eligible (see <see cref="AllowedProducts"/>).
        /// </summary>
        public bool CatalogueHasProducts => UI.Query.PartOMixedProductsOffered(analyticalModel, Descriptors);

        private bool catalogueOffered;

        //Whether the baseline itself was edited in this session (an accepted design) and is not yet saved.
        private bool baselineEdited;

        //The baseline each previewed acceptance was answered against (weak: a preview never keeps a model alive).
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<PartODwellingDesignAcceptance, AnalyticalModel> acceptanceBases = new();

        /// <summary>
        /// Whether the final run offers the catalogue - products are then selected from the project's pool. A build
        /// input: changing it asks SAM again whether the final result still describes what would be built.
        /// </summary>
        public bool CatalogueOffered
        {
            get => catalogueOffered;
            set
            {
                if (catalogueOffered == value)
                {
                    return;
                }

                catalogueOffered = value;

                ValidateFinal();
                Refresh();
            }
        }

        private string? simulationCaseKey;

        /// <summary>
        /// The simulation case the next run would use (<c>Query.PartOSimulationCaseKey</c>): evidence simulated under another
        /// one is stale. Set by the window whenever the case changes; changing it re-validates.
        /// </summary>
        public string? SimulationCaseKey
        {
            get => simulationCaseKey;
            set
            {
                if (string.Equals(simulationCaseKey, value, StringComparison.Ordinal))
                {
                    return;
                }

                simulationCaseKey = value;

                ValidateFinal();
                Refresh();
            }
        }

        /// <summary>
        /// Asks again whether the saved final run still describes the model and its results file - after a run that did not
        /// complete, which may have rewritten that file.
        /// </summary>
        public void RevalidateFinal()
        {
            ValidateFinal();
            Refresh();
        }

        /// <summary>The catalogue as the next build offers it: <see cref="Descriptors"/> (possibly empty - the project test unit is SAM's to add), or null where products are not selected.</summary>
        public List<VentilationUnitCapacityDescriptor>? DescriptorsOffered => catalogueOffered ? Descriptors : null;

        public PartOMixedDesignState State { get; }

        public PartOMixedDesignConstraints Constraints => State.Constraints;

        public IReadOnlyList<PartOMixedDwellingRow> Rows => rows;

        public PartOMixedDwellingRow? Row(Guid guid_Zone) => dictionary_Row.TryGetValue(guid_Zone, out PartOMixedDwellingRow? row) ? row : null;

        /// <summary>Zones that are not dwellings. They are not strategy rows: SAM classifies and assesses common spaces itself.</summary>
        public int CommonZoneCount { get; private set; }

        /// <summary>SAM's reasons the open model is not a clean baseline - empty where it is one.</summary>
        public List<PartOMaterialisationRefusal> BaselineFindings { get; private set; } = [];

        public bool IsCleanBaseline => BaselineFindings.Count == 0;

        /// <summary>
        /// Whether the open model is a Part O result rather than a design model - <c>Query.PartODesignModelRefusal</c>
        /// over the findings above. Mixed Design already refuses it as an unclean baseline; this only lets the
        /// refusal say first that it is a result and that cases run from the design model (PR-4).
        /// </summary>
        public bool IsPartOResult => !IsCleanBaseline && UI.Query.PartODesignModelRefusal(analyticalModel, BaselineFindings) is not null;

        /// <summary>SAM's refusals from the last check or build, shown against their dwellings until the next one.</summary>
        public IReadOnlyList<PartOMaterialisationRefusal> Refusals => refusals;

        /// <summary>
        /// The products a dwelling may be given - SAM's rule, asked and never restated
        /// (<c>PartOEquipmentSelection.AllowedDescriptors</c>, the default selection where the project sets none, exactly as
        /// the materialisation reads it): the project test unit takes part only where the engineer ticked it into a
        /// selected pool or selects by hand, never under "all catalogue products".
        /// </summary>
        public List<VentilationUnitCapacityDescriptor> AllowedProducts => UI.Query.PartOMixedAllowedProducts(analyticalModel, Descriptors);

        /// <summary>A one-line description of the product pool the materialisation will select from.</summary>
        public string ProductPoolText
        {
            get
            {
                if (!CatalogueHasProducts)
                {
                    return "No selectable product is in the ventilation unit catalogue and the project sets no test unit, so MVHR units stay generic (Approved Document F duty only).";
                }

                PartOEquipmentSelection? partOEquipmentSelection = analyticalModel.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);
                int count = AllowedProducts.Count;

                return partOEquipmentSelection is null
                    ? string.Format("Automatic selection from all {0} catalogue products (no project pool is set).", count)
                    : string.Format("{0} · {1} permitted. Change the pool in Part O › Prepare & Run.", Core.Query.Description(partOEquipmentSelection.Mode), UI.Query.PartOCount(count, "product", "products"));
            }
        }

        // ---- The baseline ---------------------------------------------------------------------------------------

        /// <summary>
        /// Adopts the open model as the baseline - on opening, and after a person saved the selection onto it. Reloads
        /// the draft from the model's persisted set, so a row never shows a strategy the model does not carry unless it
        /// is marked as an unsaved edit.
        /// </summary>
        public void Rebase(AnalyticalModel analyticalModel_Baseline)
        {
            analyticalModel = analyticalModel_Baseline ?? throw new ArgumentNullException(nameof(analyticalModel_Baseline));
            baselineEdited = false;

            BaselineFindings = Analytical.Query.PartOBaselineFindings(analyticalModel) ?? [];

            PartODwellingStrategySet? partODwellingStrategySet = Saved;
            foreach (PartOMixedDwellingRow row in rows)
            {
                row.SetSelected(partODwellingStrategySet?.Strategy(row.ZoneGuid));
            }

            fingerprint_Design = UI.Query.PartOScreeningDesignFingerprint(analyticalModel);
            fingerprint_Catalogue = UI.Query.PartOMixedCatalogueFingerprint(analyticalModel, Descriptors);

            ValidateFinal();

            Refresh();
        }

        /// <summary>
        /// Asks SAM's materialisation record, and the results file, whether the saved final run still describes the
        /// baseline - the saved selection, the catalogue, the building. Fails closed.
        /// </summary>
        private void ValidateFinal()
        {
            finalStale_Baseline = null;

            PartOMixedRunEvidence? partOMixedRunEvidence = State.FinalRun;
            if (partOMixedRunEvidence is null)
            {
                return;
            }

            if (partOMixedRunEvidence.IsCurrent(analyticalModel, DescriptorsOffered, TemplatesOffered, out finalStale_Baseline))
            {
                //SAM's record and the results file agree; the simulation case must too.
                finalStale_Baseline = SimulationCaseStale(partOMixedRunEvidence.SimulationCaseKey) is string reason_Case ? string.Format("The mixed run {0}", reason_Case) : null;
            }
            else
            {
                finalStale_Baseline ??= "The saved mixed result no longer describes the model.";

                //SAM says the catalogue moved; where it is the setting that moved, say which way in the engineer's terms.
                if (partOMixedRunEvidence.CatalogueOffered != catalogueOffered && partOMixedRunEvidence.ReadRefusal is null)
                {
                    finalStale_Baseline = partOMixedRunEvidence.CatalogueOffered
                        ? "The mixed run selected MVHR products from the catalogue, and products are no longer selected from it, so the next build would use generic units. Build and run again."
                        : "The mixed run used generic MVHR units, and products are now selected from the catalogue, so the next build would select products. Build and run again.";
                }
            }
        }

        /// <summary>Why evidence simulated under <paramref name="simulationCaseKey_Evidence"/> is not of the current case, or null.</summary>
        private string? SimulationCaseStale(string? simulationCaseKey_Evidence)
        {
            if (string.IsNullOrWhiteSpace(simulationCaseKey_Evidence))
            {
                return "does not record the simulation case it was run under, so it cannot be shown as current. Run again.";
            }

            if (simulationCaseKey is null)
            {
                return "cannot be confirmed: no simulation case is selected.";
            }

            return string.Equals(simulationCaseKey_Evidence, simulationCaseKey, StringComparison.Ordinal)
                ? null
                : "was simulated under a different simulation case (weather or solar method), so it does not describe the case selected now. Run again.";
        }

        /// <summary>Whether screening evidence is current: SAM's fingerprints, then the simulation case.</summary>
        private bool ScreeningCurrent(PartOScreeningEvidence partOScreeningEvidence, out string? reason)
        {
            if (!partOScreeningEvidence.IsCurrent(fingerprint_Design, fingerprint_Catalogue, out reason))
            {
                return false;
            }

            if (SimulationCaseStale(partOScreeningEvidence.SimulationCaseKey) is string reason_Case)
            {
                reason = string.Format("The screening {0}", reason_Case);
                return false;
            }

            return true;
        }

        /// <summary>The strategy set persisted on the model, or null where there is none (a legacy model).</summary>
        public PartODwellingStrategySet? Saved => analyticalModel.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies);

        /// <summary>The draft selection, as a SAM strategy set.</summary>
        public PartODwellingStrategySet Draft
        {
            get
            {
                PartODwellingStrategySet result = new();
                foreach (PartOMixedDwellingRow row in rows)
                {
                    if (row.Selected is PartODwellingStrategy partODwellingStrategy)
                    {
                        result.Set(partODwellingStrategy);
                    }
                }

                return result;
            }
        }

        /// <summary>Whether the draft differs from what the model carries. Compared as SAM's canonical serialisation.</summary>
        public bool IsDirty
        {
            get
            {
                //An accepted design is a change of the baseline itself, not only of the selection.
                if (baselineEdited)
                {
                    return true;
                }

                PartODwellingStrategySet? saved = Saved;
                PartODwellingStrategySet draft = Draft;

                if (saved is null)
                {
                    return draft.Count != 0;
                }

                return saved.ToJsonObject()?.ToJsonString() != draft.ToJsonObject()?.ToJsonString();
            }
        }

        /// <summary>
        /// A copy of the baseline carrying the draft selection - what the caller adopts as the open model when a person
        /// saves. The baseline itself is not written.
        /// </summary>
        public AnalyticalModel WithSelection()
        {
            AnalyticalModel result = new(analyticalModel);
            result.SetValue(Analytical.AnalyticalModelParameter.PartODwellingStrategies, Draft);

            return result;
        }

        // ---- Editing ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Select natural ventilation for these dwellings. Refused whole where the project requires mechanical ventilation,
        /// and where a dwelling has active cooling on: its cooling is on the MVHR supply, so it is turned off explicitly
        /// first rather than dropped unseen.
        /// </summary>
        public string? SetNatural(IEnumerable<PartOMixedDwellingRow> rows_Selected)
        {
            List<PartOMixedDwellingRow> rows_Temp = [.. rows_Selected ?? []];

            List<string> names_Cooled = [.. rows_Temp.Where(x => x.Selected?.ActiveCooling == PartOActiveCooling.SupplyAirCooling).Select(x => x.Name)];
            if (names_Cooled.Count != 0)
            {
                return string.Format("Turn active cooling off first for {0}: a naturally ventilated dwelling has no mechanical supply to cool.", Names(names_Cooled));
            }

            return Assign(rows_Temp, row => new PartODwellingStrategy(row.ZoneGuid, PartOVentilationMode.NaturalVentilation));
        }

        /// <summary>
        /// Select MVHR with this product - or null for automatic selection from the project's pool. Cooling is orthogonal:
        /// a dwelling already MVHR keeps its active cooling setting; one that was not starts with cooling off.
        /// </summary>
        public string? SetMvhr(IEnumerable<PartOMixedDwellingRow> rows_Selected, VentilationUnitReference? ventilationUnitReference)
        {
            if (ventilationUnitReference is not null && !AllowedProducts.Any(x => SameProduct(x.VentilationUnitReference, ventilationUnitReference)))
            {
                return string.Format("{0} is not in the project's permitted product pool, so it cannot be selected.", ventilationUnitReference);
            }

            return Assign(rows_Selected, row => new PartODwellingStrategy(row.ZoneGuid, PartOVentilationMode.MVHR, ventilationUnitReference, Cooling(row)));
        }

        /// <summary>
        /// Active cooling on or off for these dwellings - intent only, orthogonal to their ventilation strategy: nothing
        /// else about the selection changes, and no cooling figure is stored (a cooled dwelling's cooling is its selected
        /// product's manufacturer guidance, which SAM applies and refuses where it cannot).
        /// <para>
        /// On is refused whole for a dwelling that is not mechanically ventilated - the only cooling path is the MVHR
        /// supply - and where the project does not allow cooling. Off is always allowed and leaves a natural or unselected
        /// dwelling as it is.
        /// </para>
        /// </summary>
        public string? SetCooling(IEnumerable<PartOMixedDwellingRow> rows_Selected, bool on)
        {
            List<PartOMixedDwellingRow> rows_Temp = [.. rows_Selected ?? []];
            if (rows_Temp.Count == 0)
            {
                return "Select one or more dwellings first.";
            }

            if (!on)
            {
                //Never blocked by another project rule: taking cooling away only ever removes intent.
                List<PartOMixedDwellingRow> rows_Cooled = rows_Temp.FindAll(x => x.Selected?.ActiveCooling == PartOActiveCooling.SupplyAirCooling);
                foreach (PartOMixedDwellingRow row in rows_Cooled)
                {
                    row.SetSelected(new PartODwellingStrategy(row.Selected!) { ActiveCooling = PartOActiveCooling.None });
                }

                Edited(rows_Cooled);
                Refresh();

                return null;
            }

            List<string> names_NotMvhr = [.. rows_Temp.Where(x => x.Selected?.VentilationMode != PartOVentilationMode.MVHR).Select(x => x.Name)];
            if (names_NotMvhr.Count != 0)
            {
                return string.Format("Active cooling is on the MVHR supply: select MVHR (or Optimised MVHR) first for {0}.", Names(names_NotMvhr));
            }

            return Assign(rows_Temp, row => new PartODwellingStrategy(row.Selected!) { ActiveCooling = PartOActiveCooling.SupplyAirCooling });
        }

        /// <summary>The row's active cooling where it stays MVHR - cooling is orthogonal to a change of product or airflow basis.</summary>
        private static PartOActiveCooling Cooling(PartOMixedDwellingRow row)
        {
            PartODwellingStrategy? partODwellingStrategy = row.Selected;

            return partODwellingStrategy?.VentilationMode == PartOVentilationMode.MVHR && partODwellingStrategy.ActiveCooling == PartOActiveCooling.SupplyAirCooling ? PartOActiveCooling.SupplyAirCooling : PartOActiveCooling.None;
        }

        /// <summary>
        /// Retain the design airflow the baseline's terminals already carry, for these MVHR dwellings: SAM's dwelling
        /// design fingerprint is recorded as the guard, and nothing else - no airflow is copied into the strategy.
        /// Refused where a dwelling's baseline carries no design terminals, where it is not MVHR, or where the project
        /// does not allow an optimised design.
        /// </summary>
        public string? SetRetainedDesign(IEnumerable<PartOMixedDwellingRow> rows_Selected)
        {
            List<PartOMixedDwellingRow> rows_Temp = [.. rows_Selected ?? []];

            List<string> names_NoTerminals = [];
            List<string> names_NotMvhr = [];
            Dictionary<Guid, string> fingerprints = [];

            Dictionary<Guid, Zone> dictionary_Zone = Zones();

            foreach (PartOMixedDwellingRow row in rows_Temp)
            {
                if (row.Selected?.VentilationMode != PartOVentilationMode.MVHR)
                {
                    names_NotMvhr.Add(row.Name);
                    continue;
                }

                string? fingerprint = dictionary_Zone.TryGetValue(row.ZoneGuid, out Zone? zone) && HasDesignTerminals(zone)
                    ? analyticalModel.AdjacencyCluster.PartODwellingDesignFingerprint(zone)
                    : null;

                if (fingerprint is null)
                {
                    names_NoTerminals.Add(row.Name);
                    continue;
                }

                fingerprints[row.ZoneGuid] = fingerprint;
            }

            if (names_NotMvhr.Count != 0)
            {
                return string.Format("A retained design is an MVHR design: select MVHR first for {0}.", Names(names_NotMvhr));
            }

            if (names_NoTerminals.Count != 0)
            {
                return string.Format("The baseline carries no design ventilation terminals for {0}, so there is no design airflow to retain.", Names(names_NoTerminals));
            }

            return Assign(rows_Temp, row => new PartODwellingStrategy(row.Selected!) { DesignAirFlowBasis = PartODesignAirFlowBasis.RetainedDesign, DesignFingerprint = fingerprints[row.ZoneGuid] });
        }

        /// <summary>
        /// Asks SAM to accept one dwelling's design airflow from <paramref name="analyticalModel_Source"/> - a completed
        /// Iteration 2B result - onto this baseline (<c>Modify.AcceptPartODwellingDesign</c>: lineage, terminals, the
        /// Approved Document F floor are all SAM's). Nothing changes here: the answer is what a person confirms.
        /// </summary>
        public PartODwellingDesignAcceptance PreviewAcceptDesign(PartOMixedDwellingRow row, AnalyticalModel? analyticalModel_Source)
        {
            PartODwellingDesignAcceptance result = Analytical.Modify.AcceptPartODwellingDesign(analyticalModel, row?.ZoneGuid ?? Guid.Empty, analyticalModel_Source);

            //Which baseline it was answered against - adopting it later over a baseline edited since would drop that edit.
            acceptanceBases.AddOrUpdate(result, analyticalModel);

            return result;
        }

        /// <summary>
        /// Adopts an accepted design a person confirmed: the baseline becomes SAM's accepted model (only that dwelling's
        /// design terminals differ) and the dwelling is selected as Optimised MVHR - MVHR with a retained design, guarded
        /// by SAM's fingerprint and carrying no airflow. A pending edit of the baseline until the selection is saved; the
        /// whole design is rebuilt from the baseline at the next build.
        /// </summary>
        /// <returns>Why it was not adopted, or null.</returns>
        public string? AcceptDesign(PartOMixedDwellingRow row, PartODwellingDesignAcceptance partODwellingDesignAcceptance)
        {
            if (row is null || partODwellingDesignAcceptance is null || !partODwellingDesignAcceptance.IsAccepted || partODwellingDesignAcceptance.ZoneGuid != row.ZoneGuid)
            {
                return partODwellingDesignAcceptance?.Refusal ?? "There is no accepted design for this dwelling.";
            }

            if (!acceptanceBases.TryGetValue(partODwellingDesignAcceptance, out AnalyticalModel? analyticalModel_Base) || !ReferenceEquals(analyticalModel_Base, analyticalModel))
            {
                return string.Format("The accepted design for {0} was prepared against an earlier state of the baseline, so adopting it would undo the changes made since. Accept it again.", row.Name);
            }

            //A product already chosen for the dwelling is kept; otherwise the unit is selected from the project's pool.
            VentilationUnitReference? ventilationUnitReference = row.Selected?.VentilationMode == PartOVentilationMode.MVHR ? row.Selected.VentilationUnitReference : null;
            PartODwellingStrategy partODwellingStrategy = new(row.ZoneGuid, PartOVentilationMode.MVHR, ventilationUnitReference, Cooling(row), PartODesignAirFlowBasis.RetainedDesign, partODwellingDesignAcceptance.DesignFingerprint);

            string? refusal = Constraints.Refusal(partODwellingStrategy);
            if (refusal is not null)
            {
                return refusal;
            }

            analyticalModel = partODwellingDesignAcceptance.AnalyticalModel;
            baselineEdited = true;

            BaselineFindings = Analytical.Query.PartOBaselineFindings(analyticalModel) ?? [];
            fingerprint_Design = UI.Query.PartOScreeningDesignFingerprint(analyticalModel);
            fingerprint_Catalogue = UI.Query.PartOMixedCatalogueFingerprint(analyticalModel, Descriptors);
            ValidateFinal();

            row.SetSelected(partODwellingStrategy);

            Edited([row]);
            Refresh();

            return null;
        }

        /// <summary>Removes the selection. An unselected dwelling is never built as natural ventilation - SAM refuses it.</summary>
        public void Clear(IEnumerable<PartOMixedDwellingRow> rows_Selected)
        {
            List<PartOMixedDwellingRow> rows_Temp = [.. rows_Selected ?? []];
            foreach (PartOMixedDwellingRow row in rows_Temp)
            {
                row.SetSelected(null);
            }

            Edited(rows_Temp);
            Refresh();
        }

        /// <summary>
        /// The changes <i>Apply suggestions</i> would make to these rows - only rows with an applicable suggestion that
        /// differs from their selection, and only where the project constraints permit it. Nothing is changed.
        /// </summary>
        public List<PartOMixedSelectionChange> SuggestionChanges(IEnumerable<PartOMixedDwellingRow> rows_Selected)
        {
            List<PartOMixedSelectionChange> result = [];

            foreach (PartOMixedDwellingRow row in rows_Selected ?? [])
            {
                PartODwellingSuggestion? partODwellingSuggestion = row.Suggestion;
                if (partODwellingSuggestion?.DwellingStrategy is not PartODwellingStrategy partODwellingStrategy || !row.SuggestionDiffers)
                {
                    continue;
                }

                if (Constraints.Refusal(partODwellingStrategy) is not null)
                {
                    continue;
                }

                result.Add(new PartOMixedSelectionChange(row, row.Selected, partODwellingStrategy, partODwellingSuggestion.Reason));
            }

            return result;
        }

        /// <summary>Makes changes a person has seen and accepted. The only path from a suggestion into the selection.</summary>
        public void Apply(IEnumerable<PartOMixedSelectionChange> changes)
        {
            List<PartOMixedSelectionChange> changes_Temp = [.. changes ?? []];
            foreach (PartOMixedSelectionChange change in changes_Temp)
            {
                change.Row.SetSelected(new PartODwellingStrategy(change.To) { ZoneGuid = change.Row.ZoneGuid });
            }

            Edited(changes_Temp.Select(x => x.Row));
            Refresh();
        }

        private string? Assign(IEnumerable<PartOMixedDwellingRow> rows_Selected, Func<PartOMixedDwellingRow, PartODwellingStrategy> func)
        {
            List<PartOMixedDwellingRow> rows_Temp = [.. rows_Selected ?? []];
            if (rows_Temp.Count == 0)
            {
                return "Select one or more dwellings first.";
            }

            List<(PartOMixedDwellingRow Row, PartODwellingStrategy Strategy)> assignments = [];
            foreach (PartOMixedDwellingRow row in rows_Temp)
            {
                PartODwellingStrategy partODwellingStrategy = func(row);

                //The project rule, once for the whole assignment: nothing is half-applied.
                string? refusal = Constraints.Refusal(partODwellingStrategy);
                if (refusal is not null)
                {
                    return refusal;
                }

                assignments.Add((row, partODwellingStrategy));
            }

            foreach ((PartOMixedDwellingRow row, PartODwellingStrategy partODwellingStrategy) in assignments)
            {
                row.SetSelected(partODwellingStrategy);
            }

            Edited(rows_Temp);
            Refresh();

            return null;
        }

        /// <summary>
        /// A dwelling whose selection a person has just changed no longer carries the refusal SAM gave its PREVIOUS
        /// selection: it is answered again by the next check or build. Refusals naming no dwelling, and other
        /// dwellings' refusals, stay until then.
        /// </summary>
        private void Edited(IEnumerable<PartOMixedDwellingRow> rows_Edited)
        {
            HashSet<Guid> guids = [.. rows_Edited.Select(x => x.ZoneGuid)];
            refusals.RemoveAll(x => x.ZoneGuid is Guid guid && guids.Contains(guid));
        }

        // ---- Evidence ---------------------------------------------------------------------------------------------

        /// <summary>Records a screening's evidence - replacing each screened strategy's previous evidence - and refreshes.</summary>
        public void ApplyScreening(IEnumerable<PartOScreeningEvidence> evidence)
        {
            foreach (PartOScreeningEvidence partOScreeningEvidence in evidence ?? [])
            {
                State.SetScreeningEvidence(partOScreeningEvidence);
            }

            Refresh();
        }

        /// <summary>Records a completed mixed run as the final result, and refreshes.</summary>
        public void ApplyFinal(PartOMixedRunEvidence? partOMixedRunEvidence)
        {
            if (partOMixedRunEvidence is not null)
            {
                State.FinalRun = partOMixedRunEvidence;
            }

            ValidateFinal();

            Refresh();
        }

        /// <summary>SAM's structured refusals from a check or a build, attached to the dwellings they name.</summary>
        public void SetRefusals(IEnumerable<PartOMaterialisationRefusal>? partOMaterialisationRefusals)
        {
            refusals.Clear();
            refusals.AddRange(partOMaterialisationRefusals ?? []);

            Refresh();
        }

        /// <summary>Why this strategy's screening evidence is stale, or null where it is current or absent.</summary>
        public string? ScreeningStale(PartOScreeningStrategy partOScreeningStrategy) => screeningStale.TryGetValue(partOScreeningStrategy, out string? reason) ? reason : null;

        /// <summary>The screening evidence that is current for this baseline and catalogue.</summary>
        public List<PartOScreeningEvidence> CurrentScreening()
        {
            List<PartOScreeningEvidence> result = [];
            foreach (PartOScreeningEvidence partOScreeningEvidence in State.Screening)
            {
                if (ScreeningCurrent(partOScreeningEvidence, out _))
                {
                    result.Add(partOScreeningEvidence);
                }
            }

            return result;
        }

        /// <summary>Whether the saved final mixed run still describes the baseline, the saved selection and the catalogue.</summary>
        public bool FinalCurrent => State.FinalRun is not null && finalStale is null;

        /// <summary>Why the final mixed run is not current, or null.</summary>
        public string? FinalStale => finalStale;

        /// <summary>The final result in one line, or null where no mixed run has completed.</summary>
        public string? FinalText
        {
            get
            {
                PartOMixedRunEvidence? partOMixedRunEvidence = State.FinalRun;
                if (partOMixedRunEvidence is null)
                {
                    return null;
                }

                string counts = string.Format("{0} pass · {1} fail · {2} not assessed",
                    UI.Query.PartOCount(partOMixedRunEvidence.Count(PartODwellingOutcome.Pass), "dwelling", "dwellings"),
                    partOMixedRunEvidence.Count(PartODwellingOutcome.Fail),
                    partOMixedRunEvidence.Count(PartODwellingOutcome.NotAssessed));

                string when = partOMixedRunEvidence.CreatedUtc.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

                string? corridor = partOMixedRunEvidence.CorridorText;
                if (corridor is not null)
                {
                    counts = string.Format("{0} · {1}", counts, corridor);
                }

                if (partOMixedRunEvidence.Route == PartOSimulationRoute.Systems)
                {
                    counts = string.Format("{0} · TAS Systems route, {1}", counts, UI.Query.PartOCount(partOMixedRunEvidence.Record?.CooledDwellings.Count ?? 0, "dwelling cooled", "dwellings cooled"));
                }

                return FinalCurrent
                    ? string.Format("Final mixed run: {0} — {1} ({2}).", Core.Query.Description(partOMixedRunEvidence.Overall), counts, when)
                    : string.Format("Previous mixed run ({0}) is STALE and is not the current result: {1}", when, finalStale);
            }
        }

        public PartOMixedReadiness Readiness()
        {
            PartOMixedReadiness result = new() { DwellingCount = rows.Count };

            if (!IsCleanBaseline)
            {
                result.Blockers.Add(string.Format("{0}The open model is not a clean Part O baseline ({1}). Save a cleaned copy with Results > Part O > Remove Results and open it, or reopen the pre-Part-O model.", IsPartOResult ? UI.Query.PartODesignModelRefusal_Lead + " " + (UI.Query.PartODerivedFromSentence(analyticalModel, Path_Model) is string derivedFrom ? derivedFrom + " " : string.Empty) : string.Empty, string.Join("; ", BaselineFindings.Select(x => Core.Query.Description(x.Reason)).Distinct())));
            }

            if (rows.Count == 0)
            {
                result.Blockers.Add("The model has no dwelling zones. Mark the dwelling zones with 'Is Dwelling' first.");
            }

            foreach (PartOMixedDwellingRow row in rows)
            {
                PartODwellingStrategy? partODwellingStrategy = row.Selected;
                if (partODwellingStrategy is null)
                {
                    result.NotSelected++;
                }
                else if (partODwellingStrategy.VentilationMode == PartOVentilationMode.NaturalVentilation)
                {
                    result.Natural++;
                }
                else if (partODwellingStrategy.DesignAirFlowBasis == PartODesignAirFlowBasis.RetainedDesign)
                {
                    result.Optimised++;
                }
                else
                {
                    result.Mvhr++;
                }

                if (partODwellingStrategy?.ActiveCooling == PartOActiveCooling.SupplyAirCooling)
                {
                    result.Cooled++;
                }

                if (row.NeedsAttention)
                {
                    result.NeedsAttention++;
                }
            }

            return result;
        }

        /// <summary>Whether a row is shown under this filter and search text (name or group, case-insensitive).</summary>
        public static bool Matches(PartOMixedDwellingRow row, PartOMixedDwellingFilter partOMixedDwellingFilter, string? text)
        {
            if (row is null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                string search = text!.Trim();
                if (row.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0 && row.Group.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return partOMixedDwellingFilter switch
            {
                PartOMixedDwellingFilter.NeedsAttention => row.NeedsAttention,
                PartOMixedDwellingFilter.FailingFinal => row.FinalFail,
                PartOMixedDwellingFilter.NotSelected => !row.HasSelection,
                PartOMixedDwellingFilter.SuggestionDiffers => row.SuggestionDiffers,
                _ => true,
            };
        }

        /// <summary>
        /// Recomputes every derived cell - screening, suggestion, final, attention - from the evidence, the constraints
        /// and the current selection. Nothing derived is stored between refreshes.
        /// </summary>
        public void Refresh()
        {
            // ---- Screening: current evidence per strategy, or STALE ----------------------------------------------

            screeningStale.Clear();

            List<PartOScreeningEvidence> evidence_Current = [];
            Dictionary<PartOScreeningStrategy, PartOScreeningEvidence> dictionary_Evidence = [];

            foreach (PartOScreeningEvidence partOScreeningEvidence in State.Screening)
            {
                if (ScreeningCurrent(partOScreeningEvidence, out string? reason))
                {
                    evidence_Current.Add(partOScreeningEvidence);
                    dictionary_Evidence[partOScreeningEvidence.Strategy] = partOScreeningEvidence;
                }
                else
                {
                    screeningStale[partOScreeningEvidence.Strategy] = reason ?? "The screening evidence is not current.";
                }
            }

            // ---- Final: SAM's record against the baseline (validated on rebase), then the draft ---------------------

            PartOMixedRunEvidence? partOMixedRunEvidence = State.FinalRun;
            finalStale = finalStale_Baseline;

            if (partOMixedRunEvidence is not null && finalStale is null && IsDirty)
            {
                //The saved selection still matches the run, but the draft does not: the run is the saved design's result,
                //not the design on screen.
                finalStale = "The selection has been edited since the mixed design was run. Build and run again for a result of the edited design.";
            }

            // ---- Refusals by dwelling -------------------------------------------------------------------------------

            Dictionary<Guid, List<string>> dictionary_Refusal = [];
            foreach (PartOMaterialisationRefusal partOMaterialisationRefusal in refusals)
            {
                if (partOMaterialisationRefusal.ZoneGuid is Guid guid_Zone && dictionary_Row.ContainsKey(guid_Zone))
                {
                    if (!dictionary_Refusal.TryGetValue(guid_Zone, out List<string>? messages))
                    {
                        messages = [];
                        dictionary_Refusal[guid_Zone] = messages;
                    }

                    messages.Add(string.Format("{0}: {1}", Core.Query.Description(partOMaterialisationRefusal.Reason), partOMaterialisationRefusal.Message));
                }
            }

            bool catalogueHasProducts = CatalogueHasProducts;
            List<VentilationUnitCapacityDescriptor> allowedProducts = catalogueOffered && rows.Any(x => x.Selected?.VentilationUnitReference is not null) ? AllowedProducts : [];

            foreach (PartOMixedDwellingRow row in rows)
            {
                foreach (PartOScreeningStrategy partOScreeningStrategy in UI.Query.PartOScreeningStrategies())
                {
                    string text;
                    if (dictionary_Evidence.TryGetValue(partOScreeningStrategy, out PartOScreeningEvidence? partOScreeningEvidence))
                    {
                        text = Core.Query.Description(partOScreeningEvidence.Outcome(row.ZoneGuid));
                    }
                    else if (screeningStale.ContainsKey(partOScreeningStrategy))
                    {
                        text = "STALE";
                    }
                    else if (UI.Query.PartOScreeningStrategyUnavailable(partOScreeningStrategy, catalogueHasProducts) is not null)
                    {
                        text = Core.Query.Description(PartODwellingOutcome.Unavailable);
                    }
                    else
                    {
                        text = Core.Query.Description(PartODwellingOutcome.NotRun);
                    }

                    row.SetScreening(partOScreeningStrategy, text);
                }

                row.ScreeningChanged();

                row.SetSuggestion(UI.Query.PartODwellingSuggestion(row.ZoneGuid, evidence_Current, Constraints, catalogueOffered));

                PartODwellingResult? partODwellingResult = partOMixedRunEvidence?.Result(row.ZoneGuid);
                PartODwellingStrategy? partODwellingStrategy_Ran = partOMixedRunEvidence?.Strategies?.Strategy(row.ZoneGuid);
                row.SetFinal(partODwellingResult, partODwellingResult is not null && finalStale is null, partODwellingResult is null ? null : UI.Query.PartODwellingStrategyText(partODwellingStrategy_Ran));

                List<string> attention = [];
                if (dictionary_Refusal.TryGetValue(row.ZoneGuid, out List<string>? messages_Row))
                {
                    attention.AddRange(messages_Row);
                }

                string? refusal_Constraint = Constraints.Refusal(row.Selected);
                if (refusal_Constraint is not null)
                {
                    attention.Add(refusal_Constraint);
                }

                //A product chosen earlier that cannot be honoured now - products no longer offered, or the product no longer in
                //the project's permitted pool: flagged, never rewritten.
                if (row.Selected?.VentilationUnitReference is VentilationUnitReference ventilationUnitReference)
                {
                    if (!catalogueOffered)
                    {
                        attention.Add(string.Format("{0} is selected, but MVHR products are not selected from the catalogue now, so it cannot be built. Offer the catalogue again, or choose MVHR with an automatic (generic) unit.", ventilationUnitReference));
                    }
                    else if (!allowedProducts.Any(x => SameProduct(x.VentilationUnitReference, ventilationUnitReference)))
                    {
                        attention.Add(string.Format("{0} is selected, but it is not in the project's permitted product pool now, so it cannot be built. Choose a permitted product or automatic selection.", ventilationUnitReference));
                    }
                }

                row.SetAttention(attention.Count == 0 ? null : string.Join(" ", attention));
            }
        }

        // ---- Construction -----------------------------------------------------------------------------------------

        private void BuildRows()
        {
            rows.Clear();
            dictionary_Row.Clear();

            AdjacencyCluster? adjacencyCluster = analyticalModel.AdjacencyCluster;
            List<Zone> zones = adjacencyCluster?.GetZones() ?? [];
            zones.RemoveAll(x => x is null);

            //SAM's one dwelling rule - exactly the dwellings the materialisation will assess. Common spaces are never
            //strategy rows; SAM includes an assessed communal corridor automatically.
            List<Zone> zones_Dwelling = zones.PartFDwellingZones() ?? [];

            HashSet<Guid> guids_Dwelling = [.. zones_Dwelling.Select(x => x.Guid)];
            CommonZoneCount = zones.Count(x => !guids_Dwelling.Contains(x.Guid));

            zones_Dwelling.Sort((x, y) =>
            {
                int compare = NaturalCompare(x.Name, y.Name);
                return compare != 0 ? compare : x.Guid.CompareTo(y.Guid);
            });

            foreach (Zone zone in zones_Dwelling)
            {
                string group = zone.TryGetValue(ZoneParameter.ZoneCategory, out string category) && !string.IsNullOrWhiteSpace(category) ? category : "Dwellings";
                int spaceCount = adjacencyCluster!.GetRelatedObjects<Space>(zone)?.Count ?? 0;

                PartOMixedDwellingRow row = new(zone.Guid, string.IsNullOrWhiteSpace(zone.Name) ? zone.Guid.ToString() : zone.Name, group, spaceCount, false);
                rows.Add(row);
                dictionary_Row[zone.Guid] = row;
            }
        }

        private Dictionary<Guid, Zone> Zones()
        {
            Dictionary<Guid, Zone> result = [];
            foreach (Zone zone in analyticalModel.AdjacencyCluster?.GetZones() ?? [])
            {
                if (zone is not null)
                {
                    result[zone.Guid] = zone;
                }
            }

            return result;
        }

        private bool HasDesignTerminals(Zone zone)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
            {
                if (space is not null && (adjacencyCluster.VentilationTerminals(space)?.Count ?? 0) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SameProduct(VentilationUnitReference? x, VentilationUnitReference? y)
        {
            return x is not null && y is not null
                && string.Equals(x.Manufacturer, y.Manufacturer, StringComparison.Ordinal)
                && string.Equals(x.Model, y.Model, StringComparison.Ordinal)
                && string.Equals(x.Reference, y.Reference, StringComparison.Ordinal);
        }

        private static string Names(List<string> names)
        {
            const int count_Shown = 5;

            return names.Count <= count_Shown
                ? string.Join(", ", names)
                : string.Format("{0} and {1} more", string.Join(", ", names.Take(count_Shown)), names.Count - count_Shown);
        }

        /// <summary>"Flat 2" before "Flat 10": digit runs compared as numbers.</summary>
        internal static int NaturalCompare(string? x, string? y)
        {
            x ??= string.Empty;
            y ??= string.Empty;

            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                {
                    int i_End = i, j_End = j;
                    while (i_End < x.Length && char.IsDigit(x[i_End])) i_End++;
                    while (j_End < y.Length && char.IsDigit(y[j_End])) j_End++;

                    string digits_X = x.Substring(i, i_End - i).TrimStart('0');
                    string digits_Y = y.Substring(j, j_End - j).TrimStart('0');

                    int compare = digits_X.Length != digits_Y.Length ? digits_X.Length.CompareTo(digits_Y.Length) : string.CompareOrdinal(digits_X, digits_Y);
                    if (compare != 0)
                    {
                        return compare;
                    }

                    i = i_End;
                    j = j_End;
                    continue;
                }

                int compare_Char = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (compare_Char != 0)
                {
                    return compare_Char;
                }

                i++;
                j++;
            }

            return (x.Length - i).CompareTo(y.Length - j);
        }
    }
}
