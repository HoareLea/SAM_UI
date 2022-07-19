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
            if(jObject == null)
            {
                return false;
            }


            if (jObject.ContainsKey("VisibilitySettings"))
            {
                dictionary = new Dictionary<string, List<IVisibilitySetting>>();
                JArray jArray = jObject.Value<JArray>("Templates");
                foreach(JObject jObject_Temp in jArray)
                {
                    if(!jObject_Temp.ContainsKey("TemplateName") || !jObject_Temp.ContainsKey("VisibilitySettings"))
                    {
                        continue;
                    }

                    string templateName = jObject_Temp.Value<string>("TemplateName");
                    JArray jArray_VisibilitySettings = jObject_Temp.Value<JArray>("VisibilitySettings");

                    foreach(JObject jObject_VisibilitySetting in jArray_VisibilitySettings)
                    {
                        if (!dictionary.TryGetValue(templateName, out List<IVisibilitySetting> visibilitySettings))
                        {
                            visibilitySettings = new List<IVisibilitySetting>();
                            dictionary[templateName] = visibilitySettings;
                        }

                        visibilitySettings.Add(new JSAMObjectWrapper(jObject_VisibilitySetting).ToIJSAMObject() as IVisibilitySetting);
                    }
                }
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject result = new JObject();

            if(dictionary != null)
            {
                JArray jArray = new JArray();
                foreach(KeyValuePair<string, List<IVisibilitySetting>> keyValuePair in dictionary)
                {
                    JObject jObject_Template = new JObject();


                    string templateName = keyValuePair.Key;
                    jObject_Template.Add("TemplateName", templateName);

                    JArray jArray_VisibilitySettings = new JArray();
                    foreach(IVisibilitySetting visibilitySetting in keyValuePair.Value)
                    {
                        jArray_VisibilitySettings.Add(visibilitySetting.ToJObject());
                    }

                    jObject_Template.Add("VisibilitySettings", jArray_VisibilitySettings);

                }
                result.Add("Templates", jArray);
            }

            return result;
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
