namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static MollierPoint DewPoint(this CoolingProcess coolingProcess)
        {
            if(coolingProcess == null)
            {
                return null;
            }

            return Mollier.Query.DewPoint(coolingProcess.Start, coolingProcess.Efficiency);
        }
    }
}
