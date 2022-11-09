using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;

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
            ribbonButton_General_OpenModel.Click += RibbonButton_General_OpenModel_Click;

            ribbonButton_General_CloseModel.LargeImageSource = Core.Windows.Convert.ToBitmapSource(Properties.Resources.SAM_Close);
            ribbonButton_General_CloseModel.Click += RibbonButton_General_CloseModel_Click;
            
            UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel();
            uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;
            viewControl.UIGeometryObjectModel = uIGeometryObjectModel;
            viewControl.Mode = Mode.ThreeDimensional;
            viewControl.Loaded += ViewControl_Loaded;
        }

        private void UIGeometryObjectModel_Modified(object sender, EventArgs e)
        {

        }

        private void ViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewControl viewControl = sender as ViewControl;
            if (viewControl == null)
            {
                return;
            }

            viewControl.CenterView();
        }

        private void RibbonButton_General_CloseModel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RibbonButton_General_OpenModel_Click(object sender, RoutedEventArgs e)
        {
            UIGeometryObjectModel uIGeometryObjectModel = viewControl.UIGeometryObjectModel;
            if (uIGeometryObjectModel == null)
            {
                return;
            }

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

            UIGeometryObjectModel uIGeometryObjectModel_Temp = new UIGeometryObjectModel();
            uIGeometryObjectModel_Temp.Path = path;

            Core.Windows.Forms.MarqueeProgressForm.Show("Opening File", () => uIGeometryObjectModel_Temp.Open());

            uIGeometryObjectModel_Temp.Path = path;
            uIGeometryObjectModel.JSAMObject = uIGeometryObjectModel_Temp?.JSAMObject;

            viewControl.CenterView();
        }

        public ViewControl ViewControl
        {
            get
            {
                return viewControl;
            }
        }
    }
}
