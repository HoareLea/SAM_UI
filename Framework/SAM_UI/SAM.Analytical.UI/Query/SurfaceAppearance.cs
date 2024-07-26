using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System.Collections.Generic;
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

            //thickness 0.001
            return new SurfaceAppearance(color, ControlPaint.Dark(color), 0.01);
        }

        public static SurfaceAppearance SurfaceAppearance(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null)
            {
                return null;
            }

            Color color = Analytical.Query.Color(aperture, aperturePart);

            SurfaceAppearance result = new SurfaceAppearance(color, ControlPaint.Dark(color), 0);
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

            return new SurfaceAppearance(color, ControlPaint.Dark(color), 0);
        }

        public static SurfaceAppearance SurfaceAppearance(this Panel panel, ViewSettings viewSettings, System.Windows.Media.Color? color)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(panel)?.FirstOrDefault();
            if (result == null && color != null)
            {
                result = new SurfaceAppearance(color.Value.ToDrawing(), ControlPaint.Dark(color.Value.ToDrawing()), 0.01);
            }

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

        public static SurfaceAppearance SurfaceAppearance(this Space space, ViewSettings viewSettings, System.Windows.Media.Color? color)
        {
            SurfaceAppearance result = viewSettings.GetAppearances<SurfaceAppearance>(space)?.FirstOrDefault();
            if (result == null && color != null)
            {
                result = new SurfaceAppearance(color.Value.ToDrawing(), ControlPaint.Dark(color.Value.ToDrawing()), 0.02);
            }

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

        public static SurfaceAppearance SurfaceAppearance(this Aperture aperture, AperturePart aperturePart, ViewSettings viewSettings, System.Windows.Media.Color? color = null)
        {
            if(aperturePart == AperturePart.Undefined)
            {
                return null;
            }

            List<SurfaceAppearance> surfaceAppearances = viewSettings.GetAppearances<SurfaceAppearance>(aperture);

            SurfaceAppearance result = null;
            if (surfaceAppearances != null && surfaceAppearances.Count != 0)
            {
                int index = aperturePart == AperturePart.Pane ? 0 : 1;
                if(surfaceAppearances.Count > index)
                {
                    result = surfaceAppearances[index];
                }
            }

            if(result == null && color != null)
            {
                result = new SurfaceAppearance(color.Value.ToDrawing(), ControlPaint.Dark(color.Value.ToDrawing()), 0);
            }

            if (result == null)
            {
                result = SurfaceAppearance(aperture, aperturePart);
            }

            return result;
        }
    }
}