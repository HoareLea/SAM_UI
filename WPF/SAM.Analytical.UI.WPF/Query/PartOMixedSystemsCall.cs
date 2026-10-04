// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What the ONE SAM_Systems call of a mixed Systems-route run is handed: the rooms of every MVHR dwelling, and the
    /// manufacturer-guidance settings of the cooled dwellings' units only - or why it cannot be composed.
    /// </summary>
    internal sealed class PartOMixedSystemsCall
    {
        /// <summary>Every room of every MVHR dwelling SAM materialised, cooled or not. No natural dwelling, no common space.</summary>
        public List<Space> Spaces { get; } = [];

        /// <summary>Each cooled dwelling's unit (by the materialised unit's guid) → its guidance settings. Only cooled units.</summary>
        public Dictionary<Guid, MechanicalVentilationGuidanceSettings> GuidanceSettings { get; } = [];

        /// <summary>Why the call cannot be made; empty where it can.</summary>
        public List<string> Refusals { get; } = [];

        public bool IsValid => Refusals.Count == 0 && Spaces.Count != 0 && GuidanceSettings.Count != 0;
    }

    public static partial class Query
    {
        /// <summary>
        /// Composes the mixed SAM_Systems call from SAM's own materialisation record - exactly what the PR3B licensed gate
        /// proved (SAM PR3B record §4).
        ///
        /// <para><b>Nothing is decided here</b></para>
        /// <list type="bullet">
        /// <item>The scope is the MVHR dwellings SAM materialised (<see cref="PartOMaterialisationRecord.VentilationSystemGuids"/>):
        /// natural dwellings and common spaces stay outside it and free-run.</item>
        /// <item>The cooled units are SAM's <see cref="PartOMaterialisationRecord.CooledDwellings"/>; each one's settings are
        /// SAM_Systems' <c>MechanicalVentilationGuidanceSettings(template, designSupply, designExtract)</c>, which applies
        /// SAM's cooling operating airflow rule to the design duty SAM recorded - no airflow, range or capacity is read or
        /// restated in this assembly.</item>
        /// <item>The template is SAM's one match for the product (<c>Analytical.Query.PartOCoolingTemplate</c>): ambiguous or
        /// missing refuses, never guessed.</item>
        /// </list>
        /// </summary>
        internal static PartOMixedSystemsCall PartOMixedSystemsCall(PartOMaterialisation partOMaterialisation, IEnumerable<VentilationUnitTemplate>? ventilationUnitTemplates)
        {
            PartOMixedSystemsCall result = new();

            PartOMaterialisationRecord? partOMaterialisationRecord = partOMaterialisation?.Record;
            AdjacencyCluster? adjacencyCluster = partOMaterialisation?.AnalyticalModel?.AdjacencyCluster;

            if (partOMaterialisation is null || !partOMaterialisation.IsMaterialised || partOMaterialisationRecord is null || adjacencyCluster is null)
            {
                result.Refusals.Add("There is no materialised mixed model to build the TAS Systems ventilation from.");
                return result;
            }

            if (partOMaterialisation.Route != PartOSimulationRoute.Systems || partOMaterialisationRecord.CooledDwellings.Count == 0)
            {
                result.Refusals.Add("SAM's record puts this model on the IZAM route (no dwelling is cooled), so it has no TAS Systems ventilation to build.");
                return result;
            }

            foreach (Guid guid_Zone in partOMaterialisationRecord.VentilationSystemGuids.Keys)
            {
                Zone? zone = adjacencyCluster.GetObject<Zone>(guid_Zone);
                List<Space>? spaces = zone is null ? null : adjacencyCluster.GetRelatedObjects<Space>(zone);
                if (spaces is null || spaces.Count == 0)
                {
                    result.Refusals.Add(string.Format("MVHR dwelling {0} named by SAM's record has no rooms in the materialised model.", zone?.Name ?? guid_Zone.ToString()));
                    continue;
                }

                result.Spaces.AddRange(spaces);
            }

            foreach (PartOCooledDwelling partOCooledDwelling in partOMaterialisationRecord.CooledDwellings)
            {
                string name = adjacencyCluster.GetObject<Zone>(partOCooledDwelling.ZoneGuid)?.Name ?? partOCooledDwelling.ZoneGuid.ToString();

                if (!partOMaterialisationRecord.VentilationSystemGuids.ContainsKey(partOCooledDwelling.ZoneGuid))
                {
                    result.Refusals.Add(string.Format("Cooled dwelling {0} is not one of the MVHR dwellings SAM materialised.", name));
                    continue;
                }

                if (adjacencyCluster.GetObject<AirHandlingUnit>(partOCooledDwelling.AirHandlingUnitGuid) is null)
                {
                    result.Refusals.Add(string.Format("The unit SAM recorded for cooled dwelling {0} is not in the materialised model.", name));
                    continue;
                }

                VentilationUnitTemplate? ventilationUnitTemplate = Analytical.Query.PartOCoolingTemplate(ventilationUnitTemplates, partOCooledDwelling.VentilationUnitReference);
                if (ventilationUnitTemplate is null)
                {
                    result.Refusals.Add(string.Format("The manufacturer guidance of {0}, selected for cooled dwelling {1}, is not in the catalogue offered to this run (or is listed more than once).", partOCooledDwelling.VentilationUnitReference, name));
                    continue;
                }

                MechanicalVentilationGuidanceSettings mechanicalVentilationGuidanceSettings = ventilationUnitTemplate.MechanicalVentilationGuidanceSettings(partOCooledDwelling.DesignSupply_Lps, partOCooledDwelling.DesignExtract_Lps, out string refusal);
                if (mechanicalVentilationGuidanceSettings is null)
                {
                    result.Refusals.Add(string.Format("Cooled dwelling {0}: {1}", name, refusal ?? "its unit's manufacturer guidance could not be resolved."));
                    continue;
                }

                Zone? zone_Cooled = adjacencyCluster.GetObject<Zone>(partOCooledDwelling.ZoneGuid);
                List<Space>? spaces_Cooled = zone_Cooled is null ? null : adjacencyCluster.GetRelatedObjects<Space>(zone_Cooled);
                if (partOCooledDwelling.CoolingStatSpaceGuid == Guid.Empty || spaces_Cooled is null || !spaces_Cooled.Exists(x => x.Guid == partOCooledDwelling.CoolingStatSpaceGuid))
                {
                    result.Refusals.Add(string.Format("Cooled dwelling {0} has no valid selected cooling control room in its materialised spaces.", name));
                    continue;
                }

                mechanicalVentilationGuidanceSettings.CoolingStatSpaceGuid = partOCooledDwelling.CoolingStatSpaceGuid;

                result.GuidanceSettings[partOCooledDwelling.AirHandlingUnitGuid] = mechanicalVentilationGuidanceSettings;
            }

            return result;
        }

        /// <summary>
        /// Which of the materialised mixed model's ventilation systems its ONE SAM_Systems call is handed: SAM's
        /// <c>Analytical.Query.PartOSystemsMaterialisationScope</c> - the rule Iteration 3 uses - over the systems SAM's
        /// record says it built (<see cref="PartOMaterialisationRecord.VentilationSystemGuids"/>), never over names.
        /// <para>
        /// The dwelling rooms are those of every assessed dwelling (<see cref="PartOMaterialisationRecord.ZoneGuids_Assessed"/>,
        /// natural ones included), which only words a refusal. The materialised model is read, never changed; the scope's
        /// working copy is what SAM_Systems sees, and the thermal model keeps every authored system.
        /// </para>
        /// </summary>
        internal static PartOSystemsMaterialisationScope PartOMixedSystemsScope(PartOMaterialisation partOMaterialisation)
        {
            PartOMaterialisationRecord? partOMaterialisationRecord = partOMaterialisation?.Record;
            AdjacencyCluster? adjacencyCluster = partOMaterialisation?.AnalyticalModel?.AdjacencyCluster;

            List<Guid> guids_Space_Dwelling = [];
            if (adjacencyCluster is not null)
            {
                foreach (Guid guid_Zone in partOMaterialisationRecord?.ZoneGuids_Assessed ?? [])
                {
                    Zone? zone = adjacencyCluster.GetObject<Zone>(guid_Zone);
                    foreach (Space space in (zone is null ? null : adjacencyCluster.GetRelatedObjects<Space>(zone)) ?? [])
                    {
                        guids_Space_Dwelling.Add(space.Guid);
                    }
                }
            }

            return Analytical.Query.PartOSystemsMaterialisationScope(adjacencyCluster, partOMaterialisationRecord?.VentilationSystemGuids.Values, guids_Space_Dwelling);
        }
    }
}
