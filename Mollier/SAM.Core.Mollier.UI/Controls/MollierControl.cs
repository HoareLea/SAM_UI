using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierControl : UserControl
    {
        private ChartType chartType;
        private double pressure = 101325;
        private bool density_line = true, enthalpy_line = true, specific_volume_line = true, wet_bulb_temperature_line = true;
        private string color = "default";
        private List<MollierPoint> mollierPoints;
        private List<IMollierProcess> mollierProcesses;
        int count = 0;

        public MollierControl()
        {
            InitializeComponent();
           // generate_graph_mollier();
        }
        private void create_relative_humidity_line_Mollier(int temperature_Min, int temperature_Max, double relative_humidity, double pressure)
        {
            List<List<Geometry.Planar.Point2D>> humidity_ratio_points = new List<List<Geometry.Planar.Point2D>>();
            for (int i = temperature_Min; i <= temperature_Max; i++)
            {
                humidity_ratio_points.Add(new List<Geometry.Planar.Point2D>());
            }
            for (int i = 1; i <= 10; i++)
            {
                string unit = (i * 10).ToString() + '%';
                Series series = MollierChart.Series.Add(unit);
                series.IsVisibleInLegend = false;
                series.Color = add_color(color, "Relative Humidity", "line");
                series.ChartType = SeriesChartType.Spline;
                List<Geometry.Planar.Point2D> relative_humidity_points = new List<Geometry.Planar.Point2D>();
                for (int j = temperature_Min; j <= temperature_Max; j++)
                {
                    double humidity_ratio = Query.HumidityRatio(j, relative_humidity, pressure);
                    double diagram_temperature = Query.DiagramTemperature(j, humidity_ratio);
                    if (humidity_ratio_points[j - temperature_Min].Count == 0)
                        humidity_ratio_points[j - temperature_Min].Add(new Geometry.Planar.Point2D(0, j));
                    relative_humidity_points.Add(new Geometry.Planar.Point2D(humidity_ratio * 1000, diagram_temperature));
                    humidity_ratio_points[j - temperature_Min].Add(new Geometry.Planar.Point2D(humidity_ratio * 1000, diagram_temperature));
                }
                foreach (Geometry.Planar.Point2D point2D in relative_humidity_points)
                {
                    series.Points.AddXY(point2D.X, point2D.Y);
                    if (i == 10)
                    {
                        series.BorderWidth = 3;
                    }
                }
                //rotate relative humidity label
                int index_Point = 8;
                int count = relative_humidity_points.Count;
                Geometry.Planar.Point2D point2D_1 = relative_humidity_points[count - (index_Point - 1) - i];
                Geometry.Planar.Point2D point2D_2 = relative_humidity_points[count - (index_Point + 1) - i];
                Geometry.Planar.Vector2D vector2D = new Geometry.Planar.Vector2D(point2D_1, point2D_2);
                double angle = vector2D.Angle(SAM.Geometry.Planar.Vector2D.WorldX.GetNegated());
                series.SmartLabelStyle.Enabled = false;
                if (i == 5)
                    series.Points[count - index_Point - i].Label = unit + " Relative Humidity φ";
                else
                    series.Points[count - index_Point - i].Label = unit;
                series.Points[count - index_Point - i].LabelForeColor = add_color(color, "Relative Humidity", "unit");
                series.Points[count - index_Point - i].LabelAngle = -(System.Convert.ToInt32(angle * 180 / System.Math.PI) - 22);
                relative_humidity += 10;
            }
            int list_size = humidity_ratio_points.Count;
            for (int i = 0; i < list_size; i++)
            {
                string unit_1 = (i - 25).ToString();
                Series series_1 = MollierChart.Series.Add(unit_1);
                series_1.IsVisibleInLegend = false;
                if (color == "default")
                    series_1.Color = Color.LightGray;
                else
                    series_1.Color = add_color(color, "Diagram Temperature", "line");
                series_1.ChartType = SeriesChartType.Spline;
                List<Geometry.Planar.Point2D> point2Ds_humidity = new List<Geometry.Planar.Point2D>();
                point2Ds_humidity = humidity_ratio_points[i];
                for (int j = 0; j < point2Ds_humidity.Count; j++)
                {
                    series_1.Points.AddXY(point2Ds_humidity[j].X, point2Ds_humidity[j].Y);
                }
                if (i % 5 == 0)//bolds every 5th line 
                {
                    series_1.BorderWidth = 2;
                    if(color == "default")
                        series_1.Color = Color.DarkGray;
                    else
                        series_1.Color = add_color(color, "Diagram Temperature", "line");
                }
            }
        }
        private void create_relative_humidity_line_Psychrometric(int temperature_Min, int temperature_Max, double relative_humidity, double pressure)
        {
            for (int i = 1; i <= 10; i++)
            {
                string unit = (i * 10).ToString() + '%';
                Series series = MollierChart.Series.Add(unit);
                series.IsVisibleInLegend = false;
                series.Color = add_color(color, "Relative Humidity", "line");
                series.ChartType = SeriesChartType.Spline;
                List<Geometry.Planar.Point2D> relative_humidity_points = new List<Geometry.Planar.Point2D>();
                for (int j = temperature_Min; j <= temperature_Max; j++)
                {
                    double humidity_ratio = Query.HumidityRatio(j, relative_humidity, pressure);
                    relative_humidity_points.Add(new Geometry.Planar.Point2D(j, humidity_ratio));

                }
                foreach (Geometry.Planar.Point2D point2D in relative_humidity_points)
                {
                    series.Points.AddXY(point2D.X, point2D.Y);
                    if (i == 10)
                    {
                        series.BorderWidth = 3;
                    }
                }
                int index_Point = 8;
                int count = relative_humidity_points.Count;
                Geometry.Planar.Point2D point2D_1 = relative_humidity_points[count - (index_Point - 1) - i];
                Geometry.Planar.Point2D point2D_2 = relative_humidity_points[count - (index_Point + 1) - i];
                Geometry.Planar.Vector2D vector2D = new Geometry.Planar.Vector2D(point2D_1, point2D_2);
                double angle = vector2D.Angle(SAM.Geometry.Planar.Vector2D.WorldX.GetNegated());
                series.SmartLabelStyle.Enabled = false;
                if (i == 5)
                    series.Points[count - index_Point - i].Label = unit + " Relative Humidity φ";
                else
                    series.Points[count - index_Point - i].Label = unit;
                series.Points[count - index_Point - i].LabelForeColor = add_color(color, "Relative Humidity", "unit");
                series.Points[count - index_Point - i].LabelAngle = (System.Convert.ToInt32(angle * 180 / System.Math.PI) - 50);
                relative_humidity += 10;
            }
        }

        private void create_density_line(ChartType chartType, double density_Min, double density_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = GetMollierPoints_Density(density_Min, density_Max, pressure);
            if(dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, "kg / m³", "Density");
            Series series_Temp = series?.Find(x => x.Name.Contains((1.2).ToString()));
            if(series_Temp != null)
            {
                double X = series_Temp.Points[0].XValue;
                double Y = series_Temp.Points[0].YValues[0];
                create_moved_label(chartType, X, Y, 0, 80, 2, 0, 0, 0.005, "Density [ kg / m³ ]", "Density", "label");
            }
        }
        private Dictionary<double, List<MollierPoint>> GetMollierPoints_Density(double density_Min, double density_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();
            while (density_Min <= density_Max)
            {
                result[density_Min] = new List<MollierPoint>();
                double temperature_1 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 0, pressure);
                double humidityRatio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[density_Min].Add(mollierPoint_1);

                double temperature_2 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 100, pressure);
                double humidityRatio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, humidityRatio_2, pressure);
                result[density_Min].Add(mollierPoint_2);

                density_Min += 0.02;
            }
            return result;
        }

        private void create_enthalpy_line(ChartType chartType, double enthalpy_Min, double enthalpy_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = GetMollierPoints_Enthalpy(enthalpy_Min, enthalpy_Max, pressure);
            if(dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, "kJ / kg", "Enthalpy");
        }
        private Dictionary<double, List<MollierPoint>> GetMollierPoints_Enthalpy(double enthalpy_Min, double enthalpy_Max, double pressure)
        {   
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();

            while (enthalpy_Min <= enthalpy_Max)
            {
                result[enthalpy_Min] = new List<MollierPoint>();
                double humidityRatio_Min = Query.HumidityRatio_ByEnthalpy(-20, enthalpy_Min * 1000);

                double humidityRatio_1 = Query.HumidityRatio_ByEnthalpy(100, enthalpy_Min * 1000);
                double temperature_1 = Query.DryBulbTemperature(enthalpy_Min * 1000, humidityRatio_1);
                double temperature_2 = Query.DryBulbTemperature_ByEnthalpy(enthalpy_Min * 1000, 100, pressure);
                double humidityRatio_2 = Query.HumidityRatio(temperature_2, 100, pressure);

                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[enthalpy_Min].Add(mollierPoint_1);

                if(enthalpy_Min % 10 == 0)
                {
                    Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(humidityRatio_1,temperature_1);
                    Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(humidityRatio_2,temperature_2);
                    Math.PolynomialEquation polynomialEquation = SAM.Geometry.Create.PolynomialEquation(new Geometry.Planar.Point2D[] { Point_2, Point_1 });
                    double a = chartType == ChartType.Mollier ? 0.0015 : 0.0006;
                    temperature_2 = polynomialEquation.Evaluate(Point_2.X + a);

                    MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, Point_2.X + a, pressure);
                    result[enthalpy_Min].Add(mollierPoint_2);
                }
                else
                {
                    MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, humidityRatio_2, pressure);
                    result[enthalpy_Min].Add(mollierPoint_2);
                }

                enthalpy_Min += 1;
            }
            return result;
        }  

        private void create_Wet_Bulb_Temperature_line(ChartType chartType, double wetBulbTemperature_Min, double wetBulbTemperature_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = GetMollierPoints_WetBulbTemperature(wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            if(dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, "°C", "Wet Bulb Temperature");

            Series series_Temp = series?.Find(x => x.Name.Contains((15).ToString()));
            if (series_Temp != null)
            {
                double X = series_Temp.Points.Last().XValue;
                double Y = series_Temp.Points.Last().YValues[0];
                create_moved_label(chartType, X, Y, 33, 23, -1.2, 3.2, 4.5, -0.0018, "Wet Bulb Temperature [ °C ]", "Wet Bulb Temperature", "label");
            }
        }
        private Dictionary<double, List<MollierPoint>> GetMollierPoints_WetBulbTemperature(double wetBulbTemperature_Min, double wetBulbTemperature_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>(); 
            while(wetBulbTemperature_Min <= wetBulbTemperature_Max)
            {
                result[wetBulbTemperature_Min] = new List<MollierPoint>();

                double temperature_1 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 0, pressure);
                double humidityRatio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[wetBulbTemperature_Min].Add(mollierPoint_1);

                double temperature_2 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 100, pressure);
                double humidityRatio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, humidityRatio_2, pressure);
                result[wetBulbTemperature_Min].Add(mollierPoint_2);

                wetBulbTemperature_Min += 5;
            }
            return result;
        }
   
        private void create_specific_volume_line(ChartType chartType, double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = GetMollierPoints_SpecificVolume(specific_volume_Min, specific_volume_Max, pressure);
            if(dictionary == null)
            {
                return;
            }

            List<Series> series = CreateSeries(dictionary, chartType, "kg/m³", "specific volume");

            Series series_Temp = series?.Find(x => x.Name.Contains((0.9).ToString()));
            if (series_Temp != null)
            {
                double X = series_Temp.Points.Last().XValue;
                double Y = series_Temp.Points.Last().YValues[0];
                create_moved_label(chartType, X, Y, 6, 66, -2, 0.5, 3, -0.005, "Specific volume [ kg/m³ ]", "Specific_volume", "label");
            }
        }
        private Dictionary<double, List<MollierPoint>> GetMollierPoints_SpecificVolume(double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();

            while (specific_volume_Min <= specific_volume_Max)
            {
                result[specific_volume_Min] = new List<MollierPoint>();

                MollierPoint mollierPoint_1 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(0, specific_volume_Min, pressure);
                result[specific_volume_Min].Add(mollierPoint_1);
                MollierPoint mollierPoint_2 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(100, specific_volume_Min, pressure);
                result[specific_volume_Min].Add(mollierPoint_2);

                specific_volume_Min += 0.05;
            }

            return result;
        }
      
        
        private Color add_color(string color, string lineType, string parameter_type)//, bool highlighted
        {
            switch (color)
            {
                case "default":
                    switch (lineType)
                    {
                        case "Relative Humidity":
                            switch (parameter_type)
                            {
                                case "line":
                                    return Color.LightBlue;
                                case "unit":
                                    return Color.Gray;
                                case "label":
                                    return Color.Gray;
                            }
                            break;
                        case "Density":
                            switch (parameter_type)
                            {
                                case "line":
                                    return Color.LightGreen;
                                case "unit":
                                    return Color.Gray;
                                case "label":
                                    return Color.Gray;
                            }
                            break;
                        case "Enthalpy":
                            switch (parameter_type)
                            {
                                case "line":
                                    return Color.LightGray;
                                case "unit":
                                    return Color.Black;
                                case "label":
                                    return Color.Gray;
                            }
                            break;
                        case "Specific Volume":
                            switch (parameter_type)
                            {
                                case "line":
                                    return Color.LightSlateGray;
                                case "unit":
                                    return Color.Gray;
                                case "label":
                                    return Color.Gray;
                            }
                            break;
                        case "Wet Bulb Temperature":
                            switch (parameter_type)
                            {
                                case "line":
                                    return Color.LightSalmon;
                                case "unit":
                                    return Color.Gray;
                                case "label":
                                    return Color.Gray;
                            }
                            break;
                    }
                    break;
                case "blue":
                    return Color.LightBlue;
                case "gray":
                    return Color.LightGray;
            }
            return Color.Gray; // defualt line color
        }
        private void create_moved_label(ChartType chartType, double X, double Y, int Mollier_angle, int Psychrometric_angle, double Mollier_X, double Mollier_Y, double Psychrometric_X, double Psychrometric_Y, string LabelContent, string name, string parameterType)
        {
            double x = chartType == ChartType.Mollier ? Mollier_X : Psychrometric_X;
            double y = chartType == ChartType.Mollier ? Mollier_Y : Psychrometric_Y;
            //X, Y - coordinates of the point before moveing by x and y
            
            Series new_label = MollierChart.Series.Add(String.Format(LabelContent + name));
            new_label.IsVisibleInLegend = false;
            new_label.ChartType = SeriesChartType.Spline;
            new_label.SmartLabelStyle.Enabled = false;
            new_label.Points.AddXY(X + x, Y + y);
            new_label.Label = LabelContent;
            new_label.LabelAngle = chartType == ChartType.Mollier ? Mollier_angle : Psychrometric_angle;
            new_label.LabelForeColor = add_color(color, name, parameterType);
        }
        private void createLabels(ChartType chartType, string unit, string prefix, Series series, double value)
        {
            double X, Y;
            switch (prefix)
            {
                case "specific volume":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    create_moved_label(chartType, X, Y, 0, 0, 0, 0, 0, 0, value.ToString(), prefix, "unit");
                    series.Color = add_color(color, prefix, "line");
                    break;

                case "Wet Bulb Temperature":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    create_moved_label(chartType, X, Y, 0, 0, 0, -3, -1, 0, value.ToString(), prefix, "unit");
                    series.Color = add_color(color, prefix, "line");
                    break;
                case "Density":
                    X = series.Points[0].XValue;
                    Y = series.Points[0].YValues[0];
                    create_moved_label(chartType, X, Y, 0, 80, 0.4, 0, 0.2, 0.0002, value.ToString(), prefix, "unit");
                    series.Color = add_color(color, prefix, "line");
                    series.BorderDashStyle = ChartDashStyle.DashDotDot;
                    break;
                case "Enthalpy":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    series.Color = add_color(color, prefix, "line");
                    if (value % 10 == 0)
                    {
                        if (chartType == ChartType.Mollier)
                            create_moved_label(chartType, X, Y, 0, 0, 0, -2.5, 0, 0, value.ToString(), prefix, "unit");
                        else
                            create_moved_label(chartType, X, Y, 0, 0, 0, 0, 0, 0, value.ToString(), prefix, "unit");

                        series.Color = add_color(color, prefix, "highlighted");
                    }
                    break;
            }
        }
        private List<Series> CreateSeries(Dictionary<double, List<MollierPoint>> dictionary, ChartType chartType, string unit, string prefix)
        {
            List<Series> result = new List<Series>();
            foreach (KeyValuePair<double, List<MollierPoint>> keyValuePair in dictionary)
            {
                Series series = CreateSeries(keyValuePair.Value, chartType, keyValuePair.Key, unit, prefix);
                if(series != null)
                {
                    result.Add(series);
                }
            }

            return result;
        }
        private Series CreateSeries(List<MollierPoint> mollierPoints, ChartType chartType, double value, string unit, string prefix)
        {
            Series result = MollierChart.Series.Add(String.Format("{0} {1} {2}", prefix, value, unit));
            result.ChartType = SeriesChartType.Spline;
            result.IsVisibleInLegend = false;

            foreach(MollierPoint mollierPoint in mollierPoints)
            {
                double temperature = mollierPoint.DryBulbTemperature;
                double humidityRatio = mollierPoint.HumidityRatio;

                if(chartType == ChartType.Mollier)
                {
                    temperature = Query.DiagramTemperature(mollierPoint);
                    humidityRatio = humidityRatio * 1000;
                    result.Points.AddXY(humidityRatio, temperature);
                }
                else
                {
                    result.Points.AddXY(temperature, humidityRatio);
                }
            }
            createLabels(chartType,unit, prefix, result, value);
            return result;
        }


        private void generate_graph()
        {
            if(chartType == ChartType.Mollier)
            {
                generate_graph_mollier();
            }
            else if (chartType == ChartType.Psychrometric)
            {
                generate_graph_psychrometric();
            }
        }
        private void generate_graph_mollier()
        {
            if (pressure < 90000 || pressure > 110000)
            {
                return;
            }
            //INITIAL SIZES
            int wetBulbTemperature_Min = -10;
            int wetBulbTemperature_Max = 30;
            int temperature_Min = -20;
            int temperature_Max = 50;
            int humidity_ratio_Min = 0;
            int humidity_ratio_Max = 35;
            double density_Min = 0.96;
            double density_Max = 1.41;
            int enthalpy_Min = -20;
            int enthalpy_Max = 140;
            double specific_volume_Min = 0.75;
            double specific_volume_Max = 0.95;
            double relative_humidity = 10;
            //CHART
            MollierChart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            MollierChart.Series.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            //yourChartArea.AxisX2.Enabled = AxisEnabled.True;
            //MollierChart.ChartAreas[0].AxisX2.Enabled = AxisEnabled.True;
            //MollierChart.ChartAreas[0].AxisX2.Title = "P_w  x [ kPa ]";
            //MollierChart.ChartAreas[0].AxisX2.Maximum = 50;
            //MollierChart.ChartAreas[0].AxisX2.Minimum = 0;
            //MollierChart.ChartAreas[0].AxisX2.Interval = 5;
            //MollierChart.ChartAreas[0].AxisX2.MajorGrid.LineColor = Color.Gray;
            //MollierChart.ChartAreas[0].AxisX2.MinorGrid.Interval = 1;
            //MollierChart.ChartAreas[0].AxisX2.MinorGrid.Enabled = true;
            //MollierChart.ChartAreas[0].AxisX2.MinorGrid.LineColor = Color.LightGreen;

            
            //MollierChart.ChartAreas.AxisX2.Enabled = AxisEnabled.True;
            //AXIS X

            Axis axisX = chartArea.AxisX;
            axisX.Title = "Humidity Ratio  x [ g/kg ]";
            axisX.Maximum = humidity_ratio_Max;
            axisX.Minimum = humidity_ratio_Min;
            axisX.Interval = 5;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = true;
            axisX.MinorGrid.LineColor = Color.LightGray;
            axisX.IsReversed = false;
            //AXIS Y
            Axis axisY = chartArea.AxisY;
            axisY.Title = "Dry Bulb Temperature t [ °C ]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = temperature_Max;
            axisY.Minimum = temperature_Min;
            axisY.Interval = 5;
            //CREATING RELATIVE HUMIDITY AND HUMIDITY RATIO LINES
            create_relative_humidity_line_Mollier(temperature_Min, temperature_Max, relative_humidity, pressure);
            //CREATING DENSITY LINE
            if (density_line)
                create_density_line(ChartType.Mollier, density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line(ChartType.Mollier, enthalpy_Min, enthalpy_Max, pressure);
                //CREATETING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_Wet_Bulb_Temperature_line(ChartType.Mollier, wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line(ChartType.Mollier, specific_volume_Min, specific_volume_Max, pressure);
            if (mollierPoints != null)
            {
                Series series = MollierChart.Series.Add(System.Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Point;
                foreach (MollierPoint point in mollierPoints)
                {
                    double humidity_ratio = point.HumidityRatio;
                    double DryBulbTemperature = point.DryBulbTemperature;
                    double diagram_temperature = SAM.Core.Mollier.Query.DiagramTemperature(point);
                    int index = series.Points.AddXY(humidity_ratio * 1000, diagram_temperature);
                    series.Points[index].ToolTip = ToolTip(point, chartType);
                    series.Points[index].Tag = point;
                }
            }
            if(mollierProcesses != null)
            {

                foreach (IMollierProcess mollierProcess in mollierProcesses)
                {
                    Series series = MollierChart.Series.Add(System.Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.ChartType = SeriesChartType.Line;
                    series.BorderWidth = 5;
                    //series.Color = Color.Red;

                    MollierPoint start = mollierProcess?.Start;
                    MollierPoint end = mollierProcess?.End;
                    if(start == null || end == null)
                    {
                        continue;
                    }

                    int index;

                    index = series.Points.AddXY(start.HumidityRatio * 1000, Query.DiagramTemperature(start));
                    series.Points[index].ToolTip = ToolTip(start, chartType);
                    series.Points[index].Tag = start;

                    index = series.Points.AddXY(end.HumidityRatio * 1000, Query.DiagramTemperature(end));
                    series.Points[index].ToolTip = ToolTip(end, chartType);
                    series.Points[index].Tag = end;

                    series.Tag = mollierProcess;
                    series.ToolTip = ToolTip(mollierProcess);
                }
            }

        }
        private void generate_graph_psychrometric()
        {
            if (pressure < 90000 || pressure > 110000)
            {
                return;
            }
            //INITIAL SIZES
            int wetBulbTemperature_Min = -10;
            int wetBulbTemperature_Max = 30;
            int temperature_Min = -20;
            int temperature_Max = 50;
            double humidity_ratio_Min = 0;
            double humidity_ratio_Max = 0.035;
            double density_Min = 0.96;
            double density_Max = 1.41;
            int enthalpy_Min = -20;
            int enthalpy_Max = 140;
            double specific_volume_Min = 0.75;
            double specific_volume_Max = 0.95;
            double relative_humidity = 10;
            //MollierChart
            MollierChart.Series.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            //MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
            //MollierChart.ChartAreas[0].AxisY2.Title = "P_w  x [ kPa ]";
            //MollierChart.ChartAreas[0].AxisY2.Maximum = 50;
            //MollierChart.ChartAreas[0].AxisY2.Minimum = 0;
            //MollierChart.ChartAreas[0].AxisY2.Interval = 5;
            //MollierChart.ChartAreas[0].AxisY2.MajorGrid.LineColor = Color.Gray;
            //MollierChart.ChartAreas[0].AxisY2.MinorGrid.Interval = 1;
            //MollierChart.ChartAreas[0].AxisY2.MinorGrid.Enabled = true;
            //MollierChart.ChartAreas[0].AxisY2.MinorGrid.LineColor = Color.LightGreen;
            //AXIS X
            Axis axisX = chartArea.AxisX;
            axisX.Title = "Dry Bulb Temperature t [ °C ]";
            axisX.Maximum = temperature_Max;
            axisX.Minimum = temperature_Min;
            axisX.Interval = 5;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = true;
            axisX.MinorGrid.LineColor = Color.LightGray;
            //AXIS Y
            Axis axisY = chartArea.AxisY;
            axisY.Title = "Humidity Ratio  x [ kg/kg ]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = humidity_ratio_Max;
            axisY.Minimum = humidity_ratio_Min;
            axisY.Interval = 0.005;
            axisY.MajorGrid.LineColor = Color.Gray;
            axisY.MinorGrid.Interval = 0.001;
            axisY.MinorGrid.Enabled = true;
            axisY.MinorGrid.LineColor = Color.LightGray;
            //CREATING RELATIVE HUMIDITY LINES
            create_relative_humidity_line_Psychrometric(temperature_Min, temperature_Max, relative_humidity, pressure);
            //CREATING DENSITY LINE
            if (density_line)
                create_density_line(ChartType.Psychrometric, density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line(ChartType.Psychrometric, enthalpy_Min, enthalpy_Max, pressure);
                //create_enthalpy_line_Psychrometric(enthalpy_Min, enthalpy_Max, pressure);
            //CREATING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_Wet_Bulb_Temperature_line(ChartType.Psychrometric, wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line(ChartType.Psychrometric, specific_volume_Min, specific_volume_Max, pressure);
            
            if (mollierPoints != null)
            {
                Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Point;
                foreach (MollierPoint point in mollierPoints)
                {
                    double humidity_ratio = point.HumidityRatio;
                    double DryBulbTemperature = point.DryBulbTemperature;
                    series.Points.AddXY(DryBulbTemperature, humidity_ratio);
                    int index = series.Points.AddXY(DryBulbTemperature, humidity_ratio);
                    series.Points[index].ToolTip = ToolTip(point, chartType);
                    series.Points[index].Tag = point;
                }
            }

            if (mollierProcesses != null)
            {
                foreach (IMollierProcess mollierProcess in mollierProcesses)
                {
                    Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.ChartType = SeriesChartType.Line;

                    MollierPoint start = mollierProcess?.Start;
                    MollierPoint end = mollierProcess?.End;
                    if (start == null || end == null)
                    {
                        continue;
                    }

                    int index;

                    index = series.Points.AddXY(start.DryBulbTemperature, start.HumidityRatio);
                    series.Points[index].ToolTip = ToolTip(start, chartType);
                    series.Points[index].Tag = start;

                    index = series.Points.AddXY(end.DryBulbTemperature, end.HumidityRatio);
                    series.Points[index].ToolTip = ToolTip(end, chartType);
                    series.Points[index].Tag = end;

                    series.Tag = mollierProcess;
                    //series.ToolTip = ToolTip(mollierProcess);
                }
            }
        }

        private string ToolTip(MollierPoint mollierPoint, ChartType chartType)
        {
            if(mollierPoint == null)
            {
                return null;
            }

            string mask = "φ = {0} %\nx = {1}{2}\nt_d = {3} °C\np = {4} Pa\nh = {5} kJ/kg\nt_w = {6} °C\n𝜈 = {7} kg/m³";

            switch (chartType)
            {
                case ChartType.Psychrometric:
                    return String.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.01), Core.Query.Round(mollierPoint.HumidityRatio, Tolerance.MacroDistance), " kg/kg", Core.Query.Round(mollierPoint.DryBulbTemperature, 0.01), mollierPoint.Pressure, mollierPoint.Enthalpy, Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(),0.01));

                case ChartType.Mollier:
                    return String.Format(mask, Core.Query.Round(mollierPoint.RelativeHumidity, 0.01), Core.Query.Round(mollierPoint.HumidityRatio * 1000, 0.1), " g/kg", Core.Query.Round(Query.DiagramTemperature(mollierPoint), 0.01), mollierPoint.Pressure, mollierPoint.Enthalpy, Core.Query.Round(mollierPoint.WetBulbTemperature(), 0.01), Core.Query.Round(mollierPoint.SpecificVolume(), 0.01));
            }
            return null;
            
        }

        private string ToolTip(IMollierProcess mollierProcess)
        {
            if (mollierProcess is HeatingProcess)
            {
                return "Heating";
            }

            if (mollierProcess is CoolingProcess)
            {
                return "Cooling";
            }

            if (mollierProcess is MixingProcess)
            {
                return "Mixing";
            }

            if (mollierProcess is HeatRecoveryProcess)
            {
                return "Heat Recovery";
            }

            if (mollierProcess is HumidificationProcess)
            {
                return "Humidification";
            }

            return null;
        }

        public bool temperature_checking(double temperature_1, double temperature_2)
        {
            bool result = true;

            if (double.IsNaN(temperature_1))
            {
                MessageBox.Show("temperature_is_too_big");
                result = false;
            }
            if (double.IsNaN(temperature_2))
            {
                MessageBox.Show("temperature_is_too_low");
                result = false;
            }

            return result;
        }
        public double DryBulbTemp_by_wet(double WetBulbTemperature, double relative_humidity, double pressure)
        {
            double result = 99.9;
            while (Query.WetBulbTemperature(result, relative_humidity, pressure) > WetBulbTemperature)
            {
                result -= 0.1;
            }
            return result;
        }
        public double temperature_by_specific_volume(double specific_volume, double humidity_ratio, double pressure)
        {
            double result = 50;
            while (Query.SpecificVolume(result, humidity_ratio, pressure) > specific_volume)
            {
                count++;
                result -= 0.001;
            }
            return result;
        }
        public double humidity_ratio_for_100relativity(double dryBulbTemperature, Math.PolynomialEquation polynomialEquation, double pressure)
        {
            double result = 0;
            while (Query.RelativeHumidity(polynomialEquation.Evaluate(result * 1000), result, pressure) < 100)
            {
                count++;
                double x = polynomialEquation.Evaluate(result * 1000);
                double test = Query.RelativeHumidity(polynomialEquation.Evaluate(result * 1000), result, pressure);
                result += 0.0001;
            }
            return result;
        }

        private void MollierControl_Load(object sender, EventArgs e)
        {
            chartType = ChartType.Mollier;
            generate_graph();
        }

        public bool Density_line
        {
            get
            {
                return density_line;
            }
            set
            {
       
                if(density_line != value)
                {
                    density_line = value;
                    generate_graph();
                }
            }
        }
        public bool Enthalpy_line
        {
            get
            {
                return enthalpy_line;
            }

            set
            {
                if (enthalpy_line != value)
                {
                    enthalpy_line = value;
                    generate_graph();
                }
            }
        }
        public bool Specific_volume_line
        {
            get
            {
                return specific_volume_line;
            }

            set
            {
                if (specific_volume_line != value)
                {
                    specific_volume_line = value;
                    generate_graph();
                }
            }
        }
        public bool Wet_bulb_temperature_line
        {
            get
            {
                return wet_bulb_temperature_line;
            }

            set
            {
                if (wet_bulb_temperature_line != value)
                {
                    wet_bulb_temperature_line = value;
                    generate_graph();
                }
            }
        }

        public double Pressure
        {
            get
            {
                return pressure;
            }

            set
            {
                if(pressure != value)
                {
                    pressure = value;
                    generate_graph();
                    if(mollierPoints != null)
                    {
                        for(int i=0; i<mollierPoints.Count(); i++)
                        {
                            mollierPoints[i] = new MollierPoint(mollierPoints[i].DryBulbTemperature, mollierPoints[i].HumidityRatio, pressure);
                        }
                    }
                }
            }
        }
        public string Default_Color
        {
            get
            {
                return color;
            }

            set
            {
                if (color != value)
                {
                    color = value;
                    generate_graph();
                }
            }
        }
        public string Blue_Color
        {
            get
            {
                return color;
            }

            set
            {
                if(color != value)
                {
                    color = value;
                    generate_graph();
                }
            }
        }
        public string Gray_Color
        {
            get
            {
                return color;
            }

            set
            {
                if (color != value)
                {
                    color = value;
                    generate_graph();
                }
            }
        }
        public void AddPoints(IEnumerable<MollierPoint> mollierPoints)
        {
            if (mollierPoints == null)
                return;
            if(this.mollierPoints == null)
            {
                this.mollierPoints = new List<MollierPoint>();
            }
            this.mollierPoints.AddRange(mollierPoints);
            generate_graph();
        }

        public void AddProcess(IMollierProcess mollierProcess)
        {
            if(mollierProcess == null)
            {
                return;
            }

            if(mollierProcesses == null)
            {
                mollierProcesses = new List<IMollierProcess>();
            }

            mollierProcesses.Add(mollierProcess);
            generate_graph();
        }

        public bool Save()
        {
            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "emf files (*.emf)|*.emf|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }
                path = saveFileDialog.FileName;
            }

            MollierChart.SaveImage(path, ImageFormat.Emf);
            return true;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChartType ChartType
        {
            get
            {
                return chartType;
            }
            set
            {
                if (chartType != value)
                {
                    chartType = value;
                    generate_graph();
                }
            }
        }

    }
}
