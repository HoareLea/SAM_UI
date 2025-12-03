using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CreateCaseByWindowSize.xaml
    /// </summary>
    public partial class CreateCaseByWindowSizeControl : UserControl
    {
        public CreateCaseByWindowSizeControl()
        {
            InitializeComponent();

            DataContext = new CreateCaseViewModel<WindowSizeCase>();
        }

        public IEnumerable<WindowSizeCase>? WindowSizeCases
        {
            get
            {
                if (DataContext is not CreateCaseViewModel<WindowSizeCase> createCaseViewModel)
                {
                    return null;
                }

                List<WindowSizeCase> result = [];
                foreach(WindowSizeCase windowSizeCase in createCaseViewModel.Items)
                {
                    result.Add(windowSizeCase);
                }

                return result;
            }

            set
            {
                if (DataContext is not CreateCaseViewModel<WindowSizeCase> createCaseViewModel)
                {
                    return;
                }

                createCaseViewModel.Items.Clear();

                if (value == null)
                {
                    return;
                }

                foreach(WindowSizeCase windowSizeCase in value)
                {
                    createCaseViewModel.Items.Add(windowSizeCase);
                }
            }
        }

        private void button_Selection_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
