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
        }

        public GeometryWindow(GeometryObjectModel geometryObjectModel)
        {
            InitializeComponent();

            if(geometryObjectModel != null)
            {
                UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                viewControl.UIGeometryObjectModel = uIGeometryObjectModel;
            }
        }

        public GeometryWindow(IEnumerable<ISAMGeometryObject> geometryObjects)
        {
            InitializeComponent();

            if (geometryObjects != null)
            {
                GeometryObjectModel geometryObjectModel = new GeometryObjectModel();

                foreach(ISAMGeometryObject sAMGeometryObject in geometryObjects)
                {
                    geometryObjectModel.Add(sAMGeometryObject);
                }

                UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                viewControl.UIGeometryObjectModel = uIGeometryObjectModel;
            }
        }

        public ViewControl ViewControl
        {
            get
            {
                return viewControl;
            }
        }

        private void UIGeometryObjectModel_Modified(object sender, EventArgs e)
        {

        }
    }
}
