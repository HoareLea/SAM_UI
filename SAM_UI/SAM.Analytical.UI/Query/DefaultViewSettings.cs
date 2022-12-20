using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static ViewSettings DefaultViewSettings()
        {
            ViewSettings result = new ThreeDimensionalViewSettings(System.Guid.NewGuid(), "3D View", null, new System.Type[] { typeof(Panel), typeof(Aperture) });

            return result;
        }
    }
}