// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Geometry.UI
{
    public class ParameterAppearanceSettings : ValueAppearanceSettings
    {
        public string ParameterName { get; set; }

        public ParameterAppearanceSettings(string parameterName)
            :base()
        {
            ParameterName = parameterName;
        }

        public ParameterAppearanceSettings(ParameterAppearanceSettings parameterAppearanceSettings)
            :base(parameterAppearanceSettings)
        {
            if(parameterAppearanceSettings != null)
            {
                ParameterName = parameterAppearanceSettings.ParameterName;
            }
        }

        public ParameterAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return false;
            }

            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ParameterName"))
            {
                ParameterName = jObject.Value<string>("ParameterName");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null)
            {
                return null;
            }

            if(ParameterName != null)
            {
                result.Add("ParameterName", ParameterName);
            }

            return result;
        }

        public override bool TryGetValue<T>(IJSAMObject jSAMObject, out T value)
        {
            value = default;

            object value_Temp = null;
            if(!Core.Query.TryGetValue(jSAMObject, ParameterName, out value_Temp))
            {
                return false;
            }

            if(value_Temp is T)
            {
                value = (T)value_Temp;
                return true;
            }

            if(!Core.Query.TryConvert(value_Temp, out T value_Converted))
            {
                return false;
            }

            value = value_Converted;
            return true;
        }
    }
}
