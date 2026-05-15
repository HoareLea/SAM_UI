// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Core;

namespace SAM.Geometry.UI
{
    public abstract class ValueAppearanceSettings : Core.UI.IAppearanceSettings
    {
        public ValueAppearanceSettings() 
        {

        }
        
        public ValueAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
        {
            if(valueAppearanceSettings != null)
            {

            }
        }

        public ValueAppearanceSettings(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public virtual bool IsValid(IJSAMObject jSAMObject)
        {
            if(jSAMObject == null)
            {
                return false;
            }

            return TryGetValue(jSAMObject, out object value);
        }

        public abstract bool TryGetValue<T>(IJSAMObject sAMObject, out T value);

        public virtual bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            //if (jObject.ContainsKey("ParameterName"))
            //{
            //    ParameterName = jObject["ParameterName"]?.GetValue<string>() ?? null;
            //}

            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            //if(ParameterName != null)
            //{
            //    jObject.Add("ParameterName", ParameterName);
            //}

            return jObject;
        }
    }
}
