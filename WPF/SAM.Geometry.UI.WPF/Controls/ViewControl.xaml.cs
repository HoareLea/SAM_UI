using SAM.Core;
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
        public event ObjectHooveredEventHandler ObjectHoovered;
        public event ObjectDoubleClickedEventHandler ObjectDoubleClicked;
        public event ObjectContextMenuOpeningEventHandler ObjectContextMenuOpening;

        private Mode mode = Mode.ThreeDimensional;
        
        private UIGeometryObjectModel uIGeometryObjectModel;

        private IVisualJSAMObject visualSAMObject_Highlight;

        private VisualBackground visualBackground;

        private Point point_Current;

        private bool show = false;

        public ViewControl()
        {
            InitializeComponent();

            viewport3D.Camera = null;
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

        public string Hint
        {
            get
            {
                return Information.Text;
            }

            set
            {
                Information.Text = value;
            }
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

        public ProjectionCamera ProjectionCamera
        {
            get
            {
                return viewport3D.Camera as ProjectionCamera;
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
                Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(viewport3D);
                return;
            }

            Transform3D tranform3D = Transform3D.Identity;

            VisualGeometryObjectModel visualGeometryObjectModel = Core.UI.WPF.Query.VisualJSAMObjects<VisualGeometryObjectModel>(viewport3D)?.FirstOrDefault();
            if (visualGeometryObjectModel != null)
            {
                tranform3D = visualGeometryObjectModel.Transform;
            }

            Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(viewport3D);

            visualGeometryObjectModel = Convert.ToMedia3D(geometryObjectModel);
            visualGeometryObjectModel.Transform = tranform3D;

            if (visualGeometryObjectModel != null)
            {
                viewport3D.Children.Add(visualGeometryObjectModel);
            }

            UpdateCamera();
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Grid grid = sender as Grid;
            grid.ContextMenu = new ContextMenu();

            Point point_Current = Mouse.GetPosition(viewport3D);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(viewport3D, point_Current);
            IVisualGeometryObject visualGeometryObject = hitTestResult?.VisualHit as IVisualGeometryObject;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CenterView";
            menuItem.Header = "Center View";
            menuItem.Click += MenuItem_CenterView_Click;
            menuItem.Tag = hitTestResult;
            grid.ContextMenu.Items.Add(menuItem);

            if(visualGeometryObject != null)
            {
                ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(grid.ContextMenu, e, visualGeometryObject));
            }



            //if (visualGeometryObject != null)
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

        private void ProjectionCamera_Changed(object sender, EventArgs e)
        {
            ChangeCamera();
        }

        private void ChangeCamera()
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.Direction = ProjectionCamera.LookDirection;
            }

            if (visualBackground != null)
            {
                viewport3D?.Children?.Remove(visualBackground);
                visualBackground = null;
            }

            if(Mode == Mode.ThreeDimensional)
            {
                visualBackground = Create.VisualBackground(viewport3D);
                if (visualBackground == null)
                {
                    return;
                }

                viewport3D.Children.Add(visualBackground);
            }
        }

        public List<T> GetVisualSAMObjects<T>() where T : IVisualJSAMObject
        {
            return Core.UI.WPF.Query.VisualJSAMObjects<T>(viewport3D);
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

            HelixToolkit.Wpf.CameraHelper.ZoomExtents(projectionCamera, viewport3D, rect3D);
        }

        private void UpdateCamera()
        {
            ProjectionCamera projectionCamera = null;
            
            switch(mode)
            {
                case Mode.TwoDimensional:
                    if(viewport3D.Camera is OrthographicCamera)
                    {
                        return;
                    }

                    OrthographicCamera orthographicCamera = new OrthographicCamera()
                    {
                        Position = new Point3D(0, 0, 9),
                        LookDirection = (new Spatial.Vector3D(0, 0.0001, -0.9999)).ToMedia3D(),
                        NearPlaneDistance = double.NegativeInfinity,
                    };

                    projectionCamera = orthographicCamera;
                    break;

                case Mode.ThreeDimensional:
                    if (viewport3D.Camera is PerspectiveCamera)
                    {
                        return;
                    }

                    PerspectiveCamera perspectiveCamera = new PerspectiveCamera()
                    {
                        Position = new Point3D(10, 5, 9),
                        LookDirection = new Vector3D(-10, -5, -9),
                        NearPlaneDistance = 1,
                    };

                    projectionCamera = perspectiveCamera;
                    break;
            }

            if(projectionCamera == null)
            {
                return;
            }

            projectionCamera.Changed += ProjectionCamera_Changed;
            viewport3D.Camera = projectionCamera;
            ChangeCamera();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(viewport3D);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(viewport3D, point_Current, out IVisualJSAMObject visualJSAMObject);
                if (rayMeshGeometry3DHitTestResult != null && visualJSAMObject != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, visualJSAMObject));
                }
            }
        }

        private void Viewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            visualSAMObject_Highlight?.SetHighlight(false);
            visualSAMObject_Highlight = null;

            Point point = e.GetPosition(viewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(viewport3D, point, out IVisualJSAMObject visualJSAMObject);
            if (rayMeshGeometry3DHitTestResult != null && visualJSAMObject != null)
            {
                visualSAMObject_Highlight = visualJSAMObject;
                visualJSAMObject.SetHighlight(true);

                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, visualJSAMObject));
            }

            List<IVisualFace3DObject> visualFace3DObjects = null;

            if (show)
            {
                visualFace3DObjects = GetVisualSAMObjects<IVisualFace3DObject>();
                visualFace3DObjects?.ForEach(x => x.Opacity = 1);
                show = false;
            }

            double dx = point.X - point_Current.X;
            double dy = point.Y - point_Current.Y;
            point_Current = point;

            if(Mode == Mode.TwoDimensional)
            {
                return;
            }

            if (e.MouseDevice.MiddleButton is MouseButtonState.Pressed || e.MouseDevice.LeftButton is MouseButtonState.Pressed)
            {
                double distance = dx * dx + dy * dy;
                if (distance <= 0)
                {
                    return;
                }

                PerspectiveCamera perspectiveCamera = viewport3D.Camera as PerspectiveCamera;
                if (perspectiveCamera == null)
                {
                    return;
                }

                Planar.Vector2D vector2D = new Planar.Vector2D(dx, dy);

                if (visualFace3DObjects == null)
                {
                    visualFace3DObjects = GetVisualSAMObjects<IVisualFace3DObject>();
                }

                if (visualFace3DObjects == null)
                {
                    return;
                }

                Rect3D rect3D = Query.Bounds(GetVisualSAMObjects<VisualGeometryObjectModel>());
                Point3D center = Core.UI.WPF.Query.Center(rect3D);
                //Point3D center = Query.Center(visualFace3DObjects);
                if (center == null || center.IsNaN())
                {
                    center = new Point3D(0, 0, 0);
                }

                if (e.MouseDevice.MiddleButton is MouseButtonState.Pressed)
                {
                    vector2D.Scale(0.1);

                    //Version 3
                    Spatial.Plane plane = Query.Plane(ProjectionCamera);

                    double angle = distance / perspectiveCamera.FieldOfView % 45;
                    Spatial.Vector3D vector3D = Spatial.Query.Convert(plane, vector2D);
                    vector3D = vector3D.CrossProduct(plane.Normal).GetNegated();
                    vector3D = new Spatial.Vector3D(0, 0, vector3D.Z);

                    GetVisualSAMObjects<VisualGeometryObjectModel>()?.ForEach(x => Modify.Rotate(x, vector3D, center.ToSAM(), angle));

                    //Version 2
                    //perspectiveCamera.Rotate(vector2D, center.ToSAM());

                    //Version 1
                    //vector3D = vector3D.CrossProduct(plane.Normal);
                    //double angle = distance / perspectiveCamera.FieldOfView % 45;
                    //perspectiveCamera.Rotate(Convert.ToMedia3D(vector3D), angle);
                }
                else if (e.MouseDevice.LeftButton is MouseButtonState.Pressed)
                {
                    double factor = ProjectionCamera.Position.ToSAM().Distance(center.ToSAM()) / 10;
                    vector2D.Scale(factor);

                    Spatial.Plane plane = Query.Plane(ProjectionCamera);
                    Spatial.Vector3D vector3D = Spatial.Query.Convert(plane, vector2D);

                    perspectiveCamera.Move(Convert.ToMedia3D(vector3D), 0.01);
                }
            }
        }

        private void Viewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            Information.Text = string.Empty;
            //CenterView();
        }

        private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mode == Mode.ThreeDimensional)
            {
                Point point = e.GetPosition(viewport3D);

                Spatial.Vector3D vector3D = null;

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult<ModelVisual3D>(viewport3D, point);
                if (rayMeshGeometry3DHitTestResult != null)
                {
                    vector3D = new Spatial.Vector3D(ProjectionCamera.Position.ToSAM(), rayMeshGeometry3DHitTestResult.PointHit.ToSAM());
                }
                else
                {
                    vector3D = new Spatial.Vector3D(ProjectionCamera.Position.X, ProjectionCamera.Position.Y, ProjectionCamera.Position.Z);
                    vector3D.Negate();
                }

                if (vector3D == null)
                {
                    return;
                }

                double factor = e.Delta / 100;//* vector3D.Length;

                vector3D.Normalize();
                vector3D.Scale(factor);

                ProjectionCamera.Position = Convert.ToMedia3D(ProjectionCamera.Position.ToSAM().GetMoved(vector3D) as Spatial.Point3D);
            }


        }

        public List<IJSAMObject> Show<T>(IEnumerable<T> jSAMObjects) where T : IJSAMObject
        {
            show = true;

            List<VisualGeometryObjectModel> VisualGeometryObjectModels = GetVisualSAMObjects<VisualGeometryObjectModel>();
            if (VisualGeometryObjectModels == null || VisualGeometryObjectModels.Count == 0)
            {
                return null;
            }

            List<IJSAMObject> result = new List<IJSAMObject>();
            foreach (VisualGeometryObjectModel visualGeometryObjectModel in VisualGeometryObjectModels)
            {
                if (visualGeometryObjectModel == null)
                {
                    continue;
                }

                Visual3DCollection visual3DCollection = visualGeometryObjectModel.Children;
                if(visual3DCollection == null)
                {
                    continue;
                }

                foreach(Visual3D visual3D in visual3DCollection)
                {
                    if(!(visual3D is IVisualGeometryObject))
                    {
                        continue;
                    }

                    IVisualGeometryObject visualGeometryObject = (IVisualGeometryObject)visual3D;
                    foreach(T t in jSAMObjects)
                    {
                        visualGeometryObject.Opacity = 0.1;

                        if (visualGeometryObject.Similar(t))
                        {
                            visualGeometryObject.Opacity = 1;
                        }
                    }
                }


                //IJSAMObject jSAMObject = jSAMObjects.ToList().Find(x => visualJSAMObject.Similar(x));
                //visualJSAMObject.Opacity = jSAMObject == null ? 1 : 0.1;
            }

            return result;
        }

    }
}
