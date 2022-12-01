using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static RayMeshGeometry3DHitTestResult RayMeshGeometry3DHitTestResult<T>(this Visual visual, Point point, out T t)
        {
            t = default;

            if (visual == null || point.IsNaN())
            {
                return null;
            }

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(visual, point);
            if (hitTestResult == null)
            {
                return null;
            }

            if(hitTestResult.VisualHit is CameraController)
            {
                return null;
            }

            if(hitTestResult.VisualHit is T)
            {
                t = (T)(object)hitTestResult.VisualHit;

                return hitTestResult as RayMeshGeometry3DHitTestResult;
            }


            return RayMeshGeometry3DHitTestResult(hitTestResult.VisualHit as Visual, point, out t);
        }

        public static RayMeshGeometry3DHitTestResult RayMeshGeometry3DHitTestResult<T>(this Visual visual, Point point)
        {
            return RayMeshGeometry3DHitTestResult(visual, point, out T t);
        }
    }
}
