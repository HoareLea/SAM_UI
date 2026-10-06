// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// How far the Approved Document O run in this session has got. A lifecycle, not a set of flags: the
    /// states are reached in order and each one owns different objects.
    /// </summary>
    public enum PartORunState
    {
        /// <summary>
        /// Nothing is pending. Either no iteration has been prepared, or a prepared one was invalidated -
        /// <c>PartORun.InvalidationReason</c> tells the two apart.
        /// </summary>
        None,

        /// <summary>
        /// An iteration has been prepared and its model adopted. The preparation output and its scenarios are
        /// owned from here; there is no simulation yet, so nothing can be assessed.
        /// </summary>
        Prepared,

        /// <summary>
        /// A TAS workflow completed over the prepared model. This state owns the model the workflow returned
        /// and the TSD it wrote, alongside the scenarios of the preparation it belongs to - the only state a
        /// TM59 assessment may run from.
        /// </summary>
        WorkflowCompleted
    }
}
