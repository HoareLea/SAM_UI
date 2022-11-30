using SAM.Core.UI.WPF;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static IVisualJSAMObject SelectableVisualJSAMObject(this IVisualJSAMObject visualJSAMObject)
        {
            if(visualJSAMObject == null)
            {
                return null;
            }

            List<DependencyObject> dependencyObjects = Core.UI.WPF.Query.Branch(visualJSAMObject as DependencyObject);
            if(dependencyObjects == null || dependencyObjects.Count == 0)
            {
                return visualJSAMObject;
            }

            if(dependencyObjects == null || dependencyObjects.Count <= 1)
            {
                return visualJSAMObject;
            }

            for(int i = dependencyObjects.Count -2; i >= 0; i--)
            {
                if(dependencyObjects[i] is IVisualJSAMObject)
                {
                    return (IVisualJSAMObject)dependencyObjects[i];
                }
            }

            return visualJSAMObject;
        }
    }
}
