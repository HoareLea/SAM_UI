using SAM.Core.UI.WPF;
using SAM.Geometry.UI.WPF;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public class VisualAperture : VisualJSAMObject<Aperture>, IVisualFace3DObject
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
            if (Children.Count != 0)
            {
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    if (Children[i] is VisualEdges)
                    {
                        Children.RemoveAt(i);
                    }
                }
            }

            if (highlight)
            {
                VisualEdges visualEdges = jSAMObject?.Face3D?.ToMedia3D_VisualEdges(Color.FromRgb(0, 0, 255), 0.01);

                if (visualEdges != null)
                {
                    Children.Add(visualEdges);
                }
            }

            double opacity = highlight ? 0.4 : 0.6;
            if (Opacity != opacity)
            {
                Opacity = opacity;
                return true;
            }

            return false;
        }
    }
}
