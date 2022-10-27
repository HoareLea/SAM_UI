using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class VisualBackground : ModelVisual3D
    {
        public VisualBackground()
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
