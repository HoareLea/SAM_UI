using SAM.Core;
using SAM.Core.UI;
using SAM.Core.UI.WPF;
using SAM.Geometry.Object;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public event ObjectSelectionChangedEventHandler ObjectSelectionChanged;

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
            rectangularSelector.Selected += RectangularSelector_Selected;

            rectangularSelector.Selecting += RectangularSelector_Selecting;

        }

        private void Select(Rect rect, SelectionType selectionType)
        {
            actionManager.Cancel<HighlightAction>();

            if (rect == null || rect == Rect.Empty)
            {
                return;
            }

            if (selectionType == SelectionType.Undefined)
            {
                return;
            }

            List<SAMObject> sAMObjects = new List<SAMObject>();
            IEnumerable<HelixToolkit.Wpf.Viewport3DHelper.RectangleHitResult> rectangleHitResults = HelixToolkit.Wpf.Viewport3DHelper.FindHits(helixViewport3D.Viewport, rect, selectionType == SelectionType.Inside ? HelixToolkit.Wpf.SelectionHitMode.Inside : HelixToolkit.Wpf.SelectionHitMode.Touch);
            if (rectangleHitResults != null)
            {
                foreach (HelixToolkit.Wpf.Viewport3DHelper.RectangleHitResult rectangleHitResult in rectangleHitResults)
                {
                    IJSAMObject jSAMObject_Model = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(rectangleHitResult.Model);
                    IJSAMObject jSAMObject_Visual = Core.UI.WPF.Query.JSAMObject<IJSAMObject>(rectangleHitResult.Visual);

                    if (jSAMObject_Model is Text3DObject)
                    {
                        continue;
                    }

                    SAMObject sAMObject = (jSAMObject_Visual as ITaggable)?.Tag?.Value as SAMObject;
                    if (sAMObject != null)
                    {
                        Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
                        if (jSAMObject_Model is ISAMGeometry3DObject && jSAMObject_Visual is IEnumerable<ISAMGeometry3DObject>)
                        {
                            ISAMGeometry3DObject sAMGeometry3DObject = (ISAMGeometry3DObject)jSAMObject_Model;
                            IEnumerable<ISAMGeometry3DObject> sAMGeometry3DObjects = (IEnumerable<ISAMGeometry3DObject>)jSAMObject_Visual;
                            if (!sAMGeometry3DObjects.Contains(sAMGeometry3DObject))
                            {
                                continue;
                            }
                        }

                        sAMObjects.Add(sAMObject);
                    }

                }
            }

            Select(sAMObjects);
        }

        private void RectangularSelector_Selecting(object sender, EventArgs e)
        {

        }

        private void RectangularSelector_Selected(object sender, EventArgs e)
        {
            actionManager.Cancel<HighlightAction>();

            if (rectangularSelector == null)
            {
                return;
            }

            Rect rect = rectangularSelector.Rect;
            if (rect == null || rect == Rect.Empty)
            {
                return;
            }

            SelectionType selectionType = rectangularSelector.SelectionType;
            if (selectionType == SelectionType.Undefined)
            {
                return;
            }

            Select(rect, selectionType);

            //List<Spatial.Point3D> point3Ds = new List<Spatial.Point3D>();

            //foreach (Point2D point2D in point2Ds)
            //{
            //    Point point = new Point(point2D.X, point2D.Y);

            //    //point = helixViewport3D.Viewport.PointFromScreen(point);

            //    HelixToolkit.Wpf.Viewport3DHelper.Point2DtoPoint3D(helixViewport3D.Viewport, point, out Point3D point3D_Near, out Point3D point3D_Far);

            //    Spatial.Point3D point3D_Near_SAM = point3D_Near.ToSAM();
            //    point3D_Near_SAM = new Spatial.Point3D(point3D_Near_SAM.X, point3D_Near.Y, 5);

            //    Spatial.Point3D point3D_Far_SAM = point3D_Far.ToSAM();
            //    point3D_Far_SAM = new Spatial.Point3D(point3D_Far_SAM.X, point3D_Far.Y, -10);

            //    point3Ds.Add(point3D_Near_SAM);
            //    point3Ds.Add(point3D_Far_SAM);
            //}

            //Spatial.BoundingBox3D boundingBox3D = new Spatial.BoundingBox3D(point3Ds);

            //List<Spatial.ISAMGeometry3DObject> sAMGeometry3DObjects = null;
            //switch (selectionType)
            //{
            //    case SelectionType.Inside:
            //        sAMGeometry3DObjects = uIGeometryObjectModel?.JSAMObject?.GetSAMGeometryObjects<Spatial.ISAMGeometry3DObject>(x => x is Spatial.ISAMGeometry3DObject && Spatial.Query.Inside(boundingBox3D, x, true));
            //        break;

            //    case SelectionType.InsideOrIntersect:
            //        sAMGeometry3DObjects = uIGeometryObjectModel?.JSAMObject?.GetSAMGeometryObjects<Spatial.ISAMGeometry3DObject>(x => x is Spatial.ISAMGeometry3DObject && Spatial.Query.InRange(boundingBox3D, x));
            //        break;
            //}

            //if (sAMGeometry3DObjects == null)
            //{
            //    return;
            //}

            //Select(sAMGeometry3DObjects.ConvertAll(x => x as ITaggable).FindAll(x => x != null).ConvertAll(x => x?.Tag?.Value as SAMObject));

        }

        public Mode Mode
        {
            get
            {
                return mode;
            }

            set
            {
                if (mode != value)
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

        public Visual3D GetVisual3D<T>(Guid guid) where T : SAMObject
        {
            return Core.UI.WPF.Query.Visual3D<T>(helixViewport3D.Children, guid);
        }

        public bool ContainsAny<T>(IEnumerable<Guid> guids) where T : SAMObject
        {
            return Query.ContainsAny<T>(helixViewport3D.Children, guids);
        }

        public T GetSAMObject<T>(Guid guid) where T : SAMObject
        {
            return Core.UI.WPF.Query.JSAMObject<T>(GetVisual3D<T>(guid));
        }

        public bool Select(SAMObject sAMObject)
        {
            bool result = false;

            if (sAMObject == null)
            {
                result = actionManager.Cancel<SelectAction>();
                if(result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
                return result;
            }

            Visual3D visual3D = GetVisual3D<SAMObject>(sAMObject.Guid);
            if (visual3D == null)
            {
                return result;
            }

            result = actionManager.Apply(new SelectAction(visual3D));
            if (result)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

            return result;
        }

        public bool Select<T>(IEnumerable<T> sAMObjects) where T : SAMObject
        {
            bool result = false;
            
            if (sAMObjects == null)
            {
                result = actionManager.Cancel<SelectAction>();
                if (result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
                return result;
            }

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach (T t in sAMObjects)
            {
                Visual3D visual3D = GetVisual3D<SAMObject>(t.Guid);
                if (visual3D == null)
                {
                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            if (visual3Ds == null || visual3Ds.Count == 0)
            {
                result = actionManager.Cancel<SelectAction>();
                if (result)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

                return result;
            }

            result = actionManager.Apply(new SelectAction(visual3Ds));
            if (result)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

            return result;
        }

        public List<T> SelectedSAMObjects<T>() where T : SAMObject
        {
            if (actionManager == null)
            {
                return null;
            }

            List<SelectAction> selectActions = actionManager.GetActions<SelectAction>();
            if (selectActions == null || selectActions.Count == 0)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (SelectAction selectAction in selectActions)
            {
                List<Visual3D> visual3Ds = selectAction.Visual3Ds;
                if(visual3Ds != null)
                {
                    foreach(Visual3D visual3D in visual3Ds)
                    {
                        ITaggable taggable = visual3D?.JSAMObject<IJSAMObject>() as ITaggable;
                        if (taggable != null)
                        {
                            T t = taggable.Tag?.Value as T;
                            if(t != null)
                            {
                                result.Add(t);
                            }
                        }
                    }
                }
            }

            return result;
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
                helixViewport3D.Camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0.0001, -0.9999);
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

            bool cancelled = actionManager.Cancel();
            if(cancelled)
            {
                ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
            }

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

            bool cancelled = false;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                cancelled = actionManager.Cancel<SelectAction>();
            }

            if (rayMeshGeometry3DHitTestResult == null && modelVisual3D == null)
            {
                e.Handled = true;

                if(cancelled)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }

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
            ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());

        }

        private void helixViewport3D_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                bool cancelled = actionManager.Cancel<SelectAction>();
                if(cancelled)
                {
                    ObjectSelectionChanged?.Invoke(this, new ObjectSelectionChangedEventArgs());
                }
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
