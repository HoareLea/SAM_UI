using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewControl : UserControl
    {
        private Mode mode = Mode.ThreeDimensional;
        
        private UIGeometryObjectModel uIGeometryObjectModel;

        private VisualBackground visualBackground;

        public ViewControl()
        {
            InitializeComponent();

            Viewport.Camera = null;
        }

        public UIGeometryObjectModel UIGeometryObjectModel
        {
            get
            {
                return uIGeometryObjectModel;
            }

            set
            {
                uIGeometryObjectModel = value;
                if (uIGeometryObjectModel != null)
                {
                    uIGeometryObjectModel.Modified -= UIGeometryObjectModel_Modified;
                    uIGeometryObjectModel.Modified += UIGeometryObjectModel_Modified;

                    uIGeometryObjectModel.Closed -= UIGeometryObjectModel_Closed;
                    uIGeometryObjectModel.Closed += UIGeometryObjectModel_Closed;

                    uIGeometryObjectModel.Opened -= UIGeometryObjectModel_Opened;
                    uIGeometryObjectModel.Opened += UIGeometryObjectModel_Opened;
                }

                Load(uIGeometryObjectModel?.JSAMObject);
            }
        }

        private void UIGeometryObjectModel_Opened(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void UIGeometryObjectModel_Closed(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void UIGeometryObjectModel_Modified(object sender, EventArgs e)
        {
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void Load(GeometryObjectModel geometryObjectModel)
        {
            if(geometryObjectModel == null)
            {
                Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(Viewport);
                return;
            }

            Transform3D tranform3D = Transform3D.Identity;

            VisualGeometryObjectModel visualGeometryObjectModel = Core.UI.WPF.Query.VisualJSAMObjects<VisualGeometryObjectModel>(Viewport)?.FirstOrDefault();
            if (visualGeometryObjectModel != null)
            {
                tranform3D = visualGeometryObjectModel.Transform;
            }

            Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(Viewport);

            visualGeometryObjectModel = Convert.ToMedia3D(geometryObjectModel);
            visualGeometryObjectModel.Transform = tranform3D;

            if (visualGeometryObjectModel != null)
            {
                Viewport.Children.Add(visualGeometryObjectModel);
            }

            UpdateCamera();
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu_Grid.Items.Clear();

            Point point_Current = Mouse.GetPosition(Viewport);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current);
            IVisualGeometryObject visualGeometryObjectt = hitTestResult?.VisualHit as IVisualGeometryObject;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CenterView";
            menuItem.Header = "Center View";
            menuItem.Click += MenuItem_CenterView_Click;
            menuItem.Tag = hitTestResult;
            ContextMenu_Grid.Items.Add(menuItem);


            //if (visualFace3DObject_Current != null)
            //{
            //    menuItem = new MenuItem();
            //    menuItem.Name = "MenuItem_Properties";
            //    menuItem.Header = "Properties";
            //    menuItem.Click += MenuItem_Properties_Click;
            //    menuItem.Tag = hitTestResult;
            //    ContextMenu_Grid.Items.Add(menuItem);
            //}
        }

        private void MenuItem_CenterView_Click(object sender, RoutedEventArgs e)
        {
            CenterView();
        }

        private void Camera_Changed(object sender, EventArgs e)
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.Direction = ProjectionCamera.LookDirection;
            }

            if (visualBackground != null)
            {
                Viewport.Children.Remove(visualBackground);
            }

            visualBackground = Create.VisualBackground(Viewport);
            if(visualBackground == null)
            {
                return;
            }

            Viewport.Children.Add(visualBackground);
        }

        public List<T> GetVisualSAMObjects<T>() where T : IVisualJSAMObject
        {
            return Core.UI.WPF.Query.VisualJSAMObjects<T>(Viewport);
        }
        
        public void CenterView()
        {
            ProjectionCamera projectionCamera = ProjectionCamera;
            if(projectionCamera.IsFrozen)
            {
                return;
            }
            
            List<VisualGeometryObjectModel> visualGeometryObjectModels = GetVisualSAMObjects<VisualGeometryObjectModel>();
            if (visualGeometryObjectModels == null)
            {
                return;
            }

            Rect3D rect3D = Query.Bounds(visualGeometryObjectModels);

            HelixToolkit.Wpf.CameraHelper.ZoomExtents(projectionCamera, Viewport, rect3D);
        }

        public Mode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                mode = value;
                UpdateCamera();
            }
        }

        private void UpdateCamera()
        {
            switch(mode)
            {
                case Mode.TwoDimensional:
                    if(Viewport.Camera is OrthographicCamera)
                    {
                        return;
                    }

                    OrthographicCamera orthographicCamera = new OrthographicCamera()
                    {
                        Position = new Point3D(0, 0, 9),
                        LookDirection = (new Spatial.Vector3D(0, 0.0001, -0.9999)).ToMedia3D(),
                        NearPlaneDistance = double.NegativeInfinity,
                    };

                    orthographicCamera.Changed += Camera_Changed;
                    
                    Viewport.Camera = orthographicCamera;
                    break;

                case Mode.ThreeDimensional:
                    if (Viewport.Camera is PerspectiveCamera)
                    {
                        return;
                    }

                    PerspectiveCamera perspectiveCamera = new PerspectiveCamera()
                    {
                        Position = new Point3D(10, 5, 9),
                        LookDirection = new Vector3D(-10, -5, -9),
                        NearPlaneDistance = 1,
                    };

                    perspectiveCamera.Changed += Camera_Changed;

                    Viewport.Camera = perspectiveCamera;
                    break;
            }
        }

        public ProjectionCamera ProjectionCamera
        {
            get
            {
                return Viewport.Camera as ProjectionCamera;
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }
    }
}
