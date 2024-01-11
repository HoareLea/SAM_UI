using SAM.Core;
using SAM.Geometry;
using SAM.Geometry.Object;
using System;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static T IJSAMObject<T>(this GeometryObjectModel geometryObjectModel, Guid guid, bool recursive = true) where T : SAMObject, IAnalyticalObject
        {
            if (geometryObjectModel == null)
            {
                return default(T);
            }

            Func<ISAMGeometryObject, T> func_1 = new Func<ISAMGeometryObject, T>((ISAMGeometryObject x) => 
            {
                ITaggable taggable = x as ITaggable;
                if(taggable == null)
                {
                    return null;
                }

                return taggable.Tag?.Value as T;
            });

            Func<ISAMGeometryObject, bool> func_2 = new Func<ISAMGeometryObject, bool>((ISAMGeometryObject x) =>
            {
                T t = func_1.Invoke(x);
                if(t == null)
                {
                    return false;
                }

                return t.Guid == guid;

            });

            ISAMGeometryObject sAMGeometryObject = geometryObjectModel.GetSAMGeometryObject(func_2, recursive);
            if(sAMGeometryObject == null)
            {
                return default(T);
            }

            return func_1.Invoke(sAMGeometryObject);
        }
    }
}