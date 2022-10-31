using SAM.Geometry.Spatial;

namespace SAM.Analytical.UI
{
    public class VisualAperture : SAMGeometry3DObjectCollection
    {
        private Aperture aperture;

        public VisualAperture(Aperture aperture)
        {
            this.aperture = aperture;
        }
    }
}
