namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Counts default colors of Gradient Point
        /// </summary>
        /// <returns>Returns default colors of Gradient Point</returns>
        public static PointGradientVisibilitySetting DefaultPointGradientVisibilitySetting()
        {
            PointGradientVisibilitySetting result = new PointGradientVisibilitySetting(System.Drawing.Color.Red, System.Drawing.Color.Blue);

            return result;
        }
    }
}

