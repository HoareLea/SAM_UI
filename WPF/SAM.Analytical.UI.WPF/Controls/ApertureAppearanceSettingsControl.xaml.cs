using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureAppearanceSettingsControl.xaml
    /// </summary>
    public partial class ApertureAppearanceSettingsControl : UserControl
    {
        
        private AdjacencyCluster adjacencyCluster;

        public event EventHandler ValueChanged;

        public ApertureAppearanceSettingsControl()
        {
            InitializeComponent();
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return AdjacencyCluster;
            }

            set
            {
                SetAdjacencyCluster(value);
            }
        }

        private void LoadParameterNames()
        {
            comboBox_ParameterName.Items.Clear();
            comboBox_ParameterName.Items.Add(string.Empty);

            if (adjacencyCluster == null)
            {
                return;
            }

            if (radioButton_Default.IsChecked.HasValue && radioButton_Default.IsChecked.Value)
            {
                comboBox_ParameterName.Visibility = Visibility.Hidden;
                label_ParameterName.Visibility = Visibility.Hidden;
                return;
            }
            else
            {
                comboBox_ParameterName.Visibility = Visibility.Visible;
                label_ParameterName.Visibility = Visibility.Visible;
            }

            Type type = null;

            List<string> parameterNames_ToBeAdded = new List<string>();

            List<object> @objects = new List<object>();
            if (radioButton_ApertureConstruction.IsChecked.HasValue && radioButton_ApertureConstruction.IsChecked.Value)
            {
                IEnumerable<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
                if (apertureConstructions != null)
                {
                    foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                    {
                        if (apertureConstruction == null)
                        {
                            continue;
                        }

                        objects.Add(apertureConstruction);
                    }
                }
                type = typeof(ApertureConstruction);
            }
            else if (radioButton_Panel.IsChecked.HasValue && radioButton_Panel.IsChecked.Value)
            {
                IEnumerable<Panel> panels = adjacencyCluster.GetPanels();
                if (panels != null)
                {
                    foreach (Panel panel in panels)
                    {
                        if (panel == null)
                        {
                            continue;
                        }

                        objects.Add(panel);
                    }
                }
                type = typeof(Panel);
            }
            else if (radioButton_Aperture.IsChecked.HasValue && radioButton_Aperture.IsChecked.Value)
            {
                IEnumerable<Aperture> apertures = adjacencyCluster.GetApertures();
                if (apertures != null)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        if (aperture == null)
                        {
                            continue;
                        }

                        objects.Add(aperture);
                    }
                }
                type = typeof(Aperture);
            }
            else if (radioButton_OpeningProperties.IsChecked.HasValue && radioButton_OpeningProperties.IsChecked.Value)
            {
                IEnumerable<Aperture> apertures = adjacencyCluster.GetApertures();
                if (apertures != null)
                {
                    foreach (Aperture aperture in apertures)
                    {
                        if (aperture == null)
                        {
                            continue;
                        }

                        if(!aperture.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                        {
                            continue;
                        }

                        objects.Add(openingProperties);
                    }
                }
                type = typeof(IOpeningProperties);
            }

            Core.UI.WPF.Modify.AddParameterNames(comboBox_ParameterName, objects, type, new string[] { "ToString", "Location", "InternalCondition", "ToJObject", "ParameterSets", "HashCode", "Type", "ApertureConstruction", "BoundingBox", "Clone", "Color", "ExternalEdge3D", "ExternalEdge2D", "Face3D", "FrameFace3D", "PaneFace3Ds", "PlanarBoundary3D", "Plane", "FrameConstructionLayers", "PaneConstructionLayers", "Apertures", "Construction", "InternalPoint3D", "Origin", "Normal" }, parameterNames_ToBeAdded);
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            LoadParameterNames();
            ValueChanged?.Invoke(this, new EventArgs());
        }

        public ApertureAppearanceSettings ApertureAppearanceSettings
        {
            get
            {
                return GetApertureAppearanceSettings();
            }

            set
            {
                SetApertureAppearanceSettings(value);
            }
        }

        private ApertureAppearanceSettings GetApertureAppearanceSettings()
        {
            if (radioButton_Panel.IsChecked.HasValue && radioButton_Panel.IsChecked.Value)
            {
                return new ApertureAppearanceSettings(new PanelAppearanceSettings( comboBox_ParameterName?.SelectedItem?.ToString()));
            }

            if (radioButton_Aperture.IsChecked.HasValue && radioButton_Aperture.IsChecked.Value)
            {
                return new ApertureAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString());
            }

            if (radioButton_ApertureConstruction.IsChecked.HasValue && radioButton_ApertureConstruction.IsChecked.Value)
            {
                return new ApertureAppearanceSettings(new ApertureConstructionAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString()));
            }

            if (radioButton_OpeningProperties.IsChecked.HasValue && radioButton_OpeningProperties.IsChecked.Value)
            {
                return new ApertureAppearanceSettings(new OpeningPropertiesAppearanceSettings(comboBox_ParameterName?.SelectedItem?.ToString()));
            }

            if (radioButton_Default.IsChecked.HasValue && radioButton_Default.IsChecked.Value)
            {
                return null;
            }

            return null;
        }

        private void SetApertureAppearanceSettings(ApertureAppearanceSettings apertureAppearanceSettings)
        {
            Core.UI.IAppearanceSettings appearanceSettings = apertureAppearanceSettings?.GetValueAppearanceSettings<ValueAppearanceSettings>();
            if (appearanceSettings == null)
            {
                radioButton_Default.IsChecked = true;
                LoadParameterNames();
                return;
            }

            string parameterName = null;
            if (appearanceSettings is ParameterAppearanceSettings)
            {
                parameterName = ((ParameterAppearanceSettings)appearanceSettings).ParameterName;
            }
            else if (appearanceSettings is ITypeAppearanceSettings)
            {
                parameterName = ((ITypeAppearanceSettings)appearanceSettings).GetValueAppearanceSettings<ParameterAppearanceSettings>()?.ParameterName;
            }

            if (parameterName == null)
            {
                return;
            }

            if (appearanceSettings is ApertureConstructionAppearanceSettings)
            {
                radioButton_ApertureConstruction.IsChecked = true;
            }
            else if (appearanceSettings is PanelAppearanceSettings)
            {
                radioButton_Panel.IsChecked = true;
            }
            else if (appearanceSettings is ApertureAppearanceSettings)
            {
                radioButton_Aperture.IsChecked = true;
            }
            else if (appearanceSettings is OpeningPropertiesAppearanceSettings)
            {
                radioButton_OpeningProperties.IsChecked = true;
            }
            else
            {
                radioButton_Aperture.IsChecked = true;
            }

            LoadParameterNames();
            if (parameterName != null)
            {
                comboBox_ParameterName.SelectedItem = parameterName;
            }
        }

        private void comboBox_ParameterName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }

    }


}
