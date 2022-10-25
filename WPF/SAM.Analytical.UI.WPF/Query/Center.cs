using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static Point3D Center<T>(IEnumerable<T> visualSAMObjects) where T: IVisualJSAMObject
        {
            if(visualSAMObjects == null || visualSAMObjects.Count() == 0)
            {
                return new Point3D(double.NaN, double.NaN, double.NaN);
            }

            Rect3D rect3D = Rect3D.Empty;
            foreach(T visualSAMObject in visualSAMObjects)
            {
                GeometryModel3D geometryModel3D = visualSAMObject?.GeometryModel3D;
                if(geometryModel3D == null)
                {
                    continue;
                }

                Rect3D rect3D_Temp = visualSAMObject.GeometryModel3D.Bounds;
                if(rect3D == Rect3D.Empty)
                {
                    rect3D = rect3D_Temp;
                }
                else
                {
                    rect3D.Union(rect3D_Temp);
                }
            }

            return Geometry.UI.WPF.Query.Center(rect3D);
        }
    }
}
