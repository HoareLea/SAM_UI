// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Tests for <see cref="Modify.BuildReportText(PartFCalculator, PartFOperatingMode)"/>: pure text
    /// generation with no WPF dependency, so a model with thousands of spaces can be exercised as a plain
    /// string check rather than through a live report window.
    /// </summary>
    /// <remarks>
    /// The report body comes from SAM.Analytical's shared <see cref="PartFReport"/>, so the same text
    /// reaches the report window, the clipboard, the Grasshopper output and these tests. What this layer
    /// adds is the model-level notes that belong to the run rather than to any one dwelling, and the
    /// wrapping that makes long paragraphs readable.
    /// </remarks>
    public class PartFReportTextTests
    {
        /// <summary>
        /// The report opens with its assumptions, verbatim and before any number, so a reader sees the
        /// basis of the assessment before any result. Locked exactly.
        /// </summary>
        [Fact]
        public void BuildReportText_BeginsWithTheRequiredAssumptions()
        {
            string text = Modify.BuildReportText(Calculator());

            Assert.StartsWith("ASSUMPTIONS\r\n\r\nNew dwelling in England.\r\nApproved Document F, Volume 1, 2021 edition.\r\n", text);
        }

        /// <summary>The dwelling summary reaches the report with its rates and its governing calculation.</summary>
        [Fact]
        public void BuildReportText_ContainsTheDwellingSummary()
        {
            string text = Modify.BuildReportText(Calculator());

            string text_Unwrapped = Unwrap(text);

            Assert.Contains("DWELLING: Flat 1", text_Unwrapped);
            Assert.Contains("Internal floor area:            45.5 m2", text_Unwrapped);
            Assert.Contains("Habitable rooms:                2", text_Unwrapped);
            Assert.Contains("Bedrooms:                       1", text_Unwrapped);
            Assert.Contains("Continuous design airflow:      13 l/s", text_Unwrapped);
            Assert.Contains("Governing calculation:", text_Unwrapped);
        }

        /// <summary>Every schedule the assessment produces reaches the report window.</summary>
        [Theory]
        [InlineData("AIRFLOW SCHEMATIC")]
        [InlineData("DWELLING SUMMARY")]
        [InlineData("SUPPLY TERMINAL SCHEDULE")]
        [InlineData("GENERAL EXTRACT SCHEDULE")]
        [InlineData("LOCAL KITCHEN EXTRACT SCHEDULE")]
        [InlineData("INTERNAL TRANSFER AIR ROUTING (CALCULATED)")]
        [InlineData("DOOR UNDERCUT AND FREE AREA SCHEDULE (PARAGRAPH 1.25 ASSESSMENT)")]
        [InlineData("PURGE VENTILATION ASSESSMENT")]
        [InlineData("COMMISSIONING STATUS")]
        [InlineData("FAILED CHECKS")]
        [InlineData("UNRESOLVED CHECKS")]
        [InlineData("ENGINEERING REVIEW REQUIRED")]
        [InlineData("REGULATORY REFERENCES")]
        [InlineData("OVERALL PART F CONFORMANCE ASSESSMENT")]
        public void BuildReportText_ContainsEverySection(string title)
        {
            Assert.Contains(title, Modify.BuildReportText(Calculator()));
        }

        /// <summary>
        /// The schematic is a DRAWING: its indentation places each branch under the arrow above it. The
        /// wrapper must leave those lines alone, or the diagram ends up pointing at nothing.
        /// </summary>
        [Fact]
        public void BuildReportText_DoesNotWrapTheSchematic()
        {
            PartFCalculator partFCalculator = Calculator();

            //A branch line long enough to exceed the wrap column, from a room with a very long name.
            PartFComplianceResult complianceResult = partFCalculator.DwellingResults[0].ComplianceResult;

            complianceResult.TransferPaths.Add(new PartFDoorTransferData("D01")
            {
                UpstreamSpaceGuid = complianceResult.Terminals[0].SpaceGuid,
                DownstreamSpaceGuid = System.Guid.NewGuid(),
                UpstreamSpaceName = complianceResult.Terminals[0].SpaceName,
                DownstreamSpaceName = new string('B', 140),
                IsDoorRepresented = true,
                IsInternalDwellingDoor = true,
                ContinuousDesignTransferFlowRate_Lps = 5,
            });

            string text = Modify.BuildReportText(partFCalculator);

            //The whole branch survives on one line, however long it is.
            Assert.Contains("5 l/s through D01 " + PartFSchematic.Horizontal + PartFSchematic.Horizontal + PartFSchematic.Horizontal + PartFSchematic.Horizontal + PartFSchematic.ArrowRight + " " + new string('B', 140), text);
        }

        /// <summary>Long prose paragraphs are still wrapped, so a warning does not run several screens wide.</summary>
        [Fact]
        public void BuildReportText_WrapsLongProse()
        {
            PartFCalculator partFCalculator = Calculator();

            //Real words, because a single token longer than the column is deliberately emitted whole - see
            //BuildReportText_DoesNotSplitALongName.
            partFCalculator.DwellingResults[0].Warnings.Add(string.Join(" ", Enumerable.Repeat("warning", 80)));

            string text = Modify.BuildReportText(partFCalculator);

            Assert.All(text.Split('\n'), x => Assert.True(x.TrimEnd('\r').Length <= 200));
        }

        /// <summary>
        /// A word longer than the wrap column is emitted whole rather than split: the long tokens here are
        /// space and zone names, and breaking one in half would make the report name a space that does not
        /// exist.
        /// </summary>
        [Fact]
        public void BuildReportText_DoesNotSplitALongName()
        {
            PartFCalculator partFCalculator = Calculator();

            string name = new('x', 250);

            partFCalculator.UnclassifiedSpaceNames.Add(name);

            Assert.Contains(name, Modify.BuildReportText(partFCalculator));
        }

        /// <summary>Model-level notes that belong to no dwelling still reach the report.</summary>
        [Fact]
        public void BuildReportText_ContainsTheModelLevelNotes()
        {
            PartFCalculator partFCalculator = Calculator();

            partFCalculator.ExcludedZoneNames.Add("Corridor");
            partFCalculator.UnclassifiedSpaceNames.Add("Server Room");

            string text = Unwrap(Modify.BuildReportText(partFCalculator));

            Assert.Contains("MODEL NOTES", text);
            Assert.Contains("Zones not sized as dwellings: Corridor", text);
            Assert.Contains("Unclassified space(s): Server Room", text);
        }

        /// <summary>A run that sized nothing says so, rather than producing an empty report.</summary>
        [Fact]
        public void BuildReportText_WithNoDwellings_SaysSo()
        {
            string text = Modify.BuildReportText(new PartFCalculator(null));

            Assert.StartsWith("ASSUMPTIONS", text);
            Assert.Contains("No dwelling was assessed.", text);
        }

        /// <summary>The result is an assessment. It is never described as a certificate.</summary>
        [Fact]
        public void BuildReportText_IsNeverCalledACertificate()
        {
            string text = Modify.BuildReportText(Calculator());

            Assert.Contains("Part F conformance assessment", text);
            Assert.DoesNotContain("certificate of compliance", text);
            Assert.DoesNotContain("certifies", text);
        }

        /// <summary>Each operating mode names itself, so two conditions can never be confused on screen.</summary>
        [Theory]
        [InlineData(PartFOperatingMode.ContinuousDesign, "CONTINUOUS DESIGN")]
        [InlineData(PartFOperatingMode.HighBoost, "HIGH/BOOST")]
        [InlineData(PartFOperatingMode.Setback, "SETBACK")]
        [InlineData(PartFOperatingMode.MeasuredCommissioning, "MEASURED COMMISSIONING")]
        public void BuildReportText_NamesItsOperatingMode(PartFOperatingMode partFOperatingMode, string expected)
        {
            Assert.Contains("AIRFLOW SCHEMATIC " + PartFSchematic.EmDash + " " + expected, Modify.BuildReportText(Calculator(), partFOperatingMode));
        }

        /// <summary>
        /// A large model must not be truncated anywhere in this layer: every dwelling's every section has
        /// to survive to the clipboard, because Copy All copies exactly what is shown.
        /// </summary>
        [Fact]
        public void BuildReportText_IsNotTruncatedForALargeModel()
        {
            PartFCalculator partFCalculator = new(null);

            for (int i = 0; i < 200; i++)
            {
                partFCalculator.DwellingResults.Add(Dwelling(string.Format("Flat {0}", i)));
            }

            string text = Modify.BuildReportText(partFCalculator);

            Assert.Contains("200 dwelling(s) assessed", text);
            Assert.Contains("DWELLING: Flat 0", text);
            Assert.Contains("DWELLING: Flat 199", text);
            Assert.Equal(200, Count(text, "OVERALL PART F CONFORMANCE ASSESSMENT"));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static string Unwrap(string text)
        {
            //Hard wrapping can legitimately break a line mid-phrase at the column width, which is a valid
            //line break rather than a content bug. Content-presence checks read the unwrapped text so a
            //wrap point cannot fail them.
            return text.Replace("\r", string.Empty).Replace("\n", " ");
        }

        private static int Count(string text, string value)
        {
            int result = 0;
            int index = 0;

            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) != -1)
            {
                result++;
                index += value.Length;
            }

            return result;
        }

        private static PartFDwellingResult Dwelling(string name)
        {
            PartFDwellingResult result = new(name)
            {
                SpaceNames = ["Bedroom 1", "Bathroom 1"],
                HabitableRoomCount = 2,
                HabitableRoomNames = ["Bedroom 1"],
                BedroomCount = 1,
                InternalFloorArea_M2 = 45.5,
                ContinuousDesignSystemRate_Lps = 13,
                SetbackSystemRate_Lps = 3.9,
                SetbackFlowRateFactor = 0.3,
                TotalSupply_Lps = 13,
                TotalExtract_Lps = 13,
                OneHabitableRoomRuleApplied = false,
            };

            PartFComplianceResult complianceResult = new(name)
            {
                ContinuousDesignSystemRate_Lps = 13,
                TotalContinuousSupply_Lps = 13,
                TotalContinuousExtract_Lps = 13,
            };

            complianceResult.Terminals.Add(new PartFVentilationTerminalRequirement("Bedroom 1 - supply", System.Guid.NewGuid(), PartFTerminalRole.Supply)
            {
                SpaceName = "Bedroom 1",
                ContinuousDesignFlowRate_Lps = 13,
                HighFlowRate_Lps = 13,
                SetbackFlowRate_Lps = 3.9,
                IsInBalancedFlow = true,
                SourceReference = "paragraph 1.67",
            });

            complianceResult.Terminals.Add(new PartFVentilationTerminalRequirement("Bathroom 1 - general extract", System.Guid.NewGuid(), PartFTerminalRole.GeneralExtract)
            {
                SpaceName = "Bathroom 1",
                ContinuousDesignFlowRate_Lps = 13,
                HighFlowRate_Lps = 13,
                SetbackFlowRate_Lps = 3.9,
                MinimumRequiredFlowRate_Lps = 8,
                IsInBalancedFlow = true,
                SourceReference = "paragraph 1.70",
            });

            complianceResult.AddCheck(new PartFComplianceCheck("Ventilation controls", "Approved Document F, Volume 1: Dwellings (2021 edition), paragraph 1.33", "Ventilation is controllable.")
            {
                Status = PartFComplianceStatus.CannotBeDetermined,
                Category = "System design and installation",
            });

            complianceResult.Resolve();

            result.ComplianceResult = complianceResult;

            return result;
        }

        private static PartFCalculator Calculator()
        {
            PartFCalculator result = new(null);

            result.DwellingResults.Add(Dwelling("Flat 1"));

            return result;
        }
    }
}
