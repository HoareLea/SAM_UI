using System.Reflection;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static RayHitTestParameters RayFromViewportPoint(Viewport3DVisual viewport, Point point)
        {
            MethodInfo method = typeof(Camera).GetMethod("RayFromViewportPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            double distanceAdjustment = 0;
            object[] parameters = new object[]
            {
                point, viewport.Viewport.Size, null, distanceAdjustment
            };

            return (RayHitTestParameters)method.Invoke(viewport.Camera, parameters);
        }
    }
}
