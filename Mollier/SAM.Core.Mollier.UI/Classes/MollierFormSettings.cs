using Newtonsoft.Json.Linq;

namespace SAM.Core.Mollier.UI
{
    public class MollierFormSettings : IJSAMObject
    {
        public int Width { get; set; } = -1;
        public int Height { get; set; } = -1;

        public MollierFormSettings()
        {

        }

        public MollierFormSettings(MollierFormSettings mollierFormSettings)
        {
            if(mollierFormSettings != null)
            {
                Width = mollierFormSettings.Width;
                Height = mollierFormSettings.Height;
            }
        }

        public MollierFormSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool IsValid()
        {
            if (Width != 0 && Height != 0)
            {
                return false;
            }

            return true;
        }

        //loading from file
        public bool FromJObject(JObject jObject)
        {
            if (jObject.ContainsKey("Width"))
            {
                Width = jObject.Value<int>("Width");
            }

            if (jObject.ContainsKey("Height"))
            {
                Height = jObject.Value<int>("Height");
            }

            return true;
        }

        //saving to file
        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (!double.IsNaN(Width))
            {
                result.Add("Width", Width);
            }
            if (!double.IsNaN(Height))
            {
                result.Add("Height", Height);
            }

            return result;
        }
    }
}
