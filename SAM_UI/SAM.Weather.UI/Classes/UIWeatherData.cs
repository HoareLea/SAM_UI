// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Weather.UI
{
    class UIWeatherData : Core.UI.UIJSAMObject<WeatherData>
    {
        public UIWeatherData(string path)
            : base(path)
        {

        }

        public UIWeatherData(WeatherData weatherData)
            : base(weatherData)
        {

        }

        public UIWeatherData()
            : base()
        {

        }
    }
}
