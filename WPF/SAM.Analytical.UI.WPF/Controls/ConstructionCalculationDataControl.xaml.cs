using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConstructionCalculationDataControl.xaml
    /// </summary>
    public partial class ConstructionCalculationDataControl : UserControl
    {
        private ConstructionManager constructionManager;
        private IConstructionCalculationData constructionCalculationData;

        public ConstructionCalculationDataControl()
        {
            InitializeComponent();

            foreach(ConstructionCalculationType constructionCalculationType in Enum.GetValues(typeof(ConstructionCalculationType)))
            {
                if(constructionCalculationType == ConstructionCalculationType.Undefined)
                {
                    continue;
                }

                ComboBox_ConstructionCalculationType.Items.Add(Core.Query.Description(constructionCalculationType));
            }

            foreach (HeatFlowDirection heatFlowDirection in Enum.GetValues(typeof(HeatFlowDirection)))
            {
                if (heatFlowDirection == HeatFlowDirection.Undefined)
                {
                    continue;
                }

                ComboBox_HeatFlowDirection.Items.Add(Core.Query.Description(heatFlowDirection));
            }

            TextBox_ThermalTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_MinThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_MaxThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
        }

        private void LoadList()
        {
            ListBox_Main.Items.Clear();

            ConstructionCalculationType constructionCalculationType = ConstructionCalculationType;
            if(constructionCalculationType == ConstructionCalculationType.Undefined)
            {
                return;
            }

            switch(constructionCalculationType)
            {
                case ConstructionCalculationType.Constrcution:
                    ListBox_Main.SelectionMode = SelectionMode.Multiple;
                    List<Construction> constructions = constructionManager?.Constructions;
                    if(constructions != null)
                    {
                        foreach(Construction construction_Temp in constructions)
                        {
                            if(construction_Temp == null)
                            {
                                continue;
                            }

                            string name = string.IsNullOrWhiteSpace(construction_Temp.Name) ? "???" : construction_Temp.Name;
                            ListBoxItem listBoxItem = new ListBoxItem() { Content = name, Tag = construction_Temp };

                            ListBox_Main.Items.Add(listBoxItem);
                        }
                    }
                    return;

                case ConstructionCalculationType.LayerThickness:
                    ListBox_Main.SelectionMode = SelectionMode.Single;
                    Construction construction = constructionManager?.GetConstructions(TextBox_ConstructionName.Text)?.FirstOrDefault();
                    if(construction != null)
                    {
                        List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                        if (constructionLayers != null && constructionLayers.Count != 0)
                        {
                            foreach (ConstructionLayer constructionLayer in constructionLayers)
                            {
                                if(constructionLayer == null)
                                {
                                    continue;
                                }

                                string name = string.IsNullOrWhiteSpace(constructionLayer.Name) ? "???" : constructionLayer.Name;
                                if(!double.IsNaN(constructionLayer.Thickness))
                                {
                                    name = string.Format("{0} [{1}cm]", name, constructionLayer.Thickness * 100);
                                }

                                ListBoxItem listBoxItem = new ListBoxItem() { Content = name, Tag = constructionLayer };

                                ListBox_Main.Items.Add(listBoxItem);
                            }
                        }
                    }
                    return;
            }

        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return constructionManager;
            }

            set
            {
                constructionManager = value;
                SetConstructionCalculationData(constructionCalculationData);
            }
        }

        private IConstructionCalculationData GetConstructionCalculationData()
        {
            IConstructionCalculationData result = null;

            switch (ConstructionCalculationType)
            {
                case ConstructionCalculationType.Constrcution:
                    result = new ConstructionCalculationData();
                    break;

                case ConstructionCalculationType.LayerThickness:
                    result = new LayerThicknessCalculationData();
                    break;
            }

            if (result == null)
            {
                return null;
            }

            if(result is ConstructionCalculationData)
            {
                ConstructionCalculationData constructionCalculationData = (ConstructionCalculationData)result;
                constructionCalculationData.ConstructionName = TextBox_ConstructionName.Text;

                if (Core.Query.TryConvert(TextBox_ThermalTransmittance.Text, out double thermalTransmittance))
                {
                    constructionCalculationData.ThermalTransmittance = thermalTransmittance;
                }
                else
                {
                    constructionCalculationData.ThermalTransmittance = double.NaN;
                }

                if (Core.Query.TryGetEnum(ComboBox_HeatFlowDirection.Text, out HeatFlowDirection heatFlowDirection))
                {
                    constructionCalculationData.HeatFlowDirection = heatFlowDirection;
                }

                constructionCalculationData.External = CheckBox_External.IsChecked == null || !CheckBox_External.IsChecked.HasValue ? constructionCalculationData.External : CheckBox_External.IsChecked.Value;

                double minThickness = constructionCalculationData.ThicknessRange == null ? double.NaN : constructionCalculationData.ThicknessRange.Min;
                if (Core.Query.TryConvert(TextBox_MinThickness.Text, out double minThickness_Temp))
                {
                    minThickness = minThickness_Temp;
                }

                double maxThickness = constructionCalculationData.ThicknessRange == null ? double.NaN : constructionCalculationData.ThicknessRange.Max;
                if (Core.Query.TryConvert(TextBox_MaxThickness.Text, out double maxThickness_Temp))
                {
                    maxThickness = maxThickness_Temp;
                }

                constructionCalculationData.ThicknessRange = new Core.Range<double>(minThickness, maxThickness);

                if(ListBox_Main.SelectedItems != null)
                {
                    constructionCalculationData.ConstructionNames = new HashSet<string>();
                    foreach(ListBoxItem listBoxItem in ListBox_Main.SelectedItems)
                    {
                        string name = (listBoxItem?.Tag as Construction)?.Name;
                        if(string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        constructionCalculationData.ConstructionNames.Add(name);
                    }
                }
            }
            else if(result is LayerThicknessCalculationData)
            {
                LayerThicknessCalculationData layerThicknessCalculationData = (LayerThicknessCalculationData)result;

                layerThicknessCalculationData.ConstructionName = TextBox_ConstructionName.Text;
                layerThicknessCalculationData.LayerIndex = ListBox_Main.SelectedIndex;

                if (Core.Query.TryConvert(TextBox_ThermalTransmittance.Text, out double thermalTransmittance))
                {
                    layerThicknessCalculationData.ThermalTransmittance = thermalTransmittance;
                }
                else
                {
                    layerThicknessCalculationData.ThermalTransmittance = double.NaN;
                }

                if (Core.Query.TryGetEnum(ComboBox_HeatFlowDirection.Text, out HeatFlowDirection heatFlowDirection))
                {
                    layerThicknessCalculationData.HeatFlowDirection = heatFlowDirection;
                }

                layerThicknessCalculationData.External = CheckBox_External.IsChecked == null || !CheckBox_External.IsChecked.HasValue ? layerThicknessCalculationData.External : CheckBox_External.IsChecked.Value;

                double minThickness = layerThicknessCalculationData.ThicknessRange == null ? double.NaN : layerThicknessCalculationData.ThicknessRange.Min;
                if (Core.Query.TryConvert(TextBox_MinThickness.Text, out double minThickness_Temp))
                {
                    minThickness = minThickness_Temp;
                }

                double maxThickness = layerThicknessCalculationData.ThicknessRange == null ? double.NaN : layerThicknessCalculationData.ThicknessRange.Max;
                if (Core.Query.TryConvert(TextBox_MaxThickness.Text, out double maxThickness_Temp))
                {
                    maxThickness = maxThickness_Temp;
                }

                layerThicknessCalculationData.ThicknessRange = new Core.Range<double>(minThickness, maxThickness);
            }

            return result;
        }

        private void SetConstructionCalculationData(IConstructionCalculationData constructionCalculationData)
        {
            this.constructionCalculationData = constructionCalculationData;
            ConstructionCalculationType = constructionCalculationData is ConstructionCalculationData ? ConstructionCalculationType.Constrcution : ConstructionCalculationType.LayerThickness;

            if(constructionCalculationData is ConstructionCalculationData)
            {
                ConstructionCalculationData constructionCalculationData_Temp = (ConstructionCalculationData)constructionCalculationData;

                TextBox_ConstructionName.Text = constructionCalculationData_Temp.ConstructionName;

                TextBox_ThermalTransmittance.Text = double.IsNaN(constructionCalculationData_Temp.ThermalTransmittance) ? null : constructionCalculationData_Temp.ThermalTransmittance.ToString();

                string string_HeatFlowDirection = Core.Query.Description(constructionCalculationData_Temp.HeatFlowDirection);
                for (int i = 0; i < ComboBox_HeatFlowDirection.Items.Count; i++)
                {
                    if (string_HeatFlowDirection == ComboBox_HeatFlowDirection.Items[i]?.ToString())
                    {
                        ComboBox_HeatFlowDirection.SelectedIndex = i;
                        break;
                    }
                }

                CheckBox_External.IsChecked = constructionCalculationData_Temp.External;

                if (constructionCalculationData_Temp.ThicknessRange != null)
                {
                    TextBox_MinThickness.Text = constructionCalculationData_Temp.ThicknessRange.Min.ToString();
                    TextBox_MaxThickness.Text = constructionCalculationData_Temp.ThicknessRange.Max.ToString();
                }

                LoadList();

                ListBox_Main.SelectedItems.Clear();

                HashSet<string> constructionNames = constructionCalculationData_Temp.ConstructionNames;
                if(constructionNames != null && constructionNames.Count != 0)
                {
                    foreach(TreeViewItem treeViewItem in ListBox_Main.Items)
                    {
                        Construction construction = treeViewItem?.Tag as Construction;
                        if(construction == null)
                        {
                            continue;
                        }

                        if(!constructionNames.Contains(construction.Name))
                        {
                            continue;
                        }

                        ListBox_Main.SelectedItems.Add(treeViewItem);
                    }
                }
            }
            else if(constructionCalculationData is LayerThicknessCalculationData)
            {
                LayerThicknessCalculationData layerThicknessCalculationData = (LayerThicknessCalculationData)constructionCalculationData;

                TextBox_ConstructionName.Text = layerThicknessCalculationData.ConstructionName;

                TextBox_ThermalTransmittance.Text = double.IsNaN(layerThicknessCalculationData.ThermalTransmittance) ? null : layerThicknessCalculationData.ThermalTransmittance.ToString();

                string string_HeatFlowDirection = Core.Query.Description(layerThicknessCalculationData.HeatFlowDirection);
                for (int i = 0; i < ComboBox_HeatFlowDirection.Items.Count; i++)
                {
                    if (string_HeatFlowDirection == ComboBox_HeatFlowDirection.Items[i]?.ToString())
                    {
                        ComboBox_HeatFlowDirection.SelectedIndex = i;
                        break;
                    }
                }

                CheckBox_External.IsChecked = layerThicknessCalculationData.External;

                if (layerThicknessCalculationData.ThicknessRange != null)
                {
                    TextBox_MinThickness.Text = layerThicknessCalculationData.ThicknessRange.Min.ToString();
                    TextBox_MaxThickness.Text = layerThicknessCalculationData.ThicknessRange.Max.ToString();
                }

                LoadList();

                if (layerThicknessCalculationData.LayerIndex != -1 && ListBox_Main.Items.Count > layerThicknessCalculationData.LayerIndex)
                {
                    ListBox_Main.SelectedIndex = layerThicknessCalculationData.LayerIndex;
                }
            }
        }

        public IConstructionCalculationData ConstructionCalculationData
        {
            get
            {
                return GetConstructionCalculationData();
            }

            set
            {
                SetConstructionCalculationData(value);
            }
        }

        private ConstructionCalculationType GetConstructionCalculationType()
        {
            string text = ComboBox_ConstructionCalculationType.SelectedItem?.ToString();
            if(string.IsNullOrWhiteSpace(text))
            {
                return ConstructionCalculationType.Undefined;
            }

            if(!Core.Query.TryGetEnum(text, out ConstructionCalculationType result))
            {
                return ConstructionCalculationType.Undefined;
            }

            return result;
        }

        private void SetConstructionCalculationType(ConstructionCalculationType constructionCalculationType)
        {
            string text = constructionCalculationType == ConstructionCalculationType.Undefined ? null : Core.Query.Description(constructionCalculationType);

            ComboBox_ConstructionCalculationType.Text = text;
        }

        public ConstructionCalculationType ConstructionCalculationType
        {
            get
            {
                return GetConstructionCalculationType();
            }

            set
            {
                SetConstructionCalculationType(value);
            }
        }

        private void TextBox_PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }

        private void ComboBox_ConstructionCalculationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadList();
        }
    }
}
