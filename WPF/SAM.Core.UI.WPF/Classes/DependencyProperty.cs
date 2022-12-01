using System.Windows;

namespace SAM.Core.UI
{
    public static partial class DependencyProperty
    {
        public static readonly System.Windows.DependencyProperty IJSAMObjectProperty = System.Windows.DependencyProperty.Register( "IJSAMObject", typeof(IJSAMObject), typeof(DependencyObject), new PropertyMetadata(null));
    }
}
