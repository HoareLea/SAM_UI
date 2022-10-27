using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject<T> VisualGeometryObject<T>(this T sAMGeometryObject, Material material, double thickness = 0.001) where T: ISAMGeometryObject
        {
            if(sAMGeometryObject == null || material == null)
            {
                return null;
            }

            Model3D model3D = null;
            if(sAMGeometryObject is IFace3DObject)
            {
                Face3D face3D = ((IFace3DObject)sAMGeometryObject).Face3D;

                model3D = new GeometryModel3D(face3D.ToMedia3D(), material);
            }
            else if(sAMGeometryObject is ISegmentable3DObject)
            {

                ISegmentable3D segmentable3D = ((ISegmentable3DObject)sAMGeometryObject).Segmentable3D;

                Model3DGroup model3DGroup = new Model3DGroup();
                segmentable3D.GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, thickness), material)));

                model3D = model3DGroup;
            }

            if(model3D == null)
            {
                return null;
            }

            VisualGeometryObject<T> result = new VisualGeometryObject<T>(sAMGeometryObject);
            result.Content = model3D;

            return result;
        }
    }
}
