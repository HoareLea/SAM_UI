using SAM.Geometry.Planar;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Modify
    {
        [Obsolete("To be removed Maciek")] // TODO: Remove (MollierProcess)
        public static List<UIMollierProcess> AddMollierProcesses(Control control, Chart chart, MollierControlSettings mollierControlSettings, List<List<UIMollierProcess>> systems,  List<UIMollierProcess> mollierProcesses, List<Tuple<MollierPoint, string>> created_points)
        {
            if(mollierProcesses == null)
            {
                return null;
            }

            ChartType chartType = mollierControlSettings.ChartType;
            created_points = new List<Tuple<MollierPoint, string>>();//used for labeling in label process new 2

            //create series for all points and lines in processes(create circles, lines, tooltips, ADP etc.)
            for (int i = 0; i < mollierProcesses.Count; i++)
            {
                UIMollierProcess uIMollierProcess = mollierProcesses[i];//contains all spcified data of the process like color, start label etc.
                MollierProcess mollierProcess = uIMollierProcess.MollierProcess;//contains the most important data of the process: only start end point, and what type of process is it 

                if (mollierProcess is UndefinedProcess)
                {
                    createSeries_RoomProcess(chart, mollierControlSettings, uIMollierProcess);
                    continue;
                }
                //process series
                Series series = chart.Series.Add(Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 4;
                series.Color = (uIMollierProcess.UIMollierAppearance.Color == Color.Empty) ? mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess) : uIMollierProcess.UIMollierAppearance.Color;
                series.Tag = mollierProcess;

                MollierPoint start = mollierProcess?.Start;
                MollierPoint end = mollierProcess?.End;
                if (start == null || end == null)
                {
                    continue;
                }
                //creating series - processes points pattern
                createSeries_ProcessesPoints(chart, start, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierAppearance_Start.Label);
                createSeries_ProcessesPoints(chart, end, uIMollierProcess, chartType, toolTipName: uIMollierProcess.UIMollierAppearance_End.Label);
                //add start and end point to the process series
                int index;
                series.ToolTip = Query.ToolTipText(start, end, chartType, Query.FullProcessName(uIMollierProcess));
                index = chartType == ChartType.Mollier ? series.Points.AddXY(start.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(start)) : series.Points.AddXY(start.DryBulbTemperature, start.HumidityRatio);
                series.Points[index].Tag = start;
                index = chartType == ChartType.Mollier ? series.Points.AddXY(end.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(end)) : series.Points.AddXY(end.DryBulbTemperature, end.HumidityRatio);
                series.Points[index].Tag = end;


                //cooling process create one unique process with ADP point
                if (mollierProcess is CoolingProcess)
                {
                    CoolingProcess coolingProcess = (CoolingProcess)mollierProcess;
                    if (start.HumidityRatio == end.HumidityRatio)
                    {
                        continue;
                    }
                    MollierPoint apparatusDewPoint = coolingProcess.ApparatusDewPoint();
                    double X = chartType == ChartType.Mollier ? apparatusDewPoint.HumidityRatio * 1000 : apparatusDewPoint.DryBulbTemperature;
                    double Y = chartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(apparatusDewPoint) : apparatusDewPoint.HumidityRatio;
                    CreateMovedLabel(chart, mollierControlSettings, X, Y, 0, 0, 0, -3 * Query.ScaleVector2D(control, mollierControlSettings).Y, -1 * Query.ScaleVector2D(control, mollierControlSettings).X, -0.0007 * Query.ScaleVector2D(control, mollierControlSettings).Y, "ADP", ChartDataType.Undefined, ChartParameterType.Point, color: Color.Gray);

                    MollierPoint ADPPoint_Temp = chartType == ChartType.Mollier ? new MollierPoint(apparatusDewPoint.DryBulbTemperature - 3 * Query.ScaleVector2D(control, mollierControlSettings).Y, apparatusDewPoint.HumidityRatio, mollierControlSettings.Pressure) : new MollierPoint(apparatusDewPoint.DryBulbTemperature - 1 * Query.ScaleVector2D(control, mollierControlSettings).X, apparatusDewPoint.HumidityRatio - 0.0007 * Query.ScaleVector2D(control, mollierControlSettings).Y, mollierControlSettings.Pressure);

                    created_points.Add(new Tuple<MollierPoint, string>(ADPPoint_Temp, "ADP"));

                    MollierPoint secondPoint = coolingProcess.DewPoint();
                    //creating series - processes points pattern
                    createSeries_ProcessesPoints(chart, apparatusDewPoint, uIMollierProcess, chartType, toolTipName: "Dew Point", pointType: "DewPoint");
                    createSeries_ProcessesPoints(chart, secondPoint, uIMollierProcess, chartType, pointType: "SecondPoint");
                    //creating series - special with ADP process pattern
                    createSeries_DewPoint(chart, start, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
                    createSeries_DewPoint(chart, end, apparatusDewPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
                    createSeries_DewPoint(chart, end, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);


                    //Additional Lines 2023.06.06
                    List<MollierPoint> mollierPoints = Mollier.Query.ProcessMollierPoints(coolingProcess);
                    if (mollierPoints != null && mollierPoints.Count > 1)
                    {
                        for (int j = 0; j < mollierPoints.Count - 1; j++)
                        {
                            createSeries_DewPoint(chart, mollierPoints[j], mollierPoints[j + 1], mollierProcess, chartType, Color.Gray, 3, ChartDashStyle.Solid);
                        }
                    }
                }

            }
            List<UIMollierProcess> uIMollierProcesses_Temp = mollierProcesses == null ? null : new List<UIMollierProcess>(mollierProcesses.Cast<UIMollierProcess>());
            uIMollierProcesses_Temp?.Sort((x, y) => System.Math.Max(y.Start.HumidityRatio, y.End.HumidityRatio).CompareTo(System.Math.Max(x.Start.HumidityRatio, x.End.HumidityRatio)));
            List<UIMollierProcess> processesWithLabels = createProcessesLabels(systems, uIMollierProcesses_Temp, chartType); // for tooltip


            //labeling all the processes
            createPorcessesLabels_New(control, chart, mollierControlSettings, uIMollierProcesses_Temp, created_points);
            return processesWithLabels;
        }


        private static void createSeries_DewPoint(Chart chart, MollierPoint mollierPoint_1, MollierPoint mollierPoint_2, IMollierProcess mollierProcess, ChartType chartType, Color color, int borderWidth, ChartDashStyle borderDashStyle)
        {
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            if (chartType == ChartType.Mollier)
            {
                series.Points.AddXY(mollierPoint_1.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint_1));
                series.Points.AddXY(mollierPoint_2.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint_2));
            }
            else
            {
                series.Points.AddXY(mollierPoint_1.DryBulbTemperature, mollierPoint_1.HumidityRatio);
                series.Points.AddXY(mollierPoint_2.DryBulbTemperature, mollierPoint_2.HumidityRatio);
            }
            series.Color = color;
            series.IsVisibleInLegend = false;
            series.BorderWidth = borderWidth;
            series.ChartType = SeriesChartType.Spline;
            series.BorderDashStyle = borderDashStyle;
            series.Tag = "dashLine";
        }
        private static void createSeries_ProcessesPoints(Chart chart, MollierPoint mollierPoint, UIMollierProcess UI_MollierProcess, ChartType chartType, string toolTipName = null, string pointType = "Default")
        {
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            int index = chartType == ChartType.Mollier ? series.Points.AddXY(mollierPoint.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(mollierPoint)) : series.Points.AddXY(mollierPoint.DryBulbTemperature, mollierPoint.HumidityRatio);
            switch (pointType)
            {
                case "Default":
                    series.MarkerSize = 8;
                    series.MarkerBorderWidth = 2;
                    series.MarkerBorderColor = Color.Orange;
                    break;
                case "DewPoint":
                    series.MarkerSize = 8;
                    break;
                case "SecondPoint":
                    series.MarkerSize = 5;
                    break;
            }
            series.Color = Color.Gray;
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;
            series.Tag = UI_MollierProcess.MollierProcess;
            if (pointType == "SecondPoint")
            {
                series.Tag = "SecondPoint";
            }
            series.MarkerStyle = MarkerStyle.Circle;
            series.Points[index].ToolTip = Query.ToolTipText(mollierPoint, chartType, toolTipName);
            //seriesDew.mark
        }
        private static void createSeries_RoomProcess(Chart chart, MollierControlSettings mollierControlSettings, UIMollierProcess uIMollierProcess)
        {
            //defines the end label of the process
            MollierProcess mollierProcess = uIMollierProcess.MollierProcess;
            //specified the color of the Room air condition point
            Color color = uIMollierProcess.UIMollierAppearance.Color == Color.Empty ? Color.Gray : uIMollierProcess.UIMollierAppearance.Color;
            //creating series for room process
            Series series = chart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            series.BorderDashStyle = ChartDashStyle.Dash;
            series.BorderWidth = 3;
            series.Tag = mollierProcess;
            //add start and end point to the process series
            MollierPoint start = mollierProcess.Start;
            MollierPoint end = mollierProcess.End;
            int index;
            index = mollierControlSettings.ChartType == ChartType.Mollier ? series.Points.AddXY(start.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(start)) : series.Points.AddXY(start.DryBulbTemperature, start.HumidityRatio);
            series.Points[index].Tag = start;
            index = mollierControlSettings.ChartType == ChartType.Mollier ? series.Points.AddXY(end.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(end)) : series.Points.AddXY(end.DryBulbTemperature, end.HumidityRatio);
            series.Points[index].Tag = end;
            series.ToolTip = Query.ToolTipText(start, end, mollierControlSettings.ChartType, Query.FullProcessName(uIMollierProcess));

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
            double X = mollierControlSettings.ChartType == ChartType.Mollier ? end.HumidityRatio * 1000 : end.DryBulbTemperature;
            double Y = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(end) : end.HumidityRatio;
            seriesRoomPoint.Points.AddXY(X, Y);
            seriesRoomPoint.Points[0].ToolTip = Query.ToolTipText(end, mollierControlSettings.ChartType, "ROOM");
            if (!string.IsNullOrWhiteSpace(uIMollierProcess?.UIMollierAppearance_Start?.Label))
            {
                createSeries_ProcessesPoints(chart, start, uIMollierProcess, mollierControlSettings.ChartType);
            }

        }
        private static List<UIMollierProcess> createProcessesLabels(List<List<UIMollierProcess>> systems, List<UIMollierProcess> mollierProcesses, ChartType chartType)
        {
            // Dla systemu procesów ta metoda labeluje punkty A1 - A2, SUP, etc.
            if (systems == null)
            {
                return null;
            }
            //Item1 - MollierPoint, Item2 - factor X, Item3 - factor Y
            List<Tuple<MollierPoint, double, double>> tuples = new List<Tuple<MollierPoint, double, double>>();
            char name = 'A';
            for (int i = 0; i < systems.Count; i++)
            {
                for (int j = 0; j < systems[i].Count; j++)
                {
                    UIMollierProcess UI_MollierProcess = systems[i][j];
                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess;
                    if (UI_MollierProcess.UIMollierAppearance_End?.Label == "SUP")
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = null;
                    }


                    if (UI_MollierProcess.UIMollierAppearance_Start.Label == null && systems[i].Count == 1)
                    {
                        UI_MollierProcess.UIMollierAppearance_Start.Label = name + "1";
                    }
                    else if (UI_MollierProcess.UIMollierAppearance_Start.Label == null && j == 0)
                    {
                        UI_MollierProcess.UIMollierAppearance_Start.Label = "OSA";
                    }
                    if (UI_MollierProcess.UIMollierAppearance_End.Label == null && systems[i].Count > 1 && j == systems[i].Count - 2 && systems[i][j + 1].MollierProcess is UndefinedProcess)
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    }
                    else if (UI_MollierProcess.UIMollierAppearance_End.Label == null && systems[i].Count > 1 && j == systems[i].Count - 1)
                    {
                        UI_MollierProcess.UIMollierAppearance_End.Label = "SUP";
                    }
                    UI_MollierProcess.UIMollierAppearance.Label = UI_MollierProcess.UIMollierAppearance.Label == null ? Query.ProcessName(mollierProcess) : UI_MollierProcess.UIMollierAppearance.Label;
                    UI_MollierProcess.UIMollierAppearance_End.Label = UI_MollierProcess.UIMollierAppearance_End.Label == null ? name + "2" : UI_MollierProcess.UIMollierAppearance_End.Label;

                    name++;
                }
            }

            return mollierProcesses;//used only to remember point name to show it in tooltip
        }
        private static void createPorcessesLabels_New(Control control, Chart chart, MollierControlSettings mollierControlSettings, List<UIMollierProcess> mollierProcesses, List<Tuple<MollierPoint, string>> created_points)//creates sorted list of points that has to be labaled
        {
            List<Tuple<MollierPoint, string>> points_list = new List<Tuple<MollierPoint, string>>();

            foreach (UIMollierProcess UI_MollierProcess in mollierProcesses)
            {
                if (!string.IsNullOrWhiteSpace(UI_MollierProcess?.UIMollierAppearance_Start?.Label))
                {
                    points_list.Add(new Tuple<MollierPoint, string>(UI_MollierProcess.Start, UI_MollierProcess.UIMollierAppearance_Start.Label));
                }
                if (UI_MollierProcess.UIMollierAppearance_End.Label != null && UI_MollierProcess.UIMollierAppearance_End.Label != "")
                {
                    points_list.Add(new Tuple<MollierPoint, string>(UI_MollierProcess.End, UI_MollierProcess.UIMollierAppearance_End.Label));
                }

            }
            points_list?.Sort((x, y) => x.Item1.HumidityRatio.CompareTo(y.Item1.HumidityRatio));
            foreach (UIMollierProcess UI_MollierProcess in mollierProcesses)
            {
                if (UI_MollierProcess.UIMollierAppearance.Label != null && UI_MollierProcess.UIMollierAppearance.Label != "")
                {
                    double dryBulbTemperature = (UI_MollierProcess.Start.DryBulbTemperature + UI_MollierProcess.End.DryBulbTemperature) / 2;
                    double humdityRatio = (UI_MollierProcess.Start.HumidityRatio + UI_MollierProcess.End.HumidityRatio) / 2;
                    MollierPoint mid = new MollierPoint(dryBulbTemperature, humdityRatio, mollierControlSettings.Pressure);
                    points_list.Add(new Tuple<MollierPoint, string>(mid, UI_MollierProcess.UIMollierAppearance.Label));
                }
            }
            createPorcessesLabels_New_2(control, chart, mollierControlSettings, points_list, created_points, mollierProcesses);
        }
        private static void createPorcessesLabels_New_2(Control control, Chart chart, MollierControlSettings mollierControlSettings, List<Tuple<MollierPoint, string>> points_list, List<Tuple<MollierPoint, string>> created_points, List<UIMollierProcess> mollierProcesses)
        {
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                for (int i = 0; i < points_list.Count; i++)
                {
                    MollierPoint mollierPoint = points_list[i].Item1;
                    string label = points_list[i].Item2;
                    Vector2D vector2D = Query.ScaleVector2D(control, mollierControlSettings);
                    //1st option right
                    bool is_space = true;
                    //how much move the label 
                    double moveHumidityRatio = (0.25 + 0.2 * label.Length / 2.0) * vector2D.X;
                    double moveTemperature = -1.4 * vector2D.Y;
                    //mollier point moved  riBght, top, left, down
                    MollierPoint mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = 0;
                        moveTemperature = 0;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    }

                    //2nd option top
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -(0.25 + 0.2 * label.Length / 2.0) * vector2D.X;
                        moveTemperature = -1.4 * vector2D.Y;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    }
                    //3rd option left
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = 0;
                        moveTemperature = -2.5 * vector2D.Y;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    }
                    //4th option down   
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                    }
                }
            }
            else
            {
                for (int i = 0; i < points_list.Count; i++)
                {
                    MollierPoint mollierPoint = points_list[i].Item1;
                    string label = points_list[i].Item2;
                    Vector2D vector2D = Query.ScaleVector2D(control, mollierControlSettings);
                    //1st option top
                    bool is_space = true;
                    //how much move the label 
                    double moveHumidityRatio = 0;
                    double moveTemperature = 0;
                    //mollier point moved  right, top, left, down
                    MollierPoint mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -0.0007 * vector2D.Y;
                        moveTemperature = (0.5 + 0.4 * label.Length / 2.0) * vector2D.X;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    }

                    //2nd option right
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -0.0015 * vector2D.Y;
                        moveTemperature = 0;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    }
                    //3rd option down
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -0.0007 * vector2D.Y;
                        moveTemperature = -(0.5 + 0.4 * label.Length / 2.0) * vector2D.X;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    }
                    //4th option left  
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (Query.Overlaps(control, mollierControlSettings, mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !Query.Intersect(control, mollierControlSettings, mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                    }
                }
            }

            for (int i = 0; i < created_points.Count; i++)
            {
                if (created_points[i].Item2 == "ADP")
                {
                    continue;
                }
                double Y = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(created_points[i].Item1) : created_points[i].Item1.HumidityRatio;
                double X = mollierControlSettings.ChartType == ChartType.Mollier ? created_points[i].Item1.HumidityRatio * 1000 : created_points[i].Item1.DryBulbTemperature;

                CreateMovedLabel(chart, mollierControlSettings, X, Y, 0, 0, 0, 0, 0, 0, created_points[i].Item2, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black);
            }
        }

    }
}
