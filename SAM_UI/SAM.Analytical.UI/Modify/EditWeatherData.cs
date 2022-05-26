using SAM.Weather;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void EditWeatherData(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData);

            if(weatherData == null)
            {
                Query.TryGetWeatherData(out weatherData, owner);
            }

            if(weatherData == null)
            {
                return;
            }

            using (Weather.Windows.Forms.WeatherDataForm weatherDataForm = new Weather.Windows.Forms.WeatherDataForm(weatherData, Core.Query.Enums(typeof(WeatherData))))
            {
                if(weatherDataForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
                
                weatherData = weatherDataForm.WeatherData;
            }

            if(weatherData == null)
            {
                return;
            }

            analyticalModel = new AnalyticalModel(analyticalModel);
            analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, weatherData);

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}