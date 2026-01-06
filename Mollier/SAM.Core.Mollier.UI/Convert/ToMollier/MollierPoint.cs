// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Convert
    {
        public static MollierPoint ToMollier(this Point2D point2D, ChartType chartType, double pressure)
        {
            double x = point2D.X;
            double y = point2D.Y;

            double humidityRatio = chartType == ChartType.Mollier ? x / 1000 : y / 1000;
            double dryBulbTemperature = chartType == ChartType.Mollier ? Mollier.Query.DryBulbTemperature_ByDiagramTemperature(y, humidityRatio, pressure) : x;

            return new MollierPoint(dryBulbTemperature, humidityRatio, pressure);
        }

    }
}

