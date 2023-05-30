using HelixToolkit.Wpf;
using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Convert
    {
        public static LinesVisual3D ToMedia3D(this Segment3DObject segment3DObject)
        {
            if(segment3DObject == null)
            {
                return null;
            }

            return ToMedia3D(segment3DObject.Segment3D, segment3DObject.CurveAppearance.Thickness);
        }

        public static LinesVisual3D ToMedia3D(this ISegmentable3D segmentable3D, double thickness = Core.Tolerance.MacroDistance)
        {
            return ToMedia3D(new ISegmentable3D[] { segmentable3D }, thickness);
        }

        public static LinesVisual3D ToMedia3D(this Polygon3DObject polygon3DObject)
        {
            if (polygon3DObject == null)
            {
                return null;
            }

            return ToMedia3D(polygon3DObject.Polygon3D, polygon3DObject.CurveAppearance.Thickness);
        }

        public static LinesVisual3D ToMedia3D(this Polyline3DObject polyline3DObject)
        {
            if (polyline3DObject == null)
            {
                return null;
            }

            return ToMedia3D(polyline3DObject.Polyline3D, polyline3DObject.CurveAppearance.Thickness);
        }

        public static LinesVisual3D ToMedia3D(this BoundingBox3DObject boundingBox3DObject)
        {
            if (boundingBox3DObject == null)
            {
                return null;
            }

            return ToMedia3D(boundingBox3DObject.BoundingBox3D, boundingBox3DObject.CurveAppearance.Thickness);
        }

        public static LinesVisual3D ToMedia3D<T>(this IEnumerable<T> segmentable3Ds, double thickness = Core.Tolerance.MacroDistance) where T: ISegmentable3D
        {
            if(segmentable3Ds == null)
            {
                return null;
            }

            LinesVisual3D linesVisual3D = new LinesVisual3D();

            foreach (T segmentable3D in segmentable3Ds)
            {
                List<Point3D> point3Ds = segmentable3D?.GetPoints();
                if (point3Ds == null)
                {
                    continue; ;
                }

                linesVisual3D.Points = Create.Point3DCollection(point3Ds);
                linesVisual3D.Thickness = thickness;
            }


            return linesVisual3D;
        }
    }
}
