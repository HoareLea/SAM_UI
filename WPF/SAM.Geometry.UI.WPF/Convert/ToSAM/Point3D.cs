namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static Spatial.Point3D ToSAM(this System.Windows.Media.Media3D.Point3D point3D)
        {
            if(point3D == null)
            {
                return new Spatial.Point3D(double.NaN, double.NaN, double.NaN);
            }

            return new Spatial.Point3D(-point3D.X, point3D.Z, point3D.Y);
        }
    }
}
