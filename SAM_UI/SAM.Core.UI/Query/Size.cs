using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static Size Size(this string text, Font font)
        {
            if (text == null || font == null)
                return System.Drawing.Size.Empty;

            return TextRenderer.MeasureText(text, font);
        }
    }
}