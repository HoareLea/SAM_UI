namespace SAM.Geometry.UI
{
    public static partial class Query
    {
        public static Mode Mode(this ViewSettings viewSettings)
        {
            if(viewSettings == null)
            {
                return UI.Mode.Undefined;
            }

            if(viewSettings is ThreeDimensionalViewSettings)
            {
                return UI.Mode.ThreeDimensional;
            }

            if(viewSettings is TwoDimensionalViewSettings)
            {
                return UI.Mode.TwoDimensional;
            }

            return UI.Mode.Undefined;
        }

    }
}
