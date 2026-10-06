// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The exclusion of generated TAS plant zones from the Part O TM59 assessment seam.
    /// <para>
    /// <b>What these zones are.</b> <c>SAM.Analytical.Tas.Modify.UpdateIZAMs</c> builds one small TAS zone
    /// per air handling unit that carries an <c>AirHandlingUnitAirMovement</c> - the unit's own plant zone,
    /// renamed to the unit's name ("MVHR-01" and so on) - and the simulation runs over it like any other
    /// zone. It comes back in the TSD, converts to a SAM space, and then resolves to no design space,
    /// because there is none: it is not a room. Before this exclusion it produced a
    /// "does not resolve to exactly one design space" refusal on every restoration and every assessment,
    /// every optimisation round.
    /// </para>
    /// <para>
    /// <b>What these tests pin.</b> The identification is positive - unresolved through the
    /// <see cref="SimulationSpaceMap"/> AND named after a design-model air handling unit - so a generated
    /// plant zone is excluded while a genuinely unresolved room still refuses, exactly as before. The
    /// calculator-level tests prove both halves against the production seam
    /// (<c>PartOTM59Assessment.WithoutPlantZoneSpaces</c> followed by <c>TM59AssessmentCalculator</c>).
    /// </para>
    /// </summary>
    public class PartOPlantZoneTests
    {
        //Zone guids, in the string form TAS writes them. The design room and its simulated counterpart share
        //one; the plant zone's guid exists only on the simulated side, which is exactly the unresolved state.
        private const string zoneGuid_Kitchen = "{6F1B0F2E-0000-4000-8000-0000000000A1}";
        private const string zoneGuid_MVHR01 = "{6F1B0F2E-0000-4000-8000-0000000000B1}";
        private const string zoneGuid_Stray = "{6F1B0F2E-0000-4000-8000-0000000000C1}";

        private static Space Stamped(string name, string zoneGuid)
        {
            Space result = new(name);
            result.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, zoneGuid);

            return result;
        }

        private static SimulationSpaceMap Map(List<Space> spaces_Design, List<Space> spaces_Simulated)
        {
            return new SimulationSpaceMap(spaces_Design, spaces_Simulated, Analytical.Tas.Query.SimulationSpaceKey);
        }

        private static AnalyticalModel Model(params Space[] spaces)
        {
            AdjacencyCluster adjacencyCluster = new();
            foreach (Space space in spaces)
            {
                adjacencyCluster.AddObject(space);
            }

            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, null, null);
        }

        private static AnalyticalModel Model(AirHandlingUnit airHandlingUnit, params Space[] spaces)
        {
            AdjacencyCluster adjacencyCluster = new();
            foreach (Space space in spaces)
            {
                adjacencyCluster.AddObject(space);
            }

            adjacencyCluster.AddObject(airHandlingUnit);

            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, null, null);
        }

        // ---------------------------------------------------------------------------------------------
        // The identification itself
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The generated zone: unresolved through the map, and named after the design model's unit. Both
        /// conditions hold, so it is identified.
        /// </summary>
        [Fact]
        public void AnUnresolvedSpace_NamedAfterAnAirHandlingUnit_IsAPlantZone()
        {
            List<Space> spaces_Design = [Stamped("Kitchen", zoneGuid_Kitchen)];
            List<Space> spaces_Simulated = [Stamped("Kitchen", zoneGuid_Kitchen), Stamped("MVHR-01", zoneGuid_MVHR01)];

            List<AirHandlingUnit> airHandlingUnits = [Analytical.Create.AirHandlingUnit("MVHR-01")];

            List<Space> result = Query.PartOPlantZoneSpaces(spaces_Simulated, airHandlingUnits, Map(spaces_Design, spaces_Simulated));

            Space space = Assert.Single(result);
            Assert.Equal("MVHR-01", space.Name);
        }

        /// <summary>
        /// The export's own zone lookup is case- and whitespace-insensitive, so the identification is too:
        /// the zone TAS hands back is the one the export named, however its spelling survived the round trip.
        /// </summary>
        [Fact]
        public void TheNameMatch_FollowsTheExportsOwnNormalisation()
        {
            List<Space> spaces_Simulated = [Stamped(" mvhr-01 ", zoneGuid_MVHR01)];

            List<AirHandlingUnit> airHandlingUnits = [Analytical.Create.AirHandlingUnit("MVHR-01")];

            List<Space> result = Query.PartOPlantZoneSpaces(spaces_Simulated, airHandlingUnits, Map([], spaces_Simulated));

            Assert.Single(result);
        }

        /// <summary>
        /// Condition (1) alone is never enough: a room that shares a unit's name but DOES resolve to a design
        /// space is a room, and is never excluded.
        /// </summary>
        [Fact]
        public void AResolvedSpace_NamedLikeAUnit_IsNotAPlantZone()
        {
            //A design room genuinely called "MVHR-01", and the unit of the same name beside it.
            List<Space> spaces_Design = [Stamped("MVHR-01", zoneGuid_Kitchen)];
            List<Space> spaces_Simulated = [Stamped("MVHR-01", zoneGuid_Kitchen)];

            List<AirHandlingUnit> airHandlingUnits = [Analytical.Create.AirHandlingUnit("MVHR-01")];

            List<Space> result = Query.PartOPlantZoneSpaces(spaces_Simulated, airHandlingUnits, Map(spaces_Design, spaces_Simulated));

            Assert.Empty(result);
        }

        /// <summary>
        /// Condition (2) alone is never enough either: an unresolved space named after no unit is a genuinely
        /// unmappable room, and it must keep warning.
        /// </summary>
        [Fact]
        public void AnUnresolvedSpace_NamedAfterNoUnit_IsNotAPlantZone()
        {
            List<Space> spaces_Design = [Stamped("Kitchen", zoneGuid_Kitchen)];
            List<Space> spaces_Simulated = [Stamped("Bedroom 9", zoneGuid_Stray)];

            List<AirHandlingUnit> airHandlingUnits = [Analytical.Create.AirHandlingUnit("MVHR-01")];

            List<Space> result = Query.PartOPlantZoneSpaces(spaces_Simulated, airHandlingUnits, Map(spaces_Design, spaces_Simulated));

            Assert.Empty(result);
        }

        /// <summary>A model with no air handling units generates no plant zones, so nothing is excluded.</summary>
        [Fact]
        public void NoAirHandlingUnits_NoExclusion()
        {
            List<Space> spaces_Simulated = [Stamped("MVHR-01", zoneGuid_MVHR01)];

            Assert.Empty(Query.PartOPlantZoneSpaces(spaces_Simulated, [], Map([], spaces_Simulated)));
            Assert.Empty(Query.PartOPlantZoneSpaces(spaces_Simulated, null, Map([], spaces_Simulated)));
        }

        // ---------------------------------------------------------------------------------------------
        // The production seam: excluded from restoration and from assessment, without touching the rest
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Over the excluded model, the calculator's restoration says nothing about the plant zone - while
        /// the genuinely unresolved room beside it still refuses, in the same words as before. Both halves
        /// of the contract, in one run of the production seam.
        /// </summary>
        [Fact]
        public void TheExcludedModel_ProducesNoPlantZoneRefusal_AndStillRefusesAGenuineGap()
        {
            Space space_Kitchen_Design = Stamped("Kitchen", zoneGuid_Kitchen);

            AnalyticalModel analyticalModel_Design = Model(Analytical.Create.AirHandlingUnit("MVHR-01"), space_Kitchen_Design);

            //What Convert.ToSAM(path_TSD) hands back: the room, the unit's generated plant zone, and a room
            //that genuinely does not map - the defect that must keep surfacing.
            AnalyticalModel analyticalModel_Simulated = Model(
                Stamped("Kitchen", zoneGuid_Kitchen),
                Stamped("MVHR-01", zoneGuid_MVHR01),
                Stamped("Bedroom 9", zoneGuid_Stray));

            SimulationSpaceMap simulationSpaceMap = Map(analyticalModel_Design.GetSpaces(), analyticalModel_Simulated.GetSpaces());

            //The exact call PartOTM59Assessment.Assess makes.
            AnalyticalModel analyticalModel_Excluded = PartOTM59Assessment.WithoutPlantZoneSpaces(analyticalModel_Simulated, analyticalModel_Design, simulationSpaceMap);

            //The caller's own conversion result is untouched - the exclusion works on a copy.
            Assert.Equal(3, analyticalModel_Simulated.GetSpaces().Count);

            TM59AssessmentCalculator tM59AssessmentCalculator = new(analyticalModel_Excluded, analyticalModel_Design, simulationSpaceMap);

            tM59AssessmentCalculator.RestoreDesignInternalConditions();

            //No false noise: the plant zone is simply not there to be restored.
            Assert.DoesNotContain(tM59AssessmentCalculator.AssociationRefusals, x => x.Contains("MVHR-01"));

            //And the real one: a design space nothing resolved to still refuses, named.
            Assert.Contains(tM59AssessmentCalculator.AssociationRefusals, x => x.Contains("Bedroom 9") && x.Contains("does not resolve to exactly one design space"));

            //The same two facts hold for the assessment selection.
            List<Space> spaces_Assessed = tM59AssessmentCalculator.Spaces(null, null);

            Space space_Assessed = Assert.Single(spaces_Assessed);
            Assert.Equal("Kitchen", space_Assessed.Name);

            Assert.DoesNotContain(tM59AssessmentCalculator.AssociationRefusals, x => x.Contains("MVHR-01"));
            Assert.Contains(tM59AssessmentCalculator.AssociationRefusals, x => x.Contains("Bedroom 9") && x.Contains("cannot be assessed"));
        }

        /// <summary>Nothing to exclude: the input model is handed back as-is, not copied.</summary>
        [Fact]
        public void WithoutPlantZones_TheModelIsReturnedUnmodified()
        {
            AnalyticalModel analyticalModel_Design = Model(Analytical.Create.AirHandlingUnit("MVHR-01"), Stamped("Kitchen", zoneGuid_Kitchen));
            AnalyticalModel analyticalModel_Simulated = Model(Stamped("Kitchen", zoneGuid_Kitchen));

            SimulationSpaceMap simulationSpaceMap = Map(analyticalModel_Design.GetSpaces(), analyticalModel_Simulated.GetSpaces());

            AnalyticalModel result = PartOTM59Assessment.WithoutPlantZoneSpaces(analyticalModel_Simulated, analyticalModel_Design, simulationSpaceMap);

            Assert.Same(analyticalModel_Simulated, result);
            Assert.Single(result.GetSpaces());
        }
    }
}
