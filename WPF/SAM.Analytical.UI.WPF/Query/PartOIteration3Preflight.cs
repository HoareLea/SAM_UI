// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// The Iteration 3 pre-flight: which ventilation units one method would change, to what, and - for a
        /// product method - whether every one of them can run, answered from the prepared design and the
        /// catalogue with <b>no TAS work at all</b>.
        ///
        /// <para><b>The run's own steps, not a second opinion</b></para>
        /// <para>
        /// It asks exactly what <c>Modify.RunPartOIteration3</c> asks before its first TAS call - the
        /// eligibility authority, the system scope, and the same resolution query for the method
        /// (<see cref="PartOIteration3EquipmentResolution"/>, <see cref="PartOIteration3CoolingResolution"/>,
        /// <see cref="PartOIteration3GuidanceResolution(AdjacencyCluster, IEnumerable{Zone}, PartODwellingStrategySet, VentilationUnitCatalogue, out Dictionary{Guid, MechanicalVentilationGuidanceSettings}, out List{string})"/>)
        /// over the same inputs. So a refusal shown here is the refusal the run would record at Equipment
        /// resolution, only minutes of TAS earlier; and a pre-flight that passes promises nothing about TAS
        /// itself, which is still ahead of it.
        /// </para>
        /// <para>
        /// <b>Cost.</b> One walk over the dwelling scope and one resolution pass over the units; nothing reads
        /// a results file. Asked once per method per showing of the hub, never per keystroke.
        /// </para>
        /// </summary>
        public static PartOIteration3Preflight PartOIteration3Preflight(PartORun partORun, PartOIteration3Eligibility partOIteration3Eligibility, PartOIteration3BehaviourMode partOIteration3BehaviourMode, VentilationUnitCatalogue ventilationUnitCatalogue)
        {
            if (partORun is null)
            {
                return new PartOIteration3Preflight(partOIteration3BehaviourMode, null, ["There is no completed Part O run to use as the reference case."]);
            }

            if (partOIteration3Eligibility is not null && !partOIteration3Eligibility.CanRun)
            {
                return new PartOIteration3Preflight(partOIteration3BehaviourMode, null, [partOIteration3Eligibility.Refusal_Run]);
            }

            AdjacencyCluster adjacencyCluster = PartOIteration3ScopedCluster(partORun, out List<string> refusals_Scope);

            if (adjacencyCluster is null)
            {
                return new PartOIteration3Preflight(partOIteration3BehaviourMode, null, refusals_Scope);
            }

            List<string> refusals = [];

            switch (partOIteration3BehaviourMode)
            {
                case PartOIteration3BehaviourMode.SelectedProduct:
                    refusals = PartOIteration3EquipmentResolution(adjacencyCluster, ventilationUnitCatalogue, out Dictionary<Guid, MechanicalVentilationUnitSettings> _, out List<PartOIteration3EquipmentEvidence> _, out List<string> _);
                    break;

                case PartOIteration3BehaviourMode.SelectedProductCooling:
                    refusals = PartOIteration3CoolingResolution(adjacencyCluster, ventilationUnitCatalogue, out Dictionary<Guid, MechanicalVentilationCoolingSettings> _, out List<PartOIteration3CoolingEvidence> _, out List<string> _);
                    break;

                case PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance:
                    refusals = PartOIteration3GuidanceResolution(adjacencyCluster, partORun.PreparationContext.Zones,
                        partORun.AnalyticalModel_Prepared.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies),
                        ventilationUnitCatalogue, out Dictionary<Guid, MechanicalVentilationGuidanceSettings> _, out List<string> _);
                    break;
            }

            //The units, as the resolutions enumerate them: every air handling unit a retained ventilation
            //system names.
            List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetObjects<AirHandlingUnit>() ?? [];
            airHandlingUnits.RemoveAll(x => x is null || adjacencyCluster.VentilationSystems(x).Count == 0);
            airHandlingUnits.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

            List<VentilationUnitTemplate> ventilationUnitTemplates = ventilationUnitCatalogue?.Templates ?? [];

            //Each resolution refusal that names a unit is attached to that unit; the rest stay general.
            //Matched on the quoted name, which is how every resolution names a unit.
            List<string> refusals_General = [.. refusals];
            List<PartOIteration3PreflightUnit> units = [];

            foreach (AirHandlingUnit airHandlingUnit in airHandlingUnits)
            {
                string product;

                if (!IsPartOIteration3ProductMethod(partOIteration3BehaviourMode))
                {
                    product = "Design airflows, no product";
                }
                else
                {
                    VentilationUnitReference ventilationUnitReference = Analytical.Query.SelectedVentilationUnitReference(airHandlingUnit);

                    if (ventilationUnitReference is null || !ventilationUnitReference.IsValid)
                    {
                        product = "No product selected";
                    }
                    else
                    {
                        VentilationUnitTemplate ventilationUnitTemplate = airHandlingUnit.SelectedVentilationUnitTemplate(ventilationUnitTemplates);

                        //The cooling module only where the method uses it.
                        product = partOIteration3BehaviourMode == PartOIteration3BehaviourMode.SelectedProduct
                            ? ventilationUnitReference.ToString()
                            : PartOIteration3ProductText(ventilationUnitReference.ToString(), ventilationUnitTemplate?.CoolingModuleModel);
                    }
                }

                string quoted = string.Format("'{0}'", airHandlingUnit.Name);
                string refusal = refusals.Find(x => x is not null && x.Contains(quoted));

                if (refusal is not null)
                {
                    refusals_General.RemoveAll(x => x is not null && x.Contains(quoted));
                }

                units.Add(new PartOIteration3PreflightUnit(airHandlingUnit.Name, product, refusal));
            }

            //Every unit refusal still stops the run, so it stays in the refusal list - attached to its unit
            //for display, and counted once.
            List<string> refusals_All = [.. refusals_General];
            foreach (PartOIteration3PreflightUnit partOIteration3PreflightUnit in units)
            {
                if (!partOIteration3PreflightUnit.IsReady)
                {
                    refusals_All.Add(partOIteration3PreflightUnit.Refusal);
                }
            }

            return new PartOIteration3Preflight(partOIteration3BehaviourMode, units, refusals_All);
        }

        /// <summary>
        /// The prepared design narrowed to the ventilation under assessment - what the run's System scope
        /// stage hands to its resolution. Null, with the reasons, where there is none.
        /// </summary>
        internal static AdjacencyCluster PartOIteration3ScopedCluster(PartORun partORun, out List<string> refusals)
        {
            refusals = [];

            AdjacencyCluster adjacencyCluster_Prepared = partORun?.AnalyticalModel_Prepared?.AdjacencyCluster;

            if (adjacencyCluster_Prepared is null || partORun.PreparationContext is null)
            {
                refusals.Add("The reference case holds no prepared design, so there are no ventilation units to change.");

                return null;
            }

            Dictionary<Guid, PartOIteration3Room> dictionary_Room = PartOIteration3Rooms(adjacencyCluster_Prepared, partORun.PreparationContext.Zones);

            List<Guid> guids_Space_Dwelling = [.. dictionary_Room.Keys];
            guids_Space_Dwelling.Sort();

            PartOIteration3SystemScope partOIteration3SystemScope = PartOIteration3SystemScope(adjacencyCluster_Prepared, partORun.Guids_VentilationSystem_Prepared, guids_Space_Dwelling);

            if (!partOIteration3SystemScope.IsScoped)
            {
                refusals.AddRange(partOIteration3SystemScope.Refusals);

                return null;
            }

            return partOIteration3SystemScope.AdjacencyCluster;
        }

        /// <summary>
        /// A manufacturer-guidance result's guidance as fields, for the result window.
        /// <list type="bullet">
        /// <item>Where the record carries them - every result produced since they were recorded - those.</item>
        /// <item>For a result recorded before, re-read from the catalogue by the same resolution the run used,
        /// but ONLY where the catalogue is byte-for-byte the one the result was produced with (its SHA-256 is
        /// on the record) and the prepared design is at hand. Otherwise none, and the window shows the
        /// recorded notes instead - a changed catalogue must never be presented as what was run.</item>
        /// </list>
        /// </summary>
        /// <param name="source">What the fields are, in one sentence, or why there are none.</param>
        public static List<PartOIteration3GuidanceEvidence> PartOIteration3GuidanceEvidence(PartORun partORun, PartOIteration3Record partOIteration3Record, VentilationUnitCatalogue ventilationUnitCatalogue, out string source)
        {
            source = null;

            if (partOIteration3Record is null || partOIteration3Record.BehaviourMode != PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance)
            {
                return [];
            }

            if (partOIteration3Record.Guidance.Count != 0)
            {
                source = "As resolved and recorded when this result was produced.";

                return partOIteration3Record.Guidance;
            }

            if (ventilationUnitCatalogue is null
                || string.IsNullOrWhiteSpace(partOIteration3Record.Sha256_VentilationUnitCatalogue)
                || !string.Equals(ventilationUnitCatalogue.Sha256, partOIteration3Record.Sha256_VentilationUnitCatalogue, StringComparison.OrdinalIgnoreCase))
            {
                source = "The ventilation unit catalogue has changed since this result was produced, so the guidance is shown only as recorded, under Technical details.";

                return [];
            }

            AdjacencyCluster adjacencyCluster = PartOIteration3ScopedCluster(partORun, out List<string> _);

            if (adjacencyCluster is null)
            {
                source = "The prepared design is not available, so the guidance is shown only as recorded, under Technical details.";

                return [];
            }

            List<string> refusals = PartOIteration3GuidanceResolution(adjacencyCluster, partORun.PreparationContext.Zones,
                partORun.AnalyticalModel_Prepared.GetValue<PartODwellingStrategySet>(Analytical.AnalyticalModelParameter.PartODwellingStrategies),
                ventilationUnitCatalogue, out Dictionary<Guid, MechanicalVentilationGuidanceSettings> _, out List<string> _, out List<PartOIteration3GuidanceEvidence> evidence);

            if (refusals.Count != 0)
            {
                source = "The guidance could not be re-read for this design, so it is shown only as recorded, under Technical details.";

                return [];
            }

            source = "Re-read from the ventilation unit catalogue, which is unchanged since this result was produced.";

            return evidence;
        }
    }
}
