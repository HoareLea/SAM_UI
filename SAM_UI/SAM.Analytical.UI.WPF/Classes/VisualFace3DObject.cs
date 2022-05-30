using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public class VisualPanel : ModelVisual3D
    {
        private Panel panel;

        public VisualPanel(Panel panel)
        {
            this.panel = panel;
        }

        public Panel Panel
        {
            get
            {
                return panel;
            }
        }

        public GeometryModel3D GeometryModel3D
        {
            get
            {
                return Content as GeometryModel3D;
            }
        }

        public bool SetHightinght(bool highlight)
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

            double opacity = highlight ? 0.5 : 1;

            if (solidColorBrush.Opacity != opacity)
            {
                solidColorBrush.Opacity = opacity;
                return true;
            }

            return false;
            //Color color = Analytical.Query.Color(panel).ToMedia();
            //if(highlight)
            //{
            //    SAM.Core.Windows.Query.Lerp();
            //}

            //if(solidColorBrush.Color != color)
            //{
            //    solidColorBrush.Color = color;
            //}
        }
    }
}
