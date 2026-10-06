// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What one <see cref="DesignAirFlowOverlayMark"/> reports: a space's aggregated design supply, its
    /// aggregated design extract, or the net of the two. Distinct from Approved Document F's own
    /// <c>PartFTerminalRole</c> - this is the DESIGN airflow authority
    /// (<c>VentilationTerminal.DesignFlowRate_Lps</c>), not a regulatory requirement.
    /// </summary>
    public enum DesignAirFlowMarkType
    {
        Supply,
        Extract,

        /// <summary>Supply minus extract. Positive means the space is net supplied; negative, net extracted.</summary>
        Net,

        /// <summary>
        /// Design air moving between two spaces through the dwelling's own internal openings, read from
        /// the <c>SpaceAirMovement</c> objects <c>Modify.AddPartFTransferAirMovements</c> writes on every
        /// Approved Document O round and exports to TAS as an inter-zone air movement.
        /// <para>
        /// <b>Not the same figure as Approved Document F's own transfer requirement</b>, which
        /// <c>PartFFloorPlanOverlay</c> draws from <c>PartFDoorTransferData</c>. Approved Document F's is
        /// sized once from Table 1.2 and does not move when an optimisation round raises a room's design
        /// airflow; this one is re-solved against the new design duty every round, over the same
        /// <c>PartFAirflowNetwork</c>. The two can disagree, and where they do it is because they are
        /// answering different questions - see <c>Query.DesignTransferFlowRate_Lps</c>.
        /// </para>
        /// </summary>
        Transfer,
    }
}
