using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public class VisualGeometryObject<T> : Core.UI.WPF.VisualJSAMObject<T>, IVisualGeometryObject where T : ISAMGeometryObject
    {
        protected T sAMGeometryObject;

        public VisualGeometryObject(T sAMGeometryObject)
            :base(sAMGeometryObject)
        {
            this.sAMGeometryObject = sAMGeometryObject;
        }

        public GeometryModel3D GeometryModel3D
        {
            get
            {
                return Content as GeometryModel3D;
            }
        }
    }
}
