using System.Collections.Generic;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for BatchCreateViewsWindow.xaml
    /// </summary>
    public partial class BatchCreateViewsWindow : System.Windows.Window
    {
        public BatchCreateViewsWindow()
        {
            InitializeComponent();


        }

        public BatchCreateViewsWindow(AdjacencyCluster adjacencyCluster)
        {
            InitializeComponent();

            batchCreateViewsControl.AdjacencyCluster = adjacencyCluster;
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return batchCreateViewsControl.AdjacencyCluster;
            }

            set
            {
                batchCreateViewsControl.AdjacencyCluster = value;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public List<AnalyticalTwoDimensionalViewSettings> AnalyticalTwoDimensionalViewSettingsList
        {
            get
            {
                return batchCreateViewsControl.AnalyticalTwoDimensionalViewSettingsList;
            }
        }
    }
}
