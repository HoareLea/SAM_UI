﻿namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static PointAppearance DefaultPointAppearance()
        {
            return new PointAppearance(System.Windows.Media.Color.FromRgb(0, 0, 0), 0.001);
        }

    }
}