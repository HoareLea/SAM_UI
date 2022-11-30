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
    }
}
