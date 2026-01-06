// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculates constant value curve depends on type
        /// </summary>
        /// <param name="chartDataType">Chart data type</param>
        /// <param name="mollierControlSettings">mollier control settings</param>
        /// <returns>Constant value curve</returns>
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

            VisibilitySettings visibilitySettings = mollierControlSettings.VisibilitySettings;
            string templateName = mollierControlSettings.DefaultTemplateName;

            double step = double.NaN;

            switch (chartDataType)
            {
                case ChartDataType.DryBulbTemperature:
                    step = 1;

                    return Mollier.Create.ConstantTemperatureCurves_DryBulbTemperature(mollierRange, step, pressure, visibilitySettings, templateName)?.ConvertAll(x => x as ConstantValueCurve);


                case ChartDataType.Density:
                    if(!mollierControlSettings.Density_Line)
                    {
                        return null;
                    }

                    double denisty_Min = mollierControlSettings.Density_Min;
                    double denisty_Max = mollierControlSettings.Density_Max;
                    step = mollierControlSettings.Density_Interval;

                    Range<double> denistyRange = new Range<double>(denisty_Min, denisty_Max);

                    return Mollier.Create.ConstantValueCurves_Density(mollierRange, denistyRange, step, pressure, visibilitySettings, templateName);

                case ChartDataType.RelativeHumidity:
                    step = 10;
                    
                    return Mollier.Create.ConstantValueCurves_RelativeHumidity(new MollierRange(Limit.DryBulbTemperature_Min, Limit.DryBulbTemperature_Max, mollierRange.HumidityRatio_Min, mollierRange.HumidityRatio_Max), new Range<double>(0, 100), step, pressure, visibilitySettings, templateName);

                case ChartDataType.Enthalpy:
                    if (!mollierControlSettings.Enthalpy_Line)
                    {
                        return null;
                    }

                    double enthalpy_Min = mollierControlSettings.Enthalpy_Min;
                    double enthalpy_Max = mollierControlSettings.Enthalpy_Max;
                    step = mollierControlSettings.Enthalpy_Interval;

                    return Mollier.Create.ConstantEnthalpyCurves(mollierRange, new Range<double>(enthalpy_Min, enthalpy_Max), pressure, step, new Phase[] { Phase.Gas }, visibilitySettings, templateName)?.ConvertAll(x => x as ConstantValueCurve);

                case ChartDataType.WetBulbTemperature:
                    if (!mollierControlSettings.WetBulbTemperature_Line)
                    {
                        return null;
                    }
                    step = mollierControlSettings.WetBulbTemperature_Interval;

                    return Mollier.Create.ConstantValueCurves_WetBulbTemperature(mollierRange, step, pressure, visibilitySettings, templateName);

                case ChartDataType.SpecificVolume:
                    if (!mollierControlSettings.SpecificVolume_Line)
                    {
                        return null;
                    }

                    double specificVolume_Min = mollierControlSettings.SpecificVolume_Min;
                    double specificVolume_Max = mollierControlSettings.SpecificVolume_Max;
                    step = mollierControlSettings.SpecificVolume_Interval;

                    return Mollier.Create.ConstantValueCurves_SpecificVolume(mollierRange, new Range<double>(specificVolume_Min, specificVolume_Max), step, pressure, visibilitySettings, templateName);

                default:
                    return null;
            }
        }
    }
}
