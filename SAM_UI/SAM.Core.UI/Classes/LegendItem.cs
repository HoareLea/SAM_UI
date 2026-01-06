// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using System;
using System.Drawing;

namespace SAM.Core.UI
{
    public class LegendItem : IJSAMObject
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

        public LegendItem(JObject jObject)
        {
            FromJObject(jObject);
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

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Color"))
            {
                SAMColor sAMColor = Core.Query.IJSAMObject(jObject.Value<JObject>("Color")) as SAMColor;
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
                text = jObject.Value<string>("Text");
            }

            if (jObject.ContainsKey("Editable"))
            {
                editable = jObject.Value<bool>("Editable");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (color != Color.Empty)
            {
                jObject.Add("Color", new SAMColor(color).ToJObject());
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
