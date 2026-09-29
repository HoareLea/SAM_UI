// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Prepares an Approved Document O base iteration over the model's dwelling zones, shows what it
        /// produced, and - on OK - adopts the prepared model and starts the session's Part O run.
        /// <para>
        /// <b>Orchestration only.</b> The engineering is one call:
        /// <c>SAM.Analytical.Modify.PreparePartOIteration</c>. This method chooses nothing it could get from
        /// there - not the dwelling scope (<c>Query.PartFDwellingZones</c>), not the route the iteration is
        /// defined over (<c>Query.PartOIterationVentilationMode</c>, through
        /// <see cref="PartOVentilationStrategyOption"/>), and above all not the ventilation unit. The
        /// catalogue is passed in as descriptors and the smallest-capable-unit rule stays inside the
        /// preparation, run per dwelling against the realized terminal network's duty. Selecting a product
        /// never writes a design airflow.
        /// </para>
        /// <para>
        /// <b>The run is started only after the model is adopted</b>, and
        /// <see cref="PartORun.ExpectModification"/> is armed immediately before that write so the run's own
        /// change is not read as somebody else's edit. Everything else that replaces the model between here
        /// and a completed workflow drops the run - see <see cref="PartORun"/>.
        /// </para>
        /// </summary>
        /// <param name="uIAnalyticalModel">The loaded model. Not modified unless the user accepts.</param>
        /// <param name="partORun">The session's Part O run, which this command moves to Prepared.</param>
        /// <param name="owner">Owner window for the dialogs.</param>
        public static void PreparePartOIteration(this UIAnalyticalModel? uIAnalyticalModel, PartORun partORun, IWin32Window? owner = null)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel is null || partORun is null)
            {
                return;
            }

            List<Zone> zones = analyticalModel.GetZones() ?? [];
            if (zones.Count == 0)
            {
                MessageBox.Show("The model has no zones, so no dwelling can be assessed. Zone the model, mark its dwellings, and size it against Approved Document F first.", "Part O — Preparation");

                return;
            }

            //Read before the dialog so the dialog can say which of the three catalogue states it is in - and
            //so "the catalogue is missing" can never be presented as "no product can serve this dwelling".
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = zones,
                VentilationUnitCatalogue = ventilationUnitCatalogue,
            };

            //The PROJECT's own equipment preselection, restored off the model it belongs to. This is what
            //makes the mode and the permitted pool survive closing and reopening this dialog, and reopening
            //the project - and what keeps one project's pool out of the next one, which a global application
            //setting could not. Absent reads as the historic default. See PartOEquipmentSelection.
            //
            //The project's own test ventilation unit, restored the same way and from the same project. Set
            //BEFORE the preselection below: the pool is restored by ticking catalogue rows, and the test
            //product has no row until it has been stated.
            partOIterationWindow.ProjectTestVentilationUnit = analyticalModel.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);

            //How many dwellings the SAVED project has fitted with it - which is what makes removing or
            //renaming it refusable rather than something that silently orphans those assignments. Counted
            //here because this is the layer that holds a model; the control is told, not asked.
            partOIterationWindow.ProjectTestVentilationUnitAssignmentCount = Query.PartOVentilationUnitAssignmentCount(analyticalModel, partOIterationWindow.ProjectTestVentilationUnit?.VentilationUnitReference);

            //Assigned AFTER VentilationUnitCatalogue, because the pool is restored by ticking catalogue rows
            //and those rows do not exist until the catalogue has been set.
            partOIterationWindow.EquipmentSelection = analyticalModel.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partOIterationWindow).Owner = owner.Handle;
            }

            bool? showDialog = partOIterationWindow.ShowDialog();
            if (showDialog is null || !showDialog.Value)
            {
                return;
            }

            PartOVentilationStrategyOption? option = partOIterationWindow.SelectedOption;
            List<Zone> zones_Dwelling = partOIterationWindow.Zones_Dwelling;

            if (option is null || zones_Dwelling.Count == 0)
            {
                return;
            }

            //Everything the dialog collected, in the one shape the preparation seam takes. The picker and
            //the high-level Prepare & Run dialog differ only in how a request is arrived at; from here on
            //there is one path.
            PartOWorkflowRequest partOWorkflowRequest = new(
                option,
                partOIterationWindow.Isolate ? PartOWorkflowScope.SelectedDwellingsIsolated : PartOWorkflowScope.SelectedDwellings,
                zones_Dwelling,
                partOIterationWindow.SelectVentilationUnit)
            {
                OptimisationSettings = partOIterationWindow.OptimisationSettings,
                EquipmentSelection = partOIterationWindow.EquipmentSelection,
                ProjectTestVentilationUnit = partOIterationWindow.ProjectTestVentilationUnit,
            };

            PreparePartOIteration(uIAnalyticalModel, partORun, partOWorkflowRequest, ventilationUnitCatalogue, owner);
        }

        /// <summary>
        /// The preparation itself, over a request that has already been made: prepare, show what it produced,
        /// and - on OK - adopt the prepared model and start the session's Part O run.
        /// <para>
        /// <b>The one implementation.</b> The Prepare Iteration picker builds a request from its own controls
        /// and calls this; so does <see cref="RunPartOWorkflow"/>. Neither has a preparation of its own, so
        /// the two cannot drift, and the engineering below is still the single call
        /// <c>SAM.Analytical.Modify.PreparePartOIteration</c>.
        /// </para>
        /// <para>
        /// <b>The preparation summary is shown either way.</b> It states the thermal model scope, the route,
        /// the duty and the equipment, and it is the last point at which a person can decline before a
        /// full-year TAS run starts. A high-level workflow that skipped it would be hiding the one screen
        /// that says what is about to be simulated.
        /// </para>
        /// </summary>
        /// <returns>
        /// True where the prepared model was adopted and <paramref name="partORun"/> is now
        /// <see cref="PartORunState.Prepared"/>. False for a refusal, a decline, or a failed adoption - each
        /// of which has already been reported to the user by the time this returns.
        /// </returns>
        public static bool PreparePartOIteration(this UIAnalyticalModel? uIAnalyticalModel, PartORun partORun, PartOWorkflowRequest partOWorkflowRequest, VentilationUnitCatalogue ventilationUnitCatalogue, IWin32Window? owner = null)
        {
            return PrepareAndReviewPartOIteration(uIAnalyticalModel, partORun, partOWorkflowRequest, ventilationUnitCatalogue, ReviewIntent_PrepareIteration, owner) == PartOPreparationResult.Adopted;
        }

        /// <summary>
        /// What the Prepare Iteration command's review promises: adoption only. That command ends when the
        /// model is adopted - it has never started TAS - so its review must not say it will.
        /// </summary>
        internal const PartOReviewIntent ReviewIntent_PrepareIteration = PartOReviewIntent.PrepareOnly;

        /// <summary>
        /// <see cref="PreparePartOIteration(UIAnalyticalModel, PartORun, PartOWorkflowRequest, VentilationUnitCatalogue, IWin32Window)"/>,
        /// saying HOW it ended - so the Hub can tell an engineer who declined the review apart from a
        /// preparation that refused. Both leave the model and the run untouched; only one was a choice.
        /// </summary>
        /// <param name="partOReviewIntent">
        /// What the calling command does after an accepted review - it sets the review's wording, and nothing
        /// in this method depends on it.
        /// </param>
        internal static PartOPreparationResult PrepareAndReviewPartOIteration(UIAnalyticalModel? uIAnalyticalModel, PartORun partORun, PartOWorkflowRequest partOWorkflowRequest, VentilationUnitCatalogue ventilationUnitCatalogue, PartOReviewIntent partOReviewIntent, IWin32Window? owner = null)
        {
            AnalyticalModel? analyticalModel = uIAnalyticalModel?.JSAMObject;

            PartOVentilationStrategyOption? option = partOWorkflowRequest?.Option;

            List<Zone> zones_Dwelling = partOWorkflowRequest?.Zones_Dwelling ?? [];

            if (analyticalModel is null || partORun is null || option is null || zones_Dwelling.Count == 0)
            {
                return PartOPreparationResult.NotPrepared;
            }

            ventilationUnitCatalogue ??= VentilationUnitCatalogue.Read();

            //One canonical word for every zone in scope. There is no path by which anything else can be in
            //this dictionary - the option carries the word and neither dialog has a text field.
            Dictionary<Guid, string> dictionary_VentilationStrategy = partOWorkflowRequest!.VentilationStrategies();

            //Null, not an empty list, where no selection is wanted: the preparation reads null as "no
            //catalogue was offered" and leaves AirHandlingUnitParameter.VentilationUnitReference untouched,
            //which is Iteration 1a. An empty list would be a catalogue that offers nothing.
            //
            //The request states the INTENT and this reads the capability, which is why the two are separate:
            //an Iteration 2 request on a machine with no readable catalogue hands the preparation an EMPTY
            //list, so it refuses per dwelling and says so, rather than silently becoming an Iteration 1a run.
            //The Prepare & Run dialog blocks that combination before it gets here - see
            //PartOWorkflowInspection's Equipment stage - and this is what happens if anything else reaches it.
            //The request, or failing that the PROJECT, or failing that none - the same resolution order as
            //the preselection below and for the same reason. Absent is ordinary; most projects state none.
            PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit = Query.PartOProjectTestVentilationUnit(partOWorkflowRequest, analyticalModel);

            //What the project's own test product contributes: none or one. Qualified - SAM.Analytical.UI.WPF
            //declares a Query of its own.
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_ProjectTest = Analytical.Query.CapacityDescriptors(partOProjectTestVentilationUnit);

            //Null, not an empty list, where no selection is wanted: the preparation reads null as "no
            //catalogue was offered" and leaves AirHandlingUnitParameter.VentilationUnitReference untouched,
            //which is Iteration 1a. An empty list would be a catalogue that offers nothing.
            //
            //The request states the INTENT and this reads the capability, which is why the two are separate:
            //an Iteration 2 request on a machine with no readable catalogue hands the preparation an EMPTY
            //list, so it refuses per dwelling and says so, rather than silently becoming an Iteration 1a run.
            //The Prepare & Run dialog blocks that combination before it gets here - see
            //PartOWorkflowInspection's Equipment stage - and this is what happens if anything else reaches it.
            //
            //The project's test product joins the CAPABILITY LOOKUP - it has to, or a dwelling assigned to it
            //would report "capacity unknown" and Iteration 2B would lose that dwelling's ceiling. It joins
            //INSIDE this guard, so a test product can never by itself turn an unreadable manufacturer
            //catalogue into a selectable one: whether equipment selection happens at all is still
            //VentilationUnitCatalogue.HasSelectableProducts' answer, taken in the dialog above.
            List<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors = partOWorkflowRequest.SelectVentilationUnit
                ? [.. ventilationUnitCatalogue.CapacityDescriptors, .. ventilationUnitCapacityDescriptors_ProjectTest]
                : null;

            //The request, or failing that the PROJECT, or failing that the historic default - in that order,
            //and the order is load-bearing. Query.PartOEquipmentSelection says at length why, and is a named
            //function precisely because the failure it prevents is silent.
            PartOEquipmentSelection partOEquipmentSelection = Query.PartOEquipmentSelection(partOWorkflowRequest, analyticalModel);

            //TWO different lists, and conflating them is the one mistake that would break this feature.
            //
            //  ventilationUnitCapacityDescriptors  - the WHOLE selectable catalogue. A CAPABILITY LOOKUP.
            //      It goes into PartOPreparationContext, where Iteration 2B and the capacity envelope read
            //      it through Query.SelectedVentilationUnitCapacityDescriptor to find what each dwelling's
            //      ALREADY SELECTED product is rated at. Narrowing it to the pool would make a dwelling
            //      manually assigned a product that has since left the pool report "capacity unknown", and
            //      2B would lose the ceiling it stops at.
            //
            //  ventilationUnitCapacityDescriptors_Candidate - what an automatic selection may CHOOSE FROM.
            //      The pool, applied once, here. Null under manual authority, which is how
            //      Analytical.Modify.PreparePartOIteration is already told "run no rule and leave every
            //      existing identity alone" - so manual mode needs no new code path in SAM at all. An empty
            //      list under the pooled mode is an explicit refusal and is never widened back.
            //The two-list overload, and the distinction is load-bearing: "Automatic - all catalogue products"
            //means the MANUFACTURER catalogue, so a project test product left enabled must not join it, or a
            //project would select a made-up unit because a capacity happened to still be typed in a box and
            //every historic answer would move. PartOEquipmentSelection.CandidateDescriptors is where that
            //rule is written down.
            List<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors_Candidate = partOWorkflowRequest.SelectVentilationUnit
                ? partOEquipmentSelection.CandidateDescriptors(ventilationUnitCatalogue.CapacityDescriptors, ventilationUnitCapacityDescriptors_ProjectTest)
                : null;

            //Everything this preparation was asked for, kept so an Iteration 2B optimisation can repeat it
            //identically over a changed design. Also carries the optimisation the user asked for, which is
            //not a preparation input and does not affect the call below - see PartOPreparationContext.
            PartOPreparationContext partOPreparationContext = new(option.PartOIteration, zones_Dwelling, dictionary_VentilationStrategy, ventilationUnitCapacityDescriptors)
            {
                OptimisationSettings = partOWorkflowRequest.OptimisationSettings,
                Isolated = partOWorkflowRequest.Isolate,
                EquipmentSelection = partOEquipmentSelection,

                //Recorded so a preparation is not reused for a re-rated what-if - the capacity itself is
                //already in the lookup above. See PartOWorkflowInspection.Reusable.
                ProjectTestVentilationUnit = partOProjectTestVentilationUnit,
            };

            PartOIterationPreparation partOIterationPreparation = Analytical.Modify.PreparePartOIteration(analyticalModel, option.PartOIteration, zones_Dwelling, dictionary_VentilationStrategy, ventilationUnitCapacityDescriptors_Candidate, partOWorkflowRequest.Isolate);

            //A refusal returns no model at all, by contract. Nothing is adopted and the run is dropped with
            //the reason, so the ribbon can say why an assessment is unavailable.
            if (partOIterationPreparation.Refusal is not null)
            {
                partORun.Invalidate(partOIterationPreparation.Refusal);

                PartOProgressHost.Current?.Hide();

                MessageBox.Show(string.Format("The Part O iteration was not prepared.\n\n{0}", partOIterationPreparation.Refusal), "Part O — Preparation");

                return PartOPreparationResult.NotPrepared;
            }

            //An isolated run gets its own project name, so its TBD, TSD, .sam and TM59 report cannot land
            //on a full run's or on another selection's. Naming only - see Query.ProjectName_Isolated; the
            //context stamped on the model remains the authority for what this run actually was.
            PartOIsolationContext? partOIsolationContext = partOIterationPreparation.AnalyticalModel?.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            if (partOIsolationContext is not null && partOIsolationContext.IsValid)
            {
                partOIterationPreparation.AnalyticalModel!.Name = Query.ProjectName_Isolated(analyticalModel.Name, partOIsolationContext.ScopeToken);
            }

            //THE prepared model, and ONE working copy of its cluster.
            //
            //AnalyticalModel.AdjacencyCluster hands back a FRESH COPY on every access, so a cluster written
            //through one access is discarded the moment the next access is made. That makes an authored
            //equipment assignment exactly the kind of change that can be applied, reported as applied, and
            //silently lost - so the copy is taken once here, the commit below writes into THAT object, and
            //the model is rebuilt from it. The same discipline, and the same reason, as the
            //`new AnalyticalModel(analyticalModel, adjacencyCluster)` that ends
            //Analytical.Modify.PreparePartOIteration's own work.
            AnalyticalModel analyticalModel_Prepared = partOIterationPreparation.AnalyticalModel!;

            AdjacencyCluster adjacencyCluster_Prepared = analyticalModel_Prepared.AdjacencyCluster;

            //Space -> what to call the dwelling or zone it belongs to, resolved ONCE for the whole window.
            //Built here rather than inside the equipment branch below because BOTH tables need it - the
            //space table names a dwelling per row, and the assignment table names one per unit - and
            //because an Iteration 1a run has a space table too.
            Dictionary<Guid, string> dictionary_DwellingName_Space = DwellingNames_Space(adjacencyCluster_Prepared, zones_Dwelling);

            //The assignment table, built once against the WHOLE catalogue so that every assigned product
            //can resolve its own rating, and carrying the mode and pool so the window knows who decides.
            PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet = partOWorkflowRequest.SelectVentilationUnit
                ? EquipmentAssignmentSet(adjacencyCluster_Prepared, partOIterationPreparation, dictionary_DwellingName_Space, ventilationUnitCatalogue.CapacityDescriptors, partOEquipmentSelection, ventilationUnitCapacityDescriptors_ProjectTest)
                : null;

            PartOPreparationWindow partOPreparationWindow = new()
            {
                Intent = partOReviewIntent,
                ReviewSummary = Summary(partOIterationPreparation, option, ventilationUnitCatalogue, partOWorkflowRequest.SelectVentilationUnit, partOIsolationContext, partOEquipmentAssignmentSet),
                SpaceRows = (adjacencyCluster_Prepared.GetSpaces() ?? []).ConvertAll(x => new PartOSpaceRow(x, Name_Dwelling(dictionary_DwellingName_Space, x))),
            };

            if (partOEquipmentAssignmentSet is not null)
            {
                partOPreparationWindow.EquipmentAssignmentSet = partOEquipmentAssignmentSet;
            }
            else
            {
                //Iteration 1a, or a catalogue that could not be read: the rows still say what each dwelling
                //is designed to move, and say plainly that no product was selected.
                partOPreparationWindow.EquipmentRows = EquipmentRows(adjacencyCluster_Prepared, partOIterationPreparation, null, dictionary_DwellingName_Space);
            }

            partOPreparationWindow.SetDiagnostics(partOIterationPreparation.Notes, partOIterationPreparation.Warnings, partOIterationPreparation.Refusals);

            if (owner is not null)
            {
                new System.Windows.Interop.WindowInteropHelper(partOPreparationWindow).Owner = owner.Handle;
            }

            //The engineer's decision. The Part O progress window, where one is up, stands aside for it and
            //comes back for what follows.
            PartOProgressHost.Current?.Hide();

            bool? showDialog_Preparation = partOPreparationWindow.ShowDialog();

            PartOProgressHost.Current?.Show();

            return ConcludePartOReview(showDialog_Preparation, uIAnalyticalModel!, partORun, partOWorkflowRequest, analyticalModel_Prepared, adjacencyCluster_Prepared, partOIterationPreparation, partOPreparationContext, partOEquipmentAssignmentSet, partOProjectTestVentilationUnit);
        }

        /// <summary>
        /// What follows the engineer's answer to the Review iteration window: nothing at all for Cancel, and
        /// for Accept &amp; Run TAS the one write, the adoption and the model replacement - exactly the steps
        /// that followed OK before, moved here unchanged so the decision point can be tested without a dialog.
        /// <para>
        /// <b>Declined leaves everything as it was.</b> The loaded model is untouched - the preparation worked
        /// on a copy - and the run is not moved, so nothing can later be simulated and assessed against
        /// scenarios nobody accepted, and the caller never reaches TAS.
        /// </para>
        /// </summary>
        /// <param name="accepted">The window's DialogResult: true only for Accept &amp; Run TAS.</param>
        internal static PartOPreparationResult ConcludePartOReview(bool? accepted, UIAnalyticalModel uIAnalyticalModel, PartORun partORun, PartOWorkflowRequest partOWorkflowRequest, AnalyticalModel analyticalModel_Prepared, AdjacencyCluster adjacencyCluster_Prepared, PartOIterationPreparation partOIterationPreparation, PartOPreparationContext partOPreparationContext, PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet, PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit)
        {
            if (accepted is null || !accepted.Value)
            {
                return PartOPreparationResult.Declined;
            }

            //THE ONE WRITE, and only what the engineer actually changed - so a table that was merely
            //converted to manual, which changes no identity, writes nothing and leaves the prepared model
            //bit-for-bit as the preparation built it. Each write goes through
            //Analytical.Modify.AssignVentilationUnit, which moves no airflow of any kind.
            if (partOEquipmentAssignmentSet is not null)
            {
                if (!partOEquipmentAssignmentSet.Commit(adjacencyCluster_Prepared, out List<string> notes_Commit, out List<string> refusals_Commit))
                {
                    PartOProgressHost.Current?.Hide();

                    MessageBox.Show(string.Format("The equipment assignments were not applied, so the prepared model was not adopted.\n\n{0}", string.Join("\n\n", refusals_Commit)), "Part O — Preparation");

                    return PartOPreparationResult.NotPrepared;
                }

                partOIterationPreparation.Notes.AddRange(notes_Commit);

                if (partOEquipmentAssignmentSet.HasChanges)
                {
                    //Rebuilt from the cluster the assignments were written into - see the comment where that
                    //copy was taken. Guarded, so an unchanged table costs no rebuild.
                    analyticalModel_Prepared = new AnalyticalModel(analyticalModel_Prepared, adjacencyCluster_Prepared);
                }

                //The mode and pool as they now stand - "Convert to Manual" changed the mode, and this is
                //where that becomes the project's own recorded preference rather than a fact about one
                //dialog. It rides on the model, so it survives the project being saved and reopened and
                //cannot leak into another project.
                partOPreparationContext.EquipmentSelection = partOEquipmentAssignmentSet.EquipmentSelection;

                analyticalModel_Prepared.SetValue(Analytical.AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentAssignmentSet.EquipmentSelection);
            }

            //The project's test product, stamped beside the preselection and for the same reasons: it rides
            //on the model, so it survives the project being saved and reopened - which is what lets a
            //dwelling assigned to it resolve its capacity again rather than coming back as "capacity
            //unknown" - and it cannot leak into another project. Removed where the project no longer states
            //one, so a disabled what-if does not linger in a saved file.
            if (partOWorkflowRequest.SelectVentilationUnit)
            {
                if (partOProjectTestVentilationUnit is null)
                {
                    analyticalModel_Prepared.RemoveValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);
                }
                else
                {
                    analyticalModel_Prepared.SetValue(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);
                }
            }

            if (!AdoptPartOPreparation(partORun, analyticalModel_Prepared, partOIterationPreparation, partOPreparationContext))
            {
                PartOProgressHost.Current?.Hide();

                MessageBox.Show(string.Format("The prepared model was not adopted.\n\n{0}", partORun.InvalidationReason), "Part O — Preparation");

                return PartOPreparationResult.NotPrepared;
            }

            //Armed immediately before the write, so this replacement is not read as an outside edit.
            partORun.ExpectModification();

            uIAnalyticalModel.SetJSAMObject(analyticalModel_Prepared, new FullModification());

            return partORun.State == PartORunState.Prepared ? PartOPreparationResult.Adopted : PartOPreparationResult.NotPrepared;
        }

        /// <summary>
        /// Adopts an accepted preparation into the run - over the dialog's own prepared model, which is
        /// rebuilt after an equipment edit and so is not always the preparation's - <b>with the identities of
        /// the ventilation systems the preparation built</b>.
        /// <para>
        /// Those identities are the Iteration 3 system scope (SAM #114), and this is the only moment they are
        /// known: see <see cref="PartORun.Guids_VentilationSystem_Prepared"/>. Adopting without them leaves a
        /// run that simulates and assesses normally and can never start Iteration 3.
        /// </para>
        /// </summary>
        internal static bool AdoptPartOPreparation(PartORun partORun, AnalyticalModel analyticalModel_Prepared, PartOIterationPreparation partOIterationPreparation, PartOPreparationContext partOPreparationContext)
        {
            return partORun.Prepare(
                analyticalModel_Prepared,
                partOIterationPreparation.OverheatingScenarios,
                partOPreparationContext,
                PartORun.Guids_VentilationSystem(partOIterationPreparation),
                partOIterationPreparation.Refusal);
        }

        /// <summary>
        /// One equipment row per air handling unit the preparation built, each value read from its own
        /// authority: the duty from <c>Query.AirHandlingUnitDesignDuty</c>, the product from
        /// <c>Query.SelectedVentilationUnitReference</c>, and the capacity from the descriptor that product
        /// resolves to in the offered catalogue.
        /// <para>
        /// Rows are keyed on the air handling unit rather than paired positionally with
        /// <c>VentilationUnitSelections</c>, which is explicitly not item-for-item with
        /// <c>AirHandlingUnits</c> - a dwelling nothing could serve contributes a refusal and no selection.
        /// Reading the selection off the unit is what the preparation's own documentation says to do where the
        /// pairing matters.
        /// </para>
        /// </summary>
        /// <summary>
        /// Builds the dwelling assignment table from a completed preparation - the one place every model
        /// read for it happens.
        ///
        /// <para><b>Scoped to this run's own dwelling units</b></para>
        /// <para>
        /// <c>PartOIterationPreparation.AirHandlingUnits</c> and <c>.VentilationSystems</c> are what this
        /// preparation built, index for index, so nothing here has to re-derive which units belong to the
        /// run. A legacy unit the model was drawn with is not one of them and does not appear.
        /// </para>
        ///
        /// <para><b>The whole catalogue, never the pool</b></para>
        /// <para>
        /// <paramref name="ventilationUnitCapacityDescriptors"/> is the capability lookup. A dwelling
        /// assigned a product that is no longer permitted still resolves its own rating from it and is
        /// flagged as outside the pool - which is the difference between telling an engineer about a
        /// procurement change and losing their design to one.
        /// </para>
        ///
        /// <para><b>Cost</b></para>
        /// <para>
        /// One relation lookup per dwelling zone to name the dwellings, one per system to find its spaces,
        /// and one duty derivation per unit - then the table is captured numbers. No <c>GetSpaces</c> or
        /// <c>GetZones</c> rebuild happens inside a dwelling loop, and none happens again when a row is
        /// edited or the pool changes.
        /// </para>
        /// </summary>
        private static PartOEquipmentAssignmentSet EquipmentAssignmentSet(AdjacencyCluster? adjacencyCluster, PartOIterationPreparation partOIterationPreparation, Dictionary<Guid, string> dictionary_DwellingName_Space, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, PartOEquipmentSelection partOEquipmentSelection, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors_ProjectTest)
        {
            List<AirHandlingUnit> airHandlingUnits = partOIterationPreparation.AirHandlingUnits;
            List<VentilationSystem> ventilationSystems = partOIterationPreparation.VentilationSystems;

            Dictionary<Guid, string> dictionary_VentilationSystemName = [];

            for (int i = 0; i < airHandlingUnits.Count; i++)
            {
                AirHandlingUnit airHandlingUnit = airHandlingUnits[i];

                VentilationSystem? ventilationSystem = i < ventilationSystems.Count ? ventilationSystems[i] : null;

                if (airHandlingUnit is not null && ventilationSystem is not null)
                {
                    dictionary_VentilationSystemName[airHandlingUnit.Guid] = ventilationSystem.FullName;
                }
            }

            Dictionary<Guid, string> dictionary_DwellingName = DwellingNames_AirHandlingUnit(adjacencyCluster, partOIterationPreparation, dictionary_DwellingName_Space);

            return PartOEquipmentAssignmentSet.Create(
                adjacencyCluster,
                airHandlingUnits,
                dictionary_VentilationSystemName,
                dictionary_DwellingName,
                ventilationUnitCapacityDescriptors,
                partOEquipmentSelection,
                ventilationUnitCapacityDescriptors_ProjectTest);
        }

        /// <summary>
        /// Air handling unit -> the dwelling it serves, found through its ventilation system's own spaces.
        /// <para>
        /// One resolution for both equipment tables. The Iteration 2 table named its rows "Flat 1" this way
        /// while the Iteration 1a table printed the system name, "MVHR 1", so the same dwelling had two names
        /// on two screens. A unit left out where nothing resolves falls back, in the row, to the system or
        /// the unit name - never to an invented dwelling.
        /// </para>
        /// </summary>
        internal static Dictionary<Guid, string> DwellingNames_AirHandlingUnit(AdjacencyCluster? adjacencyCluster, PartOIterationPreparation partOIterationPreparation, Dictionary<Guid, string> dictionary_DwellingName_Space)
        {
            Dictionary<Guid, string> result = [];

            if (adjacencyCluster is null || partOIterationPreparation is null || dictionary_DwellingName_Space is null)
            {
                return result;
            }

            List<AirHandlingUnit> airHandlingUnits = partOIterationPreparation.AirHandlingUnits;
            List<VentilationSystem> ventilationSystems = partOIterationPreparation.VentilationSystems;

            for (int i = 0; i < airHandlingUnits.Count; i++)
            {
                AirHandlingUnit airHandlingUnit = airHandlingUnits[i];

                VentilationSystem? ventilationSystem = i < ventilationSystems.Count ? ventilationSystems[i] : null;

                if (airHandlingUnit is null || ventilationSystem is null)
                {
                    continue;
                }

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
                {
                    if (space is not null && dictionary_DwellingName_Space.TryGetValue(space.Guid, out string name_Dwelling) && !string.Equals(name_Dwelling, PartOSpaceRow.Unresolved, StringComparison.Ordinal))
                    {
                        result[airHandlingUnit.Guid] = name_Dwelling;

                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Space guid -> what to call the dwelling or zone it belongs to, resolved once for the whole
        /// preparation window.
        ///
        /// <para><b>Part O dwelling membership has absolute precedence</b></para>
        /// <para>
        /// The dwelling zones in the current scope are written first, so nothing below can overwrite a
        /// dwelling attribution. A space in a flat reads as that flat, whatever else it also belongs to.
        /// </para>
        ///
        /// <para><b>The fallback is a real relationship, and is gated twice</b></para>
        /// <para>
        /// A space outside every dwelling in scope is named after the zone that groups it - a communal
        /// corridor, a stair, a landlord area - and a zone qualifies only if it
        /// </para>
        /// <list type="number">
        /// <item>is in the SAME <c>ZoneParameter.ZoneCategory</c> as the dwellings in scope, which is the
        /// category the Part O assessment operates over; <b>and</b></item>
        /// <item>is a <c>Query.PartOClassifyAssessmentZones</c> <b>common-space</b> zone of that category -
        /// the other half of the one classification that already distinguishes a flat from a corridor.</item>
        /// </list>
        /// <para>
        /// <b>This is what keeps unrelated classifications out.</b> A fire zone, a thermal zone, a
        /// system-grouping zone or a reporting zone is not part of the dwelling / common-space
        /// classification and is normally in a different category, so it fails both gates and can never
        /// reach the column. Choosing whichever zone happened to sort first would have produced a
        /// deterministic answer that was confidently wrong - "Fire compartment 3" in a column headed
        /// "Dwelling / Zone" - and a wrong answer here is worse than no answer, because it reads as a
        /// statement about the model.
        /// </para>
        ///
        /// <para><b>Absence and ambiguity both read as an absence</b></para>
        /// <para>
        /// A space that resolves to nothing gets <see cref="PartOSpaceRow.Unresolved"/>. So does a space in
        /// TWO qualifying common-space zones: both names would be defensible, neither is authoritative, and
        /// picking one would invent an answer the model did not give. Nothing is ever concatenated.
        /// </para>
        ///
        /// <para><b>Cost</b></para>
        /// <para>
        /// One <c>GetZones</c> on the prepared cluster and one indexed <c>GetRelatedObjects</c> per zone -
        /// then every row is an O(1) probe. No <c>GetZones(space)</c> per space, which would materialise
        /// each space's whole related set (panels, systems, terminals) to find its zones, and no model
        /// rescan inside a row loop.
        /// </para>
        /// </summary>
        internal static Dictionary<Guid, string> DwellingNames_Space(AdjacencyCluster? adjacencyCluster, List<Zone> zones_Dwelling)
        {
            Dictionary<Guid, string> result = [];

            if (adjacencyCluster is null)
            {
                return result;
            }

            //Identity, never name: two dwellings may legitimately be called the same thing.
            HashSet<Guid> guids_Zone_Dwelling = [];

            foreach (Zone zone in zones_Dwelling ?? [])
            {
                if (zone is not null)
                {
                    guids_Zone_Dwelling.Add(zone.Guid);
                }
            }

            List<Zone> zones = adjacencyCluster.GetZones() ?? [];

            //PASS 1 - the dwellings in scope. Written first, so they win outright.
            HashSet<string> zoneCategories = [];

            List<Zone> zones_Scope = [];

            foreach (Zone zone in zones)
            {
                if (zone is null || !guids_Zone_Dwelling.Contains(zone.Guid))
                {
                    continue;
                }

                zones_Scope.Add(zone);

                //Null category is its own key rather than being skipped: a model whose zones state no
                //category still has one consistent answer to "the category the dwellings are in".
                zoneCategories.Add(zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) && zoneCategory is not null ? zoneCategory : string.Empty);

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    if (space is not null)
                    {
                        result[space.Guid] = zone.Name;
                    }
                }
            }

            //PASS 2 - the common-space zones OF THOSE CATEGORIES, and nothing else. Both gates are applied
            //here: the category, and then the dwelling / common-space classification within it.
            List<Zone> zones_Category = [];

            foreach (Zone zone in zones)
            {
                if (zone is null || guids_Zone_Dwelling.Contains(zone.Guid))
                {
                    continue;
                }

                string zoneCategory_Zone = zone.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory) && zoneCategory is not null ? zoneCategory : string.Empty;

                if (zoneCategories.Contains(zoneCategory_Zone))
                {
                    zones_Category.Add(zone);
                }
            }

            //Asked rather than repeated - PartFDwellingZones remains the single source of what a dwelling
            //is, and this is its other half. The scope zones are included so that a zone the classification
            //would call a dwelling is not offered here as a common space.
            Analytical.Query.PartOClassifyAssessmentZones([.. zones_Scope, .. zones_Category], out List<Zone> _, out List<Zone> zones_CommonSpace);

            //Spaces seen in more than one qualifying common-space zone: an ambiguity, reported as an
            //absence rather than resolved by picking one.
            HashSet<Guid> guids_Space_Ambiguous = [];

            Dictionary<Guid, string> dictionary_CommonSpace = [];

            foreach (Zone zone in zones_CommonSpace ?? [])
            {
                if (zone is null || guids_Zone_Dwelling.Contains(zone.Guid))
                {
                    continue;
                }

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    if (space is null || result.ContainsKey(space.Guid))
                    {
                        continue;
                    }

                    if (dictionary_CommonSpace.ContainsKey(space.Guid))
                    {
                        guids_Space_Ambiguous.Add(space.Guid);

                        continue;
                    }

                    dictionary_CommonSpace[space.Guid] = zone.Name;
                }
            }

            foreach (KeyValuePair<Guid, string> keyValuePair in dictionary_CommonSpace)
            {
                if (!guids_Space_Ambiguous.Contains(keyValuePair.Key))
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// One space's dwelling or zone name, or null where nothing resolved -
        /// <see cref="PartOSpaceRow"/> turns a null into the em dash it displays.
        /// </summary>
        private static string? Name_Dwelling(Dictionary<Guid, string> dictionary_DwellingName_Space, Space? space)
        {
            return space is not null && dictionary_DwellingName_Space.TryGetValue(space.Guid, out string name) ? name : null;
        }

        private static List<PartOEquipmentRow> EquipmentRows(AdjacencyCluster? adjacencyCluster, PartOIterationPreparation partOIterationPreparation, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, Dictionary<Guid, string>? dictionary_DwellingName_Space = null)
        {
            List<PartOEquipmentRow> result = [];

            if (adjacencyCluster is null)
            {
                return result;
            }

            Dictionary<Guid, string> dictionary_DwellingName = DwellingNames_AirHandlingUnit(adjacencyCluster, partOIterationPreparation, dictionary_DwellingName_Space ?? []);

            List<AirHandlingUnit> airHandlingUnits = partOIterationPreparation!.AirHandlingUnits;
            List<VentilationSystem> ventilationSystems = partOIterationPreparation.VentilationSystems;

            for (int i = 0; i < airHandlingUnits.Count; i++)
            {
                AirHandlingUnit airHandlingUnit = airHandlingUnits[i];
                if (airHandlingUnit is null)
                {
                    continue;
                }

                //Item for item with AirHandlingUnits, per PartOIterationPreparation's own contract.
                string? systemName = i < ventilationSystems.Count ? ventilationSystems[i]?.FullName : null;

                if (!Analytical.Query.AirHandlingUnitDesignDuty(adjacencyCluster, airHandlingUnit, out double supplyDuty_Lps, out double extractDuty_Lps))
                {
                    supplyDuty_Lps = double.NaN;
                    extractDuty_Lps = double.NaN;
                }

                //Null where nothing was selected, or where the selected reference is not in the offered
                //catalogue. Either way there is no capacity to show, and none is invented.
                VentilationUnitCapacityDescriptor? ventilationUnitCapacityDescriptor = ventilationUnitCapacityDescriptors is null
                    ? null
                    : Analytical.Query.SelectedVentilationUnitCapacityDescriptor(airHandlingUnit, ventilationUnitCapacityDescriptors);

                //A refusal is only stated where a catalogue was actually offered - without one, "not
                //applicable" is the truth and "refused" would be a fabrication.
                string? refusal = ventilationUnitCapacityDescriptors is not null && ventilationUnitCapacityDescriptor is null
                    ? Analytical.Query.IsVentilationUnitSufficient(adjacencyCluster, airHandlingUnit, ventilationUnitCapacityDescriptors, out string reason) ? null : reason
                    : null;

                result.Add(new PartOEquipmentRow(airHandlingUnit.Name, systemName, supplyDuty_Lps, extractDuty_Lps, ventilationUnitCapacityDescriptor, refusal, dictionary_DwellingName.TryGetValue(airHandlingUnit.Guid, out string name_Dwelling) ? name_Dwelling : null));
            }

            return result;
        }

        /// <summary>
        /// What the Review iteration window says about the prepared run, part by part. The same statements
        /// the one-paragraph summary made, with two corrections: the heading is the WORKFLOW scenario
        /// (<see cref="PartOWorkflowScenario.Find"/>), so an Iteration 2 run no longer heads itself
        /// "Iteration 1a"; and an Iteration 1a or 1b run no longer leads its equipment line with the catalogue,
        /// which that run does not use - the catalogue sentence is kept, on the tooltip.
        /// </summary>
        internal static PartOReviewSummary Summary(PartOIterationPreparation partOIterationPreparation, PartOVentilationStrategyOption option, VentilationUnitCatalogue ventilationUnitCatalogue, bool selectVentilationUnit, PartOIsolationContext? partOIsolationContext, PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet)
        {
            //The option's text is the fallback only for a pairing no workflow scenario describes - the
            //legacy picker can ask for one.
            string scenario = PartOWorkflowScenario.Find(option, selectVentilationUnit)?.Text ?? option.Text;

            //Said as a scope rather than as a setting. An isolated run is a different thermal model from the
            //whole building - the interfaces to the dwellings left out are simulated as adiabatic - and a
            //person reading these results later has to be told that without having to go looking for it, so
            //the consequence is shown, not tooltipped.
            //The one spelling the TM59 result window heads the same run's results with.
            string scope = Query.PartOThermalModelScopeText(partOIsolationContext, out string? scopeDetail);

            //ONE route word, not the settled mode followed by the canonical word in brackets - which on
            //the mechanical route printed the literal reading "MVHR (MVHR)". Query.PartOVentilationRouteText
            //proves the two agree rather than assuming it.
            string route = Query.PartOVentilationRouteText(partOIterationPreparation.VentilationMode, option.VentilationStrategy);

            //The whole-run totals, which are sums across every dwelling this run built - NOT any one
            //dwelling's duty. Said so explicitly, because a three-flat model summing to 156 l/s beside a
            //150 l/s product would otherwise read as an exceeded unit.
            int count_System = partOIterationPreparation.VentilationSystems.Count;

            bool mechanical = !double.IsNaN(partOIterationPreparation.DesignSupplyDuty_Lps);

            string duty = mechanical
                ? string.Format("{0:N1} l/s supply · {1:N1} l/s extract, totalled across {2}", partOIterationPreparation.DesignSupplyDuty_Lps, partOIterationPreparation.DesignExtractDuty_Lps, UI.Query.PartOCount(count_System, "dwelling system", "dwelling systems"))
                : "No mechanical design duty";

            string dutyDetail = mechanical
                ? "A sum across every dwelling system this run built, not any one dwelling's duty. Per-dwelling duties are in the dwelling table."
                : "The natural ventilation route realizes no continuous mechanical terminals.";

            //Asked of the assignment table rather than restated: it knows the mode, how many dwellings
            //carry a product and how many need looking at, and a second count here could disagree with the
            //grid immediately below it.
            string equipment;
            string? equipmentDetail;

            if (!selectVentilationUnit)
            {
                equipment = "No equipment selection · design duty only, no product is selected";
                equipmentDetail = ventilationUnitCatalogue.Description;
            }
            else if (partOEquipmentAssignmentSet is not null)
            {
                equipment = string.Format("{0} · {1}", Core.Query.Description(partOEquipmentAssignmentSet.EquipmentSelection.Mode), partOEquipmentAssignmentSet.AssignmentSummary);
                equipmentDetail = "A product's maximum is its capability ceiling and is never a design airflow.";
            }
            else
            {
                equipment = string.Format("Equipment selection ran against {0}", UI.Query.PartOCount(ventilationUnitCatalogue.CapacityDescriptors.Count, "selectable product", "selectable products"));
                equipmentDetail = "A selected product's maximum is its capability ceiling and is never a design airflow.";
            }

            int count_Scenario = partOIterationPreparation.OverheatingScenarios.Count;

            string overheatingScenarios = string.Format("{0} stated", UI.Query.PartOCount(count_Scenario, "overheating scenario", "overheating scenarios"));

            return new PartOReviewSummary(scenario, scope, scopeDetail, route, duty, dutyDetail, equipment, equipmentDetail, overheatingScenarios, mechanical);
        }
    }
}
