using Microsoft.Win32;
using SAM.Core;
using System;
using System.Collections.Generic;
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
            ribbonButton_View_Json.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);

            viewportControl.Mode = Mode.ThreeDimensional;

            jsonControl.TextChanged += JsonControl_TextChanged;

            switch(viewportControl.Mode)
            {
                case Mode.ThreeDimensional:
                    ribbonButton_View_Mode.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
                    ribbonButton_View_Mode.Label = "2D";
                    break;

                case Mode.TwoDimensional:
                    ribbonButton_View_Mode.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
                    ribbonButton_View_Mode.Label = "3D";
                    break;
            }
        }

        private void JsonControl_TextChanged(object sender, Core.UI.WPF.TextChangedEventArgs e)
        {
            string json = e.Text;
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            List<IJSAMObject> jSAMObjects = Core.Convert.ToSAM(json);

            UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel();
            uIGeometryObjectModel.Open(jSAMObjects);

            viewportControl.UIGeometryObjectModel = uIGeometryObjectModel;
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

        private void RibbonButton_View_Mode_Click(object sender, RoutedEventArgs e)
        {
            switch (viewportControl.Mode)
            {
                case Mode.ThreeDimensional:
                    viewportControl.Mode = Mode.TwoDimensional;
                    ribbonButton_View_Mode.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
                    ribbonButton_View_Mode.Label = "3D";
                    break;

                case Mode.TwoDimensional:
                    viewportControl.Mode = Mode.ThreeDimensional;
                    ribbonButton_View_Mode.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Open);
                    ribbonButton_View_Mode.Label = "2D";
                    break;
            }
        }

        private void RibbonButton_View_Json_Click(object sender, RoutedEventArgs e)
        {
            if(grid.ColumnDefinitions[2].Width.Value > 0)
            {
                grid.ColumnDefinitions[2].Width = new GridLength(0);
            }
            else
            {
                grid.ColumnDefinitions[2].Width = new GridLength(400);
            }
        }
    }
}
