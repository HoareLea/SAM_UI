using SAM.Core;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public class VisualSAMObject<T> : ModelVisual3D, IVisualSAMObject where T : IJSAMObject
    {
        protected T jSAMObject;

        public VisualSAMObject(T jSAMObject)
        {
            this.jSAMObject = jSAMObject;
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
            if(Children.Count != 0)
            {
                for(int i = Children.Count - 1; i >= 0; i--)
                {
                    if(Children[i] is VisualEdges)
                    {
                        Children.RemoveAt(i);
                    }
                }
            }

            if(highlight && jSAMObject is Panel)
            {
                Children.Add(((Panel)(object)jSAMObject).GetFace3D(true).ToMedia3D_VisualEdges(Color.FromRgb(0, 0, 255), 0.01));
            }

            DiffuseMaterial diffuseMaterial = GeometryModel3D?.Material as DiffuseMaterial;
            if (diffuseMaterial == null)
            {
                return false;
            }

            SolidColorBrush solidColorBrush = diffuseMaterial.Brush as SolidColorBrush;
            if (solidColorBrush == null)
            {
                return false;
            }

            double opacity = highlight ? 0.90 : 1;

            if (solidColorBrush.Opacity != opacity)
            {
                solidColorBrush.Opacity = opacity;
                return true;
            }

            return false;
        }
    }
}
