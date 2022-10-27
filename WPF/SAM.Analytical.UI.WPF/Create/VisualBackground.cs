using SAM.Geometry.Spatial;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Create
    {
        public static VisualBackground VisualBackground(this Viewport3D viewport3D, double scale = 1000)
        {
            if(viewport3D == null)
            {
                return null;
            }

            PerspectiveCamera perspectiveCamera = viewport3D.Camera as PerspectiveCamera;
            Plane plane = Geometry.UI.WPF.Query.Plane(perspectiveCamera);
            if(plane == null)
            {
                return null;
            }

            Geometry.Spatial.Vector3D vector3D = plane.Normal;
            vector3D.Scale(scale);

            plane = plane.GetMoved(vector3D) as Plane;

            Geometry.Planar.Rectangle2D rectangle2D = new Geometry.Planar.Rectangle2D(new Geometry.Planar.Point2D(-(scale / 2), -(scale / 2)), scale, scale);

            Rectangle3D rectangle3D = Geometry.Spatial.Query.Convert(plane, rectangle2D);

            MeshGeometry3D meshGeometry3D = Geometry.UI.WPF.Convert.ToMedia3D(new Face3D(rectangle3D), false);
            if(meshGeometry3D == null)
            {
                return null;
            }

            VisualBackground result = new VisualBackground();
            //Query.Material(Color.FromRgb(0, 255, 0))
            result.Content = new GeometryModel3D(meshGeometry3D, Core.UI.WPF.Query.TransaprentMaterial());

            return result;
        }
    }
}
