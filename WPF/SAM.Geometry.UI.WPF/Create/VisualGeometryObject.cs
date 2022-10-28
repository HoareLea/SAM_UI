using SAM.Geometry.Spatial;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Create
    {
        public static VisualGeometryObject<T> VisualGeometryObject<T>(this T sAMGeometry3DObject, Material material, double thickness = 0.001) where T: ISAMGeometry3DObject
        {
            if(sAMGeometry3DObject == null || material == null)
            {
                return null;
            }

            ISAMGeometry3D sAMGeometry3D = Spatial.Query.SAMGeometry3D<ISAMGeometry3D>(sAMGeometry3DObject);

            Model3D model3D = null;
            if(sAMGeometry3D is Face3D)
            {
                model3D = new GeometryModel3D(((Face3D)sAMGeometry3D).ToMedia3D(), material);
            }
            else if(sAMGeometry3D is ISegmentable3D)
            {
                Model3DGroup model3DGroup = new Model3DGroup();
                ((ISegmentable3D)sAMGeometry3D).GetSegments().ForEach(x => model3DGroup.Children.Add(new GeometryModel3D(x.ToMedia3D(true, thickness), material)));

                model3D = model3DGroup;
            }

            if(model3D == null)
            {
                return null;
            }

            VisualGeometryObject<T> result = new VisualGeometryObject<T>(sAMGeometry3DObject);
            result.Content = model3D;

            return result;
        }
    }
}
