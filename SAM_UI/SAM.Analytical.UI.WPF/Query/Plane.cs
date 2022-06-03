using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Geometry.Spatial.Plane Plane(this PerspectiveCamera perspectiveCamera)
        {
            Geometry.Spatial.Vector3D upDirection = Convert.ToSAM(perspectiveCamera.UpDirection);
            Geometry.Spatial.Vector3D lookDirection = Convert.ToSAM(perspectiveCamera.LookDirection);

            //Geometry.Spatial.Plane result = new Geometry.Spatial.Plane(Convert.ToSAM(perspectiveCamera.Position), -lookDirection.CrossProduct(upDirection), upDirection);

            Geometry.Spatial.Plane result = new Geometry.Spatial.Plane(Convert.ToSAM(perspectiveCamera.Position), lookDirection);

            upDirection = Geometry.Spatial.Query.Project(result, upDirection);

            result = new Geometry.Spatial.Plane(Convert.ToSAM(perspectiveCamera.Position), -lookDirection.CrossProduct(upDirection), upDirection);

            return result;
        }
    }
}
