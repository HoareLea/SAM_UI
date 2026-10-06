using SAM.Geometry.UI;
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
        /// <param name="isNewViewSettings">
        /// True where this dialog is creating a view rather than editing one that exists. It is what allows a
        /// new view to be given a useful preset - the Part F airflow annotation, when the Part F colour scheme
        /// is chosen - without ever changing how somebody has already set an existing view up.
        /// </param>
        public ViewSettingsWindow(IViewSettings viewSettings, AnalyticalModel analyticalModel, bool isNewViewSettings = false)
        {
            InitializeComponent();

            UserControl userControl = null;

            if(viewSettings is TwoDimensionalViewSettings)
            {
                AnalyticalTwoDimensionalViewSettingsControl analyticalTwoDimensionalViewSettingsControl = new AnalyticalTwoDimensionalViewSettingsControl((TwoDimensionalViewSettings)viewSettings, analyticalModel) { IsNewViewSettings = isNewViewSettings };
                userControl = analyticalTwoDimensionalViewSettingsControl;
            }
            else if (viewSettings is ThreeDimensionalViewSettings)
            {
                AnalyticalThreeDimensionalViewSettingsControl analyticalThreeDimensionalViewSettingsControl = new AnalyticalThreeDimensionalViewSettingsControl((ThreeDimensionalViewSettings)viewSettings, analyticalModel);
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

        public ViewSettings ViewSettings
        {
            get
            {
                foreach(UIElement uIElement in viewSettingControl.Children)
                {
                    if(uIElement is AnalyticalTwoDimensionalViewSettingsControl)
                    {
                        return ((AnalyticalTwoDimensionalViewSettingsControl)uIElement).TwoDimensionalViewSettings;
                    }

                    if (uIElement is AnalyticalThreeDimensionalViewSettingsControl)
                    {
                        return ((AnalyticalThreeDimensionalViewSettingsControl)uIElement).ThreeDimensionalViewSettings;
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
