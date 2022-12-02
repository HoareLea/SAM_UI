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

        public ViewSettingsWindow(Geometry.UI.IViewSettings viewSettings)
        {
            InitializeComponent();

            UIElement uIElement = null;

            if(viewSettings is AnalyticalTwoDimensionalViewSettings)
            {
                AnalyticalTwoDimensionalViewSettingsControl analyticalTwoDimensionalViewSettingsControl = new AnalyticalTwoDimensionalViewSettingsControl((AnalyticalTwoDimensionalViewSettings)viewSettings);
                uIElement = analyticalTwoDimensionalViewSettingsControl;
            }
            else if (viewSettings is Geometry.UI.TwoDimensionalViewSettings)
            {
                AnalyticalTwoDimensionalViewSettingsControl analyticalTwoDimensionalViewSettingsControl = new AnalyticalTwoDimensionalViewSettingsControl(new AnalyticalTwoDimensionalViewSettings((Geometry.UI.TwoDimensionalViewSettings)viewSettings));
                uIElement = analyticalTwoDimensionalViewSettingsControl;
            }
            else if (viewSettings is AnalyticalThreeDimensionalViewSettings)
            {
                AnalyticalThreeDimensionalViewSettingsControl analyticalThreeDimensionalViewSettingsControl = new AnalyticalThreeDimensionalViewSettingsControl((AnalyticalThreeDimensionalViewSettings)viewSettings);
                uIElement = analyticalThreeDimensionalViewSettingsControl;
            }
            else if (viewSettings is Geometry.UI.ThreeDimensionalViewSettings)
            {
                AnalyticalThreeDimensionalViewSettingsControl analyticalThreeDimensionalViewSettingsControl = new AnalyticalThreeDimensionalViewSettingsControl(new AnalyticalThreeDimensionalViewSettings((Geometry.UI.ThreeDimensionalViewSettings)viewSettings));
                uIElement = analyticalThreeDimensionalViewSettingsControl;
            }


            if (uIElement != null)
            {
                viewSettingControl.Children.Add(uIElement);
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
    }
}
