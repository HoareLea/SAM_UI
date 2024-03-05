using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Drawing;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        // ------------------GENERAL-ADD-PROCESSES-METHOD----------------------
        /// <summary>
        /// Creates series for all the processes on the chart
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="groups">Mollier groups of processes</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddMollierProcesses(this Chart chart, List<IMollierGroup> groups, MollierControlSettings mollierControlSettings)
        {
            List<Series> result = new List<Series>();
            if(groups == null)
            {
                return null;
            }
            Control control = chart.Parent;
            ChartType chartType = mollierControlSettings.ChartType;
            List<UIMollierProcess> labeledMollierProcesses = generateLabels(groups);

            labeledMollierProcesses?.Sort((x, y) => System.Math.Max(x.Start.HumidityRatio, x.End.HumidityRatio).CompareTo(System.Math.Max(y.Start.HumidityRatio, y.End.HumidityRatio)));
            //List<UIMollierPoint> processPointsToLabel = new List<UIMollierPoint>();

            foreach(UIMollierProcess uIMollierProcess in labeledMollierProcesses)
            {
                MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
                MollierPoint start = mollierProcess?.Start;
                MollierPoint end = mollierProcess?.End;

                if (start == null || end == null)
                {
                    continue;
                }

                if(mollierProcess is UndefinedProcess)
                {
                    createUndefinedProcessSeries(chart, uIMollierProcess, mollierControlSettings);
                    continue;
                }

                if (mollierProcess is RoomProcess)
                {
                    createRoomProcessSeries(chart, uIMollierProcess, mollierControlSettings);
                    continue;
                }

                Series series_Temp = null;

                createProcessSeries(chart, uIMollierProcess, mollierControlSettings);

                if(!mollierControlSettings.DisableStartProcessPoint)
                {
                    //createProcessPointsSeries(chart, start, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierPointAppearance_Start.Label);
                    UIMollierPoint uIMollierPoint_Start = uIMollierProcess.GetUIMollierPoint_Start();
                    series_Temp = AddMollierPoint(chart, chartType, uIMollierPoint_Start, mollierControlSettings);
                    //series_Temp.Tag = mollierProcess;

                    //processPointsToLabel.Add(uIMollierPoint_Start);
                }

                if (!mollierControlSettings.DisableEndProcessPoint)
                {
                    //createProcessPointsSeries(chart, end, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierPointAppearance_End.Label);
                    UIMollierPoint uIMollierPoint_End = uIMollierProcess.GetUIMollierPoint_End();
                    series_Temp = AddMollierPoint(chart, chartType, uIMollierPoint_End, mollierControlSettings);
                    //series_Temp.Tag = mollierProcess;
                    //processPointsToLabel.Add(uIMollierPoint_End);
                }

                //MollierPoint mid = mollierPointsMid(start, end);
                //processPointsToLabel.Add(new UIMollierPoint(mid, new UIMollierPointAppearance(Color.Empty, uIMollierProcess.UIMollierAppearance.Label)));

                if(!mollierControlSettings.DisableCoolingAuxiliaryProcesses)
                {
                    //cooling process create one unique process with ADP point
                    if (labeledMollierProcesses.Count < 30 && mollierProcess is CoolingProcess)
                    {
                        createCoolingAdditionalLines(chart, uIMollierProcess, mollierControlSettings);
                        //if(ADPPoint != null) processPointsToLabel.Add(ADPPoint);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates series for all the processes on the chart
        /// </summary>
        /// <param name="chart">Mollier chart</param>
        /// <param name="mollierModel">Mollier model</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of created series</returns>
        public static List<Series> AddMollierProcesses(this Chart chart, MollierModel mollierModel, MollierControlSettings mollierControlSettings)
        {
            if (mollierModel == null)
            {
                return null;
            }

            List<UIMollierProcess> uIMollierProcesses = mollierModel.GetMollierObjects<UIMollierProcess>();
            uIMollierProcesses = uIMollierProcesses?.FindAll(x => x.UIMollierAppearance.Visible == true);

            List<IMollierGroup> mollierGroups = new List<IMollierGroup>();

            if(uIMollierProcesses?.Count < 30)
            {
                mollierGroups = uIMollierProcesses?.Group();
            }
            else
            {
                MollierGroup mollierGroup = new MollierGroup("New Group");
                uIMollierProcesses?.ForEach(x => mollierGroup.Add(x));
                mollierGroups.Add(mollierGroup);
            }
                
            return chart.AddMollierProcesses(mollierGroups, mollierControlSettings);
        }
        
        // ------------------SERIES--------------------------------------------
        private static Series createDewPointDashLineSeries(Chart chart, MollierPoint mollierPoint_1, MollierPoint mollierPoint_2, UIMollierProcess uIMollierProcess, ChartType chartType, Color color, int borderWidth, ChartDashStyle borderDashStyle)
        {
            // Create additional dash line in Cooling process to ADP 
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            Point2D point2D_1 = Convert.ToSAM(mollierPoint_1, chartType);
            Point2D point2D_2 = Convert.ToSAM(mollierPoint_2, chartType);

            series.Points.AddXY(point2D_1.X, point2D_1.Y);
            series.Points.AddXY(point2D_2.X, point2D_2.Y);

            series.Color = color;
            series.IsVisibleInLegend = false;
            series.BorderWidth = borderWidth;
            series.ChartType = SeriesChartType.Line; //SeriesChartType.Spline;
            series.BorderDashStyle = borderDashStyle;
            series.Tag = uIMollierProcess;//new UIMollierProcess(Mollier.Create.RoomProcess(mollierPoint_1, mollierPoint_2), Color.Black);

            return series;
        }          
        private static List<Series> createRoomProcessSeries(Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            List<Series> result = new List<Series>();

            //Defines the end label of the process
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            uIMollierProcess.UIMollierPointAppearance_End.Label = "ROOM";
            ChartType chartType = mollierControlSettings.ChartType;
            //Specified the color of the Room air condition point
            Color color = uIMollierProcess.UIMollierAppearance.Color == Color.Empty ? Color.Gray : uIMollierProcess.UIMollierAppearance.Color;
            //Creating series for room process
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            series.BorderDashStyle = ChartDashStyle.Dash;
            series.BorderWidth = mollierControlSettings.ProccessLineThickness != -1 ? mollierControlSettings.ProccessLineThickness : 3;
            series.Tag = uIMollierProcess;
            //add start and end point to the process series
            MollierPoint mollierPoint_Start = mollierProcess.Start;
            Point2D point2D_Start = Convert.ToSAM(mollierPoint_Start, chartType);
            MollierPoint mollierPoint_End = mollierProcess.End;
            Point2D point2D_End = Convert.ToSAM(mollierPoint_End, chartType);

            int index;
            index = series.Points.AddXY(point2D_Start.X, point2D_Start.Y);
            series.Points[index].Tag = mollierPoint_Start;

            index = series.Points.AddXY(point2D_End.X, point2D_End.Y);
            series.Points[index].Tag = mollierPoint_End;

            series.ToolTip = Query.ToolTipText(mollierPoint_Start, mollierPoint_End, mollierControlSettings.ChartType, Query.FullProcessName(uIMollierProcess));

            //creating series for Room air condition point 
            Series seriesRoomPoint = chart.Series.Add(Guid.NewGuid().ToString());
            seriesRoomPoint.IsVisibleInLegend = false;
            seriesRoomPoint.ChartType = SeriesChartType.Point;
            seriesRoomPoint.Color = Color.Gray;
            seriesRoomPoint.MarkerStyle = MarkerStyle.Triangle;
            seriesRoomPoint.MarkerBorderWidth = 2;
            seriesRoomPoint.MarkerBorderColor = color;
            seriesRoomPoint.MarkerSize = 8;
            seriesRoomPoint.Tag = mollierProcess;
            //add Room air condition point to the series and create label for it

            seriesRoomPoint.Points.AddXY(point2D_End.X, point2D_End.Y);
            seriesRoomPoint.Points[0].ToolTip = Query.ToolTipText(mollierPoint_End, mollierControlSettings.ChartType, "ROOM");

            if (!string.IsNullOrWhiteSpace(uIMollierProcess?.UIMollierPointAppearance_Start?.Label))
            {
                UIMollierPoint uIMollierPoint = new UIMollierPoint(uIMollierProcess.Start, Create.UIMollierPointAppearance(DisplayPointType.Room));
                
                Series series_Temp = AddMollierPoint(chart, chartType, uIMollierPoint, mollierControlSettings);
                //series_Temp.Tag = mollierProcess;
                //createProcessPointsSeries(chart, mollierPoint_Start, uIMollierProcess, mollierControlSettings.ChartType);
            }

            result.Add(series);
            result.Add(seriesRoomPoint);
            return result;
        }
        private static List<Series> createUndefinedProcessSeries(Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            List<Series> result = new List<Series>();

            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            ChartType chartType = mollierControlSettings.ChartType;
            // Specified the color of the Room air condition point
            Color color = uIMollierProcess.UIMollierAppearance.Color == Color.Empty ? Color.Gray : uIMollierProcess.UIMollierAppearance.Color;
            // Creating series for room process
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            series.BorderDashStyle = ChartDashStyle.Dash;
            series.BorderWidth = mollierControlSettings.ProccessLineThickness != -1 ? mollierControlSettings.ProccessLineThickness : 3;
            series.Tag = uIMollierProcess;

            // Add start and end point to the process series
            Point2D start = Convert.ToSAM(uIMollierProcess.Start, chartType);
            Point2D end = Convert.ToSAM(uIMollierProcess.End, chartType);

            int index;
            index = series.Points.AddXY(start.X, start.Y);
            series.Points[index].Tag = uIMollierProcess.Start;

            index = series.Points.AddXY(end.X, end.Y);
            series.Points[index].Tag = uIMollierProcess.End;
            return result;
        }
        private static Series createProcessSeries(Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            MollierPoint mollierPoint_Start = mollierProcess.Start;
            Point2D point2D_Start = Convert.ToSAM(mollierPoint_Start, chartType);
            MollierPoint mollierPoint_End = mollierProcess.End;
            Point2D point2D_End = Convert.ToSAM(mollierPoint_End, chartType);
            Series series = chart.Series.Add(Guid.NewGuid().ToString());

            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = mollierControlSettings.ProccessLineThickness != -1 ? mollierControlSettings.ProccessLineThickness : 4;
            series.Color = (uIMollierProcess.UIMollierAppearance.Color == Color.Empty) ?
                mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess)
                : uIMollierProcess.UIMollierAppearance.Color;
            series.Tag = uIMollierProcess;

            int index;
            series.ToolTip = Query.ToolTipText(mollierPoint_Start, mollierPoint_End, chartType, Query.FullProcessName(mollierProcess));
            index = series.Points.AddXY(point2D_Start.X, point2D_Start.Y);
            series.Points[index].Tag = mollierPoint_Start;

            index = series.Points.AddXY(point2D_End.X, point2D_End.Y);
            series.Points[index].Tag = mollierPoint_End;

            return series;
        }
        private static UIMollierPoint createCoolingAdditionalLines(Chart chart, UIMollierProcess uIMollierProcess, MollierControlSettings mollierControlSettings)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            MollierPoint start = mollierProcess.Start;
            MollierPoint end = mollierProcess.End;

            if (mollierProcess == null || !(mollierProcess is CoolingProcess) || start == null || end == null || start.HumidityRatio == end.HumidityRatio)
            {
                return null;
            }

            //this is tolerance when we should not display real cooling line < 0.0005
            if (System.Math.Abs(start.HumidityRatio - end.HumidityRatio) < 0.001)
            {
                return null;
            }
            
            Series series = null;

            MollierPoint apparatusDewPoint = ((CoolingProcess)mollierProcess).ApparatusDewPoint();
            //createProcessPointsSeries(chart, apparatusDewMollierPoint, uIMollierProcess, chartType, toolTipName: "Dew Point", pointType: "DewPoint");
            series = AddMollierPoint(chart, chartType, new UIMollierPoint(apparatusDewPoint, Create.UIMollierPointAppearance(DisplayPointType.Dew, DisplayPointType.Dew.Description())), mollierControlSettings);
            series.Tag = uIMollierProcess;


            MollierPoint dewPoint = ((CoolingProcess)mollierProcess).DewPoint();
            //createProcessPointsSeries(chart, secondPoint, uIMollierProcess, chartType, pointType: "SecondPoint");
            series = AddMollierPoint(chart, chartType, new UIMollierPoint(dewPoint, Create.UIMollierPointAppearance(DisplayPointType.CoolingSaturation)), mollierControlSettings);
            series.Tag = uIMollierProcess;//"SecondPoint";

            int borderWidth = mollierControlSettings.ProccessLineThickness != -1 ? mollierControlSettings.ProccessLineThickness : 2;

            createDewPointDashLineSeries(chart, start, dewPoint, uIMollierProcess, chartType, Color.LightGray, borderWidth, ChartDashStyle.Dash);
            createDewPointDashLineSeries(chart, end, apparatusDewPoint, uIMollierProcess, chartType, Color.LightGray, borderWidth, ChartDashStyle.Dash);
            //createDewPointDashLineSeries(chart, end, dewPoint, uIMollierProcess, chartType, Color.LightGray, borderWidth, ChartDashStyle.Dash);

            double pressure = end.Pressure;
            double relativeHumidity = 99;
            double dryBulbTemperatureStep = 0.5;

            Series series_Temp = chart.Series.Add(Guid.NewGuid().ToString());

            series_Temp.Color = Color.LightGray;
            series_Temp.IsVisibleInLegend = false;
            series_Temp.BorderWidth = borderWidth;
            series_Temp.ChartType = SeriesChartType.Line; //SeriesChartType.Spline;
            series_Temp.BorderDashStyle = ChartDashStyle.Dash;
            series_Temp.Tag = uIMollierProcess;//new UIMollierProcess(Mollier.Create.RoomProcess(mollierPoint_1, mollierPoint_2), Color.Black);

            double humidityRatio_Temp = Mollier.Query.HumidityRatio(dewPoint.DryBulbTemperature, relativeHumidity, pressure);
            MollierPoint mollierPoint = new MollierPoint(dewPoint.DryBulbTemperature, humidityRatio_Temp, pressure);
            Point2D point2D = Convert.ToSAM(mollierPoint, chartType);

            series_Temp.Points.AddXY(point2D.X, point2D.Y);

            double dryBulbTemperature = dewPoint.DryBulbTemperature - dryBulbTemperatureStep;
            while (dryBulbTemperature > end.DryBulbTemperature)
            {
                humidityRatio_Temp = Mollier.Query.HumidityRatio(dryBulbTemperature, relativeHumidity, pressure);
                if (!double.IsNaN(humidityRatio_Temp))
                {
                    mollierPoint = new MollierPoint(dryBulbTemperature, humidityRatio_Temp, pressure);
                    point2D = Convert.ToSAM(mollierPoint, chartType);

                    series_Temp.Points.AddXY(point2D.X, point2D.Y);
                }

                dryBulbTemperature -= dryBulbTemperatureStep;
            }

            List<MollierPoint> mollierPoints = Mollier.Query.ProcessMollierPoints((CoolingProcess)mollierProcess);
            if (mollierPoints != null && mollierPoints.Count > 1)
            {
                for (int j = 0; j < mollierPoints.Count - 1; j++)
                {
                    createDewPointDashLineSeries(chart, mollierPoints[j], mollierPoints[j + 1], uIMollierProcess, chartType, Color.Gray, 3, ChartDashStyle.Solid);
                }
            }

            return new UIMollierPoint(apparatusDewPoint, new UIMollierPointAppearance(Color.Empty, "ADP"));
        }

        //private static Series createProcessPointsSeries(this Chart chart, MollierPoint mollierPoint, UIMollierProcess UI_MollierProcess, ChartType chartType, string toolTipName = null, string pointType = "Default")
        //{
        //    Series series = chart.Series.Add(Guid.NewGuid().ToString());
        //    Point2D point2D = Convert.ToSAM(mollierPoint, chartType);
        //    int index = series.Points.AddXY(point2D.X, point2D.Y);
        //    switch (pointType)
        //    {
        //        case "Default":
        //            series.MarkerSize = 8;
        //            series.MarkerBorderWidth = 2;
        //            series.MarkerBorderColor = Color.Orange;
        //            break;
        //        case "DewPoint":
        //            series.MarkerSize = 8;
        //            break;
        //        case "SecondPoint":
        //            series.MarkerSize = 5;
        //            break;
        //    }
        //    series.Color = Color.Gray;
        //    series.IsVisibleInLegend = false;
        //    series.ChartType = SeriesChartType.Point;
        //    series.Tag = UI_MollierProcess.MollierProcess;
        //    if (pointType == "SecondPoint")
        //    {
        //        series.Tag = "SecondPoint";
        //    }
        //    series.MarkerStyle = MarkerStyle.Circle;
        //    series.Points[index].ToolTip = Query.ToolTipText(mollierPoint, chartType, toolTipName);

        //    return series;
        //}
        
        
        // General methods used above 
        private static List<UIMollierProcess> generateLabels(List<IMollierGroup> groups)
        {
            List<UIMollierProcess> labeledMollierProcesses = new List<UIMollierProcess>();

            foreach(IMollierGroup group in groups)
            {
                MollierGroup mollierGroup = (MollierGroup)group;
                if(mollierGroup == null)
                {
                    continue;
                }
                char name = 'A';
                List<IMollierProcess> processList = mollierGroup.GetObjects<IMollierProcess>();
                if(processList == null)
                {
                    continue;
                }
                for (int j = 0; j < processList.Count; j++)
                {
                    UIMollierProcess UI_MollierProcess = (UIMollierProcess)processList[j];
                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess;

                    // Set default values whether they're not set
                    if(UI_MollierProcess.UIMollierPointAppearance_Start == null)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_Start = new UIMollierPointAppearance(Color.Empty, string.Empty);
                    }

                    if(UI_MollierProcess.UIMollierPointAppearance_Start.Label == null)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_Start.Label = string.Empty;
                    }


                    if(UI_MollierProcess.UIMollierPointAppearance_End == null)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_End = new UIMollierPointAppearance(Color.Empty, string.Empty);
                    }

                    if (UI_MollierProcess.UIMollierPointAppearance_End.Label == null)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_End.Label = string.Empty;
                    }

                    if (UI_MollierProcess?.MollierProcess is RoomProcess)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_End.Label = "ROOM";
                    }

                    //if (processList.Count > 1 && j == 0)
                    //{
                    //    UI_MollierProcess.UIMollierAppearance_Start.Label = "OSA";
                    //}
                    //else 
                    if (UI_MollierProcess.UIMollierPointAppearance_Start.Label == string.Empty)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_Start.Label = name + "1";
                    }


                    //if (processList.Count > 1 && j == processList.Count - 2 && ((UIMollierProcess)processList[j + 1]).MollierProcess is RoomProcess)
                    //{
                    //    UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    //}
                    //else if (processList.Count > 1 && j == processList.Count - 1)
                    //{
                    //    UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    //}
                    //  else;
                    if (UI_MollierProcess.UIMollierPointAppearance_End.Label == string.Empty)
                    {
                        UI_MollierProcess.UIMollierPointAppearance_End.Label = name + "2";
                    }

                    UI_MollierProcess.UIMollierAppearance.Label = UI_MollierProcess.UIMollierAppearance.Label == null ? Query.ProcessName(mollierProcess) : UI_MollierProcess.UIMollierAppearance.Label;
                    
                    name++;
                    if (UI_MollierProcess.UIMollierAppearance.Visible == true)
                    {
                        labeledMollierProcesses.Add(new UIMollierProcess(UI_MollierProcess));
                    }
                }
            }

            return labeledMollierProcesses;
        //  this.mollierProcesses = mollierProcesses;//used only to remember point name to show it in tooltip
        }
        private static MollierPoint mollierPointsMid(MollierPoint mollierPoint1, MollierPoint mollierPoint2)
        {
            double dryBulbTemperature = (mollierPoint1.DryBulbTemperature + mollierPoint2.DryBulbTemperature) / 2;
            double humidityRatio = (mollierPoint1.HumidityRatio + mollierPoint2.HumidityRatio) / 2;
            return new MollierPoint(dryBulbTemperature, humidityRatio, Standard.Pressure);
        }

    }
}
