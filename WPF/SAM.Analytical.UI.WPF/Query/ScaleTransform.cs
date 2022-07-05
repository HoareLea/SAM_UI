using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static ScaleTransform ScaleTransform(this UIElement uIElement)
        {
            if(uIElement == null)
            {
                return null;
            }

            return (ScaleTransform)((TransformGroup)uIElement.RenderTransform).Children.First(scaleTransform => scaleTransform is ScaleTransform);
        }
    }
}
