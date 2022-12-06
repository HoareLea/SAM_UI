using SAM.Geometry.UI;
using System.Drawing;
using System.Linq;
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

            return new SurfaceAppearance(Core.UI.Convert.ToMedia(color), Core.UI.Convert.ToMedia(ControlPaint.Dark(color)), 0);
        }

        public static SurfaceAppearance SurfaceAppearance(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null)
            {
                return null;
            }

            Color color = Analytical.Query.Color(aperture.ApertureType, aperturePart);

            SurfaceAppearance result = new SurfaceAppearance(Core.UI.Convert.ToMedia(color), Core.UI.Convert.ToMedia(ControlPaint.Dark(color)), 0);
            if(aperturePart == AperturePart.Pane)
            {
                result.Opacity = 0.6;
            }

            return result;
        }

        public static SurfaceAppearance SurfaceAppearance(this Space space)
        {
            if (space == null)
            {
                return null;
            }


            if(!space.TryGetValue(SpaceParameter.Color, out Color color))
            {
                color = System.Drawing.Color.Empty;
                InternalCondition internalCondition = space.InternalCondition;
                if(internalCondition == null || !internalCondition.TryGetValue(InternalConditionParameter.Color, out color))
                {
                    color = System.Drawing.Color.Empty;
                }
            }

            if(color.IsEmpty)
            {
                color = System.Drawing.Color.FromKnownColor(KnownColor.LightGray);
            }

            return new SurfaceAppearance(Core.UI.Convert.ToMedia(color), Core.UI.Convert.ToMedia(ControlPaint.Dark(color)), 0);
        }

        public static SurfaceAppearance SurfaceAppearance(this Panel panel, ViewSettings viewSettings)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(panel)?.FirstOrDefault();
            if (result == null)
            {
                result = SurfaceAppearance(panel);
            }

            return result;
        }

        public static SurfaceAppearance SurfaceAppearance(this Space space, ViewSettings viewSettings)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(space)?.FirstOrDefault();
            if (result == null)
            {
                result = SurfaceAppearance(space);
            }

            return result;
        }

        public static SurfaceAppearance SurfaceAppearance(this Space space, ViewSettings viewSettings, SurfaceAppearance surfaceAppearance)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(space)?.FirstOrDefault();
            if(result == null)
            {
                result = surfaceAppearance;
            }

            if (result == null)
            {
                result = SurfaceAppearance(space);
            }

            return result;
        }

        public static SurfaceAppearance SurfaceAppearance(this Aperture aperture, AperturePart aperturePart, ViewSettings viewSettings)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(aperture)?.FirstOrDefault();
            if (result == null)
            {
                result = SurfaceAppearance(aperture, aperturePart);
            }

            return result;
        }
    }
}