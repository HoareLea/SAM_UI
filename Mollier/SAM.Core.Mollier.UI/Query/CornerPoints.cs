// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<Point2D> CornerPoints(MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;

            double xMin = chartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Min * 1000 : mollierControlSettings.Temperature_Min;
            double xMax = chartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Max * 1000 : mollierControlSettings.Temperature_Max;
            double yMin = chartType == ChartType.Mollier ? mollierControlSettings.Temperature_Min : mollierControlSettings.HumidityRatio_Min * 1000;
            double yMax = chartType == ChartType.Mollier ? mollierControlSettings.Temperature_Max : mollierControlSettings.HumidityRatio_Max * 1000;

            return new List<Point2D>() { new Point2D(xMin, yMax), new Point2D(xMin, yMin), new Point2D(xMax, yMin), new Point2D(xMax, yMax) };
        }
    }
}
