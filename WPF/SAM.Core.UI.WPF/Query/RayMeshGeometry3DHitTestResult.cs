using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static RayMeshGeometry3DHitTestResult RayMeshGeometry3DHitTestResult<T>(this Visual visual, Point point, IEnumerable<System.Type> excludedTypes, out T t) where T : IVisualJSAMObject
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

            DependencyObject dependencyObject = hitTestResult.VisualHit;

            if (dependencyObject is CameraController)
            {
                return null;
            }

            if (dependencyObject is T)
            {
                bool excluded = false;
                if(excludedTypes != null)
                {
                    IJSAMObject jSAMObject = (dependencyObject as dynamic).JSAMObject;

                    System.Type type = jSAMObject.GetType();

                    foreach(System.Type type_Temp in excludedTypes)
                    {
                        if(type_Temp.IsAssignableFrom(type))
                        {
                            excluded = true;
                            break;
                        }
                    }
                }

                if(!excluded)
                {
                    t = (T)(object)dependencyObject;

                    return hitTestResult as RayMeshGeometry3DHitTestResult;
                }
            }

            return RayMeshGeometry3DHitTestResult(dependencyObject as Visual, point, out t);
        }

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
