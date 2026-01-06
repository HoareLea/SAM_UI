// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public class LegendItemData : IJSAMObject
    {
        private SAMObject sAMObject;
        private object value;
        private string text;

        public LegendItemData(SAMObject sAMObject, object value, string text)
        {
            this.sAMObject = sAMObject;
            this.value = value;
            this.text = text;
        }

        public LegendItemData(LegendItemData legendItemData)
        {
            if(legendItemData != null)
            {
                sAMObject = legendItemData.sAMObject;
                value = legendItemData.value;
                text = legendItemData.text;
            }
        }

        public LegendItemData(JObject jObject)
        {
            FromJObject(jObject);
        }

        public SAMObject SAMObject
        {
            get
            {
                return sAMObject;
            }
        }

        public Guid Guid
        {
            get
            {
                return sAMObject == null ? Guid.Empty : sAMObject.Guid;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public object Value
        {
            get
            {
                return value;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("SAMObject"))
            {
                sAMObject = Core.Query.IJSAMObject(jObject.Value<JObject>("SAMObject")) as SAMObject;
            }

            JToken jToken = jObject.GetValue("Value");
            if(jToken != null)
            {
                switch (jToken.Type)
                {
                    case JTokenType.Integer:
                        value = jToken.Value<int>();
                        break;

                    case JTokenType.Float:
                        value = jToken.Value<double>();
                        break;

                    case JTokenType.String:
                        value = jToken.Value<string>();
                        break;

                    case JTokenType.Null:
                        value = null;
                        break;

                    case JTokenType.Boolean:
                        value = jToken.Value<bool>();
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            if (jObject.ContainsKey("Text"))
            {
                text = jObject.Value<string>("Text");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (sAMObject != null)
            {
                jObject.Add("SAMObject", sAMObject.ToJObject());
            }

            if(text != null)
            {
                jObject.Add("Text", text);
            }

            if(value != null)
            {
                jObject.Add("Value", value as dynamic);
            }

            return jObject;
        }
    }
}
