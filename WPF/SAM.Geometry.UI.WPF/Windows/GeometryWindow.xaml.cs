using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// Interaction logic for GeometryWindow.xaml
    /// </summary>
    public partial class GeometryWindow : Window
    {
        public GeometryWindow()
        {
            InitializeComponent();

            ribbonButton_General_OpenModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
            ribbonButton_General_CloseModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);

            viewportControl.Mode = Mode.TwoDimensional;
        }

        private void RibbonButton_General_CloseModel_Click(object sender, RoutedEventArgs e)
        {
            viewportControl.UIGeometryObjectModel = new UIGeometryObjectModel();
        }

        private void RibbonButton_General_OpenModel_Click(object sender, RoutedEventArgs e)
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

            UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel();
            uIGeometryObjectModel.Path = path;

            Core.Windows.Forms.MarqueeProgressForm.Show("Opening File", () => uIGeometryObjectModel.Open());

            viewportControl.UIGeometryObjectModel = uIGeometryObjectModel;
        }
    }
}
