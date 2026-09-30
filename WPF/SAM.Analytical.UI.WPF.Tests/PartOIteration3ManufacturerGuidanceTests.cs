// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// SAM#123: the "Selected product - manufacturer guidance" Iteration 3 mode has its own documents beside B0's
    /// and B4's, leaves theirs exactly as they were, and labels itself as manufacturer guidance.
    /// </summary>
    public class PartOIteration3ManufacturerGuidanceTests
    {
        [Fact]
        public void TheMode_WritesItsOwnDocuments_AndLeavesB0AndB4Unchanged()
        {
            PartOIteration3Paths paths_B0 = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd");
            PartOIteration3Paths paths_B4 = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductCooling);
            PartOIteration3Paths paths_MG = PartOIteration3Paths.Create(PartOIteration3Fixture.SimulationContext("C:\\out", "Flat1"), "C:\\out\\Flat1.tsd", PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance);

            //A reference in a legacy flat folder: that folder is the Part O root, and Iteration 3 writes into its own
            //folder beneath it.
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3B.tpd", paths_B0.Path_TPD);
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3B4.tpd", paths_B4.Path_TPD);
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3BMG.tpd", paths_MG.Path_TPD);
            Assert.Equal("C:\\out\\Iteration3\\diagnostics\\Flat1-It3BMG-OperatingAirFlow.csv", paths_MG.Path_OperatingAirFlow);
            Assert.Equal("C:\\out\\Iteration3\\tas\\Flat1-It3BMG-Bridge.tsd", paths_MG.Path_TSD_Bridge);
        }

        [Fact]
        public void TheMode_LabelsItselfAsManufacturerGuidance()
        {
            Assert.Contains("manufacturer guidance", Core.Query.Description(PartOIteration3BehaviourMode.SelectedProductManufacturerGuidance));
            Assert.Equal("Parity (foundation control)", Core.Query.Description(PartOIteration3BehaviourMode.Parity));
            Assert.Equal("Selected product cooling module (B0 + cooling)", Core.Query.Description(PartOIteration3BehaviourMode.SelectedProductCooling));
        }
    }
}
