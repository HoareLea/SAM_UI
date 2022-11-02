namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static System.Windows.Media.Media3D.Point3D ToMedia3D(this Spatial.Point3D point3D)
        {
            if(point3D == null)
            {
                return new System.Windows.Media.Media3D.Point3D(double.NaN, double.NaN, double.NaN);
            }

            return new System.Windows.Media.Media3D.Point3D(-point3D.X, point3D.Z, point3D.Y);
        }
    }
}
