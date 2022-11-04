﻿using SAM.Core.UI.WPF;
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

        private Mode mode = Mode.ThreeDimensional;
        
        private UIGeometryObjectModel uIGeometryObjectModel;

        private IVisualJSAMObject visualSAMObject_Highlight;

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
            ContextMenu_Grid.Items.Clear();

            Point point_Current = Mouse.GetPosition(viewport3D);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(viewport3D, point_Current);
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
            ChangeCamera();
        }

        private void ChangeCamera()
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.Direction = ProjectionCamera.LookDirection;
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

        //public Viewport3D Viewport3D
        //{
        //    get
        //    {
        //        return Viewport;
        //    }
        //}

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

            projectionCamera.Changed += Camera_Changed;
            viewport3D.Camera = projectionCamera;
            ChangeCamera();
        }

        public ProjectionCamera ProjectionCamera
        {
            get
            {
                return viewport3D.Camera as ProjectionCamera;
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point point_Current = e.GetPosition(viewport3D);

                RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult = Core.UI.WPF.Query.RayMeshGeometry3DHitTestResult<IVisualJSAMObject>(viewport3D, point_Current, out IVisualJSAMObject visualJSAMObject);
                if (rayMeshGeometry3DHitTestResult != null && visualJSAMObject != null)
                {
                    ObjectDoubleClicked?.Invoke(this, new ObjectDoubleClickedEventArgs(e, visualJSAMObject));
                }
            }
        }

        private void Viewport_PreviewMouseMove(object sender, MouseEventArgs e)
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
        }

        private void Viewport_Loaded(object sender, RoutedEventArgs e)
        {
            Information.Text = string.Empty;
            //CenterView();
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


    }
}
