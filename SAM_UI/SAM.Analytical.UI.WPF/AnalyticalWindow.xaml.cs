using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalWindow.xaml
    /// </summary>
    public partial class AnalyticalWindow : Window
    {
        public AnalyticalWindow()
        {
            InitializeComponent();

            RibbonButton_General_NewAnalyticalModel.LargeImageSource =  Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_New, 32, 32);
            RibbonButton_General_OpenAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open, 32, 32);
            RibbonButton_General_SaveAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Save, 32, 32);
            RibbonButton_General_SaveAsAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SaveAs, 32, 32);
            RibbonButton_General_CloseAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close, 32, 32);

            RibbonButton_Edit_Location.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Location, 32, 32);

            foreach(object @object in View3DControl.Viewport.Children)
            {
                string name = @object.GetType().Name;
            }
        }
    }
}
