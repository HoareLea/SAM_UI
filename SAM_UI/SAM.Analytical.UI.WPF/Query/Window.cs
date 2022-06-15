using System.Windows;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static System.Windows.Window Window(this FrameworkElement frameworkElement)
        {
            if(frameworkElement == null)
            {
                return null;
            }

            if(frameworkElement.Parent == null)
            {
                return null;
            }

            if(frameworkElement.Parent is System.Windows.Window)
            {
                return (System.Windows.Window)frameworkElement.Parent;
            }

            return Window(frameworkElement.Parent as FrameworkElement);
        }
    }
}
