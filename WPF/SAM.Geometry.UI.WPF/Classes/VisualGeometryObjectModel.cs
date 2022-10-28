using SAM.Core.UI.WPF;

namespace SAM.Geometry.UI.WPF
{
    public class VisualGeometryObjectModel : VisualJSAMObject<GeometryObjectModel>
    {
        public VisualGeometryObjectModel(GeometryObjectModel analyticalModel)
            :base(analyticalModel)
        {

        }

        public GeometryObjectModel GeometryObjectModel
        {
            get
            {
                return jSAMObject;
            }
        }
    }
}
