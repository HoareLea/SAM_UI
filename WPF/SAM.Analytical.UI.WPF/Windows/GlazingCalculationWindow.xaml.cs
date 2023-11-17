using SAM.Analytical.Tas;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for GlazingCalculationWindow.xaml
    /// </summary>
    public partial class GlazingCalculationWindow : System.Windows.Window
    {
        public GlazingCalculationWindow()
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
                return GlazingCalculationControl_Main.ConstructionManager;
            }

            set
            {
                GlazingCalculationControl_Main.ConstructionManager = value;
            }
        }

        public GlazingCalculationData GlazingCalculationData
        {
            get
            {
                return GlazingCalculationControl_Main.GlazingCalculationData;
            }

            set
            {
                GlazingCalculationControl_Main.GlazingCalculationData = value;
            }
        }
    }
}
