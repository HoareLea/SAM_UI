using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static T JSAMObject<T>(this IVisualJSAMObject visualJSAMObject) where T : IJSAMObject
        {
            if(visualJSAMObject == null)
            {
                return default;
            }

            try
            {
                object value = (visualJSAMObject as dynamic).JSAMObject;
                return value is T ? (T)(object)value : default;
            }
            catch
            {

            }


            return default;
        }

        public static T JSAMObject<T>(this Model3D model3D) where T : IJSAMObject
        {
            if(model3D == null)
            {
                return default;
            }

            object @object = model3D.GetValue(DependencyProperty.IJSAMObjectProperty);
            if(@object is T)
            {
                return (T)(object)@object;
            }

            return default;
        }


    }
}
