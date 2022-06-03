using SAM.Core;
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

        public double Opacity
        {
            get
            {
                DiffuseMaterial diffuseMaterial = GeometryModel3D?.Material as DiffuseMaterial;
                if (diffuseMaterial == null)
                {
                    return double.NaN;
                }

                SolidColorBrush solidColorBrush = diffuseMaterial.Brush as SolidColorBrush;
                if (solidColorBrush == null)
                {
                    return double.NaN;
                }

                return solidColorBrush.Opacity;
            }

            set
            {
                DiffuseMaterial diffuseMaterial = GeometryModel3D?.Material as DiffuseMaterial;
                if (diffuseMaterial == null)
                {
                    return;
                }

                SolidColorBrush solidColorBrush = diffuseMaterial.Brush as SolidColorBrush;
                if (solidColorBrush == null)
                {
                    return;
                }

                if(solidColorBrush.Opacity != value)
                {
                    solidColorBrush.Opacity = value;
                }
            }
        }

        public virtual bool SetHighlight(bool highlight)
        {
            double opacity = highlight ? 0.70 : 1;
            if(Opacity != opacity)
            {
                Opacity = opacity;
                return true;
            }

            return false;
        }

        public virtual bool Similar(IJSAMObject jSAMObject)
        {
            if(jSAMObject == null)
            {
                return false;
            }

            System.Type type = this.jSAMObject?.GetType();
            if(type == null)
            {
                return false;
            }

            if(!type.Equals(jSAMObject.GetType()))
            {
                return false;
            }

            if(!(jSAMObject is SAMObject))
            {
                return true;
            }

            return ((SAMObject)jSAMObject).Guid == ((SAMObject)(object)this.jSAMObject).Guid;
        }
    }
}
