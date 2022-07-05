namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static System.Windows.Media.Media3D.Vector3D ToMedia3D(this Geometry.Spatial.Vector3D point3D)
        {
            if(point3D == null)
            {
                return new System.Windows.Media.Media3D.Vector3D(double.NaN, double.NaN, double.NaN);
            }

            return new System.Windows.Media.Media3D.Vector3D(point3D.X, point3D.Y, point3D.Z);
        }
    }
}
