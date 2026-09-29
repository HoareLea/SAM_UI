// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the Part O preparation produced, for review before it is adopted - and, where the engineer is
    /// the selection authority, where the per-dwelling equipment assignments are made.
    ///
    /// <para><b>Why the manual work is here and not in the previous dialog</b></para>
    /// <para>
    /// A dwelling's design duty, its equipment's capacity and the headroom between them only exist once the
    /// iteration has been prepared. The previous dialog can state which products are permitted; it cannot
    /// show what any of them would mean for a dwelling. So the pool and the mode are chosen there, and the
    /// assignments - including "Convert to Manual" and every per-dwelling override - are made here, against
    /// real numbers, before the accepted review adopts the result.
    /// </para>
    ///
    /// <para><b>Nothing in this window decides anything</b></para>
    /// <para>
    /// Every edit is delegated to <see cref="EquipmentAssignmentSet"/>, which owns validation, suggestions
    /// and the single write path. There is no capacity comparison and no selection rule in this file - a
    /// second implementation of either is how a dialog comes to disagree with the engine it is a view of.
    /// The set writes to a model only when <c>Modify.PreparePartOIteration</c> commits it, after the review is
    /// accepted.
    /// </para>
    ///
    /// <para><b>The decision is named for what it does</b></para>
    /// <para>
    /// It was an unlabelled "OK", and what it does depends on the command that opened the window. From the
    /// Prepare &amp; Run Hub it adopts the model and the Hub goes on into TAS, so it says "Accept &amp; Run TAS";
    /// from the Prepare Iteration command it only adopts, so it says "Accept Preparation". See
    /// <see cref="Intent"/>. Cancel declines and changes nothing. Neither is the default button: Enter must
    /// never start a TAS run.
    /// </para>
    /// </summary>
    public partial class PartOPreparationWindow : System.Windows.Window
    {
        private List<PartOEquipmentRow> equipmentRows = [];

        private List<PartOSpaceRow> spaceRows = [];

        private PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet;

        /// <summary>
        /// The notes, warnings and refusals as handed in - counted and, for the screen, grouped. Held so
        /// that Copy All and the "Show every line" tick both read the same complete record rather than
        /// re-deriving it from what the box happens to be showing.
        /// </summary>
        private PartODiagnosticSummary partODiagnosticSummary = new(null, null, null);

        public PartOPreparationWindow()
        {
            InitializeComponent();

            UpdateEquipmentAvailability();

            UpdateDecision();
        }

        private PartOReviewIntent partOReviewIntent = PartOReviewIntent.PrepareOnly;

        /// <summary>
        /// What accepting this window leads to, as the command that opened it does it - and therefore what the
        /// primary action, its caption and its tooltip say. Wording only: the caller decides what happens after
        /// an accepted review, and this window returns the same DialogResult either way. Defaults to
        /// <see cref="PartOReviewIntent.PrepareOnly"/>, so a window nobody told otherwise never promises TAS.
        /// </summary>
        public PartOReviewIntent Intent
        {
            get
            {
                return partOReviewIntent;
            }
            set
            {
                partOReviewIntent = value;

                UpdateDecision();
            }
        }

        /// <summary>The primary action's text for an intent.</summary>
        internal static string AcceptText(PartOReviewIntent partOReviewIntent)
        {
            return partOReviewIntent == PartOReviewIntent.PrepareAndRun ? "Accept & Run TAS" : "Accept Preparation";
        }

        /// <summary>The line beside the decision buttons for an intent.</summary>
        internal static string DecisionText(PartOReviewIntent partOReviewIntent)
        {
            return partOReviewIntent == PartOReviewIntent.PrepareAndRun
                ? "Accept & Run TAS adopts this prepared model and starts the full-year TAS simulation, then the TM59 assessment. Cancel changes nothing and starts nothing."
                : "Accept Preparation adopts this prepared model. No TAS simulation is started. Cancel changes nothing.";
        }

        private void UpdateDecision()
        {
            bool run = partOReviewIntent == PartOReviewIntent.PrepareAndRun;

            button_Accept.Content = AcceptText(partOReviewIntent);

            button_Accept.ToolTip = run
                ? "Adopt the prepared model, with the assignments above, and start the full-year TAS simulation. The TM59 assessment follows."
                : "Adopt the prepared model, with the assignments above. Nothing is simulated.";

            textBlock_Decision.Text = DecisionText(partOReviewIntent);

            textBlock_Subtitle.Text = run
                ? "The iteration is prepared. Review it here; nothing is simulated until you accept."
                : "The iteration is prepared. Review it here; accepting adopts it and does not start a simulation.";
        }

        /// <summary>What the line beside the decision buttons says. For a test to read.</summary>
        internal string DecisionCaption => textBlock_Decision.Text;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            //Its decision row is at the bottom; an owner low on a short monitor otherwise opens it with Accept
            //and Cancel under the taskbar. The Hub's own placement, shared.
            PartOWindowPlacement.KeepOnScreen(this);
        }

        /// <summary>The equipment rows, one per dwelling air handling unit.</summary>
        public List<PartOEquipmentRow> EquipmentRows
        {
            get
            {
                return equipmentRows;
            }
            set
            {
                equipmentRows = value ?? [];

                dataGrid_Equipment.ItemsSource = equipmentRows;

                UpdateEquipmentAvailability();
            }
        }

        /// <summary>
        /// The assignment table this window edits, or null where equipment selection is not in play at all -
        /// Iteration 1a, or a catalogue that could not be read.
        /// <para>
        /// Setting it builds the rows from it, so the grid and the set cannot be given different content.
        /// </para>
        /// </summary>
        public PartOEquipmentAssignmentSet? EquipmentAssignmentSet
        {
            get
            {
                return partOEquipmentAssignmentSet;
            }
            set
            {
                partOEquipmentAssignmentSet = value;

                if (value is not null)
                {
                    EquipmentRows = value.Assignments.ConvertAll(x => new PartOEquipmentRow(x, value));
                }
                else
                {
                    UpdateEquipmentAvailability();
                }
            }
        }

        /// <summary>The space rows.</summary>
        public List<PartOSpaceRow> SpaceRows
        {
            get
            {
                return spaceRows;
            }
            set
            {
                spaceRows = value ?? [];

                dataGrid_Spaces.ItemsSource = spaceRows;
            }
        }

        private PartOReviewSummary? partOReviewSummary;

        /// <summary>
        /// What the preparation produced, as the window's header shows it: the scenario, then scope, route,
        /// design duty, equipment and overheating scenarios. The details each part carries are on its tooltip,
        /// except an isolated scope's consequence, which is shown.
        /// </summary>
        public PartOReviewSummary? ReviewSummary
        {
            get
            {
                return partOReviewSummary;
            }
            set
            {
                partOReviewSummary = value;

                textBlock_Scenario.Text = value?.Scenario ?? string.Empty;

                textBlock_Scope.Text = value?.Scope ?? string.Empty;
                textBlock_ScopeDetail.Text = value?.ScopeDetail ?? string.Empty;
                textBlock_ScopeDetail.Visibility = string.IsNullOrWhiteSpace(value?.ScopeDetail) ? Visibility.Collapsed : Visibility.Visible;

                textBlock_Route.Text = value?.Route ?? string.Empty;

                textBlock_Duty.Text = value?.Duty ?? string.Empty;
                textBlock_Duty.ToolTip = string.IsNullOrWhiteSpace(value?.DutyDetail) ? null : value!.DutyDetail;

                textBlock_Equipment.Text = value?.Equipment ?? string.Empty;
                textBlock_Equipment.ToolTip = string.IsNullOrWhiteSpace(value?.EquipmentDetail) ? null : value!.EquipmentDetail;

                textBlock_OverheatingScenarios.Text = value?.OverheatingScenarios ?? string.Empty;

                //The natural ventilation route has no mechanical design duty and no equipment: its space
                //table keeps the Part F requirement as a reference and does not show mechanical design
                //SUP/EXT columns as though this case had a mechanical system.
                bool mechanical = value?.HasMechanicalDesignDuty ?? true;
                Visibility visibility_Design = mechanical ? Visibility.Visible : Visibility.Collapsed;
                column_Spaces_DesignSupply.Visibility = visibility_Design;
                column_Spaces_DesignExtract.Visibility = visibility_Design;
                textBlock_SpacesCaption.Text = mechanical
                    ? "Approved Document F requirement and design airflow are different quantities"
                    : "Approved Document F requirement, for reference · natural ventilation, no mechanical design airflow";
            }
        }

        /// <summary>Whether the space table shows the mechanical design SUP/EXT columns. For a test to read.</summary>
        internal bool ShowsSpaceDesignAirflow => column_Spaces_DesignSupply.Visibility == Visibility.Visible;

        /// <summary>The preparation summary as one block - what Copy All puts first.</summary>
        public string Summary => partOReviewSummary?.Text ?? string.Empty;

        /// <summary>The scenario the window is headed with. For a test to read.</summary>
        internal string ScenarioHeading => textBlock_Scenario.Text;

        /// <summary>What the primary action says. For a test to read.</summary>
        internal string AcceptActionText => button_Accept.Content as string ?? string.Empty;

        /// <summary>What the secondary action says. For a test to read.</summary>
        internal string CancelActionText => button_Cancel.Content as string ?? string.Empty;

        /// <summary>
        /// Whether the primary action is the default button - it must not be, or Enter would start a TAS run.
        /// And whether Cancel is the cancel button, so Esc declines. For a test to read.
        /// </summary>
        internal bool IsAcceptDefault => button_Accept.IsDefault;

        /// <summary>Whether Esc presses Cancel. For a test to read.</summary>
        internal bool IsCancelTheCancelButton => button_Cancel.IsCancel;

        /// <summary>Presses Accept &amp; Run TAS, as a click would. For a test driving a shown dialog.</summary>
        internal void PressAccept()
        {
            button_Accept.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        /// <summary>Presses Cancel, as a click would. For a test driving a shown dialog.</summary>
        internal void PressCancel()
        {
            button_Cancel.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        /// <summary>Whether the equipment authority controls - Convert to Manual, Assign suggested, bulk - are drawn.</summary>
        internal bool AreEquipmentControlsShown => stackPanel_Authority.Visibility == Visibility.Visible && grid_Bulk.Visibility == Visibility.Visible && grid_Assignment.Visibility == Visibility.Visible;

        /// <summary>Whether the "no dwelling units" line stands in for the dwelling table.</summary>
        internal bool IsNoEquipmentShown => textBlock_NoEquipment.Visibility == Visibility.Visible;

        /// <summary>No dwelling unit and no equipment selection (Iteration 1b): shown as a sentence, not a table.</summary>
        private bool IsEquipmentEmpty => equipmentRows.Count == 0 && partOEquipmentAssignmentSet is null;

        /// <summary>What this window currently says about the selection authority. For a test to read.</summary>
        public string ModeDescription => textBlock_Mode.Text;

        /// <summary>What this window currently says about the selected dwelling. For a test to read.</summary>
        public string AssignmentDescription => textBlock_Assignment.Text;

        /// <summary>Whether "Convert to Manual" is currently offered.</summary>
        internal bool CanConvertToManual => button_ConvertToManual.IsEnabled;

        /// <summary>Whether the assignment grid is currently editable - true only in manual mode.</summary>
        internal bool IsEquipmentEditable => !dataGrid_Equipment.IsReadOnly;

        /// <summary>Whether bulk assignment is currently offered at all - manual authority only.</summary>
        internal bool IsBulkAssignmentAvailable => grid_Bulk.IsEnabled;

        /// <summary>Whether "Apply to selected" can currently be clicked.</summary>
        internal bool CanApplyToSelected => button_ApplyToSelected.IsEnabled;

        /// <summary>What the window currently says about the selection. For a test to read.</summary>
        internal string BulkSelectionDescription => textBlock_BulkSelection.Text;

        /// <summary>
        /// What the window says the last bulk assignment did, or empty where none has been made since the
        /// selection or the authority last moved. Presentation feedback; nothing reads it but a test.
        /// </summary>
        internal string BulkConfirmation => textBlock_BulkConfirmation.Text;

        /// <summary>The products bulk assignment currently offers. For a test to read.</summary>
        internal List<VentilationUnitCapacityDescriptor> BulkProducts => [.. comboBox_BulkProduct.ItemsSource?.OfType<VentilationUnitCapacityDescriptor>() ?? []];

        /// <summary>
        /// Selects the rows for the named dwelling units, as clicking and Ctrl+clicking them would. For a
        /// test to drive a bulk assignment without a mouse.
        /// </summary>
        internal void SelectEquipmentRows(IEnumerable<Guid> guids_AirHandlingUnit)
        {
            HashSet<Guid> guids = [.. guids_AirHandlingUnit ?? []];

            dataGrid_Equipment.SelectedItems.Clear();

            foreach (PartOEquipmentRow partOEquipmentRow in equipmentRows)
            {
                if (guids.Contains(partOEquipmentRow.Guid_AirHandlingUnit))
                {
                    dataGrid_Equipment.SelectedItems.Add(partOEquipmentRow);
                }
            }

            UpdateBulkAvailability();
        }

        /// <summary>The product bulk assignment will apply. Settable so a test can choose one.</summary>
        internal VentilationUnitCapacityDescriptor? BulkProduct
        {
            get
            {
                return comboBox_BulkProduct.SelectedItem as VentilationUnitCapacityDescriptor;
            }
            set
            {
                comboBox_BulkProduct.SelectedItem = value;

                UpdateBulkAvailability();
            }
        }

        /// <summary>
        /// <b>Apply to selected.</b> Assigns the chosen product to every selected dwelling and to no other,
        /// as one deliberate act.
        ///
        /// <para><b>Why this exists rather than "editing a cell edits the selection"</b></para>
        /// <para>
        /// Because a hundred or a thousand dwellings cannot be authored one row at a time, and because the
        /// obvious alternative is dangerous: a picker that quietly wrote its value into every highlighted
        /// row would be triggered by an ordinary mis-click and would leave no trace of having done it. So
        /// single-row editing stays single-row, and bulk assignment is a named button next to a count of
        /// what it will touch.
        /// </para>
        ///
        /// <para><b>Each dwelling is judged on its own duty</b></para>
        /// <para>
        /// The set re-evaluates each assigned row independently, so capability, pool membership, headroom,
        /// status and any suggestion are that dwelling's own answer. Nothing here compares a capacity or
        /// reduces a design airflow to fit a product - and an insufficient assignment stays assigned and is
        /// reported, exactly as a single-row one does.
        /// </para>
        ///
        /// <para><b>Only the rows that changed are refreshed</b></para>
        /// <para>
        /// An assignment changes that row's derived state and no other row's, so refreshing the whole table
        /// would be O(D) work per bulk operation for nothing. On a thousand-dwelling project that is the
        /// difference between an instant operation and a visible pause.
        /// </para>
        /// </summary>
        /// <returns>True where every selected dwelling was assigned.</returns>
        internal bool ApplyToSelected()
        {
            if (partOEquipmentAssignmentSet is null || !partOEquipmentAssignmentSet.IsManual)
            {
                return false;
            }

            if (comboBox_BulkProduct.SelectedItem is not VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor)
            {
                return false;
            }

            List<PartOEquipmentRow> partOEquipmentRows = [.. dataGrid_Equipment.SelectedItems.OfType<PartOEquipmentRow>()];

            if (partOEquipmentRows.Count == 0)
            {
                return false;
            }

            bool result = partOEquipmentAssignmentSet.Assign(
                partOEquipmentRows.ConvertAll(x => x.Guid_AirHandlingUnit),
                ventilationUnitCapacityDescriptor.VentilationUnitReference,
                out List<Guid> guids_Assigned,
                out List<string> refusals);

            HashSet<Guid> guids = [.. guids_Assigned];

            foreach (PartOEquipmentRow partOEquipmentRow in partOEquipmentRows)
            {
                if (guids.Contains(partOEquipmentRow.Guid_AirHandlingUnit))
                {
                    partOEquipmentRow.Refresh();
                }
            }

            UpdateAssignmentText();

            UpdateBulkAvailability();

            //Said inline, beside the button that did it, because a bulk assignment that changes twelve rows
            //off the top of a scrolled table is otherwise indistinguishable from a click that did nothing.
            //Presentation feedback: it reports the set's own answer and decides none of it.
            textBlock_BulkConfirmation.Text = guids.Count == 0
                ? string.Empty
                : string.Format(
                    "Assigned {0} to {1} dwelling{2}.",
                    Query.PartOProductLabel(ventilationUnitCapacityDescriptor.VentilationUnitReference),
                    guids.Count,
                    guids.Count == 1 ? string.Empty : "s");

            if (refusals.Count != 0)
            {
                MessageBox.Show(string.Format("Not every selected dwelling was assigned.\n\n{0}", string.Join("\n\n", refusals)), "Part O — Review iteration");
            }

            return result;
        }

        /// <summary>
        /// <b>Convert to Manual.</b> Hands the selection authority to the engineer and preserves every
        /// dwelling's product exactly, by not touching any of them.
        /// <para>
        /// No selection is rerun, no product is improved upon, no airflow of any kind is recalculated and no
        /// simulation work is triggered. All that changes is who decides next - and, as a consequence, that
        /// the assigned-product cells become editable.
        /// </para>
        /// </summary>
        internal void ConvertToManual()
        {
            if (partOEquipmentAssignmentSet is null || partOEquipmentAssignmentSet.IsManual)
            {
                return;
            }

            partOEquipmentAssignmentSet.ConvertToManual();

            //The identities are untouched; only the derived columns and the editability move.
            foreach (PartOEquipmentRow partOEquipmentRow in equipmentRows)
            {
                partOEquipmentRow.Refresh();
            }

            UpdateEquipmentAvailability();
        }

        /// <summary>Takes the suggested product for the currently selected dwelling. An explicit act.</summary>
        internal bool AssignSuggested()
        {
            if (dataGrid_Equipment.SelectedItem is not PartOEquipmentRow partOEquipmentRow || !partOEquipmentRow.AssignSuggested())
            {
                return false;
            }

            //One dwelling changed, so every row's derived state is re-read - a pool or suggestion is a
            //property of the set, and only the set knows what a change means for the rest.
            foreach (PartOEquipmentRow partOEquipmentRow_Other in equipmentRows)
            {
                partOEquipmentRow_Other.Refresh();
            }

            UpdateEquipmentAvailability();

            return true;
        }

        /// <summary>
        /// Notes, warnings and refusals, refusals first - counted in the header, and shown with
        /// character-for-character identical warnings collapsed.
        ///
        /// <para><b>Nothing is interpreted and nothing is lost</b></para>
        /// <para>
        /// No line is parsed, classified, re-graded or suppressed; the only aggregation is that two
        /// byte-identical warnings become one line and a <c>× 2</c>.
        /// <see cref="DiagnosticsFullText"/> - what Copy All copies, and what the "Show every line" tick
        /// puts back on screen - is every line as produced. See <see cref="PartODiagnosticSummary"/>.
        /// </para>
        ///
        /// <para><b>The tick is HIDDEN where nothing was collapsed, not merely disabled</b></para>
        /// <para>
        /// And on today's Part O warnings that is every run. Every warning this window can receive names
        /// its space - <c>Modify.AddPartOBaseMVHRSystem</c>'s stale-relation warning names the space and
        /// the system, and <c>Query.ReconcileVentilationSystemDesignDuty</c>'s headroom and shortfall
        /// warnings name the space, the direction and both airflows - so no two of them are ever
        /// character-for-character identical and the collapsing is a no-op. The counts in the header are
        /// the part that always earns its place.
        /// </para>
        /// <para>
        /// A permanently greyed tick is worse than no tick: it advertises a capability that never arrives
        /// and leaves the engineer working out why they cannot use it. So the affordance appears only when
        /// it has something to do - which keeps the grouping honest for any producer that does repeat a
        /// line, without putting dead furniture on the window.
        /// </para>
        /// </summary>
        public void SetDiagnostics(IEnumerable<string> notes, IEnumerable<string> warnings, IEnumerable<string> refusals)
        {
            partODiagnosticSummary = new PartODiagnosticSummary(notes, warnings, refusals);

            label_Diagnostics.Text = partODiagnosticSummary.Header;

            //Offered only where collapsing actually removed a line - and taken off the window entirely
            //otherwise, rather than left greyed.
            checkBox_ShowEveryLine.IsEnabled = partODiagnosticSummary.IsGrouped;

            if (!partODiagnosticSummary.IsGrouped)
            {
                checkBox_ShowEveryLine.IsChecked = false;
            }

            UpdateDiagnosticsText();

            //Collapsed by default - the counts say what there is - and open where the preparation REFUSED
            //something: a refusal is not a note, and it is not left one click away.
            ShowDiagnostics = partODiagnosticSummary.RefusalCount != 0;
        }

        /// <summary>What the diagnostics header says - the counts. For a test to read.</summary>
        internal string DiagnosticsHeader => label_Diagnostics.Text ?? string.Empty;

        /// <summary>
        /// Whether the diagnostics box is shown. False by default, true where anything was refused. Settable,
        /// as the Show details switch sets it. Nothing is lost while it is false: the box still holds every
        /// line, and Copy All copies every line either way.
        /// </summary>
        internal bool ShowDiagnostics
        {
            get
            {
                return checkBox_ShowDiagnostics.IsChecked ?? false;
            }
            set
            {
                checkBox_ShowDiagnostics.IsChecked = value;

                UpdateDiagnosticsVisibility();
            }
        }

        /// <summary>Whether the diagnostic lines are on screen. For a test to read.</summary>
        internal bool IsDiagnosticsTextShown => textBox_Notes.Visibility == Visibility.Visible;

        private void UpdateDiagnosticsVisibility()
        {
            bool show = checkBox_ShowDiagnostics.IsChecked ?? false;

            textBox_Notes.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            //"Show every line" only means something while the lines are shown.
            checkBox_ShowEveryLine.Visibility = show && partODiagnosticSummary.IsGrouped ? Visibility.Visible : Visibility.Collapsed;
        }

        private void checkBox_ShowDiagnostics_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDiagnosticsVisibility();
        }

        /// <summary>What the diagnostics box is currently showing - grouped, or every line. For a test to read.</summary>
        internal string DiagnosticsText => textBox_Notes.Text;

        /// <summary>
        /// Every note, warning and refusal as produced, ungrouped. What Copy All copies, whatever the box is
        /// showing. For a test to read.
        /// </summary>
        internal string DiagnosticsFullText => partODiagnosticSummary.Text;

        /// <summary>How many warnings were handed in, before any identical lines were collapsed.</summary>
        internal int WarningCount => partODiagnosticSummary.WarningCount;

        /// <summary>Whether the box is listing every line rather than the grouped view. Settable for tests.</summary>
        internal bool ShowEveryLine
        {
            get
            {
                return checkBox_ShowEveryLine.IsChecked ?? false;
            }
            set
            {
                checkBox_ShowEveryLine.IsChecked = value;

                UpdateDiagnosticsText();
            }
        }

        /// <summary>Whether collapsing identical warnings removed anything, and so whether the tick has work.</summary>
        internal bool IsDiagnosticsGrouped => partODiagnosticSummary.IsGrouped;

        /// <summary>
        /// Whether the "Show every line" tick is on the window at all. False - and hidden rather than
        /// greyed - wherever no two warnings were identical, which is every run of today's Part O
        /// warnings. Exposed so that is assertable rather than merely intended.
        /// </summary>
        internal bool IsShowEveryLineOffered => checkBox_ShowEveryLine.IsEnabled && checkBox_ShowEveryLine.Visibility == (ShowDiagnostics ? Visibility.Visible : Visibility.Collapsed);

        private void UpdateDiagnosticsText()
        {
            textBox_Notes.Text = ShowEveryLine
                ? partODiagnosticSummary.Text
                : partODiagnosticSummary.GroupedText;
        }

        private void checkBox_ShowEveryLine_Click(object sender, RoutedEventArgs e)
        {
            UpdateDiagnosticsText();
        }

        /// <summary>
        /// Keeps the equipment controls consistent with who the authority currently is: the grid is
        /// read-only in the automatic modes, where its rows are results, and editable in manual mode, where
        /// they are choices.
        /// </summary>
        private void UpdateEquipmentAvailability()
        {
            bool manual = partOEquipmentAssignmentSet?.IsManual ?? false;

            //Drawn only where equipment selection is in play. On Iteration 1a and 1b none of these can ever
            //apply, and four greyed controls invite a person to work out why. Their enable rules below are
            //unchanged either way.
            Visibility visibility_Authority = partOEquipmentAssignmentSet is null ? Visibility.Collapsed : Visibility.Visible;

            stackPanel_Authority.Visibility = visibility_Authority;
            grid_Assignment.Visibility = visibility_Authority;
            grid_Bulk.Visibility = visibility_Authority;

            //Iteration 1b builds no dwelling unit: a sentence instead of an empty ten-column table, and the
            //row gives its height to the space table.
            bool empty = IsEquipmentEmpty;

            dataGrid_Equipment.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            textBlock_NoEquipment.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            rowDefinition_Equipment.Height = empty ? GridLength.Auto : new GridLength(1, GridUnitType.Star);

            dataGrid_Equipment.IsReadOnly = !manual;

            //Offered only under manual authority: in an automatic mode these rows are a rule's results, and
            //a bulk override made without taking authority would be an authored assignment nobody authored.
            grid_Bulk.IsEnabled = manual;

            //Rebuilt from the project's permitted set, which "Convert to Manual" and a pool change both
            //move. The chosen product is preserved by identity where it is still permitted.
            VentilationUnitReference? ventilationUnitReference = (comboBox_BulkProduct.SelectedItem as VentilationUnitCapacityDescriptor)?.VentilationUnitReference;

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = partOEquipmentAssignmentSet?.AllowedCandidates ?? [];

            comboBox_BulkProduct.ItemsSource = ventilationUnitCapacityDescriptors;
            comboBox_BulkProduct.SelectedItem = ventilationUnitReference is null
                ? null
                : ventilationUnitCapacityDescriptors.Find(x => ventilationUnitReference.Matches(x.VentilationUnitReference));

            UpdateBulkAvailability();

            //Offered only where there is an automatic answer to convert. Converting an empty table would
            //change an authority over nothing, and converting a manual one is already done.
            button_ConvertToManual.IsEnabled = partOEquipmentAssignmentSet is not null
                && !partOEquipmentAssignmentSet.IsManual
                && partOEquipmentAssignmentSet.HasAssignments;

            textBlock_Mode.Text = partOEquipmentAssignmentSet is null
                ? string.Empty
                : Core.Query.Description(partOEquipmentAssignmentSet.EquipmentSelection.Mode);

            //A confirmation of an assignment made under a different authority would be misleading here.
            textBlock_BulkConfirmation.Text = string.Empty;

            UpdateAssignmentText();
        }

        /// <summary>The selected dwelling in words, and whether a suggestion can be taken for it.</summary>
        private void UpdateAssignmentText()
        {
            PartOEquipmentRow? partOEquipmentRow = dataGrid_Equipment.SelectedItem as PartOEquipmentRow;

            textBlock_Assignment.Text = partOEquipmentRow?.Description ?? string.Empty;

            //A suggestion can only be TAKEN in manual mode. In an automatic mode the product is the rule's
            //answer, and applying a suggestion over it would be an authored assignment made without the
            //engineer having taken authority.
            button_AssignSuggested.IsEnabled = (partOEquipmentAssignmentSet?.IsManual ?? false) && (partOEquipmentRow?.HasSuggestion ?? false);
        }

        private void button_ConvertToManual_Click(object sender, RoutedEventArgs e)
        {
            ConvertToManual();
        }

        private void button_AssignSuggested_Click(object sender, RoutedEventArgs e)
        {
            AssignSuggested();
        }

        /// <summary>
        /// What a bulk assignment would currently do, and whether it can be done at all: a product has to
        /// be chosen, at least one dwelling selected, and the engineer has to hold the authority.
        /// </summary>
        private void UpdateBulkAvailability()
        {
            bool manual = partOEquipmentAssignmentSet?.IsManual ?? false;

            int selected = dataGrid_Equipment.SelectedItems.OfType<PartOEquipmentRow>().Count();

            //Live, and phrased as a proportion: "3 of 412 dwellings selected" says both what a bulk
            //assignment would touch and how much of the project it is, which "Selected dwellings: 3" did
            //not.
            textBlock_BulkSelection.Text = manual
                ? string.Format("{0} of {1} dwelling{2} selected", selected, equipmentRows.Count, equipmentRows.Count == 1 ? string.Empty : "s")
                : string.Empty;

            button_ApplyToSelected.IsEnabled = manual && selected != 0 && comboBox_BulkProduct.SelectedItem is VentilationUnitCapacityDescriptor;
        }

        private void dataGrid_Equipment_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //A confirmation about the previous selection would be read as being about this one.
            textBlock_BulkConfirmation.Text = string.Empty;

            UpdateAssignmentText();

            UpdateBulkAvailability();
        }

        private void comboBox_BulkProduct_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateBulkAvailability();
        }

        private void button_ApplyToSelected_Click(object sender, RoutedEventArgs e)
        {
            ApplyToSelected();
        }

        /// <summary>
        /// After a picker commits, every row's derived state is re-read. The edit itself happened in
        /// <c>PartOEquipmentRow.SelectedCandidate</c>, which delegated it to the assignment set; this only
        /// makes the rest of the table agree with what the set now says.
        /// </summary>
        private void dataGrid_Equipment_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != System.Windows.Controls.DataGridEditAction.Commit)
            {
                return;
            }

            //THAT row, and not the table. One dwelling's assignment cannot change another dwelling's
            //capability, pool membership, headroom, status or suggestion - each of those is an answer about
            //that dwelling's own duty against the project's own permitted set. Refreshing all of them was
            //harmless on a demonstration model and is O(D) per keystroke-committed edit on a real one.
            PartOEquipmentRow? partOEquipmentRow_Edited = e.Row?.Item as PartOEquipmentRow;

            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                partOEquipmentRow_Edited?.Refresh();

                UpdateAssignmentText();

                UpdateBulkAvailability();
            }));
        }

        /// <summary>
        /// What Copy All copies: the summary, both tables and the complete diagnostic record.
        ///
        /// <para><b>It reproduces the REVIEWED tables</b></para>
        /// <para>
        /// Which is the whole point of it - this text is pasted into an issue, a report or an email - so
        /// every airflow goes through <c>PartOAirFlowConverter.Text</c>,
        /// the same formatting the cells use. Formatted independently with <c>N1</c> it disagreed with the
        /// grid immediately: an absent value painted as an em dash on screen and pasted as <c>NaN</c>, up
        /// to four times per equipment row on an Iteration 1a or 1b table where no product is selected.
        /// </para>
        /// <para>
        /// Separated from the click handler so the text is assertable without a clipboard - a clipboard the
        /// test host may not even own.
        /// </para>
        /// </summary>
        internal string CopyAllText()
        {
            StringBuilder stringBuilder = new();

            stringBuilder.AppendLine(Summary);
            stringBuilder.AppendLine();

            //The same as the window: no dwelling unit is a sentence, not an empty table whose headings name
            //mechanical design SUP/EXT airflow on a route that has none.
            if (IsEquipmentEmpty)
            {
                stringBuilder.AppendLine(textBlock_NoEquipment.Text);
            }
            else
            {
                stringBuilder.AppendLine("Dwelling\tUnit\tDesign SUP (l/s)\tDesign EXT (l/s)\tAssigned product\tMax SUP (l/s)\tMax EXT (l/s)\tSUP headroom (l/s)\tEXT headroom (l/s)\tStatus");
            }

            foreach (PartOEquipmentRow row in equipmentRows)
            {
                stringBuilder.AppendLine(string.Format(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                    row.Dwelling,
                    row.UnitName,
                    PartOAirFlowConverter.Text(row.DesignSupplyDuty_Lps),
                    PartOAirFlowConverter.Text(row.DesignExtractDuty_Lps),
                    row.SelectedProduct,
                    PartOAirFlowConverter.Text(row.MaximumSupply_Lps),
                    PartOAirFlowConverter.Text(row.MaximumExtract_Lps),
                    PartOAirFlowConverter.Text(row.SupplyHeadroom_Lps),
                    PartOAirFlowConverter.Text(row.ExtractHeadroom_Lps),
                    row.SelectionOutcome));
            }

            stringBuilder.AppendLine();

            if (!(partOReviewSummary?.HasMechanicalDesignDuty ?? true))
            {
                //The natural ventilation route: the same columns the table shows.
                stringBuilder.AppendLine("Dwelling / Zone\tSpace\tPart F required (l/s)");
                foreach (PartOSpaceRow row in spaceRows)
                {
                    stringBuilder.AppendLine(string.Format("{0}\t{1}\t{2}", row.Dwelling, row.Name, PartOAirFlowConverter.Text(row.PartFRequired_Lps)));
                }
            }
            else
            {
                stringBuilder.AppendLine("Dwelling / Zone\tSpace\tPart F required (l/s)\tDesign SUP (l/s)\tDesign EXT (l/s)");
                foreach (PartOSpaceRow row in spaceRows)
                {
                    stringBuilder.AppendLine(string.Format(
                        "{0}\t{1}\t{2}\t{3}\t{4}",
                        row.Dwelling,
                        row.Name,
                        PartOAirFlowConverter.Text(row.PartFRequired_Lps),
                        PartOAirFlowConverter.Text(row.DesignSupply_Lps),
                        PartOAirFlowConverter.Text(row.DesignExtract_Lps)));
                }
            }

            stringBuilder.AppendLine();

            //THE COMPLETE RECORD, never what the box happens to be showing: the grouped view exists to be
            //read quickly and Copy All exists to be pasted into an issue, a report or an email. A copy that
            //silently dropped a repeated warning would be the one place this aggregation could do harm.
            stringBuilder.AppendLine(partODiagnosticSummary.Text);

            return stringBuilder.ToString();
        }

        private void button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CopyAllText());
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                //The clipboard is held by another process. Nothing about the preparation depends on it.
            }
        }

        /// <summary>
        /// The window's only "yes" - "Accept &amp; Run TAS" or "Accept Preparation" by <see cref="Intent"/>. The
        /// caller then does exactly what it did after OK: Prepare &amp; Run adopts and simulates, the Prepare
        /// Iteration command adopts only. Only the name changed.
        /// </summary>
        private void button_Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
