using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.UI
{
    public class BoundaryTypeAppearanceSettings : AdjacencyClusterAppearanceSettings
    {
        public BoundaryTypeAppearanceSettings()
            :base()
        {

        }

        public BoundaryTypeAppearanceSettings(BoundaryTypeAppearanceSettings boundaryTypeAppearanceSettings)
            :base(boundaryTypeAppearanceSettings)
        {

        }

        public BoundaryTypeAppearanceSettings(JObject jObject)
            :base(jObject)
        {
        }

        public override bool TryGetValue<T>(IJSAMObject sAMObject, out T value)
        {
            value = default(T);
            if(AdjacencyCluster == null)
            {
                return false;
            }

            Panel panel = sAMObject as Panel;
            if(panel == null)
            {
                return false;
            }

            if(!(sAMObject is Panel))
            {
                return false;
            }

            BoundaryType boundaryType = AdjacencyCluster.BoundaryType(panel);
            if(typeof(T) == typeof(string))
            {
                value = (T)(object)Core.Query.Description(boundaryType);
                return true;
            }

            if(!Core.Query.TryConvert(boundaryType, out T value_Temp))
            {
                return false;
            }

            value = value_Temp;
            return true;
        }
    }
}
