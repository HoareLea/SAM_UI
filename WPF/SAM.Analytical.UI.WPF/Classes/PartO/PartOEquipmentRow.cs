// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.ComponentModel;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One dwelling's row in the Part O preparation window: what it is designed to move, what product it is
    /// fitted with, what that product can move at most, and - in manual mode - what else it could be.
    /// <para>
    /// <b>These are four different quantities and the row keeps them in four different places.</b>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>. The
    /// design duty is what the dwelling moves; the maximum is a property of the equipment. On the accepted
    /// fixture three dwellings designed at 30/30, 63/63 and 63/63 l/s all select the same 150/150 l/s
    /// product, and the 150 belongs only in <see cref="MaximumSupply_Lps"/> /
    /// <see cref="MaximumExtract_Lps"/> - never in <see cref="DesignSupplyDuty_Lps"/>. Selecting a larger
    /// unit does not make a dwelling move more air, and neither does assigning one.
    /// </para>
    /// <para>
    /// <b>Every value is read from its own authority; none is computed here.</b> The duties come from
    /// <c>Query.AirHandlingUnitDesignDuty</c>, the product from
    /// <c>Query.SelectedVentilationUnitReference</c>, the capacity from the
    /// <c>VentilationUnitCapacityDescriptor</c> that product resolves to. The one derived pair is headroom,
    /// which is stated here exactly as <c>VentilationUnitSelection.SupplyHeadroom_Lps</c> defines it -
    /// <c>Descriptor.MaximumSupplyFlowRate_Lps - SupplyDuty_Lps</c> - over the two values already on this
    /// row, so what is shown as headroom cannot disagree with the capacity and duty shown beside it.
    /// </para>
    /// <para>
    /// <b>Nothing here decides anything.</b> An automatic row reports what
    /// <c>Modify.PreparePartOIteration</c> selected. A manual row is a view onto one
    /// <c>PartOEquipmentAssignment</c>, and every edit is delegated straight to
    /// <c>PartOEquipmentAssignmentSet</c> - which owns validation, suggestions and the single write path.
    /// No selection rule, and no capacity comparison, is implemented here.
    /// </para>
    /// </summary>
    public class PartOEquipmentRow : INotifyPropertyChanged
    {
        private readonly PartOEquipmentAssignment? partOEquipmentAssignment;

        private readonly PartOEquipmentAssignmentSet? partOEquipmentAssignmentSet;

        /// <summary>The capacity an automatic, assignment-free row was built with.</summary>
        private readonly VentilationUnitCapacityDescriptor? descriptor;

        /// <param name="unitName">The air handling unit's name.</param>
        /// <param name="systemName">The ventilation system it supplies, for identifying the dwelling.</param>
        /// <param name="designSupplyDuty_Lps">The dwelling's design supply duty, from SAM.</param>
        /// <param name="designExtractDuty_Lps">The dwelling's design extract duty, from SAM.</param>
        /// <param name="ventilationUnitCapacityDescriptor">
        /// The selected product's capacity record, or null where no product is selected - which is Iteration
        /// 1a's normal state and not a failure.
        /// </param>
        /// <param name="refusal">Why no product was selected, where that is the reason. Null otherwise.</param>
        /// <param name="dwellingName">
        /// The dwelling the unit serves, where it resolves - so an Iteration 1a row says "Flat 1" as the
        /// Iteration 2 row does, rather than the system name. Null falls back to the system or unit name.
        /// </param>
        public PartOEquipmentRow(string unitName, string systemName, double designSupplyDuty_Lps, double designExtractDuty_Lps, VentilationUnitCapacityDescriptor? ventilationUnitCapacityDescriptor, string? refusal = null, string? dwellingName = null)
        {
            UnitName = unitName;
            SystemName = systemName;
            DwellingName = dwellingName;
            DesignSupplyDuty_Lps = designSupplyDuty_Lps;
            DesignExtractDuty_Lps = designExtractDuty_Lps;
            descriptor = ventilationUnitCapacityDescriptor;
            Refusal = refusal;
        }

        /// <summary>
        /// A row over one dwelling's assignment, editable where the set says the engineer is the authority.
        /// <para>
        /// The set is held rather than copied from, so this row cannot drift from it: what the picker offers,
        /// whether the product is outside the pool, and what is suggested are all asked of the set at the
        /// moment they are shown.
        /// </para>
        /// </summary>
        public PartOEquipmentRow(PartOEquipmentAssignment partOEquipmentAssignment, PartOEquipmentAssignmentSet partOEquipmentAssignmentSet, string? refusal = null)
        {
            this.partOEquipmentAssignment = partOEquipmentAssignment;
            this.partOEquipmentAssignmentSet = partOEquipmentAssignmentSet;

            UnitName = partOEquipmentAssignment?.AirHandlingUnitName;
            SystemName = partOEquipmentAssignment?.VentilationSystemName;
            DwellingName = partOEquipmentAssignment?.DwellingName;
            DesignSupplyDuty_Lps = partOEquipmentAssignment?.DesignSupplyDuty_Lps ?? double.NaN;
            DesignExtractDuty_Lps = partOEquipmentAssignment?.DesignExtractDuty_Lps ?? double.NaN;
            descriptor = partOEquipmentAssignment?.Descriptor;
            Refusal = refusal;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Which air handling unit this row reports on, by identity.
        /// <para>
        /// Exposed so that a bulk assignment can name the dwellings a grid selection covers. Names cannot
        /// do that job: two dwellings may legitimately be called the same thing, and
        /// <see cref="Dwelling"/> falls back to a system or a unit name where nothing resolves.
        /// </para>
        /// </summary>
        public System.Guid Guid_AirHandlingUnit => partOEquipmentAssignment?.Guid_AirHandlingUnit ?? System.Guid.Empty;

        /// <summary>The air handling unit this row reports on.</summary>
        public string? UnitName { get; }

        /// <summary>The ventilation system it supplies.</summary>
        public string? SystemName { get; }

        /// <summary>
        /// What to call this dwelling. Falls back to the system, then the unit - a row always has something
        /// to be identified by, and none of the three is ever invented.
        /// </summary>
        public string? DwellingName { get; }

        /// <summary>What this row is called in the dwelling column.</summary>
        public string? Dwelling => string.IsNullOrWhiteSpace(DwellingName) ? (string.IsNullOrWhiteSpace(SystemName) ? UnitName : SystemName) : DwellingName;

        /// <summary>The dwelling's design supply duty [l/s]. Never the equipment's capability.</summary>
        public double DesignSupplyDuty_Lps { get; }

        /// <summary>The dwelling's design extract duty [l/s]. Never the equipment's capability.</summary>
        public double DesignExtractDuty_Lps { get; }

        /// <summary>What the fitted product can move. Re-read from the assignment where there is one.</summary>
        public VentilationUnitCapacityDescriptor? Descriptor => partOEquipmentAssignment is null ? descriptor : partOEquipmentAssignment.Descriptor;

        /// <summary>Why no product was selected, where a catalogue was offered and refused.</summary>
        public string? Refusal { get; }

        /// <summary>Whether a product is fitted to this dwelling.</summary>
        public bool HasSelectedProduct => Descriptor?.VentilationUnitReference is not null || (partOEquipmentAssignment?.IsAssigned ?? false);

        /// <summary>
        /// The product, in words. Distinguishes "no catalogue was offered" - Iteration 1a's normal state -
        /// from a catalogue that was offered and could serve nothing.
        /// <para>
        /// Spelled by <c>Query.PartOProductLabel</c>, so a project test product named "test" reads as
        /// <c>test (project test)</c> rather than as the literal identity join <c>Project test test</c>.
        /// Display only - the identity this row reports through <see cref="Descriptor"/> and
        /// <see cref="SelectedCandidate"/> is untouched.
        /// </para>
        /// </summary>
        public string SelectedProduct
        {
            get
            {
                if (partOEquipmentAssignment?.IsAssigned ?? false)
                {
                    return Query.PartOProductLabel(partOEquipmentAssignment.VentilationUnitReference);
                }

                if (Descriptor?.VentilationUnitReference is not null)
                {
                    return Query.PartOProductLabel(Descriptor.VentilationUnitReference);
                }

                return Refusal is null ? "No product selected (no catalogue offered)" : "No product selected";
            }
        }

        /// <summary>The most the fitted product can supply [l/s], or NaN where none is fitted or its rating is unknown.</summary>
        public double MaximumSupply_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumSupplyFlowRate_Lps;

        /// <summary>The most the fitted product can extract [l/s].</summary>
        public double MaximumExtract_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumExtractFlowRate_Lps;

        /// <summary>Capability less design duty [l/s]. Deliberately not taken up as design airflow.</summary>
        public double SupplyHeadroom_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumSupplyFlowRate_Lps - DesignSupplyDuty_Lps;

        /// <summary>Capability less design duty [l/s] on the extract side.</summary>
        public double ExtractHeadroom_Lps => Descriptor is null ? double.NaN : Descriptor.MaximumExtractFlowRate_Lps - DesignExtractDuty_Lps;

        /// <summary>
        /// The outcome, in one word for the Status column.
        /// <para>
        /// The two pre-existing answers are kept exactly: a row with no catalogue behind it is
        /// <c>Not applicable</c> - Iteration 1a, normal - and one whose catalogue could serve nothing is
        /// <c>Refused</c>. Those are different facts from "the fitted product cannot do the job", which is
        /// what an assignment-backed row reports, and collapsing them would tell an engineer to fix a
        /// dwelling that was never being fitted with anything.
        /// </para>
        /// </summary>
        public string SelectionOutcome
        {
            get
            {
                if (partOEquipmentAssignment is not null && Refusal is null)
                {
                    return Core.Query.Description(partOEquipmentAssignment.Status);
                }

                if (HasSelectedProduct)
                {
                    return "Selected";
                }

                return Refusal is null ? "Not applicable" : "Refused";
            }
        }

        /// <summary>Whether the fitted product sits outside the project's currently permitted set. Reported, never corrected.</summary>
        public bool IsOutsideAllowedPool => partOEquipmentAssignment?.IsOutsideAllowedPool ?? false;

        /// <summary>Whether the engineer may change this row's product - true only in manual mode.</summary>
        public bool IsEditable => partOEquipmentAssignmentSet?.IsManual ?? false;

        /// <summary>
        /// Whether a capable alternative was found for a dwelling whose product cannot do the job. A
        /// suggestion to offer, never an action taken.
        /// </summary>
        public bool HasSuggestion => partOEquipmentAssignment?.Suggestion is not null;

        /// <summary>The status and, where there is one, the suggestion - in the words an engineer reads.</summary>
        public string? Description => partOEquipmentAssignment?.Description ?? Refusal;

        /// <summary>
        /// The products this dwelling's picker offers: the permitted pool, plus its own currently assigned
        /// product where that has since left the pool. Asked of the set, which owns the rule.
        /// </summary>
        public List<VentilationUnitCapacityDescriptor> Candidates => partOEquipmentAssignment is null || partOEquipmentAssignmentSet is null
            ? []
            : partOEquipmentAssignmentSet.Candidates(partOEquipmentAssignment.Guid_AirHandlingUnit);

        /// <summary>
        /// The picker's selected item. Setting it assigns that product to this dwelling and to no other -
        /// delegated to <c>PartOEquipmentAssignmentSet.Assign</c>, which validates and records but writes
        /// nothing to the model until the preparation is adopted.
        /// </summary>
        public VentilationUnitCapacityDescriptor? SelectedCandidate
        {
            get
            {
                return partOEquipmentAssignment?.Descriptor;
            }
            set
            {
                if (partOEquipmentAssignment is null || partOEquipmentAssignmentSet is null || value?.VentilationUnitReference is null)
                {
                    return;
                }

                if (!partOEquipmentAssignmentSet.Assign(partOEquipmentAssignment.Guid_AirHandlingUnit, value.VentilationUnitReference, out string _))
                {
                    return;
                }

                Refresh();
            }
        }

        /// <summary>
        /// Takes the suggested product for this dwelling. Called from an explicit control, so that accepting
        /// a suggestion is something the engineer does rather than something that happens.
        /// </summary>
        public bool AssignSuggested()
        {
            if (partOEquipmentAssignment is null || partOEquipmentAssignmentSet is null)
            {
                return false;
            }

            if (!partOEquipmentAssignmentSet.AssignSuggested(partOEquipmentAssignment.Guid_AirHandlingUnit, out string _))
            {
                return false;
            }

            Refresh();

            return true;
        }

        /// <summary>
        /// Re-reads every derived column off the assignment. Called after an edit here, and by the window
        /// after a mode or pool change, so the grid states what the set states.
        /// </summary>
        public void Refresh()
        {
            Raise(nameof(SelectedProduct));
            Raise(nameof(SelectedCandidate));
            Raise(nameof(Candidates));
            Raise(nameof(Descriptor));
            Raise(nameof(MaximumSupply_Lps));
            Raise(nameof(MaximumExtract_Lps));
            Raise(nameof(SupplyHeadroom_Lps));
            Raise(nameof(ExtractHeadroom_Lps));
            Raise(nameof(SelectionOutcome));
            Raise(nameof(IsOutsideAllowedPool));
            Raise(nameof(IsEditable));
            Raise(nameof(HasSuggestion));
            Raise(nameof(HasSelectedProduct));
            Raise(nameof(Description));
        }

        public override string ToString()
        {
            return string.Format(
                "{0}: design {1:0.#}/{2:0.#} l/s, {3}, maximum {4:0.#}/{5:0.#} l/s, {6}",
                Dwelling,
                DesignSupplyDuty_Lps,
                DesignExtractDuty_Lps,
                SelectedProduct,
                MaximumSupply_Lps,
                MaximumExtract_Lps,
                SelectionOutcome);
        }

        private void Raise(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
