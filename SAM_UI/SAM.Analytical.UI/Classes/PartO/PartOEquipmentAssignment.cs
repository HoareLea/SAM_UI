// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One dwelling's equipment assignment, as an engineer reads it: the duty it was designed at, the
    /// product fitted to it, what that product can move, and whether the two agree.
    ///
    /// <para><b>A captured statement, not a live view of the model</b></para>
    /// <para>
    /// The design duties are captured as numbers when the row is built and never re-derived. That is what
    /// makes the whole assignment table cheap enough to re-evaluate on every keystroke - no
    /// <c>GetSpaces</c>, <c>GetZones</c> or <c>GetObjects</c> rescan happens per row, per edit or per pool
    /// change. It also means a row is a statement about the design as prepared, which is exactly the design
    /// an assignment is being judged against.
    /// </para>
    ///
    /// <para><b>Four quantities, kept apart</b></para>
    /// <para>
    /// <see cref="DesignSupplyDuty_Lps"/> is what this dwelling is designed to move;
    /// <see cref="MaximumSupply_Lps"/> is what the fitted box could move. They are different facts, they
    /// live in different places - the model and the catalogue - and this row never lets one become the
    /// other. Headroom is the gap between them and is deliberately <b>not</b> taken up.
    /// </para>
    ///
    /// <para><b>Nothing here writes anything</b></para>
    /// <para>
    /// Including <see cref="Suggestion"/>. A suggestion is a value on a row that an engineer may act on; it
    /// is never applied by being calculated. <c>PartOEquipmentAssignmentSet</c> owns every change, and only
    /// its <c>Commit</c> touches a model.
    /// </para>
    /// </summary>
    public class PartOEquipmentAssignment
    {
        private readonly VentilationUnitReference ventilationUnitReference_Prepared;

        /// <param name="guid_AirHandlingUnit">The unit's identity, which is how a change is committed back.</param>
        /// <param name="airHandlingUnitName">The unit's name. Display only - names are not identities.</param>
        /// <param name="ventilationSystemName">The system it supplies. Display only.</param>
        /// <param name="dwellingName">
        /// What to call this dwelling. Supplied by the caller, which already knows the run's zones, so that
        /// this row never has to reach into a model to name itself.
        /// </param>
        /// <param name="designSupplyDuty_Lps">The dwelling's design supply duty, captured.</param>
        /// <param name="designExtractDuty_Lps">The dwelling's design extract duty, captured.</param>
        /// <param name="ventilationUnitReference">
        /// The product the model currently says this unit is - however it got there. Null where nothing is
        /// assigned, which is a state to be shown and never one to be filled in.
        /// </param>
        public PartOEquipmentAssignment(Guid guid_AirHandlingUnit, string airHandlingUnitName, string ventilationSystemName, string dwellingName, double designSupplyDuty_Lps, double designExtractDuty_Lps, VentilationUnitReference ventilationUnitReference)
        {
            Guid_AirHandlingUnit = guid_AirHandlingUnit;
            AirHandlingUnitName = airHandlingUnitName;
            VentilationSystemName = ventilationSystemName;
            DwellingName = dwellingName;
            DesignSupplyDuty_Lps = designSupplyDuty_Lps;
            DesignExtractDuty_Lps = designExtractDuty_Lps;

            ventilationUnitReference_Prepared = ventilationUnitReference is null ? null : new VentilationUnitReference(ventilationUnitReference);

            VentilationUnitReference = ventilationUnitReference_Prepared is null ? null : new VentilationUnitReference(ventilationUnitReference_Prepared);
        }

        /// <summary>
        /// The same, where the caller already knows what the assigned product can move - the automatic
        /// read-only path, which has just selected it and holds the descriptor in hand.
        /// <para>
        /// A row built this way and then placed in a <see cref="PartOEquipmentAssignmentSet"/> has its
        /// capability re-resolved from that set's catalogue, which is the authority whenever there is one.
        /// </para>
        /// </summary>
        public PartOEquipmentAssignment(Guid guid_AirHandlingUnit, string airHandlingUnitName, string ventilationSystemName, string dwellingName, double designSupplyDuty_Lps, double designExtractDuty_Lps, VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor)
            : this(guid_AirHandlingUnit, airHandlingUnitName, ventilationSystemName, dwellingName, designSupplyDuty_Lps, designExtractDuty_Lps, ventilationUnitCapacityDescriptor?.VentilationUnitReference)
        {
            Descriptor = ventilationUnitCapacityDescriptor;
        }

        /// <summary>The air handling unit this row is about, by identity.</summary>
        public Guid Guid_AirHandlingUnit { get; }

        /// <summary>The unit's name. Display only.</summary>
        public string AirHandlingUnitName { get; }

        /// <summary>The ventilation system it supplies. Display only.</summary>
        public string VentilationSystemName { get; }

        /// <summary>What to call this dwelling. Display only.</summary>
        public string DwellingName { get; }

        /// <summary>The dwelling's design supply duty [l/s]. Never the equipment's capability.</summary>
        public double DesignSupplyDuty_Lps { get; }

        /// <summary>The dwelling's design extract duty [l/s]. Never the equipment's capability.</summary>
        public double DesignExtractDuty_Lps { get; }

        /// <summary>
        /// The product currently assigned in this row - the pending answer while the engineer is editing,
        /// and the committed one afterwards. Null where nothing is assigned.
        /// </summary>
        public VentilationUnitReference VentilationUnitReference { get; internal set; }

        /// <summary>
        /// What <see cref="VentilationUnitReference"/> can move, looked up in the catalogue. Null where
        /// nothing is assigned, where the catalogue does not hold the identity, or where it gives that
        /// identity two different capacities - all of which are "unknown", and none of which is a pass.
        /// </summary>
        public VentilationUnitCapacityDescriptor Descriptor { get; internal set; }

        /// <summary>
        /// Whether the assigned product sits outside the project's currently permitted set.
        /// <para>
        /// <b>Reported, never corrected.</b> An authored assignment is not deleted or replaced because the
        /// pool changed afterwards; the engineer is told and decides. Kept separate from
        /// <see cref="Status"/> because a procurement change is not an engineering failure.
        /// </para>
        /// </summary>
        public bool IsOutsideAllowedPool { get; internal set; }

        /// <summary>
        /// The smallest permitted product that <i>could</i> move this dwelling's duty, where the assigned
        /// one cannot. A value to offer, not an action taken - see the class remarks.
        /// </summary>
        public VentilationUnitCapacityDescriptor Suggestion { get; internal set; }

        /// <summary>Whether a product is assigned at all.</summary>
        public bool IsAssigned => VentilationUnitReference is not null && VentilationUnitReference.IsValid;

        /// <summary>
        /// Whether this row's product differs from the one the model carried when the row was built. Only
        /// these rows are written on commit, so converting authority - which changes no identity - writes
        /// nothing at all.
        /// </summary>
        public bool HasChanged => VentilationUnitReference.Compare(ventilationUnitReference_Prepared, VentilationUnitReference) != 0;

        /// <summary>The product the model carried when this row was built, for reporting a change.</summary>
        public VentilationUnitReference VentilationUnitReference_Prepared => ventilationUnitReference_Prepared is null ? null : new VentilationUnitReference(ventilationUnitReference_Prepared);

        /// <summary>What the assigned product can move on the supply side [l/s], or NaN where unknown.</summary>
        public double MaximumSupply_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumSupplyFlowRate_Lps;

        /// <summary>What the assigned product can move on the extract side [l/s], or NaN where unknown.</summary>
        public double MaximumExtract_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumExtractFlowRate_Lps;

        /// <summary>
        /// Capability less design duty [l/s]. Deliberately not taken up as design airflow - it is what the
        /// fitted box has left, and Iteration 2B is the only thing allowed to spend any of it.
        /// </summary>
        public double SupplyHeadroom_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumSupplyFlowRate_Lps - DesignSupplyDuty_Lps;

        /// <summary>Capability less design duty [l/s] on the extract side.</summary>
        public double ExtractHeadroom_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumExtractFlowRate_Lps - DesignExtractDuty_Lps;

        /// <summary>
        /// Whether the assigned product can move this dwelling's design duty. See
        /// <see cref="PartOEquipmentAssignmentStatus"/> - each value is a different thing to do next.
        /// </summary>
        public PartOEquipmentAssignmentStatus Status
        {
            get
            {
                if (!IsAssigned)
                {
                    return PartOEquipmentAssignmentStatus.NotAssigned;
                }

                if (Descriptor is null)
                {
                    return PartOEquipmentAssignmentStatus.CapacityUnknown;
                }

                //NO duty is not a duty of zero. A unit whose systems or terminals were removed derives 0/0,
                //which every capacity satisfies, and calling that OK would say the plant is fine for a
                //dwelling that currently moves no air. Analytical.Query.IsVentilationUnitSufficient refuses
                //the same case for the same reason, and this has to agree with it.
                if (!IsUsable(DesignSupplyDuty_Lps) || !IsUsable(DesignExtractDuty_Lps))
                {
                    return PartOEquipmentAssignmentStatus.CapacityUnknown;
                }

                return Descriptor.IsSufficientFor(DesignSupplyDuty_Lps, DesignExtractDuty_Lps)
                    ? PartOEquipmentAssignmentStatus.Ok
                    : PartOEquipmentAssignmentStatus.Insufficient;
            }
        }

        /// <summary>
        /// The status in words, with the pool note appended where it applies - written so that an engineer
        /// reading one line knows both whether the design works and whether the product is still one the
        /// project permits.
        /// </summary>
        public string Description
        {
            get
            {
                string result;

                switch (Status)
                {
                    case PartOEquipmentAssignmentStatus.NotAssigned:
                        result = "No product is assigned to this dwelling.";
                        break;

                    case PartOEquipmentAssignmentStatus.CapacityUnknown:
                        result = IsUsable(DesignSupplyDuty_Lps) && IsUsable(DesignExtractDuty_Lps)
                            ? string.Format("'{0}' is not among the products the current catalogue states a single capacity for, so what it can move is unknown - which is not the same as adequate.", VentilationUnitReference)
                            : string.Format("This dwelling states no design duty, so there is nothing to check '{0}' against. Adequacy is unknown rather than met.", VentilationUnitReference);
                        break;

                    case PartOEquipmentAssignmentStatus.Insufficient:
                        result = string.Format(
                            "'{0}' can move {1:0.#} l/s supply and {2:0.#} l/s extract, against a design duty of {3:0.#} and {4:0.#} l/s. The assignment stands and the design airflow is unchanged - an undersized unit is reported, never accommodated.",
                            VentilationUnitReference,
                            MaximumSupply_Lps,
                            MaximumExtract_Lps,
                            DesignSupplyDuty_Lps,
                            DesignExtractDuty_Lps);

                        if (Suggestion is not null)
                        {
                            result = string.Format(
                                "{0} '{1}' ({2:0.#} / {3:0.#} l/s) could meet it.",
                                result,
                                Suggestion.VentilationUnitReference,
                                Suggestion.MaximumSupplyFlowRate_Lps,
                                Suggestion.MaximumExtractFlowRate_Lps);
                        }

                        break;

                    default:
                        result = string.Format(
                            "'{0}' can move this dwelling's design duty, with {1:0.#} l/s supply and {2:0.#} l/s extract of headroom left unused.",
                            VentilationUnitReference,
                            SupplyHeadroom_Lps,
                            ExtractHeadroom_Lps);
                        break;
                }

                return IsOutsideAllowedPool
                    ? string.Format("{0} Assigned product is outside the current allowed pool.", result)
                    : result;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0}: design {1:0.#}/{2:0.#} l/s, {3}, maximum {4:0.#}/{5:0.#} l/s, {6}",
                string.IsNullOrWhiteSpace(DwellingName) ? AirHandlingUnitName : DwellingName,
                DesignSupplyDuty_Lps,
                DesignExtractDuty_Lps,
                IsAssigned ? VentilationUnitReference.ToString() : "no product assigned",
                MaximumSupply_Lps,
                MaximumExtract_Lps,
                Core.Query.Description(Status));
        }

        private static bool IsUsable(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
        }
    }
}
