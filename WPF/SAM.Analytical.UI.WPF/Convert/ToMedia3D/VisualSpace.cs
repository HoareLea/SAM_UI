using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Convert
    {
        public static VisualSpace ToMedia3D(this Space space, AdjacencyCluster adjacencyCluster)
        {
            Model3DGroup model3DGroup = Geometry.UI.WPF.Convert.ToMedia3D_Model3DGroup(adjacencyCluster?.Shell(space), Color.FromRgb(100, 100, 100), true);
            if(model3DGroup == null)
            {
                return null;
            }

            VisualSpace result = new VisualSpace(space);
            result.Content = model3DGroup;

            result.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 270));

            return result;
        }
    }
}
