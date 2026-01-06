// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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

            ITaggable taggable = (ITaggable)jSAMObject;

            if(taggable.Tag == null)
            {
                return default;
            }

            return taggable.Tag.GetValue<T>();



        }
    }
}
