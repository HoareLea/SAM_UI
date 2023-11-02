using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConstructionCalculationResultWindow.xaml
    /// </summary>
    public partial class ConstructionCalculationResultWindow : System.Windows.Window
    {
        private ConstructionManager constructionManager;
        private IConstructionCalculationResult constructionCalculationResult;

        public ConstructionCalculationResultWindow()
        {
            InitializeComponent();
        }

        private void Button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void Button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
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
                SetConstructionCalculationResult(constructionCalculationResult);
            }
        }

        public IConstructionCalculationResult ConstructionCalculationResult
        {
            get
            {
                return constructionCalculationResult;
            }

            set
            {
                SetConstructionCalculationResult(value);
            }
        }

        private void SetConstructionCalculationResult(IConstructionCalculationResult constructionCalculationResult)
        {
            DockPanel_Main.Children.Clear();

            if (constructionCalculationResult == null)
            {
                return;
            }

            if(constructionCalculationResult is LayerThicknessCalculationResult)
            {
                DockPanel_Main.Children.Add(new LayerThicknessCalculationResultControl() { ConstructionManager = constructionManager, LayerThicknessCalculationResult = (LayerThicknessCalculationResult)constructionCalculationResult });
            }
            else if(constructionCalculationResult is ConstructionCalculationResult)
            {
                DockPanel_Main.Children.Add(new ConstructionCalculationResultControl() { ConstructionManager = constructionManager, ConstructionCalculationResult = (ConstructionCalculationResult)constructionCalculationResult });
            }
        }
    }
}
