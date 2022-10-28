using SAM.Geometry.Spatial;

namespace SAM.Geometry.UI
{
    public class Polygon3DObject : Polygon3D, IPolygon3DObject
    {
        public Polygon3D Polygon3D
        {
            get
            {
                return new Polygon3D(this);
            }
        }

        public Polygon3DObject(Polygon3D polygon3D)
            : base(polygon3D)
        {

        }
    }
}
