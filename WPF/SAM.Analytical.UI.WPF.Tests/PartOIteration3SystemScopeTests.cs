// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>SAM #114's production answer, and the comparability gate PR4 adds to it.</b>
    ///
    /// <para><b>The canonical case</b></para>
    /// <para>
    /// A real Approved Document O model reaches this stage carrying the systems the iteration built and
    /// the authored natural, uncontrolled and legacy ones <c>PreparePartOIteration</c> deliberately
    /// preserved. PR1 refuses such a model outright, so the scope has to decide which are the design
    /// under assessment - by identity, from what the preparation reported, and never by name.
    /// </para>
    ///
    /// <para><b>And the case PR4 must now refuse</b></para>
    /// <para>
    /// Candidate B's no-IZAM source removes mechanical ventilation <b>model-wide</b> and reinstates only
    /// what is materialised. A legacy mechanical system serving a room outside the assessed dwellings is
    /// therefore not harmless context: its ventilation exists in Reference A, is gone in Candidate B,
    /// and that room is coupled to the assessed ones. Calling the difference a difference between the
    /// two ROUTES would be wrong, so the pairing is refused instead.
    /// </para>
    /// </summary>
    public class PartOIteration3SystemScopeTests
    {
        /// <summary>
        /// The canonical prepared model: two MVHR systems that the iteration built, plus an NV system, a
        /// UV system and a legacy MV system that it did not - the shape SAM #114 records off the real
        /// fixture.
        /// </summary>
        private static Guid guid_NV;

        private static Guid guid_UV;

        private static AdjacencyCluster Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV)
        {
            AdjacencyCluster result = PartOIteration3Fixture.Design(out guids_Prepared, out zones);

            //Authored and preserved by the preparation: neither names a unit and neither carries a design
            //terminal.
            guid_NV = PartOIteration3Fixture.VentilationSystem_Unnamed(result, "NV").Guid;
            guid_UV = PartOIteration3Fixture.VentilationSystem_Unnamed(result, "UV").Guid;

            //Names a resolvable unit but carries no design terminal - PR1's second refusal in SAM #114.
            ventilationSystem_LegacyMV = PartOIteration3Fixture.VentilationSystem(result, "AHU1", "MV", out AirHandlingUnit _);

            //A thermally participating room outside every assessed dwelling - the corridor.
            space_Corridor = PartOIteration3Fixture.Space(result, "Corridor");

            result.AddRelation(ventilationSystem_LegacyMV, space_Corridor);

            return result;
        }

        private static List<Guid> DwellingSpaceGuids(AdjacencyCluster adjacencyCluster, IEnumerable<Zone> zones)
        {
            List<Guid> result = [];

            foreach (Zone zone in zones)
            {
                foreach (Space space in PartOIteration3Fixture.Spaces(adjacencyCluster, zone))
                {
                    result.Add(space.Guid);
                }
            }

            return result;
        }

        [Fact]
        public void The_canonical_shape_retains_the_built_systems_and_leaves_the_authored_ones_out_with_a_note()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem _);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.True(partOIteration3SystemScope.IsScoped);
            Assert.Empty(partOIteration3SystemScope.Refusals);

            List<Guid> guids_Retained = [.. guids_Prepared];
            guids_Retained.Sort();

            Assert.Equal(guids_Retained, partOIteration3SystemScope.Guids_Retained);
            Assert.Equal(3, partOIteration3SystemScope.Guids_Removed.Count);

            //Every removal says why it is not mechanical duty Candidate B has to recreate.
            Assert.Contains(partOIteration3SystemScope.Notes, x => x.Contains("no design ventilation terminal"));

            //The working copy carries exactly the design under assessment.
            Assert.Equal(guids_Prepared.Count, partOIteration3SystemScope.AdjacencyCluster.GetObjects<VentilationSystem>().Count);
        }

        /// <summary>
        /// The design is an input. Nothing here may rewrite it - and in particular the natural and
        /// uncontrolled ventilation, which the thermal model simulates in both cases, is still on it.
        /// </summary>
        [Fact]
        public void The_source_design_is_left_exactly_as_it_was_including_its_NV_and_UV_systems()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem _);

            int count_System = adjacencyCluster.GetObjects<VentilationSystem>().Count;
            int count_Terminal = adjacencyCluster.GetObjects<VentilationTerminal>().Count;
            int count_Space = adjacencyCluster.GetSpaces().Count;
            int count_Movement = adjacencyCluster.GetObjects<SpaceAirMovement>().Count;

            Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.Equal(count_System, adjacencyCluster.GetObjects<VentilationSystem>().Count);
            Assert.Equal(count_Terminal, adjacencyCluster.GetObjects<VentilationTerminal>().Count);
            Assert.Equal(count_Space, adjacencyCluster.GetSpaces().Count);
            Assert.Equal(count_Movement, adjacencyCluster.GetObjects<SpaceAirMovement>().Count);

            //Named explicitly: NV and UV are authored thermal behaviour and are never removed from
            //anything that is simulated. Asserted by identity, because that is how the scope decides.
            Assert.Contains(adjacencyCluster.GetObjects<VentilationSystem>(), x => x.Guid == guid_NV);
            Assert.Contains(adjacencyCluster.GetObjects<VentilationSystem>(), x => x.Guid == guid_UV);
        }

        /// <summary>
        /// A second mechanical design for a room this iteration already designed. Choosing between two
        /// designs is not an orchestration's decision.
        /// </summary>
        [Fact]
        public void A_competing_mechanical_design_inside_the_dwelling_scope_refuses_naming_both_identities()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem ventilationSystem_LegacyMV);

            Space space = PartOIteration3Fixture.Spaces(adjacencyCluster, zones[0])[0];

            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_LegacyMV, space, FlowClassification.Extract, 9.0);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.False(partOIteration3SystemScope.IsScoped);
            Assert.Null(partOIteration3SystemScope.AdjacencyCluster);

            string refusal = Assert.Single(partOIteration3SystemScope.Refusals);

            Assert.Contains(ventilationSystem_LegacyMV.Guid.ToString(), refusal);
            Assert.Contains(space.Guid.ToString(), refusal);
            Assert.Contains(space.Name, refusal);
            Assert.Contains("inside the assessed Approved Document O dwelling scope", refusal);
            Assert.Contains("second mechanical ventilation design", refusal);
        }

        /// <summary>
        /// <b>The gate PR4 adds.</b> The room is outside the assessed dwellings and is part of the same
        /// thermal model, so removing its ventilation in Candidate B and not in Reference A puts a second
        /// difference into the comparison. Refused, and not labelled "context" and carried on past.
        /// </summary>
        [Fact]
        public void An_unreinstated_mechanical_duty_on_a_thermally_participating_room_outside_the_scope_refuses()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV);

            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_LegacyMV, space_Corridor, FlowClassification.Supply, 20.0);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.False(partOIteration3SystemScope.IsScoped);

            string refusal = Assert.Single(partOIteration3SystemScope.Refusals);

            Assert.Contains(ventilationSystem_LegacyMV.Guid.ToString(), refusal);
            Assert.Contains(space_Corridor.Guid.ToString(), refusal);
            Assert.Contains("Corridor", refusal);
            Assert.Contains("outside the assessed dwellings but is part of the same thermal model", refusal);
            Assert.Contains("removes mechanical ventilation from the WHOLE model", refusal);
        }

        /// <summary>
        /// A terminal that states no airflow, or states nothing, moves no air - so there is nothing for
        /// the no-IZAM sweep to remove and nothing for Candidate B to reinstate. Refusing over one would
        /// refuse models nothing is wrong with.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(0.0)]
        [InlineData(double.NaN)]
        public void A_terminal_with_no_effective_duty_does_not_stop_the_scope(double? designFlowRate_Lps)
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space space_Corridor, out VentilationSystem ventilationSystem_LegacyMV);

            PartOIteration3Fixture.Terminal(adjacencyCluster, ventilationSystem_LegacyMV, space_Corridor, FlowClassification.Supply, designFlowRate_Lps);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.True(partOIteration3SystemScope.IsScoped);
            Assert.Contains(ventilationSystem_LegacyMV.Guid, partOIteration3SystemScope.Guids_Removed);
            Assert.Contains(partOIteration3SystemScope.Notes, x => x.Contains("none of which states an effective design airflow"));
        }

        /// <summary>
        /// Fail closed: a duty that serves no identified room cannot be shown to be outside the thermal
        /// case, and "probably harmless" is not a standard a comparison can be built on.
        /// </summary>
        [Fact]
        public void A_duty_that_serves_no_identified_room_refuses()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem ventilationSystem_LegacyMV);

            VentilationTerminal ventilationTerminal = new("orphan", FlowClassification.Supply, 15.0);

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationSystem_LegacyMV, ventilationTerminal);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.False(partOIteration3SystemScope.IsScoped);
            Assert.Contains(partOIteration3SystemScope.Refusals, x => x.Contains("not related to any space"));
        }

        [Fact]
        public void A_run_that_captured_no_identities_refuses_rather_than_guessing()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> _, out List<Zone> zones, out Space _, out VentilationSystem _);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, [], DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.False(partOIteration3SystemScope.IsScoped);
            Assert.Contains(partOIteration3SystemScope.Refusals, x => x.Contains("captured no ventilation system identities"));
        }

        [Fact]
        public void A_captured_identity_that_is_not_on_the_model_refuses_naming_it()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem _);

            Guid guid = new("dddddddd-dddd-dddd-dddd-dddddddddddd");

            guids_Prepared.Add(guid);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.False(partOIteration3SystemScope.IsScoped);
            Assert.Contains(partOIteration3SystemScope.Refusals, x => x.Contains(guid.ToString()));
        }

        /// <summary>
        /// Membership is decided by guid and by nothing else. Two systems with the same display name -
        /// one built by this iteration, one not - must be told apart.
        /// </summary>
        [Fact]
        public void Two_systems_with_the_same_display_name_are_told_apart_by_identity()
        {
            AdjacencyCluster adjacencyCluster = PartOIteration3Fixture.Design(out List<Guid> guids_Prepared, out List<Zone> zones);

            //A second system with the same unit name as the first dwelling's, carrying no duty.
            VentilationSystem ventilationSystem = PartOIteration3Fixture.VentilationSystem(adjacencyCluster, "MVHR-01", "MVHR", out AirHandlingUnit _);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            Assert.True(partOIteration3SystemScope.IsScoped);
            Assert.Equal([ventilationSystem.Guid], partOIteration3SystemScope.Guids_Removed);
            Assert.DoesNotContain(ventilationSystem.Guid, partOIteration3SystemScope.Guids_Retained);
        }

        [Fact]
        public void No_model_refuses()
        {
            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(null, [Guid.NewGuid()], []);

            Assert.False(partOIteration3SystemScope.IsScoped);
            Assert.Single(partOIteration3SystemScope.Refusals);
        }

        /// <summary>
        /// Ordered before it is handed on, so the working copy, the record and the evidence are the same
        /// on every machine - a dictionary walk is not.
        /// </summary>
        [Fact]
        public void The_retained_and_removed_identities_are_ordered_deterministically()
        {
            AdjacencyCluster adjacencyCluster = Canonical(out List<Guid> guids_Prepared, out List<Zone> zones, out Space _, out VentilationSystem _);

            PartOIteration3SystemScope partOIteration3SystemScope = Query.PartOIteration3SystemScope(adjacencyCluster, guids_Prepared, DwellingSpaceGuids(adjacencyCluster, zones));

            List<Guid> guids_Retained = partOIteration3SystemScope.Guids_Retained;
            List<Guid> guids_Removed = partOIteration3SystemScope.Guids_Removed;

            List<Guid> guids_Retained_Sorted = [.. guids_Retained];
            guids_Retained_Sorted.Sort();

            List<Guid> guids_Removed_Sorted = [.. guids_Removed];
            guids_Removed_Sorted.Sort();

            Assert.Equal(guids_Retained_Sorted, guids_Retained);
            Assert.Equal(guids_Removed_Sorted, guids_Removed);
        }
    }
}
