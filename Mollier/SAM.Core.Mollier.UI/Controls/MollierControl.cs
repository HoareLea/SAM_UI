using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Core.Mollier.UI.Forms;
using SAM.Geometry.Mollier;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierControl : UserControl
    {
        Core.UI.HooverTimer hooverTimer;
        
        public event MollierPointSelectedEventHandler MollierPointSelected;

        private Point mdown = Point.Empty;
        private bool selection = false;
        private MollierControlSettings mollierControlSettings;
        private MollierModel mollierModel = new MollierModel();
        private List<Tuple<Series, int>> seriesData = new List<Tuple<Series, int>>();

        private int selectedObjectWidth_Process = 4;
        private int selectedObjectWidth_Default = 2;

        public bool EnableHoover
        {
            get
            {
                return hooverTimer != null && hooverTimer.Enabled;
            }

            set
            {
                if(value)
                {
                    if(hooverTimer == null)
                    {
                        hooverTimer = new Core.UI.HooverTimer(MollierChart, 500);
                        hooverTimer.Update += HooverTimer_Update;
                    }

                    hooverTimer.Enabled = value;
                }
                else
                {
                    if(hooverTimer != null)
                    {
                        hooverTimer.Update -= HooverTimer_Update;
                        hooverTimer.Control = null;
                        hooverTimer = null;
                    }
                }
            }
        }

        public MollierControl()
        {
            InitializeComponent();

            mollierControlSettings = new MollierControlSettings();


        }

        private void HooverTimer_Update(object sender, MouseEventArgs e)
        {
            foreach (Tuple<Series, int> seriesData_Temp in seriesData)
            {
                seriesData_Temp.Item1.BorderWidth = seriesData_Temp.Item2;
            }

            seriesData.Clear();

            if(!hooverTimer.Enabled)
            {
                return;
            }

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

                if (series.Tag is MollierProcess)
                {
                    series.BorderWidth += selectedObjectWidth_Process;
                }
                else
                {
                    series.BorderWidth += selectedObjectWidth_Default;
                }

            }
        }

        private void CreateYAxis()
        {
            ChartArea chartArea = MollierChart.ChartAreas[0];
            Axis axisY = chartArea.AxisY;

            double humidityRatioMin = mollierControlSettings.HumidityRatio_Min;
            double humidityRatioMax = mollierControlSettings.HumidityRatio_Max;

            double partialVapourPressureMin = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatioMin, 45, mollierControlSettings.Pressure) / 1000;
            double partialVapourPressureMax = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatioMax, 45, mollierControlSettings.Pressure) / 1000;

            axisY.Minimum = chartArea.AxisY2.Minimum;
            axisY.Maximum = chartArea.AxisY2.Maximum;
            if (!mollierControlSettings.PartialVapourPressure_Axis)
            {
                axisY.MinorTickMark.Enabled = false;
                axisY.MinorGrid.Enabled = false;
                axisY.LabelStyle.Enabled = false;
                axisY.MajorGrid.Enabled = false;
                axisY.MajorTickMark.Enabled = false;
                axisY.Title = "";
                return;
            }

            axisY.Enabled = AxisEnabled.True;
            axisY.MinorTickMark.Enabled = false;
            axisY.MinorGrid.Enabled = false;
            axisY.Title = "Partial Vapour Pressure pW [kPa]";
            axisY.Interval = mollierControlSettings.PartialVapourPressure_Interval;
            axisY.MajorGrid.Enabled = false;
            axisY.MajorGrid.LineColor = Color.Gray;
            axisY.MinorGrid.Interval = 0.1;
            axisY.MinorGrid.Enabled = false;
            axisY.MinorGrid.LineColor = Color.LightGray;
            axisY.MajorTickMark.Enabled = false;
            axisY.LabelStyle.Enabled = true;

            axisY.CustomLabels.Clear();
            for (double i = 0; i <= partialVapourPressureMax; i += axisY.Interval)
            {
                if (i < partialVapourPressureMin)
                {
                    continue;
                }

                double labelPositionY = Mollier.Query.HumidityRatio_ByPartialVapourPressure(i * 1000, mollierControlSettings.Pressure) * 1000;
                CustomLabel lbl = new CustomLabel(labelPositionY - 1.6, labelPositionY + 1.6, i.ToString(), 0, LabelMarkStyle.LineSideMark);
                axisY.CustomLabels.Add(lbl);

                // Partial vapour pressure axis minor tick mark series 
                Series seriesTemp = MollierChart.Series.Add(Guid.NewGuid() + "Pw minor Tick Mark Mollier");
                seriesTemp.IsVisibleInLegend = false;
                seriesTemp.ChartType = SeriesChartType.Line;
                double xFactor = Query.ScaleVector2D(this, mollierControlSettings).X;
                seriesTemp.Points.AddXY(MollierChart.ChartAreas[0].AxisX.Minimum, labelPositionY);
                seriesTemp.Points.AddXY(MollierChart.ChartAreas[0].AxisX.Minimum + 0.5 * xFactor, labelPositionY);
                seriesTemp.Color = Color.Black;
            }
        }
       
        private void CreateXAxis()
        {
            double humidityRatioMin = mollierControlSettings.HumidityRatio_Min;
            double humidityRatioMax = mollierControlSettings.HumidityRatio_Max;

            double partialVapourPressureMin = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatioMin, 45, mollierControlSettings.Pressure) / 1000;
            double partialVapourPressureMax = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatioMax, 45, mollierControlSettings.Pressure) / 1000;
            MollierChart.ChartAreas[0].AxisX2.Enabled = AxisEnabled.False;

            MollierChart.ApplyPaletteColors();
            Axis axisX2 = MollierChart.ChartAreas[0].AxisX2;
            Axis axisX = MollierChart.ChartAreas[0].AxisX;
            Axis axisY = MollierChart.ChartAreas[0].AxisY;

            if (!mollierControlSettings.PartialVapourPressure_Axis)
            {
                axisX2.Enabled = AxisEnabled.True;
                axisX2.LabelStyle.Enabled = false;
                axisX2.Title = "";
                return;
            }

            axisX2.Enabled = AxisEnabled.True;
            axisX2.MinorTickMark.Enabled = false;
            axisX2.MajorGrid.Enabled = false;
            axisX2.MinorGrid.Enabled = false;
            axisX2.MajorTickMark.Enabled = false;
            axisX2.LabelStyle.Enabled = true;
            axisX2.LabelStyle.Font = axisY.LabelStyle.Font;
            axisX2.Title = "Partial Vapour Pressure pW [kPa]";
            axisX2.Minimum = partialVapourPressureMin;
            axisX2.Maximum = partialVapourPressureMax;
            axisX2.Interval = partialVapourPressureMax;// To disable default labels 

            double partialVapourPressure_Interval = mollierControlSettings.PartialVapourPressure_Interval;

            axisX2.CustomLabels.Clear();
            for (double i = 0; i <= partialVapourPressureMax; i += partialVapourPressure_Interval)
            {
                if (i < axisX2.Minimum)
                {
                    continue;
                }

                double labelPositionX = Mollier.Query.HumidityRatio_ByPartialVapourPressure(i * 1000, mollierControlSettings.Pressure) * 1000;
                double ratio = (labelPositionX - axisX.Minimum) / (axisX.Maximum - axisX.Minimum);
                double labelAxisPosition = partialVapourPressureMin + ratio * (axisX2.Maximum - axisX2.Minimum);
                CustomLabel lbl = new CustomLabel(labelAxisPosition - 1.6, labelAxisPosition + 1.6, i.ToString(), 0, LabelMarkStyle.LineSideMark);
                axisX2.CustomLabels.Add(lbl);

                // Partial vapour pressure axis minor tick mark series 
                Series seriesTemp = MollierChart.Series.Add(Guid.NewGuid() + "Pw minor Tick Mark Mollier");
                seriesTemp.IsVisibleInLegend = false;
                seriesTemp.ChartType = SeriesChartType.Line;
                double yFactor = Query.ScaleVector2D(this, mollierControlSettings).Y;
                seriesTemp.Points.AddXY(labelPositionX, axisY.Maximum);
                seriesTemp.Points.AddXY(labelPositionX, axisY.Maximum - 0.7 * yFactor);
                seriesTemp.Color = Color.Black;
            }

        }
        
        private void setAxisGraph_Mollier()
        {
            if (mollierControlSettings == null || !mollierControlSettings.IsValid())
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

            if (Limit.Pressure_Min > pressure || pressure > Limit.Pressure_Max)
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

            //OX AXIS
            Axis axisX = chartArea.AxisX;
            axisX.Title = "Humidity Ratio x [g/kg]";
            axisX.Maximum = humidityRatio_Max * 1000;
            axisX.Minimum = humidityRatio_Min * 1000;
            axisX.Interval = humidityRatio_interval * 1000;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = true;
            axisX.MinorGrid.LineColor = Color.LightGray;
            axisX.IsReversed = false;
            axisX.LabelStyle.Format = "0.##";
            axisX.LabelStyle.Font = chartArea_New.AxisY.LabelStyle.Font;

            //OY AXIS
            MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.False;
            Axis axisY = chartArea.AxisY;
            axisY.CustomLabels.Clear();
            axisY.MinorTickMark.Enabled = false;

            axisY.MinorGrid.Enabled = false;
            axisY.Enabled = AxisEnabled.True;
            axisY.Title = "Dry Bulb Temperature t [°C]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Minimum = temperature_Min;
            axisY.Maximum = temperature_Max;
            axisY.Interval = temperature_interval;
            axisY.LabelStyle.Format = "0.##";
            axisY.LabelStyle.Font = chartArea_New.AxisY.LabelStyle.Font;
            axisY.MajorTickMark.Enabled = false;
            axisY.LabelStyle.Enabled = true;
            axisY.MajorTickMark.Enabled = true;

            CreateXAxis();

        }
        
        private void setAxisGraph_Psychrometric()
        {
            //INITIAL SIZES

            double pressure = mollierControlSettings.Pressure;
            double humidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            double humidityRatio_interval = mollierControlSettings.HumidityRatio_Interval;
            double temperature_Min = mollierControlSettings.Temperature_Min;
            double temperature_Max = mollierControlSettings.Temperature_Max;
            double temperature_interval = mollierControlSettings.Temperature_Interval;

            if (Limit.Pressure_Min > pressure || pressure > Limit.Pressure_Max)
            {
                return;
            }

            //BASE CHART INITIALIZATION

            MollierChart.Series?.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            chartArea.Position = new ElementPosition(2, 2, 95, 95);//define sizes of chart
            chartArea.InnerPlotPosition = new ElementPosition(7, 6, 88, 88);
            chartArea.AxisX2.Enabled = AxisEnabled.False;

            // Humidity ratio axis
            chartArea.AxisY2.MinorTickMark.Enabled = false;
            chartArea.AxisY2.MinorGrid.Enabled = false;
            chartArea.AxisY2.Enabled = AxisEnabled.True;
            chartArea.AxisY2.Title = "Humidity Ratio  x [g/kg]";
            chartArea.AxisY2.Maximum = humidityRatio_Max * 1000;
            chartArea.AxisY2.Minimum = humidityRatio_Min * 1000;
            chartArea.AxisY2.Interval = humidityRatio_interval * 1000;
            chartArea.AxisY2.MajorGrid.LineColor = Color.Gray;
            chartArea.AxisY2.MinorGrid.Interval = 1;
            chartArea.AxisY2.MinorGrid.Enabled = true;
            chartArea.AxisY2.MinorGrid.LineColor = Color.LightGray;
            chartArea.AxisY2.LabelStyle.Format = "0.###";
            chartArea.AxisY2.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font;

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
            axisX.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font;

            CreateYAxis();
        }

        public bool ClearObjects(bool regenerate = true)
        {
            mollierModel?.Clear();
            if(regenerate)
            {
                Regenerate();
            }

            return true;
        }

        public void Regenerate()
        {
            if (mollierControlSettings == null)
            {
                return;
            }

            MollierChart.Series.SuspendUpdates();

            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                setAxisGraph_Mollier();

            }
            else if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                setAxisGraph_Psychrometric();
            }

            //Add Chart Lines
            MollierChart.AddLinesSeries(mollierControlSettings);
            //Add Chart Lables : Missing
            
            MollierChart.AddMollierLines(mollierModel, mollierControlSettings);

            MollierChart.AddMollierPoints(mollierModel, mollierControlSettings);
            MollierChart.AddMollierProcesses(mollierModel, mollierControlSettings);
            MollierChart.AddMollierZones(mollierModel, mollierControlSettings);
            MollierChart.AddDivisionArea(mollierModel, mollierControlSettings);
            MollierChart.AddColorPoint(mollierModel, mollierControlSettings);

            MollierChart.AddLabels(mollierControlSettings);


            foreach (ChartArea chartArea in MollierChart.ChartAreas)
            {
                if (mollierControlSettings.ChartType == ChartType.Mollier)
                {
                    chartArea.Position = new ElementPosition(-2, chartArea.Position.Y, chartArea.Position.Width + 8, chartArea.Position.Height);
                }
                else
                {
                    chartArea.Position = new ElementPosition(0, chartArea.Position.Y, chartArea.Position.Width + 2, chartArea.Position.Height);
                }
            }

            //TODO: [MACIEK] Solve order issue, Organize Series Tags to allow easy way of filtering Items. in this case Spline (Relative Humidity series) have to be move to the top of series
            List<Series> series_RelativeHumidity = new List<Series>();
            List<Series> series_CoolingProcessDash = new List<Series>();

            List<object> objects = new List<object>();
            foreach (Series series_Temp in MollierChart.Series)
            {
                ConstantValueCurve constantValueCurve = series_Temp.Tag as ConstantValueCurve;
                if (constantValueCurve != null && constantValueCurve.ChartDataType == ChartDataType.RelativeHumidity)
                {
                    series_RelativeHumidity.Add(series_Temp);
                }

                UIMollierProcess mollierProcess = series_Temp.Tag as UIMollierProcess;
                if (mollierProcess?.MollierProcess is CoolingProcess && series_Temp.ChartType == SeriesChartType.Point)
                {
                    series_CoolingProcessDash.Add(series_Temp);
                }

                objects.Add(series_Temp.Tag);
            }

            object @object = objects.FindLast(x => x is ConstantValueCurve);

            int index = objects.IndexOf(@object);

            foreach (Series series_Temp in series_CoolingProcessDash)
            {
                MollierChart.Series.Remove(series_Temp);
                MollierChart.Series.Insert(index, series_Temp);
            }

            foreach (Series series_Temp in series_RelativeHumidity)
            {
                MollierChart.Series.Remove(series_Temp);
                MollierChart.Series.Insert(index, series_Temp);
            }

            MollierChart.Series.ResumeUpdates();
            MollierChart.Series.Invalidate();
        }
        
        public List<IUIMollierObject> AddMollierObjects<T>(IEnumerable<T> mollierObjects, bool checkPressure = true, bool regenerate = true) where T : IMollierObject
        {
            if (mollierObjects == null || mollierObjects.Count() == 0 || mollierModel == null)
            {
                return null;
            }

            List<IUIMollierObject> result = new List<IUIMollierObject>();
            foreach (IMollierObject mollierObject in mollierObjects)
            {
                if (mollierObject is IMollierPoint)
                {
                    result.AddRange(AddPoints(new List<IMollierPoint>() { (IMollierPoint)mollierObject }));
                }
                else if (mollierObject is IMollierProcess)
                {
                    result.AddRange(AddProcesses(new List<IMollierProcess>() { (IMollierProcess)mollierObject }));
                }
                else if (mollierObject is IMollierGroup)
                {
                    result.AddRange(AddGroups(new List<IMollierGroup>() { (IMollierGroup)mollierObject }));
                }
                else if (mollierObject is IMollierZone)
                {
                    result.AddRange(AddZones(new List<IMollierZone>() { (IMollierZone)mollierObject }));
                }
                else if (mollierObject is IMollierCurve)
                {
                    result.AddRange(AddCurves(new List<IMollierCurve>() { (IMollierCurve)mollierObject }));
                }
            }

            if(regenerate)
            {
                Regenerate();
            }
            return result;
        }
        
        private List<UIMollierPoint> AddPoints(IEnumerable<IMollierPoint> mollierPoints, bool checkPressure = true)
        {
            if (mollierPoints == null || mollierModel == null)
            {
                return null;
            }

            List<UIMollierPoint> result = new List<UIMollierPoint>();
            foreach (IMollierPoint mollierPoint in mollierPoints)
            {
                if (mollierPoint == null)
                {
                    continue;
                }
                
                if (checkPressure && !(mollierPoint.Pressure.AlmostEqual(mollierControlSettings.Pressure)))
                {
                    continue;
                }

                UIMollierPoint uIMollierPoint = mollierPoint as UIMollierPoint;
                if(uIMollierPoint == null)
                {
                    MollierPoint mollierPoint_Temp = mollierPoint as MollierPoint;
                    UIMollierPointAppearance uIMollierPointAppearance = Create.UIMollierPointAppearance(DisplayPointType.Default, string.Empty);
                    uIMollierPoint = new UIMollierPoint(mollierPoint_Temp, uIMollierPointAppearance);
                }

                result.Add(uIMollierPoint);
            }

            mollierModel.AddRange(result);
            return result;
        }
        
        private List<UIMollierProcess> AddProcesses(IEnumerable<IMollierProcess> mollierProcesses, bool checkPressure = true)
        {
            if (mollierProcesses == null || mollierModel == null)
            {
                return null;
            }

            List<UIMollierProcess> result = new List<UIMollierProcess>();
            foreach (IMollierProcess mollierProcess in mollierProcesses)
            {
                if (mollierProcess == null || mollierProcess.Start == null || mollierProcess.End == null)
                {
                    continue;
                }
                if (checkPressure && !(mollierProcess.Start.Pressure.AlmostEqual(mollierControlSettings.Pressure)))
                {
                    continue;
                }

                if(mollierProcess is UIMollierProcess)
                {
                    result.Add((UIMollierProcess)mollierProcess);
                    continue;
                }

                MollierProcess mollierProcess_Temp = (MollierProcess)mollierProcess;
                if(mollierProcess_Temp == null)
                {
                    continue;
                }


                Color color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess_Temp);
                string label = Query.ProcessName(mollierProcess);
                bool visible = true;

                UIMollierAppearance uIMollierAppearance = new UIMollierAppearance(color, label) { Visible = visible };

                UIMollierProcess uIMollierProcess = new UIMollierProcess(mollierProcess_Temp, uIMollierAppearance);
                UIMollierPointAppearance uIMollierPointAppearance = Create.UIMollierPointAppearance(DisplayPointType.Process);
                //uIMollierPointAppearance.Color = color;
                //uIMollierPointAppearance.BorderColor = Color.Gray;

                uIMollierProcess.UIMollierPointAppearance_Start = uIMollierPointAppearance;
                uIMollierProcess.UIMollierPointAppearance_End = uIMollierPointAppearance;

                result.Add(uIMollierProcess);
            }
            mollierModel.AddRange(result);
            return result;
        }
        
        private List<UIMollierGroup> AddGroups(IEnumerable<IMollierGroup> mollierGroups, bool checkPressure = true)
        {
            if (mollierGroups == null || mollierModel == null)
            {
                return null;
            }

            List<UIMollierGroup> result = new List<UIMollierGroup>();
            foreach (IMollierGroup mollierGroup in mollierGroups)
            {
                Color color = Color.Empty;
                string name = string.Empty;
                MollierGroup mollierGroup1 = null;
                if (mollierGroup is UIMollierGroup)
                {
                    if (((UIMollierGroup)mollierGroup).UIMollierAppearance != null)
                    {
                        color = ((UIMollierGroup)mollierGroup).UIMollierAppearance.Color;
                        name = (((UIMollierGroup)mollierGroup).UIMollierAppearance as UIMollierAppearance).Label;
                    }
                }
                mollierGroup1 = (MollierGroup)mollierGroup;

                UIMollierGroup uIMollierGroup = new UIMollierGroup(mollierGroup1, new UIMollierAppearance(color, name));

                uIMollierGroup = mollierGroup1.ToSAM_UI(true, mollierControlSettings); // TODO: move it / change it

                result.Add(uIMollierGroup);
            }

            mollierModel.AddRange(result);
            return result;
        }
        
        private List<UIMollierZone> AddZones(IEnumerable<IMollierZone> mollierZones)
        {
            if (mollierZones == null || mollierZones.Count() == 0 || mollierModel == null)
            {
                return null;
            }

            List<UIMollierZone> result = new List<UIMollierZone>();
            foreach (IMollierZone mollierZone in mollierZones)
            {
                Color color = Color.Blue;
                string label = "";
                MollierZone mollierZone1 = mollierZone as MollierZone;

                if (mollierZone is UIMollierZone)
                {
                    color = ((UIMollierZone)mollierZone).UIMollierAppearance.Color;
                    label = (((UIMollierZone)mollierZone).UIMollierAppearance as UIMollierAppearance).Label;
                }

                UIMollierZone uIMollierZone = new UIMollierZone(mollierZone1, new UIMollierAppearance(color, label));

                result.Add(uIMollierZone);
            }

            mollierModel.AddRange(result);
            return result;
        }

        private List<UIMollierCurve> AddCurves(IEnumerable<IMollierCurve> mollierCurves)
        {
            if (mollierCurves == null || mollierModel == null)
            {
                return null;
            }

            List<UIMollierCurve> result = new List<UIMollierCurve>();
            foreach(IMollierCurve mollierCurve in mollierCurves)
            {
                if(mollierCurve is UIMollierCurve)
                {
                    if(((UIMollierCurve)mollierCurve).MollierCurve is MollierLine)
                    {
                        result.Add((UIMollierCurve)mollierCurve);
                    }

                    continue;
                }

                if(mollierCurve is MollierLine)
                {
                    result.Add(new UIMollierCurve((MollierCurve)mollierCurve, Color.LightGray));
                }
            }

            mollierModel.AddRange(result);
            return result;
        }

        public void RemoveProcesses(IEnumerable<IMollierProcess> mollierProcesses)
        {
            if (mollierProcesses == null || mollierModel == null)
            {
                return;
            }

            foreach (IMollierProcess mollierProcess in mollierProcesses)
            {
                mollierModel.Remove(mollierProcess, false);
            }

            Regenerate();
        }
        
        public void RemovePoints(IEnumerable<IMollierPoint> mollierPoints)
        {
            if (mollierPoints == null || mollierModel == null)
            {
                return;
            }

            foreach (IMollierPoint mollierPoint in mollierPoints)
            {
                mollierModel.Remove(mollierPoint, false);
            }

            Regenerate();
        }
        
        public void RemoveZones(IEnumerable<IMollierZone> mollierZones)
        {
            if (mollierZones == null || mollierModel == null)
            {
                return;
            }

            foreach (IMollierZone mollierZone in mollierZones)
            {
                mollierModel.Remove(mollierZone, false);
            }
            Regenerate();
        }

        public bool Save(ChartExportType chartExportType, PageSize pageSize = PageSize.A4, PageOrientation pageOrientation = PageOrientation.Landscape, string path = null)
        {
            string pageType = string.Format("{0}_{1}", pageSize, pageOrientation);

            if (string.IsNullOrEmpty(path))
            {
                string sufix = string.Empty;

                Form form = FindForm();
                if(form != null)
                {
                    sufix = string.Format("{0}x{1}_{2}Pa", form.Size.Width, form.Size.Height, mollierControlSettings.Pressure);
                }

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    string name = string.Format("{0}{1}", mollierControlSettings.ChartType == ChartType.Mollier ? "Mollier" : "Psychrometric", sufix);
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
                //string fileName = pageOrientation == PageOrientation.Portrait ? "PDF_Print_AHU_Portrait.xlsx" : "PDF_Print_AHU.xlsx";
                string fileName = "PDF_Print_AHU.xlsx";
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
                    uniqueNames.Add("[epsilon]");
                    int numberOfData = 15;
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
                                    dictionary[name_Temp] = worksheet.Cells[i, j];
                                    break;
                                }
                            }
                        }
                    }

                    List<UIMollierProcess> mollierProcesses = mollierModel.GetMollierObjects<UIMollierProcess>(false);
                    List<MollierGroup> processesGroups = Mollier.Query.Group(mollierProcesses)?.ConvertAll(x => (MollierGroup)x);

                    List<MollierPoint> mollierPoints = mollierModel.GetMollierObjects<MollierPoint>(false);
                    MollierGroup pointsGroup = new MollierGroup("MollierPoints");
                    mollierPoints?.ForEach(x => pointsGroup.Add(x));

                    List<MollierGroup> mollierGroups = mollierModel.GetMollierObjects<MollierGroup>();
                    mollierGroups?.ForEach(x => x.SortGroup());

                    mollierGroups?.AddRange(processesGroups);
                    mollierGroups?.Add(pointsGroup);

                    if (mollierGroups == null || mollierGroups.Count == 0)
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
                        for (int i = 0; i < mollierGroups.Count; i++)
                        {
                            range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                            move_Temp++;
                            List<UIMollierProcess> processes = mollierGroups[i].GetObjects<UIMollierProcess>();
                            for (int j = 0; j < processes.Count; j++)
                            {
                                UIMollierProcess mollierProcess = processes[j];
                                if (mollierProcess.UIMollierPointAppearance_Start.Label != null && mollierProcess.UIMollierPointAppearance_Start.Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                                if (mollierProcess.UIMollierPointAppearance_End.Label != null && mollierProcess.UIMollierPointAppearance_End.Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                            }
                            if (i != mollierGroups.Count - 1)
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

                            for (int i = 0; i < mollierGroups.Count; i++)
                            {
                                worksheet.Cells[rowIndex + id, columnIndex].Value = "-------";
                                id++;
                                List<UIMollierProcess> processes = mollierGroups[i].GetObjects<UIMollierProcess>();

                                for (int j = 0; j < processes.Count; j++)
                                {
                                    worksheet.Cells[rowIndex + id, columnIndex].Value = "------";
                                    id ++;
                                    UIMollierProcess mollierProcess = processes[j];
                                    
                                    MollierPoint start = mollierProcess.Start;
                                    MollierPoint end = mollierProcess.End;
                                    string value_1 = string.Empty;
                                    string value_2 = string.Empty;
                                    switch (key_Temp)
                                    {
                                        case "[ProcessPointName]":
                                            value_1 = mollierProcess.UIMollierPointAppearance_Start.Label;
                                            value_2 = mollierProcess.UIMollierPointAppearance_End.Label;
                                            break;
                                        case "[DryBulbTemperature]":
                                            value_1 = String.Format("{0:#,0.00}", start.DryBulbTemperature);
                                            value_2 = String.Format("{0:#,0.00}", end.DryBulbTemperature);
                                            break;

                                        case "[HumidityRatio]":
                                            value_1 = String.Format("{0:#,0.00}", (start.HumidityRatio * 1000));
                                            value_2 = String.Format("{0:#,0.00}", (end.HumidityRatio * 1000));
                                            break;
                                        case "[RelativeHumidity]":
                                            value_1 = String.Format("{0:#,0.00}", start.RelativeHumidity);
                                            value_2 = String.Format("{0:#,0.00}", end.RelativeHumidity);
                                            break;
                                        case "[WetBulbTemperature]":
                                            value_1 = String.Format("{0:#,0.00}", start.WetBulbTemperature());
                                            value_2 = String.Format("{0:#,0.00}", end.WetBulbTemperature());
                                            break;
                                        case "[SaturationTemperature]":
                                            value_1 = String.Format("{*:#,0.00}", start.SaturationTemperature());
                                            value_2 = String.Format("{*:#,0.00}", end.SaturationTemperature());
                                            break;
                                        case "[Enthalpy]":
                                            value_1 = String.Format("{0:#,0.00}", (start.Enthalpy / 1000));
                                            value_2 = String.Format("{0:#,0.00}", (end.Enthalpy / 1000));
                                            break;
                                        case "[SpecificVolume]":
                                            value_1 = String.Format("{0:#,0.000}", start.SpecificVolume());
                                            value_2 = String.Format("{0:#,0.000}", end.SpecificVolume());
                                            break;
                                        case "[Density]":
                                            value_1 = String.Format("{0:#,0.00}", start.Density());
                                            value_2 = String.Format("{0:#,0.00}", end.Density());
                                            break;
                                        case "[AtmosphericPressure]":
                                            value_1 = start.Pressure.ToString();
                                            value_2 = end.Pressure.ToString();
                                            break;
                                        case "[ProcessName]":
                                            value_2 = Query.FullProcessName(mollierProcess);
                                            value_2 = value_2.Substring(0, value_2.Length - 8);
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
                                        case "[epsilon]":
                                            value_2 = System.Math.Round(Mollier.Query.Epsilon(mollierProcess.MollierProcess), 0).ToString();
                                            break;
                                    }



                                    if (mollierProcess.UIMollierPointAppearance_Start.Label != null && mollierProcess.UIMollierPointAppearance_Start.Label != "")
                                    {
                                        if (value_1 != string.Empty)
                                        {
                                            //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = value_1;
                                            id++;
                                        }
                                        else if (key_Temp == "[ProcessName]" || key_Temp == "[deltaT]" || key_Temp == "[deltaX]" || key_Temp == "[deltaH]" || key_Temp == "[epsilon]")
                                        {
                                            //range_Temp.Copy(worksheet.Cells[rowIndex + id, columnIndex]);
                                            worksheet.Cells[rowIndex + id, columnIndex].Value = "-";
                                            id++;
                                        }
                                    }
                                    if (mollierProcess.UIMollierPointAppearance_End.Label != null && mollierProcess.UIMollierPointAppearance_End.Label != "")
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

                                if (key_Temp == "[ProcessName]")
                                {
                                    int integer = 2;
                                }
                                worksheet.Cells[rowIndex + id, columnIndex].Value = "------";
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


                    Form form = FindForm();

                    Size size_Temp = Size;
                    Size formSize_Temp = form.Size;
                    double widthDifference = (formSize_Temp.Width - size_Temp.Width);
                    double heightDifference = (formSize_Temp.Height - size_Temp.Height);

                    if (pageSize == PageSize.A3)//a3 pdf
                    {
                        Size = new Size(System.Convert.ToInt32(width * 1.4), System.Convert.ToInt32(height * 1.4));
                    }
                    else//a4 pdf
                    {
                        if (pageOrientation == PageOrientation.Landscape)
                        {
                            form.WindowState = FormWindowState.Normal;
                            // form.Size = new Size(System.Convert.ToInt32((width + widthDifference) * 1.2), System.Convert.ToInt32((height + heightDifference) * 1.2));
                            form.Size = new Size(System.Convert.ToInt32((width + widthDifference)), System.Convert.ToInt32((height + heightDifference)));
                            Regenerate();
                        }
                        else
                        {
                            form.WindowState = FormWindowState.Normal;

                            // form.Size = new Size(System.Convert.ToInt32((width + widthDifference) * 1.2), System.Convert.ToInt32((height + heightDifference) * 1.2));
                            form.Size = new Size(System.Convert.ToInt32((width + widthDifference)), System.Convert.ToInt32((height + heightDifference)));
                            Regenerate();


                        }
                    }
                    Save(ChartExportType.EMF, path: path_Temp);

                    if(pageSize == PageSize.A4)
                    {
                        form.WindowState = FormWindowState.Maximized;
                        form.Size = formSize_Temp;
                    }
                    else
                    {
                        Size = size_Temp;
                    }

                    NetOffice.ExcelApi.Shape shape = worksheet.Shapes.AddPicture(path_Temp, NetOffice.OfficeApi.Enums.MsoTriState.msoFalse, NetOffice.OfficeApi.Enums.MsoTriState.msoCTrue, left, top, width, height);

                    //double shapeSizeFactor = Query.ShapeSizeFactor(DeviceDpi);

                   
                    // A3 landscape
                    //if(pageSize == PageSize.A3)
                    //{
                    //    shape.PictureFormat.Crop.ShapeHeight = (float)(shape.PictureFormat.Crop.ShapeHeight * Query.ShapeSizeFactor(DeviceDpi, 0.81));
                    //    shape.PictureFormat.Crop.ShapeWidth = (float)(shape.PictureFormat.Crop.ShapeWidth * Query.ShapeSizeFactor(DeviceDpi, 0.78));
                    //    shape.Width = width;
                    //    shape.Height = height;
                    //}
                    //else
                    //{
                    //    if(pageOrientation == PageOrientation.Landscape)
                    //    {

                    //        shape.PictureFormat.Crop.ShapeHeight = (float)(shape.PictureFormat.Crop.ShapeHeight * Query.ShapeSizeFactor(DeviceDpi, 0.81));
                    //        shape.PictureFormat.Crop.ShapeWidth = (float)(shape.PictureFormat.Crop.ShapeWidth * Query.ShapeSizeFactor(DeviceDpi, 0.78));
                    //        shape.Width = width + 150;
                    //        shape.Height = height + 120;
                    //    }
                    //    else
                    //    {
                    //        shape.PictureFormat.Crop.ShapeHeight = (float)(shape.PictureFormat.Crop.ShapeHeight * Query.ShapeSizeFactor(DeviceDpi, 0.81));
                    //        shape.PictureFormat.Crop.ShapeWidth = (float)(shape.PictureFormat.Crop.ShapeWidth * Query.ShapeSizeFactor(DeviceDpi, 0.78));
                    //        shape.Width = width + 90;
                    //        shape.Height = height + 220;
                    //    }
                    //}
                    range.Value = string.Empty;

                    workbook.SaveCopyAs(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "TEST.xlsx"));

                    worksheet.ExportAsFixedFormat(NetOffice.ExcelApi.Enums.XlFixedFormatType.xlTypePDF, path);

                    return false;
                });

                Excel.Modify.Edit(path_Template, func);

               // System.Threading.Thread.Sleep(1000);

                if (System.IO.File.Exists(path_Temp))
                {
                    if (Core.Query.WaitToUnlock(path_Temp))
                    {
                        System.IO.File.Delete(path_Temp);
                    }
                }
                path_Temp = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "TEST.xlsx");
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
            if (mollierModel == null || mollierControlSettings.DivisionArea)
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
            Regenerate();
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
            Regenerate();
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

            if (hooverTimer != null && hooverTimer.Enabled)
            {

                foreach (Tuple<Series, int> seriesData_Temp in seriesData)
                {
                    seriesData_Temp.Item1.BorderWidth = seriesData_Temp.Item2;
                }

                seriesData.Clear();
            }


            //Point point = e.Location;

            //HitTestResult[] hitTestResults = MollierChart?.HitTest(point.X, point.Y, false, ChartElementType.DataPoint);
            //if (hitTestResults == null)
            //{
            //    return;
            //}

            //foreach (HitTestResult hitTestResult in hitTestResults)
            //{
            //    Series series = hitTestResult?.Series;
            //    if (series == null)
            //    {
            //        continue;
            //    }

            //    seriesData.Add(new Tuple<Series, int>(series, series.BorderWidth));

            //    if (series.Tag is MollierProcess)
            //    {
            //        series.BorderWidth += selectedObjectWidth_Process;
            //    }
            //    else
            //    {
            //        series.BorderWidth += selectedObjectWidth_Default;
            //    }

            //}
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
            ChartType chartType = mollierControlSettings.ChartType;

            double X_Min = chartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Min : mollierControlSettings.Temperature_Min;
            double Y_Min = chartType == ChartType.Mollier ? mollierControlSettings.Temperature_Min : mollierControlSettings.HumidityRatio_Min;
            double X_Max = chartType == ChartType.Mollier ? mollierControlSettings.HumidityRatio_Max : mollierControlSettings.Temperature_Max;
            double Y_Max = chartType == ChartType.Mollier ? mollierControlSettings.Temperature_Max : mollierControlSettings.HumidityRatio_Max;

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

            // [g/kg] to [kg/kg]
            x_Min = chartType == ChartType.Mollier ? x_Min / 1000 : x_Min;
            x_Max = chartType == ChartType.Mollier ? x_Max / 1000 : x_Max;
            y_Min = chartType == ChartType.Mollier ? y_Min : y_Min / 1000;
            y_Max = chartType == ChartType.Mollier ? y_Max : y_Max / 1000;

            double x_Difference = x_Max - x_Min;
            double y_Difference = y_Max - y_Min;
            if ((chartType == ChartType.Mollier && (x_Difference < 0.001 || y_Difference < 1)) || (chartType == ChartType.Psychrometric && (x_Difference < 1 || y_Difference < 0.001)))
            {
                MollierChart.Refresh();
                return;
            }

            mollierControlSettings.HumidityRatio_Min = chartType == ChartType.Mollier ? System.Math.Max(x_Min, X_Min) : System.Math.Max(y_Min, Y_Min);
            mollierControlSettings.HumidityRatio_Max = chartType == ChartType.Mollier ? System.Math.Min(x_Max, X_Max) : System.Math.Min(y_Max, Y_Max);
            mollierControlSettings.Temperature_Min = chartType == ChartType.Mollier ? System.Math.Max(y_Min, Y_Min) : System.Math.Max(x_Min, X_Min);
            mollierControlSettings.Temperature_Max = chartType == ChartType.Mollier ? System.Math.Min(y_Max, Y_Max) : System.Math.Min(x_Max, X_Max);

            mollierControlSettings.HumidityRatio_Min = System.Math.Round(mollierControlSettings.HumidityRatio_Min, 3);
            mollierControlSettings.HumidityRatio_Max = System.Math.Round(mollierControlSettings.HumidityRatio_Max, 3);
            mollierControlSettings.Temperature_Min = System.Math.Round(mollierControlSettings.Temperature_Min);
            mollierControlSettings.Temperature_Max = System.Math.Round(mollierControlSettings.Temperature_Max);

            Regenerate();

            MollierChart.Refresh();
            selection = false;
        }
        
        private void MollierChart_MouseClick(object sender, MouseEventArgs e)
        {
            MollierPoint mollierPoint = GetMollierPoint(e.X, e.Y, out Point2D point2D);

            MollierPointSelected?.Invoke(this, new MollierPointSelectedEventArgs(mollierPoint, point2D));
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
            }
        }

        public List<IUIMollierObject> UIMollierObjects(Type type, bool includeNestedObjects = true)
        {
            List<IUIMollierObject> result = new List<IUIMollierObject>();

            List<IMollierObject> mollierObjects = mollierModel.GetMollierObjects(type, includeNestedObjects);
            if (mollierObjects == null)
            {
                return null;
            }

            foreach (IMollierObject mollierObject in mollierObjects)
            {
                if(mollierObject is IUIMollierObject)
                {
                    result.Add((IUIMollierObject)mollierObject);
                }
            }

            return result;
        }
        
        public List<T> UIMollierObjects<T>(bool includeNestedObjects = true) where T : IUIMollierObject
        {
            return UIMollierObjects(typeof(T), includeNestedObjects)?.FindAll(x => x is T)?.ConvertAll(x => (T)(object)x);
        }
        
        public MollierModel MollierModel
        {
            get
            {
                return mollierModel;
            }
            set
            {
                if(value != null)
                {
                    mollierModel = value;
                }
            }
        }
        
        public void Select(IMollierObject mollierObject) 
        {
            foreach (Tuple<Series, int> seriesData_Temp in seriesData)
            {
                seriesData_Temp.Item1.BorderWidth = seriesData_Temp.Item2;
            }

            seriesData.Clear();

            foreach (Series series in MollierChart.Series)
            {
                if(mollierObject.AlmostEqual(series.Tag as IMollierObject))
                {
                    if (mollierObject is MollierProcess)
                    {
                        seriesData.Add(new Tuple<Series, int>(series, series.BorderWidth));
                        series.BorderWidth += selectedObjectWidth_Process;
                    }
                    else
                    {
                        seriesData.Add(new Tuple<Series, int>(series, series.BorderWidth));
                        series.BorderWidth += selectedObjectWidth_Default;
                    }
                    break;
                }
            }
        }
        
        public MollierPoint GetMollierPoint(int x, int y, out Point2D point2D)
        {
            double chart_X = MollierChart.ChartAreas[0].AxisX.PixelPositionToValue(x);
            double chart_Y = MollierChart.ChartAreas[0].AxisY.PixelPositionToValue(y);
            point2D = new Point2D(chart_X, chart_Y);

            return Convert.ToMollier(point2D, mollierControlSettings.ChartType, mollierControlSettings.Pressure);
        }

        public Point2D GetPoint2D(MollierPoint mollierPoint)
        {
            if(mollierPoint == null)
            {
                return null;
            }

            Point2D point2D = Convert.ToSAM(mollierPoint, mollierControlSettings.ChartType);
            if(point2D == null)
            {
                return null;
            }

            return new Point2D(MollierChart.ChartAreas[0].AxisX.ValueToPixelPosition(point2D.X), MollierChart.ChartAreas[0].AxisY.ValueToPixelPosition(point2D.Y));
        }

        public Point2D GetPixelPositionToValue(Point2D point2D)
        {
            if(point2D == null)
            {
                return null;
            }

            return new Point2D(MollierChart.ChartAreas[0].AxisX.PixelPositionToValue(point2D.X), MollierChart.ChartAreas[0].AxisY.PixelPositionToValue(point2D.Y));
        }

        public Point2D GetValueToPixelPosition(Point2D point2D)
        {
            if (point2D == null)
            {
                return null;
            }

            return new Point2D(MollierChart.ChartAreas[0].AxisX.ValueToPixelPosition(point2D.X), MollierChart.ChartAreas[0].AxisY.ValueToPixelPosition(point2D.Y));
        }

        private void CustomizePointForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form form = sender as Form;
            if(form == null)
            {
                return;
            }

            if (form.DialogResult != DialogResult.OK)
            {
                return;
            }

            Apply(form);
        }

        public void Apply(Form form)
        {
            IUIMollierObject uIMollierObject = null;

            if (form is UIMollierPointForm)
            {
                uIMollierObject = ((UIMollierPointForm)form).UIMollierPoint;

                if (uIMollierObject is UIMollierProcessPoint)
                {
                    uIMollierObject = ((UIMollierProcessPoint)uIMollierObject).UIMollierProcess;
                }
            }
            else if (form is UIMollierProcessForm)
            {
                uIMollierObject = ((UIMollierProcessForm)form).UIMollierProcess;
            }
            else
            {
                return;
            }


            if (uIMollierObject == null)
            {
                return;
            }

            mollierModel.Update(uIMollierObject, true);

            Regenerate();
        }

        private void ContextMenuStrip_Chart_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IUIMollierObject uIMollierObject = GetUIMollierObject();


            ToolStripMenuItem_Edit.Visible = uIMollierObject != null;
            ToolStripMenuItem_Remove.Visible = uIMollierObject != null;
            ToolStripSeparator_Edit.Visible = uIMollierObject != null;

            ToolStripMenuItem_Edit.Tag = uIMollierObject;
            ToolStripMenuItem_Remove.Tag = uIMollierObject;
        }

        private IUIMollierObject GetUIMollierObject()
        {
            Point point = MollierChart.PointToClient(MousePosition);

            List<HitTestResult> hitTestResults = new List<HitTestResult>();

            HitTestResult[] hitTestResults_Temp = MollierChart?.HitTest(point.X, point.Y, true, ChartElementType.DataPointLabel);
            if (hitTestResults_Temp != null)
            {
                hitTestResults.AddRange(hitTestResults_Temp.ToList().FindAll(x => x.Series != null));
            }

            if (hitTestResults.Count == 0)
            {
                hitTestResults_Temp = MollierChart?.HitTest(point.X, point.Y, true, ChartElementType.DataPoint);
                if (hitTestResults_Temp != null)
                {
                    hitTestResults.AddRange(hitTestResults_Temp.ToList().FindAll(x => x.Series != null));
                }
            }

            if (hitTestResults == null || hitTestResults.Count == 0)
            {
                return null;
            }

            foreach (HitTestResult hitTestResult in hitTestResults)
            {
                Series series = hitTestResult?.Series;
                if (series == null)
                {
                    continue;
                }

                IUIMollierObject uIMollierObject = series.Tag as UIMollierProcess;
                if (uIMollierObject != null)
                {
                    return uIMollierObject;
                }

                if (series.Tag is IReference)
                {
                    IReference reference = series.Tag as IReference;
                    uIMollierObject = Geometry.Mollier.Query.UIMollierObject<UIMollierPoint>(mollierModel, reference);
                    if (uIMollierObject != null)
                    {
                        return uIMollierObject;
                    }
                }

                uIMollierObject = series.Tag as UIMollierPoint;
                if (uIMollierObject != null)
                {
                    IReference reference = Create.Reference((UIMollierPoint)uIMollierObject);
                    if (Geometry.Mollier.Query.UIMollierObject<IUIMollierObject>(mollierModel, reference) == null)
                    {
                        uIMollierObject = null;
                        continue;
                    }

                    return uIMollierObject;
                }

                int index = hitTestResult.PointIndex;
                if (index == -1)
                {
                    continue;
                }

                uIMollierObject = series.Points[index].Tag as UIMollierPoint;
                if (uIMollierObject != null)
                {
                    return uIMollierObject;
                }
            }

            return null;
        }

        private void ToolStripMenuItem_Edit_Click(object sender, EventArgs e)
        {
            IUIMollierObject uIMollierObject = ToolStripMenuItem_Remove.Tag as IUIMollierObject;
            if (uIMollierObject == null)
            {
                return;
            }

            if(uIMollierObject is UIMollierPoint)
            {
                UIMollierPointForm customizePointForm = new UIMollierPointForm((UIMollierPoint)uIMollierObject)
                {
                    MollierControl = this,
                };

                customizePointForm.FormClosing += CustomizePointForm_FormClosing;
                customizePointForm.Show();
                return;
            }

            if(uIMollierObject is UIMollierProcess)
            {
                UIMollierProcessForm uIMollierProcessForm = new UIMollierProcessForm((UIMollierProcess)uIMollierObject)
                {
                    MollierControl = this,
                };

                uIMollierProcessForm.FormClosing += CustomizePointForm_FormClosing;
                uIMollierProcessForm.Show();
                return;
            }
        }

        private void ToolStripMenuItem_Remove_Click(object sender, EventArgs e)
        {
            IUIMollierObject uIMollierObject = ToolStripMenuItem_Remove.Tag as IUIMollierObject;
            if(uIMollierObject == null)
            {
                return;
            }

            if (MessageBox.Show("Are you sure to remove item?", "Remove", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }

            mollierModel.Remove(uIMollierObject);
            Regenerate();
        }
    }
}
