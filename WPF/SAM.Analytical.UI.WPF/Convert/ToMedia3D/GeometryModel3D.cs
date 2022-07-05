using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static GeometryModel3D ToMedia3D_GeometryModel3D(this Panel panel)
        {
            Face3D face3D = panel?.GetFace3D(true);

            MeshGeometry3D meshGeometry3D = ToMedia3D(face3D, false);
            if(meshGeometry3D == null)
            {
                return null;
            }

            Material material =  Query.Material(panel);

            GeometryModel3D result = new GeometryModel3D(meshGeometry3D, material);

            return result;
        }

        public static GeometryModel3D ToMedia3D_GeometryModel3D(this Aperture aperture)
        {
            Face3D face3D = aperture?.Face3D;

            MeshGeometry3D meshGeometry3D = ToMedia3D(face3D, false);
            if (meshGeometry3D == null)
            {
                return null;
            }

            Material material = Query.Material(aperture);

            GeometryModel3D result = new GeometryModel3D(meshGeometry3D, material);

            return result;
        }
    }
}
