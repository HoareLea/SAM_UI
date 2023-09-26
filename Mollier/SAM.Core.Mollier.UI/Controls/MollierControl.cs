using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierControl : UserControl
    {
        public event MollierPointSelectedEventHandler MollierPointSelected;

        private Point mdown = Point.Empty;
        private bool selection = false;
        private MollierControlSettings mollierControlSettings;
        private MollierModel mollierModel = new MollierModel();
        private List<Tuple<Series, int>> seriesData = new List<Tuple<Series, int>>();

        private int selectedObjectWidth_Process = 4;
        private int selectedObjectWidth_Default = 2;

        public MollierControl()
        {
            InitializeComponent();

            mollierControlSettings = new MollierControlSettings();
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

        public bool ClearObjects()
        {
            mollierModel?.Clear();
            GenerateGraph();
            return true;
        }
        public void GenerateGraph()
        {
            if (mollierControlSettings == null)
            {
                return;
            }

            double pressure = mollierControlSettings.Pressure;

            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                setAxisGraph_Mollier();

            }
            else if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                setAxisGraph_Psychrometric();
            }

            MollierChart.AddLinesSeries(mollierControlSettings);

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
        }
        public List<IUIMollierObject> AddMollierObjects<T>(IEnumerable<T> mollierObjects, bool checkPressure = true) where T : IMollierObject
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
            }

            GenerateGraph();
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

                Color color = Color.Blue;
                string label = "";
                bool visible = true;
                MollierPoint mollierPoint1 = mollierPoint as MollierPoint;

                if (mollierPoint is UIMollierPoint)
                {
                    if (((UIMollierPoint)mollierPoint).UIMollierAppearance != null)
                    {
                        color = ((UIMollierPoint)mollierPoint).UIMollierAppearance.Color;
                        label = ((UIMollierPoint)mollierPoint).UIMollierAppearance.Label;
                        visible = ((UIMollierPoint)mollierPoint).UIMollierAppearance.Visible;
                    }
                }

                UIMollierPoint uIMollierPoint = new UIMollierPoint(mollierPoint1, new UIMollierAppearance(color, label));

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

                Color color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess);
                string label = Query.ProcessName(mollierProcess);
                bool visible = true;
                MollierProcess mollierProcess1 = null;

                Color startColor = Color.Black;
                string startLabel = "";
                Color endColor = Color.Black;
                string endLabel = "";

                if (mollierProcess is UIMollierProcess)
                {
                    mollierProcess1 = ((UIMollierProcess)mollierProcess).MollierProcess;
                    if (((UIMollierProcess)mollierProcess).UIMollierAppearance.Color != Color.Empty)
                    {
                        color = ((UIMollierProcess)mollierProcess).UIMollierAppearance.Color;
                    }
                    label = ((UIMollierProcess)mollierProcess).UIMollierAppearance.Label;
                    visible = ((UIMollierProcess)mollierProcess).UIMollierAppearance.Visible;
                }
                else if (mollierProcess is MollierProcess)
                {
                    mollierProcess1 = (MollierProcess)mollierProcess;
                    color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, mollierProcess1);
                }

                UIMollierProcess uIMollierProcess = new UIMollierProcess(mollierProcess1, color);
                uIMollierProcess.UIMollierAppearance.Label = label;
                uIMollierProcess.UIMollierAppearance.Visible = visible;

                if (mollierProcess is UIMollierProcess && ((UIMollierProcess)mollierProcess).UIMollierAppearance_Start != null)
                {
                    uIMollierProcess.UIMollierAppearance_Start = ((UIMollierProcess)mollierProcess).UIMollierAppearance_Start;
                }
                else
                {
                    uIMollierProcess.UIMollierAppearance_Start = new UIMollierAppearance(startColor, startLabel);
                }

                if (mollierProcess is UIMollierProcess && ((UIMollierProcess)mollierProcess).UIMollierAppearance_End != null)
                {
                    uIMollierProcess.UIMollierAppearance_End = ((UIMollierProcess)mollierProcess).UIMollierAppearance_End;
                }
                else
                {
                    uIMollierProcess.UIMollierAppearance_End = new UIMollierAppearance(endColor, endLabel);
                }

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
                        name = ((UIMollierGroup)mollierGroup).UIMollierAppearance.Label;
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
                    label = ((UIMollierZone)mollierZone).UIMollierAppearance.Label;
                }

                UIMollierZone uIMollierZone = new UIMollierZone(mollierZone1, new UIMollierAppearance(color, label));

                result.Add(uIMollierZone);
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

            GenerateGraph();
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

            GenerateGraph();
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
            GenerateGraph();
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
                                if (mollierProcess.UIMollierAppearance_Start.Label != null && mollierProcess.UIMollierAppearance_Start.Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                                if (mollierProcess.UIMollierAppearance_End.Label != null && mollierProcess.UIMollierAppearance_End.Label != "")
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
                                            value_1 = mollierProcess.UIMollierAppearance_Start.Label;
                                            value_2 = mollierProcess.UIMollierAppearance_End.Label;
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



                                    if (mollierProcess.UIMollierAppearance_Start.Label != null && mollierProcess.UIMollierAppearance_Start.Label != "")
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
                                    if (mollierProcess.UIMollierAppearance_End.Label != null && mollierProcess.UIMollierAppearance_End.Label != "")
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


                    Form form = this.FindForm();

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
                            GenerateGraph();
                        }
                        else
                        {
                            form.WindowState = FormWindowState.Normal;

                            // form.Size = new Size(System.Convert.ToInt32((width + widthDifference) * 1.2), System.Convert.ToInt32((height + heightDifference) * 1.2));
                            form.Size = new Size(System.Convert.ToInt32((width + widthDifference)), System.Convert.ToInt32((height + heightDifference)));
                            GenerateGraph();


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
                    GenerateGraph();
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
        public MollierPoint GetMollierPoint(int x, int y)
        {
            double chart_X = MollierChart.ChartAreas[0].AxisX.PixelPositionToValue(x);
            double chart_Y = MollierChart.ChartAreas[0].AxisY.PixelPositionToValue(y);
            Geometry.Planar.Point2D point2D = new Geometry.Planar.Point2D(chart_X, chart_Y);

            return Convert.ToMollier(point2D, mollierControlSettings.ChartType, mollierControlSettings.Pressure);
        }


    }
}
