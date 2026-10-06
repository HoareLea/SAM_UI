// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Architectural;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalTwoDimensionalViewSettingsControl.xaml
    /// </summary>
    public partial class AnalyticalTwoDimensionalViewSettingsControl : UserControl
    {
        private TwoDimensionalViewSettings twoDimensionalViewSettings;

        /// <summary>
        /// The view's Part F airflow presentation, edited in its own dialog and stored back on the view.
        /// <para>
        /// Null means this view has never been told about Part F, which is not the same as being told to turn
        /// it off: a null is left absent from the view rather than written as a disabled setting, so the
        /// hundreds of views saved before the annotation existed stay exactly as they are.
        /// </para>
        /// </summary>
        private PartFAirflowViewSettings partFAirflowViewSettings;

        /// <summary>
        /// The view's Ventilation Design presentation, edited in its own dialog and stored back on the
        /// view - the DESIGN airflow overlay, separate from and orthogonal to <see cref="partFAirflowViewSettings"/>.
        /// Null means this view has never been told about it, kept absent rather than written as disabled
        /// for the same reason as the Part F settings.
        /// </summary>
        private DesignAirFlowViewSettings designAirFlowViewSettings;

        private AdjacencyCluster adjacencyCluster_PartF;

        /// <summary>
        /// True while this panel is editing a view that does not exist yet.
        /// <para>
        /// The one thing that decides whether the Part F preset may be applied. This same panel edits a new
        /// view and an existing one, and the difference matters: choosing the Part F colour scheme on a NEW
        /// view should give the engineer a working Part F drawing, and doing the same on a view somebody has
        /// already set up must not overwrite how they set it up. Set by whoever is creating the view; false by
        /// default, which is the safe direction.
        /// </para>
        /// </summary>
        public bool IsNewViewSettings { get; set; }

        public AnalyticalTwoDimensionalViewSettingsControl()
        {
            InitializeComponent();

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
            elevationControl.ValueChanged += ElevationControl_ValueChanged;
            spaceAppearanceSettingsControl.ValueChanged += SpaceAppearanceSettingsControl_ValueChanged;
        }

        public AnalyticalTwoDimensionalViewSettingsControl(TwoDimensionalViewSettings twoDimensionalViewSettings, AnalyticalModel analyticalModel)
        {
            InitializeComponent();

            SetAnalyticalModel(analyticalModel);

            SetAnalyticalTwoDimensionalViewSettings(twoDimensionalViewSettings);

            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;

            elevationControl.ValueChanged += ElevationControl_ValueChanged;
            spaceAppearanceSettingsControl.ValueChanged += SpaceAppearanceSettingsControl_ValueChanged;
        }

        private void ElevationControl_ValueChanged(object sender, EventArgs e)
        {
            UpdateName();
        }

        private void SpaceAppearanceSettingsControl_ValueChanged(object sender, EventArgs e)
        {
            ApplyPartFAirflowPreset();

            UpdateName();
        }

        /// <summary>
        /// Gives a NEW view a usable Part F drawing the moment its colour scheme is set to Part F data,
        /// instead of leaving the engineer to discover that nine more options are behind another dialog.
        /// <para>
        /// Deliberately narrow. It applies only while <see cref="IsNewViewSettings"/> is true - so no existing
        /// view is ever touched - and only where the view has no Part F settings yet, so a person who has
        /// already configured this view, or reopened its settings, or duplicated a Part F view, keeps exactly
        /// what they had. Once applied it is theirs to edit; selecting the scheme again does not reset it.
        /// </para>
        /// <para>
        /// It does not turn the annotation OFF again if the colour scheme is then changed to something else.
        /// The two are independent - the fills say what each room is, the tags say what its air does - and a
        /// person who wants the tags without the Part F colours is entitled to that combination.
        /// </para>
        /// </summary>
        private void ApplyPartFAirflowPreset()
        {
            if (!IsNewViewSettings || partFAirflowViewSettings is not null || !IsPartFColorScheme())
            {
                return;
            }

            //Qualified: an unqualified Create here is SAM.Analytical.UI.WPF's own, which shadows it.
            partFAirflowViewSettings = Analytical.UI.Create.PartFAirflowViewSettings(adjacencyCluster_PartF);

            UpdatePartFAirflowButton();
        }

        /// <summary>
        /// Whether the colour scheme currently chosen is the Part F one. Asked of the settings object rather
        /// than of the radio button, so it is the same question the rest of the code asks and no control name
        /// or caption is depended on.
        /// </summary>
        private bool IsPartFColorScheme()
        {
            return spaceAppearanceSettingsControl.SpaceAppearanceSettings?.GetValueAppearanceSettings<ValueAppearanceSettings>() is PartFSpaceDataAppearanceSettings;
        }

        public TwoDimensionalViewSettings TwoDimensionalViewSettings
        {
            get
            {
                return GetTwoDimensionalViewSettings();
            }

            set
            {
                SetAnalyticalTwoDimensionalViewSettings(value);
            }
        }

        private void SetAnalyticalModel(AnalyticalModel analyticalModel)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            //Kept for the Part F airflow dialog, which lists the model's zone categories and dwelling zones.
            adjacencyCluster_PartF = adjacencyCluster;

            spaceAppearanceSettingsControl.AdjacencyCluster = adjacencyCluster;

            List<Level> levels = Analytical.Create.Levels(adjacencyCluster, false);
            levels?.Sort((x, y) => x.Elevation.CompareTo(y.Elevation));

            elevationControl.Levels = levels;
            if (levels != null && levels.Count != 0)
            {
                elevationControl.SelectedLevel = levels[0];
            }

            comboBox_Group.Items.Clear();

            List<ViewSettings> viewSettingsList = analyticalModel.ViewSettings<ViewSettings>();
            if(viewSettingsList != null && viewSettingsList.Count != 0)
            {
                HashSet<string> groups = new HashSet<string>();
                foreach(ViewSettings viewSettings in viewSettingsList)
                {
                    if(viewSettings.TryGetValue(ViewSettingsParameter.Group, out string group) && !string.IsNullOrWhiteSpace(group))
                    {
                        groups.Add(group);
                    }
                }

                List<string> groups_Sorted = new List<string>(groups);
                groups_Sorted.Sort();

                foreach(string group in groups_Sorted)
                {
                    comboBox_Group.Items.Add(group);
                }
            }
        }

        private void SetAnalyticalTwoDimensionalViewSettings(TwoDimensionalViewSettings twoDimensionalViewSettings)
        {
            this.twoDimensionalViewSettings = twoDimensionalViewSettings;

            checkBox_Visibilty_Space.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Space));
            checkBox_Visibilty_Panel.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Panel));
            checkBox_Visibilty_Aperture.IsChecked = twoDimensionalViewSettings.ContainsType(typeof(Aperture));

            TextAppearance textAppearance = twoDimensionalViewSettings.TextAppearance;
            if(textAppearance == null)
            {
                textAppearance = Geometry.Object.Query.DefaultTextAppearance();
            }

            checkBox_TextVisibility.IsChecked = textAppearance.Opacity != 0;

            textBox_TextSize.Text = textAppearance.Height.ToString();

            spaceAppearanceSettingsControl.SpaceAppearanceSettings = twoDimensionalViewSettings.GetValueAppearanceSettings<SpaceAppearanceSettings>()?.FirstOrDefault();

            textBox_Name.Text = twoDimensionalViewSettings.Name;

            elevationControl.Elevation = twoDimensionalViewSettings.Plane.Origin.Z;

            checkBox_UseDefaultName.IsChecked = true;
            if(twoDimensionalViewSettings.TryGetValue(ViewSettingsParameter.UseDefaultName, out bool useDefaultName))
            {
                checkBox_UseDefaultName.IsChecked = useDefaultName;
            }

            comboBox_Group.Text = string.Empty;
            if(twoDimensionalViewSettings.TryGetValue(ViewSettingsParameter.Group, out string group))
            {
                comboBox_Group.Text = group;
            }

            partFAirflowViewSettings = twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.PartFAirflow, out PartFAirflowViewSettings partFAirflowViewSettings_Temp)
                ? partFAirflowViewSettings_Temp
                : null;

            UpdatePartFAirflowButton();

            designAirFlowViewSettings = twoDimensionalViewSettings.TryGetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, out DesignAirFlowViewSettings designAirFlowViewSettings_Temp)
                ? designAirFlowViewSettings_Temp
                : null;

            UpdateVentilationDesignButton();
        }

        /// <summary>
        /// Opens the Ventilation Design dialog for this view. A separate dialog from Part F Airflow's, on
        /// the same footing: the two overlays read different authorities and are independently switched.
        /// </summary>
        private void button_VentilationDesign_Click(object sender, RoutedEventArgs e)
        {
            DesignAirFlowViewSettingsWindow designAirFlowViewSettingsWindow = new()
            {
                DesignAirFlowViewSettings = designAirFlowViewSettings,

                //Fully qualified: an unqualified Window here is SAM.Analytical.Window, the architectural element.
                Owner = System.Windows.Window.GetWindow(this),
            };

            if (designAirFlowViewSettingsWindow.ShowDialog() != true)
            {
                return;
            }

            designAirFlowViewSettings = designAirFlowViewSettingsWindow.DesignAirFlowViewSettings;

            UpdateVentilationDesignButton();
        }

        /// <summary>Says on the button whether this view carries the overlay, so it reads at a glance.</summary>
        private void UpdateVentilationDesignButton()
        {
            button_VentilationDesign.Content = designAirFlowViewSettings is not null && designAirFlowViewSettings.Enabled
                ? "Ventilation Design: on..."
                : "Ventilation Design...";
        }

        /// <summary>
        /// Opens the Part F airflow dialog for this view. Kept as a dialog rather than another group box on an
        /// already crowded panel, and separate from the colour scheme on purpose: the two are independent and
        /// are meant to be used together.
        /// </summary>
        private void button_PartFAirflow_Click(object sender, RoutedEventArgs e)
        {
            PartFAirflowViewSettingsWindow partFAirflowViewSettingsWindow = new()
            {
                AdjacencyCluster = adjacencyCluster_PartF,
            };

            //Assigned after the model, because the dwelling list has to exist before a saved dwelling can be
            //selected in it.
            partFAirflowViewSettingsWindow.PartFAirflowViewSettings = partFAirflowViewSettings;

            //Fully qualified: an unqualified Window here is SAM.Analytical.Window, the architectural element.
            partFAirflowViewSettingsWindow.Owner = System.Windows.Window.GetWindow(this);

            if (partFAirflowViewSettingsWindow.ShowDialog() != true)
            {
                return;
            }

            partFAirflowViewSettings = partFAirflowViewSettingsWindow.PartFAirflowViewSettings;

            UpdatePartFAirflowButton();
        }

        /// <summary>
        /// Says on the button whether this view carries the annotation, so it reads at a glance.
        /// <para>
        /// The middle case is the one that matters, and it is asked BEFORE <c>Enabled</c>. Where the preset
        /// could not tell what the drawing is about - several dwelling categories, or a zoned model with no
        /// dwelling among them - the annotation is on and the scope is undecided, so nothing is assessed and
        /// nothing is drawn. The switch alone would then read "on" over an empty plan; the button says what
        /// is actually outstanding instead, which is the one action left.
        /// </para>
        /// </summary>
        private void UpdatePartFAirflowButton()
        {
            button_PartFAirflow.Content =
                partFAirflowViewSettings is null ? "Part F Airflow..."
                : !partFAirflowViewSettings.HasDwellingScope ? "Part F Airflow: choose the dwellings..."
                : partFAirflowViewSettings.Enabled ? "Part F Airflow: on..."
                : "Part F Airflow...";
        }

        private TwoDimensionalViewSettings GetTwoDimensionalViewSettings()
        {
            TwoDimensionalViewSettings result = new TwoDimensionalViewSettings(textBox_Name.Text, twoDimensionalViewSettings);

            CheckBox checkBox;

            List<Type> types = new List<Type>();

            checkBox = checkBox_Visibilty_Space;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Space));
            }

            checkBox = checkBox_Visibilty_Aperture;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Aperture));
            }

            checkBox = checkBox_Visibilty_Panel;
            if (checkBox.IsChecked != null && checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                types.Add(typeof(Panel));
            }

            result.SetTypes(types);

            TextAppearance textAppearance = Geometry.Object.Query.DefaultTextAppearance();
            textAppearance.Opacity = checkBox_TextVisibility.IsChecked != null && checkBox_TextVisibility.IsChecked.HasValue && checkBox_TextVisibility.IsChecked.Value ? 1 : 0;

            if (Core.Query.TryConvert(textBox_TextSize.Text, out double textSize))
            {
                textAppearance.Height = textSize;
            }

            result.TextAppearance = textAppearance;

            if (spaceAppearanceSettingsControl.SpaceAppearanceSettings == null)
            {
                result.RemoveAppearanceSettings<SpaceAppearanceSettings>();
            }
            else
            {
                result.AddAppearanceSettings(spaceAppearanceSettingsControl.SpaceAppearanceSettings);
            }

            result.Plane = Geometry.Spatial.Create.Plane(elevationControl.Elevation);

            //Written only where the view has one. Absent means "never told about Part F", and writing a
            //disabled setting instead would change every view a person merely opened this dialog on.
            if (partFAirflowViewSettings is not null)
            {
                result.SetValue(AnalyticalViewSettingsParameter.PartFAirflow, partFAirflowViewSettings);
            }

            //Same rule as Part F: absent means "never told about it", not "off".
            if (designAirFlowViewSettings is not null)
            {
                result.SetValue(AnalyticalViewSettingsParameter.VentilationDesignAirflow, designAirFlowViewSettings);
            }

            if (checkBox_UseDefaultName.IsChecked != null && checkBox_UseDefaultName.IsChecked.HasValue)
            {
                result.SetValue(ViewSettingsParameter.UseDefaultName, checkBox_UseDefaultName.IsChecked.Value);
            }

            if (!string.IsNullOrWhiteSpace(comboBox_Group.Text))
            {
                result.SetValue(ViewSettingsParameter.Group, comboBox_Group.Text);
            }

            return result;
        }

        private void checkBox_Visibilty_Space_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            groupBox_ColorScheme.IsEnabled = checkBox_Visibilty_Space.IsChecked != null && checkBox_Visibilty_Space.IsChecked.Value;
        }

        private void checkBox_UseDefaultName_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateName();
        }

        private void UpdateName()
        {
            bool @checked = checkBox_UseDefaultName.IsChecked != null && checkBox_UseDefaultName.IsChecked.HasValue && checkBox_UseDefaultName.IsChecked.Value;

            textBox_Name.IsEnabled = !@checked;

            if (@checked)
            {
                textBox_Name.Text = Query.DefaultName(elevationControl.SelectedLevel, elevationControl.Elevation, spaceAppearanceSettingsControl.SpaceAppearanceSettings);
            }
        }

        private void textBox_TextSize_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            SAM.Core.UI.WPF.Query.ControlText_NumberOnly(sender, e);
        }
    }
}
