using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static Spatial.Plane Plane(this ProjectionCamera projectionCamera)
        {
            if(projectionCamera == null)
            {
                return null;
            }

            if (Core.UI.WPF.Query.IsNaN(projectionCamera.Position) || Core.UI.WPF.Query.IsNaN(projectionCamera.UpDirection) || Core.UI.WPF.Query.IsNaN(projectionCamera.LookDirection))
            {
                return null;
            }

            Spatial.Vector3D upDirection = Convert.ToSAM(projectionCamera.UpDirection);
            Spatial.Vector3D lookDirection = Convert.ToSAM(projectionCamera.LookDirection);

            //Geometry.Spatial.Plane result = new Geometry.Spatial.Plane(Convert.ToSAM(perspectiveCamera.Position), -lookDirection.CrossProduct(upDirection), upDirection);

            Spatial.Plane result = new Spatial.Plane(Convert.ToSAM(projectionCamera.Position), lookDirection);

            upDirection = Spatial.Query.Project(result, upDirection);

            result = new Spatial.Plane(Convert.ToSAM(projectionCamera.Position), -lookDirection.CrossProduct(upDirection), upDirection);

            return result;
        }
    }
}
