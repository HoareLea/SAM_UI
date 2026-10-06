// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What accepting the Review iteration window leads to, which depends on the command that opened it. It
    /// decides the window's WORDING and nothing else: the Prepare &amp; Run Hub goes on into TAS after an
    /// accepted review, and the Prepare Iteration ribbon command stops once the model is adopted. Each
    /// command's behaviour is its own; this only stops the window promising the other one's.
    /// </summary>
    public enum PartOReviewIntent
    {
        /// <summary>
        /// The Prepare Iteration ribbon command: accepting adopts the prepared model and the run is left
        /// Prepared. No simulation is started. The default, so a window nobody told otherwise never promises TAS.
        /// </summary>
        PrepareOnly,

        /// <summary>
        /// The Prepare &amp; Run Hub: accepting adopts the prepared model and the Hub continues into the
        /// full-year TAS simulation and the TM59 assessment.
        /// </summary>
        PrepareAndRun,
    }
}
