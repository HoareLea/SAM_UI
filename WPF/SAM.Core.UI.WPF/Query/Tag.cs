using System.Windows;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static T Tag<T>(this DependencyObject dependencyObject) where T: IJSAMObject
        {
            IJSAMObject jSAMObject = JSAMObject<IJSAMObject>(dependencyObject);
            if(jSAMObject == null)
            {
                return default;
            }

            if(!(jSAMObject is ITaggable))
            {
                return default;
            }

            ITaggable taggable = (ITaggable)(object)jSAMObject;

            if(taggable.Tag == null)
            {
                return default;
            }

            return taggable.Tag.GetValue<T>();
        }
    }
}
