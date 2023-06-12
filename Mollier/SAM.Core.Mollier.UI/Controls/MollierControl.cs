using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class MollierControl : UserControl
    {
        private ToolTip toolTip = new ToolTip();

        public event MollierPointSelectedEventHandler MollierPointSelected;

        //public static double MaxPressure = 108400, MinPressure = 90000;
        public static double MaxPressure = 400000, MinPressure = 35000;
        private Point mdown = Point.Empty;
        private bool selection = false;
        private MollierControlSettings mollierControlSettings;
        private List<MollierPoint> mollierPoints;
        private List<UIMollierProcess> mollierProcesses;
        private List<MollierZone> mollierZones;
        private List<List<UIMollierProcess>> systems;
        private List<Tuple<MollierPoint, string>> created_points;

        private List<Tuple<Series, int>> seriesData = new List<Tuple<Series, int>>();

        public MollierControl()
        {
            InitializeComponent();

            mollierControlSettings = new MollierControlSettings();


        }

        private void create_relative_humidity_line_Mollier(int temperature_Min, int temperature_Max, double relative_humidity, double pressure)
        {
            List<List<Point2D>> humidity_ratio_points = new List<List<Point2D>>();
            for (int i = temperature_Min; i <= temperature_Max; i++)
            {
                humidity_ratio_points.Add(new List<Point2D>());
            }
            int index = temperature_Min;
            for (int i = 1; i <= 10; i++)
            {
                bool adjust_RH = temperature_Min == -20 ? true : false;
                if (adjust_RH == true && i % 2 == 1)
                    temperature_Min = -10;
                string unit = (i * 10).ToString() + '%';
                Series series = MollierChart.Series.Add(unit);
                series.IsVisibleInLegend = false;
                series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, ChartDataType.RelativeHumidity);
                series.ChartType = SeriesChartType.Spline;
                List<Point2D> relative_humidity_points = new List<Point2D>();
                for (int j = temperature_Min; j <= temperature_Max; j++)
                {
                    double humidity_ratio = Mollier.Query.HumidityRatio(j, relative_humidity, pressure);
                    double diagram_temperature = Mollier.Query.DiagramTemperature(j, humidity_ratio);
                    if (humidity_ratio_points[j - index].Count == 0)
                        humidity_ratio_points[j - index].Add(new Point2D(0, j));
                    relative_humidity_points.Add(new Point2D(humidity_ratio * 1000, diagram_temperature));
                    humidity_ratio_points[j - index].Add(new Point2D(humidity_ratio * 1000, diagram_temperature));
                }
                foreach (Point2D point2D in relative_humidity_points)
                {
                    series.Points.AddXY(point2D.X, point2D.Y);
                    if (i == 10)
                    {
                        series.BorderWidth = 3;
                    }
                }
                //rotate relative humidity label
                int index_Point = 5;
                int count = relative_humidity_points.Count;
                if ((count - (index_Point + 1) - i < 0) || (count - (index_Point - 1) - i < 0))
                {
                    if (adjust_RH == true)
                        temperature_Min = -20;
                    relative_humidity += 10;
                    continue;
                }
                double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min) * 2;
                Point2D point2D_1 = relative_humidity_points[count - (index_Point - 1) - i];
                Point2D point2D_2 = relative_humidity_points[count - (index_Point + 1) - i];
                point2D_1.X *= range_difference;
                point2D_2.X *= range_difference;
                Vector2D vector2D = new Vector2D(point2D_2, point2D_1);
                int angle = System.Convert.ToInt32((vector2D.Angle(Vector2D.WorldX)) * 180 / System.Math.PI);
                string label = " Relative Humidity φ";
                series.SmartLabelStyle.Enabled = false;
                if (i == 5)
                {
                    string newLabel = "";
                    if (!mollierControlSettings.DisableUnits)
                    {
                        newLabel += unit;
                    }
                    if (!mollierControlSettings.DisableLabels)
                    {
                        newLabel += label;
                    }
                    series.Points[count - index_Point - i].Label = newLabel;
                }
                else if (!mollierControlSettings.DisableUnits)
                {
                    series.Points[count - index_Point - i].Label = unit;
                }
                series.Points[count - index_Point - i].LabelAngle = -angle;
                series.Points[count - index_Point - i].LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Unit, ChartDataType.RelativeHumidity);
                //series.Points[count - index_Point - i].LabelAngle = -(System.Convert.ToInt32(angle * 180 / System.Math.PI) - 22);
                if (adjust_RH == true)
                    temperature_Min = -20;
                relative_humidity += 10;
            }
            int list_size = humidity_ratio_points.Count;
            for (int i = 0; i < list_size; i++)
            {
                string unit_1 = (i - 25).ToString();
                Series series_1 = MollierChart.Series.Add(unit_1);
                series_1.IsVisibleInLegend = false;
                series_1.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, ChartDataType.DiagramTemperature);
                series_1.ChartType = SeriesChartType.Spline;
                List<Point2D> point2Ds_humidity = new List<Point2D>();
                point2Ds_humidity = humidity_ratio_points[i];
                for (int j = 0; j < point2Ds_humidity.Count; j++)
                {
                    series_1.Points.AddXY(point2Ds_humidity[j].X, point2Ds_humidity[j].Y);
                }
                if (i % 5 == 0)//bolds every 5th line 
                {
                    series_1.BorderWidth = 2;
                    series_1.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.BoldLine, ChartDataType.DiagramTemperature);
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
                series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, ChartDataType.RelativeHumidity);
                series.ChartType = SeriesChartType.Spline;
                List<Point2D> relative_humidity_points = new List<Point2D>();
                for (int j = temperature_Min; j <= temperature_Max; j++)
                {
                    double humidity_ratio = Mollier.Query.HumidityRatio(j, relative_humidity, pressure);
                    relative_humidity_points.Add(new Point2D(j, humidity_ratio));

                }
                foreach (Point2D point2D in relative_humidity_points)
                {
                    series.Points.AddXY(point2D.X, point2D.Y);
                    if (i == 10)
                    {
                        series.BorderWidth = 3;
                    }
                }
                int index_Point = 5;
                int count = relative_humidity_points.Count;
                if ((count - (index_Point + 1) - i < 0) || (count - (index_Point - 1) - i < 0))
                {
                    relative_humidity += 10;
                    continue;
                }
                double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min);
                Point2D point2D_1 = relative_humidity_points[count - (index_Point - 1) - i];
                Point2D point2D_2 = relative_humidity_points[count - (index_Point + 1) - i];
                point2D_2.X = 2 * point2D_2.X - point2D_1.X;
                point2D_2.Y *= 1000 * range_difference;
                point2D_1.Y *= 1000 * range_difference;
                Vector2D vector2D = new Vector2D(point2D_1, point2D_2);

                int angle = System.Convert.ToInt32(vector2D.Angle(Vector2D.WorldX) * 180 / System.Math.PI);
                string label = " Relative Humidity φ";
                series.SmartLabelStyle.Enabled = false;
                if (i == 5)
                {
                    string newLabel = "";
                    if (!mollierControlSettings.DisableUnits)
                    {
                        newLabel += unit;
                    }
                    if (!mollierControlSettings.DisableLabels)
                    {
                        newLabel += label;
                    }
                    series.Points[count - index_Point - i].Label = newLabel;
                }
                else if (!mollierControlSettings.DisableUnits)
                {
                    series.Points[count - index_Point - i].Label = unit;
                }
                series.Points[count - index_Point - i].LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Unit, ChartDataType.RelativeHumidity);
                series.Points[count - index_Point - i].LabelAngle = angle - 180;
                relative_humidity += 10;
            }
        }

        private void create_density_line(ChartType chartType, double density_Min, double density_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = Mollier.Query.DensityLine(density_Min, density_Max, pressure);
            if (dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, ChartDataType.Density, "kg / m³", "Density");

            Series series_Temp = series?.Find(x => x.Name.Contains((1.2).ToString()));
            if (series_Temp != null)
            {
                double X = series_Temp.Points[0].XValue;
                double Y = series_Temp.Points[0].YValues[0];
                int angle = findAngle(series_Temp, chartType);
                create_moved_label(chartType, X, Y, angle, angle, 2, -0.5, -0.5, 0.005, "Density ρ [kg/m³]", ChartDataType.Density, ChartParameterType.Label, mollierControlSettings.DisableLabels);
            }
        }

        private Dictionary<double, List<MollierPoint>> GetMollierPoints_Density(double density_Min, double density_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();
            while (density_Min <= density_Max)
            {
                result[density_Min] = new List<MollierPoint>();
                double temperature_1 = Mollier.Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 0, pressure);
                double humidityRatio_1 = Mollier.Query.HumidityRatio(temperature_1, 0, pressure);
                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[density_Min].Add(mollierPoint_1);

                double temperature_2 = Mollier.Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 100, pressure);
                double humidityRatio_2 = Mollier.Query.HumidityRatio(temperature_2, 100, pressure);
                MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, humidityRatio_2, pressure);
                result[density_Min].Add(mollierPoint_2);
                double a = (temperature_1 - temperature_2) / (humidityRatio_1 - humidityRatio_2);
                density_Min += 0.02;
            }
            return result;
        }

        private void create_enthalpy_line(ChartType chartType, double enthalpy_Min, double enthalpy_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = Mollier.Query.EnthalpyLine(chartType, enthalpy_Min, enthalpy_Max, pressure);
            if (dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, ChartDataType.Enthalpy, "kJ / kg", "Enthalpy");

            Series series_Temp = series?.Find(x => x.Name.Contains((20).ToString()));

            if (series_Temp != null)
            {
                double X = series_Temp.Points.Last().XValue;
                double Y = series_Temp.Points.Last().YValues[0];
                create_moved_label(chartType, X, Y, 0, 0, 6.5, 24, 27.0, 0.0054, "Enthalpy h [kJ/kg]", ChartDataType.Enthalpy, ChartParameterType.Label, mollierControlSettings.DisableLabels);
            }
        }

        private Dictionary<double, List<MollierPoint>> GetMollierPoints_Enthalpy(double enthalpy_Min, double enthalpy_Max, ChartType chartType, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();

            while (enthalpy_Min <= enthalpy_Max)
            {
                result[enthalpy_Min] = new List<MollierPoint>();
                double humidityRatio_Min = Mollier.Query.HumidityRatio_ByEnthalpy(-20, enthalpy_Min * 1000);

                double humidityRatio_1 = Mollier.Query.HumidityRatio_ByEnthalpy(100, enthalpy_Min * 1000);
                double temperature_1 = Mollier.Query.DryBulbTemperature(enthalpy_Min * 1000, humidityRatio_1);
                double temperature_2 = Mollier.Query.DryBulbTemperature_ByEnthalpy(enthalpy_Min * 1000, 100, pressure);
                double humidityRatio_2 = Mollier.Query.HumidityRatio(temperature_2, 100, pressure);

                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[enthalpy_Min].Add(mollierPoint_1);

                if (enthalpy_Min % 10 == 0 && chartType == ChartType.Psychrometric)
                {
                    Point2D Point_1 = new Point2D(humidityRatio_1, temperature_1);
                    Point2D Point_2 = new Point2D(humidityRatio_2, temperature_2);
                    Math.PolynomialEquation polynomialEquation = Geometry.Create.PolynomialEquation(new Point2D[] { Point_2, Point_1 });
                    double a = 0.0006;
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

        private void create_Wet_Bulb_Temperature_line(ChartType chartType, double temperature_Max, double wetBulbTemperature_Min, double wetBulbTemperature_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = Mollier.Query.WetBulbTemperatureLine(wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            if (dictionary == null)
            {
                return;
            }
            List<Series> series = CreateSeries(dictionary, chartType, ChartDataType.WetBulbTemperature, "°C", "Wet Bulb Temperature");

            Series series_Temp = series?.Find(x => x.Name.Contains((15).ToString()));
            if (series_Temp != null)
            {
                double X = series_Temp.Points.Last().XValue;
                double Y = series_Temp.Points.Last().YValues[0];
                int angle = findAngle(series_Temp, chartType);
                create_moved_label(chartType, X, Y, angle, angle, -1.2, 3.2, 4.5, -0.0018, "Wet Bulb Temperature t_wb [°C]", ChartDataType.WetBulbTemperature, ChartParameterType.Label, mollierControlSettings.DisableLabels);
            }
        }

        private Dictionary<double, List<MollierPoint>> GetMollierPoints_WetBulbTemperature(double wetBulbTemperature_Min, double wetBulbTemperature_Max, double temperature_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();
            while (wetBulbTemperature_Min <= wetBulbTemperature_Max)
            {
                result[wetBulbTemperature_Min] = new List<MollierPoint>();
                double temperature_1 = Mollier.Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature_Min, 0, pressure);
                double humidityRatio_1 = Mollier.Query.HumidityRatio(temperature_1, 0, pressure);
                if (wetBulbTemperature_Min == 30)
                {
                    temperature_1 = Mollier.Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature_Min, 20, pressure);
                    humidityRatio_1 = Mollier.Query.HumidityRatio(temperature_1, 20, pressure);
                }
                MollierPoint mollierPoint_1 = new MollierPoint(temperature_1, humidityRatio_1, pressure);
                result[wetBulbTemperature_Min].Add(mollierPoint_1);

                double temperature_2 = Mollier.Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature_Min, 100, pressure);
                double humidityRatio_2 = Mollier.Query.HumidityRatio(temperature_2, 100, pressure);
                MollierPoint mollierPoint_2 = new MollierPoint(temperature_2, humidityRatio_2, pressure);
                result[wetBulbTemperature_Min].Add(mollierPoint_2);

                wetBulbTemperature_Min += 5;
            }
            return result;
        }

        private void create_specific_volume_line(ChartType chartType, double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> dictionary = Mollier.Query.SpecificVolumeLine(specific_volume_Min, specific_volume_Max, pressure);
            if (dictionary == null || dictionary.Count == 0)
            {
                return;
            }

            List<Series> series = CreateSeries(dictionary, chartType, ChartDataType.SpecificVolume, "m³/kg", "specific volume");

            Series series_Temp = series?.Find(x => x.Name.Contains(0.9.ToString()));
            if (series_Temp != null)
            {
                double X = series_Temp.Points.Last().XValue;
                double Y = series_Temp.Points.Last().YValues[0];
                int angle = findAngle(series_Temp, chartType);
                create_moved_label(chartType, X, Y, angle, angle, -3, 0.8, 2.5, -0.005, "Specific volume v [m³/kg]", ChartDataType.SpecificVolume, ChartParameterType.Label, mollierControlSettings.DisableLabels);
            }
        }

        private Dictionary<double, List<MollierPoint>> GetMollierPoints_SpecificVolume(double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            Dictionary<double, List<MollierPoint>> result = new Dictionary<double, List<MollierPoint>>();

            while (specific_volume_Min <= specific_volume_Max)
            {
                result[specific_volume_Min] = new List<MollierPoint>();

                MollierPoint mollierPoint_1 = Mollier.Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(0, specific_volume_Min, pressure);
                result[specific_volume_Min].Add(mollierPoint_1);
                MollierPoint mollierPoint_2 = Mollier.Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(100, specific_volume_Min, pressure);
                result[specific_volume_Min].Add(mollierPoint_2);

                specific_volume_Min += 0.05;
            }

            return result;
        }


        private int findAngle(Series series, ChartType chartType)
        {
            //takes series (line must be straight) and chartType and returns angle of label along the line

            double range_difference = (mollierControlSettings.Temperature_Max - mollierControlSettings.Temperature_Min) / (mollierControlSettings.HumidityRatio_Max - mollierControlSettings.HumidityRatio_Min);
            Point2D a = new Point2D(); a.X = series.Points[0].XValue; a.Y = series.Points[0].YValues[0];
            Point2D b = new Point2D(); b.X = series.Points[1].XValue * range_difference; b.Y = series.Points[1].YValues[0];

            a.X = series.Points[0].XValue;
            a.Y = chartType == ChartType.Mollier ? series.Points[0].YValues[0] : series.Points[0].YValues[0] * 1000;
            b.X = chartType == ChartType.Mollier ? series.Points[1].XValue * range_difference * 2 : 2 * series.Points[1].XValue - a.X;
            b.Y = chartType == ChartType.Mollier ? series.Points[1].YValues[0] : series.Points[1].YValues[0] * 1000 * range_difference;

            Vector2D vector = chartType == ChartType.Mollier ? new Vector2D(a, b) : new Vector2D(a, b);
            if(Core.Query.AlmostEqual( vector.Length, 0))
            {
                return 0;
            }
            
            int result = System.Convert.ToInt32(vector.Angle(Vector2D.WorldX) * 180 / System.Math.PI);

            return chartType == ChartType.Mollier ? result : 180 - result;

        }

        private void create_moved_label(ChartType chartType, double X, double Y, int Mollier_angle, int Psychrometric_angle, double Mollier_X, double Mollier_Y, double Psychrometric_X, double Psychrometric_Y, string LabelContent, ChartDataType chartDataType, ChartParameterType chartParameterType, bool IsDisabled = false, bool fontChange = false, Color? color = null, string tag = null)
        {
            if (IsDisabled)
            {
                return;
            }
            double x = chartType == ChartType.Mollier ? Mollier_X : Psychrometric_X;
            double y = chartType == ChartType.Mollier ? Mollier_Y : Psychrometric_Y;
            //X, Y - coordinates of the point before moveing by x and y

            Series new_label = MollierChart.Series.Add(String.Format(LabelContent + chartDataType.ToString() + Guid.NewGuid().ToString()));
            new_label.IsVisibleInLegend = false;
            new_label.ChartType = SeriesChartType.Spline;
            if (tag == "ColorPointLabel")//in save as pdf we want to move this label(colorpointlabel) so it has to be point not spline
            {
                new_label.ChartType = SeriesChartType.Point;
            }
            new_label.Color = Color.Transparent;
            new_label.SmartLabelStyle.Enabled = false;
            new_label.Points.AddXY(X + x, Y + y);
            new_label.Label = LabelContent;
            new_label.LabelAngle = chartType == ChartType.Mollier ? Mollier_angle % 90 : Psychrometric_angle % 90;
            new_label.LabelForeColor = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, chartParameterType, chartDataType);
            if (color != null)
            {
                new_label.LabelForeColor = (Color)color;
            }
            if (fontChange)
            {
                new_label.Font = SystemFonts.MenuFont;
            }
            if (tag != null)
            {
                new_label.Tag = tag;
            }
        }

        private void createLabels(ChartType chartType, string name, ChartDataType chartDataType, Series series, double value)
        {
            if (series == null || series.Points == null || series.Points.Count == 0)
            {
                return;
            }

            double X, Y;
            int angle = 0;
            switch (name)
            {
                case "specific volume":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, chartDataType);
                    angle = findAngle(series, chartType);
                    create_moved_label(chartType, X, Y, angle, angle, -0.5, 0, 0.7, -0.0015, value.ToString(), chartDataType, ChartParameterType.Unit, mollierControlSettings.DisableUnits);
                    break;

                case "Wet Bulb Temperature":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, chartDataType);
                    create_moved_label(chartType, X, Y, 0, 0, 0, -1.8, -0.45, -0.00035, value.ToString(), chartDataType, ChartParameterType.Unit, mollierControlSettings.DisableUnits);
                    break;
                case "Density":
                    X = series.Points[0].XValue;
                    Y = series.Points[0].YValues[0];
                    series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, chartDataType);
                    series.BorderDashStyle = ChartDashStyle.Dash;
                    angle = findAngle(series, chartType);
                    create_moved_label(chartType, X, Y, angle, angle, 0.3, -0.2, 0.2, 0.0002, value.ToString(), chartDataType, ChartParameterType.Unit, mollierControlSettings.DisableUnits);
                    break;
                case "Enthalpy":
                    X = series.Points[1].XValue;
                    Y = series.Points[1].YValues[0];
                    series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, chartDataType);
                    if (value % 10 == 0)
                    {
                        series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.BoldLine, chartDataType);
                        if (chartType == ChartType.Mollier)
                            create_moved_label(chartType, X, Y, 0, 0, 0, -1.8, 0, 0, value.ToString(), chartDataType, ChartParameterType.Unit, mollierControlSettings.DisableUnits);
                        else
                            create_moved_label(chartType, X, Y, 0, 0, 0, 0, 0, -0.0002, value.ToString(), chartDataType, ChartParameterType.Unit, mollierControlSettings.DisableUnits);
                    }
                    break;
            }
        }

        private List<Series> CreateSeries(Dictionary<double, List<MollierPoint>> dictionary, ChartType chartType, ChartDataType chartDataType, string unit, string prefix)
        {
            List<Series> result = new List<Series>();
            foreach (KeyValuePair<double, List<MollierPoint>> keyValuePair in dictionary)
            {
                Series series = CreateSeries(keyValuePair.Value, chartType, chartDataType, keyValuePair.Key, unit, prefix);
                if (series != null)
                {
                    result.Add(series);
                }
            }

            return result;
        }

        private Series CreateSeries(List<MollierPoint> mollierPoints, ChartType chartType, ChartDataType chartDataType, double value, string unit, string prefix)
        {
            Series result = MollierChart.Series.Add(string.Format("{0} {1} {2}", prefix, value, unit));
            result.ChartType = SeriesChartType.Spline;
            result.IsVisibleInLegend = false;

            if (prefix == "Enthalpy" && System.Math.Round(mollierPoints[0].Enthalpy, 2) % 10000 == 0 && chartType == ChartType.Mollier)
            {
                double temperature_1 = Mollier.Query.DiagramTemperature(mollierPoints[0]);
                double humidityRatio_1 = mollierPoints[0].HumidityRatio * 1000;
                result.Points.AddXY(humidityRatio_1, temperature_1);

                double temperature_2 = Mollier.Query.DiagramTemperature(mollierPoints[1]);
                double humidityRatio_2 = mollierPoints[1].HumidityRatio * 1000;
                Point2D Point_1 = new Point2D(humidityRatio_1, temperature_1);
                Point2D Point_2 = new Point2D(humidityRatio_2, temperature_2);
                Math.PolynomialEquation polynomialEquation = Geometry.Create.PolynomialEquation(new Point2D[] { Point_2, Point_1 });
                humidityRatio_2 += 0.8;
                temperature_2 = polynomialEquation.Evaluate(humidityRatio_2);
                result.Points.AddXY(humidityRatio_2, temperature_2);

                createLabels(chartType, prefix, chartDataType, result, value);
                return result;
            }

            foreach (MollierPoint mollierPoint in mollierPoints)
            {
                double temperature = mollierPoint.DryBulbTemperature;
                if (double.IsNaN(temperature) || double.IsInfinity(temperature))
                {
                    continue;
                }

                double humidityRatio = mollierPoint.HumidityRatio;
                if (double.IsNaN(humidityRatio) || double.IsInfinity(humidityRatio))
                {
                    continue;
                }


                if (chartType == ChartType.Mollier)
                {
                    temperature = Mollier.Query.DiagramTemperature(mollierPoint);
                    if (double.IsNaN(temperature) || double.IsInfinity(temperature))
                    {
                        continue;
                    }

                    humidityRatio = humidityRatio * 1000;
                    result.Points.AddXY(humidityRatio, temperature);
                }
                else
                {
                    result.Points.AddXY(temperature, humidityRatio);
                }
            }

            if (result.Points == null || result.Points.Count == 0)
            {
                MollierChart.Series.Remove(result);
                return null;
            }

            createLabels(chartType, prefix, chartDataType, result, value);
            return result;
        }

        public void CreateYAxis(Chart chart, ChartArea area, Series series, float axisX, float axisWidth, float labelsSize, bool alignLeft, double P_w_Min, double P_w_Max)
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
            areaAxis.InnerPlotPosition
                    .FromRectangleF(areaSeries.InnerPlotPosition.ToRectangleF());

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
            areaAxisAxisY.Interval = mollierControlSettings.P_w_Interval / 1000;
            areaAxisAxisY.Title = "P_w  x [kPa]";

            areaAxis.AxisX2.Title = "";

            // Adjust area position
            areaAxis.Position.X = axisX;
            areaAxis.InnerPlotPosition.X += labelsSize;
        }

        public void CreateXAxis(Chart chart, ChartArea area, Series series, float axisY, float axisHeight, float labelsSize, bool alignLeft, double P_w_Min, double P_w_Max)
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
            areaAxis.InnerPlotPosition
                    .FromRectangleF(areaSeries.InnerPlotPosition.ToRectangleF());

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
            areaAxisAxisX.Interval = mollierControlSettings.P_w_Interval;

            areaAxis.AxisY.Title = "";

            areaAxisAxisX.Title = series.Name;
            //areaAxisAxisX.LineColor = series.Color;    // (*)
            //areaAxisAxisX.TitleForeColor = Color.DarkCyan;  // (*)

            // Adjust area position
            areaAxis.Position.Y = axisY;
            areaAxis.InnerPlotPosition.Y += labelsSize;
        }


        private void add_DivisionArea(ChartType chartType)
        {

            int deltaRelativeHumidity = 10;//RH interval from neighborhoodcount
            int deltaEnthalpy = 3;//enthalpy interval from neighborhoodcount

            //base size
            int RH_size = 100 / deltaRelativeHumidity + 7;
            int Ent_size = 200 / deltaEnthalpy + 7;

            List<MollierPoint>[,] rectangles_points = new List<MollierPoint>[RH_size, Ent_size];//for every rh interval and every enthalpy interval it stores the list of points that belong to this area 
            double maxCount;
            Query.NeighborhoodCount(mollierPoints, out maxCount, out rectangles_points);

            for (int rh = 0; rh <= 100 - deltaRelativeHumidity; rh += 10)
            {
                for (int e = -39; e <= 140 - deltaEnthalpy; e += 3)
                {
                    int index_1 = rh / deltaRelativeHumidity;
                    int index_2 = e / deltaEnthalpy + 15;
                    if (rectangles_points[index_1, index_2] == null)
                    {
                        continue;
                    }

                    Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
                    series.IsVisibleInLegend = false;
                    series.Tag = "GradientZone";
                    double pressure = mollierControlSettings.Pressure;
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e, "Y", chartType, pressure));//first corner                
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e + deltaEnthalpy, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e + deltaEnthalpy, "Y", chartType, pressure));//second corner               
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e + deltaEnthalpy, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e + deltaEnthalpy, "Y", chartType, pressure));//third corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity, e, "Y", chartType, pressure));//fourth corner
                    series.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh, e, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh, e, "Y", chartType, pressure));//first corner again to close the zone

                    double value = maxCount == 0 ? 0 : System.Convert.ToDouble(System.Convert.ToInt32(System.Math.Log(rectangles_points[index_1, index_2].Count))) / maxCount;
                    series.Color = Core.Query.Lerp(Color.Red, Color.Blue, value);
                    series.ChartType = SeriesChartType.Line;
                    series.BorderWidth = 3;
                    if (mollierControlSettings.DivisionAreaLabels)
                    {
                        Series label = MollierChart.Series.Add(Guid.NewGuid().ToString());
                        label.IsVisibleInLegend = false;
                        label.ChartType = SeriesChartType.Point;
                        if (MollierControlSettings.ChartType == ChartType.Mollier)
                        {
                            label.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "Y", chartType, pressure) - 0.5);
                        }
                        else
                        {
                            label.Points.AddXY(Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "X", chartType, pressure), Query.FindDivisionAreaCornerPoints(rh + deltaRelativeHumidity / 2, e + deltaEnthalpy / 2, "Y", chartType, pressure));
                        }
                        label.Color = Color.Transparent;
                        label.Label = rectangles_points[index_1, index_2].Count.ToString();
                        label.Tag = "GradientZoneLabel";

                    }
                }
            }
        }
        private void add_MollierZones(ChartType chartType)
        {
            foreach (MollierZone mollierZone in mollierZones)
            {
                Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 2;
                series.Color = Color.Blue;
                string zoneText = "";
                if (mollierZone is MollierControlZone)
                {
                    MollierControlZone mollierControlZone = (MollierControlZone)mollierZone;
                    series.Color = mollierControlZone.Color;
                    zoneText = mollierControlZone.Text;
                }

                List<MollierPoint> mollierPoints = mollierZone.MollierPoints;

                int size = mollierPoints.Count;
                for (int i = 0; i < size; i++)
                {
                    MollierPoint point = mollierPoints[i];
                    if (chartType == ChartType.Mollier)
                    {
                        series.Points.AddXY(point.HumidityRatio * 1000, Mollier.Query.DiagramTemperature(point));
                    }
                    else
                    {
                        series.Points.AddXY(point.DryBulbTemperature, point.HumidityRatio);
                    }
                }
                MollierPoint label = mollierZone.GetCenter();
                double labelX = chartType == ChartType.Mollier ? label.HumidityRatio * 1000 : label.DryBulbTemperature;
                double labelY = chartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(label) : label.HumidityRatio;
                create_moved_label(chartType, labelX, labelY, 0, 0, 0, 0, 0, 0, zoneText, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black);
            }
        }
        private void add_MollierPoints(ChartType chartType)
        {
            Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;
            series.Tag = mollierPoints;

            Dictionary<MollierPoint, int> dictionary = new Dictionary<MollierPoint, int>();
            double MaxCount = 0;
            List<MollierPoint>[,] rectangles_points;
            PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControlSettings.VisibilitySettings.GetVisibilitySetting("User", ChartParameterType.Point) as PointGradientVisibilitySetting;

            if (pointGradientVisibilitySetting != null)
            {
                dictionary = Query.NeighborhoodCount(mollierPoints, out MaxCount, out rectangles_points);
            }
            else
            {
                series.Color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Point, ChartDataType.Undefined);
            }
            //add points to the chart
            foreach (MollierPoint point in mollierPoints)
            {
                double humidity_ratio = point.HumidityRatio;
                double DryBulbTemperature = point.DryBulbTemperature;
                double diagram_temperature = Mollier.Query.DiagramTemperature(point);
                int index = chartType == ChartType.Mollier ? series.Points.AddXY(humidity_ratio * 1000, diagram_temperature) : series.Points.AddXY(DryBulbTemperature, humidity_ratio);
                //if gradient point is on then set a gradient point color with earlier counted intensity
                if (pointGradientVisibilitySetting != null)
                {
                    double value = MaxCount == 0 ? 0 : System.Convert.ToDouble(dictionary[point]) / MaxCount;
                    series.Points[index].Color = Core.Query.Lerp(pointGradientVisibilitySetting.Color, pointGradientVisibilitySetting.GradientColor, value);
                }
                series.Points[index].ToolTip = Query.ToolTipText(point, chartType, null);
                series.Points[index].Tag = point;

                //Series series1 = MollierChart.Series.Add(Guid.NewGuid().ToString());
                //series1.Points.AddXY(point.DryBulbTemperature, point.HumidityRatio);
                //series1.ChartType = SeriesChartType.Point;
                //series1.Label = "A2";
            }
        }
        private void add_MollierProcesses(ChartType chartType)
        {
            created_points = new List<Tuple<MollierPoint, string>>();//used for labeling in label process new 2

            List<UIMollierProcess> mollierProcesses_Temp = mollierProcesses == null ? null : new List<UIMollierProcess>(mollierProcesses.Cast<UIMollierProcess>());
            mollierProcesses_Temp?.Sort((x, y) => System.Math.Max(y.Start.HumidityRatio, y.End.HumidityRatio).CompareTo(System.Math.Max(x.Start.HumidityRatio, x.End.HumidityRatio)));
            createProcessesLabels(mollierProcesses_Temp, chartType);

            //create series for all points and lines in processes(create circles, lines, tooltips, ADP etc.)
            for (int i = 0; i < mollierProcesses.Count; i++)
            {
                UIMollierProcess UI_MollierProcess = mollierProcesses[i];//contains all spcified data of the process like color, start label etc.
                MollierProcess mollierProcess = (MollierProcess)(UI_MollierProcess.MollierProcess);//contains the most important data of the process: only start end point, and what type of process is it 

                if (mollierProcess is UndefinedProcess)
                {
                    createSeries_RoomProcess(UI_MollierProcess);
                    continue;
                }
                //process series
                Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
                series.IsVisibleInLegend = false;
                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 4;
                series.Color = (UI_MollierProcess.Color == Color.Empty) ? mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.Color, ChartParameterType.Line, mollierProcess) : UI_MollierProcess.Color;
                series.Tag = mollierProcess;

                MollierPoint start = mollierProcess?.Start;
                MollierPoint end = mollierProcess?.End;
                if (start == null || end == null)
                {
                    continue;
                }
                //creating series - processes points pattern
                createSeries_ProcessesPoints(start, UI_MollierProcess, chartType, toolTipName: UI_MollierProcess.Start_Label);
                createSeries_ProcessesPoints(end, UI_MollierProcess, chartType, toolTipName: UI_MollierProcess.End_Label);
                //add start and end point to the process series
                int index;
                series.ToolTip = Query.ToolTipText(start, end, chartType, Query.FullProcessName(UI_MollierProcess));
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
                    create_moved_label(chartType, X, Y, 0, 0, 0, -3 * Query.ScaleVector2D(this, MollierControlSettings).Y, -1 * Query.ScaleVector2D(this, MollierControlSettings).X, -0.0007 * Query.ScaleVector2D(this, MollierControlSettings).Y, "ADP", ChartDataType.Undefined, ChartParameterType.Point, color: Color.Gray);

                    MollierPoint ADPPoint_Temp = chartType == ChartType.Mollier ? new MollierPoint(apparatusDewPoint.DryBulbTemperature - 3 * Query.ScaleVector2D(this, MollierControlSettings).Y, apparatusDewPoint.HumidityRatio, mollierControlSettings.Pressure) : new MollierPoint(apparatusDewPoint.DryBulbTemperature - 1 * Query.ScaleVector2D(this, MollierControlSettings).X, apparatusDewPoint.HumidityRatio - 0.0007 * Query.ScaleVector2D(this, MollierControlSettings).Y, mollierControlSettings.Pressure);
                    created_points.Add(new Tuple<MollierPoint, string>(ADPPoint_Temp, "ADP"));

                    MollierPoint secondPoint = coolingProcess.DewPoint();
                    //creating series - processes points pattern
                    createSeries_ProcessesPoints(apparatusDewPoint, UI_MollierProcess, chartType, toolTipName: "Dew Point", pointType: "DewPoint");
                    createSeries_ProcessesPoints(secondPoint, UI_MollierProcess, chartType, pointType: "SecondPoint");
                    //creating series - special with ADP process pattern
                    createSeries_DewPoint(start, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
                    createSeries_DewPoint(end, apparatusDewPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);
                    createSeries_DewPoint(end, secondPoint, mollierProcess, chartType, Color.LightGray, 2, ChartDashStyle.Dash);


                    //Additional Lines 2023.06.06
                    List<MollierPoint> mollierPoints = Mollier.Query.ProcessMollierPoints(coolingProcess);
                    if (mollierPoints != null && mollierPoints.Count > 1)
                    {
                        for (int j = 0; j < mollierPoints.Count - 1; j++)
                        {
                            createSeries_DewPoint(mollierPoints[j], mollierPoints[j + 1], mollierProcess, chartType, Color.Gray, 3, ChartDashStyle.Solid);
                        }
                    }
                }

            }
            //labeling all the processes
            createPorcessesLabels_New(mollierProcesses_Temp, chartType);
        }

        private void createSeries_DewPoint(MollierPoint mollierPoint_1, MollierPoint mollierPoint_2, IMollierProcess mollierProcess, ChartType chartType, Color color, int borderWidth, ChartDashStyle borderDashStyle)
        {
            Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
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

        private void createSeries_ProcessesPoints(MollierPoint mollierPoint, UIMollierProcess UI_MollierProcess, ChartType chartType, string toolTipName = null, string pointType = "Default")
        {
            Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
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
        private void createSeries_RoomProcess(UIMollierProcess UI_MollierProcess)
        {
            //defines the end label of the process
            MollierProcess mollierProcess = (MollierProcess)(UI_MollierProcess.MollierProcess);
            //specified the color of the Room air condition point
            Color color = Color.Orange;
            color = UI_MollierProcess.Color == Color.Empty ? color : UI_MollierProcess.Color;
            //creating series for room process
            Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Line;
            series.Color = Color.Gray;
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
            series.ToolTip = Query.ToolTipText(start, end, mollierControlSettings.ChartType, Query.FullProcessName(UI_MollierProcess));

            //creating series for Room air condition point 
            Series seriesRoomPoint = MollierChart.Series.Add(Guid.NewGuid().ToString());
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
            if (UI_MollierProcess.Start_Label != null && UI_MollierProcess.Start_Label != "")
            {
                createSeries_ProcessesPoints(start, UI_MollierProcess, MollierControlSettings.ChartType);
            }

        }

        private void createProcessesLabels(List<UIMollierProcess> mollierProcesses, ChartType chartType)
        {
            if (systems == null)
            {
                return;
            }
            //Item1 - MollierPoint, Item2 - factor X, Item3 - factor Y
            List<Tuple<MollierPoint, double, double>> tuples = new List<Tuple<MollierPoint, double, double>>();
            char name = 'A';
            for (int i = 0; i < systems.Count; i++)
            {
                for (int j = 0; j < systems[i].Count; j++)
                {
                    UIMollierProcess UI_MollierProcess = systems[i][j];
                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess as MollierProcess;
                    if (UI_MollierProcess.End_Label == "SUP")
                    {
                        UI_MollierProcess.End_Label = null;
                    }


                    if (UI_MollierProcess.Start_Label == null && systems[i].Count == 1)
                    {
                        UI_MollierProcess.Start_Label = name + "1";
                    }
                    else if (UI_MollierProcess.Start_Label == null && j == 0)
                    {
                        UI_MollierProcess.Start_Label = "OSA";
                    }
                    if (UI_MollierProcess.End_Label == null && systems[i].Count > 1 && j == systems[i].Count - 2 && systems[i][j + 1].MollierProcess is UndefinedProcess)
                    {
                        UI_MollierProcess.End_Label = "SUP";
                    }
                    else if (UI_MollierProcess.End_Label == null && systems[i].Count > 1 && j == systems[i].Count - 1)
                    {
                        UI_MollierProcess.End_Label = "SUP";
                    }
                    UI_MollierProcess.Process_Label = UI_MollierProcess.Process_Label == null ? Query.ProcessName(mollierProcess) : UI_MollierProcess.Process_Label;
                    UI_MollierProcess.End_Label = UI_MollierProcess.End_Label == null ? name + "2" : UI_MollierProcess.End_Label;

                    name++;
                }
            }

            this.mollierProcesses = mollierProcesses;//used only to remember point name to show it in tooltip
        }
        private void createPorcessesLabels_New(List<UIMollierProcess> mollierProcesses, ChartType chartType)//creates sorted list of points that has to be labaled
        {
            List<Tuple<MollierPoint, string>> points_list = new List<Tuple<MollierPoint, string>>();

            foreach (UIMollierProcess UI_MollierProcess in mollierProcesses)
            {
                if (UI_MollierProcess.Start_Label != null && UI_MollierProcess.Start_Label != "")
                {
                    points_list.Add(new Tuple<MollierPoint, string>(UI_MollierProcess.Start, UI_MollierProcess.Start_Label));
                }
                if (UI_MollierProcess.End_Label != null && UI_MollierProcess.End_Label != "")
                {
                    points_list.Add(new Tuple<MollierPoint, string>(UI_MollierProcess.End, UI_MollierProcess.End_Label));
                }

            }
            points_list?.Sort((x, y) => x.Item1.HumidityRatio.CompareTo(y.Item1.HumidityRatio));
            foreach (UIMollierProcess UI_MollierProcess in mollierProcesses)
            {
                if (UI_MollierProcess.Process_Label != null && UI_MollierProcess.Process_Label != "")
                {
                    double dryBulbTemperature = (UI_MollierProcess.Start.DryBulbTemperature + UI_MollierProcess.End.DryBulbTemperature) / 2;
                    double humdityRatio = (UI_MollierProcess.Start.HumidityRatio + UI_MollierProcess.End.HumidityRatio) / 2;
                    MollierPoint mid = new MollierPoint(dryBulbTemperature, humdityRatio, mollierControlSettings.Pressure);
                    points_list.Add(new Tuple<MollierPoint, string>(mid, UI_MollierProcess.Process_Label));
                }
            }
            createPorcessesLabels_New_2(points_list);
        }

        private void createPorcessesLabels_New_2(List<Tuple<MollierPoint, string>> points_list)
        {
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                for (int i = 0; i < points_list.Count; i++)
                {
                    MollierPoint mollierPoint = points_list[i].Item1;
                    string label = points_list[i].Item2;
                    Vector2D vector2D = Query.ScaleVector2D(this, MollierControlSettings);
                    //1st option right
                    bool is_space = true;
                    //how much move the label 
                    double moveHumidityRatio = (0.25 + 0.2 * (double)label.Length / 2.0) * vector2D.X;
                    double moveTemperature = -1.4 * vector2D.Y;
                    //mollier point moved  riBght, top, left, down
                    MollierPoint mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
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
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -(0.25 + 0.2 * (double)label.Length / 2.0) * vector2D.X;
                        moveTemperature = -1.4 * vector2D.Y;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio / 1000, mollierControlSettings.Pressure);
                    }
                    //3rd option left
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
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
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
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
                    Vector2D vector2D = Query.ScaleVector2D(this, MollierControlSettings);
                    //1st option top
                    bool is_space = true;
                    //how much move the label 
                    double moveHumidityRatio = 0;
                    double moveTemperature = 0;
                    //mollier point moved  right, top, left, down
                    MollierPoint mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -0.0007 * vector2D.Y;
                        moveTemperature = (0.5 + 0.4 * (double)label.Length / 2.0) * vector2D.X;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    }

                    //2nd option right
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
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
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
                    {
                        created_points.Add(new Tuple<MollierPoint, string>(mollierPoint_Moved, label));
                        continue;
                    }
                    else
                    {
                        is_space = true;
                        moveHumidityRatio = -0.0007 * vector2D.Y;
                        moveTemperature = -(0.5 + 0.4 * (double)label.Length / 2.0) * vector2D.X;
                        mollierPoint_Moved = new MollierPoint(mollierPoint.DryBulbTemperature + moveTemperature, mollierPoint.HumidityRatio + moveHumidityRatio, mollierControlSettings.Pressure);
                    }
                    //4th option left  
                    for (int j = 0; j < created_points.Count; j++)
                    {
                        if (overlaps(mollierPoint_Moved, created_points[j], label))
                        {
                            is_space = false;
                            break;
                        }
                    }
                    if (is_space == true && !intersect(mollierPoint_Moved, label, mollierProcesses))//we're creating because there is a space
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

                create_moved_label(mollierControlSettings.ChartType, X, Y, 0, 0, 0, 0, 0, 0, created_points[i].Item2, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black);
            }
        }

        private bool overlaps(MollierPoint mollierPointNew, Tuple<MollierPoint, string> mollierPointLabeled, string label)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double NewPoint_X = chartType == ChartType.Mollier ? mollierPointNew.HumidityRatio * 1000 : mollierPointNew.DryBulbTemperature;
            double NewPoint_Y = chartType == ChartType.Mollier ? mollierPointNew.DryBulbTemperature : mollierPointNew.HumidityRatio;
            double OldPoint_X = chartType == ChartType.Mollier ? mollierPointLabeled.Item1.HumidityRatio * 1000 : mollierPointLabeled.Item1.DryBulbTemperature;
            double OldPoint_Y = chartType == ChartType.Mollier ? mollierPointLabeled.Item1.DryBulbTemperature : mollierPointLabeled.Item1.HumidityRatio;
            string OldLabel = mollierPointLabeled.Item2;
            Vector2D vector2D = Query.ScaleVector2D(this, MollierControlSettings);
            if (chartType == ChartType.Mollier)
            {
                double y = 0.95 * vector2D.Y;
                double x = 0.2 * vector2D.X;// 0.2 is one letter width in mollier
                x *= (double)label.Length;

                Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);
                Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);


                x = 0.2 * vector2D.X;
                x *= (double)OldLabel.Length;
                origin = new Point2D(OldPoint_X - x / 2.0, OldPoint_Y + y);
                Rectangle2D rectangle2Dold = new Rectangle2D(origin, x, y);

                return rectangle2Dnew.Intersect(rectangle2Dold, 0.001);
            }
            else
            {
                double y = 0.00049 * vector2D.Y;
                double x = 0.49 * vector2D.X;// 0.25 is one letter width in psychro
                x *= (double)label.Length;

                Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);
                Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);


                x = 0.49 * vector2D.X;
                x *= (double)OldLabel.Length;
                origin = new Point2D(OldPoint_X - x / 2.0, OldPoint_Y + y);
                Rectangle2D rectangle2Dold = new Rectangle2D(origin, x, y);

                return rectangle2Dnew.Intersect(rectangle2Dold, 0.0000001);
            }
            return false;
        }

        private bool intersect(MollierPoint mollierPointNew, string label, List<UIMollierProcess> mollierProcesses)
        {
            ChartType chartType = mollierControlSettings.ChartType;
            double NewPoint_X = chartType == ChartType.Mollier ? mollierPointNew.HumidityRatio * 1000 : mollierPointNew.DryBulbTemperature;
            double NewPoint_Y = chartType == ChartType.Mollier ? mollierPointNew.DryBulbTemperature : mollierPointNew.HumidityRatio;
            Vector2D vector2D = Query.ScaleVector2D(this, MollierControlSettings);
            double y = 0.95 * vector2D.Y;
            double x = 0.2 * vector2D.X;// 0.2 is one letter width in mollier
            x *= (double)label.Length;
            if (chartType == ChartType.Psychrometric)
            {
                y = 0.00048 * vector2D.Y;
                x = 0.48 * vector2D.X;// 0.25 is one letter width in psychro
                x *= (double)label.Length;
            }
            Point2D origin = new Point2D(NewPoint_X - x / 2.0, NewPoint_Y + y);

            Rectangle2D rectangle2Dnew = new Rectangle2D(origin, x, y);

            for (int i = 0; i < mollierProcesses.Count; i++)
            {
                IMollierProcess mollierProcess = mollierProcesses[i].MollierProcess;
                Point2D start = chartType == ChartType.Mollier ? new Point2D(mollierProcess.Start.HumidityRatio * 1000, mollierProcess.Start.DryBulbTemperature) : new Point2D(mollierProcess.Start.DryBulbTemperature, mollierProcess.Start.HumidityRatio);
                Point2D end = chartType == ChartType.Mollier ? new Point2D(mollierProcess.End.HumidityRatio * 1000, mollierProcess.End.DryBulbTemperature) : new Point2D(mollierProcess.End.DryBulbTemperature, mollierProcess.End.HumidityRatio);

                Segment2D segment2D = new Segment2D(start, end);

                if (rectangle2Dnew.Intersect(segment2D, Tolerance.MicroDistance))
                {
                    return true;
                }
            }
            return false;
        }


        public void generate_graph()
        {
            if (mollierControlSettings == null)
            {
                return;
            }

            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                generate_graph_mollier();
            }
            else if (mollierControlSettings.ChartType == ChartType.Psychrometric)
            {
                generate_graph_psychrometric();
            }
        }

        private void generate_graph_mollier()
        {
            //INITIAL SIZES
            double pressure = mollierControlSettings.Pressure;
            double humidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            double humidityRatio_interval = mollierControlSettings.HumidityRatio_Interval;
            double temperature_Min = mollierControlSettings.Temperature_Min;
            double temperature_Max = mollierControlSettings.Temperature_Max;
            double temperature_interval = mollierControlSettings.Temperature_Interval;
            bool density_line = mollierControlSettings.Density_line;
            bool enthalpy_line = mollierControlSettings.Enthalpy_line;
            bool specific_volume_line = mollierControlSettings.SpecificVolume_line;
            bool wet_bulb_temperature_line = mollierControlSettings.WetBulbTemperature_line;
            ChartType chartType = mollierControlSettings.ChartType;
            int wetBulbTemperature_Min = -10;
            int wetBulbTemperature_Max = 30;
            double density_Min = 0.45;//0.96
            double density_Max = 1.41;
            int enthalpy_Min = -20;
            int enthalpy_Max = 140;
            double specific_volume_Min = 0.65;
            double specific_volume_Max = 1.92; //0.95;
            double relative_humidity = 10;
            //checking whether creating a new graph has sense with this pressure

            double val = Mollier.Query.DryBulbTemperature_ByHumidityRatio(0.01, 100, pressure);
            double di = Mollier.Query.DiagramTemperature(val, 0.01);
            double RH = Mollier.Query.RelativeHumidity(val, 0.01, pressure);

            if (MinPressure > pressure || pressure > MaxPressure)
            {
                return;
            }
            //BASE CHART INITIALIZATION
            MollierChart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            MollierChart.Series.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            ChartArea ca = MollierChart.ChartAreas["ChartArea1"];
            ca.Position = new ElementPosition(2, 2, 95, 95);//define sizes of chart
            ca.InnerPlotPosition = new ElementPosition(7, 6, 88, 88);
            double P_w_max = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Max / 1000, pressure) / 1000;
            double P_w_min = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Min / 1000, pressure) / 1000;


            //AXIS X
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
            axisX.LabelStyle.Font = ca.AxisY.LabelStyle.Font;
            //areaAxisAxisY.LabelStyle.Font = area.AxisY.LabelStyle.Font;
            //AXIS Y
            MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.False;
            Axis axisY = chartArea.AxisY;
            axisY.Enabled = AxisEnabled.True;
            axisY.Title = "Dry Bulb Temperature t [°C]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = temperature_Max;
            axisY.Minimum = temperature_Min;
            axisY.Interval = temperature_interval;
            axisY.LabelStyle.Format = "0.##";
            axisY.LabelStyle.Font = ca.AxisY.LabelStyle.Font;

            //CREATING RELATIVE HUMIDITY AND DIAGRAM TEMPERATURE LINES
            // create_relative_humidity_line_New(System.Convert.ToInt32(temperature_Min), System.Convert.ToInt32(temperature_Max), pressure);
            //create_diagram_temperature_line_New(System.Convert.ToInt32(temperature_Min), System.Convert.ToInt32(temperature_Max), pressure);
            create_relative_humidity_line_Mollier(System.Convert.ToInt32(temperature_Min), System.Convert.ToInt32(temperature_Max), relative_humidity, pressure);

            //CREATING DENSITY LINE
            if (density_line)
                create_density_line(ChartType.Mollier, density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line(ChartType.Mollier, enthalpy_Min, enthalpy_Max, pressure);
            //CREATETING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_Wet_Bulb_Temperature_line(ChartType.Mollier, temperature_Max, wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line(ChartType.Mollier, specific_volume_Min, specific_volume_Max, pressure);
            //CREATING P_w AXIS
            Series series1 = MollierChart.Series.Add("Partial Vapour Pressure P_w [kPa]");
            series1.Points.AddXY(P_w_min, 0);
            series1.Points.AddXY(P_w_max, 0);
            series1.ChartType = SeriesChartType.Spline;
            series1.Color = Color.Transparent;
            series1.BorderColor = Color.Transparent;
            series1.IsVisibleInLegend = false;
            CreateXAxis(MollierChart, ca, series1, 2, 80, 1, false, P_w_min, P_w_max);

            if (mollierPoints != null && !mollierControlSettings.DivisionArea)
            {
                add_MollierPoints(chartType);
            }
            if (mollierProcesses != null)
            {
                add_MollierProcesses(chartType);
            }
            if (mollierZones != null)
            {
                add_MollierZones(chartType);
            }
            if (mollierControlSettings.DivisionArea)
            {
                add_DivisionArea(chartType);
            }
            ColorPoints(mollierControlSettings.FindPoint, mollierControlSettings.Percent, mollierControlSettings.FindPointType);
        }

        private void generate_graph_psychrometric()
        {
            //INITIAL SIZES
            int wetBulbTemperature_Min = -10;
            int wetBulbTemperature_Max = 30;
            double density_Min = 0.45;//0.96;
            double density_Max = 1.41;
            int enthalpy_Min = -20;
            int enthalpy_Max = 140;
            double specific_volume_Min = 0.65;// 0.75;
            double specific_volume_Max = 1.92;//0.95;
            double relative_humidity = 10;
            double pressure = mollierControlSettings.Pressure;
            double humidityRatio_Min = mollierControlSettings.HumidityRatio_Min;
            double humidityRatio_Max = mollierControlSettings.HumidityRatio_Max;
            double humidityRatio_interval = mollierControlSettings.HumidityRatio_Interval;
            double temperature_Min = mollierControlSettings.Temperature_Min;
            double temperature_Max = mollierControlSettings.Temperature_Max;
            double temperature_interval = mollierControlSettings.Temperature_Interval;
            bool density_line = mollierControlSettings.Density_line;
            bool enthalpy_line = mollierControlSettings.Enthalpy_line;
            bool specific_volume_line = mollierControlSettings.SpecificVolume_line;
            bool wet_bulb_temperature_line = mollierControlSettings.WetBulbTemperature_line;
            //checking whether creating a new graph has sense with this pressure
            if (MinPressure > pressure || pressure > MaxPressure)
            {
                return;
            }
            ChartType chartType = mollierControlSettings.ChartType;

            //BASE CHART INITIALIZATION
            MollierChart.Series?.Clear();
            ChartArea chartArea = MollierChart.ChartAreas[0];
            ChartArea ca = MollierChart.ChartAreas[0];
            ca.Position = new ElementPosition(2, 2, 95, 95);//define sizes of chart
            ca.InnerPlotPosition = new ElementPosition(8, 6, 85, 85);
            MollierChart.ChartAreas[0].AxisX2.Enabled = AxisEnabled.False;
            MollierChart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
            MollierChart.ChartAreas[0].AxisY2.Title = "Humidity Ratio  x [kg/kg]";
            MollierChart.ChartAreas[0].AxisY2.Maximum = humidityRatio_Max / 1000;
            MollierChart.ChartAreas[0].AxisY2.Minimum = humidityRatio_Min / 1000;
            MollierChart.ChartAreas[0].AxisY2.Interval = humidityRatio_interval / 1000;
            MollierChart.ChartAreas[0].AxisY2.MajorGrid.LineColor = Color.Gray;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Interval = 0.001;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.Enabled = true;
            MollierChart.ChartAreas[0].AxisY2.MinorGrid.LineColor = Color.LightGray;
            MollierChart.ChartAreas[0].AxisY2.LabelStyle.Format = "0.###";
            MollierChart.ChartAreas[0].AxisY2.LabelStyle.Font = ca.AxisY.LabelStyle.Font;
            double P_w_Min = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Min / 1000, pressure) / 1000000;
            double P_w_Max = Mollier.Query.PartialVapourPressure_ByHumidityRatio(humidityRatio_Max / 1000, pressure) / 1000000;
            //AXIS X
            Axis axisX = chartArea.AxisX;
            axisX.Title = "Dry Bulb Temperature t [°C]";
            axisX.Maximum = temperature_Max;
            axisX.Minimum = temperature_Min;
            axisX.Interval = temperature_interval;
            axisX.MajorGrid.LineColor = Color.Gray;
            axisX.MinorGrid.Interval = 1;
            axisX.MinorGrid.Enabled = true;
            axisX.MinorGrid.LineColor = Color.LightGray;
            axisX.LabelStyle.Font = ca.AxisY.LabelStyle.Font;
            //AXIS Y - P_w AXIS
            Axis axisY = chartArea.AxisY;
            axisY.Enabled = AxisEnabled.False;
            axisY.Title = "Partial Vapour Pressure P_w [kPa]";
            axisY.TextOrientation = TextOrientation.Rotated270;
            axisY.Maximum = humidityRatio_Max / 1000;
            //axisY.Minimum = humidityRatio_Min > humidityRatio_Max ? 0 : humidityRatio_Min / 1000;
            axisY.Minimum = humidityRatio_Min / 1000; //TODO: Fix Range
            axisY.Interval = humidityRatio_interval / 1000;
            axisY.MajorGrid.Enabled = false;
            axisY.MajorGrid.LineColor = Color.Gray;
            axisY.MinorGrid.Interval = 0.001;
            axisY.MinorGrid.Enabled = false;
            axisY.MinorGrid.LineColor = Color.LightGray;


            Series series1 = MollierChart.Series.Add("P_w_Psychro x [kPa]");
            series1.Points.AddXY(0, P_w_Min);
            series1.Points.AddXY(0, P_w_Max);
            series1.ChartType = SeriesChartType.Spline;
            series1.Color = Color.Transparent;
            series1.BorderColor = Color.Transparent;
            series1.IsVisibleInLegend = false;
            CreateYAxis(MollierChart, ca, series1, 5, 12, 25, true, P_w_Min, P_w_Max);


            //CREATING RELATIVE HUMIDITY LINES
            //create_relative_humidity_line_New(System.Convert.ToInt32(temperature_Min), System.Convert.ToInt32(temperature_Max), pressure);
            create_relative_humidity_line_Psychrometric(System.Convert.ToInt32(temperature_Min), System.Convert.ToInt32(temperature_Max), relative_humidity, pressure);

            //CREATING DENSITY LINE
            if (density_line)
                create_density_line(ChartType.Psychrometric, density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line(ChartType.Psychrometric, enthalpy_Min, enthalpy_Max, pressure);
            //CREATING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_Wet_Bulb_Temperature_line(ChartType.Psychrometric, temperature_Max, wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line(ChartType.Psychrometric, specific_volume_Min, specific_volume_Max, pressure);

            if (mollierPoints != null && !mollierControlSettings.DivisionArea)
            {
                add_MollierPoints(chartType);
            }

            if (mollierProcesses != null)
            {
                add_MollierProcesses(chartType);
            }
            if (mollierZones != null)
            {
                add_MollierZones(chartType);
            }
            if (mollierControlSettings.DivisionArea)
            {
                add_DivisionArea(chartType);
            }
            ColorPoints(mollierControlSettings.FindPoint, mollierControlSettings.Percent, mollierControlSettings.FindPointType);
        }



        public List<MollierPoint> AddPoints(IEnumerable<MollierPoint> mollierPoints, bool checkPressure = true)
        {
            if (mollierPoints == null)
                return null;
            if (this.mollierPoints == null)
            {
                this.mollierPoints = new List<MollierPoint>();
            }

            List<MollierPoint> mollierPointsResult = new List<MollierPoint>();
            foreach (MollierPoint point in mollierPoints)
            {
                //if (!checkPressure || Core.Query.AlmostEqual(point.Pressure, mollierControlSettings.Pressure, Tolerance.MacroDistance))
                //{
                mollierPointsResult.Add(point);
                //}
            }
            this.mollierPoints.AddRange(mollierPointsResult);
            generate_graph();
            return mollierPointsResult;
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
                //if (checkPressure && !Core.Query.AlmostEqual(mollierProcess.Pressure, mollierControlSettings.Pressure, Tolerance.MacroDistance))
                //{
                //    return null;
                //}
                if (!(mollierProcess is UIMollierProcess))
                {
                    UIMollierProcess mollierProcess_Temp = new UIMollierProcess(mollierProcess, Color.Empty);
                    mollierProcess = mollierProcess_Temp;
                }
                UIMollierProcess mollierProcess_UI = new UIMollierProcess(((UIMollierProcess)mollierProcess).MollierProcess, ((UIMollierProcess)mollierProcess).Color) { Start_Label = ((UIMollierProcess)mollierProcess).Start_Label, Process_Label = ((UIMollierProcess)mollierProcess).Process_Label, End_Label = ((UIMollierProcess)mollierProcess).End_Label };
                if (mollierProcess_UI.MollierProcess is UndefinedProcess && mollierProcess_UI.End_Label == null)
                {
                    mollierProcess_UI.End_Label = "ROOM";
                }
                this.mollierProcesses.Add(mollierProcess_UI);
                result.Add(mollierProcess_UI);
            }
            systems = Query.ProcessSortBySystem(this.mollierProcesses);
            generate_graph();
            return result;
        }

        public bool AddZone(MollierZone mollierZone)
        {
            if (mollierZone == null)
            {
                //for now to create possibility to disable Zone
                mollierZones = new List<MollierZone>();
                generate_graph();
                return false;
            }
            if (mollierZones == null)
            {
                mollierZones = new List<MollierZone>();
            }
            mollierZones.Add(mollierZone);
            generate_graph();
            return true;
        }

        public bool Clear()
        {
            mollierPoints?.Clear();
            mollierProcesses?.Clear();
            generate_graph();
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
                    uniqueNames.Add("[Enthalpy]");
                    uniqueNames.Add("[Density]");
                    uniqueNames.Add("[AtmosphericPressure]");
                    uniqueNames.Add("[SpecificVolume]");
                    uniqueNames.Add("[ProcessName]");
                    uniqueNames.Add("[deltaT]");
                    uniqueNames.Add("[deltaX]");
                    uniqueNames.Add("[deltaH]");
                    int numberOfData = 13;
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
                                MollierProcess mollierProcess = UI_MollierProcess.MollierProcess as MollierProcess;
                                if (UI_MollierProcess.Start_Label != null && UI_MollierProcess.Start_Label != "")
                                {
                                    range_1.Copy(worksheet.Range(worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min], worksheet.Cells[rowIndex_Min + move_Temp, columnIndex_Min + numberOfData]));
                                    move_Temp++;
                                }
                                if (UI_MollierProcess.End_Label != null && UI_MollierProcess.End_Label != "")
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
                                    MollierProcess mollierProcess = UI_MollierProcess.MollierProcess as MollierProcess;
                                    MollierPoint start = mollierProcess.Start;
                                    MollierPoint end = mollierProcess.End;
                                    string value_1 = string.Empty;
                                    string value_2 = string.Empty;
                                    switch (key_Temp)
                                    {
                                        case "[ProcessPointName]":
                                            value_1 = UI_MollierProcess.Start_Label;
                                            value_2 = UI_MollierProcess.End_Label;
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
                                            value_1 = System.Math.Round(start.RelativeHumidity, 2).ToString();
                                            value_2 = System.Math.Round(end.RelativeHumidity, 2).ToString();
                                            break;
                                        case "[WetBulbTemperature]":
                                            value_1 = System.Math.Round(start.WetBulbTemperature(), 2).ToString();
                                            value_2 = System.Math.Round(end.WetBulbTemperature(), 2).ToString();
                                            break;
                                        case "[Enthalpy]":
                                            value_1 = System.Math.Round(start.Enthalpy / 1000, 2).ToString();
                                            value_2 = System.Math.Round(end.Enthalpy / 1000, 2).ToString();
                                            break;
                                        case "[SpecificVolume]":
                                            value_1 = System.Math.Round(start.SpecificVolume(), 2).ToString();
                                            value_2 = System.Math.Round(end.SpecificVolume(), 2).ToString();
                                            break;
                                        case "[Density]":
                                            value_1 = System.Math.Round(start.Density(), 2).ToString();
                                            value_2 = System.Math.Round(end.Density(), 2).ToString();
                                            break;
                                        case "[AtmosphericPressure]":
                                            value_1 = System.Math.Round(start.Pressure, 2).ToString();
                                            value_2 = System.Math.Round(end.Pressure, 2).ToString();
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



                                    if (UI_MollierProcess.Start_Label != null && UI_MollierProcess.Start_Label != "")
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
                                    if (UI_MollierProcess.End_Label != null && UI_MollierProcess.End_Label != "")
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

                    shape.PictureFormat.Crop.ShapeHeight = (float)(shape.PictureFormat.Crop.ShapeHeight * Query.ShapeSizeFactor(DeviceDpi, 0.79));
                    shape.PictureFormat.Crop.ShapeWidth = (float)(shape.PictureFormat.Crop.ShapeWidth * Query.ShapeSizeFactor(DeviceDpi, 0.76));
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
            generate_graph();
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
            generate_graph();
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
                        g.DrawRectangle(Pens.Red, GetRectangle(mdown, e.Location));
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


            generate_graph();

            MollierChart.Refresh();
            selection = false;
        }

        static public Rectangle GetRectangle(Point p1, Point p2)
        {
            return new Rectangle(System.Math.Min(p1.X, p2.X), System.Math.Min(p1.Y, p2.Y), System.Math.Abs(p1.X - p2.X), System.Math.Abs(p1.Y - p2.Y));
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
                generate_graph();
            }
        }

        /// <summary>
        /// Checks if there exist any point
        /// </summary>
        public bool HasMollierPoints
        {
            get
            {
                return mollierPoints != null && mollierPoints.Count != 0;
            }
        }

        public List<MollierPoint> MollierPoints
        {
            get
            {
                if (mollierPoints == null)
                {
                    return null;
                }

                return mollierPoints.ConvertAll(x => new MollierPoint(x));
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

        private void MollierControl_SizeChanged(object sender, EventArgs e)
        {
            if (mollierControlSettings == null)
            {
                return;
            }

            generate_graph();
        }

        public void ColorPoints(bool generate, double percent, string chartDataType)
        {
            foreach (Series series_Temp in MollierChart.Series)
            {
                if (series_Temp.Tag == "ColorPoint")
                {
                    series_Temp.Enabled = false;
                }
            }
            if (generate == false || mollierPoints == null || mollierPoints.Count < 4 || percent > 100 || percent < 0)//if too 
            {
                return;
            }
            int index = System.Convert.ToInt32((1 - percent / 100) * mollierPoints.Count) - 1;
            if (index < 0)
            {
                index = 0;
            }

            List<MollierPoint> points = new List<MollierPoint>(mollierPoints);//copy of mollierPoints
            Series series = MollierChart.Series.Add(Guid.NewGuid().ToString());
            series.IsVisibleInLegend = false;
            series.ChartType = SeriesChartType.Point;
            series.BorderWidth = 4;
            series.MarkerColor = Color.Red;
            series.MarkerSize = 10;
            series.MarkerStyle = MarkerStyle.Circle;
            series.Tag = "ColorPoint";
            Series series1 = MollierChart.Series.Add(Guid.NewGuid().ToString());
            series1.IsVisibleInLegend = false;
            series1.ChartType = SeriesChartType.Point;
            series1.BorderWidth = 4;
            series1.MarkerColor = Color.Red;
            series1.MarkerSize = 15;
            series1.MarkerStyle = MarkerStyle.Circle;
            series1.Tag = "ColorPointLabelSquare";
            if (mollierControlSettings.ChartType == ChartType.Mollier)
            {
                series1.Points.AddXY((mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2, (mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4);
            }
            else
            {
                series1.Points.AddXY((mollierControlSettings.Temperature_Min + mollierControlSettings.Temperature_Max) / 4, (mollierControlSettings.HumidityRatio_Min + mollierControlSettings.HumidityRatio_Max) / 2000);
            }
            switch (chartDataType)
            {
                case "Temperature":
                    points.Sort((x, y) => x.DryBulbTemperature.CompareTo(y.DryBulbTemperature));
                    MollierPoint mollierPoint_Temperature = points[index];
                    double X_Temperature = mollierControlSettings.ChartType == ChartType.Mollier ? mollierPoint_Temperature.HumidityRatio * 1000 : mollierPoint_Temperature.DryBulbTemperature;
                    double Y_Temperature = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(mollierPoint_Temperature) : mollierPoint_Temperature.HumidityRatio;
                    series.Points.AddXY(X_Temperature, Y_Temperature);
                    string name_Temperature = Query.ToolTipText(mollierPoint_Temperature, mollierControlSettings.ChartType, "Temperature " + percent.ToString() + "%") + "\nUnmet hours: " + System.Math.Ceiling(percent / 100 * points.Count).ToString();
                    create_moved_label(mollierControlSettings.ChartType, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0, -16 * Query.ScaleVector2D(this, MollierControlSettings).Y, 0, 0, name_Temperature, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black, tag: "ColorPointLabel");
                    break;
                case "Enthalpy":
                    points.Sort((x, y) => x.Enthalpy.CompareTo(y.Enthalpy));
                    MollierPoint mollierPoint_Enthalpy = points[index];
                    double X_Enthalpy = mollierControlSettings.ChartType == ChartType.Mollier ? mollierPoint_Enthalpy.HumidityRatio * 1000 : mollierPoint_Enthalpy.DryBulbTemperature;
                    double Y_Enthalpy = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DiagramTemperature(mollierPoint_Enthalpy) : mollierPoint_Enthalpy.HumidityRatio;
                    series.Points.AddXY(X_Enthalpy, Y_Enthalpy);

                    string name_Enthalpy = Query.ToolTipText(mollierPoint_Enthalpy, mollierControlSettings.ChartType, "Enthalpy " + percent.ToString() + "%") + "\nUnmet hours: " + System.Math.Ceiling(percent / 100 * points.Count).ToString();
                    create_moved_label(mollierControlSettings.ChartType, series1.Points[0].XValue, series1.Points[0].YValues[0], 0, 0, 0, -16 * Query.ScaleVector2D(this, MollierControlSettings).Y, 0, 0, name_Enthalpy, ChartDataType.Undefined, ChartParameterType.Point, color: Color.Black, tag: "ColorPointLabel");
                    break;
            }
        }

        private void MollierChart_MouseClick(object sender, MouseEventArgs e)
        {
            MollierPoint mollierPoint = GetMollierPoint(e.X, e.Y);

            MollierPointSelected?.Invoke(this, new MollierPointSelectedEventArgs(mollierPoint));
        }

        public MollierPoint GetMollierPoint(int x, int y)
        {
            double chartX = MollierChart.ChartAreas[0].AxisX.PixelPositionToValue(x);
            double chartY = MollierChart.ChartAreas[0].AxisY.PixelPositionToValue(y);

            double humidityRatio = mollierControlSettings.ChartType == ChartType.Mollier ? chartX / 1000 : chartY;
            double dryBulbTemperature = mollierControlSettings.ChartType == ChartType.Mollier ? Mollier.Query.DryBulbTemperature_ByDiagramTemperature(chartY, humidityRatio) : chartX;

            MollierPoint result = new MollierPoint(dryBulbTemperature, humidityRatio, mollierControlSettings.Pressure);
            return result;
        }

        public void ClearObjects()
        {
            mollierPoints?.Clear();
            mollierProcesses?.Clear();
            mollierZones?.Clear();
            generate_graph();
        }
    }
}
