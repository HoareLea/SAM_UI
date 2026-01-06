// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static string? Name(this Cases? cases)
        {
            if (cases == null)
            {
                return null;
            }

            if (cases.BaseType is not Type type)
            {
                return "Unknown case";
            }
            else if (type == typeof(WindowSizeCase))
            {
                return "Window Size Case";
            }
            else if (type == typeof(VentilationCase))
            {
                return "Ventilation Case";
            }
            else if (type == typeof(ApertureConstructionCase))
            {
                return "Aperture Construction Case";
            }
            else if (type == typeof(FinShadeCase))
            {
                return "Shade Case";
            }
            else if (type == typeof(ApertureCase))
            {
                return "Aperture Case";
            }
            else if (type == typeof(WeatherDataCase))
            {
                return "Weather Data Case";
            }
            else
            {
                return "Unknown case";
            }
        }
    }
}
