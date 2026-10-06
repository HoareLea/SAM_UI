// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What the MODEL shows about the physical opening a design transfer route would have to pass through.
    /// <para>
    /// <b>Purely geometric, and deliberately not <c>PartFTransferOpeningStatus</c>.</b> That enum's
    /// stronger states - <c>ConfirmedOpening</c>, <c>CalculatedViaPermanentOpening</c> - rest on Approved
    /// Document F judgements: a recorded free area assessed against paragraph 1.25, a transfer device
    /// type, a compliance verdict. The Ventilation Design overlay is not entitled to any of those, and
    /// borrowing the enum would mean borrowing the judgement with it. What this overlay can honestly say
    /// is only what the geometry shows: whether the two rooms have a modelled door between them.
    /// </para>
    /// <para>
    /// <b>Independent of the flow.</b> A route can carry a fully determined design flow and still have no
    /// modelled opening - the flow says what the design needs to move, this says whether the model shows
    /// anywhere for it to move through. Neither answers the other, which is the same separation
    /// <c>PartFTransferOpeningStatus</c> keeps.
    /// </para>
    /// </summary>
    public enum DesignTransferOpeningStatus
    {
        /// <summary>No route has been assessed. The default, so an unset value never reads as established.</summary>
        NotAssessed,

        /// <summary>Exactly one modelled door aperture separates the two spaces, and the arrow crosses it.</summary>
        ModelledDoor,

        /// <summary>
        /// The two spaces adjoin through a real partition the plan draws, but the model carries no door
        /// aperture in it. The design still needs the air to cross here; nothing in the model says it can.
        /// </summary>
        NoModelledOpening,

        /// <summary>
        /// More than one modelled door separates the two spaces, so which of them carries the design air -
        /// or how it divides between them - is not fixed by the model. The route is shown at the
        /// partition rather than committed to one door, because picking one would assert a split nothing
        /// has calculated.
        /// </summary>
        MoreThanOneModelledOpening,
    }
}
