using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class VisibilitySettings : IJSAMObject
    {
        Dictionary<string, List<IVisibilitySetting>> dictionary;

        public bool FromJObject(JObject jObject)
        {
            throw new NotImplementedException();
        }

        public JObject ToJObject()
        {
            throw new NotImplementedException();
        }

        public bool Add(IVisibilitySetting visibilitySetting)
        {
            return Add(String.Empty, visibilitySetting);
        }

        public bool Add(string templateName, IVisibilitySetting visibilitySetting)
        {
            if(visibilitySetting == null)
            {
                return false;
            }

            if(dictionary == null)
            {
                dictionary = new Dictionary<string, List<IVisibilitySetting>>();
            }

            if(!dictionary.TryGetValue(templateName, out List<IVisibilitySetting> visibilitySettingList))
            {
                visibilitySettingList = new List<IVisibilitySetting>();
                dictionary[templateName] = visibilitySettingList;

            }

            visibilitySettingList.Add(visibilitySetting);
            return true;
        }

        public IVisibilitySetting GetVisibilitySetting(ChartDataType chartDataType, ChartParameterType chartParameterType)
        {
            return GetVisibilitySetting(String.Empty, chartDataType, chartParameterType);
        }

        public IVisibilitySetting GetVisibilitySetting(string templateName, ChartDataType chartDataType, ChartParameterType chartParameterType)
        {
            if (!dictionary.TryGetValue(templateName, out List<IVisibilitySetting> visibilitySettings) || visibilitySettings == null)
            {
                return null;
            }

            //foreach(IVisibilitySetting x in visibilitySettings)
            //{
            //    if(!(x is BuiltInVisibilitySetting))
            //    {
            //        continue;
            //    }

            //    BuiltInVisibilitySetting BuiltInVisibilitySetting = (BuiltInVisibilitySetting)x;

            //    if(BuiltInVisibilitySetting.ChartDataType != chartDataType)
            //    {
            //        continue;
            //    }

            //    if (BuiltInVisibilitySetting.ChartParameterType != chartParameterType)
            //    {
            //        continue;
            //    }

            //    return BuiltInVisibilitySetting;
            //}

            return visibilitySettings.Find(x => x is BuiltInVisibilitySetting && ((BuiltInVisibilitySetting)x).ChartParameterType == chartParameterType && ((BuiltInVisibilitySetting)x).ChartDataType == chartDataType);
        }

        public System.Drawing.Color GetColor(string templateName, ChartDataType chartDataType, ChartParameterType chartParameterType)
        {
            IVisibilitySetting visibilitySetting = GetVisibilitySetting(templateName, chartDataType, chartParameterType);
            if(visibilitySetting == null)
            {
                return System.Drawing.Color.Empty;
            }

            return visibilitySetting.Color;
        }
    }
}
