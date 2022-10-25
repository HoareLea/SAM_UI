using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualWidget ToMedia3D_VisualWidget(this Geometry.Spatial.Point3D point3D, System.Windows.Media.Color color, double radious = 0.1)
        {
            if(point3D == null)
            {
                return null;
            }

            Sphere sphere = new Sphere(point3D, radious);

            VisualWidget result = new VisualWidget();
            result.Content = new GeometryModel3D(Geometry.UI.WPF.Convert.ToMedia3D(sphere, false), Geometry.UI.WPF.Query.Material(color));

            return result;
        }
    }
}
