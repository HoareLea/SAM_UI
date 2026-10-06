// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Communal-corridor reporting in the Part O TM59 result - verification (Pass 5 acceptance).</b>
    /// <para>
    /// The classification authority is SAM: <c>TM59AssessmentReport</c> puts a corridor-bucket check in
    /// <c>CorridorChecks</c> only where the space's InternalCondition is exactly
    /// <see cref="TM59InternalConditionResolver.CommunalCorridorInternalConditionName"/>, and everything else in
    /// that bucket in <c>SupplementaryChecks</c> (SAM.Tests <c>TM59AssessmentReportTests</c> proves both ways).
    /// These tests pin that the SAM_UI summary presents exactly that split - "Communal corridor" from
    /// <c>CorridorChecks</c>, "Other &gt;28 °C checks" from <c>SupplementaryChecks</c> - with no classification
    /// of its own, and in particular none from a space name.
    /// </para>
    /// </summary>
    public class PartOTM59CorridorReportingTests
    {
        private const string source = "SAM_UI.Tests";

        /// <summary>
        /// A space named nothing like a corridor, whose InternalCondition is the TM59 communal corridor, is
        /// reported under Communal corridor and not under the supplementary checks.
        /// </summary>
        [Fact]
        public void TheCommunalCorridorInternalCondition_IsReportedAsACommunalCorridor_WhateverTheSpaceIsCalled()
        {
            Space space = new("Room_99") { InternalCondition = new InternalCondition(TM59InternalConditionResolver.CommunalCorridorInternalConditionName) };

            PartOTM59ResultSummary partOTM59ResultSummary = Summary(space);

            PartOTM59ResultSummary.Fact fact = Assert.Single(partOTM59ResultSummary.Facts, x => x.Label == "Communal corridor");
            Assert.Contains("Significant risk", fact.Value, System.StringComparison.Ordinal);
            Assert.Contains("1 corridor", fact.Value, System.StringComparison.Ordinal);

            Assert.DoesNotContain(partOTM59ResultSummary.Facts, x => x.Label == "Other >28 °C checks");
        }

        /// <summary>
        /// The converse: a space NAMED "Corridor" without the communal-corridor InternalCondition is not reported
        /// as a communal corridor - it is a supplementary, information-only check.
        /// </summary>
        [Fact]
        public void ASpaceNamedCorridor_WithoutThatInternalCondition_IsReportedAsASupplementaryCheck()
        {
            Space space = new("Corridor_5") { InternalCondition = new InternalCondition("TM59_Internal Corridor") };

            PartOTM59ResultSummary partOTM59ResultSummary = Summary(space);

            Assert.DoesNotContain(partOTM59ResultSummary.Facts, x => x.Label == "Communal corridor");

            PartOTM59ResultSummary.Fact fact = Assert.Single(partOTM59ResultSummary.Facts, x => x.Label == "Other >28 °C checks");
            Assert.Contains("1 space", fact.Value, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// The summary's corridor count is SAM's <c>CorridorChecks</c>, one by one - not a count of names.
        /// </summary>
        private static PartOTM59ResultSummary Summary(Space space)
        {
            TMResult corridor = new TM59CorridorResult(space.Name, source, space.Guid.ToString(), TM52BuildingCategory.CategoryII, 8760, 262, 337, false, 8760);

            //One mechanically ventilated occupied space as well, so the report has an occupied-space verdict.
            Space space_Occupied = new("Studio 1");
            TMResult mechanical = new TM59MechanicalVentilationResult(space_Occupied.Name, source, space_Occupied.Guid.ToString(), TM52BuildingCategory.CategoryII, 4740, 142, 100, true, TM59SpaceApplication.Cooking);

            TM59AssessmentReport tM59AssessmentReport = new([space, space_Occupied], [mechanical], null, [corridor], null, source);

            //The same split the summary will present, straight from the authority.
            Assert.Equal(
                space.InternalCondition.Name == TM59InternalConditionResolver.CommunalCorridorInternalConditionName ? 1 : 0,
                tM59AssessmentReport.CorridorChecks.Count);

            return PartOTM59ResultSummary.Create(tM59AssessmentReport, 0, null, new List<PartOTM59ResultSummary.Fact>());
        }
    }
}
