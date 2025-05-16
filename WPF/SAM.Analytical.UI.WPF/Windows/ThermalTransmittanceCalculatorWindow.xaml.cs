using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ThermalTransmittanceCalculatorWindow.xaml
    /// </summary>
    public partial class ThermalTransmittanceCalculatorWindow : System.Windows.Window
    {
        public ThermalTransmittanceCalculatorWindow()
        {
            InitializeComponent();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return ThermalTransmittanceCalculatorControl_Main.ConstructionManager;
            }

            set
            {
                ThermalTransmittanceCalculatorControl_Main.ConstructionManager = value;
            }
        }

        public List<LayerThicknessCalculationData> LayerThicknessCalculationDatas
        {
            get
            {
                return ThermalTransmittanceCalculatorControl_Main.LayerThicknessCalculationDatas;
            }
        }
    }
}
