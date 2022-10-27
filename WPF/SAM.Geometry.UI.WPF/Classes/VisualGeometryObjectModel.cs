using SAM.Core.UI.WPF;

namespace SAM.Analytical.UI.WPF
{
    public class VisualGeometryObjectModel : VisualJSAMObject<Geometry.GeometryObjectModel>
    {
        public VisualGeometryObjectModel(Geometry.GeometryObjectModel analyticalModel)
            :base(analyticalModel)
        {

        }

        public Geometry.GeometryObjectModel GeometryObjectModel
        {
            get
            {
                return jSAMObject;
            }
        }
    }
}
