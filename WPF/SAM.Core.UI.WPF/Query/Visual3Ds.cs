// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static List<T> Visual3Ds<T>(this Viewport3D viewport3D) where T: Visual3D
        {
            if(viewport3D == null)
            {
                return null;
            }

            return Visual3Ds<T>(viewport3D.Children);
        }

        public static List<T> Visual3Ds<T>(this Visual3DCollection visual3DCollection) where T : Visual3D
        {
            if (visual3DCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (object @object in visual3DCollection)
            {
                if (!(@object is T))
                {
                    if(@object is ModelVisual3D)
                    {
                        ModelVisual3D modelVisual3D = (ModelVisual3D)@object;

                        List<T> visualSAMObjects = Visual3Ds<T>(modelVisual3D.Children);
                        if(visualSAMObjects != null)
                        {
                            result.AddRange(visualSAMObjects);
                        }
                    }
                    
                    continue;
                }

                result.Add((T)@object);
            }

            return result;
        }

        public static List<T> Visual3Ds<T>(this Visual3DCollection visual3DCollection, IEnumerable<System.Type> types)
        {
            if (visual3DCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (Visual3D visual3D in visual3DCollection)
            {
                if (!(visual3D is T))
                {
                    if (visual3D is ModelVisual3D)
                    {
                        ModelVisual3D modelVisual3D = (ModelVisual3D)visual3D;
                        List<T> visualSAMObjects = Visual3Ds<T>(modelVisual3D.Children, types);
                        if (visualSAMObjects != null)
                        {
                            result.AddRange(visualSAMObjects);
                        }
                    }

                    continue;
                }

                if (types != null && types.Count() != 0)
                {
                    IJSAMObject jSAMObject = JSAMObject<IJSAMObject>(visual3D);
                    if (jSAMObject == null)
                    {
                        continue;
                    }

                    bool remove = false;
                    foreach (System.Type type in types)
                    {
                        if (type == null)
                        {
                            continue;
                        }

                        if (jSAMObject.GetType().IsAssignableFrom(type))
                        {
                            remove = true;
                            break;
                        }
                    }

                    if (!remove)
                    {
                        continue;
                    }
                }


                result.Add((T)(object)visual3D);
            }

            return result;
        }
    }
}
