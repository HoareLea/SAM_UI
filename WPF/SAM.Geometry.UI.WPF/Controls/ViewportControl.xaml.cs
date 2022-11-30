using SAM.Core;
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

        private IVisualJSAMObject visualSAMObject_Highlight;
        private List<IVisualJSAMObject> visualSAMObjects_Selected;

        public ViewportControl()
        {
            InitializeComponent();

            helixViewport3D.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            helixViewport3D.RotateGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
            uIGeometryObjectModel = new UIGeometryObjectModel();
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

        private void Clear(bool selection = false, bool highlight = false)
        {
            if(selection)
            {
                if (visualSAMObjects_Selected != null && visualSAMObjects_Selected.Count != 0)
                {
                    foreach (IVisualJSAMObject visualSAMObject_Selected in visualSAMObjects_Selected)
                    {
                        visualSAMObject_Selected.SetSelected(false);
                        Modify.Restore((visualSAMObject_Selected as dynamic)?.Content);
                        visualSAMObject_Selected.SetHighlight(false);
                        Modify.Restore((visualSAMObject_Selected as dynamic)?.Content);
                    }
                }

                visualSAMObjects_Selected = null;
            }


            if (highlight)
            {
                visualSAMObject_Highlight?.SetHighlight(false);
                Modify.Restore((visualSAMObject_Highlight as dynamic)?.Content);
                visualSAMObject_Highlight = null;

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
            int count = Core.UI.WPF.Query.VisualJSAMObjects<IVisualJSAMObject>(helixViewport3D.Children).Count;
            if(count > 0)
            {
                Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(helixViewport3D.Children);
            }

            VisualGeometryObjectModel visualGeometryObjectModel = Convert.ToMedia3D(geometryObjectModel);
            if (visualGeometryObjectModel != null)
            {
                helixViewport3D.Children.Add(visualGeometryObjectModel);
            }

            if(count == 0)
            {
                helixViewport3D.ZoomExtents();
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
            Modify.Restore((visualSAMObject_Highlight as dynamic)?.Content);
            if(visualSAMObjects_Selected != null && visualSAMObjects_Selected.Count > 0)
            {
                if(visualSAMObjects_Selected.Contains(visualSAMObject_Highlight))
                {
                    visualSAMObject_Highlight?.SetSelected(true);
                }
            }
            
            
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

        private void helixViewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point_Current = e.GetPosition(helixViewport3D);

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(helixViewport3D, point_Current, out IVisualJSAMObject visualJSAMObject);
            //if(visualJSAMObject != null)
            //{
            //    IJSAMObject jSAMObject = visualJSAMObject.JSAMObject<IJSAMObject>();
            //    if (jSAMObject is Text3DObject)
            //    {
            //        visualJSAMObject = visualJSAMObject.Parent<DependencyObject>() as IVisualJSAMObject;
            //    }

            //    visualJSAMObject = Query.SelectableVisualJSAMObject(visualJSAMObject);
            //}

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Clear(selection: true);
            }

            if (rayMeshGeometry3DHitTestResult == null && visualJSAMObject == null)
            {
                e.Handled = true;
                return;
            }

            if (visualSAMObjects_Selected == null)
            {
                visualSAMObjects_Selected = new List<IVisualJSAMObject>();
            }

            if(visualSAMObjects_Selected.Contains(visualJSAMObject))
            {
                visualJSAMObject.SetSelected(false);
                Modify.Restore((visualJSAMObject as dynamic).Content);
                visualSAMObjects_Selected.Remove(visualJSAMObject);
            }
            else
            {
                visualJSAMObject.SetSelected(true);
                visualSAMObjects_Selected.Add(visualJSAMObject);
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
    }
}
