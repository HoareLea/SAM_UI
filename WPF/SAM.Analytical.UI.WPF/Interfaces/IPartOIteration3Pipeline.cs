// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The five pieces of work an Iteration 3 run delegates to somebody else, behind one seam.
    ///
    /// <para><b>Every member is an existing authority, and none of them is here</b></para>
    /// <list type="bullet">
    /// <item><see cref="Materialise"/> is <c>SAM.Analytical.Systems.Create.MechanicalVentilation</c>.</item>
    /// <item><see cref="ThermalSource"/> is <see cref="Modify.RunPartOSimulation"/> with SAM_Tas'
    /// <c>Create.NoIzamThermalSource</c> as its workflow runner.</item>
    /// <item><see cref="Route"/> is <c>SAM.Analytical.Tas.TPD.Create.SystemVentilationRoute</c>.</item>
    /// <item><see cref="ResultantTemperatures"/> is the current
    /// <c>IResultantTemperatureProvider</c>.</item>
    /// <item><see cref="Assess"/> is <c>PartOTM59Assessment.Assess</c> and
    /// <c>Modify.SavePartOTM59Report</c>.</item>
    /// </list>
    /// <para>
    /// <c>Modify.RunPartOIteration3</c> sequences them, checks identity between them and computes
    /// descriptive statistics from what they return. It decides no engineering question, and this
    /// interface is where that is visible: there is nothing on it this assembly could implement.
    /// </para>
    ///
    /// <para><b>Why a seam rather than direct calls</b></para>
    /// <para>
    /// Four of these five need a licensed TAS installation and an hour of wall clock. Without a seam the
    /// only testable part of this pipeline would be the part that does not matter - and the parts that do
    /// are the refusal boundaries: that a refused materialisation never reaches the thermal source, that
    /// a refused route never reaches the provider, that a stage after a refusal is never even called.
    /// Each of those is a delegate that must not be invoked, which is exactly what a seam can assert and
    /// a direct call cannot.
    /// </para>
    /// <para>
    /// <b>It is not an extension point.</b> <see cref="PartOIteration3Pipeline"/> is the only production
    /// implementation, the command constructs it itself, and nothing reads this interface from
    /// configuration.
    /// </para>
    /// </summary>
    public interface IPartOIteration3Pipeline
    {
        /// <summary>
        /// Materialises the explicit mechanical ventilation from the scoped working copy of the design.
        /// </summary>
        /// <param name="adjacencyCluster">The scoped working copy - see <c>Query.PartOIteration3SystemScope</c>.</param>
        /// <param name="spaces">The Part O dwelling design scope.</param>
        /// <param name="unitSettings">
        /// PR5A (SAM#111 plan §J): each scoped air handling unit's resolved manufacturer-aware behaviour,
        /// keyed by its guid. Null or empty materialises exactly as before PR5A existed - the B0 control.
        /// </param>
        /// <param name="coolingSettings">
        /// PR5B (SAM#111): each scoped air handling unit's resolved cooling module, keyed by its guid,
        /// materialised as an internal recirculation branch in the unit's own air system. Null or empty
        /// materialises no cooling - the B0 control.
        /// </param>
        /// <param name="guidanceSettings">
        /// SAM#123: each scoped unit's selected product operated to its manufacturer's guidance, keyed by its
        /// guid - materialised on the MVRE topology with a supply DX coil. Null or empty materialises none.
        /// </param>
        MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings = null, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings = null);

        /// <summary>
        /// Part O PR-3: <see cref="Materialise(AdjacencyCluster, IEnumerable{Space}, IReadOnlyDictionary{Guid, MechanicalVentilationUnitSettings}, IReadOnlyDictionary{Guid, MechanicalVentilationCoolingSettings}, IReadOnlyDictionary{Guid, MechanicalVentilationGuidanceSettings})"/>
        /// told the scope SAM_Systems may process. An added member with a default body, so an existing implementation of
        /// this interface still compiles and binds; it serves an unstated scope and refuses a stated one it cannot honour,
        /// rather than quietly processing every system.
        /// </summary>
        /// <param name="guids_VentilationSystem">
        /// The ventilation systems SAM_Systems may process - SAM's retained scope. Stated, SAM_Systems processes no other
        /// system of <paramref name="adjacencyCluster"/>. Null processes every one.
        /// </param>
        MechanicalVentilationMaterialisation Materialise(AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, IReadOnlyDictionary<Guid, MechanicalVentilationUnitSettings> unitSettings, IReadOnlyDictionary<Guid, MechanicalVentilationCoolingSettings> coolingSettings, IReadOnlyDictionary<Guid, MechanicalVentilationGuidanceSettings> guidanceSettings, IEnumerable<Guid> guids_VentilationSystem)
        {
            if (guids_VentilationSystem is not null)
            {
                throw new NotSupportedException(GetType().Name + " cannot limit SAM_Systems to a stated ventilation system scope.");
            }

            return Materialise(adjacencyCluster, spaces, unitSettings, coolingSettings, guidanceSettings);
        }

        /// <summary>
        /// Runs Candidate B's dedicated no-IZAM thermal case - the same TAS case as Reference A, writing
        /// somewhere else, with the mechanical ventilation removed because it is about to be modelled
        /// explicitly instead.
        /// </summary>
        /// <param name="analyticalModel_Prepared">Reference A's own prepared design. Never mutated.</param>
        /// <param name="partOSimulationContext">A complete copy of Reference A's case, renamed and redirected.</param>
        /// <param name="projectName">Candidate B's project name.</param>
        /// <param name="cancellationToken">The run's token.</param>
        /// <param name="analyticalModel_Source">
        /// The model the no-IZAM workflow returned - the design side of Candidate B's TM59 assessment,
        /// because only it carries the TAS zone identities the bridge results resolve against.
        /// </param>
        /// <param name="cancelled">Whether the run was cancelled.</param>
        /// <param name="notes">What the run had to say.</param>
        /// <param name="refusal">Why it could not start, or null.</param>
        NoIzamThermalSource ThermalSource(
            AnalyticalModel analyticalModel_Prepared,
            PartOSimulationContext partOSimulationContext,
            string projectName,
            CancellationToken cancellationToken,
            out AnalyticalModel analyticalModel_Source,
            out bool cancelled,
            out List<string> notes,
            out string refusal);

        /// <summary>Converts the explicit systems to TAS Systems, simulates them and reads ZoneTemperature.</summary>
        /// <param name="fanHeatGainPolicy">
        /// PR5A (SAM#111 plan §D/§K.3): <c>ClearToZero</c> (the default - the B0 control) or
        /// <c>FromSystemsGraph</c>, which leaves a fan's <c>HeatGainFactor</c> exactly as
        /// <paramref name="mechanicalVentilationMaterialisation"/>'s unit settings resolved it.
        /// </param>
        SystemVentilationRoute Route(
            NoIzamThermalSource noIzamThermalSource,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            string path_TPD,
            int startHour,
            int endHour,
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero);

        /// <summary>
        /// The current <c>IResultantTemperatureProvider</c>'s answer for the route.
        /// <para>
        /// Named for what is wanted and not for how it is obtained, exactly as the provider interface is:
        /// when TAS Systems answers a resultant temperature natively, the implementation behind this
        /// changes and nothing in the orchestration does.
        /// </para>
        /// </summary>
        ResultantTemperatureResults ResultantTemperatures(SystemVentilationRoute systemVentilationRoute, string path_TBD_Bridge);

        /// <summary>
        /// The unchanged production TM59 assessment of one results file, with the resultant temperatures
        /// it read captured for the named rooms.
        /// </summary>
        /// <param name="spaceGuids_Capture">
        /// Which design rooms to capture. Scoped rather than "all" because a full annual series per room
        /// is what makes a five-thousand-space comparison expensive.
        /// </param>
        PartOIteration3Assessment Assess(
            AnalyticalModel analyticalModel_Workflow,
            string path_TSD,
            IEnumerable<OverheatingScenario> overheatingScenarios,
            IEnumerable<Guid> spaceGuids_Capture);

        /// <summary>
        /// Writes Candidate B's reopenable model, stamped with its provenance to the bridge results.
        /// </summary>
        /// <param name="note">What went wrong, or null.</param>
        bool Persist(AnalyticalModel analyticalModel, string path_TSD, string path_TBD, out string note);
    }
}
