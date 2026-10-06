// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The equipment scope of the Iteration 2B evidence: which of the model's air handling units the report
    /// is about.
    /// <para>
    /// <b>The defect.</b> The unit states were read over EVERY air handling unit in the model. The
    /// acceptance model carries the systems it was drawn with - a legacy "MV 1" mechanical ventilation
    /// system with its "AHU1" unit, related to rooms but never to a design terminal - so every iteration's
    /// evidence carried an "AHU1 | MV 1 | NaN/NaN | None selected" row that was never part of the Part O
    /// iteration.
    /// </para>
    /// <para>
    /// <b>The scope is relational, not textual.</b> A unit is in scope where a system bound to it is
    /// connected to at least one design ventilation terminal - a relation only the Part O preparation
    /// creates - and serves a space of the run's dwelling zones. The model is never edited to achieve this;
    /// the report is scoped, the system is untouched.
    /// </para>
    /// </summary>
    public class PartOIterationEquipmentTests
    {
        /// <summary>
        /// The acceptance shape: two dwellings, each with its own Part O Base MVHR system and unit, and the
        /// model's authored "MV 1" system serving one of the dwellings' rooms beside them - with no design
        /// terminal, which is exactly what the preparation never gives it.
        /// </summary>
        private static AdjacencyCluster Cluster(out Zone zone_Flat1, out Zone zone_Flat2, out AirHandlingUnit airHandlingUnit_MVHR1, out AirHandlingUnit airHandlingUnit_MVHR2, out AirHandlingUnit airHandlingUnit_Legacy)
        {
            AdjacencyCluster adjacencyCluster = new();

            zone_Flat1 = new Zone("Flat 1");
            zone_Flat1.SetValue(ZoneParameter.IsDwelling, true);

            zone_Flat2 = new Zone("Flat 2");
            zone_Flat2.SetValue(ZoneParameter.IsDwelling, true);

            Space space_Flat1 = new("Flat 1 Kitchen");
            Space space_Flat2 = new("Flat 2 Kitchen");

            adjacencyCluster.AddObject(zone_Flat1);
            adjacencyCluster.AddObject(zone_Flat2);
            adjacencyCluster.AddObject(space_Flat1);
            adjacencyCluster.AddObject(space_Flat2);

            adjacencyCluster.AddRelation(zone_Flat1, space_Flat1);
            adjacencyCluster.AddRelation(zone_Flat2, space_Flat2);

            //The Part O plant: one system per dwelling, each connected to its dwelling's design terminals.
            airHandlingUnit_MVHR1 = AddPartOSystem(adjacencyCluster, "MVHR-01", "1", space_Flat1);
            airHandlingUnit_MVHR2 = AddPartOSystem(adjacencyCluster, "MVHR-02", "2", space_Flat2);

            //The model's own system, as drawn: related to a room, holding no design terminal.
            VentilationSystem ventilationSystem_Legacy = Analytical.Create.MechanicalSystem(new VentilationSystemType("MV", "Mechanical ventilation"), null, "1") as VentilationSystem;
            airHandlingUnit_Legacy = Analytical.Create.AirHandlingUnit("AHU1");

            ventilationSystem_Legacy.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit_Legacy.Name);

            adjacencyCluster.AddObject(ventilationSystem_Legacy);
            adjacencyCluster.AddObject(airHandlingUnit_Legacy);
            adjacencyCluster.AddRelation(ventilationSystem_Legacy, space_Flat1);

            return adjacencyCluster;
        }

        private static AirHandlingUnit AddPartOSystem(AdjacencyCluster adjacencyCluster, string unitName, string systemId, Space space)
        {
            VentilationSystem ventilationSystem = Analytical.Create.MechanicalSystem(new VentilationSystemType("MVHR", "Continuous mechanical supply and extract with heat recovery"), null, systemId) as VentilationSystem;
            AirHandlingUnit airHandlingUnit = Analytical.Create.AirHandlingUnit(unitName);

            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit.Name);

            VentilationTerminal ventilationTerminal = new(string.Format("{0} supply", space.Name), FlowClassification.Supply, 30);

            adjacencyCluster.AddObject(ventilationSystem);
            adjacencyCluster.AddObject(airHandlingUnit);
            adjacencyCluster.AddObject(ventilationTerminal);

            adjacencyCluster.AddRelation(ventilationTerminal, space);
            adjacencyCluster.AddRelation(ventilationSystem, space);
            adjacencyCluster.AddRelation(ventilationSystem, ventilationTerminal);

            return airHandlingUnit;
        }

        /// <summary>
        /// Over the full dwelling scope, the evidence is exactly the two iteration units. The legacy unit -
        /// related to a room of Flat 1, but to no design terminal - is out of it.
        /// </summary>
        [Fact]
        public void TheLegacySystem_IsOutOfScope_EvenThoughItServesAnInScopeRoom()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Zone zone_Flat1, out Zone zone_Flat2, out _, out _, out _);

            List<AirHandlingUnit> result = Query.PartOIterationAirHandlingUnits(adjacencyCluster, [zone_Flat1, zone_Flat2]);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "MVHR-01");
            Assert.Contains(result, x => x.Name == "MVHR-02");
            Assert.DoesNotContain(result, x => x.Name == "AHU1");
        }

        /// <summary>
        /// A narrowed run reports only its own dwellings' equipment - a Base MVHR system left in the model
        /// by a wider earlier preparation is part of the model, not of this run.
        /// </summary>
        [Fact]
        public void ASubsetScope_ReportsOnlyItsOwnDwellingsEquipment()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out Zone zone_Flat1, out _, out _, out _, out _);

            List<AirHandlingUnit> result = Query.PartOIterationAirHandlingUnits(adjacencyCluster, [zone_Flat1]);

            AirHandlingUnit airHandlingUnit = Assert.Single(result);
            Assert.Equal("MVHR-01", airHandlingUnit.Name);
        }

        /// <summary>
        /// An unconstrained scope - the SAM "whole model" case - is every terminal-connected unit. The legacy
        /// unit is still excluded: the first condition is not about scope at all.
        /// </summary>
        [Fact]
        public void ANullScope_IsEveryTerminalConnectedUnit_AndNothingElse()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out _, out _, out _, out _, out _);

            List<AirHandlingUnit> result = Query.PartOIterationAirHandlingUnits(adjacencyCluster, null);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, x => x.Name == "AHU1");
        }

        /// <summary>
        /// Scope zones that resolve to nothing in this model are an empty scope, not an error - and the
        /// legacy unit is no more in it than before.
        /// </summary>
        [Fact]
        public void AScopeThatResolvesToNothing_SelectsNothing()
        {
            AdjacencyCluster adjacencyCluster = Cluster(out _, out _, out _, out _, out _);

            List<AirHandlingUnit> result = Query.PartOIterationAirHandlingUnits(adjacencyCluster, [new Zone("Flat 9")]);

            Assert.Empty(result);
        }
    }
}
