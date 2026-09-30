// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What Check design found, with no simulation: SAM's materialisation and, on the Systems route, the TAS Systems
    /// preflight Build &amp; Run makes before any TAS time is spent.
    /// </summary>
    internal sealed class PartOMixedDesignCheck
    {
        /// <summary>SAM's materialisation of the selected design; its structured refusals are the dwelling-level ones.</summary>
        public PartOMaterialisation? Materialisation { get; init; }

        /// <summary>
        /// Why the Systems-route preflight refused, or null - where it passed, or the design is not on the Systems route.
        /// The same text Build &amp; Run reports for the same model.
        /// </summary>
        public string? Refusal_Systems { get; init; }

        /// <summary>What the Systems preflight noted (the systems it left out of the SAM_Systems input, and why).</summary>
        public List<string> Notes_Systems { get; } = [];

        /// <summary>Whether the Systems preflight was asked at all - only for a materialised design on the Systems route.</summary>
        public bool SystemsChecked { get; init; }

        public bool IsMaterialised => Materialisation?.IsMaterialised ?? false;

        /// <summary>Whether Build &amp; Run would get as far as TAS.</summary>
        public bool Passed => IsMaterialised && Refusal_Systems is null;
    }

    public static partial class Modify
    {
        /// <summary>
        /// Check design without running TAS, asking exactly what Build &amp; Run asks before its first TAS call (PR-1):
        /// <list type="number">
        /// <item>SAM materialises the selected design (<c>Analytical.Modify.MaterialisePartODwellingStrategies</c>) with the
        /// catalogue and templates Build &amp; Run offers;</item>
        /// <item>where SAM's record puts it on the Systems route, the ONE mixed Systems preflight
        /// (<see cref="PartOMixedSystemsMaterialisation(PartOMaterialisation, IEnumerable{VentilationUnitTemplate}, PartOIteration3Pipeline, out string, out List{string})"/>)
        /// - the scope, the SAM_Systems graph and its agreement with SAM's record - the very function
        /// <see cref="SimulatePartOMaterialisationSystems"/> runs first.</item>
        /// </list>
        /// Nothing is simulated and the model is not modified.
        /// </summary>
        internal static PartOMixedDesignCheck CheckPartOMixedDesign(AnalyticalModel analyticalModel_WithSelection, IEnumerable<VentilationUnitCapacityDescriptor>? ventilationUnitCapacityDescriptors, IEnumerable<VentilationUnitTemplate>? ventilationUnitTemplates)
        {
            List<VentilationUnitTemplate>? ventilationUnitTemplates_Temp = ventilationUnitTemplates is null ? null : [.. ventilationUnitTemplates];

            PartOMaterialisation partOMaterialisation = Analytical.Modify.MaterialisePartODwellingStrategies(analyticalModel_WithSelection, ventilationUnitCapacityDescriptors, null, ventilationUnitTemplates_Temp);

            if (!partOMaterialisation.IsMaterialised || partOMaterialisation.Route != PartOSimulationRoute.Systems)
            {
                return new PartOMixedDesignCheck { Materialisation = partOMaterialisation };
            }

            PartOMixedSystemsMaterialisation(partOMaterialisation, ventilationUnitTemplates_Temp, new PartOIteration3Pipeline(), out string? refusal, out List<string> notes);

            PartOMixedDesignCheck result = new()
            {
                Materialisation = partOMaterialisation,
                Refusal_Systems = refusal,
                SystemsChecked = true,
            };

            result.Notes_Systems.AddRange(notes);

            return result;
        }
    }
}
