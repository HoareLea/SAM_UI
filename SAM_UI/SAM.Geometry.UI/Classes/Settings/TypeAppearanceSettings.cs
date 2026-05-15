// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Core;
using SAM.Core.UI;

namespace SAM.Geometry.UI
{
    public abstract class TypeAppearanceSettings<T> : ValueAppearanceSettings, ITypeAppearanceSettings where T : IJSAMObject
    {
        private ValueAppearanceSettings valueAppearanceSettings;
        
        public TypeAppearanceSettings(string parameterName)
            :base()
        {
            valueAppearanceSettings = new ParameterAppearanceSettings(parameterName);
        }

        public TypeAppearanceSettings(JsonObject jObject)
            :base(jObject)
        {

        }

        public TypeAppearanceSettings(TypeAppearanceSettings<T> typeAppearanceSettings)
            : base(typeAppearanceSettings)
        {
            if(typeAppearanceSettings != null)
            {
                valueAppearanceSettings = typeAppearanceSettings.valueAppearanceSettings == null ? null : Core.Query.Clone(typeAppearanceSettings.valueAppearanceSettings);
            }
        }

        public TypeAppearanceSettings(ValueAppearanceSettings valueAppearanceSettings)
            :base()
        {
            this.valueAppearanceSettings = valueAppearanceSettings == null ? null : Core.Query.Clone(valueAppearanceSettings);
        }

        public Z GetValueAppearanceSettings<Z>() where Z: ValueAppearanceSettings
        {
            if(valueAppearanceSettings == null)
            {
                return default;
            }

            IAppearanceSettings result = Core.Query.Clone(valueAppearanceSettings);
            if(!(result is Z))
            {
                return default;
            }

            return (Z)result;
        }

        public override bool TryGetValue<Z>(IJSAMObject jSAMObject, out Z value)
        {
            value = default;

            if(valueAppearanceSettings == null)
            {
                return false;
            }

            return valueAppearanceSettings.TryGetValue(jSAMObject, out value);
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if(jSAMObject == null || !(jSAMObject is T))
            {
                return false;
            }

            return base.IsValid(jSAMObject);
        }

        public override bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ValueAppearanceSettings"))
            {
                valueAppearanceSettings = Core.Query.IJSAMObject(jObject["ValueAppearanceSettings"] as JsonObject) as ValueAppearanceSettings;
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(valueAppearanceSettings != null)
            {
                jObject.Add("ValueAppearanceSettings", valueAppearanceSettings.ToJsonObject());
            }

            return jObject;
        }
    }
}
