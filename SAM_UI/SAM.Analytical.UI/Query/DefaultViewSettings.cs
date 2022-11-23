using SAM.Core;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static ViewSettings DefaultViewSettings(int id)
        {
            ViewSettings result = new ThreeDimensionalViewSettings(id, null, new System.Type[] { typeof(Panel), typeof(Aperture) });

            return result;
        }
    }
}