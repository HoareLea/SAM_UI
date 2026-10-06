// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Analytical.UI;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The models, graphs and routes the Approved Document O Iteration 3 tests work from.
    ///
    /// <para><b>Real types, not stand-ins</b></para>
    /// <para>
    /// Everything here is built from the production constructors of the authorities PR4 orchestrates -
    /// SAM's <c>AdjacencyCluster</c>, SAM_Systems' <c>MechanicalVentilationMaterialisation</c>, SAM_Tas'
    /// <c>NoIzamThermalSource</c>, <c>SystemVentilationRoute</c> and <c>ResultantTemperatureResults</c>.
    /// A hand-written substitute for any of them would let the orchestration be tested against its own
    /// idea of what those return, which is the disagreement the reconciliation exists to catch.
    /// </para>
    /// <para>
    /// <b>None of it touches TAS.</b> Every type used here is free of TAS COM types; the two files the
    /// thermal source's completeness check stats are ordinary temporary files.
    /// </para>
    ///
    /// <para><b>The dwelling shape</b></para>
    /// <para>
    /// Two dwellings of three rooms each, deliberately with the <b>same room names in both</b> - "Bedroom
    /// 2" in Flat 1 and "Bedroom 2" in Flat 2 - so any join that fell back to a name would silently
    /// merge two different rooms and the tests would see it.
    /// </para>
    /// </summary>
    internal static class PartOIteration3Fixture
    {
        internal const double Area = 12.0;

        internal const double Volume = 30.0;

        internal const int HourCount = 8760;

        internal static Space Space(AdjacencyCluster adjacencyCluster, string name)
        {
            Space result = new(name, new Geometry.Spatial.Point3D(0, 0, 0));

            result.SetValue(SpaceParameter.Area, Area);
            result.SetValue(SpaceParameter.Volume, Volume);

            adjacencyCluster.AddObject(result);

            return result;
        }

        internal static Zone Zone(AdjacencyCluster adjacencyCluster, string name, params Space[] spaces)
        {
            Zone result = new(name);

            adjacencyCluster.AddObject(result);

            foreach (Space space in spaces)
            {
                adjacencyCluster.AddRelation(result, space);
            }

            return result;
        }

        internal static VentilationSystem VentilationSystem(AdjacencyCluster adjacencyCluster, string name_AirHandlingUnit, string type, out AirHandlingUnit airHandlingUnit)
        {
            airHandlingUnit = Analytical.Create.AirHandlingUnit(name_AirHandlingUnit);

            adjacencyCluster.AddObject(airHandlingUnit);

            VentilationSystemType ventilationSystemType = Analytical.Create.VentilationSystemType(Guid.NewGuid(), type, type);

            VentilationSystem result = Analytical.Create.MechanicalSystem(ventilationSystemType, null, 1) as VentilationSystem;

            result.SetValue(VentilationSystemParameter.SupplyUnitName, name_AirHandlingUnit);
            result.SetValue(VentilationSystemParameter.ExhaustUnitName, name_AirHandlingUnit);

            adjacencyCluster.AddObject(result);

            return result;
        }

        /// <summary>A system with no air handling unit at all - the shape SAM #114's NV 1 and UV 1 have.</summary>
        internal static VentilationSystem VentilationSystem_Unnamed(AdjacencyCluster adjacencyCluster, string type)
        {
            VentilationSystemType ventilationSystemType = Analytical.Create.VentilationSystemType(Guid.NewGuid(), type, type);

            VentilationSystem result = Analytical.Create.MechanicalSystem(ventilationSystemType, null, 1) as VentilationSystem;

            adjacencyCluster.AddObject(result);

            return result;
        }

        internal static VentilationTerminal Terminal(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, Space space, FlowClassification flowClassification, double? designFlowRate_Lps)
        {
            VentilationTerminal result = new(string.Format("{0} {1}", space.Name, flowClassification), flowClassification, designFlowRate_Lps);

            adjacencyCluster.AddObject(result);
            adjacencyCluster.AddRelation(ventilationSystem, result);
            adjacencyCluster.AddRelation(result, space);

            return result;
        }

        internal static SpaceAirMovement Transfer(AdjacencyCluster adjacencyCluster, Space space_From, Space space_To, double airFlow_Lps)
        {
            SpaceAirMovement result = new(
                string.Format("{0} -> {1}", space_From.Name, space_To.Name),
                airFlow_Lps / 1000.0,
                new ObjectReference(space_From).ToString(),
                new ObjectReference(space_To).ToString());

            adjacencyCluster.AddObject(result);
            adjacencyCluster.AddRelation(result, space_To);

            return result;
        }

        /// <summary>
        /// The canonical prepared design: two dwellings, one MVHR system each, supply into the bedroom,
        /// extract out of the bathroom, a hall with no terminals, and one transfer per dwelling.
        /// <para>
        /// Both dwellings use the <b>same</b> room names, which is what makes every guid join in these
        /// tests load-bearing rather than incidental.
        /// </para>
        /// </summary>
        internal static AdjacencyCluster Design(out List<Guid> guids_VentilationSystem, out List<Zone> zones, int dwellingCount = 2)
        {
            AdjacencyCluster result = new();

            guids_VentilationSystem = [];
            zones = [];

            for (int i = 1; i <= dwellingCount; i++)
            {
                VentilationSystem ventilationSystem = VentilationSystem(result, string.Format("MVHR-0{0}", i), "MVHR", out AirHandlingUnit _);

                Space space_Bedroom = Space(result, "Bedroom 2");
                Space space_Hall = Space(result, "Hall");
                Space space_Bathroom = Space(result, "Bathroom");

                Terminal(result, ventilationSystem, space_Bedroom, FlowClassification.Supply, 13.0);
                Terminal(result, ventilationSystem, space_Bathroom, FlowClassification.Extract, 13.0);

                result.AddRelation(ventilationSystem, space_Bedroom);
                result.AddRelation(ventilationSystem, space_Hall);
                result.AddRelation(ventilationSystem, space_Bathroom);

                Transfer(result, space_Bedroom, space_Hall, 13.0);
                Transfer(result, space_Hall, space_Bathroom, 13.0);

                zones.Add(Zone(result, string.Format("Flat {0}", i), space_Bedroom, space_Hall, space_Bathroom));

                guids_VentilationSystem.Add(ventilationSystem.Guid);
            }

            return result;
        }

        /// <summary>Every space of a zone, in the cluster's own order.</summary>
        internal static List<Space> Spaces(AdjacencyCluster adjacencyCluster, Zone zone)
        {
            return adjacencyCluster.GetRelatedObjects<Space>(adjacencyCluster.GetObject<Zone>(zone.Guid)) ?? [];
        }

        internal static AnalyticalModel Model(AdjacencyCluster adjacencyCluster, string name = "Flat")
        {
            return new AnalyticalModel(name, null, null, null, adjacencyCluster, null, null);
        }

        internal static List<OverheatingScenario> Scenarios()
        {
            return [new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive)];
        }

        //-------------------------------------------------------------------------------------------------
        //SAM_Tas result types, built through their own production constructors.
        //-------------------------------------------------------------------------------------------------

        /// <summary>A simulation that is evidenced as complete without any file having to exist.</summary>
        internal static SimulationEvidence Evidence(string path_Document, string path_Output)
        {
            SimulationEvidence result = new(SimulationOutputShape.SeparateOutputFile, path_Document, path_Output);

            result.RecordCallReturned();

            return result;
        }

        /// <summary>
        /// A complete no-IZAM thermal source. Its two files are created, because
        /// <c>NoIzamThermalSource.IsComplete</c> stats them - which is the behaviour under test elsewhere
        /// and is not worked around here.
        /// </summary>
        internal static NoIzamThermalSource ThermalSource(string directory, IEnumerable<Guid> guids_Space, bool removedIZAMs = true, bool removedGains = true, string projectName_CandidateB = "Flat-It3B")
        {
            string path_TBD = Path.Combine(directory, projectName_CandidateB + ".tbd");
            string path_TSD = Path.Combine(directory, projectName_CandidateB + ".tsd");

            File.WriteAllText(path_TBD, "tbd");
            File.WriteAllText(path_TSD, "tsd");

            Dictionary<Guid, string> zoneReferences = [];

            foreach (Guid guid in guids_Space)
            {
                zoneReferences[guid] = guid.ToString();
            }

            return new NoIzamThermalSource(path_TBD, path_TSD, removedIZAMs, removedGains, Evidence(path_TBD, path_TSD), zoneReferences, null, null);
        }

        internal static SystemVentilationBinding Binding(Guid guid_Space, Guid guid_AirSystem, double? supply_Lps, double? extract_Lps, out Guid guid_SystemSpace)
        {
            guid_SystemSpace = Guid.NewGuid();

            return new SystemVentilationBinding(
                guid_Space,
                guid_SystemSpace,
                guid_AirSystem,
                guid_Space.ToString(),
                string.Concat("load-", guid_Space),
                string.Concat("system-", guid_AirSystem),
                supply_Lps,
                extract_Lps);
        }

        internal static SystemZoneTemperatureResults ZoneTemperatures(IEnumerable<SystemVentilationBinding> systemVentilationBindings, int startHour, int endHour)
        {
            SystemZoneTemperatureResults result = new(startHour, endHour);

            foreach (SystemVentilationBinding systemVentilationBinding in systemVentilationBindings)
            {
                IndexedDoubles indexedDoubles = new();

                for (int i = startHour; i <= endHour; i++)
                {
                    indexedDoubles.Add(i, 21.0);
                }

                result.Add(new SystemZoneTemperatureResult(
                    systemVentilationBinding.Guid_Space,
                    systemVentilationBinding.Guid_SystemSpace,
                    systemVentilationBinding.Reference_SystemZone,
                    systemVentilationBinding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    indexedDoubles,
                    null));
            }

            result.Validate([.. systemVentilationBindings]);

            return result;
        }

        internal static ResultantTemperatureResults ResultantTemperatures(string path_TSD_Bridge, IEnumerable<Guid> guids_Space, int startHour, int endHour, Func<Guid, int, double> func_Value)
        {
            List<ResultantTemperatureResult> results = [];

            foreach (Guid guid in guids_Space)
            {
                IndexedDoubles indexedDoubles = new();

                for (int i = startHour; i <= endHour; i++)
                {
                    indexedDoubles.Add(i, func_Value(guid, i));
                }

                results.Add(new ResultantTemperatureResult(guid, guid.ToString(), startHour, endHour, indexedDoubles, null));
            }

            return new ResultantTemperatureResults(
                "the Approved Document O thermostat bridge",
                startHour,
                endHour,
                guids_Space,
                results,
                path_TSD_Bridge,
                Evidence(path_TSD_Bridge, path_TSD_Bridge),
                null,
                null);
        }

        internal static PartOSimulationContext SimulationContext(string directory, string projectName = "Flat")
        {
            return new PartOSimulationContext(directory, projectName, new WeatherData("CIBSE 2021 Leeds_TRY", "Fixture", 53.8, -1.5, 50), SolarCalculationMethod.TAS, 1, 365);
        }

        /// <summary>A directory of this test run's own, deleted by the caller.</summary>
        internal static string Directory_Temp()
        {
            string result = Path.Combine(Path.GetTempPath(), "SAM.PartOIteration3", Guid.NewGuid().ToString("N"));

            System.IO.Directory.CreateDirectory(result);

            //The folders an Iteration 3 run against a reference here writes into (PartOOutputPaths), so a test can
            //arrange a record or a TAS file in them before the run - which creates them itself - has started.
            PartOOutputPaths.Create(result, PartOOutputCase.Iteration3).CreateDirectories();

            return result;
        }
    }
}
