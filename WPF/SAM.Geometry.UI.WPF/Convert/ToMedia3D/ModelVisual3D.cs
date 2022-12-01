using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static ModelVisual3D ToMedia3D(this GeometryObjectModel geometryObjectModel)
        {
            if (geometryObjectModel == null)
            {
                return null;
            }

            ModelVisual3D result = new ModelVisual3D();
            List<ISAMGeometryObject> sAMGeometryObjects = geometryObjectModel?.GetSAMGeometryObjects<ISAMGeometryObject>();
            if (sAMGeometryObjects != null)
            {
                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    if (sAMGeometryObject == null)
                    {
                        continue;
                    }

                    ModelVisual3D modelVisual3D = ToMedia3D(sAMGeometryObject);
                    if(modelVisual3D == null)
                    {
                        continue;
                    }

                    result.Children.Add(modelVisual3D);
                }
            }

            Core.UI.WPF.Modify.SetIJSAMObject(result, geometryObjectModel);

            return result;
        }

        public static ModelVisual3D ToMedia3D(this ISAMGeometryObject sAMGeometryObject)
        {
            if(sAMGeometryObject == null)
            {
                return null;
            }

            Model3D model3D = Create.Model3D(sAMGeometryObject as dynamic);
            if(model3D == null)
            {
                return null;
            }

            ModelVisual3D result = new ModelVisual3D();
            result.Content = model3D;

            return result;
        }
    }
}
