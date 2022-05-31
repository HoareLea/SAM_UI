using Microsoft.Win32;
using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for AnalyticalWindow.xaml
    /// </summary>
    public partial class AnalyticalWindow : System.Windows.Window
    {
        private UIAnalyticalModel uIAnalyticalModel;

        public AnalyticalWindow()
        {
            InitializeComponent();

            RibbonButton_General_NewAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_New, 32, 32);
            RibbonButton_General_OpenAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open, 32, 32);
            RibbonButton_General_SaveAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Save, 32, 32);
            RibbonButton_General_SaveAsAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_SaveAs, 32, 32);
            RibbonButton_General_CloseAnalyticalModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close, 32, 32);

            RibbonButton_Edit_Location.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Location, 32, 32);

            RibbonButton_General_OpenAnalyticalModel.Click += RibbonButton_General_OpenAnalyticalModel_Click;

            uIAnalyticalModel = new UIAnalyticalModel();
        }

        private void RibbonButton_General_OpenAnalyticalModel_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog(this) == false)
            {
                return;
            }
            path = openFileDialog.FileName;


            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            uIAnalyticalModel.Path = path;

            uIAnalyticalModel.Open();

            View3DControl.UIAnalyticalModel = uIAnalyticalModel;
        }
    }
}
