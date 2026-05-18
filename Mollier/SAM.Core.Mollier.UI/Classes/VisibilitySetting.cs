// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public abstract class VisibilitySetting : IVisibilitySetting
    {
        public Color Color { get; set; } = Color.Empty;

        public bool Visible { get; set; } = true;

        public VisibilitySetting(Color color)
        {
            Color = color;
        }

        public VisibilitySetting(VisibilitySetting visibilitySetting)
        {
            Color = visibilitySetting.Color;
            Visible = visibilitySetting.Visible;
        }
        public VisibilitySetting(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public virtual bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Color"))
            {
                JsonObject jObject_Color = jObject["Color"] as JsonObject;
                if (jObject_Color != null)
                {
                    SAMColor sAMColor = new SAMColor(jObject_Color);
                    if (sAMColor != null)
                    {
                        Color = sAMColor.ToColor();
                    }
                }
            }

            if (jObject.ContainsKey("Visible"))
            {
                Visible = jObject["Visible"]?.GetValue<bool>() ?? default(bool);
            }

            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Color != Color.Empty)
            {
                result.Add("Color", (new SAMColor(Color)).ToJsonObject());
            }

            result.Add("Visible", Visible);

            return result;
        }
    }
}
