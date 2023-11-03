using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureConstructionCalculationDataControl.xaml
    /// </summary>
    public partial class ApertureConstructionCalculationDataControl : UserControl
    {
        private ConstructionManager constructionManager;
        private ApertureConstructionCalculationData apertureConstructionCalculationData;

        public ApertureConstructionCalculationDataControl()
        {
            InitializeComponent();
        }

        public ApertureConstructionCalculationData ApertureConstructionCalculationData
        {
            get
            {
                return GetApertureConstructionCalculationData();
            }

            set
            {
                SetApertureConstructionCalculationData(value);
            }
        }

        private ApertureConstructionCalculationData GetApertureConstructionCalculationData()
        {
            ApertureConstructionCalculationData result = apertureConstructionCalculationData == null ? new ApertureConstructionCalculationData() : new ApertureConstructionCalculationData(apertureConstructionCalculationData);
            result.ApertureConstructionName = TextBox_ApertureConstructionName.Text;

            if(ListBox_ApertureConstructions.SelectedItems != null)
            {
                result.ApertureConstructionNames = new HashSet<string>();
                foreach(ListBoxItem listBoxItem in ListBox_ApertureConstructions.SelectedItems)
                {
                    ApertureConstruction apertureConstruction = listBoxItem?.Tag as ApertureConstruction;
                    if ((apertureConstruction == null))
                    {
                        continue;
                    }
                    result.ApertureConstructionNames.Add(apertureConstruction.Name);
                }
            }

            if (Core.Query.TryConvert(TextBox_PaneThermalTransmittance.Text, out double paneThermalTransmittance))
            {
                result.PaneThermalTransmittance = paneThermalTransmittance;
            }
            else
            {
                result.PaneThermalTransmittance = double.NaN;
            }

            if (Core.Query.TryConvert(TextBox_FrameThermalTransmittance.Text, out double frameThermalTransmittance))
            {
                result.FrameThermalTransmittance = frameThermalTransmittance;
            }
            else
            {
                result.FrameThermalTransmittance = double.NaN;
            }

            if (Core.Query.TryGetEnum(ComboBox_HeatFlowDirection.Text, out HeatFlowDirection heatFlowDirection))
            {
                result.HeatFlowDirection = heatFlowDirection;
            }

            result.External = CheckBox_External.IsChecked == null || !CheckBox_External.IsChecked.HasValue ? apertureConstructionCalculationData.External : CheckBox_External.IsChecked.Value;

            double minThickness = result.ThicknessRange == null ? double.NaN : apertureConstructionCalculationData.ThicknessRange.Min;
            if (Core.Query.TryConvert(TextBox_MinThickness.Text, out double minThickness_Temp))
            {
                minThickness = minThickness_Temp;
            }

            double maxThickness = result.ThicknessRange == null ? double.NaN : apertureConstructionCalculationData.ThicknessRange.Max;
            if (Core.Query.TryConvert(TextBox_MaxThickness.Text, out double maxThickness_Temp))
            {
                maxThickness = maxThickness_Temp;
            }

            result.ThicknessRange = new Core.Range<double>(minThickness, maxThickness);

            return result;
        }

        private void SetApertureConstructionCalculationData(ApertureConstructionCalculationData apertureConstructionCalculationData)
        {
            this.apertureConstructionCalculationData = apertureConstructionCalculationData;

            if (apertureConstructionCalculationData == null)
            {
                return;
            }

            TextBox_ApertureConstructionName.Text = apertureConstructionCalculationData.ApertureConstructionName;

            ListBox_ApertureConstructions.Items.Clear();

            HashSet<string> apertureConstructionNames = apertureConstructionCalculationData.ApertureConstructionNames;

            List<ApertureConstruction> apertureConstructions = constructionManager?.ApertureConstructions?.FindAll(x => apertureConstructionCalculationData.ApertureType == x.ApertureType);
            if (apertureConstructions != null)
            {
                foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    if(apertureConstruction == null)
                    {
                        continue;
                    }

                    string name = string.IsNullOrWhiteSpace(apertureConstruction.Name) ? "???" : apertureConstruction.Name;

                    ListBoxItem listBoxItem = new ListBoxItem() { Content = name, Tag = apertureConstruction };

                    ListBox_ApertureConstructions.Items.Add(listBoxItem);

                    if(apertureConstructionNames != null && apertureConstructionNames.Contains(name))
                    {
                        ListBox_ApertureConstructions.SelectedItems.Add(listBoxItem);
                    }
                }
            }

            TextBox_PaneThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationData.PaneThermalTransmittance) ? null : apertureConstructionCalculationData.PaneThermalTransmittance.ToString();
            TextBox_FrameThermalTransmittance.Text = double.IsNaN(apertureConstructionCalculationData.FrameThermalTransmittance) ? null : apertureConstructionCalculationData.FrameThermalTransmittance.ToString();

            string string_HeatFlowDirection = Core.Query.Description(apertureConstructionCalculationData.HeatFlowDirection);
            for (int i = 0; i < ComboBox_HeatFlowDirection.Items.Count; i++)
            {
                if (string_HeatFlowDirection == ComboBox_HeatFlowDirection.Items[i]?.ToString())
                {
                    ComboBox_HeatFlowDirection.SelectedIndex = i;
                    break;
                }
            }

            CheckBox_External.IsChecked = apertureConstructionCalculationData.External;

            if (apertureConstructionCalculationData.ThicknessRange != null)
            {
                TextBox_MinThickness.Text = apertureConstructionCalculationData.ThicknessRange.Min.ToString();
                TextBox_MaxThickness.Text = apertureConstructionCalculationData.ThicknessRange.Max.ToString();
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
                SetApertureConstructionCalculationData(apertureConstructionCalculationData);
            }
        }

    }
}
