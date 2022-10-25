using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static bool IsNaN(this Point3D point3D)
        {
            return double.IsNaN(point3D.X) || double.IsNaN(point3D.Y) || double.IsNaN(point3D.Z);
        }

        public static bool IsNaN(this System.Windows.Point point)
        {
            return double.IsNaN(point.X) || double.IsNaN(point.Y);
        }
    }
}
