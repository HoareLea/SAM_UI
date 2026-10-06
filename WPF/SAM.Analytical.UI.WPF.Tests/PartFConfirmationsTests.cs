// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Analytical.UI.WPF;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regression tests for the persistence of the clause-level check confirmations a person records in the
    /// Part F assessment window.
    /// <para>
    /// A confirmation the person WITHDREW must not come back. Unchecking a previously confirmed check returns
    /// it to its calculated status in the grid, but the record still reached <c>PersistConfirmations</c>,
    /// which wrote <c>UserConfirmed</c> back unconditionally - so the next calculation reinstated a
    /// confirmation the person had explicitly removed. The fix stores <c>NotAssessed</c> for a withdrawn
    /// check while keeping the supporting notes, and still stores <c>UserConfirmed</c> for one that is still
    /// confirmed (including one the guard redirected after a calculated failure).
    /// </para>
    /// </summary>
    public class PartFConfirmationsTests
    {
        [Fact]
        public void WithdrawnConfirmation_IsPersistedAsNotAssessed()
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Zone("Flat 1"));

            PartFComplianceCheck check = new("System designed and installed to minimise noise", "source", "requirement")
            {
                CalculatedStatus = PartFComplianceStatus.CannotBeDetermined,
                Status = PartFComplianceStatus.CannotBeDetermined,
                UserEvidence = "Acoustic report ref 123",
                ConfirmedBy = "A. Engineer",
                ConfirmationDate = "2026-08-17",
            };

            PartFComplianceResult complianceResult = new("Flat 1")
            {
                Checks = [check],
            };

            Modify.PersistConfirmations(adjacencyCluster, new PartFDwellingResult("Flat 1") { ComplianceResult = complianceResult }, complianceResult);

            PartFCommissioningData partFCommissioningData = adjacencyCluster.GetZones()?[0]?.GetValue<PartFCommissioningData>(ZoneParameter.PartFCommissioningData);

            Assert.NotNull(partFCommissioningData);

            PartFComplianceCheck persisted = Assert.Single(partFCommissioningData.InstallationChecks);

            Assert.Equal(PartFComplianceStatus.NotAssessed, persisted.Status);
            Assert.Equal("Acoustic report ref 123", persisted.UserEvidence);
            Assert.Equal("A. Engineer", persisted.ConfirmedBy);
        }

        [Fact]
        public void ConfirmedCheck_IsPersistedAsUserConfirmed()
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Zone("Flat 1"));

            PartFComplianceCheck check = new("System designed and installed to minimise noise", "source", "requirement")
            {
                CalculatedStatus = PartFComplianceStatus.CannotBeDetermined,
                Status = PartFComplianceStatus.UserConfirmed,
                UserEvidence = "Acoustic report ref 123",
            };

            PartFComplianceResult complianceResult = new("Flat 1")
            {
                Checks = [check],
            };

            Modify.PersistConfirmations(adjacencyCluster, new PartFDwellingResult("Flat 1") { ComplianceResult = complianceResult }, complianceResult);

            PartFCommissioningData partFCommissioningData = adjacencyCluster.GetZones()?[0]?.GetValue<PartFCommissioningData>(ZoneParameter.PartFCommissioningData);

            Assert.NotNull(partFCommissioningData);

            PartFComplianceCheck persisted = Assert.Single(partFCommissioningData.InstallationChecks);

            Assert.Equal(PartFComplianceStatus.UserConfirmed, persisted.Status);
        }

        /// <summary>
        /// A check whose ONLY remaining record is its note is still recorded. The note is a person's answer,
        /// and the check has to reach <c>PersistConfirmations</c> so the withdrawn confirmation it once held
        /// is stored as <c>NotAssessed</c> - otherwise the previous <c>UserConfirmed</c> record survives in
        /// the zone and the next calculation reinstates the withdrawal.
        /// </summary>
        [Fact]
        public void NotesOnlyCheck_IsPersistedAsNotAssessed()
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Zone("Flat 1"));

            PartFComplianceCheck check = new("System designed and installed to minimise noise", "source", "requirement")
            {
                CalculatedStatus = PartFComplianceStatus.CannotBeDetermined,
                Status = PartFComplianceStatus.CannotBeDetermined,
                Notes = "Not checked on site",
            };

            PartFComplianceResult complianceResult = new("Flat 1")
            {
                Checks = [check],
            };

            Modify.PersistConfirmations(adjacencyCluster, new PartFDwellingResult("Flat 1") { ComplianceResult = complianceResult }, complianceResult);

            PartFCommissioningData partFCommissioningData = adjacencyCluster.GetZones()?[0]?.GetValue<PartFCommissioningData>(ZoneParameter.PartFCommissioningData);

            Assert.NotNull(partFCommissioningData);

            PartFComplianceCheck persisted = Assert.Single(partFCommissioningData.InstallationChecks);

            Assert.Equal(PartFComplianceStatus.NotAssessed, persisted.Status);
            Assert.Equal("Not checked on site", persisted.Notes);
        }

        /// <summary>
        /// The person and date of a withdrawn confirmation count on their own too, for the same reason the
        /// note does: the answer must reach persistence so the withdrawal is stored.
        /// </summary>
        [Fact]
        public void PersonAndDateOnlyCheck_IsPersistedAsNotAssessed()
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Zone("Flat 1"));

            PartFComplianceCheck check = new("System designed and installed to minimise noise", "source", "requirement")
            {
                CalculatedStatus = PartFComplianceStatus.CannotBeDetermined,
                Status = PartFComplianceStatus.CannotBeDetermined,
                ConfirmedBy = "A. Engineer",
                ConfirmationDate = "2026-08-17",
            };

            PartFComplianceResult complianceResult = new("Flat 1")
            {
                Checks = [check],
            };

            Modify.PersistConfirmations(adjacencyCluster, new PartFDwellingResult("Flat 1") { ComplianceResult = complianceResult }, complianceResult);

            PartFCommissioningData partFCommissioningData = adjacencyCluster.GetZones()?[0]?.GetValue<PartFCommissioningData>(ZoneParameter.PartFCommissioningData);

            Assert.NotNull(partFCommissioningData);

            PartFComplianceCheck persisted = Assert.Single(partFCommissioningData.InstallationChecks);

            Assert.Equal(PartFComplianceStatus.NotAssessed, persisted.Status);
            Assert.Equal("A. Engineer", persisted.ConfirmedBy);
            Assert.Equal("2026-08-17", persisted.ConfirmationDate);
        }
    }
}
