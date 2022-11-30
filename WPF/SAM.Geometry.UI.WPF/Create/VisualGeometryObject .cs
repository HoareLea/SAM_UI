namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject VisualGeometryObject(this ISAMGeometryObject sAMGeometryObject)
        {
            if(sAMGeometryObject == null)
            {
                return null;
            }

            VisualGeometryObject result = new VisualGeometryObject(sAMGeometryObject);
            result.Content = Model3D(sAMGeometryObject as dynamic);

            return result;
        }
    }
}
