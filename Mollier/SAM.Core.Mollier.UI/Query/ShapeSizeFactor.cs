namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static double ShapeSizeFactor(int deviceDpi)
        {
            Math.LinearEquation linearEquation = Math.Create.LinearEquation(96.0, 0.76, 120.0, 1.0);

            return linearEquation.Evaluate((double)deviceDpi);
        }
    }
}
