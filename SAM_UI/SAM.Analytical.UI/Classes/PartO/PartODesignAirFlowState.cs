// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;


namespace SAM.Analytical.UI
{
    /// <summary>
    /// <b>One room and one direction of the design ventilation vector, as it stands at one iteration.</b>
    /// A step's collection of these is the COMPLETE vector - every space and direction this run's equipment
    /// actually serves - and not only the ones some round happened to move.
    /// <para>
    /// <b>Why the run has to record this.</b> A step's evidence used to be its adjustments alone, and an
    /// adjustment exists only where something changed. So a room-direction that a round left alone was
    /// absent from the run entirely, and the airflow history could not print it: for the real Flat 1 the
    /// baseline and the ordinary rounds showed the studio's supply and the bathroom's extract, and the
    /// studio's own 22 l/s extract appeared nowhere - which reads as though it had been removed, when the
    /// ventilation network still carries it and is balanced around it.
    /// </para>
    /// <para>
    /// <b>It is a statement of what EXISTS, not of what changed.</b> Nothing here decides anything: the
    /// design is read from the room's terminals and the requirement from Approved Document F, both at the
    /// authority that owns them. What changed remains the adjustments' business, and the history keeps the
    /// two apart on every row - see <c>PartOOptimisationAirFlowRow</c>.
    /// </para>
    /// <para>
    /// <b>The four authorities stay apart.</b>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>. This
    /// carries the first two, each from its own query; the unit's capacity belongs to the equipment table,
    /// and nothing here is a runtime airflow.
    /// </para>
    /// </summary>
    public class PartODesignAirFlowState
    {
        public PartODesignAirFlowState(Guid spaceGuid, string spaceName, FlowClassification flowClassification, double design_Lps, double requirement_Lps)
        {
            SpaceGuid = spaceGuid;
            SpaceName = spaceName;
            FlowClassification = flowClassification;
            Design_Lps = design_Lps;
            Requirement_Lps = requirement_Lps;
        }

        /// <summary>The room's identity - what a row is matched on, never its name.</summary>
        public Guid SpaceGuid { get; }

        /// <summary>The room, as the history prints it.</summary>
        public string SpaceName { get; }

        /// <summary>Supply or extract.</summary>
        public FlowClassification FlowClassification { get; }

        /// <summary>The room's design airflow in this direction [l/s], summed over its terminals.</summary>
        public double Design_Lps { get; }

        /// <summary>What Approved Document F requires of the room in this direction [l/s].</summary>
        public double Requirement_Lps { get; }

        public override string ToString()
        {
            return string.Format("{0} {1}: {2:0.###} l/s", SpaceName, FlowClassification, Design_Lps);
        }
    }
}
