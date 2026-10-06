// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// What a person asked the Approved Document O workflow for: one base provision, one dwelling scope, and
    /// whether a manufacturer unit is selected against it.
    /// <para>
    /// <b>The same inputs <c>SAM.Analytical.Modify.PreparePartOIteration</c> takes, and nothing else.</b>
    /// This carries them from the dialog to the orchestration without any layer in between having to
    /// re-derive one. It computes exactly one thing - the per-zone ventilation strategy dictionary - and that
    /// is a fan-out of the option's own canonical word over the zones in scope, not a decision.
    /// </para>
    /// <para>
    /// <b>What a dwelling is was decided before this object existed.</b> <see cref="Zones_Dwelling"/> holds
    /// zones the caller took from <c>Query.PartFDwellingZones</c>; there is no second dwelling rule here and
    /// no filtering of its answer.
    /// </para>
    /// </summary>
    public class PartOWorkflowRequest
    {
        private readonly List<Zone> zones_Dwelling = [];

        /// <param name="partOVentilationStrategyOption">
        /// The base provision - Iteration 1a or 1b - together with the canonical route word it is defined
        /// over. Both come from <see cref="PartOVentilationStrategyOption.Options"/>, which asks SAM which
        /// route each iteration belongs to; neither is chosen here.
        /// </param>
        /// <param name="partOWorkflowScope">Which dwellings, and whether they are simulated in isolation.</param>
        /// <param name="zones_Dwelling">
        /// The dwelling zones in scope, as <c>Query.PartFDwellingZones</c> returned them and the user's
        /// selection narrowed them.
        /// </param>
        /// <param name="selectVentilationUnit">
        /// Whether a manufacturer product is selected against the realized design - Iteration 2. False is
        /// Iteration 1a, and the difference reaches the preparation as a null catalogue rather than an empty
        /// one.
        /// </param>
        public PartOWorkflowRequest(PartOVentilationStrategyOption partOVentilationStrategyOption, PartOWorkflowScope partOWorkflowScope, IEnumerable<Zone> zones_Dwelling, bool selectVentilationUnit)
        {
            Option = partOVentilationStrategyOption;
            Scope = partOWorkflowScope;
            SelectVentilationUnit = selectVentilationUnit;

            foreach (Zone zone in zones_Dwelling ?? [])
            {
                if (zone is not null)
                {
                    this.zones_Dwelling.Add(zone);
                }
            }
        }

        /// <summary>The base provision and the canonical route word that travel together.</summary>
        public PartOVentilationStrategyOption Option { get; }

        /// <summary>The base iteration handed to the preparation.</summary>
        public PartOIteration PartOIteration => Option is null ? PartOIteration.Undefined : Option.PartOIteration;

        /// <summary>Which dwellings, and whether they are simulated as a thermal model of their own.</summary>
        public PartOWorkflowScope Scope { get; }

        /// <summary>The dwelling zones in scope. Never re-filtered here.</summary>
        public List<Zone> Zones_Dwelling => [.. zones_Dwelling];

        /// <summary>Whether equipment selection runs - the Iteration 1a / Iteration 2 difference.</summary>
        public bool SelectVentilationUnit { get; }

        /// <summary>
        /// Whether the selected dwellings are extracted into their own thermal model. Read off the scope
        /// rather than stored, so the two can never disagree.
        /// </summary>
        public bool Isolate => Scope == PartOWorkflowScope.SelectedDwellingsIsolated;

        /// <summary>
        /// The Iteration 2B optimisation to allow once this run has results, or null where none was asked
        /// for. Not a preparation input - see <see cref="PartOPreparationContext.OptimisationSettings"/>.
        /// </summary>
        public PartOOptimisationSettings OptimisationSettings { get; set; }

        /// <summary>
        /// The equipment preselection this run is asking for - how ventilation units are to be chosen, and
        /// which products they may be chosen from.
        /// <para>
        /// <b>Null means "not stated", not "the default".</b> A caller that says nothing - Prepare &amp; Run
        /// builds its request from a scenario and a scope and says nothing about equipment - inherits the
        /// PROJECT's own preselection, and only a project that has never stated one gets the historic
        /// default. <c>Query.PartOEquipmentSelection</c> is that resolution, and says why the order is
        /// load-bearing: the alternative silently replaces authored manual assignments.
        /// </para>
        /// <para>
        /// <b>Not the same fact as <see cref="SelectVentilationUnit"/>.</b> That says whether equipment
        /// selection happens at all - the Iteration 1a / Iteration 2 difference. This says how, once it
        /// does. A run with <see cref="SelectVentilationUnit"/> false ignores this entirely.
        /// </para>
        /// </summary>
        public PartOEquipmentSelection EquipmentSelection { get; set; }

        /// <summary>
        /// The project's own test ventilation unit this run is asking for, or null where none is stated.
        /// <para>
        /// <b>Null means "not stated", not "none"</b>, exactly as for <see cref="EquipmentSelection"/>: a
        /// caller that says nothing inherits the PROJECT's own statement, and only a project that has never
        /// stated one has none. <c>Query.PartOProjectTestVentilationUnit</c> is that resolution.
        /// </para>
        /// <para>
        /// A capability, not a selection - it says what a made-up unit can move, never that any dwelling is
        /// fitted with it. Which dwellings are is <c>AirHandlingUnitParameter.VentilationUnitReference</c>,
        /// as for any other product.
        /// </para>
        /// </summary>
        public PartOProjectTestVentilationUnit ProjectTestVentilationUnit { get; set; }

        /// <summary>
        /// The canonical ventilation route stated for every zone in scope: the option's own word, over the
        /// zones in scope. There is no free-text path into this dictionary, which is what keeps the
        /// "prepares then refuses every space at assessment" synonym unreachable - see
        /// <see cref="PartOVentilationStrategyOption"/>.
        /// </summary>
        public Dictionary<Guid, string> VentilationStrategies()
        {
            Dictionary<Guid, string> result = [];

            if (Option is null)
            {
                return result;
            }

            foreach (Zone zone in zones_Dwelling)
            {
                result[zone.Guid] = Option.VentilationStrategy;
            }

            return result;
        }
    }
}
