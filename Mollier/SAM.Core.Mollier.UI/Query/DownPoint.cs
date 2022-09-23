namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static MollierPoint DownPoint(this CoolingProcess coolingProcess)
        {
            if(coolingProcess == null)
            {
                return null;
            }

            return Mollier.Query.DewPoint(coolingProcess.Start, coolingProcess.Efficiency);
        }
    }
}
