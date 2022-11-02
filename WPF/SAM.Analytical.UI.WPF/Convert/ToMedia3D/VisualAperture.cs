using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualAperture ToMedia3D(this Aperture aperture)
        {
            GeometryModel3D geometryModel3D = ToMedia3D_GeometryModel3D(aperture);
            if(geometryModel3D == null)
            {
                return null;
            }

            VisualAperture result = new VisualAperture(aperture);
            result.Content = geometryModel3D;

            //result.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 270));

            return result;
        }
    }
}
