﻿using SAM.Geometry.Object;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static CurveAppearance HighlightCurveAppearance(CurveAppearance curveAppearance)
        {
            if(curveAppearance == null)
            {
                return null;
            }

            double thickness = curveAppearance.Thickness < 0.01 ? 0.02 : curveAppearance.Thickness * 2;

            return new CurveAppearance(curveAppearance.Color, thickness);
        }

    }
}
