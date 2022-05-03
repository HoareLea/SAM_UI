using SAM.Weather;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static void ImportWeatherData(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            if(uIAnalyticalModel == null)
            {
                return;
            }

            if (!Windows.Query.TryGetWeatherData(out WeatherData weatherData, owner) || weatherData == null)
            {
                return;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, weatherData);
            uIAnalyticalModel.JSAMObject = analyticalModel;

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}