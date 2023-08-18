using System; 

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool IsEqual(this IMollierProcess mollierProcess1, IMollierProcess mollierProcess2, double tolerance = Tolerance.Distance)
        {
            if(mollierProcess1 == null || mollierProcess2 == null)
            {
                return false;
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
    
        public static bool IsEqual(this IMollierPoint mollierPoint1, IMollierPoint mollierPoint2, double tolerance = Tolerance.Distance)
        {
            if(mollierPoint1 == null || mollierPoint2 == null)
            {
                return false;
            }

            MollierPoint mollierPoint_1;
            MollierPoint mollierPoint_2;
            if(mollierPoint1 is UIMollierPoint)
            {
                mollierPoint_1 = (mollierPoint1 as UIMollierPoint).MollierPoint;
            }
            else if(mollierPoint1 is MollierPoint)
            {
                mollierPoint_1 = mollierPoint1 as MollierPoint;
            }
            else
            {
                return false;
            }

            if (mollierPoint2 is UIMollierPoint)
            {
                mollierPoint_2 = (mollierPoint2 as UIMollierPoint).MollierPoint;
            }
            else if (mollierPoint2 is MollierPoint)
            {
                mollierPoint_2 = mollierPoint2 as MollierPoint;
            }
            else
            {
                return false;
            }

            if (System.Math.Abs(mollierPoint_1.DryBulbTemperature - mollierPoint_2.DryBulbTemperature) >= tolerance)
            {
                return false;
            }
            if (System.Math.Abs(mollierPoint_1.HumidityRatio - mollierPoint_2.HumidityRatio) >= tolerance)
            {
                return false;
            }

            return true;
        }
    }
}
