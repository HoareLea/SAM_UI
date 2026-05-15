// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using System;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public class BuiltInVisibilitySetting : VisibilitySetting, IChartVisibilitySetting
    {
        public ChartParameterType ChartParameterType { get; set; }
        public ChartDataType ChartDataType { get; set; }


        public BuiltInVisibilitySetting(ChartParameterType chartParameterType, ChartDataType chartDataType, Color color)
            : base(color)
        {
            ChartParameterType = chartParameterType;
            ChartDataType = chartDataType;
        }

        public BuiltInVisibilitySetting(ChartParameterType chartParameterType,  Color color)
            : base(color)
        {
            ChartParameterType = chartParameterType;
            ChartDataType = ChartDataType.Undefined;
        }

        public BuiltInVisibilitySetting(JsonObject jObject)
            : base(jObject)
        {

        }

        public BuiltInVisibilitySetting(BuiltInVisibilitySetting builtInVisibilitySetting)
            : base(builtInVisibilitySetting)
        {
            ChartParameterType = builtInVisibilitySetting.ChartParameterType;
            ChartDataType = builtInVisibilitySetting.ChartDataType;
        }

        public override bool FromJsonObject(JsonObject jObject)
        {
            if(!base.FromJsonObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("ChartDataType"))
            {
                if (Enum.TryParse(jObject["ChartDataType"]?.GetValue<string>() ?? null, out ChartDataType chartDataType))
                {
                    ChartDataType = chartDataType;
                }
            }

            if (jObject.ContainsKey("ChartParameterType"))
            {
                if (Enum.TryParse(jObject["ChartParameterType"]?.GetValue<string>() ?? null, out ChartParameterType chartParameterType))
                {
                    ChartParameterType = chartParameterType;
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jObject = base.ToJsonObject();
            if(jObject == null)
            {
                return null;
            }

            jObject.Add("ChartParameterType", ChartParameterType.ToString());
            jObject.Add("ChartDataType", ChartDataType.ToString());

            return jObject;
        }
    }
}
