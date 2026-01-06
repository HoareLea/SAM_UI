// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool Contains(this Series series, ChartType chartType, MollierPoint mollierPoint, double tolerance = Tolerance.Distance)
        {
            if(series == null || chartType == ChartType.Undefined || mollierPoint == null)
            {
                return false;
            }

            foreach(DataPoint dataPoint in series.Points)
            {
                Point2D point2D = new Point2D(dataPoint.XValue, dataPoint.YValues[0]);
                MollierPoint mollierPoint_Temp = Convert.ToMollier(point2D, chartType, mollierPoint.Pressure);
                if(mollierPoint.DryBulbTemperature.AlmostEqual(mollierPoint_Temp.DryBulbTemperature, tolerance) && mollierPoint.HumidityRatio.AlmostEqual(mollierPoint_Temp.HumidityRatio, tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains(this Series series, double x, double y, double tolerance = Tolerance.Distance)
        {
            if(series == null || double.IsNaN(x) || double.IsNaN(y))
            {
                return false;
            }

            foreach(DataPoint dataPoint in series.Points)
            {
                if(!dataPoint.XValue.AlmostEqual(x, tolerance))
                {
                    continue;
                }

                foreach(double y_Temp in dataPoint.YValues)
                {
                    if (!y_Temp.AlmostEqual(y, tolerance))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

