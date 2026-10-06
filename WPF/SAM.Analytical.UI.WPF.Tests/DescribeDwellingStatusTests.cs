// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Tests for <see cref="MapInternalConditionsControl.DescribeDwellingStatus(bool?)"/>, the tooltip
    /// wording for a zone's Is Dwelling state. Must match PartFCalculator.SelectDwellingZones exactly:
    /// with the flag not set, the outcome depends on whether any OTHER zone in the category carries it.
    /// </summary>
    public class DescribeDwellingStatusTests
    {
        [Fact]
        public void True_DescribesAsYes()
        {
            Assert.Equal("yes", MapInternalConditionsControl.DescribeDwellingStatus(true));
        }

        [Fact]
        public void False_DescribesAsNo()
        {
            Assert.Equal("no", MapInternalConditionsControl.DescribeDwellingStatus(false));
        }

        /// <summary>
        /// Not set must explain both outcomes SelectDwellingZones can actually produce for an unmarked
        /// zone - excluded when mixed with marked zones, included when every zone in the category is
        /// unmarked (legacy category-only mode) - not just the mixed-category case.
        /// </summary>
        [Fact]
        public void Null_ExplainsBothMixedAndLegacyOutcomes()
        {
            string description = MapInternalConditionsControl.DescribeDwellingStatus(null);

            Assert.Contains("not set", description);
            Assert.Contains("excluded", description);
            Assert.Contains("unmarked zones like this one are excluded", description);
            Assert.Contains("every zone in the category is included", description);
            Assert.Contains("legacy category-only mode", description);
        }
    }
}
