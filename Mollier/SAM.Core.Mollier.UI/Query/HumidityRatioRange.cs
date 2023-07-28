namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Range<double> HumidityRatioRange(this MollierControlSettings mollierControlSettings)
        {
            return mollierControlSettings?.MollierRange()?.HumidityRatioRange;

        }
    }
}
