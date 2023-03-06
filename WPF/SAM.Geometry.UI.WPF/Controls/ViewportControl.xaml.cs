using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private ActionManager actionManager;

        private RectangularSelector rectangularSelector;

        public ViewportControl()
        {
            InitializeComponent();

            helixViewport3D.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            helixViewport3D.RotateGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
            uIGeometryObjectModel = new UIGeometryObjectModel();

            helixViewport3D.Loaded += helixViewport3D_Loaded;

            actionManager = new ActionManager();
            rectangularSelector = new RectangularSelector(grid);
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

        public Visual3D GetVisual3D<T>(Guid guid) where T :SAMObject
        {
            return Core.UI.WPF.Query.Visual3D<T>(helixViewport3D.Children, guid);
        }

        public bool ContainsAny<T>(IEnumerable<Guid> guids) where T: SAMObject
        {
            return Query.ContainsAny<T>(helixViewport3D.Children, guids);
        }

        public T GetSAMObject<T>(Guid guid) where T : SAMObject
        {
            return Core.UI.WPF.Query.JSAMObject<T>(GetVisual3D<T>(guid));
        }

        public bool Select(SAMObject sAMObject)
        {
            if(sAMObject == null)
            {
                return false;
            }

            Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
            if(visual3D == null)
            {
                return false;
            }

            return actionManager.Apply(new SelectAction(visual3D));
        }

        public bool Select<T>(IEnumerable<T> sAMObjects) where T : SAMObject
        {
            if (sAMObjects == null)
            {
                return false;
            }

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach(T t in sAMObjects)
            {
                Visual3D visual3D = GetVisual3D<SAMObject>(t.Guid);
                if (visual3D == null)
                {
                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            if(visual3Ds == null || visual3Ds.Count == 0)
            {
                return false;
            }

            return actionManager.Apply(new SelectAction(visual3Ds));
        }

        public bool Zoom(SAMObject sAMObject)
        {
            if (sAMObject == null)
            {
                return false;
            }

            Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
            if (visual3D == null)
            {
                return false;
            }

            Rect3D rect3D = visual3D.Bounds();
            if(rect3D == Rect3D.Empty)
            {
                return false;
            }

            helixViewport3D.ZoomExtents(rect3D);
            return true;
        }

        public bool Zoom<T>(IEnumerable<T> sAMObjects) where T: SAMObject
        {
            if (sAMObjects == null)
            {
                return false;
            }

            List<Rect3D> rect3Ds = new List<Rect3D>();
            foreach(SAMObject sAMObject in sAMObjects)
            {
                Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
                if (visual3D == null)
                {
                    continue;
                }

                Rect3D rect3D = visual3D.Bounds();
                if (rect3D == Rect3D.Empty)
                {
                    continue;
                }

                rect3Ds.Add(rect3D);
            }


            Rect3D rect3D_Union = Rect3D.Empty;
            foreach(Rect3D rect3D_Temp in rect3Ds)
            {
                rect3D_Union.Union(rect3D_Temp);
            }

            if(rect3D_Union == Rect3D.Empty)
            {
                return false;
            }

            helixViewport3D.ZoomExtents(rect3D_Union);
            return true;
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

        private void Load(GeometryObjectModel geometryObjectModel)
        {
            helixViewport3D.Lights.Children.Clear();
            helixViewport3D.Lights.Children.Add(new AmbientLight());

            actionManager.Cancel();
            int count = Core.UI.WPF.Query.Visual3Ds<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) }).Count;
            if (count > 0)
            {
                Core.UI.WPF.Modify.Clear<ModelVisual3D>(helixViewport3D.Children, new Type[] { typeof(GeometryObjectModel) });
            }

            if (geometryObjectModel == null)
            {
                return;
            }

            ModelVisual3D modelVisual3D = Convert.ToMedia3D(geometryObjectModel);
            if (modelVisual3D != null)
            {
                helixViewport3D.Children.Add(modelVisual3D);
            }

            if (count == 0)
            {
                helixViewport3D.ZoomExtents();
            }

            legendControl.Visibility = Visibility.Hidden;
            if (geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings))
            {
                Legend legend = viewSettings.Legend;
                List<LegendItem> legendItems = legend?.LegendItems;

                if (legend != null && legend.Visible && legendItems != null && legendItems.Count != 0)
                {
                    legendControl.Visibility = Visibility.Visible;
                    legendControl.Legend = legend;
                }
            }
        }

        private void MenuItem_ZoomExtents_Click(object sender, RoutedEventArgs e)
        {
            helixViewport3D.ZoomExtents();
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
            actionManager.Cancel<HighlightAction>();

            Point point = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point, out ModelVisual3D modelVisual3D);
            if (rayMeshGeometry3DHitTestResult != null && modelVisual3D != null)
            {
                actionManager.Apply(new HighlightAction(modelVisual3D));

                ObjectHoovered?.Invoke(this, new ObjectHooveredEventArgs(e, modelVisual3D));
            }
        }

        private void helixViewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point_Current = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out ModelVisual3D modelVisual3D);
            
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                actionManager.Cancel<SelectAction>();
            }

            if (rayMeshGeometry3DHitTestResult == null && modelVisual3D == null)
            {
                e.Handled = true;
                return;
            }

            SelectAction selectAction = actionManager.GetAction<SelectAction>();
            if (selectAction == null)
            {
                selectAction = new SelectAction();
            }

            if (selectAction != null && selectAction.Contains(modelVisual3D))
            {
                selectAction.Remove(modelVisual3D);
            }
            else
            {
                selectAction.Add(modelVisual3D);
            }

            actionManager.Apply(selectAction);

        }

        private void helixViewport3D_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                actionManager.Cancel<SelectAction>();
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
            menuItem.Name = "MenuItem_ZoomExtents";
            menuItem.Header = "Zoom Extents";
            menuItem.Click += MenuItem_ZoomExtents_Click;
            helixViewport3D.ContextMenu.Items.Add(menuItem);

            ObjectContextMenuOpening?.Invoke(this, new ObjectContextMenuOpeningEventArgs(helixViewport3D.ContextMenu, e, actionManager.SelectedVisual3Ds()?.FindAll(x => x is ModelVisual3D)?.ConvertAll(x => x as ModelVisual3D)));
        }

        private void helixViewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            GeometryObjectModel geometryObjectModel = uIGeometryObjectModel?.JSAMObject;

            if (geometryObjectModel != null && geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings viewSettings) && viewSettings != null)
            {
                Camera camera = viewSettings.Camera;
                if (camera != null)
                {
                    helixViewport3D.Camera.Position = camera.Location.ToMedia3D();
                    helixViewport3D.Camera.LookDirection = camera.LookDirection.ToMedia3D();
                }
            }

            helixViewport3D.Loaded -= helixViewport3D_Loaded;
        }

        public LegendItem UndefinedLegendItem
        {
            get
            {
                return legendControl.UndefinedLegendItem;
            }

            set
            {
                legendControl.UndefinedLegendItem = value;
            }
        }

        private void helixViewport3D_CameraChanged(object sender, RoutedEventArgs e)
        {
        }
    }
}
