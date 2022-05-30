using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualPanel ToMedia3D(this Panel panel)
        {
            GeometryModel3D geometryModel3D = ToMedia3D_GeometryModel3D(panel);
            if(geometryModel3D == null)
            {
                return null;
            }

            VisualPanel result = new VisualPanel(panel);
            result.Content = geometryModel3D;

            result.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 270));

            return result;
        }
    }
}
