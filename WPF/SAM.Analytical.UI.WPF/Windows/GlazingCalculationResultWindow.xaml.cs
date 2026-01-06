using SAM.Analytical.Tas;
using System.Collections.Generic;
using System.Windows;


namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for GlazingCalculationResultWindow.xaml
    /// </summary>
    public partial class GlazingCalculationResultWindow : System.Windows.Window
    {
        public GlazingCalculationResultWindow()
        {
            InitializeComponent();
        }

        public ConstructionManager ConstructionManager
        {
            get
            {
                return GlazingCalculationResultControl_Main.ConstructionManager;
            }

            set
            {
                GlazingCalculationResultControl_Main.ConstructionManager = value;
            }
        }

        public GlazingCalculationData GlazingCalculationData
        {
            get
            {
                return GlazingCalculationResultControl_Main.GlazingCalculationData;
            }

            set
            {
                GlazingCalculationResultControl_Main.GlazingCalculationData = value;
            }
        }

        public List<GlazingCalculationResult> GlazingCalculationResults
        {
            get
            {
                return GlazingCalculationResultControl_Main.GlazingCalculationResults;
            }

            set
            {
                GlazingCalculationResultControl_Main.GlazingCalculationResults = value;
            }
        }

        public GlazingCalculationResult GlazingCalculationResult
        {
            get
            {
                return GlazingCalculationResultControl_Main.GlazingCalculationResult;
            }
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

        public bool Replace
        {
            get
            {
                return RadioButton_Replace.IsChecked != null && RadioButton_Replace.IsChecked.HasValue && RadioButton_Replace.IsChecked.Value;
            }
        }
    }
}
