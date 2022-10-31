using SAM.Geometry.Spatial;
using SAM.Geometry.UI.WPF;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualEdges ToMedia3D_VisualEdges(this Face3D face3D, System.Windows.Media.Color color, double thickness = Core.Tolerance.MacroDistance)
        {
            if(face3D == null)
            {
                return null;
            }

            VisualEdges result = new VisualEdges();

            Model3DGroup model3DGroup = new Model3DGroup();
            (face3D.GetExternalEdge3D() as ISegmentable3D).GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, thickness), Geometry.UI.Create.Material(color))));

            result.Content = model3DGroup;

            return result;
        }
    }
}
