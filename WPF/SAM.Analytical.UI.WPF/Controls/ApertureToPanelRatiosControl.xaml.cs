using SAM.Analytical.Classes;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureToPanelRatiosControl.xaml
    /// </summary>
    public partial class ApertureToPanelRatiosControl : UserControl
    {
        public ApertureToPanelRatiosControl()
        {
            InitializeComponent();

            DataContext = new JSAMObjectViewModel<ApertureToPanelRatio>();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        public ApertureToPanelRatios ApertureToPanelRatios
        {
            get
            {
                if (DataContext is not JSAMObjectViewModel<ApertureToPanelRatio> jSAMObjectViewModel)
                {
                    return null;
                }

                return new ApertureToPanelRatios(jSAMObjectViewModel.Items);
            }

            set
            {
                if (DataContext is not JSAMObjectViewModel<ApertureToPanelRatio> jSAMObjectViewModel)
                {
                    return;
                }

                jSAMObjectViewModel.Items.Clear();

                if (value == null || value.Count == 0)
                {
                    return;
                }

                for(int i=0; i < value.Count; i++)
                {
                    jSAMObjectViewModel.Items.Add(value[i]);
                }
            }
        }
    }
}
