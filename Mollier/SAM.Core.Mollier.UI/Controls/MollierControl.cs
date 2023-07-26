using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierControl : UserControl
    {
        public event MollierPointSelectedEventHandler MollierPointSelected;

        public static double MaxPressure = 400000;//TODO: [Maciek] Move do SAM.Core.Mollier.Default
        public static double MinPressure = 35000;//TODO: [Maciek] Move do SAM.Core.Mollier.Default

        private Point mdown = Point.Empty;
        private bool selection = false;
        private MollierControlSettings mollierControlSettings;
        private List<UIMollierPoint> mollierPoints;
        private List<UIMollierProcess> mollierProcesses;
        private List<MollierZone> mollierZones;
        private List<List<UIMollierProcess>> systems; // sorted systems of processes
        private List<Tuple<Series, int>> seriesData = new List<Tuple<Series, int>>();

        public MollierControl()
        {
            InitializeComponent();

            mollierControlSettings = new MollierControlSettings();
        }

        private void CreateYAxis(Chart chart, ChartArea area, Series series, float axisX, float axisWidth, float labelsSize, bool alignLeft, double P_w_Min, double P_w_Max)
        {

            chart.ApplyPaletteColors();  // (*)
            long x = DateTime.Now.Ticks;

            // Create new chart area for original series
            ChartArea areaSeries = new ChartArea();
            if (MollierChart.ChartAreas.Count != 3)
            {
                areaSeries = chart.ChartAreas.Add("Psychrometric_P_w" + x.ToString());
            }
            else
            {
                areaSeries = chart.ChartAreas[1];
                areaSeries.Name = "Psychrometric_P_w" + x.ToString();
            }
            //ChartArea areaSeries = chart.ChartAreas.Add("Psychrometric_P_w" + x.ToString());
            areaSeries.BackColor = Color.Transparent;
            areaSeries.BorderColor = Color.Transparent;
            areaSeries.Position.FromRectangleF(area.Position.ToRectangleF());
            areaSeries.InnerPlotPosition.FromRectangleF(area.InnerPlotPosition.ToRectangleF());
            areaSeries.AxisX.MajorGrid.Enabled = false;
            areaSeries.AxisX.MajorTickMark.Enabled = false;
            areaSeries.AxisX.LabelStyle.Enabled = false;
            areaSeries.AxisY.MajorGrid.Enabled = false;
            areaSeries.AxisY.MajorTickMark.Enabled = false;
            areaSeries.AxisY.LabelStyle.Enabled = false;
            areaSeries.AxisY.IsStartedFromZero = area.AxisY.IsStartedFromZero;
            // associate series with new ca
            series.ChartArea = areaSeries.Name;

            // Create new chart area for axis
            ChartArea areaAxis = new ChartArea();
            if (MollierChart.ChartAreas.Count != 3)
            {
                areaAxis = chart.ChartAreas.Add("Psychrometric_P_w_copy" + x.ToString());
            }
            else
            {
                areaAxis = chart.ChartAreas[2];
                areaAxis.Name = "Psychrometric_P_w_copy" + x.ToString();
            }
            //ChartArea areaAxis = chart.ChartAreas.Add("Psychrometric_P_w_copy" + x.ToString());

            areaAxis.BackColor = Color.Transparent;
            areaAxis.BorderColor = Color.Transparent;
            RectangleF oRect = area.Position.ToRectangleF();
            areaAxis.Position = new ElementPosition(oRect.X, oRect.Y, axisWidth, oRect.Height);
            areaAxis.InnerPlotPosition.FromRectangleF(areaSeries.InnerPlotPosition.ToRectangleF());

            // Create a copy of specified series
            Series seriesCopy = chart.Series.Add("Psychrometric_P_w_copy" + x.ToString());
            seriesCopy.ChartType = series.ChartType;
            seriesCopy.YAxisType = alignLeft ? AxisType.Primary : AxisType.Secondary;  // (**)


            foreach (DataPoint point in series.Points)
            {
                seriesCopy.Points.AddXY(point.XValue, point.YValues[0]);
            }
            // Hide copied series
            seriesCopy.IsVisibleInLegend = false;
            seriesCopy.Color = Color.Transparent;
            seriesCopy.BorderColor = Color.Transparent;
            seriesCopy.ChartArea = areaAxis.Name;

            // Disable grid lines & tickmarks
            areaAxis.AxisX.LineWidth = 0;
            areaAxis.AxisY.LineWidth = 1;
            areaAxis.AxisX.MajorGrid.Enabled = false;
            areaAxis.AxisY.MajorGrid.Enabled = true;
            areaAxis.AxisX.MajorTickMark.Enabled = false;
            areaAxis.AxisY.MajorTickMark.Enabled = true;
            areaAxis.AxisX.LabelStyle.Enabled = false;
            areaAxis.AxisY.LabelStyle.Enabled = true;

            Axis areaAxisAxisY = alignLeft ? areaAxis.AxisY : areaAxis.AxisY2;   // (**)
            areaAxisAxisY.MajorGrid.Enabled = false;
            areaAxisAxisY.IsStartedFromZero = area.AxisY.IsStartedFromZero;
            areaAxisAxisY.LabelStyle.Font = area.AxisY.LabelStyle.Font;
            areaAxisAxisY.Minimum = System.Math.Round(P_w_Min, 4);
            areaAxisAxisY.Maximum = System.Math.Round(P_w_Max, 4);
            areaAxisAxisY.Interval = mollierControlSettings.PartialVapourPressure;
            areaAxisAxisY.Title = "Partial Vapour Pressure pW [kPa]";

            areaAxis.AxisX2.Title = "";

            // Adjust area position
            areaAxis.Position.X = axisX;
            areaAxis.InnerPlotPosition.X += labelsSize;

            areaAxisAxisY.MinorTickMark.Enabled = false;
            areaAxisAxisY.MinorGrid.Enabled = false;
            areaAxisAxisY.MinorTickMark.Interval = 0.1;
        }
        private void CreateXAxis(Chart chart, ChartArea area, Series series, float axisY, float axisHeight, float labelsSize, bool alignLeft, double P_w_Min, double P_w_Max)
        {
            long x = DateTime.Now.Ticks;

            chart.ApplyPaletteColors();  // (*)
            // Create new chart area for original series
            ChartArea areaSeries = new ChartArea();
            if (MollierChart.ChartAreas.Count != 3)
            {
                areaSeries = chart.ChartAreas.Add("Mollier P_w" + x.ToString());
            }
            else
            {
                areaSeries = chart.ChartAreas[1];
                areaSeries.Name = "Mollier P_w" + x.ToString();
            }
            //areaSeries = chart.ChartAreas.Add("Mollier P_w" + x.ToString());
            areaSeries.BackColor = Color.Transparent;
            areaSeries.BorderColor = Color.Transparent;
            areaSeries.Position.FromRectangleF(area.Position.ToRectangleF());
            areaSeries.InnerPlotPosition.FromRectangleF(area.InnerPlotPosition.ToRectangleF());
            areaSeries.AxisY.MajorGrid.Enabled = false;
            areaSeries.AxisY.MajorTickMark.Enabled = false;
            areaSeries.AxisY.LabelStyle.Enabled = false;
            areaSeries.AxisX.MajorGrid.Enabled = false;
            areaSeries.AxisX.MajorTickMark.Enabled = false;
            areaSeries.AxisX.LabelStyle.Enabled = false;
            areaSeries.AxisX.IsStartedFromZero = area.AxisX.IsStartedFromZero;
            
            // associate series with new ca
            series.ChartArea = areaSeries.Name;

            // Create new chart area for axis
            ChartArea areaAxis = new ChartArea();
            if (MollierChart.ChartAreas.Count != 3)
            {
                areaAxis = chart.ChartAreas.Add("Mollier P_w_copy" + x.ToString());
            }
            else
            {
                areaAxis = chart.ChartAreas[2];
                areaAxis.Name = "Mollier P_w_Copy" + x.ToString();
            }
            // areaAxis = chart.ChartAreas.Add("Mollier P_w_copy" + x.ToString());
            areaAxis.BackColor = Color.Transparent;
            areaAxis.BorderColor = Color.Transparent;
            RectangleF oRect = area.Position.ToRectangleF();
            areaAxis.Position = new ElementPosition(oRect.X, oRect.Y, oRect.Width, axisHeight);
            areaAxis.InnerPlotPosition.FromRectangleF(areaSeries.InnerPlotPosition.ToRectangleF());

            // Create a copy of specified series
            Series seriesCopy = chart.Series.Add("Mollier P_w_copy" + x.ToString());
            seriesCopy.ChartType = series.ChartType;
            seriesCopy.XAxisType = alignLeft ? AxisType.Primary : AxisType.Secondary;  // (**)

            foreach (DataPoint point in series.Points)
            {
                seriesCopy.Points.AddXY(point.XValue, point.YValues[0]);
            }
            // Hide copied series
            seriesCopy.IsVisibleInLegend = false;
            seriesCopy.Color = Color.Transparent;
            seriesCopy.BorderColor = Color.Transparent;
            seriesCopy.ChartArea = areaAxis.Name;

            // Disable grid lines & tickmarks
            areaAxis.AxisY.LineWidth = 0;
            areaAxis.AxisX.LineWidth = 1;
            areaAxis.AxisY.MajorGrid.Enabled = false;
            areaAxis.AxisX.MajorGrid.Enabled = true;
            areaAxis.AxisY.MajorTickMark.Enabled = false;
            areaAxis.AxisX.MajorTickMark.Enabled = true;
            areaAxis.AxisY.LabelStyle.Enabled = false;
            areaAxis.AxisX.LabelStyle.Enabled = true;

            Axis areaAxisAxisX = alignLeft ? areaAxis.AxisX : areaAxis.AxisX2;   // (**)
            areaAxisAxisX.MajorGrid.Enabled = false;
            areaAxisAxisX.Minimum = System.Math.Round(P_w_Min, 2);
            areaAxisAxisX.Maximum = System.Math.Round(P_w_Max, 2);
            areaAxisAxisX.LabelStyle.Font = area.AxisX.LabelStyle.Font;
            areaAxisAxisX.Interval = mollierControlSettings.PartialVapourPressure;

            areaAxis.AxisY.Title = "";

            areaAxisAxisX.Title = series.Name;
            //areaAxisAxisX.LineColor = series.Color;    // (*)
            //areaAxisAxisX.TitleForeColor = Color.DarkCyan;  // (*)

            // Adjust area position
            areaAxis.Position.Y = axisY;
            areaAxis.InnerPlotPosition.Y += labelsSize;

            areaAxisAxisX.MinorTickMark.Enabled = true;
            areaAxisAxisX.MinorGrid.Enabled = false;
            areaAxisAxisX.MinorTickMark.Interval = 0.1;
        }
      
        private void setAxisGraph_Mollier()
        {

            if(mollierControlSettings == null || !mollierControlSettings.IsValid())
            {
                mollierControlSettings = new MollierControlSettings();
            }

            MollierChart.ChartAreas[0].AxisY2.MinorTickMark.Enabled = false;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Enabled = false;
            MollierChart.ChartAreas[0].AxisY.MinorTickMark.Enabled = false;
            MollierChart.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

            //INITIAL SIZES
            double pressure = mollierControlSettings.Pressure;
            double humidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            double humidityRatio_interval = mollierControlSettings.HumidityRatio_Interval;
            double temperature_Min = mollierControlSettings.Temperature_Min;
            double temperature_Max = mollierControlSettings.Temperature_Max;
            double temperature_interval = mollierControlSettings.Temperature_Interval;

            ChartType chartType = mollierControlSettings.ChartType;

            if (MinPressure > pressure || pressure > MaxPressure)
            {
                return;
            }

            //BASE CHART INITIALIZATION
            MollierChart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            MollierChart.Series.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            ChartArea chartArea_New = MollierChart.ChartAreas["ChartArea1"];
            chartArea_New.Position = new ElementPosition(2, 2, 95, 95);//define sizes of chart
            chartArea_New.InnerPlotPosition = new ElementPosition(7, 6, 88, 88);
            double partialVapourPressure_max = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Max / 1000, temperature_Max, pressure) / 1000;
            double partialVapourPressure_min = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Min / 1000, temperature_Min, pressure) / 1000;

            //OX AXIS
            Axis axisX = chartArea.AxisX;
            axisX.Title = "Humidity Ratio  x [g/kg]";
            axisX.Maximum = humidityRatio_Max;
            axisX.Minimum = humidityRatio_Min;
            axisX.Interval = humidityRatio_interval;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = true;
            axisX.MinorGrid.LineColor = Color.LightGray;
            axisX.IsReversed = false;
            axisX.LabelStyle.Format = "0.##";
            axisX.LabelStyle.Font = chartArea_New.AxisY.LabelStyle.Font;
            //areaAxisAxisY.LabelStyle.Font = area.AxisY.LabelStyle.Font;
            
            //OY AXIS
            MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.False;
            Axis axisY = chartArea.AxisY;
            axisY.MinorTickMark.Enabled = false;
            axisY.MinorGrid.Enabled = false;
            axisY.Enabled = AxisEnabled.True;
            axisY.Title = "Dry Bulb Temperature t [°C]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = temperature_Max;
            axisY.Minimum = temperature_Min;
            axisY.Interval = temperature_interval;
            axisY.LabelStyle.Format = "0.##";
            axisY.LabelStyle.Font = chartArea_New.AxisY.LabelStyle.Font;
            axisY.MajorTickMark.Enabled = false;
            //axisY.MinorTickMark.Enabled = false;

            //P_w AXIS
            Series series1 = MollierChart.Series.Add("Partial Vapour Pressure pW [kPa]");
            series1.Points.AddXY(partialVapourPressure_min, 0);
            series1.Points.AddXY(partialVapourPressure_max, 0);
            series1.ChartType = SeriesChartType.Spline;
            series1.Color = Color.Transparent;
            series1.BorderColor = Color.Transparent;
            series1.IsVisibleInLegend = false;
            CreateXAxis(MollierChart, chartArea_New, series1, 2, 80, 1, false, partialVapourPressure_min, partialVapourPressure_max);

        }
        private void setAxisGraph_Psychrometric()
        {
            //INITIAL SIZES
            ChartType chartType = mollierControlSettings.ChartType;

            double pressure = mollierControlSettings.Pressure;
            double humidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            double humidityRatio_interval = mollierControlSettings.HumidityRatio_Interval;
            double temperature_Min = mollierControlSettings.Temperature_Min;
            double temperature_Max = mollierControlSettings.Temperature_Max;
            double temperature_interval = mollierControlSettings.Temperature_Interval;

            if (MinPressure > pressure || pressure > MaxPressure)
            {
                return;
            }

            //BASE CHART INITIALIZATION

            MollierChart.ChartAreas[0].AxisY2.MinorTickMark.Enabled = false;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Enabled = false;

            MollierChart.Series?.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            ChartArea ca = MollierChart.ChartAreas[0];
            ca.Position = new ElementPosition(2, 2, 95, 95);//define sizes of chart
            ca.InnerPlotPosition = new ElementPosition(8, 6, 85, 85);
            MollierChart.ChartAreas[0].AxisX2.Enabled = AxisEnabled.False;
            MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
            MollierChart.ChartAreas[0].AxisY2.Title = "Humidity Ratio  x [g/kg]";
            MollierChart.ChartAreas[0].AxisY2.Maximum = humidityRatio_Max; //divide by /1000 if want to have kg/kg
            MollierChart.ChartAreas[0].AxisY2.Minimum = humidityRatio_Min;
            MollierChart.ChartAreas[0].AxisY2.Interval = humidityRatio_interval;
            MollierChart.ChartAreas[0].AxisY2.MajorGrid.LineColor = Color.Gray;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Interval = 1;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Enabled = true;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.LineColor = Color.LightGray;
            MollierChart.ChartAreas[0].AxisY2.LabelStyle.Format = "0.###";
            MollierChart.ChartAreas[0].AxisY2.LabelStyle.Font = ca.AxisY.LabelStyle.Font;
            double P_w_Min = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Min / 1000, temperature_Min, pressure) / 1000;
            double P_w_Max = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Max / 1000, temperature_Max, pressure) / 1000;
            
            //OX AXIS
            Axis axisX = chartArea.AxisX;
            axisX.Title = "Dry Bulb Temperature t [°C]";
            axisX.Maximum = temperature_Max;
            axisX.Minimum = temperature_Min;
            axisX.Interval = temperature_interval;
            axisX.MajorGrid.Enabled = false;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = false;
            axisX.MinorGrid.LineColor = Color.LightGray;
            axisX.LabelStyle.Font = ca.AxisY.LabelStyle.Font;
            //AXIS Y - P_w AXIS
            Axis axisY = chartArea.AxisY;

            axisY.MinorTickMark.Enabled = false;
            axisY.MinorGrid.Enabled = false;

            axisY.Enabled = AxisEnabled.False;
            axisY.Title = "Partial Vapour Pressure pW [kPa]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = humidityRatio_Max / 1000;
            //axisY.Minimum = humidityRatio_Min > humidityRatio_Max ? 0 : humidityRatio_Min / 1000;
            axisY.Minimum = humidityRatio_Min / 1000; //TODO: Fix Range
            axisY.Interval = humidityRatio_interval;
            axisY.MajorGrid.Enabled = false;
            axisY.MajorGrid.LineColor = Color.Gray;
            axisY.MinorGrid.Interval = 0.1;
            axisY.MinorGrid.Enabled = false;
            axisY.MinorGrid.LineColor = Color.LightGray;
            //axisY.MinorTickMark.Enabled = true;
            axisY.MajorTickMark.Enabled = true;


            Series series1 = MollierChart.Series.Add("Partial Vapour Pressure pW [kPa]");
            series1.Points.AddXY(0, P_w_Min);
            series1.Points.AddXY(0, P_w_Max);
            series1.ChartType = SeriesChartType.Spline;
            series1.Color = Color.Transparent;
            series1.BorderColor = Color.Transparent;
            series1.IsVisibleInLegend = false;
            CreateYAxis(MollierChart, ca, series1, 5, 12, 25, true, P_w_Min, P_w_Max);
        }


        public bool ClearObjects()
        {
            mollierPoints?.Clear();
            mollierProcesses?.Clear();
            mollierZones?.Clear();
            systems?.Clear();
            GenerateGraph();
            return true;
        }            
        public void GenerateGraph()
        {
            if (mollierControlSettings == null)
            {
                return;
            }

             
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                setAxisGraph_Mollier(); 
            }
            else if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                setAxisGraph_Psychrometric();
            }

            Modify.AddLinesSeries(MollierChart, mollierControlSettings);
            Modify.AddMollierPoints(MollierChart, mollierPoints, MollierControlSettings);  
            Modify.AddMollierZones(MollierChart, mollierZones, MollierControlSettings);
            Modify.AddDivisionArea(MollierChart, mollierPoints, mollierControlSettings);
            //mollierProcesses = Modify.AddMollierProcesses(this, MollierChart, mollierControlSettings, systems, mollierProcesses, created_points);
            mollierProcesses = Modify.AddMollierProcesses(MollierChart, this, systems, mollierProcesses, mollierControlSettings);
            Query.FindPoints(this, MollierChart, mollierControlSettings, mollierPoints);

            foreach(ChartArea chartArea in MollierChart.ChartAreas)
            {
                if(mollierControlSettings.ChartType == ChartType.Mollier)
                {
                    chartArea.Position = new ElementPosition(-2, chartArea.Position.Y, chartArea.Position.Width + 8, chartArea.Position.Height);
                }
                else
                {
                    chartArea.Position = new ElementPosition(2, chartArea.Position.Y, chartArea.Position.Width + 2, chartArea.Position.Height);
                }

            }
        }
        public List<UIMollierPoint> AddPoints(IEnumerable<IMollierPoint> mollierPoints, bool checkPressure = true)
        {
            if (mollierPoints == null)
            {
                return null;
            }

            if (this.mollierPoints == null)
            {
                this.mollierPoints = new List<UIMollierPoint>();
            }

            List<UIMollierPoint> result = new List<UIMollierPoint>();
            foreach (IMollierPoint mollierPoint in mollierPoints)
            {
                if(mollierPoint == null)
                {
                    continue;
                }

                if(checkPressure && !(mollierPoint.Pressure.AlmostEqual(mollierControlSettings.Pressure)))
                {
                    continue;
                }

                UIMollierPoint uIMollierPoint = mollierPoint as UIMollierPoint;
                if(uIMollierPoint == null)
                {
                    if (mollierPoint is MollierPoint)
                    {
                        UIMollierAppearance uIMollierAppearance = new UIMollierAppearance(Color.Blue);

                        uIMollierPoint = new UIMollierPoint((MollierPoint)mollierPoint, uIMollierAppearance);
                    }
                }

                if(uIMollierPoint == null)
                {
                    continue;
                }

                result.Add(uIMollierPoint);
            }
            this.mollierPoints.AddRange(result);

            GenerateGraph();
            
            return result;
        }
        public List<UIMollierProcess> AddProcesses(IEnumerable<IMollierProcess> mollierProcesses, bool checkPressure = true)
        {
            if (mollierProcesses == null)
            {
                return null;
            }
            if (this.mollierProcesses == null)
            {
                this.mollierProcesses = new List<UIMollierProcess>();
            }
            List<UIMollierProcess> result = new List<UIMollierProcess>();
            foreach (IMollierProcess mollierProcess_iteration in mollierProcesses)
            {
                IMollierProcess mollierProcess = mollierProcess_iteration;
                if (mollierProcess.Start == null || mollierProcess.End == null)
                {
                    continue;
                }

                if (checkPressure && !(mollierProcess.Start.Pressure.AlmostEqual(mollierControlSettings.Pressure)))
                {
                    continue;
                }

                //if (checkPressure && !Core.Query.AlmostEqual(mollierProcess.Pressure, mollierControlSettings.Pressure, Tolerance.MacroDistance))
                //{
                //    return null;
                //}
                if (mollierProcess is MollierProcess)
                {
                    UIMollierProcess mollierProcess_Temp = new UIMollierProcess((MollierProcess)mollierProcess, Color.Empty);
                    mollierProcess = mollierProcess_Temp;
                }
                UIMollierProcess uIMollierProcess = new UIMollierProcess((UIMollierProcess)mollierProcess);
                if (uIMollierProcess.MollierProcess is UndefinedProcess && uIMollierProcess.UIMollierAppearance_End.Label == null)
                {
                    uIMollierProcess.UIMollierAppearance_End.Label = "ROOM";
                }
                this.mollierProcesses.Add(uIMollierProcess);
                result.Add(uIMollierProcess);
            }
            systems = Query.ProcessSortBySystem(this.mollierProcesses);
            GenerateGraph();
            return result;
        }
        public bool AddZone(MollierZone mollierZone)
        {
            if (mollierZone == null)
            {
                //for now to create possibility to disable Zone
                mollierZones = new List<MollierZone>();
                GenerateGraph();
                return false;
            }
            if (mollierZones == null)
            {
                mollierZones = new List<MollierZone>();
            }
            mollierZones.Add(mollierZone);
            GenerateGraph();
            return true;
        }

        public bool Save(ChartExportType chartExportType, PageSize pageSize = PageSize.A4, PageOrientation pageOrientation = PageOrientation.Landscape, string path = null)
        {
            string pageType = string.Format("{0}_{1}", pageSize, pageOrientation);

            if (string.IsNullOrEmpty(path))
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    string name = mollierControlSettings.ChartType == ChartType.Mollier ? "Mollier" : "Psychrometric";
                    switch (chartExportType)
                    {
                        case ChartExportType.PDF:
                            saveFileDialog.Filter = "PDF document (*.pdf)|*.pdf|All files (*.*)|*.*";
                            name += "_" + pageType;
                            break;
                        case ChartExportType.JPG:
                            saveFileDialog.Filter = "JPG document (*.jpg)|*.jpg|All files (*.*)|*.*";
                            break;
                        case ChartExportType.EMF:
                            saveFileDialog.Filter = "EMF document (*.emf)|*.emf|All files (*.*)|*.*";
                            break;
                    }
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;
                    saveFileDialog.FileName = name;
                    if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return false;
                    }
                    path = saveFileDialog.FileName;
                }
            }
            if (chartExportType == ChartExportType.JPG)
            {
                MollierChart.SaveImage(path, ChartImageFormat.Jpeg);
                return true;
            }
            if (chartExportType == ChartExportType.EMF)
            {
                MollierChart.SaveImage(path, ChartImageFormat.Emf);
                return true;
            }
            if (chartExportType == ChartExportType.PDF)
            {
                string path_Template = Core.Query.TemplatesDirectory(typeof(Address).Assembly);
                if (!System.IO.Directory.Exists(path_Template))
                {
                    return false;
                }
                string fileName = pageOrientation == PageOrientation.Portrait ? "PDF_Print_AHU_Portrait.xlsx" : "PDF_Print_AHU.xlsx";
                path_Template = System.IO.Path.Combine(path_Template, "AHU", fileName);
                if (!System.IO.File.Exists(path_Template))
                {
                    return false;
                }

                string directory = System.IO.Path.GetDirectoryName(path_Template);
                string worksheetName = pageType;//this should be changed
                if (string.IsNullOrWhiteSpace(path_Template) || !System.IO.File.Exists(path_Template) || string.IsNullOrWhiteSpace(worksheetName))
                {
                    return false;
                }
                string path_Temp = "";
                Func<NetOffice.ExcelApi.Workbook, bool> func = new Func<NetOffice.ExcelApi.Workbook, bool>((NetOffice.ExcelApi.Workbook workbook) =>
                {
                    if (workbook == null)
                    {
                        return false;
                    }
                    string name = "AHU";
                    NetOffice.ExcelApi.Worksheet workseet_Template = Excel.Query.Worksheet(workbook, worksheetName);
                    if (workseet_Template == null)
                    {
                        return false;
                    }

                    NetOffice.ExcelApi.Worksheet worksheet = Excel.Query.Worksheet(workbook, name);

                    if (worksheet != null)
                    {
                        worksheet.Delete();
                    }


                    worksheet = Excel.Modify.Copy(workseet_Template, name);

                    NetOffice.ExcelApi.Range range = null;


                    HashSet<string> uniqueNames = new HashSet<string>();
                    uniqueNames.Add("[ProcessPointName]");
                    uniqueNames.Add("[DryBulbTemperature]");
                    uniqueNames.Add("[HumidityRatio]");
                    uniqueNames.Add("[RelativeHumidity]");
                    uniqueNames.Add("[WetBulbTemperature]");
                    uniqueNames.Add("[SaturationTemperature]");
                    uniqueNames.Add("[Enthalpy]");
                    uniqueNames.Add("[Density]");
                    uniqueNames.Add("[AtmosphericPressure]");
                    uniqueNames.Add("[SpecificVolume]");
                    uniqueNames.Add("[ProcessName]");
                    uniqueNames.Add("[deltaT]");
                    uniqueNames.Add("[deltaX]");
                    uniqueNames.Add("[deltaH]");
                    int numberOfData = 14;
                    int columnIndex_Min = 100;
                    int rowIndex_Min = 100;
                    Dictionary<string, NetOffice.ExcelApi.Range> dictionary = new Dictionary<string, NetOffice.ExcelApi.Range>();
                    object[,] values = worksheet.Range(worksheet.Cells[1, 1], worksheet.Cells[100, 30]).Value as object[,];
                    for (int i = values.GetLowerBound(0); i <= values.GetUpperBound(0); i++)
                    {
                        for (int j = values.GetLowerBound(1); j <= values.GetUpperBound(1); j++)
                        {
                            if (!(values[i, j] is string))
                            {
                                continue;
                            }

                            string rangeValue = values[i, j] as string;
                            if (string.IsNullOrWhiteSpace(rangeValue))
                            {
                                continue;
                            }
                            foreach (string name_Temp in uniqueNames)
                            {
                                if (rangeValue == name_Temp)
                                {
                                    if (j < columnIndex_Min)
                                    {
                                        columnIndex_Min = j;
                                        rowIndex_Min = i;
                                    }
                                    dictionary.Add(name_Temp, worksheet.Cells[i, j]);
                                    break;
                                }
                            }
                        }
                    }

                    if (systems == null || systems.Count == 0)
                    {
                        for (int i = 0; i < numberOfData; i++)
                        {
                            worksheet.Cells[rowIndex_Min, columnIndex_Min + i].Value = string.Empty;
                        }
                    }
                    else
                    {
                        NetOffice.ExcelApi.Range range_1 = worksheet.Range(worksheet.Cells[rowIndex_Min, columnIndex_Min], worksheet.Cells[rowIndex_Min, columnIndex_Min + numberOfData]);
                        int move_Temp = 1;
                        for (int i = 0; i < systems.Count; i++)
                        {
                            range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                            move_Temp++;
                            for (int j = 0; j < systems[i].Count; j++)
                            {
                                UIMollierProcess UI_MollierProcess = systems[i][j];
                                MollierProcess mollierProcess = UI_MollierProcess.MollierProcess;
                                if (UI_MollierProcess.UIMollierAppearance_Start.Label != null && UI_MollierProcess.UIMollierAppearance_Start.Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                                if (UI_MollierProcess.UIMollierAppearance_End.Label != null && UI_MollierProcess.UIMollierAppearance_End.Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                            }
                            if (i != systems.Count - 1)
                            {
                                range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                            }
                            move_Temp++;
                        }

                        //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                        foreach (string key_Temp in uniqueNames)
                        {
                            if (!dictionary.ContainsKey(key_Temp))
                            {
                                continue;
                            }

                            NetOffice.ExcelApi.Range range_Temp = dictionary[key_Temp];
                            int columnIndex = range_Temp.Column;
                            int rowIndex = range_Temp.Row;
                            int id = 0;

                            for (int i = 0; i < systems.Count; i++)
                            {
                                //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                worksheet.Cells[rowIndex + id, columnIndex].Value = "----";
                                id++;
                                for (int j = 0; j < systems[i].Count; j++)
                                {
                                    UIMollierProcess UI_MollierProcess = systems[i][j];
                                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess;
                                    MollierPoint start = mollierProcess.Start;
                                    MollierPoint end = mollierProcess.End;
                                    string value_1 = string.Empty;
                                    string value_2 = string.Empty;
                                    switch (key_Temp)
                                    {
                                        case "[ProcessPointName]":
                                            value_1 = UI_MollierProcess.UIMollierAppearance_Start.Label;
                                            value_2 = UI_MollierProcess.UIMollierAppearance_End.Label;
                                            break;
                                        case "[DryBulbTemperature]":
                                            value_1 = System.Math.Round(start.DryBulbTemperature, 2).ToString();
                                            value_2 = System.Math.Round(end.DryBulbTemperature, 2).ToString();
                                            break;
                                        case "[HumidityRatio]":
                                            value_1 = System.Math.Round(start.HumidityRatio * 1000, 2).ToString();
                                            value_2 = System.Math.Round(end.HumidityRatio * 1000, 2).ToString();
                                            break;
                                        case "[RelativeHumidity]":
                                            value_1 = System.Math.Round(start.RelativeHumidity, 1).ToString();
                                            value_2 = System.Math.Round(end.RelativeHumidity, 1).ToString();
                                            break;
                                        case "[WetBulbTemperature]":
                                            value_1 = System.Math.Round(start.WetBulbTemperature(), 2).ToString();
                                            value_2 = System.Math.Round(end.WetBulbTemperature(), 2).ToString();
                                            break;
                                        case "[SaturationTemperature]":
                                            value_1 = System.Math.Round(start.SaturationTemperature(), 2).ToString();
                                            value_2 = System.Math.Round(end.SaturationTemperature(), 2).ToString();
                                            break;
                                        case "[Enthalpy]":
                                            value_1 = System.Math.Round(start.Enthalpy / 1000, 2).ToString();
                                            value_2 = System.Math.Round(end.Enthalpy / 1000, 2).ToString();
                                            break;
                                        case "[SpecificVolume]":
                                            value_1 = System.Math.Round(start.SpecificVolume(), 3).ToString();
                                            value_2 = System.Math.Round(end.SpecificVolume(), 3).ToString();
                                            break;
                                        case "[Density]":
                                            value_1 = System.Math.Round(start.Density(), 3).ToString();
                                            value_2 = System.Math.Round(end.Density(), 3).ToString();
                                            break;
                                        case "[AtmosphericPressure]":
                                            value_1 = System.Math.Round(start.Pressure, 1).ToString();
                                            value_2 = System.Math.Round(end.Pressure, 1).ToString();
                                            break;
                                        case "[ProcessName]":
                                            value_2 = Query.FullProcessName(UI_MollierProcess);
                                            break;
                                        case "[deltaT]":
                                            value_2 = (System.Math.Round(end.DryBulbTemperature, 2) - System.Math.Round(start.DryBulbTemperature, 2)).ToString();
                                            break;
                                        case "[deltaX]":
                                            value_2 = (System.Math.Round(end.HumidityRatio * 1000, 2) - System.Math.Round(start.HumidityRatio * 1000, 2)).ToString();
                                            break;
                                        case "[deltaH]":
                                            value_2 = (System.Math.Round(end.Enthalpy / 1000, 2) - System.Math.Round(start.Enthalpy / 1000, 2)).ToString();
                                            break;
                                    }



                                    if (UI_MollierProcess.UIMollierAppearance_Start.Label != null && UI_MollierProcess.UIMollierAppearance_Start.Label != "")
                                    {
                                        if (value_1 != string.Empty)
                                        {
                                            //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = value_1;
                                            id++;
                                        }
                                        else if (key_Temp == "[ProcessName]" || key_Temp == "[deltaT]" || key_Temp == "[deltaX]" || key_Temp == "[deltaH]")
                                        {
                                            //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = "-";
                                            id++;
                                        }
                                    }
                                    if (UI_MollierProcess.UIMollierAppearance_End.Label != null && UI_MollierProcess.UIMollierAppearance_End.Label != "")
                                    {
                                        //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                        if (value_2 != string.Empty)
                                        {
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = value_2;
                                            id++;
                                        }
                                        else
                                        {
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = "-";
                                            id++;
                                        }
                                    }
                                }
                                //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                if (key_Temp == "[ProcessName]")
                                {
                                    int integer = 2;
                                }
                                worksheet.Cells[rowIndex + id, columnIndex].Value = "----";
                                id++;
                            }

                        }
                    }

                    range = Excel.Query.Range(worksheet.UsedRange, pageType);

                    if (range == null)
                    {
                        return false;
                    }
                    float left = (float)(double)range.Left;
                    float top = (float)(double)range.Top;

                    float width = (float)(double)range.Width;
                    float height = (float)(double)range.Height;

                    path_Temp = System.IO.Path.GetTempFileName();

                    Size size_Temp = Size;
                    //Size = new Size(System.Convert.ToInt32(width), System.Convert.ToInt32(height));

                    if (pageSize == PageSize.A3)//a3 pdf
                    {
                        Size = new Size(System.Convert.ToInt32(width * 1.4), System.Convert.ToInt32(height * 1.4));
                    }
                    else//a4 pdf
                    {
                        Size = new Size(System.Convert.ToInt32(width * 2), System.Convert.ToInt32(height * 2));
                    }
                    Save(ChartExportType.EMF, path: path_Temp);

                    Size = size_Temp;


                    NetOffice.ExcelApi.Shape shape = worksheet.Shapes.AddPicture(path_Temp, NetOffice.OfficeApi.Enums.MsoTriState.msoFalse, NetOffice.OfficeApi.Enums.MsoTriState.msoCTrue, left, top, width, height);

                    //double shapeSizeFactor = Query.ShapeSizeFactor(DeviceDpi);

                    shape.PictureFormat.Crop.ShapeHeight = (float)(shape.PictureFormat.Crop.ShapeHeight * Query.ShapeSizeFactor(DeviceDpi, 0.81));
                    shape.PictureFormat.Crop.ShapeWidth = (float)(shape.PictureFormat.Crop.ShapeWidth * Query.ShapeSizeFactor(DeviceDpi, 0.78));
                    shape.Width = width;
                    shape.Height = height;
                    range.Value = string.Empty;

                    workbook.SaveCopyAs(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "TEST.xlsx"));

                    worksheet.ExportAsFixedFormat(NetOffice.ExcelApi.Enums.XlFixedFormatType.xlTypePDF, path);

                    return false;
                });

                Excel.Modify.Edit(path_Template, func);

                System.Threading.Thread.Sleep(1000);

                if (System.IO.File.Exists(path_Temp))
                {
                    if (Core.Query.WaitToUnlock(path_Temp))
                    {
                        System.IO.File.Delete(path_Temp);
                    }
                }
                return true;
            }

            return true;
        }
        public bool Print()
        {
            PrintingManager printingManager = MollierChart?.Printing;
            if (printingManager == null)
            {
                return false;
            }

            printingManager.Print(true);
            return true;
        }

        private void ToolStripMenuItem_ProcessesAndPoints_Click(object sender, EventArgs e)
        {
            if (mollierPoints == null && mollierProcesses == null)
            {
                MessageBox.Show("There is no process or point to zoom");
                return;
            }
            ChartType chartType = mollierControlSettings.ChartType;
            Query.ZoomParameters(MollierChart.Series, chartType, out double x_Min, out double x_Max, out double y_Min, out double y_Max);
            mollierControlSettings.HumidityRatio_Min = chartType == ChartType.Mollier ? x_Min : y_Min;
            mollierControlSettings.HumidityRatio_Max = chartType == ChartType.Mollier ? x_Max : y_Max;
            mollierControlSettings.Temperature_Min = chartType == ChartType.Mollier ? y_Min : x_Min;
            mollierControlSettings.Temperature_Max = chartType == ChartType.Mollier ? y_Max : x_Max;
            GenerateGraph();
        }
        private void ToolStripMenuItem_Selection_Click(object sender, EventArgs e)
        {
            selection = true;
        }
        private void ToolStripMenuItem_Reset_Click(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings_1 = new MollierControlSettings();
            mollierControlSettings.HumidityRatio_Min = mollierControlSettings_1.HumidityRatio_Min;
            mollierControlSettings.HumidityRatio_Max = mollierControlSettings_1.HumidityRatio_Max;
            mollierControlSettings.Temperature_Min = mollierControlSettings_1.Temperature_Min;
            mollierControlSettings.Temperature_Max = mollierControlSettings_1.Temperature_Max;
            GenerateGraph();
        }
        private void MollierChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (!selection)
            {
                return;
            }
            mdown = e.Location;
        }
        private void MollierChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (selection)
            {
                if (e.Button == MouseButtons.Left)
                {
                    MollierChart.Refresh();
                    using (Graphics g = MollierChart.CreateGraphics())
                        g.DrawRectangle(Pens.Red, Query.Rectangle(mdown, e.Location));
                }

                return;
            }

            foreach (Tuple<Series, int> seriesData_Temp in seriesData)
            {
                seriesData_Temp.Item1.BorderWidth = seriesData_Temp.Item2;
            }

            seriesData.Clear();

            Point point = e.Location;

            HitTestResult[] hitTestResults = MollierChart?.HitTest(point.X, point.Y, false, ChartElementType.DataPoint);
            if (hitTestResults == null)
            {
                return;
            }

            foreach (HitTestResult hitTestResult in hitTestResults)
            {
                Series series = hitTestResult?.Series;
                if (series == null)
                {
                    continue;
                }

                seriesData.Add(new Tuple<Series, int>(series, series.BorderWidth));

                if(series.Tag is MollierProcess)
                {
                    series.BorderWidth += 2;
                }
                else
                {
                    series.BorderWidth += 1;
                }

                //int index = hitTestResult.PointIndex;
                //if(index >= 0)
                //{
                //    DataPoint dataPoint = series.Points[index];
                //    if(dataPoint != null)
                //    {
                //        dataPoint.BorderWidth += 2;
                //    }
                //}
            }
        }
        private void MollierChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (!selection)
            {
                return;
            }
            Axis ax = MollierChart.ChartAreas[0].AxisX;
            Axis ay = MollierChart.ChartAreas[0].AxisY;
            Point mup = e.Location;
            //chart sizes(axis)
            double X_Min = mollierControlSettings.ChartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Min : mollierControlSettings.Temperature_Min;
            double Y_Min = mollierControlSettings.ChartType == ChartType.Mollier ? mollierControlSettings.Temperature_Min : mollierControlSettings.HumidityRatio_Min;
            double X_Max = mollierControlSettings.ChartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Max : mollierControlSettings.Temperature_Max;
            double Y_Max = mollierControlSettings.ChartType == ChartType.Mollier ? mollierControlSettings.Temperature_Max : mollierControlSettings.HumidityRatio_Max;

            ////new selection area
            if (mup.X < 0 || mup.Y < 0)
            {
                MollierChart.Refresh();
                return;
            }
            double x_Min = System.Math.Min((double)ax.PixelPositionToValue(mup.X), (double)ax.PixelPositionToValue(mdown.X));
            double x_Max = System.Math.Max((double)ax.PixelPositionToValue(mup.X), (double)ax.PixelPositionToValue(mdown.X));
            double y_Min = System.Math.Min((double)ay.PixelPositionToValue(mup.Y), (double)ay.PixelPositionToValue(mdown.Y));
            double y_Max = System.Math.Max((double)ay.PixelPositionToValue(mup.Y), (double)ay.PixelPositionToValue(mdown.Y));

			//Rounding MD 2023-06-26
            //y_Min = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Round(y_Min) : System.Math.Round(y_Min * 1000) / 1000;
            //y_Max = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Round(y_Max) : System.Math.Round(y_Max * 1000) / 1000;
            //x_Min = System.Math.Round(x_Min);
            //x_Max = System.Math.Round(x_Max);
			//


            double x_Difference = x_Max - x_Min;
            double y_Difference = mollierControlSettings.ChartType == ChartType.Mollier ? y_Max - y_Min : (y_Max - y_Min) * 1000;
            if (x_Difference < 1 || y_Difference < 1)
            {
                MollierChart.Refresh();
                return;
            }

            mollierControlSettings.HumidityRatio_Min = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Max(x_Min, X_Min) : System.Math.Max(y_Min * 1000, Y_Min);
            mollierControlSettings.HumidityRatio_Max = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Min(x_Max, X_Max) : System.Math.Min(y_Max * 1000, Y_Max);
            mollierControlSettings.Temperature_Min = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Max(y_Min, Y_Min) : System.Math.Max(x_Min, X_Min);
            mollierControlSettings.Temperature_Max = mollierControlSettings.ChartType == ChartType.Mollier ? System.Math.Min(y_Max, Y_Max) : System.Math.Min(x_Max, X_Max);

            mollierControlSettings.HumidityRatio_Min = System.Math.Round(mollierControlSettings.HumidityRatio_Min);
            mollierControlSettings.HumidityRatio_Max = System.Math.Round(mollierControlSettings.HumidityRatio_Max);
            mollierControlSettings.Temperature_Min = System.Math.Round(mollierControlSettings.Temperature_Min);
            mollierControlSettings.Temperature_Max = System.Math.Round(mollierControlSettings.Temperature_Max);


            GenerateGraph();

            MollierChart.Refresh();
            selection = false;
        }
        private void MollierChart_MouseClick(object sender, MouseEventArgs e)
        {
            MollierPoint mollierPoint = GetMollierPoint(e.X, e.Y);

            MollierPointSelected?.Invoke(this, new MollierPointSelectedEventArgs(mollierPoint));
        }
        private void MollierControl_SizeChanged(object sender, EventArgs e)
        {
            if (mollierControlSettings == null)
            {
                return;
            }

            GenerateGraph();
        }
  

        public MollierControlSettings MollierControlSettings
        {
            get
            {
                if (mollierControlSettings == null)
                {
                    return null;
                }

                return new MollierControlSettings(mollierControlSettings);
            }
            set
            {
                if (value == null)
                {
                    mollierControlSettings = null;
                }

                mollierControlSettings = new MollierControlSettings(value);
                GenerateGraph();
            }
        }
        public List<UIMollierPoint> UIMollierPoints
        {
            get
            {
                if (mollierPoints == null)
                {
                    return null;
                }

                return mollierPoints.ConvertAll(x => x?.Clone());
            }
        }
        public List<UIMollierProcess> UIMollierProcesses
        {
            get
            {
                if (mollierProcesses == null)
                {
                    return null;
                }

                return mollierProcesses.ConvertAll(x => x?.Clone());
            }
        }
        public MollierPoint GetMollierPoint(int x, int y)
        {
            double chartX = MollierChart.ChartAreas[0].AxisX.PixelPositionToValue(x);
            double chartY = MollierChart.ChartAreas[0].AxisY.PixelPositionToValue(y);

            return Query.MollierPoint(chartX, chartY, mollierControlSettings);
        }
    }
}
