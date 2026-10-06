// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// How one saved 2D view presents the Approved Document F airflow annotation.
    /// <para>
    /// Presentation only. Every control here writes to a <see cref="PartFAirflowViewSettings"/>, which holds
    /// no flow rate, no compliance status and no terminal - those are re-read from the model each time the
    /// view is drawn, so a saved drawing can never disagree with its own assessment.
    /// </para>
    /// <para>
    /// The colour scheme is deliberately NOT here. <c>PartF Data</c> is a space appearance setting, edited
    /// where every other colour scheme is edited, and the two are meant to be used together: the fills say
    /// what each room is, the tags say what its air does.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The base class is fully qualified because this namespace sits inside <c>SAM.Analytical</c>, which has a
    /// <c>Window</c> of its own - the architectural element. An unqualified <c>Window</c> here binds to that
    /// one, not to the WPF class.
    /// </remarks>
    public partial class PartFAirflowViewSettingsWindow : System.Windows.Window
    {
        private List<Zone> zones = [];

        /// <summary>
        /// The dwelling zones the selector offers: those of the scope currently chosen, or none where the
        /// scope is not a zone category. Held separately from <see cref="zones"/> because offering every
        /// model zone would let a person save a dwelling the assessment of that scope never produces, and
        /// the saved view would draw nothing.
        /// </summary>
        private List<Zone> zones_Dwelling = [];

        /// <summary>
        /// The dwelling zone categories offered, in the order they appear in the scope list after its two
        /// fixed entries. Only categories the calculation would actually find dwellings in: offering one it
        /// would find none in is offering a drawing with nothing on it.
        /// </summary>
        private List<string> zoneCategoryNames = [];

        /// <summary>Whether the model is zoned at all, which is what tells a house from an unmarked block.</summary>
        private bool hasZones;

        /// <summary>The scope list's fixed entries: not chosen, then the whole model.</summary>
        private const int index_WholeModel = 1;

        public PartFAirflowViewSettingsWindow()
        {
            InitializeComponent();

            foreach (PartFOperatingMode partFOperatingMode in Enum.GetValues(typeof(PartFOperatingMode)))
            {
                ComboBox_Mode.Items.Add(Core.Query.Description(partFOperatingMode));
            }

            ComboBox_Mode.SelectedIndex = 0;

            //Built here as well as when the model arrives, so the two fixed entries exist even if no model
            //ever does: an empty list would read every saved scope back as "not chosen" and quietly discard
            //a whole-model view's scope on OK.
            RebuildDwellingScope();
        }

        /// <summary>
        /// The model the view belongs to, read for the zone categories and dwelling zones a person can pick
        /// from. Nothing is calculated here.
        /// </summary>
        public AdjacencyCluster AdjacencyCluster
        {
            set
            {
                zones = [.. (value?.GetZones() ?? []).Where(x => x is not null).OrderBy(x => x.Name, StringComparer.Ordinal)];

                hasZones = zones.Count != 0;

                zoneCategoryNames = value?.PartFDwellingZoneCategories() ?? [];

                RebuildDwellingScope();
                RebuildDwellingComboBox();
            }
        }

        /// <summary>
        /// The settings being edited. Reading builds a new instance from the controls; the annotation
        /// overrides - the labels somebody has dragged - are carried through untouched, because moving a label
        /// is not something this dialog has any business discarding.
        /// </summary>
        public PartFAirflowViewSettings PartFAirflowViewSettings
        {
            get
            {
                PartFAirflowViewSettings result = new(partFAirflowViewSettings)
                {
                    Enabled = CheckBox_Enabled.IsChecked == true,
                    OperatingMode = (PartFOperatingMode)System.Math.Max(0, ComboBox_Mode.SelectedIndex),

                    DwellingScope = DwellingScope(out string zoneCategoryName),
                    ZoneCategoryName = zoneCategoryName,

                    ShowSupply = CheckBox_Supply.IsChecked == true,
                    ShowGeneralExtract = CheckBox_GeneralExtract.IsChecked == true,
                    ShowLocalKitchenExtract = CheckBox_LocalKitchenExtract.IsChecked == true,
                    ShowTransfer = CheckBox_Transfer.IsChecked == true,
                    ShowUnresolved = CheckBox_Unresolved.IsChecked == true,
                    ShowValues = CheckBox_Values.IsChecked == true,
                    ShowCompliance = CheckBox_Compliance.IsChecked == true,
                    ShowDoorRequirements = CheckBox_DoorData.IsChecked == true,
                    ShowContextGeometry = CheckBox_Context.IsChecked == true,
                };

                //Index 0 is "all dwellings"; anything after it is one zone, keyed by GUID so renaming a flat
                //does not silently change what this view shows.
                int index = ComboBox_Dwelling.SelectedIndex - 1;

                if (index >= 0 && index < zones_Dwelling.Count)
                {
                    result.DwellingFilter = PartFDwellingFilter.SelectedDwelling;
                    result.DwellingGuid = zones_Dwelling[index].Guid;
                }
                else
                {
                    result.DwellingFilter = PartFDwellingFilter.AllDwellingsOnLevel;
                    result.DwellingGuid = Guid.Empty;
                }

                if (Core.Query.TryConvert(TextBox_AnnotationScale.Text, out double annotationScale) && annotationScale > 0)
                {
                    result.AnnotationScale = annotationScale;
                }

                return result;
            }

            set
            {
                partFAirflowViewSettings = value is null ? new PartFAirflowViewSettings() : new PartFAirflowViewSettings(value);

                CheckBox_Enabled.IsChecked = partFAirflowViewSettings.Enabled;
                ComboBox_Mode.SelectedIndex = (int)partFAirflowViewSettings.OperatingMode;

                SelectDwellingScope(partFAirflowViewSettings.DwellingScope, partFAirflowViewSettings.ZoneCategoryName);

                TextBox_AnnotationScale.Text = partFAirflowViewSettings.AnnotationScale.ToString(System.Globalization.CultureInfo.InvariantCulture);

                CheckBox_Supply.IsChecked = partFAirflowViewSettings.ShowSupply;
                CheckBox_GeneralExtract.IsChecked = partFAirflowViewSettings.ShowGeneralExtract;
                CheckBox_LocalKitchenExtract.IsChecked = partFAirflowViewSettings.ShowLocalKitchenExtract;
                CheckBox_Transfer.IsChecked = partFAirflowViewSettings.ShowTransfer;
                CheckBox_Unresolved.IsChecked = partFAirflowViewSettings.ShowUnresolved;
                CheckBox_Values.IsChecked = partFAirflowViewSettings.ShowValues;
                CheckBox_Compliance.IsChecked = partFAirflowViewSettings.ShowCompliance;
                CheckBox_DoorData.IsChecked = partFAirflowViewSettings.ShowDoorRequirements;
                CheckBox_Context.IsChecked = partFAirflowViewSettings.ShowContextGeometry;

                int index = partFAirflowViewSettings.DwellingFilter == PartFDwellingFilter.SelectedDwelling
                    ? zones_Dwelling.FindIndex(x => x.Guid == partFAirflowViewSettings.DwellingGuid)
                    : -1;

                ComboBox_Dwelling.SelectedIndex = index >= 0 ? index + 1 : 0;

                UpdateEnabled();
            }
        }

        private PartFAirflowViewSettings partFAirflowViewSettings = new();

        private void CheckBox_Enabled_Click(object sender, RoutedEventArgs e)
        {
            UpdateEnabled();
        }

        private void ComboBox_DwellingScope_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RebuildDwellingComboBox();
            UpdateScopeMessage();
        }

        /// <summary>
        /// Everything below the switch is greyed out when the annotation is off - the settings are still there
        /// and are still saved, so turning it back on restores the drawing rather than resetting it.
        /// <para>
        /// The scope group stays reachable while the annotation is off, because choosing the dwellings is
        /// what a person has come here to do when the preset left it undecided.
        /// </para>
        /// </summary>
        private void UpdateEnabled()
        {
            //GroupBox_Scope is deliberately left alone: it stays usable with the annotation off.
            GroupBox_Show.IsEnabled = CheckBox_Enabled.IsChecked == true;

            UpdateScopeMessage();
        }

        // ------------------------------------------------------------------
        // What this drawing reports on
        // ------------------------------------------------------------------

        /// <summary>
        /// The scope list: not chosen, the whole model as one dwelling, then each dwelling zone category the
        /// model actually holds.
        /// <para>
        /// "Not chosen" is a real entry rather than an empty box, because it is a real state with a
        /// consequence - nothing is assessed and nothing is drawn - and the consequence is spelled out
        /// underneath. It used to be the empty text of an editable combo, which the rest of the chain read as
        /// "the whole model is one dwelling": the same blank meant both, and a block of flats waiting to be
        /// scoped was drawn as a single dwelling.
        /// </para>
        /// </summary>
        private void RebuildDwellingScope()
        {
            ComboBox_DwellingScope.Items.Clear();

            ComboBox_DwellingScope.Items.Add("Not chosen - nothing is assessed or drawn");
            ComboBox_DwellingScope.Items.Add(Core.Query.Description(PartFDwellingScope.WholeModel));

            foreach (string zoneCategoryName in zoneCategoryNames)
            {
                ComboBox_DwellingScope.Items.Add(string.Format("Dwellings in zone category '{0}'", zoneCategoryName));
            }

            ComboBox_DwellingScope.SelectedIndex = 0;

            UpdateScopeMessage();
        }

        /// <summary>
        /// The dwelling selector: "all dwellings" always, then the dwelling zones of the scope currently
        /// chosen. Nothing else - a zone from another category would be offered a drawing the scope's
        /// assessment never produces, so a view saved against it would draw nothing.
        /// </summary>
        private void RebuildDwellingComboBox()
        {
            zones_Dwelling = DwellingZones();

            ComboBox_Dwelling.Items.Clear();
            ComboBox_Dwelling.Items.Add("All dwellings on this level");

            foreach (Zone zone in zones_Dwelling)
            {
                ComboBox_Dwelling.Items.Add(zone.Name);
            }

            ComboBox_Dwelling.SelectedIndex = 0;
        }

        /// <summary>
        /// The dwelling zones of the selected scope, decided by the same rule the calculation uses rather
        /// than restated here: the zones of the chosen category that
        /// <see cref="Query.PartFDwellingZones"/> would size. Empty where the scope is not a zone category.
        /// </summary>
        private List<Zone> DwellingZones()
        {
            PartFDwellingScope partFDwellingScope = DwellingScope(out string zoneCategoryName);

            if (partFDwellingScope != PartFDwellingScope.ZoneCategory || string.IsNullOrWhiteSpace(zoneCategoryName))
            {
                return [];
            }

            List<Zone> zones_Category = zones.FindAll(x => string.Equals(x.GetValue<string>(ZoneParameter.ZoneCategory), zoneCategoryName, StringComparison.Ordinal));

            return zones_Category.PartFDwellingZones();
        }

        /// <summary>
        /// Shows a saved scope. A category the model no longer offers is added back to the list rather than
        /// dropped, so reopening the settings of a view scoped to a category somebody has since renamed shows
        /// what the view says and does not silently rescope it.
        /// </summary>
        private void SelectDwellingScope(PartFDwellingScope partFDwellingScope, string zoneCategoryName)
        {
            if (partFDwellingScope == PartFDwellingScope.ZoneCategory && !string.IsNullOrWhiteSpace(zoneCategoryName))
            {
                int index = zoneCategoryNames.FindIndex(x => string.Equals(x, zoneCategoryName, StringComparison.Ordinal));

                if (index < 0)
                {
                    zoneCategoryNames.Add(zoneCategoryName);

                    index = zoneCategoryNames.Count - 1;

                    ComboBox_DwellingScope.Items.Add(string.Format("Dwellings in zone category '{0}'", zoneCategoryName));
                }

                ComboBox_DwellingScope.SelectedIndex = index_WholeModel + 1 + index;
            }
            else
            {
                ComboBox_DwellingScope.SelectedIndex = partFDwellingScope == PartFDwellingScope.WholeModel ? index_WholeModel : 0;
            }

            UpdateScopeMessage();
        }

        /// <summary>What the scope list currently says, as the settings record it.</summary>
        private PartFDwellingScope DwellingScope(out string zoneCategoryName)
        {
            zoneCategoryName = null;

            int index = ComboBox_DwellingScope.SelectedIndex;

            if (index == index_WholeModel)
            {
                return PartFDwellingScope.WholeModel;
            }

            index -= index_WholeModel + 1;

            if (index < 0 || index >= zoneCategoryNames.Count)
            {
                return PartFDwellingScope.Undefined;
            }

            zoneCategoryName = zoneCategoryNames[index];

            return PartFDwellingScope.ZoneCategory;
        }

        /// <summary>
        /// Says, in the dialog, why nothing will be drawn and what would fix it. Silent once the scope is
        /// chosen.
        /// </summary>
        private void UpdateScopeMessage()
        {
            if (DwellingScope(out _) != PartFDwellingScope.Undefined)
            {
                TextBlock_ScopeMessage.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            TextBlock_ScopeMessage.Visibility = System.Windows.Visibility.Visible;

            TextBlock_ScopeMessage.Text = zoneCategoryNames.Count switch
            {
                //Zones, and not one of them a dwelling. Usually Is Dwelling has not been set. Assessing the
                //whole model here would report a block of flats as one dwelling because of a missing
                //parameter, so it is offered as an explicit choice and never assumed.
                0 when hasZones => "This model is zoned, but no zone is marked Is Dwelling = true, so there is no dwelling zone category to report on. Set Is Dwelling on the zones that are flats or houses - or choose the whole model as one dwelling above, if that is really what this is. Nothing is assessed or drawn until this says what the drawing is about.",

                //Several. Which flats a drawing reports on is an engineering decision.
                > 1 => "Choose which dwellings this drawing reports on. Nothing is assessed or drawn until you do: this model holds more than one dwelling zone category, and the drawing will not assume the whole building is a single dwelling.",

                _ => "Choose what this drawing reports on. Nothing is assessed or drawn until you do.",
            };
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
