using System.Windows;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF.Windows
{
    /// <summary>
    /// Interaction logic for ViewSettingsWindow.xaml
    /// </summary>
    public partial class ViewSettingsWindow : System.Windows.Window
    {
        public ViewSettingsWindow()
        {
            InitializeComponent();
        }

        public ViewSettingsWindow(Geometry.UI.IViewSettings viewSettings, AdjacencyCluster adjacencyCluster)
        {
            InitializeComponent();

            UserControl userControl = null;

            if(viewSettings is AnalyticalTwoDimensionalViewSettings)
            {
                AnalyticalTwoDimensionalViewSettingsControl analyticalTwoDimensionalViewSettingsControl = new AnalyticalTwoDimensionalViewSettingsControl((AnalyticalTwoDimensionalViewSettings)viewSettings, adjacencyCluster);
                userControl = analyticalTwoDimensionalViewSettingsControl;
            }
            else if (viewSettings is Geometry.UI.TwoDimensionalViewSettings)
            {
                AnalyticalTwoDimensionalViewSettingsControl analyticalTwoDimensionalViewSettingsControl = new AnalyticalTwoDimensionalViewSettingsControl(new AnalyticalTwoDimensionalViewSettings((Geometry.UI.TwoDimensionalViewSettings)viewSettings), adjacencyCluster);
                userControl = analyticalTwoDimensionalViewSettingsControl;
            }
            else if (viewSettings is AnalyticalThreeDimensionalViewSettings)
            {
                AnalyticalThreeDimensionalViewSettingsControl analyticalThreeDimensionalViewSettingsControl = new AnalyticalThreeDimensionalViewSettingsControl((AnalyticalThreeDimensionalViewSettings)viewSettings, adjacencyCluster);
                userControl = analyticalThreeDimensionalViewSettingsControl;
            }
            else if (viewSettings is Geometry.UI.ThreeDimensionalViewSettings)
            {
                AnalyticalThreeDimensionalViewSettingsControl analyticalThreeDimensionalViewSettingsControl = new AnalyticalThreeDimensionalViewSettingsControl(new AnalyticalThreeDimensionalViewSettings((Geometry.UI.ThreeDimensionalViewSettings)viewSettings), adjacencyCluster);
                userControl = analyticalThreeDimensionalViewSettingsControl;
            }


            if (userControl != null)
            {
                viewSettingControl.Children.Add(userControl);

                userControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                userControl.VerticalAlignment = VerticalAlignment.Stretch;
                
                viewSettingControl.UpdateLayout();
            }
        }

        public Geometry.UI.ViewSettings ViewSettings
        {
            get
            {
                foreach(UIElement uIElement in viewSettingControl.Children)
                {
                    if(uIElement is AnalyticalTwoDimensionalViewSettingsControl)
                    {
                        return ((AnalyticalTwoDimensionalViewSettingsControl)uIElement).AnalyticalTwoDimensionalViewSettings;
                    }

                    if (uIElement is AnalyticalThreeDimensionalViewSettingsControl)
                    {
                        return ((AnalyticalThreeDimensionalViewSettingsControl)uIElement).AnalyticalThreeDimensionalViewSettings;
                    }
                }

                return null;
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(UserControl userControl in viewSettingControl.Children)
            {
                userControl.Width = viewSettingControl.ActualWidth - 10;
                userControl.Height = viewSettingControl.ActualHeight - 10;
            }
        }
    }
}
