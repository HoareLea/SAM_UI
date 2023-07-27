using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using SAM.Geometry.Planar;
using System;

namespace SAM.Core.Mollier.UI
{
    public static partial class Temporary
    {
        private static BoundingBox2D chartArea; 
        public static void Tests()
        {

            /*   Series series = MollierChart.Series[0];
            if(series.Tag is ConstantValueCurve)
            {
                ConstantValueCurve curve = (ConstantValueCurve)series.Tag;
                curve.Value; // np linia entalpii, jej wartość
                curve.ChartDataType; // np humidityRatio
                if(curve is ConstantTemperatureCurve)
                {
                    ConstantTemperatureCurve tempCurve = (ConstantTemperatureCurve)curve;
                    tempCurve.Phase; // gas -> upper 100% liquid -> under 100%
                }
                if (curve is ConstantEnthalpyCurve)
                {
                    ConstantEnthalpyCurve tempEnth = (ConstantEnthalpyCurve)curve;  
                    tempEnth.Phase; // gas -> upper 100% liquid -> under 100%
                }
            }*/

            //SAM.Geometry.Planar.Rectangle2D rectangle = SAM.Geometry.Planar.Create.Rectangle2D();
        }
        public static Series AddLinesLabels(Chart chart, MollierControlSettings mollierControlSettings)
        {
            // Creates Lines labels 
            // for now creates only labels for densityLines
            Point2D chartMinPoint = Convert.ToSAM(new MollierPoint(mollierControlSettings.Temperature_Min, mollierControlSettings.HumidityRatio_Min / 1000, mollierControlSettings.Pressure), mollierControlSettings.ChartType);
            Point2D chartMaxPoint = Convert.ToSAM(new MollierPoint(mollierControlSettings.Temperature_Max, mollierControlSettings.HumidityRatio_Max / 1000, mollierControlSettings.Pressure), mollierControlSettings.ChartType);
            chartArea = new BoundingBox2D(chartMinPoint, chartMaxPoint);

            List<Series> densitySeriesList = new List<Series>();
            List<Tuple<Segment2D, string>> visibleDensityLines = new List<Tuple<Segment2D, string>>();

            foreach (Series seriesID in chart.Series)
            {
                if(seriesID.Tag is ConstantValueCurve)
                {
                    ConstantValueCurve curve = (ConstantValueCurve)seriesID.Tag;
                    if(curve.ChartDataType == ChartDataType.Density && 
                        inActualVisibilitySettings((ConstantValueCurve)seriesID.Tag, mollierControlSettings))
                    {
                        densitySeriesList.Add(seriesID);

                        Segment2D visibleDensityLine = getVisibleLinePart(curve);
                        
                        // Tutaj dodać wyliczanie tej linii
                   //     visibleDensityLines.Add(new Tuple<Segment2D, string>(visibleDensityLine);
                    }
                }
            }
            
            // get obstacles: processes, zones, points, findPoint, etc everything that we want to be seen in front of chart
            // as segments, points and bounding boxes'



            // may be usefull new method in Query/Convert that'll be convert label and position to boundingbox


            // create list of density Lines seen on chart in actual settings and labels
            foreach(Series series in densitySeriesList)
            {

            }

            // try to put a label on a chart
            
            return chart.Series[0];
        }

        private static bool inActualVisibilitySettings(ConstantValueCurve curve, MollierControlSettings mollierControlSettings)
        {
            Point2D start = Convert.ToSAM(curve.Start, mollierControlSettings.ChartType);
            Point2D end = Convert.ToSAM(curve.End, mollierControlSettings.ChartType);
            Segment2D line = new Segment2D(start, end);

            return chartArea.InRange(line);
        }

        private static Segment2D getVisibleLinePart(ConstantValueCurve curve)
        {
            Point2D start = new Point2D(1, 2); //TODO: Changed it 
            Point2D end = new Point2D(1, 2);

            return new Segment2D(start, end);
        }
    }
}
