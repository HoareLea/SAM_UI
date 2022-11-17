using SAM.Core.UI.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// Interaction logic for ViewportControl.xaml
    /// </summary>
    public partial class ViewportControl : UserControl
    {
        private Mode mode = Mode.ThreeDimensional;

        private UIGeometryObjectModel uIGeometryObjectModel;

        public event ObjectHooveredEventHandler ObjectHoovered;
        public event ObjectDoubleClickedEventHandler ObjectDoubleClicked;
        public event ObjectContextMenuOpeningEventHandler ObjectContextMenuOpening;

        private IVisualJSAMObject visualSAMObject_Highlight;

        public ViewportControl()
        {
            InitializeComponent();

            helixViewport3D.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            helixViewport3D.RotateGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
            uIGeometryObjectModel = new UIGeometryObjectModel();

            //helixViewport3D.ShowCameraInfo = true;
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
                UpdateMode();
            }
        }

        private void UpdateMode()
        {
            if(mode == Mode.ThreeDimensional)
            {
                helixViewport3D.Camera = new PerspectiveCamera();

                helixViewport3D.Orthographic = false;
                helixViewport3D.ShowViewCube = true;
                helixViewport3D.ShowCoordinateSystem = true;
                helixViewport3D.CameraMode = HelixToolkit.Wpf.CameraMode.Inspect;
                helixViewport3D.ZoomAroundMouseDownPoint = true;
                helixViewport3D.IsPanEnabled = true;
                helixViewport3D.IsRotationEnabled = true;

            }
            else
            {
                helixViewport3D.Orthographic = true;
                helixViewport3D.ShowViewCube = false;
                helixViewport3D.ShowCoordinateSystem = false;
                helixViewport3D.Camera.LookDirection = new Vector3D(0, 0.0001, -0.9999);
                helixViewport3D.Camera.NearPlaneDistance = -1000;
                helixViewport3D.Camera.FarPlaneDistance = 1000;
                helixViewport3D.ZoomAroundMouseDownPoint = false;
                helixViewport3D.IsPanEnabled = true;
                helixViewport3D.IsRotationEnabled = false;
            }

            helixViewport3D.ZoomExtents();
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

                gridLinesVisual3D.Visible = uIGeometryObjectModel?.JSAMObject == null;
            }
        }

        private void helixViewport3D_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = true;
                return;
            }

            helixViewport3D.ContextMenu = new ContextMenu();

            Point point_Current = Mouse.GetPosition(helixViewport3D);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(helixViewport3D, point_Current);
            IVisualGeometryObject visualGeometryObject = hitTestResult?.VisualHit as IVisualGeometryObject;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomExtends";
            menuItem.Header = "Zoom Extends";
            menuItem.Click += MenuItem_ZoomExtends_Click;
            menuItem.Tag = hitTestResult;
            helixViewport3D.ContextMenu.Items.Add(menuItem);

            if (visualGeometryObject != null)
            {
                ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(helixViewport3D.ContextMenu, e, visualGeometryObject));
            }
        }

        private void MenuItem_ZoomExtends_Click(object sender, RoutedEventArgs e)
        {
            helixViewport3D.ZoomExtents();
        }

        private void Load(GeometryObjectModel geometryObjectModel, bool zoomExtends = true)
        {
            Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(helixViewport3D.Children);

            VisualGeometryObjectModel visualGeometryObjectModel = Convert.ToMedia3D(geometryObjectModel);
            if (visualGeometryObjectModel != null)
            {
                helixViewport3D.Children.Add(visualGeometryObjectModel);

                if(zoomExtends)
                {
                    helixViewport3D.ZoomExtents();
                }
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
            Load(uIGeometryObjectModel?.JSAMObject, false);
        }

        private void helixViewport3D_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(helixViewport3D);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out IVisualJSAMObject visualJSAMObject);
                if (rayMeshGeometry3DHitTestResult != null && visualJSAMObject != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, visualJSAMObject));
                }
            }
        }

        private void helixViewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            visualSAMObject_Highlight?.SetHighlight(false);
            visualSAMObject_Highlight = null;

            Point point = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point, out IVisualJSAMObject visualJSAMObject);
            if (rayMeshGeometry3DHitTestResult != null && visualJSAMObject != null)
            {
                visualSAMObject_Highlight = visualJSAMObject;
                visualJSAMObject.SetHighlight(true);

                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, visualJSAMObject));
            }
        }
    }
}
