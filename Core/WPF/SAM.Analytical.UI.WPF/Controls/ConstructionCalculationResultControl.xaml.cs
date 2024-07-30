using SAM.Analytical.Tas;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ConstructionCalculationResultControl.xaml
    /// </summary>
    public partial class ConstructionCalculationResultControl : UserControl
    {
        private ConstructionManager constructionManager;
        private ConstructionCalculationResult constructionCalculationResult;

        public ConstructionCalculationResultControl()
        {
            InitializeComponent();
        }

        public ConstructionCalculationResult ConstructionCalculationResult
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

        private void SetConstructionCalculationResult(ConstructionCalculationResult constructionCalculationResult)
        {
            this.constructionCalculationResult = constructionCalculationResult;
            if(constructionCalculationResult != null)
            {

                TextBox_ThermalTransmittance.Text = constructionCalculationResult.ThermalTransmittance.ToString();

                Construction initialConstruction = constructionManager.GetConstructions(constructionCalculationResult.InitialConstructionName)?.FirstOrDefault();

                TextBox_InitialConstructionName.Text = constructionCalculationResult.InitialConstructionName;
                TextBox_InitialConstructionThickness.Text = initialConstruction == null ? null : initialConstruction.GetThickness().ToString();
                TextBox_InitialThermalTransmittance.Text = constructionCalculationResult.InitialThermalTransmittance.ToString();

                Construction calculatedConstruction = constructionManager.GetConstructions(constructionCalculationResult.ConstructionName)?.FirstOrDefault();

                TextBox_CalculatedConstructionName.Text = constructionCalculationResult.ConstructionName;
                TextBox_CalculatedConstructionThickness.Text = calculatedConstruction == null ? null : calculatedConstruction.GetThickness().ToString();
                TextBox_CalculatedThermalTransmittance.Text = constructionCalculationResult.CalculatedThermalTransmittance.ToString();
            }
        }
    }
}
