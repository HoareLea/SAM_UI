namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static Geometry.Spatial.Vector3D ToSAM(this System.Windows.Media.Media3D.Vector3D vector3D)
        {
            if(vector3D == null)
            {
                return new Geometry.Spatial.Point3D(double.NaN, double.NaN, double.NaN);
            }

            return new Geometry.Spatial.Point3D(vector3D.X, vector3D.Y, vector3D.Z);
        }
    }
}
