// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Review and edit the Approved Document F conformance assessment of each dwelling: the terminal
    /// schedule, the internal door transfer requirements, the purge assessment, the clause-level checks,
    /// and a graphical airflow overlay drawn from the same data.
    /// <para>
    /// The window contains no regulatory logic. Everything shown is calculated in SAM.Analytical and read
    /// here; everything edited is an ENGINEERING INPUT that the calculation cannot derive - the provided
    /// undercut of a door, the openable area of a window, whether a person has confirmed that the system
    /// is designed to minimise noise. Keeping the two apart is why the same numbers appear in the report,
    /// on the clipboard, in Grasshopper and in the regression tests.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The base class is fully qualified because this namespace sits inside <c>SAM.Analytical</c>, which
    /// has a <c>Window</c> of its own - the architectural element. An unqualified <c>Window</c> here binds
    /// to that one, not to the WPF class.
    /// </remarks>
    public partial class PartFAssessmentWindow : System.Windows.Window
    {
        private List<PartFDwellingResult> dwellingResults = [];

        private readonly ObservableCollection<PartFDoorRow> rows_Door = [];
        private readonly ObservableCollection<PartFPurgeRow> rows_Purge = [];
        private readonly ObservableCollection<PartFCheckRow> rows_Check = [];

        private bool loading = true;

        public PartFAssessmentWindow()
        {
            InitializeComponent();

            InitialiseFloorPlan();
            BuildColumns();
            BuildLegend();

            foreach (PartFOperatingMode partFOperatingMode in Enum.GetValues(typeof(PartFOperatingMode)))
            {
                ComboBox_Mode.Items.Add(Core.Query.Description(partFOperatingMode));
            }

            ComboBox_Mode.SelectedIndex = 0;
        }

        /// <summary>The dwellings to review. Setting this loads the window.</summary>
        public List<PartFDwellingResult> DwellingResults
        {
            get
            {
                return dwellingResults;
            }

            set
            {
                dwellingResults = value ?? [];

                loading = true;

                ComboBox_Dwelling.Items.Clear();
                foreach (PartFDwellingResult dwellingResult in dwellingResults)
                {
                    ComboBox_Dwelling.Items.Add(string.IsNullOrWhiteSpace(dwellingResult.Name) ? "Dwelling (whole model)" : dwellingResult.Name);
                }

                loading = false;

                if (ComboBox_Dwelling.Items.Count != 0)
                {
                    ComboBox_Dwelling.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// True where the engineer applied their edits, so the caller knows to write the model back.
        /// </summary>
        public bool Applied { get; private set; }

        /// <summary>The operating condition currently being shown.</summary>
        public PartFOperatingMode OperatingMode
        {
            get { return (PartFOperatingMode)Math.Max(0, ComboBox_Mode.SelectedIndex); }
        }

        private PartFDwellingResult Selected
        {
            get
            {
                int index = ComboBox_Dwelling.SelectedIndex;

                return index >= 0 && index < dwellingResults.Count ? dwellingResults[index] : null;
            }
        }

        // ------------------------------------------------------------------
        // Loading
        // ------------------------------------------------------------------

        private void Load()
        {
            PartFDwellingResult dwellingResult = Selected;
            PartFComplianceResult complianceResult = dwellingResult?.ComplianceResult;

            rows_Door.Clear();
            rows_Purge.Clear();
            rows_Check.Clear();

            if (complianceResult is null)
            {
                DataGrid_Terminals.ItemsSource = null;
                TextBlock_Status.Text = string.Empty;

                //Through the renderer's own Clear(), never Overlay.Children.Clear() - the renderer attaches
                //its own ContainerVisual to the shared Overlay once, in its constructor, and Overlay is
                //shared with other floor-plan overlays (the Ventilation Design renderer among them).
                //Clearing Overlay.Children here would detach the renderer's container permanently, so a
                //later Draw() would write into a visual no longer attached to the plan.
                renderer?.Clear();

                TextBox_Schematic.Text = string.Empty;
                return;
            }

            (string symbol, string description, byte red, byte green, byte blue) = PartFAirflowAppearance.Status(Map(complianceResult.OverallStatus));

            TextBlock_Status.Text = string.Format("{0} {1}", symbol, Core.Query.Description(complianceResult.OverallStatus));
            TextBlock_Status.Foreground = new SolidColorBrush(Color.FromRgb(red, green, blue));

            DataGrid_Terminals.ItemsSource = complianceResult.Terminals.ConvertAll(x => new PartFTerminalRow(x));

            foreach (PartFDoorTransferData partFDoorTransferData in complianceResult.TransferPaths)
            {
                rows_Door.Add(new PartFDoorRow(partFDoorTransferData));
            }

            foreach (PartFPurgeVentilationData partFPurgeVentilationData in complianceResult.PurgeVentilation)
            {
                rows_Purge.Add(new PartFPurgeRow(partFPurgeVentilationData));
            }

            //Every check is listed, so the engineer sees the whole regulatory picture. Only the ones a
            //person is allowed to answer accept a confirmation - a calculated failure is arithmetic against
            //the Approved Document and a tick does not change it.
            foreach (PartFComplianceCheck check in complianceResult.Checks)
            {
                rows_Check.Add(new PartFCheckRow(check));
            }

            DataGrid_Doors.ItemsSource = rows_Door;
            DataGrid_Purge.ItemsSource = rows_Purge;
            DataGrid_Checks.ItemsSource = rows_Check;

            TextBox_Schematic.Text = PartFSchematic.Build(complianceResult, OperatingMode);

            LoadFloorPlan();
        }

        private static PartFComplianceStatus Map(PartFOverallStatus partFOverallStatus)
        {
            return partFOverallStatus switch
            {
                PartFOverallStatus.Pass => PartFComplianceStatus.Pass,
                PartFOverallStatus.Fail => PartFComplianceStatus.Fail,
                PartFOverallStatus.EngineeringReviewRequired => PartFComplianceStatus.EngineeringReviewRequired,
                PartFOverallStatus.CannotBeDetermined => PartFComplianceStatus.CannotBeDetermined,
                _ => PartFComplianceStatus.NotAssessed,
            };
        }

        private static double Total(PartFComplianceResult partFComplianceResult, PartFTerminalRole partFTerminalRole)
        {
            return partFComplianceResult.Terminals
                .Where(x => x.TerminalRole == partFTerminalRole && x.IsInBalancedFlow)
                .Sum(x => x.ContinuousDesignFlowRate_Lps ?? 0);
        }

        private static Brush Brush(PartFAirflowAppearance partFAirflowAppearance)
        {
            return new SolidColorBrush(Color.FromRgb(partFAirflowAppearance.Red, partFAirflowAppearance.Green, partFAirflowAppearance.Blue));
        }

        private void BuildLegend()
        {
            List<object> items = [];

            foreach (PartFAirflowAppearance appearance in PartFAirflowAppearance.All)
            {
                items.Add(new
                {
                    Text = string.Format("{0}  {1} ({2}, {3})", appearance.TerminalSymbol, appearance.Name, appearance.Abbreviation, appearance.LinePattern),
                    Brush = Brush(appearance),
                });
            }

            DataTemplate dataTemplate = new();

            FrameworkElementFactory frameworkElementFactory = new(typeof(TextBlock));
            frameworkElementFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Text"));
            frameworkElementFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Brush"));
            frameworkElementFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);

            dataTemplate.VisualTree = frameworkElementFactory;

            ItemsControl_Legend.ItemTemplate = dataTemplate;
            ItemsControl_Legend.ItemsSource = items;
        }

        // ------------------------------------------------------------------
        // Grid columns
        // ------------------------------------------------------------------

        private void BuildColumns()
        {
            void Terminal(string header, string path, double width) => Column(DataGrid_Terminals, typeof(PartFTerminalRow), header, path, width);
            void Door(string header, string path, double width) => Column(DataGrid_Doors, typeof(PartFDoorRow), header, path, width);
            void Purge(string header, string path, double width) => Column(DataGrid_Purge, typeof(PartFPurgeRow), header, path, width);
            void Check(string header, string path, double width) => Column(DataGrid_Checks, typeof(PartFCheckRow), header, path, width);

            Terminal("Space", "SpaceName", 140);
            Terminal("Role", "Role", 130);

            //Required, proposed and provided are three columns because they are three different claims,
            //and collapsing them is how a terminal SAM suggested ends up reported as one that exists.
            Terminal("Required high rate l/s", "Required", 130);
            Terminal("Sizing method", "Method", 170);
            Terminal("Proposed by SAM", "Proposed", 170);
            Terminal("Provided by design", "Provided", 170);
            Terminal("Provision", "Provision", 170);

            Terminal("Continuous l/s", "Continuous", 100);
            Terminal("High/boost l/s", "High", 100);
            Terminal("Setback l/s", "Setback", 90);
            Terminal("Status", "Status", 150);
            Terminal("Source", "Source", 260);
            Terminal("Note", "Note", 420);

            Door("Door", "Name", 120);
            Door("From", "From", 110);
            Door("To", "To", 110);
            Door("In one dwelling", "IsInternal", 100);
            Door("Transfer required", "Required", 100);

            //Headed "calculated" because they are: paragraph 1.25 requires a free AREA through an internal
            //door and prescribes no flow rate for one, so these three columns are SAM's airflow-network
            //routing and no door is assessed on them. The paragraph 1.25 assessment is the area columns.
            Door("Calculated continuous l/s", "Continuous", 140);
            Door("Calculated high l/s", "High", 120);
            Door("Calculated setback l/s", "Setback", 130);

            Door("Required area mm2", "RequiredArea", 110);
            Door("Required undercut mm", "RequiredUndercut", 130);
            Door("Provided undercut mm", "ProvidedUndercut", 130);
            Door("Provided area mm2", "ProvidedArea", 110);
            Door("Clear width mm", "ClearWidth", 100);
            Door("Floor finish fitted", "FloorFinishFitted", 110);
            Door("Transfer device", "Device", 140);
            Door("Flow override l/s", "Override", 105);
            Door("Route", "Route", 190);
            Door("Status", "Status", 150);
            Door("Diagnostic", "Diagnostic", 460);

            Purge("Room", "SpaceName", 140);
            Purge("Volume m3", "Volume", 90);
            Purge("Floor area m2", "Area", 95);
            Purge("Required l/s", "Required", 95);
            Purge("Purge method", "Method", 140);
            Purge("Opening type", "OpeningType", 230);
            Purge("Opening angle deg", "Angle", 110);
            Purge("Required opening m2", "RequiredArea", 120);
            Purge("Openable window m2", "OpenableWindow", 125);
            Purge("External door m2", "ExternalDoor", 115);
            Purge("Mechanical purge l/s", "Mechanical", 125);
            Purge("Window area in model m2", "ModelWindowArea", 140);
            Purge("Directly outside", "DirectlyOutside", 100);
            Purge("Status", "Status", 150);
            Purge("Diagnostic", "Diagnostic", 460);

            Check("Category", "Category", 180);
            Check("Check", "Name", 320);

            //The calculated status and the reported status are two columns, side by side. A single status
            //column would let a recorded answer stand where the calculation is what a reader needs to see.
            Check("SAM calculated", "Calculated", 170);
            Check("Reported", "Status", 190);

            DataGrid_Checks.Columns.Add(new DataGridCheckBoxColumn { Header = "Confirm", Binding = new System.Windows.Data.Binding("Confirmed") { Mode = System.Windows.Data.BindingMode.TwoWay }, Width = 70 });

            Check("Confirmed by", "ResponsiblePerson", 140);
            Check("Date", "Date", 110);
            Check("Recorded evidence", "UserEvidence", 300);
            Check("Alternative method", "AlternativeComplianceMethod", 300);
            Check("Reason for departure", "OverrideReason", 260);
            Check("Notes", "Notes", 260);
            Check("Source", "Source", 320);
            Check("Requirement", "Requirement", 520);
            Check("Model evidence", "Evidence", 520);
        }

        /// <summary>
        /// Adds one column, taking its editability from the row type rather than from a flag at the call
        /// site.
        /// <para>
        /// A <see cref="DataGridTextColumn"/> built with the default two-way binding throws
        /// <see cref="InvalidOperationException"/> the moment WPF attaches it to a read-only property, and
        /// WPF attaches it when the cell is first realised - so the window opens normally and then falls
        /// over the first time that tab is shown. A per-call boolean that has to agree with the row type
        /// is a standing invitation to exactly that, so there is no per-call boolean: a property with no
        /// public setter always gets a one-way read-only column, and one with a setter always gets a
        /// two-way editable column.
        /// </para>
        /// <para>
        /// A path that names no property throws here, at construction, rather than showing an empty column
        /// that looks like missing data.
        /// </para>
        /// </summary>
        private static void Column(DataGrid dataGrid, Type rowType, string header, string path, double width)
        {
            bool readOnly = PartFGridColumn.IsReadOnly(rowType, path);

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new System.Windows.Data.Binding(path) { Mode = readOnly ? System.Windows.Data.BindingMode.OneWay : System.Windows.Data.BindingMode.TwoWay },
                Width = width,
                IsReadOnly = readOnly,
            });
        }

        // ------------------------------------------------------------------
        // Events
        // ------------------------------------------------------------------

        private void ComboBox_Dwelling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading)
            {
                //Commit the dwelling being left before its rows are rebuilt, so edits made in one dwelling
                //are not silently discarded when the engineer moves to the next one.
                ApplyRows();

                Load();
            }
        }

        /// <summary>
        /// Switching operating condition changes what the system is doing, not where the rooms are, so the
        /// plan's geometry and every mark's position are left alone and only the values are re-read.
        /// </summary>
        private void ComboBox_Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading || !IsLoaded)
            {
                return;
            }

            PartFComplianceResult complianceResult = Selected?.ComplianceResult;
            if (complianceResult is null)
            {
                return;
            }

            TextBox_Schematic.Text = PartFSchematic.Build(complianceResult, OperatingMode);

            Refresh();
        }

        private void ComboBox_Level_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading && IsLoaded)
            {
                LoadFloorPlan();
            }
        }

        /// <summary>
        /// A visibility toggle changes which tags are drawn and what they say, and both change what has to
        /// fit on the plan, so the tags are placed again rather than just redrawn.
        /// </summary>
        private void Overlay_Changed(object sender, RoutedEventArgs e)
        {
            ApplyViewSettings();
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            CheckBox_Terminals.IsChecked = true;
            CheckBox_Transfer.IsChecked = true;
            CheckBox_DoorData.IsChecked = true;
            CheckBox_Unresolved.IsChecked = true;
            CheckBox_Values.IsChecked = true;
            CheckBox_Compliance.IsChecked = true;
            CheckBox_Context.IsChecked = true;

            FloorPlan.ZoomExtents();

            ApplyViewSettings();
        }

        /// <summary>Shows everything the assessment holds about one terminal.</summary>
        private void Show(PartFVentilationTerminalRequirement terminal)
        {
            if (terminal is null)
            {
                return;
            }

            TextBlock_Selection.Text = string.Join(Environment.NewLine,
                string.Format("Space: {0}", terminal.SpaceName),
                string.Format("Terminal: {0}", Core.Query.Description(terminal.TerminalRole)),
                string.Format("Sizing method: {0}", Core.Query.Description(terminal.ExtractMethod)),
                string.Format("Operating mode shown: {0}", Core.Query.Description(OperatingMode)),
                string.Format("Continuous design: {0}", Rate(terminal.ContinuousDesignFlowRate_Lps)),
                string.Format("High/boost: {0}", Rate(terminal.HighFlowRate_Lps)),
                string.Format("Setback: {0}", Rate(terminal.SetbackFlowRate_Lps)),
                string.Format("Required high rate: {0}", Rate(terminal.RequiredHighFlowRate_Lps)),
                string.Format("Proposed by SAM: {0}", Core.Query.Description(terminal.ProposedExtractMethod)),
                string.Format("Provided by design: {0}", Core.Query.Description(terminal.ProvidedExtractMethod)),
                string.Format("Provision: {0}", Core.Query.Description(terminal.ProvisionStatus)),
                string.Format("In balanced flow: {0}", terminal.IsInBalancedFlow ? "yes" : "no"),
                string.Format("Regulatory reference: {0}", terminal.SourceReference),
                string.Format("Status: {0}", Core.Query.Description(terminal.ComplianceStatus)),
                string.Empty,
                terminal.Diagnostic);
        }

        /// <summary>Shows everything the assessment holds about one internal transfer route.</summary>
        private void Show(PartFDoorTransferData partFDoorTransferData)
        {
            if (partFDoorTransferData is null)
            {
                return;
            }

            TextBlock_Selection.Text = string.Join(Environment.NewLine,
                string.Format("Door/opening: {0}", partFDoorTransferData.Name),
                string.Format("From: {0}", partFDoorTransferData.UpstreamSpaceName),
                string.Format("To: {0}", partFDoorTransferData.DownstreamSpaceName),
                string.Format("Door modelled: {0}", partFDoorTransferData.IsDoorRepresented ? "yes" : "no"),
                string.Format("Transfer opening: {0}", Core.Query.Description(partFDoorTransferData.OpeningStatus)),
                string.Format("Continuous transfer: {0}", Rate(partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps)),
                string.Format("High/boost transfer: {0}", Rate(partFDoorTransferData.HighTransferFlowRate_Lps)),
                string.Format("Setback transfer: {0}", Rate(partFDoorTransferData.SetbackTransferFlowRate_Lps)),
                string.Format("Route: {0}", Core.Query.Description(partFDoorTransferData.RouteStatus)),
                string.Format("Required free area: {0}", Area(partFDoorTransferData.MinimumRequiredFreeArea_mm2)),
                string.Format("Required undercut: {0} finished, {1} before floor finish", Length(partFDoorTransferData.RequiredUndercutHeightFinished_mm), Length(partFDoorTransferData.RequiredUndercutHeightBeforeFloorFinish_mm)),
                string.Format("Provided free area: {0}", Area(partFDoorTransferData.EffectiveProvidedFreeArea_mm2())),
                string.Format("Transfer device: {0}", Core.Query.Description(partFDoorTransferData.TransferDeviceType)),
                string.Format("Regulatory reference: {0}", partFDoorTransferData.SourceReference),
                string.Format("Status: {0}", Core.Query.Description(partFDoorTransferData.ComplianceStatus)),
                string.Empty,
                partFDoorTransferData.CalculationSource,
                string.Empty,
                partFDoorTransferData.Diagnostic);
        }

        private void Button_Report_Click(object sender, RoutedEventArgs e)
        {
            //Commit whatever the engineer is still editing before the report reads the rows, so the report
            //shows the values on screen rather than the ones from before the edit.
            ApplyRows();

            PartFDwellingResult dwellingResult = Selected;
            if (dwellingResult is null)
            {
                return;
            }

            Core.UI.WPF.ReportWindow reportWindow = new("Part F Conformance Assessment", PartFReport.Build([dwellingResult], OperatingMode))
            {
                Owner = this,
            };

            reportWindow.ShowDialog();
        }

        private void Button_CopyAll_Click(object sender, RoutedEventArgs e)
        {
            //The whole assessment, every dwelling, not just the one on screen: Copy All means all. The
            //rows still being edited are committed first, for the same reason the report is.
            ApplyRows();

            string text = PartFReport.Build(dwellingResults, OperatingMode);

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                //The clipboard can be held by another process. Falling back to the report window leaves the
                //user one keystroke from copying rather than with nothing.
                Core.UI.WPF.ReportWindow reportWindow = new("Part F Conformance Assessment", text) { Owner = this };
                reportWindow.ShowDialog();
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            ApplyRows();

            Applied = true;
            DialogResult = true;
        }

        /// <summary>
        /// Writes the rows currently on screen back into the dwelling they came from, so an edit survives
        /// both a dwelling switch and the OK button. The row objects hold their source records, so they know
        /// which dwelling to write to - applying them here writes to the dwelling being left, not the one
        /// now selected.
        /// </summary>
        private void ApplyRows()
        {
            //Commit whatever cell the user is still editing, so a value typed but not tabbed out of is not
            //silently lost.
            DataGrid_Doors.CommitEdit(DataGridEditingUnit.Row, true);
            DataGrid_Purge.CommitEdit(DataGridEditingUnit.Row, true);
            DataGrid_Checks.CommitEdit(DataGridEditingUnit.Row, true);

            foreach (PartFDoorRow row in rows_Door)
            {
                row.Apply();
            }

            foreach (PartFPurgeRow row in rows_Purge)
            {
                row.Apply();
            }

            foreach (PartFCheckRow row in rows_Check)
            {
                row.Apply();
            }

            //The confirmations just recorded can change the dwelling's overall outcome, so it is resolved
            //again rather than left showing the status from before the edit.
            foreach (PartFDwellingResult dwellingResult in dwellingResults)
            {
                dwellingResult?.ComplianceResult?.Resolve();
            }
        }

        // ------------------------------------------------------------------
        // Formatting
        // ------------------------------------------------------------------

        private static string Rate(double? value_Lps)
        {
            return value_Lps is null ? "not applicable" : string.Format(CultureInfo.InvariantCulture, "{0:0.##} l/s", value_Lps.Value);
        }

        private static string Area(double? value_mm2)
        {
            return value_mm2 is null ? "not recorded" : string.Format(CultureInfo.InvariantCulture, "{0:#,##0.##} mm2", value_mm2.Value);
        }

        private static string Length(double? value_mm)
        {
            return value_mm is null ? "not recorded" : string.Format(CultureInfo.InvariantCulture, "{0:0.##} mm", value_mm.Value);
        }

    }
}
