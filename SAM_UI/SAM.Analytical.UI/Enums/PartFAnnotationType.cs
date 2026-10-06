// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Which annotation on an object a tag is, for the shared <see cref="PartFTagPlacement"/> adapter: a
    /// manual position is keyed by an object's guid together with this, so one object can carry more than
    /// one movable label without their positions colliding.
    /// <para>
    /// Named for Part F, which is where the adapter started, but not Part F's alone: the Ventilation
    /// Design overlay's tags are placed through the same adapter and need the same kind of key, and adding
    /// a second, near-identical enum for it would only be a second name for the same thing - see
    /// <see cref="DesignSupply"/>. Neither overlay's engineering data crosses into the other because of
    /// this; the key means nothing beyond "which drawn label is this".
    /// </para>
    /// </summary>
    public enum PartFAnnotationType
    {
        [Description("Undefined")] Undefined,

        /// <summary>The rate label beside a terminal marker, e.g. "SUP 63.0 l/s".</summary>
        [Description("Terminal")] Terminal,

        /// <summary>The transfer label beside a route, e.g. "TRA 8.0 l/s".</summary>
        [Description("Transfer")] Transfer,

        /// <summary>A space's net airflow label.</summary>
        [Description("Space Net Airflow")] SpaceNetAirflow,

        /// <summary>A door's free-area or undercut requirement label.</summary>
        [Description("Door Requirement")] DoorRequirement,

        /// <summary>
        /// The Ventilation Design overlay's design supply mark - "SUP 150.0 l/s", read from
        /// <c>VentilationTerminal.DesignFlowRate_Lps</c>. Carries no Part F requirement or compliance
        /// value; it exists only so the design overlay's tags can be handed to the same placement adapter
        /// as Part F's.
        /// </summary>
        [Description("Design Supply")] DesignSupply,

        /// <summary>The design overlay's design extract mark. See <see cref="DesignSupply"/>.</summary>
        [Description("Design Extract")] DesignExtract,

        /// <summary>The design overlay's net (supply minus extract) mark. See <see cref="DesignSupply"/>.</summary>
        [Description("Design Net")] DesignNet,

        /// <summary>
        /// The design overlay's transfer mark - "TRA 150.0 l/s" between two rooms, read from the model's
        /// own <c>SpaceAirMovement</c> objects. Distinct from <see cref="Transfer"/>, which keys Approved
        /// Document F's own transfer label: the two are separate labels on the same drawing, reporting
        /// separate authorities, and each has to be able to hold its own manual position.
        /// </summary>
        [Description("Design Transfer")] DesignTransfer,
    }
}
