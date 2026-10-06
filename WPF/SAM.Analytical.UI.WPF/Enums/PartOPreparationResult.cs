// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// How a Prepare &amp; Run preparation ended, as far as the Hub needs to know. Kept apart so the Hub can
    /// say that an engineer DECLINED the review - a deliberate choice before any TAS work - rather than
    /// saying nothing, which is what it said for a refusal. Nothing persists it: it is the return value of
    /// one call, and the Hub turns it into its last-outcome line.
    /// </summary>
    public enum PartOPreparationResult
    {
        /// <summary>
        /// Nothing was adopted for a reason already reported to the user: the preparation refused, the
        /// assignments could not be committed, or the run would not adopt the model.
        /// </summary>
        NotPrepared,

        /// <summary>
        /// The engineer cancelled the Review iteration window. The loaded model is untouched, the run is
        /// unchanged and TAS was not started.
        /// </summary>
        Declined,

        /// <summary>
        /// The engineer accepted the review; the prepared model is adopted and the run is Prepared.
        /// </summary>
        Adopted,
    }
}
