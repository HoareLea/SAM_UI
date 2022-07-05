using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
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
