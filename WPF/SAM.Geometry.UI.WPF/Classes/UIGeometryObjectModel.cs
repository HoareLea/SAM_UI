namespace SAM.Geometry.UI.WPF
{
    public class UIGeometryObjectModel : Core.UI.UIJSAMObject<GeometryObjectModel>
    {
        public UIGeometryObjectModel(string path)
       : base(path)
        {

        }

        public UIGeometryObjectModel(GeometryObjectModel geometryObjectModel)
            : base(geometryObjectModel)
        {

        }

        public UIGeometryObjectModel()
            : base()
        {

        }
    }
}
