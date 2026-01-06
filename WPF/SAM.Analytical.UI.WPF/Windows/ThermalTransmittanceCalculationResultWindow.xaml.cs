using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ThermalTransmittanceCalculationResultWindow.xaml
    /// </summary>
    public partial class ThermalTransmittanceCalculationResultWindow : System.Windows.Window
    {
        public ThermalTransmittanceCalculationResultWindow()
        {
            InitializeComponent();
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

        public ConstructionManager ConstructionManager
        {
            get
            {
                return ThermalTransmittanceCalculationResultControl_Main.ConstructionManager;
            }

            set
            {
                ThermalTransmittanceCalculationResultControl_Main.ConstructionManager = value;
            }
        }

        public List<ThermalTransmittanceCalculationResult> ThermalTransmittanceCalculationResults
        {
            get
            {
                return ThermalTransmittanceCalculationResultControl_Main.ThermalTransmittanceCalculationResults;
            }

            set
            {
                ThermalTransmittanceCalculationResultControl_Main.ThermalTransmittanceCalculationResults = value;
            }
        }

        public ThermalTransmittanceCalculationResult ThermalTransmittanceCalculationResult
        {
            get
            {
                return ThermalTransmittanceCalculationResultControl_Main.ThermalTransmittanceCalculationResult;
            }
        }
    }
}
