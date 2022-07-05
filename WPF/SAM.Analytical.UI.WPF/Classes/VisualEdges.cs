using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public class VisualEdges : ModelVisual3D
    {
        public VisualEdges()
        {

        }

        public GeometryModel3D GeometryModel3D
        {
            get
            {
                return Content as GeometryModel3D;
            }
        }
    }
}
