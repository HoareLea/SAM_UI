using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public class VisualEdges : ModelVisual3D, IVisualSAMObject
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

        public virtual bool SetHighlight(bool highlight)
        {
            DiffuseMaterial diffuseMaterial = GeometryModel3D?.Material as DiffuseMaterial;
            if(diffuseMaterial == null)
            {
                return false;
            }

            SolidColorBrush solidColorBrush = diffuseMaterial.Brush as SolidColorBrush;
            if(solidColorBrush == null)
            {
                return false;
            }

            Color color = highlight ? Color.FromRgb(255, 0, 0) : Color.FromRgb(0, 0, 0);

            if (solidColorBrush.Color != color)
            {
                solidColorBrush.Color = color;
                return true;
            }

            return false;
        }
    }
}
