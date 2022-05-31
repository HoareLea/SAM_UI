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
        private Point point = new Point(-1, -1);
        private AnalyticalModel analyticalModel;
        private ModelVisual3D modelVisual3D_Selected;

        private Point from;

        public View3DControl()
        {
            InitializeComponent();
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            Clear();

            List<Panel> panels = analyticalModel.GetPanels();
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

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                if (analyticalModel == null)
                {
                    return null;
                }

                return new AnalyticalModel(analyticalModel);
            }

            set
            {
                analyticalModel = value == null ? null : new AnalyticalModel(value);
                LoadAnalyticalModel(analyticalModel);
            }
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            Point point_Current = e.GetPosition(this);

            //if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            //{
            //    if (point.X == -1 && point.Y == -1)
            //    {
            //        point = e.GetPosition(this);
            //    }

            //    MainCamera.LookDirection = new Vector3D(MainCamera.LookDirection.X - ((-point.X + point_Current.X) / 360D), MainCamera.LookDirection.Y, MainCamera.LookDirection.Z - ((point.Y - point_Current.Y) / 360D));
            //}
            //else if (e.RightButton == System.Windows.Input.MouseButtonState.Released)
            //{
            //    point = new Point(-1, -1);
            //}

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(Viewport, point_Current);
            ModelVisual3D modelVisual3D_Selected_Current = hitTestResult?.VisualHit as ModelVisual3D;
            if(modelVisual3D_Selected_Current != modelVisual3D_Selected && modelVisual3D_Selected is VisualPanel)
            {
                (modelVisual3D_Selected as dynamic).SetHightinght(false);
            }

            modelVisual3D_Selected = modelVisual3D_Selected_Current;

            if (modelVisual3D_Selected is VisualPanel)
            {
                (modelVisual3D_Selected as dynamic).SetHightinght(true);
            }
        }

        private void UserControl_MouseWeel(object sender, MouseWheelEventArgs e)
        {
            MainCamera.Position = new Point3D(MainCamera.Position.X - e.Delta / 360D, MainCamera.Position.Y - e.Delta / 360D, MainCamera.Position.Z - e.Delta / 360D);
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.MiddleButton == MouseButtonState.Pressed)
            {

            }
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            PerspectiveCamera perspectiveCamera = Viewport.Camera as PerspectiveCamera;
            if(perspectiveCamera == null)
            {
                return;
            }

            Point till = e.GetPosition(sender as IInputElement);
            double dx = till.X - from.X;
            double dy = till.Y - from.Y;
            from = till;

            var distance = dx * dx + dy * dy;
            if (distance <= 0)
                return;

            if (e.MouseDevice.RightButton is MouseButtonState.Pressed)
            {
                double angle = distance / perspectiveCamera.FieldOfView % 45;
                perspectiveCamera.Rotate(new Vector3D(0d, dx, -dy), angle);
            }
            else if(e.MouseDevice.LeftButton is MouseButtonState.Pressed)
            {
                perspectiveCamera.Move(new Vector3D(0d, dy, dx), 1 / 360D);
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
    }
}
