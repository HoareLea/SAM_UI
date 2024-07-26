using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Visual3D Visual3D<T>(this Viewport3D viewport3D, System.Guid guid) where T: SAMObject
        {
            if(viewport3D == null)
            {
                return null;
            }

            return Visual3D<T>(viewport3D.Children, guid);
        }

        public static Visual3D Visual3D<T>(this Visual3DCollection visual3DCollection, System.Guid guid) where T: SAMObject
        {
            if (visual3DCollection == null)
            {
                return null;
            }

            foreach (Visual3D visual3D in visual3DCollection)
            {
                IJSAMObject jSAMObject = null;

                if (visual3D is ModelVisual3D)
                {
                    ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                    Visual3D result = Visual3D<T>(modelVisual3D.Children, guid);
                    if (result != null)
                    {
                        return result;
                    }

                    if(modelVisual3D.Content != null)
                    {
                        jSAMObject = JSAMObject<IJSAMObject>(modelVisual3D.Content);

                        if (jSAMObject is T)
                        {
                            SAMObject sAMObject = jSAMObject as SAMObject;
                            if (sAMObject.Guid == guid)
                            {
                                return visual3D;
                            }
                        }

                        if (jSAMObject is ITaggable)
                        {
                            T sAMObject = ((ITaggable)jSAMObject).Tag?.Value as T;
                            if (sAMObject != null && sAMObject.Guid == guid)
                            {
                                return visual3D;
                            }
                        }
                    }
                }

                jSAMObject = JSAMObject<IJSAMObject>(visual3D);

                if(jSAMObject is T)
                {
                    SAMObject sAMObject = jSAMObject as SAMObject;
                    if(sAMObject.Guid == guid)
                    {
                        return visual3D;
                    }
                }

                if(jSAMObject is ITaggable)
                {
                    T sAMObject = ((ITaggable)jSAMObject).Tag?.Value as T;
                    if (sAMObject != null && sAMObject.Guid == guid)
                    {
                        return visual3D;
                    }
                }
            }

            return null;
        }
    }
}
