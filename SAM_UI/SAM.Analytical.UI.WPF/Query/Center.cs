using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Point3D Center(this Rect3D rect3D)
        {
            if(rect3D == null)
            {
                return new Point3D(double.NaN, double.NaN, double.NaN);
            }

            return new Point3D((rect3D.Location.X + rect3D.SizeX) / 2, (rect3D.Location.Y + rect3D.SizeY) / 2, (rect3D.Location.Z + rect3D.SizeZ) / 2);
        }
    }
}
