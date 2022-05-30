using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for View3DForm.xaml
    /// </summary>
    public partial class View3DControl : UserControl
    {
        private AnalyticalModel analyticalModel;

        public View3DControl()
        {
            InitializeComponent();
        }

        private void LoadAnalyticalModel(AnalyticalModel analyticalModel)
        {
            Visual3DCollection visual3DCollection = Viewport.Children;
            if(visual3DCollection == null)
            {
                return;
            }

            foreach (object @object in visual3DCollection)
            {
                if(!(@object is ModelVisual3D))
                {
                    continue;
                }

                ModelVisual3D modelVisual3D = (ModelVisual3D)@object;
                if(modelVisual3D.Content is DirectionalLight)
                {
                    continue;
                }

                modelVisual3D.Update(analyticalModel);
                break;
            }
        }

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                if(analyticalModel == null)
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

        private void View3DControl_MouseWeel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            MainCamera.Position = new Point3D(MainCamera.Position.X - e.Delta / 360D, MainCamera.Position.Y - e.Delta / 360D, MainCamera.Position.Z - e.Delta / 360D);
        }
    }
}
