using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SAM.Geometry.UI.WPF
{
    public static partial class Query
    {
        public static bool ContainsAny<T>(this Visual3DCollection visual3DCollection, IEnumerable<Guid> guids) where T : SAMObject
        {
            if(visual3DCollection == null || visual3DCollection.Count == 0 || guids == null || guids.Count() == 0)
            {
                return false;
            }

            foreach(Guid guid in guids)
            {
                Visual3D visual3D = Core.UI.WPF.Query.Visual3D<T>(visual3DCollection, guid);
                if(visual3D == null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
