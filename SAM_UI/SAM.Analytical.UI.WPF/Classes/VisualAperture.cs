using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public class VisualAperture : VisualSAMObject<Aperture>
    {
        public VisualAperture(Aperture aperture)
            :base(aperture)
        {

        }

        public Aperture Aperture
        {
            get
            {
                return jSAMObject;
            }
        }

        public override bool SetHighlight(bool highlight)
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

            double opacity = highlight ? 0.4 : 0.6;

            if (solidColorBrush.Opacity != opacity)
            {
                solidColorBrush.Opacity = opacity;
                return true;
            }

            return false;
        }
    }
}
