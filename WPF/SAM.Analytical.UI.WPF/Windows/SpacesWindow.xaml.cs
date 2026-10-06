// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// WPF replacement for the WinForms SAM.Analytical.Windows.Forms.SpacesForm (+ SpacesControl):
    /// a read-only overview grid of every Space and its key properties / internal-condition profile
    /// values. Double-clicking a row opens the WPF <see cref="InternalConditionWindow"/> to edit that
    /// space's internal condition. Mirrors the original surface (the (spaces, AnalyticalModel)
    /// constructor and the read-only Spaces getter).
    ///
    /// NOTE: the legacy grid tinted individual cells (modified / existing / read-only / error). That
    /// purely-cosmetic per-cell colouring is not reproduced here; values and the double-click edit
    /// behaviour are preserved.
    /// </summary>
    public partial class SpacesWindow : System.Windows.Window
    {
        private AnalyticalModel analyticalModel;
        private readonly ObservableCollection<SpaceRow> rows = new ObservableCollection<SpaceRow>();

        public SpacesWindow()
        {
            InitializeComponent();
            DataGrid_Main.ItemsSource = rows;
        }

        public SpacesWindow(IEnumerable<Space> spaces, AnalyticalModel analyticalModel)
            : this()
        {
            this.analyticalModel = analyticalModel;
            LoadSpaces(spaces);
        }

        private void LoadSpaces(IEnumerable<Space> spaces)
        {
            rows.Clear();

            if (spaces == null)
            {
                return;
            }

            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                rows.Add(new SpaceRow(space, analyticalModel?.ProfileLibrary));
            }
        }

        public IEnumerable<Space> Spaces
        {
            get
            {
                return rows.Select(x => x.Space).Where(x => x != null).ToList();
            }
        }

        // Tracks the same AnalyticalModel reassignment InternalConditionWindow does internally, so a
        // profile or internal-condition-library edit made while double-clicking into one space is not
        // silently dropped when the grid re-reads ProfileLibrary for the next row's Update call, and
        // is visible to a caller reading ProfileLibrary / AdjacencyCluster back after ShowDialog().
        public ProfileLibrary ProfileLibrary
        {
            get
            {
                return analyticalModel?.ProfileLibrary;
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return analyticalModel?.AdjacencyCluster;
            }
        }

        private void DataGrid_Main_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SpaceRow row = DataGrid_Main.SelectedItem as SpaceRow;
            Space space = row?.Space;
            if (space == null)
            {
                return;
            }

            InternalConditionWindow internalConditionWindow = new InternalConditionWindow(analyticalModel, space) { Owner = this };
            if (internalConditionWindow.ShowDialog() != true)
            {
                return;
            }

            analyticalModel = new AnalyticalModel(analyticalModel, internalConditionWindow.AdjacencyCluster, analyticalModel.MaterialLibrary, internalConditionWindow.ProfileLibrary);

            row.Update(internalConditionWindow.Space, analyticalModel?.ProfileLibrary);
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>Row view-model — mirrors the legacy SpacesControl.UpdateValues column population.</summary>
        public sealed class SpaceRow : System.ComponentModel.INotifyPropertyChanged
        {
            public SpaceRow(Space space, ProfileLibrary profileLibrary)
            {
                Update(space, profileLibrary);
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            public Space Space { get; private set; }

            public string Name { get; private set; }
            public double? Area { get; private set; }
            public double? Occupancy { get; private set; }
            public string InternalConditionName { get; private set; }
            public double? AreaPerPerson { get; private set; }
            public string HeatingProfileName { get; private set; }
            public double? HeatingDesignTemperature { get; private set; }
            public string CoolingProfileName { get; private set; }
            public double? CoolingDesignTemperature { get; private set; }
            public string OccupancyProfileName { get; private set; }
            public double? OccupancySensibleGainPerPerson { get; private set; }
            public double? OccupancySensibleGainCalculated { get; private set; }
            public double? OccupancyLatentGainPerPerson { get; private set; }
            public double? OccupancyLatentGainCalculated { get; private set; }
            public string LightingProfileName { get; private set; }
            public double? LightingGain { get; private set; }
            public double? LightingGainPerArea { get; private set; }
            public double? LightingGainCalculated { get; private set; }
            public double? LightingLevel { get; private set; }
            public string EquipmentSensibleProfileName { get; private set; }
            public double? EquipmentSensibleGain { get; private set; }
            public double? EquipmentSensibleGainPerArea { get; private set; }
            public double? EquipmentSensibleGainCalculated { get; private set; }
            public string EquipmentLatentProfileName { get; private set; }
            public double? EquipmentLatentGain { get; private set; }
            public double? EquipmentLatentGainPerArea { get; private set; }
            public double? EquipmentLatentGainCalculated { get; private set; }
            public string HumidificationProfileName { get; private set; }
            public double? Humidification { get; private set; }
            public string DehumidificationProfileName { get; private set; }
            public double? Dehumidification { get; private set; }
            public string InfiltrationProfileName { get; private set; }
            public double? Infiltration { get; private set; }
            public string VentilationSystemTypeName { get; private set; }
            public string HeatingSystemTypeName { get; private set; }
            public string CoolingSystemTypeName { get; private set; }

            public void Update(Space space, ProfileLibrary profileLibrary)
            {
                Space = space;

                Name = null; Area = null; Occupancy = null;
                InternalConditionName = null; AreaPerPerson = null;
                HeatingProfileName = null; HeatingDesignTemperature = null;
                CoolingProfileName = null; CoolingDesignTemperature = null;
                OccupancyProfileName = null; OccupancySensibleGainPerPerson = null; OccupancySensibleGainCalculated = null;
                OccupancyLatentGainPerPerson = null; OccupancyLatentGainCalculated = null;
                LightingProfileName = null; LightingGain = null; LightingGainPerArea = null; LightingGainCalculated = null; LightingLevel = null;
                EquipmentSensibleProfileName = null; EquipmentSensibleGain = null; EquipmentSensibleGainPerArea = null; EquipmentSensibleGainCalculated = null;
                EquipmentLatentProfileName = null; EquipmentLatentGain = null; EquipmentLatentGainPerArea = null; EquipmentLatentGainCalculated = null;
                HumidificationProfileName = null; Humidification = null;
                DehumidificationProfileName = null; Dehumidification = null;
                InfiltrationProfileName = null; Infiltration = null;
                VentilationSystemTypeName = null; HeatingSystemTypeName = null; CoolingSystemTypeName = null;

                if (space != null)
                {
                    Populate(space, profileLibrary);
                }

                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));
            }

            private void Populate(Space space, ProfileLibrary profileLibrary)
            {
                double @double;
                string @string;

                Name = space.Name;
                if (space.TryGetValue(SpaceParameter.Area, out @double))
                {
                    Area = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (space.TryGetValue(SpaceParameter.Occupancy, out @double))
                {
                    Occupancy = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                InternalCondition internalCondition = space.InternalCondition;
                if (internalCondition == null)
                {
                    return;
                }

                InternalConditionName = internalCondition.Name;

                if (internalCondition.TryGetValue(InternalConditionParameter.AreaPerPerson, out @double))
                {
                    AreaPerPerson = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.HeatingProfileName, out @string))
                {
                    HeatingProfileName = @string;
                }

                @double = Analytical.Query.HeatingDesignTemperature(internalCondition, profileLibrary);
                if (!double.IsNaN(@double))
                {
                    HeatingDesignTemperature = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.CoolingProfileName, out @string))
                {
                    CoolingProfileName = @string;
                }

                @double = Analytical.Query.CoolingDesignTemperature(internalCondition, profileLibrary);
                if (!double.IsNaN(@double))
                {
                    CoolingDesignTemperature = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyProfileName, out @string))
                {
                    OccupancyProfileName = @string;
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.OccupancySensibleGainPerPerson, out @double))
                {
                    OccupancySensibleGainPerPerson = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.OccupancyLatentGainPerPerson, out @double))
                {
                    OccupancyLatentGainPerPerson = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @double = Analytical.Query.OccupancyLatentGain(space);
                if (!double.IsNaN(@double))
                {
                    OccupancyLatentGainCalculated = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @double = Analytical.Query.OccupancySensibleGain(space);
                if (!double.IsNaN(@double))
                {
                    OccupancySensibleGainCalculated = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingProfileName, out @string))
                {
                    LightingProfileName = @string;
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingGain, out @double))
                {
                    LightingGain = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingGainPerArea, out @double))
                {
                    LightingGainPerArea = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @double = Analytical.Query.CalculatedLightingGain(space);
                if (!double.IsNaN(@double))
                {
                    LightingGainCalculated = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.LightingLevel, out @double))
                {
                    LightingLevel = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleProfileName, out @string))
                {
                    EquipmentSensibleProfileName = @string;
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGain, out @double))
                {
                    EquipmentSensibleGain = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out @double))
                {
                    EquipmentSensibleGainPerArea = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @double = Analytical.Query.CalculatedEquipmentSensibleGain(space);
                if (!double.IsNaN(@double))
                {
                    EquipmentSensibleGainCalculated = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentLatentProfileName, out @string))
                {
                    EquipmentLatentProfileName = @string;
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentLatentGain, out @double))
                {
                    EquipmentLatentGain = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.EquipmentLatentGainPerArea, out @double))
                {
                    EquipmentLatentGainPerArea = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @double = Analytical.Query.CalculatedEquipmentLatentGain(space);
                if (!double.IsNaN(@double))
                {
                    EquipmentLatentGainCalculated = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.HumidificationProfileName, out @string))
                {
                    HumidificationProfileName = @string;
                }

                Profile profile = internalCondition.GetProfile(ProfileType.Humidification, profileLibrary);
                if (profile != null && !double.IsNaN(profile.MaxValue))
                {
                    Humidification = Core.Query.Round(profile.MaxValue, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.DehumidificationProfileName, out @string))
                {
                    DehumidificationProfileName = @string;
                }

                profile = internalCondition.GetProfile(ProfileType.Dehumidification, profileLibrary);
                if (profile != null && !double.IsNaN(profile.MaxValue))
                {
                    Dehumidification = Core.Query.Round(profile.MaxValue, Core.Tolerance.MacroDistance);
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.InfiltrationProfileName, out @string))
                {
                    InfiltrationProfileName = @string;
                }

                if (internalCondition.TryGetValue(InternalConditionParameter.InfiltrationAirChangesPerHour, out @double))
                {
                    Infiltration = Core.Query.Round(@double, Core.Tolerance.MacroDistance);
                }

                @string = internalCondition.GetSystemTypeName<VentilationSystemType>();
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    VentilationSystemTypeName = @string;
                }

                @string = internalCondition.GetSystemTypeName<HeatingSystemType>();
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    HeatingSystemTypeName = @string;
                }

                @string = internalCondition.GetSystemTypeName<CoolingSystemType>();
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    CoolingSystemTypeName = @string;
                }
            }
        }
    }
}
