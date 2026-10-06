// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The Approved Document O equipment preselection, as one control: which authority chooses the products,
    /// and - visibly - which products there are to choose from.
    ///
    /// <para><b>Why this is a control and not code in each window</b></para>
    /// <para>
    /// Two workflows configure the same thing. <c>PartOIterationWindow</c> is the single-command route and
    /// <c>PartOWorkflowWindow</c> is Prepare &amp; Run, and both have to offer the same three modes over the
    /// same catalogue and produce the same <see cref="Analytical.PartOEquipmentSelection"/>. Held twice they
    /// would drift, and the first symptom of drift is the one native testing found: Prepare &amp; Run
    /// describing a smallest-capable selection over a project the engineer had put under manual authority.
    /// So the controls, the mode rules and the wording live here once, and each window hosts this.
    /// </para>
    ///
    /// <para><b>It configures; it never selects</b></para>
    /// <para>
    /// Nothing here runs a selection rule, compares a capacity or writes to a model. It produces a
    /// statement of intent - a mode and a permitted pool by identity - which
    /// <c>Modify.PreparePartOIteration</c> turns into a candidate set through
    /// <c>PartOEquipmentSelection.CandidateDescriptors</c>. Ticking a product <b>permits</b> it; it assigns
    /// nothing to anybody.
    /// </para>
    ///
    /// <para><b>The pool is never quietly widened</b></para>
    /// <para>
    /// Under <see cref="PartOEquipmentSelectionMode.AutomaticSelectedPool"/> with nothing ticked, this says
    /// so and <see cref="HasCandidates"/> is false - it does not tick everything to make the run proceed.
    /// A pool that silently became the whole catalogue would produce an answer that looked entirely correct.
    /// </para>
    /// </summary>
    public partial class PartOEquipmentSelectionControl : UserControl
    {
        private VentilationUnitCatalogue? ventilationUnitCatalogue;

        /// <summary>
        /// One row per selectable catalogue product, held so the ticks survive a mode change. The permitted
        /// set is read off these rather than off the grid's own selection, which the grid is free to discard
        /// when it re-virtualises.
        /// </summary>
        private List<PartOCatalogueProductRow> catalogueProductRows = [];

        private bool isSelectionEnabled = true;

        /// <summary>
        /// The project's own test product as the boxes currently state it, or null where none is enabled.
        /// Rebuilt from the boxes on every edit rather than mutated, so there is never a half-edited
        /// statement and never an identity that disagrees with the name on screen.
        /// </summary>
        private PartOProjectTestVentilationUnit? projectTestVentilationUnit;

        /// <summary>
        /// Whether the test product is permitted, held <b>apart</b> from its row.
        /// <para>
        /// Load-bearing for a rename. The test product's identity is derived from its name, so renaming it
        /// replaces its catalogue row with one under a new identity - and the tick has to survive that,
        /// or an engineer correcting a typo would silently drop the product out of their own pool. Held
        /// here rather than looked up by identity, because the identity is exactly what changed.
        /// </para>
        /// </summary>
        private bool isUsed_ProjectTest = true;

        /// <summary>
        /// How many dwellings the SAVED project has fitted with the current test product. Set by the host
        /// when it hands over the project; see <see cref="ProjectTestVentilationUnitAssignmentCount"/>.
        /// </summary>
        private int assignmentCount_ProjectTest;

        /// <summary>
        /// Re-entrancy guard. Editing a box rebuilds the rows and raises <see cref="SelectionChanged"/>,
        /// which a host commonly answers by restating what it has - including this very statement. Without
        /// this the two would bounce off each other on a value that never moved.
        /// </summary>
        private bool applying_ProjectTest;

        public PartOEquipmentSelectionControl()
        {
            InitializeComponent();

            radioButton_AutomaticAll.Checked += (s, e) => Apply();
            radioButton_AutomaticPool.Checked += (s, e) => Apply();
            radioButton_Manual.Checked += (s, e) => Apply();

            checkBox_ProjectTest.Checked += (s, e) => ApplyProjectTest();
            checkBox_ProjectTest.Unchecked += (s, e) => ApplyProjectTest();
            textBox_ProjectTestName.TextChanged += (s, e) => ApplyProjectTest();
            textBox_ProjectTestMaximumSupply.TextChanged += (s, e) => ApplyProjectTest();
            textBox_ProjectTestMaximumExtract.TextChanged += (s, e) => ApplyProjectTest();

            Apply();
        }

        /// <summary>
        /// Raised whenever the stated mode or the permitted pool moves, so a host can restate whatever it
        /// says about equipment elsewhere on its own surface.
        /// </summary>
        public event EventHandler? SelectionChanged;

        /// <summary>
        /// The catalogue this control offers. Setting it rebuilds the rows, all ticked - a catalogue arrives
        /// with nothing narrowed, which is the historic default and what the all-products mode means anyway.
        /// </summary>
        public VentilationUnitCatalogue? VentilationUnitCatalogue
        {
            get
            {
                return ventilationUnitCatalogue;
            }
            set
            {
                ventilationUnitCatalogue = value;

                RebuildCatalogueProductRows(true);

                Apply();
            }
        }

        /// <summary>
        /// Whether equipment selection is in play at all - the Iteration 1a / Iteration 2 difference, which
        /// each host decides its own way (a tick in one, the chosen scenario in the other).
        /// <para>
        /// False replaces the section with a compact read-only summary - the two concept names and one
        /// sentence each - rather than greying a screenful of controls nothing on this route can use. The
        /// catalogue stays readable behind the summary's own disclosure. Presentation only: the mode, the
        /// pool and the project test product this control reports are exactly what they were.
        /// </para>
        /// </summary>
        public bool IsSelectionEnabled
        {
            get
            {
                return isSelectionEnabled;
            }
            set
            {
                //Guarded, because a host may restate this on every refresh of its own. Apply raises
                //SelectionChanged, which a host may answer with that very refresh; without this the two
                //would bounce off each other for a value that never moved.
                if (isSelectionEnabled == value)
                {
                    return;
                }

                isSelectionEnabled = value;

                Apply();
            }
        }

        /// <summary>
        /// The preselection this control currently states, and the way a project's own configuration is
        /// restored into it.
        /// <para>
        /// Read off the controls rather than stored beside them, so there is no second copy to fall out of
        /// step with what the engineer is looking at. A product in a restored pool that the current
        /// catalogue no longer holds has no row to tick and is dropped, rather than keeping a permission
        /// nothing can act on.
        /// </para>
        /// </summary>
        public PartOEquipmentSelection EquipmentSelection
        {
            get
            {
                return new PartOEquipmentSelection(Mode, AllowedVentilationUnitReferences());
            }
            set
            {
                //Absent reads as the historic default rather than as a refusal - a project that has never
                //stated a preference has none. See PartOEquipmentSelection.
                PartOEquipmentSelectionMode partOEquipmentSelectionMode = value?.Mode ?? PartOEquipmentSelectionMode.AutomaticAllProducts;

                radioButton_AutomaticPool.IsChecked = partOEquipmentSelectionMode == PartOEquipmentSelectionMode.AutomaticSelectedPool;
                radioButton_Manual.IsChecked = partOEquipmentSelectionMode == PartOEquipmentSelectionMode.ManualPerDwelling;
                radioButton_AutomaticAll.IsChecked = partOEquipmentSelectionMode == PartOEquipmentSelectionMode.AutomaticAllProducts;

                bool all = value is null || !value.HasAllowedVentilationUnitReferences;

                foreach (PartOCatalogueProductRow partOCatalogueProductRow in catalogueProductRows)
                {
                    partOCatalogueProductRow.IsUsed = all || value.IsAllowed(partOCatalogueProductRow.VentilationUnitReference);
                }

                Apply();
            }
        }

        /// <summary>The selection authority this control currently states.</summary>
        public PartOEquipmentSelectionMode Mode
        {
            get
            {
                if (radioButton_Manual.IsChecked ?? false)
                {
                    return PartOEquipmentSelectionMode.ManualPerDwelling;
                }

                return (radioButton_AutomaticPool.IsChecked ?? false)
                    ? PartOEquipmentSelectionMode.AutomaticSelectedPool
                    : PartOEquipmentSelectionMode.AutomaticAllProducts;
            }
        }

        /// <summary>
        /// Whether an automatic selection has anything to choose from. False only for the pooled mode with
        /// nothing ticked - the one combination that will not prepare, and which is reported rather than
        /// corrected.
        /// </summary>
        public bool HasCandidates => Mode != PartOEquipmentSelectionMode.AutomaticSelectedPool || AllowedVentilationUnitReferences().Count != 0;

        /// <summary>
        /// The active mode in one sentence, for a host that describes the run elsewhere on its surface. This
        /// is the wording that used to be a fixed "the smallest capable unit is selected per dwelling" -
        /// which was simply untrue of a project under manual authority.
        /// </summary>
        public string ModeDescription
        {
            get
            {
                int used = AllowedVentilationUnitReferences().Count;

                switch (Mode)
                {
                    case PartOEquipmentSelectionMode.AutomaticSelectedPool:
                        return used == 0
                            ? "Equipment selection: Automatic - selected pool, with no product permitted. There is nothing for an automatic selection to choose from and this run will not prepare; no fallback to the full catalogue occurs."
                            : string.Format("Equipment selection: Automatic - selected pool. The smallest capable product is selected per dwelling only from the {0} allowed catalogue product(s). No fallback to the full catalogue occurs.", used);

                    case PartOEquipmentSelectionMode.ManualPerDwelling:
                        return "Equipment selection: Manual per dwelling. Existing authored equipment assignments are preserved. No automatic equipment selection runs.";

                    default:
                        return "Equipment selection: Automatic - all catalogue products. The smallest capable product is selected per dwelling from the full selectable catalogue. Product maximum capacity is a ceiling and never becomes a design airflow.";
                }
            }
        }

        /// <summary>What this control says about the catalogue and the current pool. The visible text.</summary>
        public string CatalogueDescription => textBlock_Catalogue.Text;

        /// <summary>The catalogue rows, so a test can read exactly what the engineer can see.</summary>
        internal List<PartOCatalogueProductRow> CatalogueProductRows => catalogueProductRows;

        /// <summary>Whether the ticks can currently be edited. See <see cref="Apply"/>.</summary>
        internal bool IsPoolEditable => !dataGrid_Catalogue.IsReadOnly;

        /// <summary>
        /// Whether the compact read-only summary is showing rather than the full section - true on a route
        /// that selects no manufacturer unit. Exposed so the 1a / 1b presentation is assertable.
        /// </summary>
        internal bool IsCompact => grid_Compact.Visibility == System.Windows.Visibility.Visible;

        /// <summary>Whether the full, actionable section is showing. The complement of <see cref="IsCompact"/>.</summary>
        internal bool IsFullSectionVisible => stackPanel_Full.Visibility == System.Windows.Visibility.Visible;

        /// <summary>What the compact summary says about equipment selection. For a test to read.</summary>
        internal string CompactEquipmentDescription => textBlock_CompactEquipment.Text;

        /// <summary>What the compact summary says about the project test product. For a test to read.</summary>
        internal string CompactProjectTestDescription => textBlock_CompactProjectTest.Text;

        /// <summary>What the compact summary's catalogue disclosure says. For a test to read.</summary>
        internal string CompactCatalogueDescription => textBlock_CompactCatalogue.Text;

        /// <summary>Whether the catalogue disclosure starts closed - it must never be prominent on 1a / 1b.</summary>
        internal bool IsCompactCatalogueExpanded => expander_CompactCatalogue.IsExpanded;

        /// <summary>
        /// The rows the compact reference view is bound to - the SAME list the Iteration 2 grid shows.
        /// Exposed so a test can prove the products are readable on 1a / 1b and are not a second copy.
        /// </summary>
        internal System.Collections.IEnumerable? CompactCatalogueRows => dataGrid_CompactCatalogue.ItemsSource;

        /// <summary>Whether the compact reference view can be edited. It cannot, and that is asserted.</summary>
        internal bool IsCompactCatalogueReadOnly => dataGrid_CompactCatalogue.IsReadOnly;

        /// <summary>
        /// What the compact reference view's columns are called. Exposed so a test can prove the identity
        /// and capacity columns are there and that no actionable "Use" column is.
        /// </summary>
        internal List<string?> CompactCatalogueColumnHeaders => Headers(dataGrid_CompactCatalogue);

        /// <summary>
        /// What the Iteration 2 catalogue's columns are called. Exposed alongside the reference view's, so
        /// a test can prove the actionable grid still has its "Use" column.
        /// </summary>
        internal List<string?> CatalogueColumnHeaders => Headers(dataGrid_Catalogue);

        private static List<string?> Headers(System.Windows.Controls.DataGrid dataGrid)
        {
            List<string?> result = [];

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
            {
                result.Add(dataGridColumn.Header as string);
            }

            return result;
        }

        /// <summary>
        /// Keeps the controls consistent with what the current choice actually offers, and restates the line
        /// under the grid.
        /// <para>
        /// <b>Under "Automatic - all" the ticks are shown and locked.</b> Every product is eligible by
        /// definition in that mode, so an editable tick would offer a choice the mode does not have - but
        /// hiding the grid would take away the catalogue visibility this control exists to provide. So it is
        /// shown, all ticked, read-only.
        /// </para>
        /// </summary>
        private void Apply()
        {
            //THE WHOLE SECTION, or a two-line summary of it. On a route that selects no manufacturer unit
            //there is nothing here to configure, and a screenful of greyed controls said so far more loudly
            //than the one sentence that is actually true. See grid_Compact in the XAML.
            stackPanel_Full.Visibility = isSelectionEnabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            grid_Compact.Visibility = isSelectionEnabled ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

            //Kept, and still correct, for the case a host disables the section while it is shown.
            stackPanel_Mode.IsEnabled = isSelectionEnabled;
            label_Catalogue.IsEnabled = isSelectionEnabled;
            dataGrid_Catalogue.IsEnabled = isSelectionEnabled;

            bool all = Mode == PartOEquipmentSelectionMode.AutomaticAllProducts;

            dataGrid_Catalogue.IsReadOnly = !isSelectionEnabled || all;

            if (isSelectionEnabled && all)
            {
                foreach (PartOCatalogueProductRow partOCatalogueProductRow in catalogueProductRows)
                {
                    partOCatalogueProductRow.IsUsed = true;
                }
            }

            UpdateCatalogueText();

            UpdateProjectTest();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// What the catalogue and the current pool amount to, in the words an engineer needs - including the
        /// one combination that will not prepare.
        /// </summary>
        private void UpdateCatalogueText()
        {
            if (!isSelectionEnabled)
            {
                textBlock_Catalogue.Text = ventilationUnitCatalogue is null
                    ? "The ventilation unit catalogue has not been read."
                    : string.Format("No equipment selection runs for this route, so no product is selected. {0}", ventilationUnitCatalogue.Description);

                //The same sentence, behind the compact summary's disclosure - the catalogue stays readable
                //on Iteration 1a and 1b without occupying the middle of the dialog.
                textBlock_CompactCatalogue.Text = textBlock_Catalogue.Text;

                return;
            }

            int used = AllowedVentilationUnitReferences().Count;
            int total = catalogueProductRows.Count;

            switch (Mode)
            {
                case PartOEquipmentSelectionMode.AutomaticSelectedPool:
                    textBlock_Catalogue.Text = used == 0
                        ? string.Format("No product is ticked, so an automatic selection has nothing to choose from and this will not prepare. Tick the products this project permits - SAM will not fall back to the other {0} in the catalogue.", total)
                        : string.Format("{0} of {1} product(s) permitted. Each dwelling is given the smallest permitted product that can meet its own design duty; the others are never selected.", used, total);
                    break;

                case PartOEquipmentSelectionMode.ManualPerDwelling:
                    textBlock_Catalogue.Text = used == 0 || used == total
                        ? string.Format("No selection rule runs. Every dwelling keeps the product it already has, and all {0} catalogue product(s) are offered when the assignments are stated.", total)
                        : string.Format("No selection rule runs. Every dwelling keeps the product it already has, and the {0} product(s) ticked here are offered when the assignments are stated.", used);
                    break;

                default:
                    textBlock_Catalogue.Text = string.Format("{0} Each dwelling is given the smallest product that can meet its own design duty. A product's Maximum is its capability ceiling and is never a design airflow.", ventilationUnitCatalogue?.Description);
                    break;
            }
        }

        /// <summary>
        /// The ticked products' identities - the project's permitted pool.
        ///
        /// <para><b>Derived from the rows, and never stored beside them</b></para>
        /// <para>
        /// This is what makes a stale project-test identity impossible rather than merely unlikely. The
        /// test product's identity comes from its name, so renaming it changes the identity a pool would
        /// hold; because the pool is recomputed from the rows every time it is asked for, and the rename
        /// replaced the row, the new identity is in the pool and the old one is simply not there to be
        /// left behind. Disabling the product removes the row and, in the same step, its permission. A
        /// pool held as its own list would have needed the two kept in step by hand, and the failure would
        /// have been an invisible permission for a product that no longer exists.
        /// </para>
        /// </summary>
        private List<VentilationUnitReference> AllowedVentilationUnitReferences()
        {
            List<VentilationUnitReference> result = [];

            foreach (PartOCatalogueProductRow partOCatalogueProductRow in catalogueProductRows)
            {
                if (partOCatalogueProductRow.IsUsed && partOCatalogueProductRow.VentilationUnitReference is not null)
                {
                    result.Add(partOCatalogueProductRow.VentilationUnitReference);
                }
            }

            return result;
        }

        /// <summary>
        /// The project's own test ventilation unit as this control states it, and the way a project's own
        /// statement is restored into it.
        ///
        /// <para><b>Set this BEFORE <see cref="EquipmentSelection"/></b></para>
        /// <para>
        /// For the same reason the catalogue is set before the pool: a permitted product is restored by
        /// ticking its row, and the test product has no row until it has been stated. A host that sets
        /// these the other way round would restore a pool that silently dropped the test product's
        /// permission.
        /// </para>
        /// </summary>
        public PartOProjectTestVentilationUnit? ProjectTestVentilationUnit
        {
            get
            {
                return projectTestVentilationUnit is null ? null : new PartOProjectTestVentilationUnit(projectTestVentilationUnit);
            }
            set
            {
                applying_ProjectTest = true;

                try
                {
                    checkBox_ProjectTest.IsChecked = value is not null;

                    textBox_ProjectTestName.Text = value?.Name ?? string.Empty;
                    textBox_ProjectTestMaximumSupply.Text = Text(value?.MaximumSupplyFlowRate_Lps);
                    textBox_ProjectTestMaximumExtract.Text = Text(value?.MaximumExtractFlowRate_Lps);
                }
                finally
                {
                    applying_ProjectTest = false;
                }

                ApplyProjectTest();
            }
        }

        /// <summary>
        /// How many dwellings the SAVED project has fitted with the current test product, as the host has
        /// counted them - see <c>Query.PartOVentilationUnitAssignmentCount</c>.
        ///
        /// <para><b>Why the control is told rather than asked</b></para>
        /// <para>
        /// Because a product assigned to dwellings must not be able to vanish from underneath them, and
        /// this control has no business reading a model to find that out. The host counts once, when it
        /// hands over the project, and this control turns the count into the one thing it implies: while
        /// anything is assigned, the enable tick and the NAME are locked, because both would change the
        /// identity those dwellings hold. The two capacities stay editable - re-rating a what-if is the
        /// whole point of it, and it moves no identity and reassigns nothing.
        /// </para>
        /// </summary>
        public int ProjectTestVentilationUnitAssignmentCount
        {
            get
            {
                return assignmentCount_ProjectTest;
            }
            set
            {
                if (assignmentCount_ProjectTest == value)
                {
                    return;
                }

                assignmentCount_ProjectTest = value;

                ApplyProjectTest();
            }
        }

        /// <summary>What this control says about the project test product. The visible text.</summary>
        public string ProjectTestDescription => textBlock_ProjectTest.Text;

        /// <summary>Whether the test product's identity can currently be changed - false while it is assigned.</summary>
        internal bool IsProjectTestIdentityEditable => checkBox_ProjectTest.IsEnabled && textBox_ProjectTestName.IsEnabled;

        /// <summary>Whether the test product's capacities can currently be changed.</summary>
        internal bool IsProjectTestCapacityEditable => textBox_ProjectTestMaximumSupply.IsEnabled && textBox_ProjectTestMaximumExtract.IsEnabled;

        /// <summary>
        /// Rebuilds the statement from the boxes, replaces the test product's catalogue row, and restates
        /// everything that depends on it - in that order, once, so the pool the control reports is never
        /// momentarily missing a product the engineer can see ticked.
        /// </summary>
        private void ApplyProjectTest()
        {
            if (applying_ProjectTest)
            {
                return;
            }

            projectTestVentilationUnit = (checkBox_ProjectTest.IsChecked ?? false)
                ? new PartOProjectTestVentilationUnit(
                    textBox_ProjectTestName.Text?.Trim(),
                    Value(textBox_ProjectTestMaximumSupply.Text),
                    Value(textBox_ProjectTestMaximumExtract.Text))
                : null;

            //The row is replaced rather than edited, and the tick is carried across explicitly - the
            //identity is derived from the name, so a rename is a new row and an old one that must not
            //linger. AllowedVentilationUnitReferences reads the rows, so this IS the pool update.
            RebuildCatalogueProductRows(false);

            //Everything that merely depends on the statement, rather than replacing it, is restated by
            //Apply - which also runs when the MODE changes, and the mode changes what this section says.
            Apply();
        }

        /// <summary>
        /// Restates what the test-product section offers and says. Called from <see cref="Apply"/> rather
        /// than only from <see cref="ApplyProjectTest"/>, because the mode and the enabled-ness of the whole
        /// control both change what it should read - and neither of those is an edit to the statement.
        /// </summary>
        private void UpdateProjectTest()
        {
            //Locked where anything is assigned: the name is the identity those dwellings hold.
            bool assigned = assignmentCount_ProjectTest > 0 && projectTestVentilationUnit is not null;

            border_ProjectTest.IsEnabled = isSelectionEnabled;

            checkBox_ProjectTest.IsEnabled = isSelectionEnabled && !assigned;
            textBox_ProjectTestName.IsEnabled = isSelectionEnabled && !assigned;

            //The two capacities stay editable while assigned: re-rating a what-if is the whole point of it,
            //it moves no identity, and it reassigns nothing.
            bool enabled = isSelectionEnabled && (checkBox_ProjectTest.IsChecked ?? false);

            textBox_ProjectTestMaximumSupply.IsEnabled = enabled;
            textBox_ProjectTestMaximumExtract.IsEnabled = enabled;

            UpdateProjectTestText(assigned);
        }

        /// <summary>What the test product amounts to, in the words an engineer needs.</summary>
        private void UpdateProjectTestText(bool assigned)
        {
            if (projectTestVentilationUnit is null)
            {
                textBlock_ProjectTest.Text = "No project test product. Only the manufacturer catalogue products above are available.";

                return;
            }

            if (!projectTestVentilationUnit.IsValid)
            {
                textBlock_ProjectTest.Text = projectTestVentilationUnit.Refusal;

                return;
            }

            //Said every time, in both automatic modes, because it is the one thing about this product an
            //engineer could reasonably assume the other way round.
            string participation = Mode == PartOEquipmentSelectionMode.AutomaticAllProducts
                ? "It does NOT take part in \"Automatic - all catalogue products\", which means the manufacturer catalogue. Choose \"Automatic - selected pool\" and tick it above, or assign it by hand."
                : "It takes part only where it is ticked above or assigned by hand.";

            textBlock_ProjectTest.Text = assigned
                ? string.Format(
                    "This project test product is assigned to {0} dwelling(s). Reassign those dwellings before removing it or renaming it - the name is the identity they hold. Its maximum airflows can still be changed, which is how a different capacity is tried.",
                    assignmentCount_ProjectTest)
                : string.Format(
                    "Project test product '{0}', {1:0.###} / {2:0.###} l/s maximum. Not manufacturer data. {3}",
                    projectTestVentilationUnit.Name,
                    projectTestVentilationUnit.MaximumSupplyFlowRate_Lps,
                    projectTestVentilationUnit.MaximumExtractFlowRate_Lps,
                    participation);
        }

        /// <summary>
        /// One row per manufacturer product, then the project's test product where it states one.
        ///
        /// <para><b>One list, and that is the point</b></para>
        /// <para>
        /// The test product is a row in the same collection the pool is derived from, so it is permitted,
        /// reported and restored by exactly the machinery every other product uses - and its permission
        /// cannot outlive it. The Origin column, not a separate list, is what keeps it distinguishable.
        /// </para>
        /// </summary>
        /// <param name="reset">
        /// True where the catalogue itself changed and every manufacturer tick starts again from "permitted"
        /// - a catalogue arrives with nothing narrowed. False where only the test product moved, in which
        /// case the manufacturer ticks the engineer has made are carried across by identity.
        /// </param>
        private void RebuildCatalogueProductRows(bool reset)
        {
            Dictionary<string, bool> dictionary_IsUsed = [];

            if (!reset)
            {
                foreach (PartOCatalogueProductRow partOCatalogueProductRow in catalogueProductRows)
                {
                    if (!partOCatalogueProductRow.IsProjectTest && partOCatalogueProductRow.VentilationUnitReference is not null)
                    {
                        dictionary_IsUsed[Key(partOCatalogueProductRow.VentilationUnitReference)] = partOCatalogueProductRow.IsUsed;
                    }
                }
            }

            List<PartOCatalogueProductRow> result = [];

            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in ventilationUnitCatalogue?.CapacityDescriptors ?? [])
            {
                bool isUsed = true;

                if (ventilationUnitCapacityDescriptor?.VentilationUnitReference is not null)
                {
                    dictionary_IsUsed.TryGetValue(Key(ventilationUnitCapacityDescriptor.VentilationUnitReference), out isUsed);
                }

                result.Add(new PartOCatalogueProductRow(ventilationUnitCapacityDescriptor, isUsed));
            }

            //Last, and only where it states something usable: an unusable statement has no identity, so it
            //has nothing a pool could hold. Qualified - SAM.Analytical.UI.WPF declares a Query of its own.
            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in Analytical.Query.CapacityDescriptors(projectTestVentilationUnit))
            {
                result.Add(new PartOCatalogueProductRow(ventilationUnitCapacityDescriptor, isUsed_ProjectTest, true));
            }

            catalogueProductRows = result;

            //Subscribed, because the grid's tick writes straight to the row and tells nobody else. Left
            //unsubscribed, the line below the grid would keep reporting the pool the engineer had BEFORE
            //they last ticked something - which is the one place they look to find out whether an empty
            //pool is about to refuse the run.
            foreach (PartOCatalogueProductRow partOCatalogueProductRow in catalogueProductRows)
            {
                PartOCatalogueProductRow row = partOCatalogueProductRow;

                row.PropertyChanged += (sender, eventArgs) =>
                {
                    if (eventArgs.PropertyName != nameof(PartOCatalogueProductRow.IsUsed))
                    {
                        return;
                    }

                    //Remembered apart from the row, so that renaming the product - which replaces the row -
                    //does not drop the permission the engineer gave it.
                    if (row.IsProjectTest)
                    {
                        isUsed_ProjectTest = row.IsUsed;
                    }

                    Apply();
                };
            }

            dataGrid_Catalogue.ItemsSource = catalogueProductRows;

            //The SAME list, so the compact reference view cannot hold a stale or separate catalogue. It
            //has no editable column, so sharing the rows lends it no authority over the pool.
            dataGrid_CompactCatalogue.ItemsSource = catalogueProductRows;
        }

        /// <summary>
        /// The three identity fields joined by a separator a product name cannot contain - the same
        /// function, and so the same notion of "the same product", as
        /// <c>PartOEquipmentAssignmentSet</c>'s own index.
        /// </summary>
        private static string Key(VentilationUnitReference ventilationUnitReference)
        {
            return string.Concat(
                ventilationUnitReference.Manufacturer ?? string.Empty,
                " ",
                ventilationUnitReference.Model ?? string.Empty,
                " ",
                ventilationUnitReference.Reference ?? string.Empty);
        }

        /// <summary>
        /// A typed capacity as a number, or <see cref="double.NaN"/> where it is not one - which
        /// <see cref="PartOProjectTestVentilationUnit"/> reads as unresolved and refuses with a sentence,
        /// rather than as a zero.
        /// </summary>
        private static double Value(string text)
        {
            return double.TryParse(text?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out double result)
                ? result
                : double.NaN;
        }

        /// <summary>A capacity for a box: empty where there is nothing to state.</summary>
        private static string Text(double? value_Lps)
        {
            return value_Lps is null || double.IsNaN(value_Lps.Value) || double.IsInfinity(value_Lps.Value)
                ? string.Empty
                : value_Lps.Value.ToString("0.###");
        }
    }
}
