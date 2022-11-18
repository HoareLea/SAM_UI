using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Point3D Center(this Rect3D rect3D)
        {
            if (rect3D == null)
            {
                return new Point3D(double.NaN, double.NaN, double.NaN);
            }

            return new Point3D((rect3D.Location.X + rect3D.SizeX) / 2, (rect3D.Location.Y + rect3D.SizeY) / 2, (rect3D.Location.Z + rect3D.SizeZ) / 2);
        }

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

            return Center(rect3D);
        }
    }
}
