// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using System;
using System.Text.Json;

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

        public LegendItemData(JsonObject jObject)
        {
            FromJsonObject(jObject);
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

        public bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("SAMObject"))
            {
                sAMObject = Core.Query.IJSAMObject(jObject["SAMObject"] as JsonObject) as SAMObject;
            }

            JsonNode jsonNode = jObject["Value"];
            if(jsonNode != null)
            {
                switch (jsonNode.GetValueKind())
                {
                    case JsonValueKind.Number:
                        if (jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<int>(out int intValue))
                        {
                            value = intValue;
                        }
                        else
                        {
                            value = jsonNode.GetValue<double>();
                        }
                        break;

                    case JsonValueKind.String:
                        value = jsonNode.GetValue<string>();
                        break;

                    case JsonValueKind.Null:
                        value = null;
                        break;

                    case JsonValueKind.False:
                    case JsonValueKind.True:
                        value = jsonNode.GetValue<bool>();
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            if (jObject.ContainsKey("Text"))
            {
                text = jObject["Text"]?.GetValue<string>() ?? null;
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (sAMObject != null)
            {
                jObject.Add("SAMObject", sAMObject.ToJsonObject());
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
