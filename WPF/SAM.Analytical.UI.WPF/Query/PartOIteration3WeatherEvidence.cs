// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Weather;
using System;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>Copies QA facts from the provenanced result model, not a mutable case hint.</summary>
        internal static void PartOIteration3WeatherEvidence(PartOIteration3Record record, WeatherData weather)
        {
            if (record is null || weather is null) return;

            record.WeatherName = weather.Name;
            record.WeatherDescription = weather.Description;
            record.WeatherLatitude = double.IsNaN(weather.Latitude) ? null : weather.Latitude;
            record.WeatherLongitude = double.IsNaN(weather.Longitude) ? null : weather.Longitude;
            record.WeatherElevation = double.IsNaN(weather.Elevtion) ? null : weather.Elevtion;

            var days = weather.WeatherYears?.FirstOrDefault()?.WeatherDays;
            if (days is null) return;

            double peak = double.NegativeInfinity;
            int count = 0;
            for (int dayIndex = 0; dayIndex < days.Count; dayIndex++)
            {
                var values = days[dayIndex]?.GetValues(WeatherDataType.DryBulbTemperature);
                if (values is null) continue;
                for (int hourIndex = 0; hourIndex < values.Count; hourIndex++)
                {
                    double value = values[hourIndex];
                    if (!double.IsNaN(value) && !double.IsInfinity(value) && value > peak)
                    {
                        peak = value;
                        record.WeatherPeakHour = dayIndex * 24 + hourIndex;
                    }
                    if (!double.IsNaN(value) && !double.IsInfinity(value)) count++;
                }
            }
            if (count == 365 * 24 && !double.IsNegativeInfinity(peak)) record.WeatherPeakDryBulb_C = peak;
            else record.WeatherPeakHour = null;
        }
    }
}
