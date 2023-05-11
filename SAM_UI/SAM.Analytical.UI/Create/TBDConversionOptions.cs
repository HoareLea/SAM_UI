﻿using SAM.Analytical.UI.WPF;
using SAM.Core;
using SAM.Weather;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        public static TBDConversionOptions TBDConversionOptions(this UIAnalyticalModel uIAnalyticalModel)
        {
            if(uIAnalyticalModel == null)
            {
                return null;
            }

            TBDConversionOptions result = new TBDConversionOptions();


            if (!string.IsNullOrWhiteSpace(uIAnalyticalModel.Path))
            {
                result.OutputDirectory = System.IO.Path.GetDirectoryName(uIAnalyticalModel.Path);
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            if(analyticalModel != null)
            {
                result.ProjectName = analyticalModel.Name;

                if (analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData))
                {
                    result.WeatherData = weatherData;
                }

                result.ZoneCategories = analyticalModel.GetZoneCategories()?.ToList();
            }

            return result;
        }
    }


}
