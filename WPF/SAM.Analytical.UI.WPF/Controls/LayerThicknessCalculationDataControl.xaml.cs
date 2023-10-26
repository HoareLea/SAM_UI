using SAM.Analytical.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for LayerThicknessCalculationDataControl.xaml
    /// </summary>
    public partial class LayerThicknessCalculationDataControl : System.Windows.Controls.UserControl
    {
        private ConstructionManager constructionManager;
        private LayerThicknessCalculationData layerThicknessCalculationData;

        public LayerThicknessCalculationDataControl()
        {
            InitializeComponent();

            foreach(HeatFlowDirection heatFlowDirection in Enum.GetValues(typeof(HeatFlowDirection)))
            {
                if(heatFlowDirection == HeatFlowDirection.Undefined)
                {
                    continue;
                }

                ComboBox_HeatFlowDirection.Items.Add(Core.Query.Description(heatFlowDirection));
            }

            TextBox_ThermalTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_MinThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_MaxThickness.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
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
                SetLayerThicknessCalculationData(layerThicknessCalculationData);
            }
        }

        public LayerThicknessCalculationData LayerThicknessCalculationData
        {
            get
            {
                return GetLayerThicknessCalculationData();
            }

            set
            {
                SetLayerThicknessCalculationData(value);
            }
        }

        private void SetLayerThicknessCalculationData(LayerThicknessCalculationData layerThicknessCalculationData)
        {
            this.layerThicknessCalculationData = layerThicknessCalculationData;

            if (layerThicknessCalculationData == null)
            {
                return;
            }

            TextBox_ConstructionName.Text = layerThicknessCalculationData.ConstructionName;

            ListBox_ConstructionLayers.Items.Clear();

            Construction construction = constructionManager?.GetConstructions(TextBox_ConstructionName.Text)?.FirstOrDefault();
            if (construction != null)
            {
                List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                if (constructionLayers != null && constructionLayers.Count != 0)
                {
                    foreach (ConstructionLayer constructionLayer in constructionLayers)
                    {
                        ListBox_ConstructionLayers.Items.Add(string.Format("{0} [{1}cm]", constructionLayer.Name, constructionLayer.Thickness * 100));
                    }
                }
            }

            if (layerThicknessCalculationData.LayerIndex != -1 && ListBox_ConstructionLayers.Items.Count > layerThicknessCalculationData.LayerIndex)
            {
                ListBox_ConstructionLayers.SelectedIndex = layerThicknessCalculationData.LayerIndex;
            }

            TextBox_ThermalTransmittance.Text = layerThicknessCalculationData.ThermalTransmittance.ToString();

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

            if(layerThicknessCalculationData.ThicknessRange != null)
            { 
                TextBox_MinThickness.Text = layerThicknessCalculationData.ThicknessRange.Min.ToString();
                TextBox_MaxThickness.Text = layerThicknessCalculationData.ThicknessRange.Max.ToString();
            }
        }

        private LayerThicknessCalculationData GetLayerThicknessCalculationData()
        {
            LayerThicknessCalculationData result = layerThicknessCalculationData == null ? new LayerThicknessCalculationData() : new LayerThicknessCalculationData(layerThicknessCalculationData);
            result.ConstructionName = TextBox_ConstructionName.Text;
            result.LayerIndex = ListBox_ConstructionLayers.SelectedIndex;

            if(Core.Query.TryConvert(TextBox_ThermalTransmittance.Text, out double thermalTransmittance))
            {
                result.ThermalTransmittance = thermalTransmittance;
            }

            if (Core.Query.TryGetEnum(ComboBox_HeatFlowDirection.Text, out HeatFlowDirection heatFlowDirection))
            {
                result.HeatFlowDirection = heatFlowDirection;
            }

            result.External = CheckBox_External.IsChecked == null || !CheckBox_External.IsChecked.HasValue ? layerThicknessCalculationData.External : CheckBox_External.IsChecked.Value;

            double minThickness = result.ThicknessRange == null ? double.NaN : layerThicknessCalculationData.ThicknessRange.Min;
            if (Core.Query.TryConvert(TextBox_MinThickness.Text, out double minThickness_Temp))
            {
                minThickness = minThickness_Temp;
            }

            double maxThickness = result.ThicknessRange == null ? double.NaN : layerThicknessCalculationData.ThicknessRange.Max;
            if (Core.Query.TryConvert(TextBox_MaxThickness.Text, out double maxThickness_Temp))
            {
                maxThickness = maxThickness_Temp;
            }

            result.ThicknessRange = new Core.Range<double>(minThickness, maxThickness);

            return result;
        }

        private void TextBox_PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }
    }
}
