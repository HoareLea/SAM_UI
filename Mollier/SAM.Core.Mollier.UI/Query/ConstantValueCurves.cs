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

            MollierRange mollierRange = mollierControlSettings.MollierRange();
            if(mollierRange == null || !mollierRange.IsValid())
            {
                return null;
            }

            double step = double.NaN;

            switch (chartDataType)
            {
                case Mollier.ChartDataType.DryBulbTemperature:
                    step = 1;

                    return Mollier.Create.ConstantTemperatureCurves_DryBulbTemperature(mollierRange, step, pressure)?.ConvertAll(x => x as ConstantValueCurve);


                case Mollier.ChartDataType.Density:
                    if(!mollierControlSettings.Density_Line)
                    {
                        return null;
                    }

                    double denisty_Min = mollierControlSettings.Density_Min;
                    double denisty_Max = mollierControlSettings.Density_Max;
                    step = mollierControlSettings.Density_Interval;

                    Range<double> denistyRange = new Range<double>(denisty_Min, denisty_Max);

                    return Mollier.Create.ConstantValueCurves_Density(mollierRange, denistyRange, step, pressure);

                case Mollier.ChartDataType.RelativeHumidity:
                    step = 10;
                    
                    return Mollier.Create.ConstantValueCurves_RelativeHumidity(new MollierRange(Limit.DryBulbTemperature_Min, Limit.DryBulbTemperature_Max, mollierRange.HumidityRatio_Min, mollierRange.HumidityRatio_Max), new Range<double>(0, 100), step, pressure);

                case Mollier.ChartDataType.Enthalpy:
                    if (!mollierControlSettings.Enthalpy_Line)
                    {
                        return null;
                    }

                    double enthalpy_Min = mollierControlSettings.Enthalpy_Min;
                    double enthalpy_Max = mollierControlSettings.Enthalpy_Max;
                    step = mollierControlSettings.Enthalpy_Interval;

                    return Mollier.Create.ConstantEnthalpyCurves(mollierRange, new Range<double>(enthalpy_Min, enthalpy_Max), pressure, step, Phase.Gas)?.ConvertAll(x => x as ConstantValueCurve);

                case Mollier.ChartDataType.WetBulbTemperature:
                    if (!mollierControlSettings.WetBulbTemperature_Line)
                    {
                        return null;
                    }
                    step = mollierControlSettings.WetBulbTemperature_Interval;

                    return Mollier.Create.ConstantValueCurves_WetBulbTemperature(mollierRange, step, pressure);

                case Mollier.ChartDataType.SpecificVolume:
                    if (!mollierControlSettings.SpecificVolume_Line)
                    {
                        return null;
                    }

                    double specificVolume_Min = mollierControlSettings.SpecificVolume_Min;
                    double specificVolume_Max = mollierControlSettings.SpecificVolume_Max;
                    step = mollierControlSettings.SpecificVolume_Interval;

                    return Mollier.Create.ConstantValueCurves_SpecificVolume(mollierRange, new Range<double>(specificVolume_Min, specificVolume_Max), step, pressure);

                default:
                    return null;
            }
        }
    }
}
