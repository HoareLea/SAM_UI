using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Vector3D RollAxis(this ProjectionCamera projectionCamera)
        {
            if(projectionCamera == null)
            {
                return new Vector3D(double.NaN, double.NaN, double.NaN);
            }

            return projectionCamera.LookDirection;
        }
    }
}
