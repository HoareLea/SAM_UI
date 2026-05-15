// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
using System;
using System.Drawing;

namespace SAM.Core.UI
{
    public class    LegendItem : IJSAMObject
    {
        private Color color;
        private string text;
        private bool editable = true;

        public LegendItem(Color color, string text)
        {
            this.color = color;
            this.text = text;
        }

        public LegendItem(LegendItem legendItem)
        {
            if(legendItem != null)
            {
                color = legendItem.color;
                text = legendItem.text;
                editable = legendItem.editable;
            }
        }

        public LegendItem(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public Color Color
        {
            get
            {
                return color;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public bool Editable
        {
            get
            {
                return editable;
            }

            set
            {
                editable = value;
            }
        }

        public bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Color"))
            {
                SAMColor sAMColor = Core.Query.IJSAMObject(jObject["Color"] as JsonObject) as SAMColor;
                if(sAMColor == null)
                {
                    color = Color.Empty;
                }
                else
                {
                    color = sAMColor.ToColor();
                }
            }

            if (jObject.ContainsKey("Text"))
            {
                text = jObject["Text"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("Editable"))
            {
                editable = jObject["Editable"]?.GetValue<bool>() ?? default(bool);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (color != Color.Empty)
            {
                jObject.Add("Color", new SAMColor(color).ToJsonObject());
            }

            if(text != null)
            {
                jObject.Add("Text", text);
            }

            jObject.Add("Editable", editable);

            return jObject;
        }

        public static bool operator ==(LegendItem legendItem_1, LegendItem legendItem_2)
        {
            if (ReferenceEquals(legendItem_1, null) && ReferenceEquals(legendItem_2, null))
                return true;

            if (ReferenceEquals(legendItem_1, null))
                return false;

            if (ReferenceEquals(legendItem_2, null))
                return false;

            return legendItem_1.color == legendItem_2.color && legendItem_1.text == legendItem_2.text && legendItem_1.editable == legendItem_2.editable;
        }

        public static bool operator !=(LegendItem legendItem_1, LegendItem legendItem_2)
        {
            if (ReferenceEquals(legendItem_1, null) && ReferenceEquals(legendItem_2, null))
                return false;

            if (ReferenceEquals(legendItem_1, null))
                return true;

            if (ReferenceEquals(legendItem_2, null))
                return true;

            return legendItem_1.color != legendItem_2.color || legendItem_1.text != legendItem_2.text || legendItem_1.editable != legendItem_2.editable;
        }

        public override int GetHashCode()
        {
            string text = this.text == null ? string.Empty : this.text;

            return (new Tuple<string, Color, bool>(text, color, editable)).GetHashCode();
        }
    }
}
