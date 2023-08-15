using System; 

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool IsEqual(this IMollierProcess mollierProcess1, IMollierProcess mollierProcess2, double tolerance = Tolerance.Distance)
        {
            if(mollierProcess1 == null || mollierProcess2 == null)
            {
                return true;
            }

            if(System.Math.Abs(mollierProcess1.Start.DryBulbTemperature - mollierProcess2.Start.DryBulbTemperature) >= tolerance)
            {
                return false;
            }

            if (System.Math.Abs(mollierProcess1.Start.DryBulbTemperature - mollierProcess2.Start.DryBulbTemperature) >= tolerance)
            {
                return false;
            }


            if (System.Math.Abs(mollierProcess1.Start.DryBulbTemperature - mollierProcess2.Start.DryBulbTemperature) >= tolerance)
            {
                return false;
            }


            if (System.Math.Abs(mollierProcess1.Start.DryBulbTemperature - mollierProcess2.Start.DryBulbTemperature) >= tolerance)
            {
                return false;
            }

            return true;
        }
    }
}
