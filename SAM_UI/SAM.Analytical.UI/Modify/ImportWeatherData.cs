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

            if (!Query.TryGetWeatherData(out WeatherData weatherData, owner) || weatherData == null)
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

            bool updateLocation = false;
            if(analyticalModel.Location == null)
            {
                updateLocation = true;
            }

            if(!updateLocation)
            {
                if (!Core.Query.AlmostEqual(weatherData.Latitude, analyticalModel.Location.Latitude, 0.1))
                {
                    updateLocation = true;
                }

                if (!Core.Query.AlmostEqual(weatherData.Longitude, analyticalModel.Location.Longitude, 0.1))
                {
                    updateLocation = true;
                }
            }

            if(updateLocation)
            {
                DialogResult dialogResult = MessageBox.Show(owner, "Do you want to update AnalyticalModel location to match WeatherData", "Update Location", MessageBoxButtons.YesNoCancel);
                switch(dialogResult)
                {
                    case DialogResult.Yes:
                        analyticalModel = new AnalyticalModel(analyticalModel, weatherData.Location);
                        break;

                    case DialogResult.No:
                        break;

                    case DialogResult.Cancel:
                        return;
                }
            }

            uIAnalyticalModel.JSAMObject = analyticalModel;
        }
    }
}