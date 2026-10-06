// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>Which dwellings a saved view draws the Part F airflow overlay for.</summary>
    public enum PartFDwellingFilter
    {
        /// <summary>
        /// Every dwelling assessed on the level, each drawn from its own assessment. This is the
        /// engineering drawing case, and the reason the overlay had to stop being one-dwelling-only.
        /// </summary>
        [Description("All dwellings on level")] AllDwellingsOnLevel,

        /// <summary>Only the dwelling named by the view's stored dwelling guid.</summary>
        [Description("Selected dwelling")] SelectedDwelling,

        /// <summary>Every dwelling in the zone category the view's dwelling belongs to.</summary>
        [Description("Selected dwelling category")] SelectedDwellingCategory,

        /// <summary>No dwelling. The plan is drawn without any airflow.</summary>
        [Description("None")] None,
    }
}
