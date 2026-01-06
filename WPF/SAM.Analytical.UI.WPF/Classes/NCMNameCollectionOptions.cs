// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    public class NCMNameCollectionOptions
    {
        public bool Editable { get; set; } = false;

        public NCMNameCollectionOptions(bool editable = false)
        {
            Editable = editable;
        }
    }
}
