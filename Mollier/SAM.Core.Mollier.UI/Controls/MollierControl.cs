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
        private List<MollierPoint> mollierPoints;
        private List<IMollierProcess> mollierProcesses;
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
                series.Color = Color.LightBlue;
                series.ChartType = SeriesChartType.Spline;
                List<Geometry.Planar.Point2D> relative_humidity_points = new List<Geometry.Planar.Point2D>();
                for (int j = temperature_Min; j <= temperature_Max; j++)
                {
                    double humidity_ratio = Query.HumidityRatio(j, relative_humidity, pressure);
                    double diagram_temperature = Query.DiagramTemperature(j, humidity_ratio);
                    double density = Query.Density(j, relative_humidity, pressure);
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
                series.Points[count - index_Point - i].LabelForeColor = Color.Gray;
                series.Points[count - index_Point - i].LabelAngle = -(System.Convert.ToInt32(angle * 180 / System.Math.PI) - 22);
                relative_humidity += 10;
            }
            int list_size = humidity_ratio_points.Count;
            for (int i = 0; i < list_size; i++)
            {
                string unit_1 = (i - 25).ToString();
                Series series_1 = MollierChart.Series.Add(unit_1);
                series_1.IsVisibleInLegend = false;
                series_1.Color = Color.LightGray;
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
                    series_1.Color = Color.DarkGray;
                }
            }
        }
        private void create_density_line_Mollier(double density_Min, double density_Max, double pressure)
        {
            while (density_Min <= density_Max)
            {
                Series series_density = MollierChart.Series.Add(String.Format("Density {0}{1}", density_Min, "kg/m3"));
                series_density.ChartType = SeriesChartType.Spline;
                series_density.Color = Color.LightGreen;
                series_density.BorderWidth = 1;
                double temperature_1 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 0, pressure);
                double temperature_2 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 100, pressure);
                temperature_checking(temperature_1, temperature_2);//checks whether temperature is out of range
                double humidity_ratio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidity_ratio_1);
                double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                series_density.Points.AddXY(humidity_ratio_1 * 1000, diagram_temperature_1);
                series_density.Points.AddXY(humidity_ratio_2 * 1000, diagram_temperature_2);
                series_density.BorderDashStyle = ChartDashStyle.DashDotDot;
                series_density.IsVisibleInLegend = false;

                //creating labels for 
                Series series_name = MollierChart.Series.Add(String.Format("label {0}{1}", density_Min, "kg/m³"));
                Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(humidity_ratio_1 * 1000 + 1.5, diagram_temperature_1);
                Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(humidity_ratio_1 * 1000 + 0.4, diagram_temperature_1);
                if (density_Min.ToString() == (1.2).ToString())
                    series_name.Points.AddXY(Point_1.X, Point_1.Y);
                else
                    series_name.Points.AddXY(Point.X, Point.Y);
                series_name.ChartType = SeriesChartType.Spline;
                DataPoint datapoint = series_name.Points[0];
                if (density_Min.ToString() == (1.2).ToString())
                    datapoint.Label = density_Min.ToString() + "  Density [ kg/m³ ]";
                else
                    datapoint.Label = density_Min.ToString();
                datapoint.LabelForeColor = Color.Gray;
                series_name.IsVisibleInLegend = false;
                density_Min += 0.02;
            }
        }
        private void create_enthalpy_line_Mollier(double enthalpy_Min, double enthalpy_Max, double pressure)
        {
            while (enthalpy_Min <= enthalpy_Max)
            {
                Series series_enthalpy = MollierChart.Series.Add(String.Format("Enthalpy {0}{1}", enthalpy_Min, "kJ/kg"));
                series_enthalpy.ChartType = SeriesChartType.Spline;
                series_enthalpy.Color = Color.LightGray;

                double humidityRatio_Min = Query.HumidityRatio_ByEnthalpy(-20, enthalpy_Min * 1000);
                double humidityRatio_Max = Query.HumidityRatio_ByEnthalpy(100, enthalpy_Min * 1000);

                double temperature_1 = Query.DiagramTemperature(100, humidityRatio_Max);
                double temperature_2 = Query.DryBulbTemperature_ByEnthalpy(enthalpy_Min * 1000, 100, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                bool Point_added = false;//it checks if we added point on every 5th line if we did we dont want to add another on 100% RH line
                if (temperature_checking(temperature_1, temperature_2))//checks whether temperature is out of range

                {
                    double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidityRatio_Max);
                    double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                    Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(humidityRatio_Max * 1000, diagram_temperature_1);
                    Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(humidity_ratio_2 * 1000, diagram_temperature_2);
                    series_enthalpy.Points.AddXY(Point_1.X, Point_1.Y);
                    series_enthalpy.IsVisibleInLegend = false;
                    Math.PolynomialEquation polynomialEquation = SAM.Geometry.Create.PolynomialEquation(new Geometry.Planar.Point2D[] { Point_2, Point_1 });
                    double a = 1.5;
                    if (enthalpy_Min % 10 == 0)
                    {
                        double Y = polynomialEquation.Evaluate(Point_2.X + a);
                        Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(Point_2.X + a, Y);
                        series_enthalpy.Points.AddXY(Point.X, Point.Y);
                        Point_added = true;
                        int count = series_enthalpy.Points.Count;
                        DataPoint data_point = series_enthalpy.Points[count - 1];
                        data_point.Label = enthalpy_Min.ToString();
                        data_point.Label.PadLeft(1);
                        series_enthalpy.Color = Color.Gray;
                    }
                    if (enthalpy_Min == 55)
                    {
                        Series series_name = MollierChart.Series.Add(String.Format("name"));
                        series_name.IsVisibleInLegend = false;
                        double Y = polynomialEquation.Evaluate(Point_2.X + a);
                        Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(Point_2.X + a + 0.4, Y - 4.3);
                        series_name.Points.AddXY(Point.X, Point.Y);
                        series_name.ChartType = SeriesChartType.Spline;
                        DataPoint data_point = series_name.Points[0];
                        data_point.Label = "Enthalpy  h [ kJ/kg ]";
                    }
                    if (Point_added == false)
                        series_enthalpy.Points.AddXY(Point_2.X, Point_2.Y);
                }
                enthalpy_Min += 1;
            }
        }
        private void create_specific_volume_line_Mollier(double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            while (specific_volume_Min <= specific_volume_Max)
            {
                Series series_specific_volume = MollierChart.Series.Add(String.Format("specific volume {0} {1}", specific_volume_Min, "kg/m³"));
                series_specific_volume.ChartType = SeriesChartType.Spline;
                series_specific_volume.IsVisibleInLegend = false;
                series_specific_volume.Color = Color.LightSlateGray;
                
                //OLD VERSION START
                
                //double temperature_1 = temperature_by_specific_volume(specific_volume_Min, 0, pressure);
                //double temperature_2 = temperature_by_specific_volume(specific_volume_Min, 0.05, pressure);
                //Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(0, temperature_1);
                //Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(50, temperature_2);
                //Math.PolynomialEquation polynomialEquation = SAM.Geometry.Create.PolynomialEquation(new Geometry.Planar.Point2D[] { Point_1, Point_2 });
                
                //double humidity_ratio_2 = humidity_ratio_for_100relativity(temperature_1, polynomialEquation, pressure);
                //double temperature_p2 = polynomialEquation.Evaluate(humidity_ratio_2 * 1000);

                //OLD VERSION END

                // NEW VERSION START

                MollierPoint mollierPoint_1 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(0, specific_volume_Min, pressure);
                double temperature_1 = mollierPoint_1.DryBulbTemperature;

                MollierPoint mollierPoint_2 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(100, specific_volume_Min, pressure);
                double humidity_ratio_2 = mollierPoint_2.HumidityRatio;
                double temperature_p2 = mollierPoint_2.DryBulbTemperature;

                //NEW VERSION END

                //Geometry.Planar.Point2D Point_3 = new Geometry.Planar.Point2D(humidity_ratio_2 * 1000, temperature_p2);
                double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, 0);
                double diagram_temperature_2 = Query.DiagramTemperature(temperature_p2, humidity_ratio_2);
                Geometry.Planar.Point2D Point_4 = new Geometry.Planar.Point2D(0, diagram_temperature_1);
                Geometry.Planar.Point2D Point_5 = new Geometry.Planar.Point2D(humidity_ratio_2 * 1000, diagram_temperature_2);
                series_specific_volume.Points.AddXY(Point_4.X, Point_4.Y);
                series_specific_volume.Points.AddXY(Point_5.X, Point_5.Y);
                DataPoint datapoint = series_specific_volume.Points[1];
                if (specific_volume_Min.ToString() == (0.9).ToString())
                {
                    Series series_name = MollierChart.Series.Add(String.Format("name1"));
                    series_name.IsVisibleInLegend = false;
                    series_name.ChartType = SeriesChartType.Spline;
                    series_name.Points.AddXY(Point_5.X - 2, Point_5.Y + 0.5);
                    series_name.SmartLabelStyle.Enabled = false;
                    series_name.Label = "Specific volume [ kg/m³ ]";
                    series_name.LabelAngle = 6;
                    series_name.LabelForeColor = Color.Gray;
                }
                datapoint.Label = specific_volume_Min.ToString();
                datapoint.LabelForeColor = Color.LightSlateGray;
                specific_volume_Min += 0.05;
            }
        }
        private void create_wet_bulb_temperature_line_Mollier(double wetBulbTemperature_Min, double wetBulbTemperature_Max, double pressure)
        {
            while (wetBulbTemperature_Min <= wetBulbTemperature_Max)
            {
                Series series_WetBulbTemperature = MollierChart.Series.Add(String.Format("Wet Bulb Temperature {0} {1}", wetBulbTemperature_Min, "°C"));
                series_WetBulbTemperature.ChartType = SeriesChartType.Spline;
                series_WetBulbTemperature.Color = Color.LightSalmon;
                series_WetBulbTemperature.IsVisibleInLegend = false;
                double temperature_1 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 0, pressure);
                double temperature_2 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 100, pressure);
                double humidity_ratio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidity_ratio_1);
                double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(humidity_ratio_1 * 1000, diagram_temperature_1);
                Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(humidity_ratio_2 * 1000, diagram_temperature_2);
                series_WetBulbTemperature.Points.AddXY(Point_1.X, Point_1.Y);
                series_WetBulbTemperature.Points.AddXY(Point_2.X, Point_2.Y);
                DataPoint datapoint = series_WetBulbTemperature.Points[1];
                datapoint.Label = wetBulbTemperature_Min.ToString();
                datapoint.LabelForeColor = Color.LightSlateGray;
                if (wetBulbTemperature_Min == 15)//added label - "wet bulb temperature" on this level
                {
                    Series series_name = MollierChart.Series.Add(String.Format("name {0}", wetBulbTemperature_Min));
                    series_name.IsVisibleInLegend = false;
                    series_name.Points.AddXY(Point_2.X - 1.2, Point_2.Y + 3.2);
                    series_name.ChartType = SeriesChartType.Spline;
                    series_name.SmartLabelStyle.Enabled = false;
                    series_name.Label = "Wet Bulb Temperature [ °C ]";
                    series_name.LabelForeColor = Color.LightSlateGray;
                    Geometry.Planar.Point2D point2D_1 = new Geometry.Planar.Point2D(Point_2.X + 2, Point_2.Y + 2);
                    Geometry.Planar.Vector2D vector2D = new Geometry.Planar.Vector2D(Point_2, point2D_1);
                    series_name.LabelAngle = 33;
                }
                wetBulbTemperature_Min += 5;
            }
        }
        private void create_relative_humidity_line_Psychrometric(int temperature_Min, int temperature_Max, double relative_humidity, double pressure)
        {
            for (int i = 1; i <= 10; i++)
            {
                string unit = (i * 10).ToString() + '%';
                Series series = MollierChart.Series.Add(unit);
                series.IsVisibleInLegend = false;
                series.Color = Color.LightBlue;
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
                series.Points[count - index_Point - i].LabelForeColor = Color.Gray;
                series.Points[count - index_Point - i].LabelAngle = (System.Convert.ToInt32(angle * 180 / System.Math.PI) - 50);
                relative_humidity += 10;
            }
        }
        private void create_density_line_Psychrometric(double density_Min, double density_Max, double pressure)
        {
            while (density_Min <= density_Max)
            {
                Series series_density = MollierChart.Series.Add(String.Format("Density {0}{1}", density_Min, "kg/m³"));
                series_density.ChartType = SeriesChartType.Spline;
                series_density.Color = Color.LightGreen;
                series_density.BorderWidth = 1;
                double temperature_1 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 0, pressure);
                double temperature_2 = Query.DryBulbTemperature_ByDensityAndRelativeHumidity(density_Min, 100, pressure);
                temperature_checking(temperature_1, temperature_2);//checks whether temperature is out of range
                double humidity_ratio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidity_ratio_1);
                double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                series_density.Points.AddXY(temperature_1, humidity_ratio_1);
                series_density.Points.AddXY(temperature_2, humidity_ratio_2);
                series_density.BorderDashStyle = ChartDashStyle.DashDotDot;
                series_density.IsVisibleInLegend = false;
                //creating labels for 
                Series series_name = MollierChart.Series.Add(String.Format("label {0}{1}", density_Min, "kg/m³"));
                Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(temperature_1, humidity_ratio_1 + 0.0015);
                if (density_Min.ToString() == (1.2).ToString())
                {
                    Series serieslabel = MollierChart.Series.Add(String.Format("name"));
                    serieslabel.Points.AddXY(temperature_1, humidity_ratio_1 + 0.005);
                    serieslabel.ChartType = SeriesChartType.Spline;
                    DataPoint dataname = serieslabel.Points[0];
                    serieslabel.LabelForeColor = Color.Gray;
                    serieslabel.Points[0].Label = "Density [ kg/m³ ]";
                    serieslabel.LabelAngle = 80;
                    serieslabel.SmartLabelStyle.Enabled = false;
                    serieslabel.IsVisibleInLegend = false;
                }
                series_name.Points.AddXY(Point.X, Point.Y);
                series_name.ChartType = SeriesChartType.Spline;
                series_name.SmartLabelStyle.Enabled = false;
                series_name.Points[0].Label = density_Min.ToString();
                series_name.Points[0].LabelAngle = 90;
                series_name.LabelForeColor = Color.Gray;
                series_name.IsVisibleInLegend = false;
                density_Min += 0.02;
            }
        }
        private void create_enthalpy_line_Psychrometric(double enthalpy_Min, double enthalpy_Max, double pressure)
        {
            while (enthalpy_Min <= enthalpy_Max)
            {
                Series series_enthalpy = MollierChart.Series.Add(String.Format("Enthalpy {0}{1}", enthalpy_Min, "kJ/kg"));
                series_enthalpy.ChartType = SeriesChartType.Spline;
                series_enthalpy.Color = Color.LightGray;

                double humidityRatio_Min = Query.HumidityRatio_ByEnthalpy(-20, enthalpy_Min * 1000);
                double humidityRatio_Max = Query.HumidityRatio_ByEnthalpy(100, enthalpy_Min * 1000);

                double temperature_1 = Query.DiagramTemperature(100, humidityRatio_Max);
                double temperature_2 = Query.DryBulbTemperature_ByEnthalpy(enthalpy_Min * 1000, 100, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                bool Point_added = false;//it checks if we added point on every 5th line if we did we dont want to add another on 100% RH line
                if (temperature_checking(temperature_1, temperature_2))//checks whether temperature is out of range

                {
                    double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidityRatio_Max);
                    double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                    Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(temperature_1, humidityRatio_Max);
                    Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(temperature_2, humidity_ratio_2);
                    series_enthalpy.Points.AddXY(Point_1.X, Point_1.Y);
                    series_enthalpy.IsVisibleInLegend = false;
                    Math.PolynomialEquation polynomialEquation = SAM.Geometry.Create.PolynomialEquation(new Geometry.Planar.Point2D[] { Point_2, Point_1 });
                    double a = 1.5;
                    if (enthalpy_Min % 10 == 0)
                    {
                        double Y = polynomialEquation.Evaluate(Point_2.X - a);
                        Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(Point_2.X - a, Y);
                        series_enthalpy.Points.AddXY(Point.X, Point.Y);
                        Point_added = true;
                        int count = series_enthalpy.Points.Count;
                        DataPoint data_point = series_enthalpy.Points[count - 1];
                        data_point.Label = enthalpy_Min.ToString();
                        data_point.Label.PadLeft(1);
                        series_enthalpy.Color = Color.Gray;
                    }
                    if (enthalpy_Min == 55)
                    {
                        Series series_name = MollierChart.Series.Add(String.Format("name123"));
                        series_name.IsVisibleInLegend = false;
                        double Y = polynomialEquation.Evaluate(Point_2.X - a);
                        Geometry.Planar.Point2D Point = new Geometry.Planar.Point2D(Point_2.X - a - 4, Y);
                        series_name.Points.AddXY(Point.X, Point.Y);
                        series_name.ChartType = SeriesChartType.Spline;
                        DataPoint data_point = series_name.Points[0];
                        data_point.Label = "Enthalpy  h [ kJ/kg ]";
                    }
                    if (Point_added == false)
                        series_enthalpy.Points.AddXY(Point_2.X, Point_2.Y);
                }
                enthalpy_Min += 1;
            }
        }
        private void create_specific_volume_line_Psychrometric(double specific_volume_Min, double specific_volume_Max, double pressure)
        {
            while (specific_volume_Min <= specific_volume_Max)
            {
                Series series_specific_volume = MollierChart.Series.Add(String.Format("specific volume {0} {1}", specific_volume_Min, "kg/m³"));
                series_specific_volume.ChartType = SeriesChartType.Spline;
                series_specific_volume.IsVisibleInLegend = false;
                series_specific_volume.Color = Color.LightSlateGray;

                //OLD VERSION START

                //double temperature_1 = temperature_by_specific_volume(specific_volume_Min, 0, pressure);
                //double temperature_2 = temperature_by_specific_volume(specific_volume_Min, 0.05, pressure);
                //Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(0, temperature_1);
                //Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(50, temperature_2);
                //Math.PolynomialEquation polynomialEquation = SAM.Geometry.Create.PolynomialEquation(new Geometry.Planar.Point2D[] { Point_1, Point_2 });
                //double humidity_ratio_2 = humidity_ratio_for_100relativity(temperature_1, polynomialEquation, pressure);
                //double temperature_p2 = polynomialEquation.Evaluate(humidity_ratio_2 * 1000);
                //Geometry.Planar.Point2D Point_3 = new Geometry.Planar.Point2D(humidity_ratio_2 * 1000, temperature_p2);
                //double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, 0);
                //double diagram_temperature_2 = Query.DiagramTemperature(temperature_p2, humidity_ratio_2);

                //OLD VERSION END

                //NEW VERSION START

                MollierPoint mollierPoint_1 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(0, specific_volume_Min, pressure);
                double temperature_1 = mollierPoint_1.DryBulbTemperature;

                MollierPoint mollierPoint_2 = Create.MollierPoint_ByRelativeHumidityAndSpecificVolume(100, specific_volume_Min, pressure);
                double humidity_ratio_2 = mollierPoint_2.HumidityRatio;
                double temperature_p2 = mollierPoint_2.DryBulbTemperature;

                //NEW VERSION END

                Geometry.Planar.Point2D Point_4 = new Geometry.Planar.Point2D(temperature_1, 0);
                Geometry.Planar.Point2D Point_5 = new Geometry.Planar.Point2D(temperature_p2, humidity_ratio_2);
                series_specific_volume.Points.AddXY(Point_4.X, Point_4.Y);
                series_specific_volume.Points.AddXY(Point_5.X, Point_5.Y);
                DataPoint datapoint = series_specific_volume.Points[1];
                if (specific_volume_Min.ToString() == (0.9).ToString())
                {
                    Series series_name = MollierChart.Series.Add(String.Format("name1"));
                    series_name.IsVisibleInLegend = false;
                    series_name.ChartType = SeriesChartType.Spline;
                    series_name.Points.AddXY(Point_5.X + 3, Point_5.Y - 0.005);
                    series_name.SmartLabelStyle.Enabled = false;
                    series_name.Label = "Specific volume [ kg/m³ ]";
                    series_name.LabelAngle = 66;
                    series_name.LabelForeColor = Color.Gray;
                }
                datapoint.Label = specific_volume_Min.ToString();
                datapoint.LabelForeColor = Color.LightSlateGray;
                specific_volume_Min += 0.05;
            }
        }
        private void create_wet_bulb_temperature_line_Psychrometric(double wetBulbTemperature_Min, double wetBulbTemperature_Max, double pressure)
        {
            while (wetBulbTemperature_Min <= wetBulbTemperature_Max)
            {
                Series series_WetBulbTemperature = MollierChart.Series.Add(String.Format("Wet Bulb Temperature {0} {1}", wetBulbTemperature_Min, "°C"));
                series_WetBulbTemperature.ChartType = SeriesChartType.Spline;
                series_WetBulbTemperature.Color = Color.LightSalmon;
                series_WetBulbTemperature.IsVisibleInLegend = false;
                series_WetBulbTemperature.Enabled = true;
                //double temperature_1 = Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature_Min, 0, pressure);
                //double temperature_2 = Query.DryBulbTemperature_ByWetBulbTemperature(wetBulbTemperature_Min, 100, pressure);
                double temperature_1 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 0, pressure);
                double temperature_2 = DryBulbTemp_by_wet(wetBulbTemperature_Min, 100, pressure);
                double humidity_ratio_1 = Query.HumidityRatio(temperature_1, 0, pressure);
                double humidity_ratio_2 = Query.HumidityRatio(temperature_2, 100, pressure);
                double diagram_temperature_1 = Query.DiagramTemperature(temperature_1, humidity_ratio_1);
                double diagram_temperature_2 = Query.DiagramTemperature(temperature_2, humidity_ratio_2);
                Geometry.Planar.Point2D Point_1 = new Geometry.Planar.Point2D(temperature_1, humidity_ratio_1);
                Geometry.Planar.Point2D Point_2 = new Geometry.Planar.Point2D(temperature_2, humidity_ratio_2);
                series_WetBulbTemperature.Points.AddXY(Point_1.X, Point_1.Y);
                series_WetBulbTemperature.Points.AddXY(Point_2.X, Point_2.Y);
                DataPoint datapoint = series_WetBulbTemperature.Points[1];
                datapoint.Label = wetBulbTemperature_Min.ToString();
                datapoint.LabelForeColor = Color.LightSlateGray;
                if (wetBulbTemperature_Min == 15)
                {
                    Series series_name = MollierChart.Series.Add(String.Format("name {0}", wetBulbTemperature_Min));
                    series_name.IsVisibleInLegend = false;
                    series_name.Points.AddXY(Point_2.X + 4.5, Point_2.Y - 0.0018);
                    DataPoint name = series_name.Points[0];
                    series_name.ChartType = SeriesChartType.Spline;
                    series_name.Label = "Wet Bulb Temperature [ °C ]";
                    series_name.LabelForeColor = Color.LightSlateGray;
                    Geometry.Planar.Point2D point2D_1 = new Geometry.Planar.Point2D(Point_2.X + 2, Point_2.Y + 2);
                    Geometry.Planar.Vector2D vector2D = new Geometry.Planar.Vector2D(Point_2, point2D_1);
                    double angle = vector2D.Angle(SAM.Geometry.Planar.Vector2D.WorldX.GetNegated());
                    angle = 180 - (angle * 180 / System.Math.PI);
                    series_name.LabelAngle = 23;
                    series_name.SmartLabelStyle.Enabled = false;
                }
                wetBulbTemperature_Min += 5;
            }
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
                create_density_line_Mollier(density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line_Mollier(enthalpy_Min, enthalpy_Max, pressure);
            //CREATETING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_wet_bulb_temperature_line_Mollier(wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line_Mollier(specific_volume_Min, specific_volume_Max, pressure);
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
                create_density_line_Psychrometric(density_Min, density_Max, pressure);
            //CREATING ENTHALPY LINE
            if (enthalpy_line)
                create_enthalpy_line_Psychrometric(enthalpy_Min, enthalpy_Max, pressure);
            //CREATING WET BULB TEMPERATURE LINE
            if (wet_bulb_temperature_line)
                create_wet_bulb_temperature_line_Psychrometric(wetBulbTemperature_Min, wetBulbTemperature_Max, pressure);
            //CREATING SPECIFIC VOLUME LINE
            if (specific_volume_line)
                create_specific_volume_line_Psychrometric(specific_volume_Min, specific_volume_Max, pressure);
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
                    series.ToolTip = ToolTip(mollierProcess);
                }
            }
        }

        private string ToolTip(MollierPoint mollierPoint, ChartType chartType)
        {
            if(mollierPoint == null)
            {
                return null;
            }

            string mask = "HR = {0}[{1}]\nT = {1}[C]\nP = {2}";

            switch (chartType)
            {
                case ChartType.Psychrometric:
                    return String.Format(mask, mollierPoint.HumidityRatio, "[kg/kg]", mollierPoint.DryBulbTemperature, mollierPoint.Pressure);

                case ChartType.Mollier:
                    return String.Format(mask, mollierPoint.HumidityRatio * 1000, "[g/kg]", Query.DiagramTemperature(mollierPoint), mollierPoint.Pressure);
            }
            return null;
            
        }

        private string ToolTip(IMollierProcess mollierProcess)
        {
            if(mollierProcess is HeatingProcess)
            {
                return "Heating";
            }

            if(mollierProcess is CoolingProcess)
            {
                return "Cooling";
            }

            if(mollierProcess is MixingProcess)
            {
                return "Mixing";
            }

            if (mollierProcess is HeatRecoveryProcess)
            {
                return "Heat Recovery";
            }

            if(mollierProcess is HumidificationProcess)
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
        public double DryBulbTemp_by_wet(double DryBulbTemperature, double relative_humidity, double pressure)
        {
            double result = 99.9;
            while (Query.WetBulbTemperature(result, relative_humidity, pressure) > DryBulbTemperature)
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
                result -= 0.001;
            }
            return result;
        }
        public double humidity_ratio_for_100relativity(double dryBulbTemperature, Math.PolynomialEquation polynomialEquation, double pressure)
        {
            double result = 0;
            while (Query.RelativeHumidity(polynomialEquation.Evaluate(result * 1000), result, pressure) < 100)
            {
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
