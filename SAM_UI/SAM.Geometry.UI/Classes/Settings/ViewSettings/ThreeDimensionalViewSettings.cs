// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ThreeDimensionalViewSettings : ViewSettings
    {
        private List<Plane> planes;

        public ThreeDimensionalViewSettings(Guid guid, string name, GuidAppearanceSettings guidAppearanceSettings, IEnumerable<Type> types, IEnumerable<ValueAppearanceSettings> valueAppearanceSettings)
            :base(guid, name, guidAppearanceSettings, types, valueAppearanceSettings)
        {

        }

        public ThreeDimensionalViewSettings(string name, GuidAppearanceSettings appearanceSettings, IEnumerable<Type> types, IEnumerable<ValueAppearanceSettings> valueAppearanceSettings)
            : base(Guid.NewGuid(), name, appearanceSettings, types, valueAppearanceSettings)
        {

        }

        public ThreeDimensionalViewSettings(JsonObject jObject)
            : base(jObject)
        {

        }

        public ThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
            :base(threeDimensionalViewSettings)
        {
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public ThreeDimensionalViewSettings(string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(name, threeDimensionalViewSettings)
        {
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public ThreeDimensionalViewSettings(Guid guid, string name, ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(guid, name, threeDimensionalViewSettings)
        {
            planes = threeDimensionalViewSettings?.Planes?.ConvertAll(x => new Plane(x));
        }

        public List<Plane> Planes
        {
            get
            {
                return planes;
            }

            set
            {
                planes = value;
            }
        }

        public override bool FromJsonObject(JsonObject jObject)
        {
            if(!base.FromJsonObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("Planes"))
            {
                JsonArray jArray = jObject["Planes"] as JsonArray;
                if(jArray != null)
                {
                    planes = new List<Plane>();
                    foreach(JsonNode jsonNode_Plane in jArray)
                    {
                        if (!(jsonNode_Plane is JsonObject jObject_Plane))
                        {
                            continue;
                        }

                        planes.Add(new Plane(jObject_Plane));
                    }
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

            if(planes != null)
            {
                JsonArray jArray = new JsonArray();
                foreach(Plane plane in planes)
                {
                    jArray.Add(plane.ToJsonObject());
                }
                jObject.Add("Planes", jArray);
            }

            return jObject;
        }
    }
}
