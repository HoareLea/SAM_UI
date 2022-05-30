using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static GeometryModel3D ToMedia3D(this Panel panel)
        {
            MeshGeometry3D meshGeometry3D = ToMedia3D(panel?.Face3D);
            if(meshGeometry3D == null)
            {
                return null;
            }

            Material material =  Query.Material(panel);

            GeometryModel3D result = new GeometryModel3D(meshGeometry3D, material);

            return result;
        }
    }
}
