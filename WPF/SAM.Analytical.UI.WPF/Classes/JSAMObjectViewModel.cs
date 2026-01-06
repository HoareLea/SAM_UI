// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.ObjectModel;

namespace SAM.Analytical.UI.WPF
{
    public class JSAMObjectViewModel<TJSAMObject> where TJSAMObject : IJSAMObject
    {
        public ObservableCollection<TJSAMObject> Items { get; set; } = [];

        public JSAMObjectViewModel()
        {

        }
    }
}
