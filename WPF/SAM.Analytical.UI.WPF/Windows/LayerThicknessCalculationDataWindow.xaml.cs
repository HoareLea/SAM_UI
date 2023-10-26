using SAM.Analytical.Tas;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for LayerThicknessCalculationDataWindow.xaml
    /// </summary>
    public partial class LayerThicknessCalculationDataWindow : System.Windows.Window
    {
        public LayerThicknessCalculationDataWindow()
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
                return LayerThicknessCalculationDataControl.ConstructionManager;
            }

            set
            {
                LayerThicknessCalculationDataControl.ConstructionManager = value;
            }
        }

        public LayerThicknessCalculationData LayerThicknessCalculationData
        {
            get
            {
                return LayerThicknessCalculationDataControl.LayerThicknessCalculationData;
            }

            set
            {
                LayerThicknessCalculationDataControl.LayerThicknessCalculationData = value;
            }
        }
    }
}
