using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public class Legend : IJSAMObject
    {
        private string name;
        private List<LegendItem> legendItems;

        public Legend(string name, IEnumerable<LegendItem> legendItems)
        {
            this.name = name;
            if(legendItems != null)
            {
                foreach(LegendItem legendItem in legendItems)
                {
                    Add(legendItem);
                }
            }
        }
        
        public Legend(string name)
        {
            this.name = name;
        }

        public Legend(Legend legend)
        {
            if(legend != null)
            {
                name = legend.name;
                if(legend.legendItems != null)
                {
                    legendItems = new List<LegendItem>();
                    foreach(LegendItem legendItem in legend.legendItems)
                    {
                        if(legendItem == null)
                        {
                            continue;
                        }

                        legendItems.Add(new LegendItem(legendItem));
                    }
                }
            }
        }

        public Legend(JObject jObject)
        {
            FromJObject(jObject);
        }

        public int Add(LegendItem legendItem)
        {
            if(legendItem == null)
            {
                return -1;
            }

            if(legendItems == null)
            {
                legendItems = new List<LegendItem>();
                legendItems.Add(legendItem);
                return 0;
            }

            int index = legendItems.FindIndex(x => x == legendItem);
            if(index != -1)
            {
                return index;
            }

            legendItems.Add(legendItem);
            return legendItems.Count - 1;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public List<LegendItem> LegendItems
        {
            get
            {
                return legendItems?.ConvertAll(x => new LegendItem(x));
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("LegendItems"))
            {
                JArray jArray = jObject.Value<JArray>("LegendItems");
                if(jArray != null)
                {
                    legendItems = new List<LegendItem>();
                    foreach(JObject jObject_LegendItem in jArray)
                    {
                        if(jObject_LegendItem == null)
                        {
                            continue;
                        }

                        legendItems.Add(new LegendItem(jObject_LegendItem));
                    }
                }
            }

            if (jObject.ContainsKey("Name"))
            {
                name = jObject.Value<string>("Name");
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

            if (legendItems != null)
            {
                JArray jArray = new JArray();
                foreach(LegendItem legendItem in legendItems)
                {
                    JObject jObject_LegendItem = legendItem?.ToJObject();
                    if(jObject_LegendItem == null)
                    {
                        continue;
                    }

                    jArray.Add(jObject_LegendItem);
                }

                jObject.Add("LegendItems", jArray);
            }

            return jObject;
        }
    }
}
