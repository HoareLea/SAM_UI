// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Tests for the airflow overlay's appearance definitions.
    /// </summary>
    /// <remarks>
    /// These are the rules that keep the drawing readable: one central definition rather than colours
    /// repeated in the renderer and again in the legend, and never colour alone, so the overlay survives
    /// printing in monochrome and a reader with colour vision deficiency.
    /// </remarks>
    public class PartFAirflowAppearanceTests
    {
        /// <summary>Every kind of air the overlay draws has a definition, and there are no duplicates.</summary>
        [Fact]
        public void EveryAirType_HasExactlyOneDefinition()
        {
            List<PartFAirflowAppearance.AirType> airTypes = [.. System.Enum.GetValues(typeof(PartFAirflowAppearance.AirType)).Cast<PartFAirflowAppearance.AirType>()];

            Assert.Equal(airTypes.Count, PartFAirflowAppearance.All.Count);

            foreach (PartFAirflowAppearance.AirType airType in airTypes)
            {
                Assert.NotNull(PartFAirflowAppearance.Get(airType));
            }

            Assert.Equal(PartFAirflowAppearance.All.Count, PartFAirflowAppearance.All.Select(x => x.Type).Distinct().Count());
        }

        /// <summary>
        /// The labels are the ones an engineer reads on a ventilation drawing, and they are what makes the
        /// overlay legible without any colour at all.
        /// </summary>
        [Theory]
        [InlineData(PartFAirflowAppearance.AirType.Supply, "SUP")]
        [InlineData(PartFAirflowAppearance.AirType.GeneralExtract, "EX")]
        [InlineData(PartFAirflowAppearance.AirType.LocalKitchenExtract, "KEX")]
        [InlineData(PartFAirflowAppearance.AirType.TransferAir, "TRA")]
        [InlineData(PartFAirflowAppearance.AirType.OutdoorAir, "ODA")]
        [InlineData(PartFAirflowAppearance.AirType.ExhaustAir, "EHA")]
        public void Abbreviation_IsTheDrawingConvention(PartFAirflowAppearance.AirType airType, string expected)
        {
            Assert.Equal(expected, PartFAirflowAppearance.Get(airType).Abbreviation);
        }

        /// <summary>
        /// The German ventilation colour convention: supply red, extract yellow, outdoor air green,
        /// exhaust brown. Transfer air has no convention of its own and is grey.
        /// </summary>
        [Fact]
        public void Colours_FollowTheVentilationConvention()
        {
            PartFAirflowAppearance supply = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.Supply);
            PartFAirflowAppearance extract = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.GeneralExtract);
            PartFAirflowAppearance outdoor = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.OutdoorAir);
            PartFAirflowAppearance exhaust = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.ExhaustAir);
            PartFAirflowAppearance transfer = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.TransferAir);

            //Red dominant for supply, yellow (red and green high, blue low) for extract.
            Assert.True(supply.Red > supply.Green && supply.Red > supply.Blue);
            Assert.True(extract.Red > 200 && extract.Green > 150 && extract.Blue < 80);
            Assert.True(outdoor.Green > outdoor.Red && outdoor.Green > outdoor.Blue);
            Assert.True(exhaust.Red > exhaust.Green && exhaust.Green > exhaust.Blue);

            //Grey: the three components equal.
            Assert.Equal(transfer.Red, transfer.Green);
            Assert.Equal(transfer.Green, transfer.Blue);
        }

        /// <summary>
        /// Local kitchen extract shares the extract colour, because that is what it is, but must be
        /// distinguishable from general extract WITHOUT colour: paragraph 1.17a and paragraph 1.17b to
        /// 1.17d are different requirements and a drawing has to show which is which.
        /// </summary>
        [Fact]
        public void LocalKitchenExtract_IsDistinguishableFromGeneralExtractWithoutColour()
        {
            PartFAirflowAppearance general = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.GeneralExtract);
            PartFAirflowAppearance local = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.LocalKitchenExtract);

            //Same colour...
            Assert.Equal(general.Hex, local.Hex);

            //...and every other cue different.
            Assert.NotEqual(general.Abbreviation, local.Abbreviation);
            Assert.NotEqual(general.LinePattern, local.LinePattern);
            Assert.True(local.Thickness > general.Thickness);
        }

        /// <summary>
        /// No two air types share a colour AND a line pattern. Two flows that look identical in monochrome
        /// would be indistinguishable on a printed drawing.
        /// </summary>
        [Fact]
        public void NoTwoAirTypes_ShareBothAColourAndALinePattern()
        {
            List<string> keys = [.. PartFAirflowAppearance.All.Select(x => string.Format("{0}|{1}", x.Hex, x.LinePattern))];

            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        /// <summary>Every air type carries a text abbreviation and a terminal symbol, not colour alone.</summary>
        [Fact]
        public void EveryAirType_CarriesATextAndASymbol()
        {
            Assert.All(PartFAirflowAppearance.All, x =>
            {
                Assert.False(string.IsNullOrWhiteSpace(x.Abbreviation));
                Assert.False(string.IsNullOrWhiteSpace(x.Name));
                Assert.False(string.IsNullOrWhiteSpace(x.TerminalSymbol));
                Assert.True(x.Thickness > 0);
            });
        }

        /// <summary>
        /// The terminal role maps to the right appearance. A local kitchen extract terminal is never drawn
        /// as general extract.
        /// </summary>
        [Theory]
        [InlineData(PartFTerminalRole.Supply, "SUP")]
        [InlineData(PartFTerminalRole.GeneralExtract, "EX")]
        [InlineData(PartFTerminalRole.LocalKitchenExtract, "KEX")]
        public void TerminalRole_MapsToItsOwnAppearance(PartFTerminalRole partFTerminalRole, string expected)
        {
            Assert.Equal(expected, PartFAirflowAppearance.Get(partFTerminalRole).Abbreviation);
        }

        /// <summary>
        /// Every arrow shows its exact value. Thickness conveys magnitude only approximately, and a reader
        /// scheduling equipment needs the number.
        /// </summary>
        [Fact]
        public void Label_CarriesTheExactRate()
        {
            Assert.Equal("SUP 30.0 l/s", PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.Supply).Label(30));
            Assert.Equal("KEX 22.0 l/s", PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.LocalKitchenExtract).Label(22));
            Assert.Equal("TRA 8.0 l/s", PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.TransferAir).Label(8));
            Assert.Equal("EX 8.0 l/s", PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.GeneralExtract).Label(8));
        }

        /// <summary>A rate that was never calculated says so rather than being drawn as zero.</summary>
        [Fact]
        public void Label_WithNoRate_SaysSoRatherThanShowingZero()
        {
            string label = PartFAirflowAppearance.Get(PartFAirflowAppearance.AirType.TransferAir).Label(null);

            Assert.Equal("TRA not calculated", label);
            Assert.DoesNotContain("0", label);
        }

        /// <summary>
        /// Every compliance status carries a symbol as well as a colour, so a failure is visible in
        /// monochrome, and no two statuses share a symbol where the distinction matters.
        /// </summary>
        [Fact]
        public void EveryStatus_CarriesASymbolAsWellAsAColour()
        {
            foreach (PartFComplianceStatus partFComplianceStatus in System.Enum.GetValues(typeof(PartFComplianceStatus)))
            {
                (string symbol, string description, byte _, byte _, byte _) = PartFAirflowAppearance.Status(partFComplianceStatus);

                Assert.False(string.IsNullOrWhiteSpace(symbol));
                Assert.False(string.IsNullOrWhiteSpace(description));
            }

            //A failure and an unresolved check must never look the same: one is a decision, the other is a
            //question, and an engineer acts differently on each.
            Assert.NotEqual(
                PartFAirflowAppearance.Status(PartFComplianceStatus.Fail).Symbol,
                PartFAirflowAppearance.Status(PartFComplianceStatus.CannotBeDetermined).Symbol);

            Assert.NotEqual(
                PartFAirflowAppearance.Status(PartFComplianceStatus.Fail).Symbol,
                PartFAirflowAppearance.Status(PartFComplianceStatus.EngineeringReviewRequired).Symbol);
        }

        /// <summary>
        /// A pass is drawn neutrally rather than green: the ordinary case does not need to shout, and
        /// reserving strong colour for problems is what makes a problem visible.
        /// </summary>
        [Fact]
        public void Pass_IsNeutralAndFailIsStrong()
        {
            (string _, string _, byte red_Pass, byte green_Pass, byte blue_Pass) = PartFAirflowAppearance.Status(PartFComplianceStatus.Pass);
            (string _, string _, byte red_Fail, byte green_Fail, byte blue_Fail) = PartFAirflowAppearance.Status(PartFComplianceStatus.Fail);

            //Neutral: the three components equal.
            Assert.Equal(red_Pass, green_Pass);
            Assert.Equal(green_Pass, blue_Pass);

            //Strong red for a failure.
            Assert.True(red_Fail > 150 && green_Fail < 80 && blue_Fail < 80);
        }

        /// <summary>The legend order runs with the air: outdoor in, what the system does, what leaves.</summary>
        [Fact]
        public void LegendOrder_RunsWithTheAir()
        {
            List<PartFAirflowAppearance.AirType> order = [.. PartFAirflowAppearance.All.Select(x => x.Type)];

            Assert.Equal(PartFAirflowAppearance.AirType.OutdoorAir, order.First());
            Assert.Equal(PartFAirflowAppearance.AirType.ExhaustAir, order.Last());
        }
    }
}
