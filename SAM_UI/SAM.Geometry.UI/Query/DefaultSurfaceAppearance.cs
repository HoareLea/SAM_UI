﻿using SAM.Geometry.Object;

namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static SurfaceAppearance DefaultSurfaceAppearance()
        {
            CurveAppearance curveAppearance = DefaultCurveAppearance();

            return new SurfaceAppearance(System.Drawing.Color.FromArgb(128, 128, 128), curveAppearance.Color, curveAppearance.Thickness);
        }

    }
}
