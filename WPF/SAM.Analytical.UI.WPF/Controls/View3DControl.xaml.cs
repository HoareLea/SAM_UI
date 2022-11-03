using SAM.Core;
using SAM.Core.UI.WPF;
using SAM.Geometry.UI.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private VisualBackground visualBackground;

        private IVisualJSAMObject visualSAMObject_Highlight;

        private Point point_Current;

        private bool show = false;

        public View3DControl()
        {
            InitializeComponent();
        }

        public List<IJSAMObject> Show<T>(IEnumerable<T> jSAMObjects) where T: IJSAMObject
        {
            show = true;
            
            List<IVisualFace3DObject> visualFace3DObjects = GetVisualSAMObjects<IVisualFace3DObject>();
            if(visualFace3DObjects == null || visualFace3DObjects.Count == 0)
            {
                return null;
            }

            bool hightlight = jSAMObjects != null && jSAMObjects.Count() != 0;
            foreach (IVisualFace3DObject visualFace3DObject in visualFace3DObjects)
            {
                if(visualFace3DObject == null)
                {
                    continue;
                }

                visualFace3DObject.Opacity = 0.1;
            }

            if(!hightlight)
            {
                return null;
            }

            List<IJSAMObject> result = new List<IJSAMObject>();
            foreach (T jSAMObject in jSAMObjects)
            {
                List<IJSAMObject> jSAMObjects_Temp = new List<IJSAMObject>();
                if(jSAMObject is Space)
                {
                    List<Panel> panels = uIAnalyticalModel.JSAMObject.AdjacencyCluster?.GetPanels((Space)(object)jSAMObject);
                    if(panels != null)
                    {
                        foreach(Panel panel in panels)
                        {
                            jSAMObjects_Temp.Add(panel);
                            panel.Apertures?.ForEach(x => jSAMObjects_Temp.Add(x));
                        }
                    }
                }
                else if(jSAMObject is Panel)
                {
                    jSAMObjects_Temp.Add(jSAMObject);
                    ((Panel)(object)jSAMObject).Apertures?.ForEach(x => jSAMObjects_Temp.Add(x));
                }
                else
                {
                    jSAMObjects_Temp.Add(jSAMObject);
                }

                foreach(IJSAMObject jSAMObject_Temp in jSAMObjects_Temp)
                {
                    IVisualFace3DObject visualFace3DObject = visualFace3DObjects.Find(x => x.Similar(jSAMObject_Temp));
                    if (visualFace3DObject == null)
                    {
                        continue;
                    }

                    visualFace3DObject.Opacity = 1;
                    result.Add(jSAMObject_Temp);
                }
            }

            return result;
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            if(analyticalModel == null)
            {
                MessageBox.Show("Could not load Analytical Model.", "Load Analytical Model");
                return;
            }

            Transform3D tranform3D = Transform3D.Identity;

            VisualAnalyticalModel visualAnalyticalModel = Core.UI.WPF.Query.VisualJSAMObjects<VisualAnalyticalModel>(Viewport)?.FirstOrDefault();
            if(visualAnalyticalModel != null)
            {
                tranform3D = visualAnalyticalModel.Transform;
            }

            Core.UI.WPF.Modify.Clear<IVisualJSAMObject>(Viewport);

            visualAnalyticalModel = Convert.ToMedia3D(analyticalModel);
            visualAnalyticalModel.Transform = tranform3D;

            if(visualAnalyticalModel != null)
            {
                Viewport.Children.Add(visualAnalyticalModel);
            }
        }

        public List<T> GetVisualSAMObjects<T>() where T : IVisualJSAMObject
        {
            return Core.UI.WPF.Query.VisualJSAMObjects<T>(Viewport);
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
            Point point_Current_Temp = e.GetPosition(Viewport);

            Geometry.Spatial.Vector3D vector3D = null;

            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult<ModelVisual3D>(Viewport, point_Current_Temp);// VisualTreeHelper.HitTest(Viewport, point_Current_Temp) as RayMeshGeometry3DHitTestResult;
            if(rayMeshGeometry3DHitTestResult != null)
            {
                vector3D = new Geometry.Spatial.Vector3D(MainCamera.Position.ToSAM(), rayMeshGeometry3DHitTestResult.PointHit.ToSAM());
            }
            else
            {
                vector3D = new Geometry.Spatial.Vector3D(MainCamera.Position.X, MainCamera.Position.Y, MainCamera.Position.Z);
                vector3D.Negate();
            }

            if(vector3D == null)
            {
                return;
            }

            double factor = e.Delta / 100;//* vector3D.Length;

            vector3D.Normalize();
            vector3D.Scale(factor);

            MainCamera.Position = Geometry.UI.WPF.Convert.ToMedia3D(MainCamera.Position.ToSAM().GetMoved(vector3D) as Geometry.Spatial.Point3D);
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(Viewport);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult<IVisualJSAMObject>(Viewport, point_Current);// VisualTreeHelper.HitTest(Viewport, point_Current) as RayMeshGeometry3DHitTestResult;

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
            Point point_Current_Temp = e.GetPosition(Viewport);

            visualSAMObject_Highlight?.SetHighlight(false);
            visualSAMObject_Highlight = null;

            List<IVisualFace3DObject> visualFace3DObjects = null;

            if (show)
            {
                visualFace3DObjects = GetVisualSAMObjects<IVisualFace3DObject>();
                visualFace3DObjects?.ForEach(x => x.Opacity = 1);
                show = false;
            }

            Information.Text = string.Format("Mouse: X={0}, Y={1}", Core.Query.Round(point_Current_Temp.X, 0.1), Core.Query.Round(point_Current_Temp.Y, 0.1));
            RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult(Viewport, point_Current_Temp, out IVisualJSAMObject visualSAMObject_Highlight_Current); //VisualTreeHelper.HitTest(Viewport, point_Current_Temp) as RayMeshGeometry3DHitTestResult;
            if (rayMeshGeometry3DHitTestResult != null && visualSAMObject_Highlight_Current != null)
            {
                if(visualSAMObject_Highlight_Current != null)
                {
                    visualSAMObject_Highlight_Current.SetHighlight(true);
                    visualSAMObject_Highlight = visualSAMObject_Highlight_Current;

                    if (visualSAMObject_Highlight_Current is VisualPanel)
                    {
                        VisualPanel visualPanel = (VisualPanel)visualSAMObject_Highlight_Current;
                        Information.Text += string.Format("\n{0} Panel Guid: {1}\nPoint: X={2}, Y={3}, Z={4}", visualPanel?.Panel?.Name, visualPanel?.Panel?.Guid, Core.Query.Round(rayMeshGeometry3DHitTestResult.PointHit.X, 0.01), Core.Query.Round(rayMeshGeometry3DHitTestResult.PointHit.Y, 0.01), Core.Query.Round(rayMeshGeometry3DHitTestResult.PointHit.Z, 0.01));
                    }
                }
            }

            double dx = point_Current_Temp.X - point_Current.X;
            double dy = point_Current_Temp.Y - point_Current.Y;
            point_Current = point_Current_Temp;

            if (e.MouseDevice.MiddleButton is MouseButtonState.Pressed || e.MouseDevice.LeftButton is MouseButtonState.Pressed)
            {
                double distance = dx * dx + dy * dy;
                if (distance <= 0)
                    return;

                PerspectiveCamera perspectiveCamera = Viewport.Camera as PerspectiveCamera;
                if (perspectiveCamera == null)
                {
                    return;
                }

                Geometry.Planar.Vector2D vector2D = new Geometry.Planar.Vector2D(dx, dy);

                if (visualFace3DObjects == null)
                {
                    visualFace3DObjects = GetVisualSAMObjects<IVisualFace3DObject>();
                }

                if(visualFace3DObjects == null)
                {
                    return;
                }

                Rect3D rect3D = Query.Bounds(GetVisualSAMObjects<VisualAnalyticalModel>());
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
                    Geometry.Spatial.Plane plane = Geometry.UI.WPF.Query.Plane(MainCamera);

                    double angle = distance / perspectiveCamera.FieldOfView % 45;
                    Geometry.Spatial.Vector3D vector3D = Geometry.Spatial.Query.Convert(plane, vector2D);
                    vector3D = vector3D.CrossProduct(plane.Normal).GetNegated();
                    vector3D = new Geometry.Spatial.Vector3D(0, vector3D.Y, 0);

                    GetVisualSAMObjects<VisualAnalyticalModel>()?.ForEach(x => Geometry.UI.WPF.Modify.Rotate(x, vector3D, center.ToSAM(), angle));

                    //Version 2
                    //perspectiveCamera.Rotate(vector2D, center.ToSAM());

                    //Version 1
                    //vector3D = vector3D.CrossProduct(plane.Normal);
                    //double angle = distance / perspectiveCamera.FieldOfView % 45;
                    //perspectiveCamera.Rotate(Convert.ToMedia3D(vector3D), angle);
                }
                else if (e.MouseDevice.LeftButton is MouseButtonState.Pressed)
                {
                    double factor = MainCamera.Position.ToSAM().Distance(center.ToSAM()) / 10;
                    vector2D.Scale(factor);

                    Geometry.Spatial.Plane plane = Geometry.UI.WPF.Query.Plane(MainCamera);
                    Geometry.Spatial.Vector3D vector3D = Geometry.Spatial.Query.Convert(plane, vector2D);

                    perspectiveCamera.Move(Geometry.UI.WPF.Convert.ToMedia3D(vector3D), 0.01);
                }
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            PerspectiveCamera perspectiveCamera = Viewport.Camera as PerspectiveCamera;
            if (perspectiveCamera == null)
            {
                return;
            }

            perspectiveCamera.Move(e.Key);
            perspectiveCamera.Rotate(e.Key);
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu_Grid.Items.Clear();

            Point point_Current = Mouse.GetPosition(Viewport);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current);
            IVisualFace3DObject visualFace3DObject_Current = hitTestResult?.VisualHit as IVisualFace3DObject;

            MenuItem menuItem = new MenuItem();
            menuItem.Name = "MenuItem_CenterView";
            menuItem.Header = "Center View";
            menuItem.Click += MenuItem_CenterView_Click;
            menuItem.Tag = hitTestResult;
            ContextMenu_Grid.Items.Add(menuItem);


            if (visualFace3DObject_Current != null)
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
            CenterView();
        }

        private void MainCamera_Changed(object sender, EventArgs e)
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.Direction = MainCamera.LookDirection;
            }

            if (visualBackground != null)
            {
                Viewport.Children.Remove(visualBackground);
            }

            visualBackground = Geometry.UI.WPF.Create.VisualBackground(Viewport);
            if(visualBackground == null)
            {
                return;
            }

            Viewport.Children.Add(visualBackground);
        }

        public void CenterView()
        {
            List<VisualAnalyticalModel> visualAnalyticalModels = GetVisualSAMObjects<VisualAnalyticalModel>();
            if (visualAnalyticalModels == null)
            {
                return;
            }

            Rect3D rect3D = Query.Bounds(visualAnalyticalModels);

            HelixToolkit.Wpf.CameraHelper.ZoomExtents(MainCamera, Viewport, rect3D);
        }
    }
}
