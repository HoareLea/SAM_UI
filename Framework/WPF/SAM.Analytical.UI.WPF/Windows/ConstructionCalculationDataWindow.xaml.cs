using SAM.Analytical.Tas;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConstructionCalculationDataWindow.xaml
    /// </summary>
    public partial class ConstructionCalculationDataWindow : System.Windows.Window
    {
        public ConstructionCalculationDataWindow()
        {
            InitializeComponent();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        public IConstructionCalculationData ConstructionCalculationData
        {
            get
            {
                return ConstructionCalculationDataControl_Main.ConstructionCalculationData;
            }

            set
            {
                ConstructionCalculationDataControl_Main.ConstructionCalculationData = value;
            }
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return ConstructionCalculationDataControl_Main.ConstructionManager;
            }

            set
            {
                ConstructionCalculationDataControl_Main.ConstructionManager = value;
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            IConstructionCalculationData constructionCalculationData = ConstructionCalculationData;
            if (constructionCalculationData == null)
            {
                MessageBox.Show("Could not collect data.");
                return;
            }

            if(constructionCalculationData is LayerThicknessCalculationData)
            {
                LayerThicknessCalculationData layerThicknessCalculationData = (LayerThicknessCalculationData)constructionCalculationData;

                if (string.IsNullOrWhiteSpace(layerThicknessCalculationData.ConstructionName))
                {
                    MessageBox.Show("Provide construction name.");
                    return;
                }

                if (double.IsNaN(layerThicknessCalculationData.ThermalTransmittance))
                {
                    MessageBox.Show("Provide thermal transmittance.");
                    return;
                }
            }
            else if(constructionCalculationData is ConstructionCalculationData)
            {
                ConstructionCalculationData constructionCalculationData_Temp = (ConstructionCalculationData)constructionCalculationData;
                
                if (string.IsNullOrWhiteSpace(constructionCalculationData_Temp.ConstructionName))
                {
                    MessageBox.Show("Provide construction name.");
                    return;
                }

                if (double.IsNaN(constructionCalculationData_Temp.ThermalTransmittance))
                {
                    MessageBox.Show("Provide thermal transmittance.");
                    return;
                }
            }



            DialogResult = true;

            Close();
        }
    }
}
