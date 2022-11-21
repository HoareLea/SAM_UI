using Newtonsoft.Json.Linq;

namespace SAM.Geometry.UI
{
    public abstract class ThreeDimensionalViewSettings : ViewSettings
    {
        public ThreeDimensionalViewSettings(int id, AppearanceSettings appearanceSettings)
            :base(id, appearanceSettings)
        {

        }

        public ThreeDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public ThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
            :base(threeDimensionalViewSettings)
        {

        }

        public virtual bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }


            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            return jObject;
        }
    }
}
