// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Criteria
    /// </summary>
    [Description("Criteria")]
    public enum Criteria
    {
        /// <summary>
        /// Undefined
        /// </summary>
        [Description("Undefined")] Undefined,

        /// <summary>
        /// All criteria have been met
        /// </summary>
        [Description("All")] All,

        /// <summary>
        /// Not all of the criteria have been met
        /// </summary>
        [Description("Not All")] NotAll,

        /// <summary>
        /// None of the criteria have been met
        /// </summary>
        [Description("None")] None,
    }
}
