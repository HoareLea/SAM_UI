using SAM.Geometry.UI;
using System.Linq;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {       
        public static TextAppearance TextAppearance(this Space space)
        {
            if (space == null)
            {
                return null;
            }

            return Geometry.UI.Query.DefaultTextAppearance();
        }

        public static TextAppearance TextAppearance(this Space space, ViewSettings viewSettings)
        {
            if (viewSettings == null)
            {
                return null;
            }

            TextAppearance result = viewSettings.GetAppearances<TextAppearance>(space)?.FirstOrDefault();
            if (result == null)
            {
                if(viewSettings is TwoDimensionalViewSettings)
                {
                    result = ((TwoDimensionalViewSettings)viewSettings).TextAppearance;
                }
            }

            if(result == null)
            {
                result = TextAppearance(space);
            }

            return result;
        }
    }
}