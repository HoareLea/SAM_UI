// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>The resultant temperatures the comparison uses are the ones the TM59 assessment read</b> - the
    /// same key, off the same spaces, keyed back to the design through the same
    /// <c>SimulationSpaceMap</c>.
    ///
    /// <para><b>Why the keying is the whole point</b></para>
    /// <para>
    /// The A/B comparison joins two series by design space guid. A capture that resolved a simulated room
    /// to the wrong design room would produce a complete, finite, entirely plausible comparison of the
    /// wrong pair of rooms - and TM59's own verdicts, which come through the same map, would agree with
    /// it. Two flats' "Bedroom 2" is exactly the case that breaks a name-based answer, so it is the case
    /// tested.
    /// </para>
    /// </summary>
    public class PartOIteration3CaptureTests
    {
        private const string Key_Stable = "SimulationKey";

        private const string Key_Series = "Resultant Temperature";

        /// <summary>
        /// A space carrying an engine-stable identity in its own parameter set - which is the shape the
        /// TAS conversion produces and the shape <c>SimulationSpaceMap</c> matches on.
        /// </summary>
        private static Space Space(string name, string key)
        {
            Space result = new(name, new Geometry.Spatial.Point3D(0, 0, 0));

            Core.ParameterSet parameterSet = new("Test");

            parameterSet.Add(Key_Stable, key);

            result.Add(parameterSet);

            return result;
        }

        private static Space Space_Simulation(string name, string key, double value, int count = 4)
        {
            Space result = Space(name, key);

            JsonArray jsonArray = [];

            for (int i = 0; i < count; i++)
            {
                jsonArray.Add(JsonValue.Create(value + i));
            }

            Series(result, jsonArray);

            return result;
        }

        private static void Series(Space space, JsonArray jsonArray)
        {
            Core.ParameterSet parameterSet = new("Test");

            parameterSet.Add(Key_Series, jsonArray);

            space.Add(parameterSet);
        }

        private static SimulationSpaceMap Map(IEnumerable<Space> spaces_Design, IEnumerable<Space> spaces_Simulation)
        {
            return new SimulationSpaceMap(spaces_Design, spaces_Simulation, x => Core.Query.TryGetValue(x, Key_Stable, out string result) ? result : null);
        }

        [Fact]
        public void Two_rooms_with_the_same_name_are_captured_against_their_own_design_identities()
        {
            Space space_Design_1 = Space("Bedroom 2", "flat1-bedroom");
            Space space_Design_2 = Space("Bedroom 2", "flat2-bedroom");

            Space space_Simulation_1 = Space_Simulation("Bedroom 2", "flat1-bedroom", 20.0);
            Space space_Simulation_2 = Space_Simulation("Bedroom 2", "flat2-bedroom", 30.0);

            List<Space> spaces_Simulation = [space_Simulation_1, space_Simulation_2];

            Dictionary<Guid, double[]> result = PartOTM59Assessment.CaptureResultantTemperatures(
                spaces_Simulation,
                Map([space_Design_1, space_Design_2], spaces_Simulation),
                Key_Series,
                null);

            Assert.Equal(2, result.Count);
            Assert.Equal([20.0, 21.0, 22.0, 23.0], result[space_Design_1.Guid]);
            Assert.Equal([30.0, 31.0, 32.0, 33.0], result[space_Design_2.Guid]);
        }

        /// <summary>
        /// A full annual series per room is what makes a five-thousand-space capture expensive, so a
        /// caller that knows its rooms says so and nothing else is read.
        /// </summary>
        [Fact]
        public void Only_the_named_rooms_are_captured()
        {
            Space space_Design_1 = Space("Bedroom 2", "flat1-bedroom");
            Space space_Design_2 = Space("Bedroom 2", "flat2-bedroom");

            List<Space> spaces_Simulation = [Space_Simulation("Bedroom 2", "flat1-bedroom", 20.0), Space_Simulation("Bedroom 2", "flat2-bedroom", 30.0)];

            Dictionary<Guid, double[]> result = PartOTM59Assessment.CaptureResultantTemperatures(
                spaces_Simulation,
                Map([space_Design_1, space_Design_2], spaces_Simulation),
                Key_Series,
                [space_Design_2.Guid]);

            Assert.Single(result);
            Assert.True(result.ContainsKey(space_Design_2.Guid));
        }

        [Fact]
        public void A_simulated_room_that_resolves_to_no_design_room_contributes_nothing()
        {
            Space space_Design = Space("Bedroom 2", "flat1-bedroom");

            //A key no design space answers to - a results file from a different batch.
            List<Space> spaces_Simulation = [Space_Simulation("Bedroom 2", "somewhere-else", 20.0)];

            Dictionary<Guid, double[]> result = PartOTM59Assessment.CaptureResultantTemperatures(
                spaces_Simulation,
                Map([space_Design], spaces_Simulation),
                Key_Series,
                null);

            Assert.Empty(result);
        }

        [Fact]
        public void A_room_with_no_series_contributes_nothing_rather_than_an_empty_one()
        {
            Space space_Design = Space("Bedroom 2", "flat1-bedroom");

            List<Space> spaces_Simulation = [Space("Bedroom 2", "flat1-bedroom")];

            Dictionary<Guid, double[]> result = PartOTM59Assessment.CaptureResultantTemperatures(
                spaces_Simulation,
                Map([space_Design], spaces_Simulation),
                Key_Series,
                null);

            Assert.Empty(result);
        }

        /// <summary>
        /// An hour that is not a JSON number reads as NaN, never as a zero - so the comparison refuses
        /// the room rather than silently treating an unknown hour as 0 degrees. <c>true</c> in
        /// particular converts cleanly to 1 through the ordinary conversion, which is why the node's own
        /// JSON kind is asked first.
        /// </summary>
        [Fact]
        public void An_hour_that_is_not_a_number_reads_as_not_a_number_rather_than_as_zero()
        {
            Space space_Design = Space("Bedroom 2", "flat1-bedroom");

            Space space_Simulation = Space("Bedroom 2", "flat1-bedroom");

            Series(space_Simulation, [JsonValue.Create(20.0), JsonValue.Create(true), JsonValue.Create("21"), null]);

            List<Space> spaces_Simulation = [space_Simulation];

            Dictionary<Guid, double[]> result = PartOTM59Assessment.CaptureResultantTemperatures(
                spaces_Simulation,
                Map([space_Design], spaces_Simulation),
                Key_Series,
                null);

            double[] values = result[space_Design.Guid];

            Assert.Equal(4, values.Length);
            Assert.Equal(20.0, values[0]);
            Assert.True(double.IsNaN(values[1]));
            Assert.True(double.IsNaN(values[2]));
            Assert.True(double.IsNaN(values[3]));

            //And the comparison refuses such a room rather than computing over the hours that survived.
            PartOIteration3Comparison partOIteration3Comparison = PartOIteration3Comparison.Create(
                [new PartOIteration3Room(space_Design.Guid, "Bedroom 2", Guid.Empty, "Flat 1")],
                result,
                new Dictionary<Guid, double[]> { { space_Design.Guid, [20, 20, 20, 20] } },
                [],
                out List<string> refusals);

            Assert.Null(partOIteration3Comparison);
            Assert.Contains(refusals, x => x.Contains("not finite"));
        }

        [Fact]
        public void An_empty_capture_scope_captures_nothing()
        {
            Space space_Design = Space("Bedroom 2", "flat1-bedroom");

            List<Space> spaces_Simulation = [Space_Simulation("Bedroom 2", "flat1-bedroom", 20.0)];

            Assert.Empty(PartOTM59Assessment.CaptureResultantTemperatures(spaces_Simulation, Map([space_Design], spaces_Simulation), Key_Series, []));
        }
    }
}
