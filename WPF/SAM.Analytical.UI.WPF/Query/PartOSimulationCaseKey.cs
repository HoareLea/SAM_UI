// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Weather;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        private static readonly ConditionalWeakTable<WeatherData, string> weatherDigests = new();

        /// <summary>
        /// What a Part O simulation of the same model depends on besides the model: the weather (by content, not by name
        /// or instance) and the solar calculation method. Evidence carries it, so a result simulated under another case
        /// is never shown as current. The output folder is not part of it - it moves no result.
        /// </summary>
        /// <returns>The key, or null where no case is stated.</returns>
        internal static string? PartOSimulationCaseKey(PartOSimulationCase? partOSimulationCase)
        {
            return partOSimulationCase is null ? null : PartOSimulationCaseKey(partOSimulationCase.WeatherData, partOSimulationCase.SolarCalculationMethod);
        }

        /// <summary>The same key for the context a run actually used - what evidence records.</summary>
        internal static string? PartOSimulationCaseKey(PartOSimulationContext? partOSimulationContext)
        {
            return partOSimulationContext is null ? null : PartOSimulationCaseKey(partOSimulationContext.WeatherData, partOSimulationContext.SolarCalculationMethod);
        }

        private static string PartOSimulationCaseKey(WeatherData? weatherData, SolarCalculationMethod solarCalculationMethod)
        {
            string weather = weatherData is not null
                ? weatherDigests.GetValue(weatherData, x => Digest(x.ToJsonObject()?.ToJsonString() ?? string.Empty))
                : "model weather";

            return string.Format("{0}|{1}", weather, solarCalculationMethod);
        }

        private static string Digest(string text)
        {
            using SHA256 sHA256 = SHA256.Create();
            return System.Convert.ToHexString(sHA256.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
