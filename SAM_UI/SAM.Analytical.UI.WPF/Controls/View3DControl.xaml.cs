using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for View3DForm.xaml
    /// </summary>
    public partial class View3DControl : UserControl
    {
        //private System.Windows.Point point = new System.Windows.Point(-1, -1);
        private AnalyticalModel analyticalModel;

        public View3DControl()
        {
            InitializeComponent();
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            ModelVisual3D?.Update(analyticalModel);
        }

        public ModelVisual3D ModelVisual3D
        {
            get
            {
                Visual3DCollection visual3DCollection = Viewport.Children;
                if (visual3DCollection == null)
                {
                    return null;
                }

                foreach (object @object in visual3DCollection)
                {
                    if (!(@object is ModelVisual3D))
                    {
                        continue;
                    }

                    ModelVisual3D modelVisual3D = (ModelVisual3D)@object;
                    if (modelVisual3D.Content is DirectionalLight)
                    {
                        continue;
                    }


                    return modelVisual3D;
                }

                return null;
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

        private void UserControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            //{
            //    if(point.X == -1 && point.Y == -1)
            //    {
            //        point = e.GetPosition(this);
            //    }

            //    System.Windows.Point point_Current = e.GetPosition(this);

            //    MainCamera.Position = new Point3D(MainCamera.Position.X - ((-point.X + point_Current.X) / 100), MainCamera.Position.Y, MainCamera.Position.Z - ((point.Y - point_Current.Y) / 100));
            //}
            //else if(e.RightButton == System.Windows.Input.MouseButtonState.Released)
            //{
            //    point = new System.Windows.Point(-1, -1);
            //}
        }

        private void UserControl_MouseWeel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            MainCamera.Position = new Point3D(MainCamera.Position.X - e.Delta / 360D, MainCamera.Position.Y - e.Delta / 360D, MainCamera.Position.Z - e.Delta / 360D);
        }

        private void UserControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
            {

            }
        }
    }
}
