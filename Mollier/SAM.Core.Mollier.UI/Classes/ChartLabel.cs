// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Drawing;
using System.Text.Json.Nodes;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI.Classes
{
    public class ChartLabel : IJSAMObject
    {
        public object Tag { get; set; }

        public Point2D Position { get; set; }
        public string Text { get; set; }
        public double Angle { get; set; }
        public Color Color { get; set; }
        
        public ChartLabel()
        {

        }

        public ChartLabel(ChartLabel chartLabel)
        {
            Position = chartLabel.Position;
            Text = chartLabel.Text;
            Angle = chartLabel.Angle;
            Color = chartLabel.Color;
        }
        public ChartLabel(Point2D position, string text, double angle, Color color)
        {
            Position = position;
            Text = text;
            Angle = angle;
            Color = color;
        }
        public bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Position"))
            {
                JsonObject jObject_Position = jObject["Position"] as JsonObject;
                if (jObject_Position != null)
                {
                    Position = new Point2D(jObject_Position);
                }
            }

            if (jObject.ContainsKey("Text"))
            {
                Text = jObject["Text"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("Angle"))
            {
                Angle = jObject["Angle"]?.GetValue<double>() ?? default(double);
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

            return true;
        }
        public JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (Position != null)
            {
                result.Add("Position", new Point2D(Position).ToJsonObject());
            }

            if (Text != null)
            {
                result.Add("Text", Text);
            }

            if (double.IsNaN(Angle))
            {
                result.Add("Angle", Angle);
            }

            if (Color != Color.Empty)
            {
                result.Add("Color", new SAMColor(Color).ToJsonObject());
            }


            return result;
        }
    }
}