using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static TranslateTransform TranslateTransform(this UIElement uIElement)
        {
            return (TranslateTransform)((TransformGroup)uIElement.RenderTransform).Children.First(translateTransform => translateTransform is TranslateTransform);
        }
    }
}
