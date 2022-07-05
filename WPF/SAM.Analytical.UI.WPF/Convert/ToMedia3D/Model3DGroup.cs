using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static Model3DGroup ToMedia3D_Model3DGroup(this Shell shell, Color color, bool doubleSided = false)
        {
            List<Face3D> face3Ds = shell?.Face3Ds;
            if (face3Ds == null)
            {
                return null;
            }

            Model3DGroup result = new Model3DGroup();
            foreach (Face3D face3D in face3Ds)
            {
                MeshGeometry3D meshGeometry3D = face3D?.ToMedia3D(doubleSided);
                if (meshGeometry3D == null)
                {
                    continue;
                }

                //new EmissiveMaterial(Brushes.Transparent)

                result.Children.Add(new GeometryModel3D(meshGeometry3D, Query.Material(color)));
            }

            return result;
        }
    }
}
