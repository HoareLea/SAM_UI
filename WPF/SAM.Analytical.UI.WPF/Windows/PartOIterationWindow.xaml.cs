// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What to prepare: which Approved Document O base provision, over which dwellings, and whether a
    /// manufacturer ventilation unit is to be selected.
    /// <para>
    /// <b>The ventilation route is picked, never typed.</b> The base provision combo offers the two options
    /// <see cref="PartOVentilationStrategyOption.Options"/> builds, each carrying the canonical word for its
    /// route, and there is no text field anywhere on this window that reaches
    /// <c>Modify.PreparePartOIteration</c>. That is what keeps the long-form synonym - which prepares
    /// successfully and is then refused by every space at assessment - out of reach from the UI.
    /// </para>
    /// <para>
    /// <b>The dwelling scope is SAM's, not this window's.</b> <c>Query.PartFDwellingZones</c> is the single
    /// source of that policy, including the legacy case where nothing carries
    /// <c>ZoneParameter.IsDwelling</c> at all; it is asked ONCE, when the zones are set, and what it returns
    /// becomes the <see cref="PartODwellingSelection"/> the user then narrows. A zone outside the scope is
    /// reported as outside it - it is not quietly given a strategy, and no common-space criterion is chosen
    /// for it here.
    /// </para>
    /// <para>
    /// <b>The selection is real, and it is the scope.</b> Every eligible dwelling starts selected; what the
    /// user leaves selected is exactly what <see cref="Zones_Dwelling"/> returns, and that is what the
    /// preparation, and therefore Iteration 2 and Iteration 2B, run over. Selection is by the zone's
    /// <see cref="Guid"/>, so two dwellings that share a display name are still two scopes.
    /// </para>
    /// </summary>
    public partial class PartOIterationWindow : System.Windows.Window
    {
        private PartODwellingSelection dwellingSelection = new([]);

        private VentilationUnitCatalogue ventilationUnitCatalogue;

        public PartOIterationWindow()
        {
            InitializeComponent();

            //The ceiling the auto-sizing gives way to the scroller at. Read from the work area rather than
            //stated in pixels, so an enlarged system font or a small screen degrades to scrolling with the
            //buttons reachable instead of to a window taller than the display.
            MaxHeight = SystemParameters.WorkArea.Height * 0.92;

            comboBox_BaseProvision.ItemsSource = PartOVentilationStrategyOption.Options;
            comboBox_BaseProvision.SelectedIndex = 0;
            comboBox_BaseProvision.SelectionChanged += (s, e) =>
            {
                UpdateVentilationStrategyText();

                //The route decides whether 2B is available at all, so this has to be re-asked when the
                //base provision changes - not only when the equipment tick moves.
                UpdateOptimiseAvailability();
            };

            checkBox_SelectVentilationUnit.Checked += (s, e) => UpdateEquipmentSelectionAvailability();
            checkBox_SelectVentilationUnit.Unchecked += (s, e) => UpdateEquipmentSelectionAvailability();

            //The control owns the modes, the ticks and the wording. All this window adds is the
            //Iteration 2B gate, which depends on whether a unit is being selected at all.
            control_EquipmentSelection.SelectionChanged += (s, e) => UpdateOptimiseAvailability();

            checkBox_Optimise.Checked += (s, e) => UpdateOptimiseText();
            checkBox_Optimise.Unchecked += (s, e) => UpdateOptimiseText();
            checkBox_CapacityEnvelope.Checked += (s, e) => UpdateOptimiseText();
            checkBox_CapacityEnvelope.Unchecked += (s, e) => UpdateOptimiseText();

            //On by default: the case the envelope answers is exactly the case in which the optimisation on
            //its own does not tell an engineer what to do next.
            checkBox_CapacityEnvelope.IsChecked = new PartOOptimisationSettings().CapacityEnvelope;

            checkBox_WarmStart.Checked += (s, e) => UpdateOptimiseText();
            checkBox_WarmStart.Unchecked += (s, e) => UpdateOptimiseText();

            checkBox_WarmStart.IsChecked = new PartOOptimisationSettings().WarmStart;

            textBox_AirFlowStep.Text = PartOOptimisationSettings.DefaultAirFlowStep_Lps.ToString();
            textBox_MaximumIterations.Text = PartOOptimisationSettings.DefaultMaximumIterations.ToString();

            //The search narrows the VIEW, never the selection: a dwelling filtered out of sight stays
            //exactly as selected or cleared as it was, and reappears with its state intact.
            textBox_Search.TextChanged += (s, e) =>
            {
                dwellingSelection.SearchText = textBox_Search.Text;

                (listBox_Zones.ItemsSource as ICollectionView)?.Refresh();
            };

            //Both act on what the search currently matches - the whole eligible set while it is empty, so a
            //large block can be narrowed by name and then taken or dropped as a group.
            button_SelectAll.Click += (s, e) => dwellingSelection.SetSelected(true);
            button_SelectNone.Click += (s, e) => dwellingSelection.SetSelected(false);

            UpdateVentilationStrategyText();
            UpdateEquipmentSelectionAvailability();
        }

        /// <summary>
        /// The model's zones. Setting them asks SAM which are dwellings - once - and fills the selectable
        /// scope and the scope report from the answer.
        /// </summary>
        public List<Zone> Zones
        {
            set
            {
                //The policy call, not a local IsDwelling filter. Asked here, once; the selection below is
                //built over the answer and nothing re-asks it per click or per keystroke.
                List<Zone> zones_Dwelling = Analytical.Query.PartFDwellingZones(value) ?? [];

                dwellingSelection = new PartODwellingSelection(zones_Dwelling);

                //Both, because the one line this window derives states the selected count AND whether a
                //search is narrowing the list - so it moves on either. Neither costs more than a pass over
                //the dwelling records: this window inspects nothing.
                dwellingSelection.SelectionChanged += (s, e) => UpdateSelectionText();
                dwellingSelection.SearchTextChanged += (s, e) => UpdateSelectionText();

                ICollectionView view = CollectionViewSource.GetDefaultView(dwellingSelection.Items);
                view.Filter = item => dwellingSelection.IsVisible((PartODwellingSelection.Item)item);

                listBox_Zones.ItemsSource = view;

                //What is out of scope is derived from what the policy RETURNED, by difference - never from a
                //second reading of IsDwelling. Where nothing in the model states the parameter,
                //PartFDwellingZones treats every zone as a dwelling and the unmarked zones are then IN scope;
                //an independent rule here for deciding that would be a second policy, and it got this wrong.
                HashSet<Guid> guids_Dwelling = [];
                foreach (Zone zone in zones_Dwelling)
                {
                    if (zone is not null)
                    {
                        guids_Dwelling.Add(zone.Guid);
                    }
                }

                //The classification supplies only the REASON a zone is out of scope, not the fact.
                Analytical.Query.PartFClassifyDwellingZones(value, out List<Zone> _, out List<Zone> zones_NotDwelling, out List<Zone> zones_Unmarked);

                HashSet<Guid> guids_Unmarked = [];
                foreach (Zone zone in zones_Unmarked ?? [])
                {
                    if (zone is not null)
                    {
                        guids_Unmarked.Add(zone.Guid);
                    }
                }

                List<string> descriptions = [];

                foreach (Zone zone in value ?? [])
                {
                    if (zone is null || guids_Dwelling.Contains(zone.Guid))
                    {
                        continue;
                    }

                    descriptions.Add(guids_Unmarked.Contains(zone.Guid)
                        ? string.Format("'{0}' (not marked either way, beside zones that are)", zone.Name)
                        : string.Format("'{0}' (marked not a dwelling)", zone.Name));
                }

                textBlock_Scope.Text = descriptions.Count == 0
                    ? string.Format("{0} dwelling zone(s) in scope.", zones_Dwelling.Count)
                    : string.Format("{0} dwelling zone(s) in scope. Outside current Part O dwelling preparation scope: {1}. Common spaces are not covered by Iteration 1a or 1b and are assessed separately.", zones_Dwelling.Count, string.Join(", ", descriptions));

                UpdateSelectionText();
            }
        }

        /// <summary>
        /// The catalogue this session read. Setting it decides whether equipment selection can be offered at
        /// all, and says which of the three states the read landed in.
        /// </summary>
        public VentilationUnitCatalogue VentilationUnitCatalogue
        {
            get
            {
                return ventilationUnitCatalogue;
            }
            set
            {
                ventilationUnitCatalogue = value;

                //Offered only where there is something to select from. Ticked by default in that case,
                //because Iteration 2 is the current stage - but never forced, since Iteration 1a (no
                //selection) stays a legitimate run.
                bool hasProducts = value?.HasSelectableProducts ?? false;

                checkBox_SelectVentilationUnit.IsEnabled = hasProducts;
                checkBox_SelectVentilationUnit.IsChecked = hasProducts;

                control_EquipmentSelection.VentilationUnitCatalogue = value;

                UpdateEquipmentSelectionAvailability();
            }
        }

        /// <summary>
        /// The dwelling zones the iteration will be prepared over: the ones the user left selected.
        /// <b>Selection is the scope</b> - this is no longer SAM's answer unmodified but SAM's answer as the
        /// user narrowed it, and every downstream consumer (preparation, Iteration 2 selection, Iteration 2B
        /// optimisation) runs over exactly this set.
        /// </summary>
        public List<Zone> Zones_Dwelling => dwellingSelection.SelectedZones();

        /// <summary>
        /// The selection model behind the dwelling list. Exposed for tests: the discovery, selection and
        /// filtering behaviour is assertable without rendering the window.
        /// </summary>
        internal PartODwellingSelection DwellingSelection => dwellingSelection;

        /// <summary>Whether OK is currently offered - false while the selection is empty. Exposed for tests.</summary>
        internal bool CanAccept => button_OK.IsEnabled;

        /// <summary>
        /// Whether the equipment section is showing its compact read-only summary rather than the full,
        /// actionable controls - true on Iteration 1a and 1b, where no product is selected. Exposed for
        /// tests.
        /// </summary>
        internal bool IsEquipmentSelectionCompact => control_EquipmentSelection.IsCompact;

        /// <summary>
        /// The "select a ventilation unit" tick itself. Exposed for tests, which have to move the CONTROL
        /// rather than the derived answer - what is under test is what the tick's own handler does.
        /// </summary>
        internal bool SelectVentilationUnitChecked
        {
            get => checkBox_SelectVentilationUnit.IsChecked ?? false;
            set => checkBox_SelectVentilationUnit.IsChecked = value;
        }

        /// <summary>What the window says about the current selection - exposed so it is assertable.</summary>
        public string SelectionDescription => textBlock_Selection.Text;

        /// <summary>
        /// What the window says about the scope: how many dwellings are in it, and which zones are outside it
        /// and why. Exposed so what the user is told is assertable, rather than only visible.
        /// </summary>
        public string ScopeDescription => textBlock_Scope.Text;

        /// <summary>
        /// Whether the selected dwellings are to be simulated as an isolated thermal model rather than as
        /// part of the whole building. <b>Off by default</b>: the whole-building simulation is the reference
        /// case, and isolation is an explicit choice to trade it for speed.
        /// <para>
        /// This changes the THERMAL MODEL only. The Approved Document O criteria, the Approved Document F
        /// requirements and the TM59 classification of the selected dwellings are unaffected by it.
        /// </para>
        /// </summary>
        public bool Isolate => checkBox_Isolate.IsChecked ?? false;

        /// <summary>What the window says about isolation. Exposed so what the user is told is assertable.</summary>
        public string IsolationDescription => textBlock_Isolate.Text;

        /// <summary>The chosen base provision - its iteration and its canonical ventilation strategy.</summary>
        public PartOVentilationStrategyOption SelectedOption => comboBox_BaseProvision.SelectedItem as PartOVentilationStrategyOption;

        /// <summary>
        /// Whether the catalogue is to be offered to the preparation's selection rule. False leaves
        /// <c>AirHandlingUnitParameter.VentilationUnitReference</c> untouched, which is Iteration 1a.
        /// </summary>
        public bool SelectVentilationUnit => (checkBox_SelectVentilationUnit.IsChecked ?? false) && (ventilationUnitCatalogue?.HasSelectableProducts ?? false);

        /// <summary>
        /// The project's equipment preselection as this window currently states it - asked of the control
        /// that owns it, so this window holds no second copy of the mode or the pool.
        /// <para>
        /// Setting it restores a project's own configuration, which is what makes the mode and pool survive
        /// closing and reopening this dialog and reopening the project. It is the SAME project-scoped
        /// configuration Prepare &amp; Run reads and writes, so a change made in either workflow is seen by
        /// the other.
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
                control_EquipmentSelection.EquipmentSelection = value;
            }
        }

        /// <summary>The selection authority this window currently states.</summary>
        public PartOEquipmentSelectionMode Mode => control_EquipmentSelection.Mode;

        /// <summary>
        /// The project's own test ventilation unit as this window currently states it - asked of the same
        /// shared control, so this window holds no second copy of it either.
        /// <para>
        /// <b>Set this before <see cref="EquipmentSelection"/>.</b> A permitted product is restored by
        /// ticking its row, and the test product has no row until it has been stated.
        /// </para>
        /// </summary>
        public PartOProjectTestVentilationUnit ProjectTestVentilationUnit
        {
            get
            {
                return control_EquipmentSelection.ProjectTestVentilationUnit;
            }
            set
            {
                control_EquipmentSelection.ProjectTestVentilationUnit = value;
            }
        }

        /// <summary>
        /// How many dwellings the saved project has fitted with the current test product. Handed in by the
        /// caller, which is the only party here that holds a model - see
        /// <c>Query.PartOVentilationUnitAssignmentCount</c>.
        /// </summary>
        public int ProjectTestVentilationUnitAssignmentCount
        {
            set
            {
                control_EquipmentSelection.ProjectTestVentilationUnitAssignmentCount = value;
            }
        }

        /// <summary>What this window currently says about the project test product.</summary>
        public string ProjectTestDescription => control_EquipmentSelection.ProjectTestDescription;

        /// <summary>The active mode in one sentence. See <c>PartOEquipmentSelectionControl.ModeDescription</c>.</summary>
        public string ModeDescription => control_EquipmentSelection.ModeDescription;

        /// <summary>The catalogue rows, so a test can read exactly what the engineer can see.</summary>
        internal List<PartOCatalogueProductRow> CatalogueProductRows => control_EquipmentSelection.CatalogueProductRows;

        /// <summary>What this window currently says about the catalogue and the current mode.</summary>
        public string CatalogueDescription => control_EquipmentSelection.CatalogueDescription;

        /// <summary>
        /// The Iteration 2B optimisation this run is set up to allow afterwards, or <b>null</b> where none
        /// was asked for or the run is not an Iteration 2B starting point.
        /// <para>
        /// <b>Null unless a ventilation unit is being selected.</b> 2B optimises a design within a selected
        /// product's capacity; without one there is no ceiling to explore inside, and the run is Iteration
        /// 1a. Offering it anyway would let somebody start an optimisation that
        /// <c>Modify.CanOptimise</c> is going to refuse minutes later.
        /// </para>
        /// <para>
        /// Null too where the step or the iteration limit typed in is not usable - the settings say why
        /// through <see cref="OptimisationRefusal"/>, and the window will not close on them.
        /// </para>
        /// </summary>
        public PartOOptimisationSettings? OptimisationSettings
        {
            get
            {
                if (!Optimise)
                {
                    return null;
                }

                if (!double.TryParse(textBox_AirFlowStep.Text, out double airFlowStep_Lps) || !int.TryParse(textBox_MaximumIterations.Text, out int maximumIterations))
                {
                    return null;
                }

                PartOOptimisationSettings result = new()
                {
                    AirFlowStep_Lps = airFlowStep_Lps,
                    MaximumIterations = maximumIterations,
                    CapacityEnvelope = checkBox_CapacityEnvelope.IsChecked ?? false,
                    WarmStart = checkBox_WarmStart.IsChecked ?? false,
                };

                return result.IsValid(out string? _) ? result : null;
            }
        }

        /// <summary>Whether the automatic Iteration 2B optimisation was asked for and is available.</summary>
        public bool Optimise => (checkBox_Optimise.IsChecked ?? false) && checkBox_Optimise.IsEnabled;

        /// <summary>
        /// Why the optimisation settings as typed cannot be used, or null where they can - or where no
        /// optimisation was asked for at all.
        /// </summary>
        public string? OptimisationRefusal
        {
            get
            {
                if (!Optimise)
                {
                    return null;
                }

                if (!double.TryParse(textBox_AirFlowStep.Text, out double airFlowStep_Lps))
                {
                    return string.Format("'{0}' is not an airflow step. Enter the number of litres per second each failing room's design airflow is raised by each round.", textBox_AirFlowStep.Text);
                }

                if (!int.TryParse(textBox_MaximumIterations.Text, out int maximumIterations))
                {
                    return string.Format("'{0}' is not a round limit. Enter the most optimisation rounds the run may take.", textBox_MaximumIterations.Text);
                }

                PartOOptimisationSettings partOOptimisationSettings = new()
                {
                    AirFlowStep_Lps = airFlowStep_Lps,
                    MaximumIterations = maximumIterations,
                };

                return partOOptimisationSettings.IsValid(out string? refusal) ? null : refusal;
            }
        }

        /// <summary>
        /// 2B needs two things, and is cleared rather than merely greyed when either goes away.
        /// <para>
        /// <b>A selected product</b>, because the optimisation explores within its capacity; and
        /// <b>the MVHR route</b>, because 2B raises MECHANICAL design airflow and a naturally ventilated
        /// dwelling has none for it to raise. <c>Modify.CanOptimise</c> refuses both categorically - but it
        /// does so after the iteration has been prepared and a full-year simulation has run, which is
        /// minutes of an engineer's time to be told something this window already knew.
        /// </para>
        /// <para>
        /// The route is read off the selected option, which carries what SAM says that iteration is defined
        /// over - never decided here.
        /// </para>
        /// </summary>
        private void UpdateOptimiseAvailability()
        {
            bool available = SelectVentilationUnit && SelectedOption?.PartOVentilationMode == PartOVentilationMode.MVHR;

            checkBox_Optimise.IsEnabled = available;

            if (!available)
            {
                checkBox_Optimise.IsChecked = false;
            }

            textBox_AirFlowStep.IsEnabled = available;
            textBox_MaximumIterations.IsEnabled = available;
            checkBox_CapacityEnvelope.IsEnabled = available;
            checkBox_WarmStart.IsEnabled = available;

            UpdateOptimiseText();
        }

        private void UpdateOptimiseText()
        {
            if (!checkBox_Optimise.IsEnabled)
            {
                textBlock_Optimise.Text = SelectedOption?.PartOVentilationMode != PartOVentilationMode.MVHR
                    ? "Iteration 2B raises mechanical design airflow, and the base provision selected above is not a mechanical route. Natural ventilation is not a mechanical airflow optimisation target."
                    : "Iteration 2B raises the design airflow of failing mechanically ventilated rooms within the capacity of the unit already selected for their dwelling, so it needs a ventilation unit to be selected above.";

                return;
            }

            textBlock_Optimise.Text = Optimise
                ? "Optional: these settings only pre-fill the Iteration 2B confirmation, where they can still be changed. When Iteration 2B runs, each eligible failing room's DESIGN airflow is raised by the step, the dwelling is rebalanced, the Part O state is rebuilt and the same weather case is re-run - until every eligible space passes, or the selected unit cannot carry another full step. The selected product is never changed and no Approved Document F requirement is altered. A design that passes at this step is the first tested passing design, not a minimum."
                : "Iteration 2B is not a base provision - it is an optimisation performed on this Iteration 2 design. It can be started once this iteration has been simulated (Results - Optimise (2B), or Part O - Prepare & Run), and its settings are confirmed then; ticking this only pre-fills them.";

            textBlock_WarmStart.Text = (checkBox_WarmStart.IsChecked ?? false) && checkBox_WarmStart.IsEnabled
                ? "Each round starts from the TBD this run's own baseline conversion produced, on its own copy of it, instead of exporting and converting the same geometry again - because a design airflow round changes the ventilation and nothing the conversion reads. Every round still runs a REAL full-year simulation of its own design and is still assessed with production TM59, and each keeps its own TBD and TSD. Any round that cannot be shown to still match that baseline converts in full and says so."
                : "Every round exports the model to gbXML and converts the geometry and shading again. This is the reference path, and the one to use when the converted geometry may no longer be valid for the current model.";

            textBlock_CapacityEnvelope.Text = (checkBox_CapacityEnvelope.IsChecked ?? false) && checkBox_CapacityEnvelope.IsEnabled
                ? "Where the optimisation stops with rooms still failing, one further DIAGNOSTIC run scales the same targets coherently until the already-selected unit's own capacity binds, and reports what TM59 makes of that design. It is reported separately, is never the optimisation's answer, and never reselects a product - it says how close the equipment already chosen can get. It costs one more full-year simulation, and nothing at all on a run that passes."
                : "Without this, a run that stops on the selected unit's capacity or on the iteration limit reports the last valid design and does not say how far that unit could have been taken.";
        }

        private void UpdateVentilationStrategyText()
        {
            PartOVentilationStrategyOption option = SelectedOption;

            textBlock_VentilationStrategy.Text = option is null
                ? string.Empty
                : string.Format("Ventilation route stated for every dwelling in scope: {0}. This is the canonical value the assessment reads; it cannot be edited here.", option.VentilationStrategy);
        }

        /// <summary>
        /// Says whether equipment selection is in play at all, and hands on to the Iteration 2B gate that
        /// depends on it.
        /// <para>
        /// Everything about HOW products are chosen belongs to <c>PartOEquipmentSelectionControl</c> -
        /// the modes, the ticks, the read-only pool under "Automatic - all", the empty-pool warning and
        /// every word of the explanatory text. Held here as well it would drift from Prepare &amp; Run,
        /// which hosts the same control.
        /// </para>
        /// </summary>
        private void UpdateEquipmentSelectionAvailability()
        {
            control_EquipmentSelection.IsSelectionEnabled = SelectVentilationUnit;

            UpdateOptimiseAvailability();
        }

        /// <summary>
        /// What the selection currently is, in one line - and the gate that keeps an empty selection from
        /// preparing an iteration over nothing.
        /// </summary>
        private void UpdateSelectionText()
        {
            int selected = dwellingSelection.SelectedCount;
            int count = dwellingSelection.Count;

            textBlock_Selection.Text = string.IsNullOrWhiteSpace(dwellingSelection.SearchText)
                ? string.Format("{0} of {1} selected.", selected, UI.Query.PartOCount(count, "dwelling", "dwellings"))
                : string.Format("{0} of {1} selected. The search is narrowing the list; Select All and None apply to what the search matches.", selected, UI.Query.PartOCount(count, "dwelling", "dwellings"));

            button_OK.IsEnabled = selected != 0;

            UpdateIsolationText();
        }

        /// <summary>
        /// What isolation would do to this selection - including, plainly, that it is a different thermal
        /// model and not a faster way to compute the same one.
        /// <para>
        /// The assumption is disclosed here rather than only in the report, because this is the moment the
        /// choice is made. A person ticking this is accepting that interfaces to the dwellings they left
        /// out are simulated as adiabatic, and they cannot accept that from a sentence they only see
        /// afterwards.
        /// </para>
        /// </summary>
        private void UpdateIsolationText()
        {
            string text = "When only part of a building is selected, simulate those dwellings as an isolated thermal model. Interfaces to excluded spaces become adiabatic while surrounding external geometry is retained as shading context. This can substantially reduce simulation time on large buildings. Results may differ from a whole-building simulation, because assuming no heat flows across those interfaces is a different thermal model - it does not change the Part O criteria or the Part F requirements.";

            //Said when it applies, not enforced: the dwellings are not the whole building, so even the full
            //dwelling set usually still excludes corridors, cores and plant.
            if (dwellingSelection.Count != 0 && dwellingSelection.SelectedCount == dwellingSelection.Count)
            {
                text = string.Format("{0}\n\nEvery assessable dwelling is currently selected, so the reduction will come only from spaces that are not dwellings - corridors, cores and plant. On a building that is mostly dwellings it may be small.", text);
            }

            textBlock_Isolate.Text = text;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            //Refused here rather than silently ignored: an unreadable step would otherwise prepare the
            //iteration, run a full-year simulation, and only then reveal that the optimisation nobody
            //could start was never going to.
            string? refusal = OptimisationRefusal;
            if (refusal is not null)
            {
                System.Windows.MessageBox.Show(string.Format("The automatic optimisation settings cannot be used.\n\n{0}", refusal), "Part O — Prepare Iteration");

                return;
            }

            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
