using SAM.Analytical.Tas;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ThermalTransmittanceCalculationResultControl.xaml
    /// </summary>
    public partial class ThermalTransmittanceCalculationResultControl : UserControl
    {
        private ConstructionManager constructionManager;

        private List<ThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults;

        public ThermalTransmittanceCalculationResultControl()
        {
            InitializeComponent();

            TextBox_TotalSolarEnergyTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_TotalSolarEnergyTransmittance_Min.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_TotalSolarEnergyTransmittance_Max.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;

            TextBox_LightTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_LightTransmittance_Min.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_LightTransmittance_Max.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;

            TextBox_ThermalTransmittance.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_ThermalTransmittance_Min.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;
            TextBox_ThermalTransmittance_Max.PreviewTextInput += TextBox_PreviewTextInput_NumberOnly;

            TextBox_TotalSolarEnergyTransmittance.TextChanged += TextBox_TextChanged;
            TextBox_TotalSolarEnergyTransmittance_Min.TextChanged += TextBox_TextChanged;
            TextBox_TotalSolarEnergyTransmittance_Max.TextChanged += TextBox_TextChanged;

            TextBox_LightTransmittance.TextChanged += TextBox_TextChanged;
            TextBox_LightTransmittance_Min.TextChanged += TextBox_TextChanged;
            TextBox_LightTransmittance_Max.TextChanged += TextBox_TextChanged;

            TextBox_ThermalTransmittance.TextChanged += TextBox_TextChanged;
            TextBox_ThermalTransmittance_Min.TextChanged += TextBox_TextChanged;
            TextBox_ThermalTransmittance_Max.TextChanged += TextBox_TextChanged;

            ThermalTransmittanceFilter = new ThermalTransmittanceFilter();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetThermalTransmittanceCalculationResults(thermalTransmittanceCalculationResults);
        }

        public List<ThermalTransmittanceCalculationResult> ThermalTransmittanceCalculationResults
        {
            get
            {
                return thermalTransmittanceCalculationResults;
            }

            set
            {
                SetThermalTransmittanceCalculationResults(value);
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
                SetConstructionManager(value);
            }
        }

        public ThermalTransmittanceCalculationResult ThermalTransmittanceCalculationResult
        {
            get
            {
                return (DataGrid_Main.SelectedItem as DisplayThermalTransmittanceCalculationResult).ThermalTransmittanceCalculationResult;
            }
        }

        private void SetConstructionManager(ConstructionManager constructionManager)
        {
            this.constructionManager = constructionManager;
            SetThermalTransmittanceCalculationResults(thermalTransmittanceCalculationResults);
        }

        private void SetThermalTransmittanceCalculationResults(IEnumerable<ThermalTransmittanceCalculationResult> thermalTransmittanceCalculationResults)
        {
            this.thermalTransmittanceCalculationResults = null;

            DataGrid_Main.ItemsSource = null;

            if(thermalTransmittanceCalculationResults == null)
            {
                return;
            }

            this.thermalTransmittanceCalculationResults = thermalTransmittanceCalculationResults.ToList();

            ThermalTransmittanceFilter thermalTransmittanceFilter = ThermalTransmittanceFilter;

            List<DisplayThermalTransmittanceCalculationResult> displayThermalTransmittanceCalculationResults = new List<DisplayThermalTransmittanceCalculationResult>();
            foreach(ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult in thermalTransmittanceCalculationResults)
            {
                DisplayThermalTransmittanceCalculationResult displayThermalTransmittanceCalculationResult = new DisplayThermalTransmittanceCalculationResult(thermalTransmittanceCalculationResult) { ConstructionManager = constructionManager };
                if(thermalTransmittanceFilter != null)
                {
                    if(!thermalTransmittanceFilter.IsValid(displayThermalTransmittanceCalculationResult))
                    {
                        continue;
                    }
                }

                displayThermalTransmittanceCalculationResults.Add(displayThermalTransmittanceCalculationResult);
            }

            DataGrid_Main.ItemsSource = displayThermalTransmittanceCalculationResults;
        }

        private void TextBox_PreviewTextInput_NumberOnly(object sender, TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_NumberOnly(sender, e);
        }

        public ThermalTransmittanceFilter ThermalTransmittanceFilter
        {
            get
            {
                return GetThermalTransmittanceFilter();
            }

            set
            {
                SetThermalTransmittanceFilter(value);
                SetThermalTransmittanceCalculationResults(thermalTransmittanceCalculationResults);
            }
        }

        private ThermalTransmittanceFilter GetThermalTransmittanceFilter()
        {
            ThermalTransmittanceFilter result = new ThermalTransmittanceFilter();

            double value_1 = double.NaN;
            double value_2 = double.NaN;

            if (!Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            result.TotalSolarEnergyTransmittance = value_1;

            if (!Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance_Min.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            if (!Core.Query.TryConvert(TextBox_TotalSolarEnergyTransmittance_Max.Text, out value_2))
            {
                value_2 = double.NaN;
            }

            result.TotalSolarEnergyTransmittanceRange = double.IsNaN(value_1) || double.IsNaN(value_2) ? null : new Range<double>(-value_1, value_2);


            if (!Core.Query.TryConvert(TextBox_LightTransmittance.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            result.LightTransmittance = value_1;

            if (!Core.Query.TryConvert(TextBox_LightTransmittance_Min.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            if (!Core.Query.TryConvert(TextBox_LightTransmittance_Max.Text, out value_2))
            {
                value_2 = double.NaN;
            }

            result.LightTransmittanceRange = double.IsNaN(value_1) || double.IsNaN(value_2) ? null : new Range<double>(-value_1, value_2);


            if (!Core.Query.TryConvert(TextBox_ThermalTransmittance.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            result.ThermalTransmittance = value_1;

            if (!Core.Query.TryConvert(TextBox_ThermalTransmittance_Min.Text, out value_1))
            {
                value_1 = double.NaN;
            }

            if (!Core.Query.TryConvert(TextBox_ThermalTransmittance_Max.Text, out value_2))
            {
                value_2 = double.NaN;
            }

            result.ThermalTransmittanceRange = double.IsNaN(value_1) || double.IsNaN(value_2) ? null : new Range<double>(-value_1, value_2);


            return result;
        }

        private void SetThermalTransmittanceFilter(ThermalTransmittanceFilter thermalTransmittanceFilter)
        {
            Range<double> range = null;

            double? value;

            value = thermalTransmittanceFilter?.TotalSolarEnergyTransmittance;
            TextBox_TotalSolarEnergyTransmittance.Text = value == null || double.IsNaN(value.Value) ? null : value?.ToString();
            TextBox_TotalSolarEnergyTransmittance_Min.Text = null;
            TextBox_TotalSolarEnergyTransmittance_Max.Text = null;
            range = thermalTransmittanceFilter?.TotalSolarEnergyTransmittanceRange;
            if(range != null)
            {
                TextBox_TotalSolarEnergyTransmittance_Min.Text = System.Math.Abs(range.Min).ToString();
                TextBox_TotalSolarEnergyTransmittance_Max.Text = System.Math.Abs(range.Max).ToString();
            }

            value = thermalTransmittanceFilter?.LightTransmittance;
            TextBox_LightTransmittance.Text = value == null || double.IsNaN(value.Value) ? null : value?.ToString();
            TextBox_LightTransmittance_Min.Text = null;
            TextBox_LightTransmittance_Max.Text = null;
            range = thermalTransmittanceFilter?.LightTransmittanceRange;
            if (range != null)
            {
                TextBox_LightTransmittance_Min.Text = System.Math.Abs(range.Min).ToString();
                TextBox_LightTransmittance_Max.Text = System.Math.Abs(range.Max).ToString();
            }

            value = thermalTransmittanceFilter?.ThermalTransmittance;
            TextBox_ThermalTransmittance.Text = value == null || double.IsNaN(value.Value) ? null : value?.ToString();
            TextBox_ThermalTransmittance_Min.Text = null;
            TextBox_ThermalTransmittance_Max.Text = null;
            range = thermalTransmittanceFilter?.ThermalTransmittanceRange;
            if (range != null)
            {
                TextBox_ThermalTransmittance_Min.Text = System.Math.Abs(range.Min).ToString();
                TextBox_ThermalTransmittance_Max.Text = System.Math.Abs(range.Max).ToString();
            }
        }
    }
}
