using System.Windows;
using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void Zoom(this UIElement uIElement, Point point, double delta)
        {
            ScaleTransform scaleTransform = Query.ScaleTransform(uIElement);
            TranslateTransform translateTransform = Query.TranslateTransform(uIElement);

            double zoom = delta > 0 ? .2 : -.2;
            if (!(delta > 0) && (scaleTransform.ScaleX < .4 || scaleTransform.ScaleY < .4))
                return;

            double absoluteX = point.X * scaleTransform.ScaleX + translateTransform.X;
            double absoluteY = point.Y * scaleTransform.ScaleY + translateTransform.Y;

            scaleTransform.ScaleX += zoom;
            scaleTransform.ScaleY += zoom;

            translateTransform.X = absoluteX - point.X * scaleTransform.ScaleX;
            translateTransform.Y = absoluteY - point.Y * scaleTransform.ScaleY;
        }
    }
}