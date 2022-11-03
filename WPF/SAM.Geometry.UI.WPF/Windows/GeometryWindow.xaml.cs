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
        private Core.Windows.WindowHandle windowHandle;

        public event ObjectHooveredEventHandler ObjectHoovered;

        public GeometryWindow()
        {
            InitializeComponent();

            windowHandle = new Core.Windows.WindowHandle(this);
        }

        public GeometryWindow(GeometryObjectModel geometryObjectModel)
        {
            InitializeComponent();

            windowHandle = new Core.Windows.WindowHandle(this);

            if(geometryObjectModel != null)
            {
                UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                ViewControl.UIGeometryObjectModel = uIGeometryObjectModel;
                ViewControl.ObjectHoovered += ViewControl_ObjectHoovered;
            }
        }

        private void ViewControl_ObjectHoovered(object sender, Core.UI.WPF.ObjectHooveredEventArgs e)
        {
            ObjectHoovered?.Invoke(this, e);
        }

        public GeometryWindow(IEnumerable<ISAMGeometryObject> geometryObjects)
        {
            InitializeComponent();

            windowHandle = new Core.Windows.WindowHandle(this);

            if (geometryObjects != null)
            {
                GeometryObjectModel geometryObjectModel = new GeometryObjectModel();

                foreach(ISAMGeometryObject sAMGeometryObject in geometryObjects)
                {
                    geometryObjectModel.Add(sAMGeometryObject);
                }

                UIGeometryObjectModel uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
                uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                ViewControl.UIGeometryObjectModel = uIGeometryObjectModel;
            }
        }

        public Mode Mode
        {
            get
            {
                return ViewControl.Mode;
            }

            set
            {
                ViewControl.Mode = value;
            }
        }

        //public ViewControl ViewControl_Temp
        //{
        //    get
        //    {
        //        return ViewControl;
        //    }
        //}

        public void CenterView()
        {
            ViewControl.CenterView();
        }

        private void UIGeometryObjectModel_Modified(object sender, EventArgs e)
        {

        }
    }
}
