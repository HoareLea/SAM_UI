// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What one air handling unit had to carry at one iteration, beside what the product selected for it is
    /// rated to move.
    /// <para>
    /// <b>Duty and maximum are different quantities and are never merged.</b> A unit rated 150/150 l/s
    /// serving a dwelling designed at 73/73 has a duty of 73 and 77 l/s of headroom on each side. The
    /// maximum is a ceiling the optimisation stops at; it is never a design airflow, never a room's target,
    /// and the headroom is reported rather than spent.
    /// </para>
    /// </summary>
    public class PartOOptimisationUnitState
    {
        public PartOOptimisationUnitState(string airHandlingUnitName, string ventilationSystemName, double supplyDuty_Lps, double extractDuty_Lps, VentilationUnitReference ventilationUnitReference, VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor, Enums.VentilationUnitSelectionOutcome ventilationUnitSelectionOutcome, string reason)
        {
            AirHandlingUnitName = airHandlingUnitName;
            VentilationSystemName = ventilationSystemName;
            SupplyDuty_Lps = supplyDuty_Lps;
            ExtractDuty_Lps = extractDuty_Lps;
            VentilationUnitReference = ventilationUnitReference;
            VentilationUnitCapacityDescriptor = ventilationUnitCapacityDescriptor;
            VentilationUnitSelectionOutcome = ventilationUnitSelectionOutcome;
            Reason = reason;
        }

        /// <summary>The unit instance.</summary>
        public string AirHandlingUnitName { get; }

        /// <summary>The dwelling system it serves, where one was resolved for this row.</summary>
        public string VentilationSystemName { get; }

        /// <summary>What it has to move on the supply side at this iteration's design [l/s].</summary>
        public double SupplyDuty_Lps { get; }

        /// <summary>And on the extract side [l/s].</summary>
        public double ExtractDuty_Lps { get; }

        /// <summary>The product it is selected as. <b>The same one for the whole optimisation</b> - 2B never reselects.</summary>
        public VentilationUnitReference VentilationUnitReference { get; }

        /// <summary>What that product is rated to move, where the run's catalogue describes it.</summary>
        public VentilationUnitCapacityDescriptor VentilationUnitCapacityDescriptor { get; }

        /// <summary>What the round found about the selected unit: kept, refused, or not applicable.</summary>
        public Enums.VentilationUnitSelectionOutcome VentilationUnitSelectionOutcome { get; }

        /// <summary>Why it was refused, where it was.</summary>
        public string Reason { get; }

        /// <summary>The supply rating less the duty [l/s]. NaN where the capacity is not known.</summary>
        public double SupplyHeadroom_Lps => VentilationUnitCapacityDescriptor is null ? double.NaN : VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps - SupplyDuty_Lps;

        /// <summary>The extract rating less the duty [l/s]. NaN where the capacity is not known.</summary>
        public double ExtractHeadroom_Lps => VentilationUnitCapacityDescriptor is null ? double.NaN : VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps - ExtractDuty_Lps;

        public override string ToString()
        {
            return string.Format(
                "{0}: duty {1:0.#}/{2:0.#}, maximum {3:0.#}/{4:0.#}, headroom {5:0.#}/{6:0.#} l/s, {7}",
                AirHandlingUnitName,
                SupplyDuty_Lps,
                ExtractDuty_Lps,
                VentilationUnitCapacityDescriptor?.MaximumSupplyFlowRate_Lps ?? double.NaN,
                VentilationUnitCapacityDescriptor?.MaximumExtractFlowRate_Lps ?? double.NaN,
                SupplyHeadroom_Lps,
                ExtractHeadroom_Lps,
                VentilationUnitReference is null ? "no product selected" : VentilationUnitReference.ToString());
        }
    }
}
