// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>One ventilation system named in <see cref="PartOSystemsInAssessment"/> - by identity, with the words an engineer sees.</summary>
    public sealed class PartOSystemsInAssessmentEntry
    {
        internal PartOSystemsInAssessmentEntry(Guid guid, string name, string? detail)
        {
            Guid = guid;
            Name = name;
            Detail = detail;
        }

        /// <summary>The system's identity - what membership is decided by. Two systems may share a name; never a guid.</summary>
        public Guid Guid { get; }

        /// <summary>What the engineer calls it: the air handling unit Part O named for a dwelling (<c>MVHR Flat 2</c>), or the system's full name (<c>NV 1</c>).</summary>
        public string Name { get; }

        /// <summary>Where it is, or what it is attached to (the dwelling, the unit it names); null when there is nothing more to say.</summary>
        public string? Detail { get; }

        public string Text => Detail is null ? Name : string.Format("{0} — {1}", Name, Detail);

        public override string ToString() => Text;
    }

    /// <summary>
    /// What a Part O assessment actually assesses of the model's ventilation systems - SAM's decision, shown.
    ///
    /// <para><b>Display only. Nothing here decides</b></para>
    /// <para>
    /// Every identity is read from SAM: the systems Part O built (<see cref="PartOMaterialisationRecord.VentilationSystemGuids"/>),
    /// the systems <c>Analytical.Query.PartOSystemsMaterialisationScope</c> left out of the Systems materialisation input,
    /// and that query's refusals. This assembly adds only the words (the unit and dwelling a system belongs to). No system
    /// is classified by its name, and no duty is read.
    /// </para>
    ///
    /// <para><b>Three outcomes, never blurred</b></para>
    /// <list type="bullet">
    /// <item><see cref="Included"/> - systems Part O built; they are what is assessed.</item>
    /// <item><see cref="Retained"/> - authored systems that stay on the design (and in the thermal model, as authored)
    /// but take no part in the Systems materialisation. <b>Nothing is removed from the design model.</b></item>
    /// <item><see cref="Refusals"/> - SAM's reason the assessment cannot be taken at all. The lists are then empty: a
    /// refused scope names no systems, so none are claimed.</item>
    /// </list>
    ///
    /// <para><b>The same object for Check and Build &amp; Run</b></para>
    /// <para>
    /// Both are built from the one Systems preflight (<c>Modify.PartOMixedSystemsMaterialisation</c>), so they cannot
    /// describe different scopes of the same design.
    /// </para>
    /// </summary>
    public sealed class PartOSystemsInAssessment
    {
        private readonly List<PartOSystemsInAssessmentEntry> included;
        private readonly List<PartOSystemsInAssessmentEntry> retained;
        private readonly List<string> refusals;

        internal PartOSystemsInAssessment(PartOSimulationRoute route, bool scopeApplied, IEnumerable<PartOSystemsInAssessmentEntry>? included, IEnumerable<PartOSystemsInAssessmentEntry>? retained, IEnumerable<string>? refusals)
        {
            Route = route;
            ScopeApplied = scopeApplied;

            this.included = [.. included ?? []];
            this.retained = [.. retained ?? []];
            this.refusals = [.. (refusals ?? []).Where(x => !string.IsNullOrWhiteSpace(x))];
        }

        /// <summary>The route SAM's record put the design on.</summary>
        public PartOSimulationRoute Route { get; }

        /// <summary>
        /// Whether SAM's Systems scope was taken. It is only on the TAS Systems route, the one that hands the systems to
        /// SAM_Systems; on the IZAM route no Systems input exists, so nothing is left out of one.
        /// </summary>
        public bool ScopeApplied { get; }

        public IReadOnlyList<PartOSystemsInAssessmentEntry> Included => included;

        public IReadOnlyList<PartOSystemsInAssessmentEntry> Retained => retained;

        /// <summary>SAM's reasons nothing can be assessed. Empty where the scope was taken (or none applies).</summary>
        public IReadOnlyList<string> Refusals => refusals;

        public bool IsRefused => refusals.Count != 0;

        /// <summary>Nothing included, nothing retained and nothing refused - no ventilation system is involved.</summary>
        public bool IsEmpty => included.Count == 0 && retained.Count == 0 && refusals.Count == 0;

        /// <summary>The header line: the three outcomes in numbers, in the engineer's words.</summary>
        public string Summary
        {
            get
            {
                if (IsRefused)
                {
                    return string.Format("refused — {0}", UI.Query.PartOCount(refusals.Count, "reason", "reasons"));
                }

                if (IsEmpty)
                {
                    return "no ventilation system is involved";
                }

                if (!ScopeApplied)
                {
                    return string.Format("{0} included", included.Count);
                }

                return string.Format("{0} included · {1} retained on the design, not assessed", included.Count, retained.Count);
            }
        }

        /// <summary>One sentence under the lists: what the lists mean, and that nothing was deleted.</summary>
        public string Note
        {
            get
            {
                if (IsRefused)
                {
                    return included.Count == 0 && retained.Count == 0
                        ? "SAM refused this assessment, so it names no included or retained system. Nothing was simulated and nothing was changed on the design."
                        : "This assessment is refused (above), so nothing was simulated. The lists show what SAM's scope decided; nothing was changed on the design.";
                }

                if (IsEmpty)
                {
                    return "The design carries no ventilation system for Part O to include.";
                }

                if (!ScopeApplied)
                {
                    return "This design runs without TAS Systems, so no Systems scope was taken. Part O built the systems listed; the design is unchanged.";
                }

                return retained.Count == 0
                    ? "Every ventilation system on the design is one Part O built."
                    : "Retained systems stay on the design and in the thermal model exactly as authored. They are not assessed here, and nothing has been removed.";
            }
        }
    }

    public static partial class Query
    {
        /// <summary>
        /// The systems a Part O mixed assessment takes, from SAM's own answers - see <see cref="PartOSystemsInAssessment"/>.
        /// </summary>
        /// <param name="partOMaterialisation">SAM's materialisation of the selected design - the model whose systems are named.</param>
        /// <param name="partOSystemsMaterialisationScope">
        /// SAM's scope over it, or null where the design is not on the TAS Systems route (no scope applies).
        /// </param>
        /// <param name="refusal_Preflight">
        /// Why the Systems preflight refused for a reason that is not the scope's (the call could not be composed, SAM_Systems
        /// would not materialise), or null. Worded exactly as Check design and Build &amp; Run word it.
        /// </param>
        internal static PartOSystemsInAssessment PartOSystemsInAssessment(PartOMaterialisation partOMaterialisation, PartOSystemsMaterialisationScope? partOSystemsMaterialisationScope, string? refusal_Preflight = null)
        {
            PartOSimulationRoute partOSimulationRoute = partOMaterialisation?.Route ?? PartOSimulationRoute.Izam;

            //A refused scope names no system - SAM's own rule, kept: the refusals are shown and no list is claimed.
            if (partOSystemsMaterialisationScope is not null && !partOSystemsMaterialisationScope.IsScoped)
            {
                return new PartOSystemsInAssessment(partOSimulationRoute, true, null, null, partOSystemsMaterialisationScope.Refusals.ConvertAll(x => x.Message));
            }

            AdjacencyCluster? adjacencyCluster = partOMaterialisation?.AnalyticalModel?.AdjacencyCluster;
            PartOMaterialisationRecord? partOMaterialisationRecord = partOMaterialisation?.Record;

            if (adjacencyCluster is null || partOMaterialisationRecord is null)
            {
                return new PartOSystemsInAssessment(partOSimulationRoute, partOSystemsMaterialisationScope is not null, null, null, string.IsNullOrWhiteSpace(refusal_Preflight) ? null : [refusal_Preflight!]);
            }

            //One pass over the systems and one over the units, then lookups by identity - linear in the model.
            Dictionary<Guid, VentilationSystem> dictionary_System = [];
            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem is not null)
                {
                    dictionary_System[ventilationSystem.Guid] = ventilationSystem;
                }
            }

            //The dwelling a system Part O built serves - SAM's record, system -> zone.
            Dictionary<Guid, Guid> dictionary_Zone = [];
            foreach (KeyValuePair<Guid, Guid> keyValuePair in partOMaterialisationRecord.VentilationSystemGuids)
            {
                dictionary_Zone[keyValuePair.Value] = keyValuePair.Key;
            }

            //Where a scope was taken it is the authority for what is included; otherwise (IZAM) the record is.
            IEnumerable<Guid> guids_Included = partOSystemsMaterialisationScope is not null ? partOSystemsMaterialisationScope.Guids_Retained : partOMaterialisationRecord.VentilationSystemGuids.Values;

            List<PartOSystemsInAssessmentEntry> entries_Included = [];
            foreach (Guid guid in guids_Included)
            {
                dictionary_System.TryGetValue(guid, out VentilationSystem? ventilationSystem);

                string? name_Dwelling = dictionary_Zone.TryGetValue(guid, out Guid guid_Zone) ? adjacencyCluster.GetObject<Zone>(guid_Zone)?.Name : null;
                string? name_Unit = ventilationSystem is null ? null : UnitName(ventilationSystem);
                string name = name_Unit ?? ventilationSystem?.FullName ?? guid.ToString();

                List<string> details = [];
                if (!string.IsNullOrWhiteSpace(name_Dwelling))
                {
                    details.Add(string.Format("dwelling {0}", name_Dwelling));
                }

                if (ventilationSystem is not null && !string.IsNullOrWhiteSpace(ventilationSystem.FullName) && !string.Equals(ventilationSystem.FullName, name, StringComparison.Ordinal))
                {
                    details.Add(string.Format("system {0}", ventilationSystem.FullName));
                }

                entries_Included.Add(new PartOSystemsInAssessmentEntry(guid, name, details.Count == 0 ? null : string.Join(" · ", details)));
            }

            List<PartOSystemsInAssessmentEntry> entries_Retained = [];
            foreach (PartOSystemsScopeExclusion partOSystemsScopeExclusion in partOSystemsMaterialisationScope?.Exclusions ?? [])
            {
                dictionary_System.TryGetValue(partOSystemsScopeExclusion.Guid_VentilationSystem, out VentilationSystem? ventilationSystem);

                string? name_Unit = ventilationSystem is null ? null : UnitName(ventilationSystem);
                string name = string.IsNullOrWhiteSpace(partOSystemsScopeExclusion.FullName_VentilationSystem) ? partOSystemsScopeExclusion.Guid_VentilationSystem.ToString() : partOSystemsScopeExclusion.FullName_VentilationSystem;

                //What is attached to it, said plainly - never a classification of the system by what it is called.
                List<string> details = [];
                if (name_Unit is not null)
                {
                    details.Add(string.Format("names air handling unit {0}", name_Unit));
                }

                details.Add(partOSystemsScopeExclusion.Count_VentilationTerminal == 0
                    ? "no design terminal"
                    : string.Format("{0}, none with a design airflow", UI.Query.PartOCount(partOSystemsScopeExclusion.Count_VentilationTerminal, "terminal", "terminals")));

                entries_Retained.Add(new PartOSystemsInAssessmentEntry(partOSystemsScopeExclusion.Guid_VentilationSystem, name, string.Join(" · ", details)));
            }

            entries_Included.Sort(CompareEntries);
            entries_Retained.Sort(CompareEntries);

            List<string>? refusals = string.IsNullOrWhiteSpace(refusal_Preflight) ? null : [refusal_Preflight!];

            //A preflight refusal after a taken scope (SAM_Systems would not materialise) says the build stops; the scope's
            //own answer is still true and stays shown beside it.
            return new PartOSystemsInAssessment(partOSimulationRoute, partOSystemsMaterialisationScope is not null, entries_Included, entries_Retained, refusals);
        }

        private static int CompareEntries(PartOSystemsInAssessmentEntry x, PartOSystemsInAssessmentEntry y)
        {
            int compare = PartOMixedDesignSession.NaturalCompare(x.Name, y.Name);

            return compare != 0 ? compare : x.Guid.CompareTo(y.Guid);
        }

        /// <summary>The air handling unit the system names (supply, else exhaust), or null where it names none.</summary>
        private static string? UnitName(VentilationSystem ventilationSystem)
        {
            string? name_Supply = ventilationSystem.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
            if (!string.IsNullOrWhiteSpace(name_Supply))
            {
                return name_Supply;
            }

            string? name_Exhaust = ventilationSystem.GetValue<string>(VentilationSystemParameter.ExhaustUnitName);

            return string.IsNullOrWhiteSpace(name_Exhaust) ? null : name_Exhaust;
        }
    }
}
