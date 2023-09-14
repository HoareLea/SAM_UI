namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Caluclates dew point for cooling process
        /// </summary>
        /// <param name="coolingProcess">Cooling process</param>
        /// <returns>Dew point</returns>
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
