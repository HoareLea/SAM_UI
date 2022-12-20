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

            return new TextAppearance(System.Windows.Media.Color.FromRgb(0, 0, 0), 1, "Segoe UI");
        }

        public static TextAppearance TextAppearance(this Space space, ViewSettings viewSettings)
        {
            TextAppearance result = viewSettings.GetAppearances<TextAppearance>(space)?.FirstOrDefault();
            if (result == null)
            {
                result = TextAppearance(space);
            }

            return result;
        }
    }
}