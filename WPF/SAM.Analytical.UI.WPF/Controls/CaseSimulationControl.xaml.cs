using Microsoft.Win32;
using SAM.Analytical.Tas;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for CaseSimulationControl.xaml
    /// </summary>
    public partial class CaseSimulationControl : System.Windows.Controls.UserControl
    {
        private string? directory = null;

        private WorkflowSettings workflowSettings = Query.DefaultWorkflowSettings();

        public CaseSimulationControl()
        {
            InitializeComponent();
        }

        public string? Directory
        {
            get
            {
                return directory;
            }
            set
            {
                directory = value;
                TextBox_Directory.Text = directory;

                //TextBox_Directory.Focus();
                //TextBox_Directory.CaretIndex = TextBox_Directory.Text.Length;
                //TextBox_Directory.ScrollToEnd();

            }
        }

        public bool Parallel
        {
            get
            {
                return checkBox_Parallel.IsChecked.HasValue && checkBox_Parallel.IsChecked.Value;
            }
            set
            {
                checkBox_Parallel.IsChecked = value;
            }
        }

        public WorkflowSettings WorkflowSettings
        {
            get
            {
                return workflowSettings;
            }

            set
            {
                if(value == null)
                {
                    return;
                }

                workflowSettings = value;
            }
        }

        private void button_Browse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new();
            bool? dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == null || !dialogResult.HasValue || !dialogResult.Value)
            {
                return;
            }

            directory = openFolderDialog.FolderName;
            TextBox_Directory.Text = directory;

            //TextBox_Directory.Focus();
            //TextBox_Directory.CaretIndex = TextBox_Directory.Text.Length;
            //TextBox_Directory.ScrollToEnd();

        }
    }
}
