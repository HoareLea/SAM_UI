// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;

namespace SAM.Analytical.UI.WPF
{
    public class CreateCaseViewModel<TCase> : JSAMObjectViewModel<TCase> where TCase: Case
    {
        public CreateCaseViewModel()
        {

        }
    }
}
