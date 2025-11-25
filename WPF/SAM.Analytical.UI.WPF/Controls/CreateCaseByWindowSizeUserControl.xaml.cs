using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSizeUserControl.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeUserControl : UserControl
    {
        public CreateCaseByWindowSizeUserControl()
        {
            InitializeComponent();
        }

        public double ApertureScaleFactor
        {
            get
            {
                if (!Core.Query.TryConvert(TextBox_ApertureScaleFactor.Text, out double result))
                {
                    return double.NaN;
                }

                return result;
            }

            set
            {
                if(double.IsNaN(value))
                {
                    TextBox_ApertureScaleFactor.Text = null;
                    return;
                }

                TextBox_ApertureScaleFactor.Text = value.ToString();
            }
        }
    }
}
