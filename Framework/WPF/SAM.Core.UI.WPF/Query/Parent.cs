using System.Windows;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {

        public static T Parent<T>(this DependencyObject dependencyObject) where T : DependencyObject
        {
            if(dependencyObject == null)
            {
                return null;
            }

            return VisualTreeHelper.GetParent(dependencyObject) as T;
        }
    }
}