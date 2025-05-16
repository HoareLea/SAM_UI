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
