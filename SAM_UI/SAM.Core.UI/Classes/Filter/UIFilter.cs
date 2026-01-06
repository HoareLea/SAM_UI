// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using System;

namespace SAM.Core.UI
{
    public abstract class UIFilter<T> :IUIFilter where T: IFilter
    {
        private string name;
        private Type type;
        private T filter;

        public UIFilter(JObject jObject)
        {
            FromJObject(jObject);
        }
        
        public UIFilter(string name, Type type, T filter)
        {
            this.name = name;
            this.type = type;
            this.filter = filter;
        }

        public UIFilter(UIFilter<T> uIFilter)
        {
            if(uIFilter != null)
            {
                name = uIFilter.name;
                type = uIFilter.type;
                if(uIFilter.filter != null)
                {
                    filter = uIFilter.filter.Clone();
                }
            }
        }

        public string Name 
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public Type Type
        {
            get
            {
                return type;
            }
        }

        public T Filter
        {
            get
            {
                return filter;
            }
        }

        public bool Inverted
        {
            get
            {
                return filter == null ? false : filter.Inverted;
            }

            set
            {
                if(filter == null)
                {
                    return;
                }

                filter.Inverted = value;
            }
        }

        public bool IsValid(IJSAMObject jSAMObject)
        {
            if(filter == null)
            {
                return false;
            }

            return filter.IsValid(jSAMObject);
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Name"))
            {
                name = jObject.Value<string>("Name");
            }

            if (jObject.ContainsKey("Type"))
            {
                type = Core.Query.Type(jObject.Value<string>("Type"));
            }

            if (jObject.ContainsKey("Filter"))
            {
                filter = (T)Core.Query.IJSAMObject(jObject.Value<JObject>("Filter"));
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (name != null)
            {
                jObject.Add("Name", name);
            }

            if(type != null)
            {
                jObject.Add("Type", Core.Query.FullTypeName(type));
            }

            if(filter != null)
            {
                jObject.Add("Filter", filter.ToJObject());
            }


            return jObject;
        }
    }
}
