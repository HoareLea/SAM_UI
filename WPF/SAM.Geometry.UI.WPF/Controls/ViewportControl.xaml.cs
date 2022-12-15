using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
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

        public Guid Guid { get; set; }

        private Visual3D visual3D_Highlight;
        private List<Visual3D> visual3Ds_Selected;

        public ViewportControl()
        {
            InitializeComponent();

            helixViewport3D.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            helixViewport3D.RotateGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
            uIGeometryObjectModel = new UIGeometryObjectModel();

            helixViewport3D.Loaded += helixViewport3D_Loaded;
        }

        private void helixViewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            GeometryObjectModel geometryObjectModel = uIGeometryObjectModel?.JSAMObject;

            if (geometryObjectModel != null && geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings) && viewSettings != null)
            {
                Camera camera = viewSettings.Camera;
                if(camera != null)
                {
                    helixViewport3D.Camera.Position = camera.Location.ToMedia3D();
                    helixViewport3D.Camera.LookDirection = camera.LookDirection.ToMedia3D();
                }
            }

            helixViewport3D.Loaded -= helixViewport3D_Loaded;
        }

        public Mode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                if(mode != value)
                {
                    mode = value;
                    UpdateMode();
                }
            }
        }

        public Camera Camera
        {
            get
            {
                return GetCamera();
            }

            set
            {
                SetCamera(value);
            }
        }

        private Camera GetCamera()
        {
            ProjectionCamera projectionCamera = helixViewport3D.Camera;
            if(projectionCamera == null)
            {
                return null;
            }

            return new Camera(projectionCamera.Position.ToSAM(), projectionCamera.LookDirection.ToSAM());
        }

        private void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            ProjectionCamera projectionCamera = helixViewport3D.Camera;
            if (projectionCamera == null)
            {
                return;
            }

            Spatial.Vector3D lookDirection = camera.LookDirection;
            if (lookDirection.AlmostEqual(-Spatial.Vector3D.WorldZ, Tolerance.MacroDistance))
            {
                lookDirection = new Spatial.Vector3D(0, 0.0001, -0.9999);
            }
            else if (lookDirection.AlmostEqual(Spatial.Vector3D.WorldZ, Tolerance.MacroDistance))
            {
                lookDirection = new Spatial.Vector3D(0, 0.0001, 0.9999);
            }

            projectionCamera.LookDirection = lookDirection.ToMedia3D();
            projectionCamera.Position = camera.Location.ToMedia3D();
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
                //helixViewport3D.ShowCameraInfo = true;

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
                //helixViewport3D.ShowCameraInfo = true;
            }

            helixViewport3D.ZoomExtents();
        }

        private void Clear(bool selection = false, bool highlight = false)
        {
            if(selection)
            {
                if (visual3Ds_Selected != null && visual3Ds_Selected.Count != 0)
                {
                    foreach (Visual3D visual3D_Selected in visual3Ds_Selected)
                    {
                        Modify.Select(visual3D_Selected, false);
                        Modify.Highlight(visual3D_Selected, false);
                    }
                }

                visual3Ds_Selected = null;
            }

            if (highlight)
            {
                Modify.Highlight(visual3D_Highlight, false);
                visual3D_Highlight = null;
            }
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

        private void MenuItem_ZoomExtends_Click(object sender, RoutedEventArgs e)
        {
            helixViewport3D.ZoomExtents();
        }

        private void Load(GeometryObjectModel geometryObjectModel)
        {
            Clear(selection: true, highlight: true);
            int count = Core.UI.WPF.Query.Visual3Ds<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) }).Count;
            if(count > 0)
            {
                Core.UI.WPF.Modify.Clear<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) });
            }

            if(geometryObjectModel == null)
            {
                return;
            }

            ModelVisual3D modelVisual3D = Convert.ToMedia3D(geometryObjectModel);
            if (modelVisual3D != null)
            {
                helixViewport3D.Children.Add(modelVisual3D);
            }

            if(count == 0)
            {
                helixViewport3D.ZoomExtents();
            }

            legendControl.Visibility = Visibility.Hidden;
            if(geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings))
            {
                Legend legend = viewSettings.Legend;
                List<LegendItem> legendItems = legend?.LegendItems;

                if(legend != null && legend.Visible && legendItems != null && legendItems.Count != 0)
                {
                    legendControl.Visibility = Visibility.Visible;
                    legendControl.Legend = legend;
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
            Load(uIGeometryObjectModel?.JSAMObject);
        }

        private void helixViewport3D_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(helixViewport3D);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out ModelVisual3D modelVisual3D);
                if (rayMeshGeometry3DHitTestResult != null && modelVisual3D != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, modelVisual3D));
                }
            }
        }

        private void helixViewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(visual3D_Highlight != null)
            {
                Modify.Highlight(visual3D_Highlight, false);
                if (visual3Ds_Selected != null && visual3Ds_Selected.Count > 0)
                {
                    if (visual3Ds_Selected.Contains(visual3D_Highlight))
                    {
                        Modify.Select(visual3D_Highlight, true);
                    }
                }
            }

            visual3D_Highlight = null;

            Point point = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point, out ModelVisual3D modelVisual3D);
            if (rayMeshGeometry3DHitTestResult != null && modelVisual3D != null)
            {
                visual3D_Highlight = modelVisual3D;
                Modify.Highlight(visual3D_Highlight, true);

                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, modelVisual3D));
            }
        }

        private void helixViewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point_Current = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out ModelVisual3D modelVisual3D);
            
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Clear(selection: true);
            }

            if (rayMeshGeometry3DHitTestResult == null && modelVisual3D == null)
            {
                e.Handled = true;
                return;
            }

            if (visual3Ds_Selected == null)
            {
                visual3Ds_Selected = new List<Visual3D>();
            }

            if(visual3Ds_Selected.Contains(modelVisual3D))
            {
                Modify.Select(modelVisual3D, false);
                visual3Ds_Selected.Remove(modelVisual3D);
            }
            else
            {
                Modify.Select(modelVisual3D, true);
                visual3Ds_Selected.Add(modelVisual3D);
            }
        }

        private void helixViewport3D_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                Clear(selection: true);
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

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_ZoomExtends";
            menuItem.Header = "Zoom Extends";
            menuItem.Click += MenuItem_ZoomExtends_Click;
            helixViewport3D.ContextMenu.Items.Add(menuItem);

            ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(helixViewport3D.ContextMenu, e, visual3Ds_Selected?.FindAll(x => x is ModelVisual3D)?.ConvertAll(x => x as ModelVisual3D)));
        }
    }
}
