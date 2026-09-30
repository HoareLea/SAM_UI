// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The one Approved Document O dialog a person who is not a SAM developer should need: pick a scenario
    /// and a scope, read what the model already provides, and run it.
    ///
    /// <para><b>What it replaces</b></para>
    /// <para>
    /// Not the expert commands - they all remain, and the note at the bottom of the window names them. What
    /// it replaces is the requirement to KNOW them: which of Map IC (TM59), AddVent PartF, Prepare Iteration,
    /// Energy Simulation and Overheating (TM59) have already been run over this model, in which order, and
    /// which of them the next step is about to refuse for.
    /// </para>
    ///
    /// <para><b>It computes nothing about the building</b></para>
    /// <para>
    /// Every status line is <see cref="PartOWorkflowInspection"/>'s, and every one of those is an existing
    /// authority's answer. The window owns three things and no more: which controls are enabled, what the
    /// dwelling list is filtered to, and which command the chosen button invokes.
    /// </para>
    ///
    /// <para><b>Built for a large model</b></para>
    /// <para>
    /// The dwelling list is one row per DWELLING ZONE and never one per space, over the same
    /// <see cref="PartODwellingSelection"/> the Prepare Iteration picker uses - virtualized, filtered in
    /// place, selection held on the record. A Select All, a None or a restored scope is ONE selection change
    /// however many dwellings it moves, never one per row.
    /// </para>
    /// <para>
    /// <b>The analytical model is inspected only when an inspection input moved.</b> A change of scenario,
    /// scope, dwelling selection, model, run, catalogue or session capability rebuilds the status list
    /// (<see cref="Refresh"/>); a workflow input such as the Simulation case, and the search text, cannot
    /// move a single stage, so they re-derive only this dialog's own state
    /// (<see cref="RefreshWorkflowInput"/>) over the inspection already built. Nothing here touches the
    /// filesystem or the catalogue, both of which the caller reads once and hands in through
    /// <see cref="Capabilities"/>.
    /// </para>
    /// <para>
    /// What ONE rebuild costs is the authorities' own cost - <c>Query.PartFRequiredFlowRate_Lps</c> resolves
    /// a space through the cluster per call, and this window does not second-guess it or cache its answers.
    /// So the guarantee this window makes is not that an inspection is a single pass; it is that a UI-only
    /// interaction does not ask for one at all.
    /// </para>
    /// <para>
    /// <b>And that opening the window is one interaction.</b> Setting the dialog up moves seven inspection
    /// inputs - the model, the run, the catalogue, the capabilities, then the restored scenario, scope and
    /// dwelling scope - and answering each of them separately inspected an initial state nobody would ever
    /// see. Those are deferred and paid once, over the fully restored state; see <see cref="initialising"/>.
    /// After that the window is eager again, and every genuine change inspects when it happens.
    /// </para>
    /// </summary>
    public partial class PartOWorkflowWindow : System.Windows.Window
    {
        private PartODwellingSelection dwellingSelection = new([]);

        private List<Zone> zones_Eligible = [];

        /// <summary>
        /// Whether THIS WINDOW is currently writing to the equipment-selection control, rather than a person
        /// moving it.
        /// <para>
        /// The control raises <c>SelectionChanged</c> for either, and this window answers that with a full
        /// inspection - a pass over every dwelling in scope. So seeding the control from the model, or
        /// handing it a catalogue, would buy a second inspection for one gesture, which on a five thousand
        /// space model is exactly the cost <see cref="PartOWorkflowInitialisationTests"/> exists to keep out.
        /// Each write site suppresses the event and pays the one inspection it owes itself.
        /// </para>
        /// </summary>
        private bool writing_EquipmentSelection;

        private AnalyticalModel? analyticalModel;

        private PartORun? partORun;

        private VentilationUnitCatalogue? ventilationUnitCatalogue;

        private PartOWorkflowCapabilities partOWorkflowCapabilities = new();

        /// <summary>
        /// The TM59 keyword map, read ONCE for the life of the dialog.
        /// <para>
        /// <c>Query.DefaultInternalConditionTextMap_TM59</c> falls back to reading and parsing a resource
        /// file where the session's settings do not carry the map, and the status list is rebuilt on every
        /// change of scenario, scope or dwelling selection - so asking it per rebuild would put a file read
        /// behind a checkbox. Null is a valid value and the inspection reports it as such.
        /// </para>
        /// </summary>
        private readonly TextMap textMap_TM59 = Analytical.Query.DefaultInternalConditionTextMap_TM59();

        private bool loaded;

        /// <summary>
        /// Whether the dialog is still being set up, and therefore whether an inspection input moving should
        /// inspect now or be answered once at the end.
        ///
        /// <para><b>The problem this exists for</b></para>
        /// <para>
        /// Opening the hub is one gesture, and it moved seven inspection inputs one at a time. The
        /// constructor settled the controls; the caller then set the model, the run, the catalogue and the
        /// session capabilities; and <see cref="Restore"/> then put back the scenario, the scope and the
        /// saved dwelling scope, each through the very control events the window answers with a full
        /// inspection. Every one of those was a correct response to a genuine change, and all but the last
        /// was a response to a state nobody would ever see - <b>nine inspections of a model to show one
        /// window</b>, eight of them of an initial state that had already been superseded before it was
        /// drawn. On a five thousand space project every one of them walks the dwelling scope.
        /// </para>
        ///
        /// <para><b>It is a deferral, not a cache</b></para>
        /// <para>
        /// Nothing is remembered, compared or reused: the pending flag says an inspection is owed, and when
        /// it is paid it is a full inspection of whatever the window then holds, asking every authority
        /// exactly what it asked before. There is no stored engineering answer here and no attempt to decide
        /// whether an input "really" changed - that would be a second opinion about the model, which is the
        /// thing this window is not allowed to have.
        /// </para>
        ///
        /// <para><b>It ends by itself, and after it ends the window is eager again</b></para>
        /// <para>
        /// Initialisation ends at the first moment the answer is actually needed - the window being shown,
        /// or any derived state being read - as well as at <see cref="CompleteInitialisation"/>, which the
        /// caller calls when it has finished setting the dialog up. From that point every genuine change of
        /// scenario, scope, dwelling selection, model, run, catalogue or capability inspects immediately, as
        /// it always did: a status list that updated only when somebody happened to read it would be a
        /// window showing the scope the user came from.
        /// </para>
        /// </summary>
        private bool initialising = true;

        private bool refresh_Pending;

        /// <summary>
        /// Whether a rebuild is already running, so that a control this window writes <b>during</b> one is not
        /// mistaken for a person moving an inspection input.
        ///
        /// <para><b>The re-entrancy this closes, which predates the deferral above</b></para>
        /// <para>
        /// A rebuild writes controls whose events this window answers - the scenario-dependent equipment
        /// controls, for instance - and a nested rebuild from inside one would inspect the model a second time
        /// before the first has produced the inspection it is about to produce. (Historically this was the
        /// Iteration 2B tick, cleared on a scenario that cannot carry one; that tick no longer exists.)
        /// </para>
        /// <para>
        /// Suppressing the nested rebuild loses nothing. The outer one has not reached
        /// <c>PartOWorkflowInspection.Inspect</c> yet, and it derives the actions afterwards, from the
        /// inspection it then produces and over the controls as it left them.
        /// </para>
        /// </summary>
        private bool refreshing;

        /// <summary>
        /// The status rows the last rebuild produced, in the inspection's own order, and the two groups the
        /// list is rendered as. Held only so a test can read them; the grouping is presentation and the rows
        /// are the inspection's, unaltered.
        /// </summary>
        private List<PartOWorkflowStatusRow> statusRows = [];

        private List<PartOWorkflowStatusGroup> statusGroups = [];

        /// <summary>
        /// Whether every status row shows the inspection's complete sentence under its compact line. One
        /// switch for the whole list, rather than a disclosure on every row. Presentation only.
        /// </summary>
        public static readonly DependencyProperty ShowStatusDetailsProperty = DependencyProperty.Register(nameof(ShowStatusDetails), typeof(bool), typeof(PartOWorkflowWindow), new PropertyMetadata(false, (d, e) => ((PartOWorkflowWindow)d).RenderLastOutcome()));

        public bool ShowStatusDetails
        {
            get => (bool)GetValue(ShowStatusDetailsProperty);
            set => SetValue(ShowStatusDetailsProperty, value);
        }

        public PartOWorkflowWindow()
        {
            InitializeComponent();

            InitialiseIteration3();

            //The ceiling the auto-sizing gives way to the scroller at. Read from the work area, so an
            //enlarged system font or a small screen degrades to scrolling with the buttons reachable.
            MaxHeight = SystemParameters.WorkArea.Height * 0.92;

            SizeChanged += OnGrown;

            comboBox_Scenario.ItemsSource = PartOWorkflowScenario.Scenarios;
            comboBox_Scenario.SelectedIndex = 0;
            comboBox_Scenario.SelectionChanged += (s, e) =>
            {
                //A person choosing another scenario is no longer looking at Iteration 3, so the primary
                //result action goes back to that scenario's own results. Programmatic restores happen before
                //the window is shown and leave the focus as the caller stated it.
                if (IsVisible)
                {
                    iteration3InFocus = false;
                }

                Refresh();
            };

            List<PartOWorkflowScope> scopes = [PartOWorkflowScope.AllDwellings, PartOWorkflowScope.SelectedDwellings, PartOWorkflowScope.SelectedDwellingsIsolated];

            comboBox_Scope.ItemsSource = scopes.ConvertAll(x => Core.Query.Description(x));
            comboBox_Scope.SelectedIndex = 0;
            comboBox_Scope.SelectionChanged += (s, e) => Refresh();

            //A FULL refresh, unlike the Iteration 2B inputs below. The mode and the pool are preparation
            //inputs: they decide which products a preparation selects, so a change can move whether the
            //already-prepared iteration is still the one being asked for - see PartOWorkflowInspection.
            //
            //Except when this window is the one writing: see the field.
            control_EquipmentSelection.SelectionChanged += (s, e) =>
            {
                if (!writing_EquipmentSelection)
                {
                    Refresh();
                }
            };

            //Iteration 2B has no inputs here: its step, round limit and envelope are confirmed when it starts
            //(Modify.RunPartOOptimisationResult), because they are not preparation inputs.

            //The search narrows the VIEW, never the selection: a dwelling filtered out of sight keeps its
            //state and reappears with it intact.
            textBox_Search.TextChanged += (s, e) =>
            {
                dwellingSelection.SearchText = textBox_Search.Text;

                (listBox_Dwellings.ItemsSource as ICollectionView)?.Refresh();

                //Deliberately NOT a full Refresh: the search changes what is visible, never what is
                //selected, so no stage's status can have moved. On a block with thousands of dwellings a
                //per-keystroke re-inspection would be a pass over every space in scope for nothing - which
                //is why setting SearchText raises PartODwellingSelection.SearchTextChanged and not its
                //SelectionChanged, the event this window answers with a full inspection.
                UpdateSelectionText();
            };

            button_SelectAll.Click += (s, e) => dwellingSelection.SetSelected(true);
            button_SelectNone.Click += (s, e) => dwellingSelection.SetSelected(false);

            loaded = true;

            Refresh();
        }

        /// <summary>
        /// The model this workflow runs over. Setting it asks SAM which of its zones are dwellings - once -
        /// and builds the selectable scope from that answer.
        /// </summary>
        public AnalyticalModel? AnalyticalModel
        {
            set
            {
                analyticalModel = value;

                //The policy call, not a local IsDwelling filter. Asked here, once; nothing re-asks it per
                //click or per keystroke.
                zones_Eligible = Analytical.Query.PartFDwellingZones(value?.GetZones() ?? []) ?? [];

                dwellingSelection = new PartODwellingSelection(zones_Eligible);

                //The SCOPE moving is a genuine inspection input - different dwellings are different spaces,
                //different Part F requirements and a different preparation match - so this one is answered
                //with the full refresh. It is raised once per gesture, never once per row.
                dwellingSelection.SelectionChanged += (s, e) => Refresh();

                ICollectionView view = CollectionViewSource.GetDefaultView(dwellingSelection.Items);
                view.Filter = item => dwellingSelection.IsVisible((PartODwellingSelection.Item)item);

                listBox_Dwellings.ItemsSource = view;

                //THE PROJECT's own equipment preselection, restored off the model it belongs to - the same
                //parameter the single-command Prepare Iteration window reads and Modify.PreparePartOIteration
                //writes. This is the whole of "changing it in one workflow is reflected in the other": there
                //is one statement, on the project, and both windows show it. Absent reads as the historic
                //default. See PartOEquipmentSelection.
                //
                //Assigned only where a catalogue is already to hand; where the caller sets the catalogue
                //afterwards, that setter re-seeds. Either order gives the same answer.
                if (ventilationUnitCatalogue is not null)
                {
                    WriteEquipmentSelection(
                        value?.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection),
                        value?.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit),
                        value);
                }

                Refresh();
            }
        }

        /// <summary>The session's Part O run. Read for what is already prepared or already assessable.</summary>
        public PartORun? PartORun
        {
            set
            {
                partORun = value;

                //Not an inspection input for the line: it reads the run's state, which is cheap and touches no file.
                RenderLastOutcome();

                Refresh();
            }
        }

        /// <summary>
        /// The catalogue this session read. Read once by the caller, because reading it touches a file and
        /// this window rebuilds its status whenever the requested scenario or scope moves.
        /// </summary>
        public VentilationUnitCatalogue? VentilationUnitCatalogue
        {
            set
            {
                ventilationUnitCatalogue = value;

                writing_EquipmentSelection = true;

                try
                {
                    control_EquipmentSelection.VentilationUnitCatalogue = value;

                    //Re-seeded, because the pool is restored by ticking catalogue rows and those rows did
                    //not exist until now. RunPartOWorkflow sets the model first and the catalogue second;
                    //doing it here as well makes the window correct under either order.
                    //
                    //The test product FIRST, for the same reason one step deeper: its row does not exist
                    //until it has been stated, and the pool is restored by ticking rows.
                    control_EquipmentSelection.ProjectTestVentilationUnit = analyticalModel?.GetValue<PartOProjectTestVentilationUnit>(Analytical.AnalyticalModelParameter.PartOProjectTestVentilationUnit);
                    control_EquipmentSelection.ProjectTestVentilationUnitAssignmentCount = Query.PartOVentilationUnitAssignmentCount(analyticalModel, control_EquipmentSelection.ProjectTestVentilationUnit?.VentilationUnitReference);

                    control_EquipmentSelection.EquipmentSelection = analyticalModel?.GetValue<PartOEquipmentSelection>(Analytical.AnalyticalModelParameter.PartOEquipmentSelection);
                }
                finally
                {
                    writing_EquipmentSelection = false;
                }

                Refresh();
            }
        }

        /// <summary>
        /// The session facts no model can answer - whether there are results to review, whether Iteration 2B
        /// can start, and why not. Supplied by the caller from the authorities that own them.
        /// </summary>
        public PartOWorkflowCapabilities? Capabilities
        {
            set
            {
                partOWorkflowCapabilities = value ?? new PartOWorkflowCapabilities();

                RenderLastOutcome();

                Refresh();
            }
        }

        /// <summary>What the window was closed to do. <see cref="PartOWorkflowAction.None"/> where it was closed.</summary>
        public PartOWorkflowAction Action { get; private set; } = PartOWorkflowAction.None;

        /// <summary>
        /// The project's equipment preselection as this window currently states it - asked of the control
        /// that owns it, exactly as <c>PartOIterationWindow</c> does.
        ///
        /// <para><b>The same configuration, not a copy of it</b></para>
        /// <para>
        /// It is read off the model when one is set and written back by
        /// <c>Modify.PreparePartOIteration</c> onto
        /// <c>AnalyticalModelParameter.PartOEquipmentSelection</c>, so a mode or pool chosen here is what
        /// the single-command Prepare Iteration window shows next time, and the other way round. There is
        /// no Prepare &amp; Run-only pool and no application-wide preference.
        /// </para>
        /// </summary>
        public PartOEquipmentSelection EquipmentSelection
        {
            get
            {
                return control_EquipmentSelection.EquipmentSelection;
            }
            set
            {
                WriteEquipmentSelection(value);

                Refresh();
            }
        }

        /// <summary>The selection authority this window currently states.</summary>
        public PartOEquipmentSelectionMode Mode => control_EquipmentSelection.Mode;

        /// <summary>
        /// The project's own test ventilation unit as this window currently states it - the SAME
        /// project-scoped statement the single-command Prepare Iteration window reads and writes, held in
        /// the same shared control. There is no Prepare &amp; Run-only what-if.
        /// </summary>
        public PartOProjectTestVentilationUnit? ProjectTestVentilationUnit
        {
            get
            {
                return control_EquipmentSelection.ProjectTestVentilationUnit;
            }
            set
            {
                WriteEquipmentSelection(EquipmentSelection, value, analyticalModel);

                Refresh();
            }
        }

        /// <summary>What this window currently says about the project test product.</summary>
        public string ProjectTestDescription => control_EquipmentSelection.ProjectTestDescription;

        /// <summary>The catalogue rows, so a test can read exactly what the engineer can see.</summary>
        internal List<PartOCatalogueProductRow> CatalogueProductRows => control_EquipmentSelection.CatalogueProductRows;

        /// <summary>What this window currently says about the catalogue and the current mode.</summary>
        public string CatalogueDescription => control_EquipmentSelection.CatalogueDescription;

        /// <summary>Whether the permitted products can currently be ticked.</summary>
        internal bool IsPoolEditable => control_EquipmentSelection.IsPoolEditable;

        /// <summary>
        /// Re-applies the choices a previous showing of this dialog was closed with, so a person who runs a
        /// baseline and comes back to review or optimise it is not silently returned to the defaults.
        /// <para>
        /// <b>Choices only.</b> Nothing about the run's state is restored - the status list is rebuilt from
        /// the model and the run every time, so restoring a scope that no longer has dwellings in it shows
        /// the blocker rather than a stale READY.
        /// </para>
        /// <para>
        /// A dwelling guid that is no longer an eligible dwelling is silently dropped: the model may have
        /// been re-zoned between the two showings, and selecting a zone that is not offered would be a scope
        /// the preparation cannot honour.
        /// </para>
        /// </summary>
        public void Restore(PartOWorkflowScenario? partOWorkflowScenario, PartOWorkflowScope partOWorkflowScope, IEnumerable<Guid>? guids_Dwelling)
        {
            if (partOWorkflowScenario is not null)
            {
                foreach (PartOWorkflowScenario item in comboBox_Scenario.ItemsSource)
                {
                    if (item.Option?.PartOIteration == partOWorkflowScenario.Option?.PartOIteration && item.SelectVentilationUnit == partOWorkflowScenario.SelectVentilationUnit)
                    {
                        comboBox_Scenario.SelectedItem = item;

                        break;
                    }
                }
            }

            Scope = partOWorkflowScope;

            if (guids_Dwelling is not null)
            {
                //One batched selection change, not one per flipped dwelling. Restoring a narrowed scope on a
                //block-scale model flips hundreds of rows, and this window answers a selection change with a
                //full inspection - so a row-by-row restore ran that inspection hundreds of times.
                dwellingSelection.RestoreSelection(guids_Dwelling);
            }

            Refresh();
        }

        /// <summary>The chosen scenario - a base provision plus whether a manufacturer unit is selected.</summary>
        public PartOWorkflowScenario? Scenario => comboBox_Scenario.SelectedItem as PartOWorkflowScenario;

        /// <summary>
        /// The status rows as the window built them, in the inspection's own order. Exposed for tests, which
        /// have to be able to assert that grouping the list moved no status and lost no row.
        /// </summary>
        internal List<PartOWorkflowStatusRow> StatusRows
        {
            get { EnsureInspected(); return statusRows; }
        }

        /// <summary>The two presentation groups the status list is rendered as. Exposed for tests.</summary>
        internal List<PartOWorkflowStatusGroup> StatusGroups
        {
            get { EnsureInspected(); return statusGroups; }
        }

        /// <summary>
        /// Whether the equipment section is showing its compact 1a / 1b summary rather than the full,
        /// actionable controls. Exposed for tests.
        /// </summary>
        internal bool IsEquipmentSelectionCompact
        {
            get { EnsureInspected(); return control_EquipmentSelection.IsCompact; }
        }

        /// <summary>The chosen scope.</summary>
        public PartOWorkflowScope Scope
        {
            get
            {
                return comboBox_Scope.SelectedIndex switch
                {
                    1 => PartOWorkflowScope.SelectedDwellings,
                    2 => PartOWorkflowScope.SelectedDwellingsIsolated,
                    _ => PartOWorkflowScope.AllDwellings,
                };
            }
            set
            {
                comboBox_Scope.SelectedIndex = value switch
                {
                    PartOWorkflowScope.SelectedDwellings => 1,
                    PartOWorkflowScope.SelectedDwellingsIsolated => 2,
                    _ => 0,
                };
            }
        }

        /// <summary>
        /// The dwelling zones the run covers.
        /// <para>
        /// On <see cref="PartOWorkflowScope.AllDwellings"/> this is SAM's own answer unmodified, whatever the
        /// list below happens to be ticked to - the scope control is the authority, not a stale set of
        /// checkboxes. On the two selected scopes it is that answer as the user narrowed it.
        /// </para>
        /// </summary>
        public List<Zone> Zones_Dwelling => Scope == PartOWorkflowScope.AllDwellings ? [.. zones_Eligible] : dwellingSelection.SelectedZones();

        /// <summary>The selection model behind the dwelling list. Exposed for tests.</summary>
        internal PartODwellingSelection DwellingSelection => dwellingSelection;

        /// <summary>
        /// The dwelling search exactly as typed. Exposed for tests, which have to reach the real
        /// <c>TextChanged</c> handler rather than the selection model behind it - the whole point of the
        /// search is what that handler does and does not do.
        /// </summary>
        internal string SearchText
        {
            get => textBox_Search.Text;
            set => textBox_Search.Text = value;
        }

        /// <summary>
        /// How many dwellings the bound list currently shows - the filtered view itself, not the predicate.
        /// Exposed so a test can assert that a search really narrowed what a person sees.
        /// </summary>
        internal int VisibleDwellingCount
        {
            get
            {
                int result = 0;

                if (listBox_Dwellings.ItemsSource is ICollectionView view)
                {
                    foreach (object item in view)
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        /// <summary>What the window says about the current selection and search. Exposed for tests.</summary>
        internal string SelectionDescription
        {
            get { EnsureInspected(); return textBlock_Selection.Text; }
        }

        /// <summary>
        /// Everything the run needs, in the shape the preparation seam takes.
        /// <para>
        /// <b>The request is the user's INTENT, never what this machine happens to be able to do.</b>
        /// <c>SelectVentilationUnit</c> is read off the chosen scenario alone: Iteration 2 stays an
        /// Iteration 2 request when no catalogue can be read, and the missing catalogue is reported as the
        /// blocker it is (<see cref="PartOWorkflowInspection"/>'s Equipment stage, from
        /// <see cref="PartOWorkflowCapabilities.EquipmentAvailable"/>). Folding the capability into the
        /// intent here silently downgraded the requested run to Iteration 1a - a different assessment,
        /// with no equipment selection and no Iteration 2B after it - and reported success.
        /// </para>
        /// </summary>
        public PartOWorkflowRequest Request
        {
            get
            {
                PartOWorkflowScenario? partOWorkflowScenario = Scenario;

                return new PartOWorkflowRequest(partOWorkflowScenario?.Option, Scope, Zones_Dwelling, partOWorkflowScenario is not null && partOWorkflowScenario.SelectVentilationUnit)
                {
                    EquipmentSelection = EquipmentSelection,
                    ProjectTestVentilationUnit = ProjectTestVentilationUnit,
                };
            }
        }

        /// <summary>The status the window is currently showing. Exposed so what the user is told is assertable.</summary>
        public PartOWorkflowInspection? Inspection
        {
            get { EnsureInspected(); return inspection; }

            private set { inspection = value; }
        }

        private PartOWorkflowInspection? inspection;

        /// <summary>What the window says about the chosen scenario. Exposed for tests.</summary>
        public string ScenarioDescription
        {
            get { EnsureInspected(); return textBlock_Scenario.Text; }
        }

        /// <summary>What the window says about the chosen scope. Exposed for tests.</summary>
        public string ScopeDescription
        {
            get { EnsureInspected(); return textBlock_Scope.Text; }
        }

        /// <summary>Why Run is unavailable, as one block of text. Empty where it is available. Exposed for tests.</summary>
        public string BlockerDescription
        {
            get { EnsureInspected(); return textBlock_Blockers.Text; }
        }

        /// <summary>What the window says about Iteration 2B on the chosen scenario. Exposed so it is assertable.</summary>
        public string OptimisationDescription
        {
            get { EnsureInspected(); return OptimisationScenarioText(Scenario); }
        }

        /// <summary>Whether Prepare and Run is currently offered. Exposed for tests.</summary>
        public bool CanRun
        {
            get { EnsureInspected(); return button_Run.IsEnabled; }
        }

        /// <summary>Whether Review Results is currently offered. Exposed for tests.</summary>
        public bool CanReviewResults
        {
            get { EnsureInspected(); return button_Review.IsEnabled; }
        }

        /// <summary>Whether Optimise (2B) is currently offered. Exposed for tests.</summary>
        public bool CanOptimise
        {
            get { EnsureInspected(); return button_Optimise.IsEnabled; }
        }

        /// <summary>Whether the Iteration 3 (A/B) action is currently offered. Exposed for tests.</summary>
        public bool CanRunIteration3
        {
            get { EnsureInspected(); return button_Iteration3.IsEnabled; }
        }

        /// <summary>What the Iteration 3 action would do, as the button says it. Exposed for tests.</summary>
        public string Iteration3ActionText
        {
            get { EnsureInspected(); return button_Iteration3.Content as string; }
        }

        /// <summary>
        /// Rebuilds every derived part of the window from the current controls: the scenario and scope notes,
        /// which controls apply, the status list, and which actions are offered.
        /// <para>
        /// <b>This is the expensive one, and it is called only when an INSPECTION input moved</b> - the
        /// scenario, the scope, the selected dwellings, the model, the run, the catalogue or the session
        /// capabilities. What it costs is whatever the authorities it asks cost over the spaces in scope; it
        /// reads no file and remembers nothing. Anything that changes only this dialog's own workflow input
        /// goes to <see cref="RefreshWorkflowInput"/> instead.
        /// </para>
        /// </summary>
        private void Refresh()
        {
            if (!loaded)
            {
                return;
            }

            //Still being set up: record that an inspection is owed and pay it once, over the final restored
            //state, rather than once per input the setup moves. See the field.
            if (initialising)
            {
                refresh_Pending = true;

                return;
            }

            //Already rebuilding: a control this window wrote is not a person moving an input. See the field.
            if (refreshing)
            {
                return;
            }

            refreshing = true;

            try
            {
                RefreshCore();
            }
            finally
            {
                refreshing = false;
            }
        }

        /// <summary>
        /// Ends the deferred-initialisation window: <b>one</b> inspection, of the state the dialog was set up
        /// into, and eager refreshing from then on.
        /// <para>
        /// Called by the caller once it has finished setting the dialog up, and by the window itself the
        /// moment the answer is needed - it is shown, or something derived from an inspection is read - so a
        /// caller that never calls it is never left looking at a window derived from nothing. Calling it
        /// twice does nothing the second time.
        /// </para>
        /// <para>
        /// It inspects only where an inspection is actually owed. Ending initialisation on a dialog nothing
        /// was set on does not manufacture one.
        /// </para>
        /// </summary>
        public void CompleteInitialisation()
        {
            if (!initialising)
            {
                return;
            }

            initialising = false;

            if (!refresh_Pending)
            {
                return;
            }

            refresh_Pending = false;

            Refresh();
        }

        /// <summary>
        /// Called from every member whose value a <see cref="RefreshCore"/> writes, so reading the window's
        /// derived state always ends initialisation first and reads the real answer rather than a blank one.
        /// </summary>
        private void EnsureInspected()
        {
            CompleteInitialisation();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            //Showing the window is the last possible moment: whatever else the caller did or did not do, what
            //a person is about to look at is inspected before they look at it.
            CompleteInitialisation();

            base.OnSourceInitialized(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            KeepOnScreen();

            placed = true;
        }

        /// <summary>Whether the first placement has run; growth after it is answered by <see cref="OnGrown"/>.</summary>
        private bool placed;

        /// <summary>
        /// The Hub grows after it has been placed - Show details, the Simulation case, a taller scenario -
        /// because its height follows its content. Growth that would take the action row below the working
        /// area moves it up again. Only while the height is still content-driven: once the person drags the
        /// grip WPF sets <c>SizeToContent</c> to <c>Manual</c>, and a size they chose is left alone.
        /// </summary>
        private void OnGrown(object sender, SizeChangedEventArgs e)
        {
            if (placed && e.HeightChanged && e.NewSize.Height > e.PreviousSize.Height && SizeToContent != SizeToContent.Manual)
            {
                KeepOnScreen();
            }
        }

        /// <summary>
        /// Keeps the Hub's action row inside the working area of the monitor it is on: when it first renders
        /// (found live: a reopened run opened at the owner's cascade position with its actions off-screen),
        /// and when its content makes it taller (<see cref="OnGrown"/>).
        /// <para>
        /// <b>Against the monitor it is actually on.</b> The constructor's height ceiling is read from the
        /// PRIMARY working area; on a shorter secondary monitor a Hub taller than that monitor could not be
        /// moved far enough, so the ceiling is lowered to this monitor's first - the content scrolls, and the
        /// action row sits outside the scroll region. On a monitor shorter than the window's own minimum
        /// height the minimum comes down with it, or WPF would hold the window at a height that does not fit.
        /// Neither is ever raised.
        /// </para>
        /// </summary>
        private void KeepOnScreen()
        {
            PartOWindowPlacement.KeepOnScreen(this);
        }

        /// <summary>
        /// Where a window of <paramref name="height"/> at <paramref name="top"/> goes to fit a working area
        /// running from <paramref name="areaTop"/> to <paramref name="areaBottom"/>.
        /// <list type="bullet">
        /// <item>The height ceiling is lowered to 92% of that area where it was higher - the constructor's
        /// own proportion.</item>
        /// <item>The minimum height is lowered to that ceiling where it was higher, so the window can shrink
        /// and scroll rather than be held taller than the monitor.</item>
        /// <item>The top moves up only as far as the height the window will really have - its height between
        /// that minimum and that ceiling - needs.</item>
        /// </list>
        /// Pure, so the arithmetic is testable without a second monitor.
        /// </summary>
        internal static (double Top, double MinHeight, double MaxHeight) Placement(double top, double height, double minHeight, double maxHeight, double areaTop, double areaBottom)
        {
            double maxHeight_Area = (areaBottom - areaTop) * 0.92;

            double maxHeight_Result = double.IsNaN(maxHeight) ? maxHeight_Area : Math.Min(maxHeight, maxHeight_Area);

            double minHeight_Result = double.IsNaN(minHeight) ? 0 : Math.Min(minHeight, maxHeight_Result);

            double height_Result = Math.Max(minHeight_Result, Math.Min(height, maxHeight_Result));

            double top_Result = top + height_Result > areaBottom
                ? Math.Max(areaTop, areaBottom - height_Result)
                : top;

            return (top_Result, minHeight_Result, maxHeight_Result);
        }

        /// <summary>
        /// How many times this window has inspected the analytical model since it was constructed.
        /// <para>
        /// <b>Exposed for tests, and it is the only honest way to assert the claim.</b> Object identity says
        /// an inspection did or did not happen between two reads; it cannot say that opening the dialog ran
        /// one rather than nine, which is exactly what the deferral above is for. A count is exact and the
        /// same on every machine; a stopwatch is neither.
        /// </para>
        /// </summary>
        internal int InspectionCount { get; private set; }

        private void RefreshCore()
        {
            InspectionCount++;

            PartOWorkflowScenario? partOWorkflowScenario = Scenario;

            //Before the scenario text, which asks the control what the active mode means.
            UpdateEquipmentSelectionControls();

            UpdateScenarioText(partOWorkflowScenario);
            UpdateScopeControls();

            PartOWorkflowCapabilities capabilities = new()
            {
                EquipmentAvailable = ventilationUnitCatalogue?.HasSelectableProducts ?? false,
                EquipmentDescription = ventilationUnitCatalogue?.Description ?? "The ventilation unit catalogue has not been read.",
                ResultsAvailable = partOWorkflowCapabilities.ResultsAvailable,
                ResultsRefusal = partOWorkflowCapabilities.ResultsRefusal,
                ResultsRestored = partOWorkflowCapabilities.ResultsRestored,
                Path_Results = partOWorkflowCapabilities.Path_Results,
                OptimisationAvailable = partOWorkflowCapabilities.OptimisationAvailable,
                OptimisationRefusal = partOWorkflowCapabilities.OptimisationRefusal,

                //A Part O result is reviewed, never run from (PR-4).
                DesignModelRefusal = partOWorkflowCapabilities.DesignModelRefusal,
            };

            PartOWorkflowInspection partOWorkflowInspection = PartOWorkflowInspection.Inspect(analyticalModel, Request, partORun, capabilities, textMap_TM59);

            Inspection = partOWorkflowInspection;

            List<PartOWorkflowStatusRow> rows = [];
            foreach (PartOWorkflowStageState partOWorkflowStageState in partOWorkflowInspection.Stages)
            {
                rows.Add(new PartOWorkflowStatusRow(partOWorkflowStageState, Summary(partOWorkflowStageState, partOWorkflowScenario, capabilities.ResultsAvailable)));
            }

            statusRows = rows;

            //The run group is only called an EXISTING run where results actually exist. The fact is the
            //capabilities' own, read rather than derived; see PartOWorkflowStatusGroup.
            statusGroups = PartOWorkflowStatusGroup.Groups(rows, capabilities.ResultsAvailable);

            //What is RENDERED is the same groups less one row: an Equipment stage that is not part of this
            //scenario. The route line under the scenario already says no manufacturer unit is required, and
            //that was the fourth place saying it. The row stays in StatusRows and StatusGroups, unaltered.
            itemsControl_Status.ItemsSource = PartOWorkflowStatusGroup.Groups(rows.FindAll(IsRendered), capabilities.ResultsAvailable);

            UpdateActions(partOWorkflowInspection);
        }

        /// <summary>Whether a status row is drawn. Every row is, except an Equipment stage that is N/A.</summary>
        private static bool IsRendered(PartOWorkflowStatusRow partOWorkflowStatusRow)
        {
            return !(partOWorkflowStatusRow.State?.Stage == PartOWorkflowStage.Equipment && partOWorkflowStatusRow.State.Status == PartOWorkflowStageStatus.NotApplicable);
        }

        /// <summary>The status groups as drawn, less the N/A Equipment row. Exposed for tests.</summary>
        internal List<PartOWorkflowStatusGroup> RenderedStatusGroups
        {
            get { EnsureInspected(); return itemsControl_Status.ItemsSource as List<PartOWorkflowStatusGroup> ?? []; }
        }

        /// <summary>The workflow strip as drawn. Exposed for tests.</summary>
        internal IReadOnlyList<PartOWorkflowStep> WorkflowSteps
        {
            get { EnsureInspected(); return stepStrip.Steps ?? []; }
        }

        /// <summary>The next-step line. Exposed for tests.</summary>
        internal string NextStepText
        {
            get { EnsureInspected(); return textBlock_NextStep.Text; }
        }

        /// <summary>The captions under the Review and Optimise actions. Exposed for tests.</summary>
        internal string ReviewCaption
        {
            get { EnsureInspected(); return textBlock_ReviewCaption.Text; }
        }

        internal string OptimiseCaption
        {
            get { EnsureInspected(); return textBlock_OptimiseCaption.Text; }
        }

        /// <summary>What the Optimise action's tooltip says. Exposed for tests.</summary>
        internal string? OptimiseToolTip
        {
            get { EnsureInspected(); return button_Optimise.ToolTip as string; }
        }

        /// <summary>Whether the Iteration 3 panel is shown at all. Exposed for tests.</summary>
        internal bool IsIteration3PanelVisible
        {
            get { EnsureInspected(); return border_Iteration3.Visibility == Visibility.Visible; }
        }

        /// <summary>Whether the equipment section is shown. Exposed for tests.</summary>
        internal bool IsEquipmentSectionVisible
        {
            get { EnsureInspected(); return stackPanel_EquipmentSelection.Visibility == Visibility.Visible; }
        }

        /// <summary>
        /// Re-derives only what this dialog's own workflow input can change: the combined Run blocker line and
        /// which actions are offered.
        /// <para>
        /// <b>It reuses the inspection already built and never asks for another one.</b> A workflow input -
        /// today the Simulation case - cannot move the dwelling scope, the TM59 mapping, the Approved Document
        /// F requirements, the prepared ventilation design, the equipment availability, the model-check state,
        /// the simulation or results state, or the engineering-preparation reuse match, and nothing
        /// <see cref="PartOWorkflowInspection.Inspect"/> asks reads it. Re-inspecting on a keystroke would
        /// re-ask every authority over every space in scope.
        /// </para>
        /// <para>
        /// Falls back to the full <see cref="Refresh"/> only where there is no inspection to reuse yet, so
        /// the window is never left showing actions derived from nothing.
        /// </para>
        /// <para>
        /// <b>It does nothing at all while the dialog is still being set up</b>, and it reads the inspection
        /// off the field rather than the property: going through the property would end the deferral - and
        /// inspect - over a state the set-up had not finished building. There is nothing to lose by returning:
        /// a full refresh is already owed, and it derives everything this does.
        /// </para>
        /// </summary>
        private void RefreshWorkflowInput()
        {
            //`refreshing` for the same reason Refresh checks it: the rebuild already running writes these
            //very controls and derives everything below afterwards.
            if (!loaded || initialising || refreshing)
            {
                return;
            }

            PartOWorkflowInspection? partOWorkflowInspection = inspection;
            if (partOWorkflowInspection is null)
            {
                Refresh();

                return;
            }

            UpdateActions(partOWorkflowInspection);
        }

        /// <summary>
        /// Which actions are offered, and why not - from the supplied inspection plus this dialog's own
        /// workflow-input refusal. Shared by the full and the lightweight refresh so both state exactly the
        /// same rule.
        /// </summary>
        private void UpdateActions(PartOWorkflowInspection partOWorkflowInspection)
        {
            //Two kinds of reason, kept apart on purpose. The inspection's blockers are what the MODEL and the
            //run do not provide; a workflow-input refusal is what was typed into THIS dialog and cannot be
            //used. Both stop Run, and the text says which is which.
            List<string> reasons = [.. partOWorkflowInspection.Blockers];

            //The Simulation case is a workflow input: typed here, checked here - the same checks the Simulate
            //dialog's OK made - and never a statement about the building.
            string? refusal_SimulationCase = SimulationCaseRefusal;
            if (refusal_SimulationCase is not null)
            {
                reasons.Add(string.Format("Simulation case: {0}", refusal_SimulationCase));
            }

            bool canRun = reasons.Count == 0;

            textBlock_Blockers.Text = canRun
                ? string.Empty
                : string.Format("Run is unavailable: {0}", string.Join(" ", reasons));

            textBlock_Blockers.Visibility = canRun ? Visibility.Collapsed : Visibility.Visible;

            button_Run.IsEnabled = canRun;
            button_Run.ToolTip = canRun
                ? (partOWorkflowInspection.ReusePreparation
                    ? "Simulate the iteration already prepared for exactly this scenario and scope, then assess it against the CIBSE TM59 criteria."
                    : "Prepare the iteration, check the model, run the full-year TAS simulation and assess it against the CIBSE TM59 criteria.")
                : textBlock_Blockers.Text;

            reviewResults_Enabled = partOWorkflowInspection.CanReviewResults;
            reviewResults_ToolTip = partOWorkflowInspection.CanReviewResults
                ? "Read this run's existing simulation results and show the CIBSE TM59 assessment. No new simulation is run."
                : partOWorkflowInspection.ResultsRefusal ?? "There are no results to review yet.";
            reviewResults_Caption = partOWorkflowInspection.CanReviewResults ? string.Empty : "No results yet";

            //Why a secondary action is unavailable, in two or three words under it. The complete reason is
            //the tooltip; these name only which of the two known conditions applies.
            bool supportsOptimisation = Scenario?.SupportsOptimisation ?? false;

            //On a scenario that can never carry an Iteration 2B, the tooltip gives THAT reason rather than the
            //missing results, which read as though a run would make it available. Where it is available, the
            //tooltip says what it does and that its settings are confirmed before anything runs.
            button_Optimise.IsEnabled = partOWorkflowInspection.CanOptimise;
            button_Optimise.ToolTip = partOWorkflowInspection.CanOptimise
                ? "Optimise ventilation (Iteration 2B): raise the design airflow of the spaces that fail TM59 by a fixed step within the selected ventilation units, rebalance, re-simulate the same full year and reassess, until they pass or a limit is reached. You confirm the step and the round limit before it starts. The selected product is never changed."
                : !supportsOptimisation && !partOWorkflowInspection.CanReviewResults
                    ? OptimisationScenarioText(Scenario)
                    : partOWorkflowInspection.OptimisationRefusal ?? "Iteration 2B optimises a completed Iteration 2 run.";

            //Where a run with results exists, the reason 2B is unavailable is THAT run's - the authority's
            //refusal above - whatever scenario the box shows for the next run (live acceptance, 26 Sep: a
            //reopened Iteration 2 run, with the box back on its first scenario, read "Iteration 2 only").
            textBlock_OptimiseCaption.Text = partOWorkflowInspection.CanOptimise
                ? "Optimise ventilation"
                : partOWorkflowInspection.CanReviewResults && partOWorkflowCapabilities.ResultsRestored
                    ? "Needs a live run"
                    : partOWorkflowInspection.CanReviewResults
                        ? "Not available"
                        : supportsOptimisation ? "After an Iteration 2 run" : "Iteration 2 only";

            //The Iteration 3 panel's own actions, from the eligibility the caller gathered once - it touches
            //the filesystem (it looks for saved results), and a status list rebuilt on every keystroke must not.
            //It also decides which case the primary Review action opens (UpdateReviewTarget).
            RefreshIteration3();

            stepStrip.Steps = PartOWorkflowProgress.Steps(partOWorkflowInspection, supportsOptimisation);

            UpdateNextStep(partOWorkflowInspection, reasons.Count);
        }

        /// <summary>
        /// The one line above the actions saying what happens next - from the inspection and the Iteration 3
        /// eligibility the window already holds, never from a state of its own.
        /// </summary>
        private void UpdateNextStep(PartOWorkflowInspection partOWorkflowInspection, int count_Blocking)
        {
            string blocked = count_Blocking == 1
                ? "✕ Prepare & Run is unavailable — resolve the blocking item shown above."
                : string.Format("✕ Prepare & Run is unavailable — resolve the {0} blocking items shown above.", count_Blocking);

            //Results that can be reviewed stay the next step whatever the current inputs say: Review reads the
            //existing run and does not depend on them (Codex review on #111). A blocker is still stated beside
            //it, because Prepare & Run is what it stops.
            if (partOWorkflowInspection.CanReviewResults)
            {
                string text = "Next: Review the TM59 results.";

                if (iteration3Eligibility?.CanRun ?? false)
                {
                    text += " Iteration 3 can then compare them with an explicit ventilation system (below).";
                }

                if (partOWorkflowInspection.CanOptimise)
                {
                    text += " Or optimise ventilation with Iteration 2B (Optimise (2B)): it raises the design airflow of the failing spaces within the selected units and re-simulates.";
                }

                if (count_Blocking != 0)
                {
                    text += " " + blocked;
                }

                textBlock_NextStep.Text = text;
                textBlock_NextStep.ClearValue(TextBlock.ForegroundProperty);

                return;
            }

            if (count_Blocking != 0)
            {
                textBlock_NextStep.Text = blocked;
                textBlock_NextStep.Foreground = Brushes.Firebrick;

                return;
            }

            textBlock_NextStep.ClearValue(TextBlock.ForegroundProperty);

            textBlock_NextStep.Text = partOWorkflowInspection.ReusePreparation
                ? "Next: Run the Part O assessment on the prepared ventilation design."
                : "Next: Prepare the ventilation design and run the Part O assessment.";
        }

        /// <summary>
        /// A shorter one-line sentence for a status row, where this window has one that reads better than
        /// the first sentence of the inspection's own detail. Null everywhere else.
        ///
        /// <para><b>Only the Part F N/A line, and only the natural-ventilation route</b></para>
        /// <para>
        /// That row's detail is four sentences of carefully worded analytical text - it has to be, because
        /// it is the sentence that proves no mechanical system was invented AND that System 1 provision was
        /// not sized either - and it sat permanently open across the middle of the status list on every
        /// Iteration 1b run. This states the fact in one line and leaves every word of the original behind
        /// the row's own "Why is this N/A?" disclosure and on its tooltip.
        /// </para>
        /// <para>
        /// <b>The route is read off the chosen scenario, never off the detail text.</b> Nothing here parses,
        /// classifies or re-derives what the inspection said: where the scenario is not the natural
        /// ventilation route - an unsettled route, say, whose detail is a different sentence entirely -
        /// this returns null and the row shortens itself by cutting at its own first full stop.
        /// </para>
        /// <para><b>And the ventilation design beside results that already exist</b></para>
        /// <para>
        /// A reopened or completed run can honestly report the design as still to prepare - the next run
        /// rebuilds it - while the simulation and results are ready. Read against "Check &amp; simulate: Done"
        /// on the strip, "Built by Prepare &amp; Run" looked like a simulation that ran without a design. This
        /// says what the status means in that case. The status is the inspection's and does not move; the
        /// fact that results exist is the capabilities' own answer, read rather than derived.
        /// </para>
        /// </summary>
        private static string? Summary(PartOWorkflowStageState partOWorkflowStageState, PartOWorkflowScenario? partOWorkflowScenario, bool resultsAvailable)
        {
            if (partOWorkflowStageState is not null && resultsAvailable
                && partOWorkflowStageState.Stage == PartOWorkflowStage.VentilationDesign
                && partOWorkflowStageState.Status == PartOWorkflowStageStatus.Prepare)
            {
                return "Rebuilt for the next Prepare & Run · the existing results are reviewable as they are";
            }

            if (partOWorkflowStageState is null || partOWorkflowStageState.Stage != PartOWorkflowStage.PartFRequirements)
            {
                return null;
            }

            if (partOWorkflowStageState.Status != PartOWorkflowStageStatus.NotApplicable)
            {
                return null;
            }

            return partOWorkflowScenario?.Option?.PartOVentilationMode == PartOVentilationMode.NaturalVentilation
                ? "Continuous mechanical rates are not applied on the natural-ventilation route."
                : null;
        }

        private void UpdateScenarioText(PartOWorkflowScenario? partOWorkflowScenario)
        {
            if (partOWorkflowScenario?.Option is null)
            {
                textBlock_Scenario.Text = string.Empty;

                return;
            }

            //The route word is SAM's, carried by the option; this states it rather than choosing it - once,
            //in one line, with what the route does about equipment. It is the only place the window says
            //that an Iteration 1 route needs no manufacturer unit.
            string route = partOWorkflowScenario.Option.VentilationStrategy;

            //ASKED OF THE ACTIVE MODE, never assumed. This line used to say "the smallest capable
            //manufacturer unit is selected per dwelling" whatever the project's configuration was - which
            //is simply untrue of a project under manual authority, and untrue of a narrowed pool. An
            //engineer reading it would have believed a selection had run that had not.
            textBlock_Scenario.Text = partOWorkflowScenario.SelectVentilationUnit
                ? string.Format("{0} · Manufacturer unit per dwelling · {1}", route, control_EquipmentSelection.ModeDescription)
                : partOWorkflowScenario.Option.PartOVentilationMode == PartOVentilationMode.MVHR
                    ? string.Format("{0} · Design duty only · No manufacturer unit required", route)
                    : string.Format("{0} · No mechanical system, unit or terminal", route);
        }

        /// <summary>
        /// Writes the preselection into the control without the window answering its own write with an
        /// inspection. Every caller pays exactly one <see cref="Refresh"/> of its own. See the field.
        /// </summary>
        private void WriteEquipmentSelection(PartOEquipmentSelection? partOEquipmentSelection)
        {
            WriteEquipmentSelection(partOEquipmentSelection, control_EquipmentSelection.ProjectTestVentilationUnit, analyticalModel);
        }

        /// <summary>
        /// Writes the project test product and then the preselection, in that order and under one
        /// re-entrancy guard.
        /// <para>
        /// <b>The order is load-bearing.</b> A permitted product is restored by ticking its catalogue row,
        /// and the test product has no row until it has been stated - so writing the pool first would
        /// silently drop the test product's permission.
        /// </para>
        /// </summary>
        private void WriteEquipmentSelection(PartOEquipmentSelection? partOEquipmentSelection, PartOProjectTestVentilationUnit? partOProjectTestVentilationUnit, AnalyticalModel? analyticalModel_Count)
        {
            writing_EquipmentSelection = true;

            try
            {
                control_EquipmentSelection.ProjectTestVentilationUnit = partOProjectTestVentilationUnit;

                //Counted off the SAVED project: those are the assignments a rename or a removal would
                //orphan. One pass over the model's air handling units, once per write.
                control_EquipmentSelection.ProjectTestVentilationUnitAssignmentCount = Query.PartOVentilationUnitAssignmentCount(analyticalModel_Count, partOProjectTestVentilationUnit?.VentilationUnitReference);

                control_EquipmentSelection.EquipmentSelection = partOEquipmentSelection;
            }
            finally
            {
                writing_EquipmentSelection = false;
            }
        }

        /// <summary>
        /// Whether equipment selection is in play at all - which on this window is the chosen scenario's
        /// own answer, the Iteration 1a / Iteration 2 difference. HOW products are then chosen belongs to
        /// the control.
        /// </summary>
        private void UpdateEquipmentSelectionControls()
        {
            control_EquipmentSelection.IsSelectionEnabled = Scenario?.SelectVentilationUnit ?? false;

            //The control states the heading itself as part of its compact summary, so this window's own
            //label would be a second copy of the same two words directly above it.
            label_EquipmentSelection.Visibility = control_EquipmentSelection.IsSelectionEnabled
                ? Visibility.Visible
                : Visibility.Collapsed;

            //And on a route that selects no unit the whole section is out of sight: the route line says it
            //once, and the pointer under Advanced / Details says where the catalogue went.
            stackPanel_EquipmentSelection.Visibility = control_EquipmentSelection.IsSelectionEnabled ? Visibility.Visible : Visibility.Collapsed;
            textBlock_AdvancedEquipment.Visibility = control_EquipmentSelection.IsSelectionEnabled ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateScopeControls()
        {
            bool selecting = Scope != PartOWorkflowScope.AllDwellings;

            grid_Dwellings.Visibility = selecting ? Visibility.Visible : Visibility.Collapsed;

            int selected = Zones_Dwelling.Count;

            UpdateSelectionText();

            //The isolation assumption is disclosed WHERE THE CHOICE IS MADE, not only in the report a person
            //reads afterwards. Ticking an isolated run is accepting that the interfaces to everything left
            //out are simulated as adiabatic.
            textBlock_Scope.Text = Scope switch
            {
                PartOWorkflowScope.AllDwellings => zones_Eligible.Count == 1
                    ? "The 1 eligible dwelling · simulated inside the whole building"
                    : string.Format("All {0} eligible dwellings · simulated inside the whole building", zones_Eligible.Count),
                PartOWorkflowScope.SelectedDwellings => string.Format("{0} of {1} · simulated inside the whole building", selected, UI.Query.PartOCount(zones_Eligible.Count, "eligible dwelling", "eligible dwellings")),
                _ => string.Format("{0} of {1} · only those are simulated. Interfaces to excluded spaces are simulated as adiabatic and the surrounding external geometry is retained as shading context, so results may differ from a whole-building simulation. The Part O criteria and the Part F requirements are unchanged.", selected, UI.Query.PartOCount(zones_Eligible.Count, "eligible dwelling", "eligible dwellings")),
            };

            //What the loaded model already IS, from the context the preparation stamped on it - said beside
            //the scope choice, because "all dwellings" of an isolated extract is not the whole building.
            PartOIsolationContext? partOIsolationContext = analyticalModel?.GetValue<PartOIsolationContext>(Analytical.AnalyticalModelParameter.PartOIsolationContext);

            if (partOIsolationContext is not null && partOIsolationContext.IsValid)
            {
                textBlock_Scope.Text = string.Format("{0} The model currently loaded is ALREADY the isolated thermal model of {1}.", textBlock_Scope.Text, string.Join(", ", partOIsolationContext.Names_Dwelling));
            }
        }

        /// <summary>
        /// The one line about the selection, on its own so that typing in the search box can update it
        /// without rebuilding the whole status list - searching narrows the view and changes no stage.
        /// </summary>
        private void UpdateSelectionText()
        {
            textBlock_Selection.Text = string.IsNullOrWhiteSpace(dwellingSelection.SearchText)
                ? string.Format("{0} of {1} selected.", dwellingSelection.SelectedCount, UI.Query.PartOCount(dwellingSelection.Count, "dwelling", "dwellings"))
                : string.Format("{0} of {1} selected. The search is narrowing the list; Select All and None apply to what the search matches.", dwellingSelection.SelectedCount, UI.Query.PartOCount(dwellingSelection.Count, "dwelling", "dwellings"));
        }

        /// <summary>
        /// What Iteration 2B is on the chosen scenario, in one sentence: why it is not offered on a scenario
        /// that can never carry one, and otherwise what it is and when it becomes available.
        /// <para>
        /// The rule is the scenario's own (<see cref="PartOWorkflowScenario.SupportsOptimisation"/>), which
        /// reads the route off what SAM says the iteration is defined over. This window states nothing about
        /// what natural ventilation is. Whether 2B can actually start is <c>Modify.CanOptimise</c>'s answer,
        /// carried in on <see cref="Capabilities"/>.
        /// </para>
        /// </summary>
        private static string OptimisationScenarioText(PartOWorkflowScenario? partOWorkflowScenario)
        {
            if (partOWorkflowScenario?.SupportsOptimisation ?? false)
            {
                return "Iteration 2B is not a scenario of its own - it optimises this Iteration 2 design once it has been simulated. Its airflow step and round limit are confirmed when it starts.";
            }

            return (partOWorkflowScenario?.Option?.PartOVentilationMode) != PartOVentilationMode.MVHR
                ? "Iteration 2B raises mechanical design airflow, and this scenario is not a mechanical route. Natural ventilation is not a mechanical airflow optimisation target."
                : "Iteration 2B works within the capacity of a selected manufacturer unit, so it is available on Iteration 2 only.";
        }

        private void button_Run_Click(object sender, RoutedEventArgs e)
        {
            Action = PartOWorkflowAction.PrepareAndRun;

            DialogResult = true;
        }

        private void button_Review_Click(object sender, RoutedEventArgs e)
        {
            Action = ReviewOpensIteration3 ? PartOWorkflowAction.Iteration3Review : PartOWorkflowAction.ReviewResults;

            DialogResult = true;
        }

        private void button_Optimise_Click(object sender, RoutedEventArgs e)
        {
            Action = PartOWorkflowAction.Optimise;

            DialogResult = true;
        }

        private void button_Iteration3_Click(object sender, RoutedEventArgs e)
        {
            Action = PartOWorkflowAction.Iteration3;

            DialogResult = true;
        }

        private void button_Close_Click(object sender, RoutedEventArgs e)
        {
            Action = PartOWorkflowAction.None;

            DialogResult = false;
        }
    }
}
