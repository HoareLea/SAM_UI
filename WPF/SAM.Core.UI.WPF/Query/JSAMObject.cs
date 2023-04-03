using System.Windows;

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

            if(@object is ITaggable)
            {
                Tag tag = ((ITaggable)@object)?.Tag;
                if(tag?.Value is T)
                {
                    return (T)(object)tag.Value;
                }
            }

            return default;
        }


    }
}
