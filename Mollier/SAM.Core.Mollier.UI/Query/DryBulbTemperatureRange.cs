namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Range<double> DryBulbTemperatureRange(this MollierControlSettings mollierControlSettings)
        {
            return mollierControlSettings?.MollierRange()?.DryBulbTemperatureRange;
        }
    }
}
