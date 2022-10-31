namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static IVisualGeometryObject IVisualGeometryObject(this ISAMGeometryObject sAMGeometryObject)
        {
            if (sAMGeometryObject == null)
            {
                return null;
            }

            return Create.VisualGeometryObject(sAMGeometryObject as dynamic);
        }
    }
}
