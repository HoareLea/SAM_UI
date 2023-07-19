using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<ConstantValueCurve> ConstantValueCurves(this ChartDataType chartDataType, MollierControlSettings mollierControlSettings)
        {
            if(mollierControlSettings == null)
            {
                return null;
            }

            double pressure = mollierControlSettings.Pressure;
            if(double.IsNaN(pressure))
            {
                return null;
            }

            double dryBulbTemperature_Min = double.NaN;
            double dryBulbTemperature_Max = double.NaN;
            double step = double.NaN;

            switch (chartDataType)
            {
                case Mollier.ChartDataType.DryBulbTemperature:
                    dryBulbTemperature_Min = mollierControlSettings.Temperature_Min;
                    dryBulbTemperature_Max = mollierControlSettings.Temperature_Max;
                    step = 1;

                    return Mollier.Create.ConstantValueCurves_DryBulbTemperature(new Range<double>(dryBulbTemperature_Min, dryBulbTemperature_Max), step, pressure);


                case Mollier.ChartDataType.DiagramTemperature:
                    dryBulbTemperature_Min = mollierControlSettings.Temperature_Min;
                    dryBulbTemperature_Max = mollierControlSettings.Temperature_Max;
                    step = 1;

                    return Mollier.Create.ConstantValueCurves_DiagramTemperature(new Range<double>(dryBulbTemperature_Min, dryBulbTemperature_Max), step, pressure);


                case Mollier.ChartDataType.Density:
                    if(!mollierControlSettings.Density_line)
                    {
                        return null;
                    }

                    double denisty_Min = mollierControlSettings.Density_Min;
                    double denisty_Max = mollierControlSettings.Density_Max;
                    step = mollierControlSettings.Density_Interval;
                    step = 0.01; //remove

                    Range<double> denistyRange = new Range<double>(denisty_Min, denisty_Max);

                    return Mollier.Create.ConstantValueCurves_Density(denistyRange, step, pressure);

                case Mollier.ChartDataType.RelativeHumidity:
                    dryBulbTemperature_Min = mollierControlSettings.Temperature_Min;
                    dryBulbTemperature_Max = mollierControlSettings.Temperature_Max;
                    step = 10;

                    dryBulbTemperature_Min = System.Math.Max(Default.DryBulbTemperature_Min, dryBulbTemperature_Min - 1);
                    dryBulbTemperature_Max = System.Math.Min(Default.DryBulbTemperature_Max, dryBulbTemperature_Max + 1);

                    return Mollier.Create.ConstantValueCurves_RelativeHumidity(new Range<double>(0, 100), step, pressure, new Range<double>(dryBulbTemperature_Min, dryBulbTemperature_Max));

                case Mollier.ChartDataType.Enthalpy:
                    if (!mollierControlSettings.Enthalpy_line)
                    {
                        return null;
                    }

                    double enthalpy_Min = mollierControlSettings.Enthalpy_Min;
                    double enthalpy_Max = mollierControlSettings.Enthalpy_Max;
                    step = mollierControlSettings.Enthalpy_Interval;

                    return Mollier.Create.ConstantValueCurves_Enthalpy(new Range<double>(enthalpy_Min, enthalpy_Max), step, pressure);

                case Mollier.ChartDataType.WetBulbTemperature:
                    if (!mollierControlSettings.WetBulbTemperature_line)
                    {
                        return null;
                    }
                    dryBulbTemperature_Min = mollierControlSettings.Temperature_Min;
                    dryBulbTemperature_Max = mollierControlSettings.Temperature_Max;
                    step = 1; //5

                    return Mollier.Create.ConstantValueCurves_WetBulbTemperature(new Range<double>(dryBulbTemperature_Min, dryBulbTemperature_Max), step, pressure);

                case Mollier.ChartDataType.SpecificVolume:
                    if (!mollierControlSettings.SpecificVolume_line)
                    {
                        return null;
                    }

                    double specificVolume_Min = mollierControlSettings.SpecificVolume_Min;
                    double specificVolume_Max = mollierControlSettings.SpecificVolume_Max;
                    step = mollierControlSettings.SpecificVolume_Interval;
                    step = 0.01;//custom interval

                    return Mollier.Create.ConstantValueCurves_SpecificVolume(new Range<double>(specificVolume_Min, specificVolume_Max), step, pressure);




                default:
                    return null;
            }
        }
    }
}
