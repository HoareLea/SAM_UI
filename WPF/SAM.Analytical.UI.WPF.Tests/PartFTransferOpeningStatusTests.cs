// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// A calculated transfer flow must never imply that a physical transfer opening exists.
    /// <para>
    /// SAM can conserve air across a dwelling and produce an exact litres-per-second figure for a route
    /// where the model carries no door and no recorded transfer device. Drawn like a route through a real
    /// door, that number tells a reader the air has somewhere to go - which is precisely what has not been
    /// established. <see cref="PartFDoorTransferData.OpeningStatus"/> is what keeps the two apart, and
    /// these tests hold it.
    /// </para>
    /// </summary>
    public class PartFTransferOpeningStatusTests
    {
        /// <summary>
        /// The case that matters most: a flow was calculated, and nothing in the model shows an opening
        /// for it to pass through.
        /// </summary>
        [Fact]
        public void NoDoorAndNoDevice_IsMissingTransferOpening()
        {
            PartFDoorTransferData partFDoorTransferData = new("Studio to Bathroom")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = false,
                TransferDeviceType = PartFTransferDeviceType.NotRepresented,
                RouteStatus = PartFTransferRouteStatus.UniquelyDetermined,
                ComplianceStatus = PartFComplianceStatus.CannotBeDetermined,
                ContinuousDesignTransferFlowRate_Lps = 8,
            };

            Assert.Equal(PartFTransferOpeningStatus.MissingTransferOpening, partFDoorTransferData.OpeningStatus);
            Assert.True(partFDoorTransferData.IsOpeningUnresolved);

            //An exact flow, and still not an established route. That combination is the whole point.
            Assert.Equal(8, partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps);
        }

        /// <summary>A modelled door is the opening the flow was calculated through.</summary>
        [Fact]
        public void ModelledDoor_IsCalculatedViaModelledDoor()
        {
            PartFDoorTransferData partFDoorTransferData = new("D01")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = true,
                RouteStatus = PartFTransferRouteStatus.UniquelyDetermined,
                ComplianceStatus = PartFComplianceStatus.CannotBeDetermined,
            };

            Assert.Equal(PartFTransferOpeningStatus.CalculatedViaModelledDoor, partFDoorTransferData.OpeningStatus);
            Assert.False(partFDoorTransferData.IsOpeningUnresolved);
        }

        /// <summary>A recorded permanent opening is an opening, even with no door aperture modelled.</summary>
        [Fact]
        public void RecordedDevice_IsCalculatedViaPermanentOpening()
        {
            PartFDoorTransferData partFDoorTransferData = new("Open passage")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = false,
                TransferDeviceType = PartFTransferDeviceType.PermanentOpening,
                RouteStatus = PartFTransferRouteStatus.UniquelyDetermined,
                ComplianceStatus = PartFComplianceStatus.CannotBeDetermined,
            };

            Assert.Equal(PartFTransferOpeningStatus.CalculatedViaPermanentOpening, partFDoorTransferData.OpeningStatus);
            Assert.False(partFDoorTransferData.IsOpeningUnresolved);
        }

        /// <summary>A recorded free area that meets paragraph 1.25 is the strongest evidence there is.</summary>
        [Fact]
        public void PassingRecordedFreeArea_IsConfirmedOpening()
        {
            PartFDoorTransferData partFDoorTransferData = new("D01")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = true,
                TransferDeviceType = PartFTransferDeviceType.DoorUndercut,
                ProvidedFreeArea_mm2 = PartFDoorTransferData.NominalEquivalentFreeArea_mm2,
                RouteStatus = PartFTransferRouteStatus.UniquelyDetermined,
                ComplianceStatus = PartFComplianceStatus.Pass,
            };

            Assert.Equal(PartFTransferOpeningStatus.ConfirmedOpening, partFDoorTransferData.OpeningStatus);
            Assert.False(partFDoorTransferData.IsOpeningUnresolved);
        }

        /// <summary>
        /// A split the topology did not fix is unresolved even where the door is real, because what is in
        /// doubt is this route's share rather than the opening.
        /// </summary>
        [Theory]
        [InlineData(PartFTransferRouteStatus.Ambiguous)]
        [InlineData(PartFTransferRouteStatus.AllocationStrategy)]
        [InlineData(PartFTransferRouteStatus.NotCalculable)]
        public void UnfixedSplit_IsAmbiguousRoute(PartFTransferRouteStatus partFTransferRouteStatus)
        {
            PartFDoorTransferData partFDoorTransferData = new("D01")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = true,
                TransferDeviceType = PartFTransferDeviceType.DoorUndercut,
                RouteStatus = partFTransferRouteStatus,
                ComplianceStatus = PartFComplianceStatus.Pass,
            };

            Assert.Equal(PartFTransferOpeningStatus.AmbiguousRoute, partFDoorTransferData.OpeningStatus);
            Assert.True(partFDoorTransferData.IsOpeningUnresolved);
        }

        /// <summary>
        /// A missing opening outranks an ambiguous split. Where there is nothing to cross, which share of
        /// the flow crosses it is not the question to put to the engineer.
        /// </summary>
        [Fact]
        public void MissingOpening_OutranksAmbiguousRoute()
        {
            PartFDoorTransferData partFDoorTransferData = new("Studio to Bathroom")
            {
                IsInternalDwellingDoor = true,
                IsDoorRepresented = false,
                TransferDeviceType = PartFTransferDeviceType.NotRepresented,
                RouteStatus = PartFTransferRouteStatus.Ambiguous,
            };

            Assert.Equal(PartFTransferOpeningStatus.MissingTransferOpening, partFDoorTransferData.OpeningStatus);
        }

        /// <summary>An untouched record reports nothing rather than guessing.</summary>
        [Fact]
        public void UnassessedRoute_IsNotAssessed()
        {
            Assert.Equal(PartFTransferOpeningStatus.NotAssessed, new PartFDoorTransferData("D01").OpeningStatus);
        }
    }
}
