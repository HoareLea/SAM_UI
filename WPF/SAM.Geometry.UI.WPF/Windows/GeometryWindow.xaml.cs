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

        private UIGeometryObjectModel uIGeometryObjectModel;

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
                uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            }
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
                
                uIGeometryObjectModel = new UIGeometryObjectModel(geometryObjectModel);
            }
        }
    }
}
