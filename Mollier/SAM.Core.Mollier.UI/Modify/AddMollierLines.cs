// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.Linq;
using SAM.Geometry.Spatial;
using SAM.Geometry;
using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        public static List<Series> AddMollierLines(this Chart chart, IEnumerable<UIMollierCurve> uIMollierCurves, MollierControlSettings mollierControlSettings)
        {
            if (uIMollierCurves == null)
            {
                return null;
            }

            List<Series> result = new List<Series>();

            ChartType chartType = mollierControlSettings.ChartType;

            Polyline2D polyline2D_RelativeHumidity = null;
            foreach (Series series_Temp in chart.Series)
            {
                ConstantValueCurve constantValueCurve = series_Temp.Tag as ConstantValueCurve;
                if (constantValueCurve == null)
                {
                    continue;
                }

                if (constantValueCurve.ChartDataType == ChartDataType.RelativeHumidity && constantValueCurve.Value == 100)
                {
                    polyline2D_RelativeHumidity = new Polyline2D(series_Temp.Points.ToList().ConvertAll(x => x.ToSAM()));
                    break;
                }
            }

            if (polyline2D_RelativeHumidity == null)
            {
                return null;
            }

            ChartArea chartArea = chart.ChartAreas[0];
            BoundingBox2D boundingBox2D = new BoundingBox2D(new Point2D(chartArea.AxisX.Minimum, chartArea.AxisY.Minimum), new Point2D(chartArea.AxisX.Maximum, chartArea.AxisY.Maximum));

            VerticalPosition verticalPosition = chartType == ChartType.Mollier ? VerticalPosition.Above : VerticalPosition.Below;

            foreach (UIMollierCurve uIMollierCurve in uIMollierCurves)
            {
                MollierLine mollierLine = uIMollierCurve?.MollierCurve as MollierLine;
                if (mollierLine == null)
                {
                    continue;
                }

                MollierSensibleHeatRatioLine mollierSensibleHeatRatioLine = mollierLine as MollierSensibleHeatRatioLine;
                if (mollierSensibleHeatRatioLine == null)
                {
                    continue;
                }

                Color color = uIMollierCurve.UIMollierAppearance.Color == Color.Empty ? Color.Gray : uIMollierCurve.UIMollierAppearance.Color;

                double sensibleHeatRatio = mollierSensibleHeatRatioLine.SensibleHeatRatio;

                MollierPoint mollierPoint = mollierSensibleHeatRatioLine.MollierPoints[0];

                double sensibleLoad = 1;
                Line2D line2D = null;

                double latentLoad = sensibleLoad * ((1 - System.Math.Abs(sensibleHeatRatio)) / System.Math.Abs(sensibleHeatRatio));
                if (double.IsInfinity(latentLoad))
                {
                    IsothermalHumidificationProcess isothermalHumidificationProcess = Mollier.Create.IsothermalHumidificationProcess_ByRelativeHumidity(mollierPoint, 100);

                    Point2D point2D_1 = Convert.ToSAM(isothermalHumidificationProcess.Start, chartType);
                    Point2D point2D_2 = Convert.ToSAM(isothermalHumidificationProcess.End, chartType);

                    line2D = new Line2D(point2D_1, new Vector2D(point2D_1, point2D_2));
                }
                else
                {
                    if (sensibleHeatRatio < 0)
                    {
                        sensibleLoad = -1;
                    }
                    RoomProcess roomProcess_Temp = Mollier.Create.RoomProcess_ByEnd(mollierPoint, 1, sensibleLoad * 1000, latentLoad * 1000);

                    Point2D point2D_1 = Convert.ToSAM(roomProcess_Temp.Start, chartType);
                    Point2D point2D_2 = Convert.ToSAM(roomProcess_Temp.End, chartType);

                    line2D = new Line2D(point2D_1, new Vector2D(point2D_1, point2D_2));
                }

                if (line2D == null)
                {
                    continue;
                }

                List<Segment2D> segment2Ds = new List<Segment2D>();

                List<Point2D> point2Ds = boundingBox2D.Intersections(line2D);
                if (point2Ds != null && point2Ds.Count != 0)
                {
                    Polyline2D polyline_1 = new Polyline2D(point2Ds);

                    List<Point2D> point2Ds_Intersection = polyline_1.Intersections((ISegmentable2D)polyline2D_RelativeHumidity);
                    if (point2Ds_Intersection != null && point2Ds_Intersection.Count != 0)
                    {
                        Point2D point2D_Start = point2Ds[0];

                        point2Ds.AddRange(point2Ds_Intersection);
                        point2Ds.SortByDistance(point2D_Start);

                        for (int i = 0; i < point2Ds.Count - 1; i++)
                        {
                            VerticalPosition verticalPosition_Temp = Geometry.Planar.Query.VerticalPosition(polyline2D_RelativeHumidity, point2Ds[i].Mid(point2Ds[i + 1]));
                            if (verticalPosition_Temp == verticalPosition || verticalPosition_Temp == VerticalPosition.Undefined)
                            {
                                segment2Ds.Add(new Segment2D(point2Ds[i], point2Ds[i + 1]));
                            }
                        }
                    }
                    else if (point2Ds.Count > 1)
                    {
                        for (int i = 0; i < point2Ds.Count - 1; i++)
                        {
                            segment2Ds.Add(new Segment2D(point2Ds[i], point2Ds[i + 1]));
                        }
                    }
                }

                if(segment2Ds == null || segment2Ds.Count == 0)
                {
                    continue;
                }

                foreach(Segment2D segment2D in segment2Ds)
                {
                    Series series = chart.Series.Add("Mollier Sensible Heat Ratio Line " + Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.ChartType = SeriesChartType.Line;
                    series.Color = color;
                    series.BorderDashStyle = ChartDashStyle.Dash;
                    series.BorderWidth = mollierControlSettings.ProccessLineThickness != -1 ? mollierControlSettings.ProccessLineThickness : 3;
                    series.Tag = uIMollierCurve;
                    series.ToolTip = Query.ToolTipText(mollierSensibleHeatRatioLine, chartType);
                    series.Points.AddXY(segment2D[0].X, segment2D[0].Y);
                    series.Points.AddXY(segment2D[1].X, segment2D[1].Y);

                    result.Add(series);
                }
            }

            return result;
        }

        public static List<Series> AddMollierLines(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierCurve> uIMollierCurves = mollierModel.GetMollierObjects<UIMollierCurve>();

            uIMollierCurves = uIMollierCurves?.FindAll(x => x.UIMollierAppearance.Visible == true);
            return chart.AddMollierLines(uIMollierCurves, mollierControlSettings);
        }
    }
}
