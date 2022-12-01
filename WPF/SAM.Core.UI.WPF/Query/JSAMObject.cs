using System.Windows;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static T JSAMObject<T>(this DependencyObject dependencyObject) where T : IJSAMObject
        {
            if(dependencyObject == null)
            {
                return default;
            }

            object @object = dependencyObject.GetValue(DependencyProperty.IJSAMObjectProperty);
            if(@object is T)
            {
                return (T)(object)@object;
            }

            return default;
        }


    }
}
