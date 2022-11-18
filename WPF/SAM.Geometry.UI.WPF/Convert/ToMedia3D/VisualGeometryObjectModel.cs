using System.Collections.Generic;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static VisualGeometryObjectModel ToMedia3D(this GeometryObjectModel geometryObjectModel)
        {
            if(geometryObjectModel == null)
            {
                return null;
            }

            VisualGeometryObjectModel result = new VisualGeometryObjectModel(geometryObjectModel);

            List<ISAMGeometryObject> sAMGeometryObjects = geometryObjectModel?.GetSAMGeometryObjects<ISAMGeometryObject>();
            if (sAMGeometryObjects != null)
            {
                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    if(sAMGeometryObject == null)
                    {
                        continue;
                    }

                    IVisualGeometryObject visualGeometryObject = Create.IVisualGeometryObject(sAMGeometryObject);
                    if (visualGeometryObject == null)
                    {
                        continue;
                    }

                    result.Children.Add(visualGeometryObject as dynamic);
                }
            }

            //result.Children.Add()

            //Rect3D rext3D = Query.Bounds(result);

            //result.Children.Add(Core.UI.WPF.Create.ModelVisual3D_Text(
            //    "AAA",
            //    new SolidColorBrush(Colors.Black),
            //    true,
            //    1,
            //    new System.Windows.Media.Media3D.Point3D(rext3D.Location.X + rext3D.SizeX / 2, rext3D.Y + rext3D.SizeY / 2, rext3D.Z + rext3D.SizeZ / 2),
            //    true,
            //    new System.Windows.Media.Media3D.Vector3D(1, 0, 0),
            //    new System.Windows.Media.Media3D.Vector3D(0, 1, 0)
            //    ));

            return result;
        }
    }
}
