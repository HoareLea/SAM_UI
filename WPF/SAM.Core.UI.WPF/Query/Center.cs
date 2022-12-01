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

        public static Point3D Center<T>(IEnumerable<T> visual3Ds) where T: Visual3D
        {
            if(visual3Ds == null || visual3Ds.Count() == 0)
            {
                return new Point3D(double.NaN, double.NaN, double.NaN);
            }

            List<Rect3D> rect3Ds = new List<Rect3D>();

            foreach (Visual3D visual3D in visual3Ds)
            {
                if(visual3D == null)
                {
                    continue;
                }

                if(visual3D is ModelVisual3D)
                {
                    ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                    
                    if(modelVisual3D.Content != null)
                    {
                        rect3Ds.Add(modelVisual3D.Content.Bounds);
                    }

                    if(modelVisual3D.Children != null && modelVisual3D.Children.Count != 0)
                    {
                        foreach(Visual3D visual3D_Temp in modelVisual3D.Children)
                        {

                        }
                    }
                }
            }

            if (rect3Ds == null || rect3Ds.Count == 0)
            {
                return new Point3D(double.NaN, double.NaN, double.NaN);
            }

            Rect3D rect3D = Rect3D.Empty;
            foreach (Rect3D rect3D_Temp in rect3Ds)
            {
                if (rect3D == Rect3D.Empty)
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
