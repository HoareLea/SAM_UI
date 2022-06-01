using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for View3DForm.xaml
    /// </summary>
    public partial class View3DControl : UserControl
    {
        private UIAnalyticalModel uIAnalyticalModel;
        private ModelVisual3D modelVisual3D_Selected;

        private Point from;

        public View3DControl()
        {
            InitializeComponent();
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            Clear();

            List<Panel> panels = analyticalModel?.GetPanels();
            if(panels != null)
            {
                foreach(Panel panel in panels)
                {
                    VisualPanel visualPanel = panel.ToMedia3D();
                    if(visualPanel == null)
                    {
                        continue;
                    }

                    Viewport.Children.Add(visualPanel);
                }
            }
        }
        
        private void Clear()
        {
            Visual3DCollection visual3DCollection = Viewport.Children;
            if (visual3DCollection == null)
            {
                return;
            }

            List<Visual3D> visual3Ds = new List<Visual3D>();
            foreach (Visual3D visual3D in visual3DCollection)
            {
                if (visual3D is ModelVisual3D && ((ModelVisual3D)visual3D).Content is Light)
                {
                    continue;
                }

                visual3Ds.Add(visual3D);
            }

            foreach(Visual3D visual3D in visual3Ds)
            {
                Viewport.Children.Remove(visual3D);
            }
        }

        public List<VisualPanel> VisualPanels
        {
            get
            {
                Visual3DCollection visual3DCollection = Viewport.Children;
                if (visual3DCollection == null)
                {
                    return null;
                }

                List<VisualPanel> result = new List<VisualPanel>();
                foreach (object @object in visual3DCollection)
                {
                    VisualPanel visualPanel = @object as VisualPanel;
                    if (visualPanel == null)
                    {
                        continue;
                    }

                    result.Add(visualPanel);
                }

                return result;
            }
        }

        public UIAnalyticalModel UIAnalyticalModel
        {
            get
            {
                return uIAnalyticalModel;
            }

            set
            {
                uIAnalyticalModel = value;
                if(uIAnalyticalModel != null)
                {
                    uIAnalyticalModel.Modified -= UIAnalyticalModel_Modified;
                    uIAnalyticalModel.Modified += UIAnalyticalModel_Modified;

                    uIAnalyticalModel.Closed -= UIAnalyticalModel_Closed;
                    uIAnalyticalModel.Closed += UIAnalyticalModel_Closed;

                    uIAnalyticalModel.Opened -= UIAnalyticalModel_Opened;
                    uIAnalyticalModel.Opened += UIAnalyticalModel_Opened;
                }
            }
        }

        private void UIAnalyticalModel_Opened(object sender, EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void UIAnalyticalModel_Closed(object sender, EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void UIAnalyticalModel_Modified(object sender, EventArgs e)
        {
            LoadAnalyticalModel(uIAnalyticalModel?.JSAMObject);
        }

        private void UserControl_MouseWeel(object sender, MouseWheelEventArgs e)
        {
            MainCamera.Position = new Point3D(MainCamera.Position.X - e.Delta / 360D, MainCamera.Position.Y - e.Delta / 360D, MainCamera.Position.Z - e.Delta / 360D);
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(Viewport);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current) as RayMeshGeometry3DHitTestResult;

                VisualPanel visualPanel = rayMeshGeometry3DHitTestResult?.VisualHit as VisualPanel;
                if (visualPanel == null)
                {
                    return;
                }

                UI.Modify.EditPanel(uIAnalyticalModel, visualPanel.Panel);
            }
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point point_Current = e.GetPosition(Viewport);

            Information.Text = string.Format("Mouse: X={0}, Y={1}", point_Current.X, point_Current.Y);
            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current) as RayMeshGeometry3DHitTestResult;
            if (rayMeshGeometry3DHitTestResult != null)
            {
                Information.Text += string.Format("\nElement: X={0}, Y={1}, Z={2}", rayMeshGeometry3DHitTestResult.PointHit.X, rayMeshGeometry3DHitTestResult.PointHit.Y, rayMeshGeometry3DHitTestResult.PointHit.Z);

                ModelVisual3D modelVisual3D_Selected_Current = rayMeshGeometry3DHitTestResult?.VisualHit as ModelVisual3D;
                if (modelVisual3D_Selected_Current != modelVisual3D_Selected && modelVisual3D_Selected is VisualPanel)
                {
                    (modelVisual3D_Selected as dynamic).SetHightinght(false);
                }

                modelVisual3D_Selected = modelVisual3D_Selected_Current;

                if (modelVisual3D_Selected is VisualPanel)
                {
                    (modelVisual3D_Selected as dynamic).SetHightinght(true);
                }
            }



            double dx = point_Current.X - from.X;
            double dy = point_Current.Y - from.Y;
            from = point_Current;

            var distance = dx * dx + dy * dy;
            if (distance <= 0)
                return;

            PerspectiveCamera perspectiveCamera = Viewport.Camera as PerspectiveCamera;
            if (perspectiveCamera == null)
            {
                return;
            }

            distance = distance / 10;

            Geometry.Spatial.Plane plane = Query.Plane(MainCamera);
            Geometry.Spatial.Vector3D vector3D = Geometry.Spatial.Query.Convert(plane, new Geometry.Planar.Vector2D(dx, dy));

            if (e.MouseDevice.MiddleButton is MouseButtonState.Pressed)
            {
                vector3D = vector3D.CrossProduct(plane.Normal);
                double angle = distance / perspectiveCamera.FieldOfView % 45;
                perspectiveCamera.Rotate(Convert.ToMedia3D(vector3D.GetNegated()), angle);
            }
            else if(e.MouseDevice.LeftButton is MouseButtonState.Pressed)
            {

                perspectiveCamera.Move(Convert.ToMedia3D(vector3D), 1 / 360D);
                //perspectiveCamera.Move(new Vector3D(0, -dy, dx), 1 / 360D);
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            PerspectiveCamera perspectiveCamera = Viewport.Camera as PerspectiveCamera;
            if (perspectiveCamera == null)
            {
                return;
            }

            perspectiveCamera.Move(e.Key).Rotate(e.Key);
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu_Grid.Items.Clear();

            Point point_Current = Mouse.GetPosition(Viewport);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current);
            ModelVisual3D modelVisual3D_Current = hitTestResult?.VisualHit as ModelVisual3D;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CenterView";
            menuItem.Header = "Center View";
            menuItem.Click += MenuItem_CenterView_Click;
            menuItem.Tag = hitTestResult;
            ContextMenu_Grid.Items.Add(menuItem);


            if (modelVisual3D_Current != null)
            {
                menuItem = new MenuItem();
                menuItem.Name = "MenuItem_Properties";
                menuItem.Header = "Properties";
                menuItem.Click += MenuItem_Properties_Click;
                menuItem.Tag = hitTestResult;
                ContextMenu_Grid.Items.Add(menuItem);
            }
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem == null)
            {
                return;
            }

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = menuItem.Tag as RayMeshGeometry3DHitTestResult;

            VisualPanel visualPanel = rayMeshGeometry3DHitTestResult?.VisualHit as VisualPanel;
            if(visualPanel == null)
            {
                return;
            }

            UI.Modify.EditPanel(uIAnalyticalModel, visualPanel.Panel);
        }

        private void MenuItem_CenterView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if(menuItem == null)
            {
                return;
            }

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = menuItem.Tag as RayMeshGeometry3DHitTestResult;

            ModelVisual3D modelVisual3D_Current = rayMeshGeometry3DHitTestResult?.VisualHit as ModelVisual3D;

            Point3D point3D = new Point3D(double.NaN, double.NaN, double.NaN);
            if (modelVisual3D_Current != null)
            {
                point3D = rayMeshGeometry3DHitTestResult.PointHit;
            }
            else
            {
                List<VisualPanel> visualPanels = VisualPanels;
                if(visualPanels == null)
                {
                    return;
                }

                Rect3D rect3D = Rect3D.Empty;
                foreach(VisualPanel visualPanel in visualPanels)
                {
                    Rect3D rect3D_VisualPanel = visualPanel.Content.Bounds;
                    if(rect3D == Rect3D.Empty)
                    {
                        rect3D = rect3D_VisualPanel;
                    }
                    else
                    {
                        rect3D.Union(rect3D_VisualPanel);
                    }
                }

                point3D = rect3D.Center();
            }

            if(point3D.IsNaN())
            {
                return;
            }

            Vector3D lookDirection = Query.LookDirection(point3D, MainCamera.Position, out Vector3D upDirection);
            MainCamera.LookDirection = lookDirection;
        }

        private void MainCamera_Changed(object sender, EventArgs e)
        {
            if(DirectionalLight != null)
            {
                DirectionalLight.Direction = MainCamera.LookDirection;
            }

        }
    }
}
