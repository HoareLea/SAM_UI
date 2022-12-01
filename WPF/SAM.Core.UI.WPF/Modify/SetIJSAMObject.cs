using System.Windows;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static void SetIJSAMObject(this DependencyObject dependencyObject, IJSAMObject jSAMObject)
        {
            if(dependencyObject == null)
            {
                return;
            }

            dependencyObject.SetValue(DependencyProperty.IJSAMObjectProperty, jSAMObject);
        }
    }
}