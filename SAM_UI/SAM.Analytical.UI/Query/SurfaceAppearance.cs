using SAM.Geometry.UI;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static SurfaceAppearance SurfaceAppearance(this Panel panel)
        {
            if (panel == null)
            {
                return null;
            }

            Color color = Analytical.Query.Color(panel);

            return new SurfaceAppearance(Core.UI.Convert.ToMedia(color), Core.UI.Convert.ToMedia(ControlPaint.Dark(color)), 0.001);
        }

        public static SurfaceAppearance SurfaceAppearance(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null)
            {
                return null;
            }

            Color color = Analytical.Query.Color(aperture.ApertureType, aperturePart);

            return new SurfaceAppearance(Core.UI.Convert.ToMedia(color), Core.UI.Convert.ToMedia(ControlPaint.Dark(color)), 0.001);
        }
    }
}