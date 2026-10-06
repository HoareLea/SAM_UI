// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Analytical.UI;
using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// What the Part O UI offers and what it shows: the canonical ventilation vocabulary, the dwelling scope,
    /// the separation of design airflow from equipment capacity, and the three catalogue states.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class PartOPresentationTests
    {
        // ---------------------------------------------------------------------------------------------
        // Ventilation strategy vocabulary
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// The picker offers exactly the two canonical words, and nothing else. The long-form synonyms
        /// <c>Query.PartOVentilationMode</c> also accepts - <c>NaturalVentilation</c> among them - prepare
        /// successfully and are then refused by every space at assessment, so they must be unreachable from
        /// the UI.
        /// </summary>
        [Fact]
        public void ThePicker_OffersOnlyTheCanonicalStrategies()
        {
            List<PartOVentilationStrategyOption> options = PartOVentilationStrategyOption.Options;

            Assert.Equal(2, options.Count);

            List<string> ventilationStrategies = options.ConvertAll(x => x.VentilationStrategy);

            Assert.Contains("NV", ventilationStrategies);
            Assert.Contains("MVHR", ventilationStrategies);

            //Named individually rather than by a count alone: a regression that added "NaturalVentilation"
            //beside them would still have kept two canonical entries.
            Assert.DoesNotContain("NaturalVentilation", ventilationStrategies);
            Assert.DoesNotContain("Natural Ventilation", ventilationStrategies);
            Assert.DoesNotContain("MVRE", ventilationStrategies);
            Assert.DoesNotContain("MV", ventilationStrategies);
            Assert.DoesNotContain("UV", ventilationStrategies);
        }

        /// <summary>
        /// <b>The anti-drift lock on the two words.</b> Each option's word is read back through
        /// <c>SAM.Analytical.Query.PartOVentilationMode</c> and must map to the very route the option says it
        /// is for, and to the route <c>Query.PartOIterationVentilationMode</c> says its iteration is defined
        /// over. The UI states two words; SAM decides what they mean, and this is where the two are checked
        /// against each other rather than assumed to agree.
        /// </summary>
        [Fact]
        public void EachOptionsWord_IsReadBackBySAMAsThatOptionsRoute()
        {
            foreach (PartOVentilationStrategyOption option in PartOVentilationStrategyOption.Options)
            {
                PartOVentilationMode partOVentilationMode = Analytical.Query.PartOVentilationMode(option.VentilationStrategy, out string refusal);

                Assert.Null(refusal);
                Assert.Equal(option.PartOVentilationMode, partOVentilationMode);

                //And the iteration it travels with is the base configuration for that same route, which is
                //what Modify.PreparePartOIteration requires of the pairing.
                Assert.Equal(partOVentilationMode, Analytical.Query.PartOIterationVentilationMode(option.PartOIteration, out string _));
            }
        }

        /// <summary>
        /// Only the two base provisions are offered. <c>AcousticRestricted</c> and <c>ActiveTrimCooling</c>
        /// are named in the enum but have no operating assumptions written, so preparing either refuses -
        /// offering them would be offering a guaranteed refusal.
        /// </summary>
        [Fact]
        public void ThePicker_OffersOnlyTheTwoBaseProvisions()
        {
            List<PartOIteration> partOIterations = PartOVentilationStrategyOption.Options.ConvertAll(x => x.PartOIteration);

            Assert.Contains(PartOIteration.BasePassive, partOIterations);
            Assert.Contains(PartOIteration.BaseNaturalVentilation, partOIterations);

            Assert.DoesNotContain(PartOIteration.AcousticRestricted, partOIterations);
            Assert.DoesNotContain(PartOIteration.ActiveTrimCooling, partOIterations);
            Assert.DoesNotContain(PartOIteration.Undefined, partOIterations);
        }

        // ---------------------------------------------------------------------------------------------
        // Dwelling scope
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A zone explicitly marked not a dwelling - the acceptance fixture's <c>Corridor</c> - stays outside
        /// the Part O dwelling preparation scope, and the window says so rather than giving it a strategy.
        /// <para>
        /// The scope itself is <c>Query.PartFDwellingZones</c>' decision; what is pinned here is that the
        /// window asks it and reports what it left out. No <c>UV</c> is assigned and no common-space criterion
        /// is chosen - that is a separate piece of work.
        /// </para>
        /// </summary>
        [WpfFact]
        public void ANonDwellingZone_StaysOutsideThePreparationScope()
        {
            Zone zone_Flat1 = new("Flat 1");
            zone_Flat1.SetValue(ZoneParameter.IsDwelling, true);

            Zone zone_Flat2 = new("Flat 2");
            zone_Flat2.SetValue(ZoneParameter.IsDwelling, true);

            Zone zone_Corridor = new("Corridor_1");
            zone_Corridor.SetValue(ZoneParameter.IsDwelling, false);

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [zone_Flat1, zone_Flat2, zone_Corridor],
            };

            List<Zone> zones_Dwelling = partOIterationWindow.Zones_Dwelling;

            Assert.Equal(2, zones_Dwelling.Count);
            Assert.DoesNotContain(zone_Corridor.Guid, zones_Dwelling.ConvertAll(x => x.Guid));

            //And it is reported as out of scope, not silently dropped.
            Assert.Contains("Corridor_1", partOIterationWindow.ScopeDescription);
            Assert.Contains("Outside current Part O dwelling preparation scope", partOIterationWindow.ScopeDescription);
        }

        /// <summary>
        /// A zone left unmarked beside marked ones is out of scope too, and is reported with its own reason -
        /// the model is saying something about its zones and staying silent about that one.
        /// </summary>
        [WpfFact]
        public void AnUnmarkedZoneBesideMarkedOnes_StaysOutsideThePreparationScope()
        {
            Zone zone_Flat1 = new("Flat 1");
            zone_Flat1.SetValue(ZoneParameter.IsDwelling, true);

            Zone zone_Unmarked = new("Plant");

            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [zone_Flat1, zone_Unmarked],
            };

            Assert.Single(partOIterationWindow.Zones_Dwelling);
            Assert.Contains("Plant", partOIterationWindow.ScopeDescription);
            Assert.Contains("not marked either way", partOIterationWindow.ScopeDescription);
        }

        /// <summary>
        /// Where nothing in the model states <c>IsDwelling</c>, every zone is a dwelling - the parameter
        /// postdates the models, and reading its absence as "not a dwelling" would size nothing at all. The
        /// window follows <c>Query.PartFDwellingZones</c> here rather than filtering on the parameter itself,
        /// and reports nothing as out of scope.
        /// </summary>
        [WpfFact]
        public void WhereNoZoneStatesIsDwelling_EveryZoneIsInScope()
        {
            PartOIterationWindow partOIterationWindow = new()
            {
                Zones = [new Zone("Flat 1"), new Zone("Flat 2")],
            };

            Assert.Equal(2, partOIterationWindow.Zones_Dwelling.Count);
            Assert.DoesNotContain("Outside current Part O dwelling preparation scope", partOIterationWindow.ScopeDescription);
        }

        // ---------------------------------------------------------------------------------------------
        // Design airflow is not equipment capacity
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The layer-separation lock, on the accepted fixture's own numbers.</b> Flat 1 is designed at
        /// 30/30 l/s and selects the 150/150 l/s Nuaire product. The design duty and the product's maximum
        /// are separate values on separate properties, the unspent difference is headroom, and there is no
        /// property on the row that reports 150 as the dwelling's design airflow.
        /// </summary>
        [Fact]
        public void ADwellingDesignedAt30_SelectingA150Product_KeepsTheTwoValuesApart()
        {
            VentilationUnitCapacityDescriptor descriptor = new(new VentilationUnitReference("Nuaire", "MRXBOXAB-ECO5-AECV", "MR-ECO-COOL-V"), 150, 150);

            PartOEquipmentRow row = new("MVHR-01", "Flat 1 MVHR", 30, 30, descriptor);

            //The dwelling still moves 30 l/s.
            Assert.Equal(30, row.DesignSupplyDuty_Lps);
            Assert.Equal(30, row.DesignExtractDuty_Lps);

            //The equipment can move 150 l/s. A different question, and a different property.
            Assert.Equal(150, row.MaximumSupply_Lps);
            Assert.Equal(150, row.MaximumExtract_Lps);

            //Selecting a larger unit did not raise the design.
            Assert.NotEqual(row.MaximumSupply_Lps, row.DesignSupplyDuty_Lps);
            Assert.NotEqual(row.MaximumExtract_Lps, row.DesignExtractDuty_Lps);

            //The rest is headroom, named as such.
            Assert.Equal(120, row.SupplyHeadroom_Lps);
            Assert.Equal(120, row.ExtractHeadroom_Lps);

            Assert.True(row.HasSelectedProduct);
            Assert.Equal("Selected", row.SelectionOutcome);
        }

        /// <summary>
        /// The other two accepted dwellings, 63/63 l/s, select the same product and keep the same separation -
        /// so one shared product cannot make three differently designed dwellings look alike.
        /// </summary>
        [Fact]
        public void TheAcceptedFixturesThreeDwellings_ShareOneProductAndKeepThreeDesignDuties()
        {
            VentilationUnitCapacityDescriptor descriptor = new(new VentilationUnitReference("Nuaire", "MRXBOXAB-ECO5-AECV", "MR-ECO-COOL-V"), 150, 150);

            PartOEquipmentRow row_Flat1 = new("MVHR-01", "Flat 1 MVHR", 30, 30, descriptor);
            PartOEquipmentRow row_Flat2 = new("MVHR-02", "Flat 2 MVHR", 63, 63, descriptor);
            PartOEquipmentRow row_Flat3 = new("MVHR-03", "Flat 3 MVHR", 63, 63, descriptor);

            Assert.Equal(30, row_Flat1.DesignSupplyDuty_Lps);
            Assert.Equal(63, row_Flat2.DesignSupplyDuty_Lps);
            Assert.Equal(63, row_Flat3.DesignSupplyDuty_Lps);

            Assert.Equal(150, row_Flat1.MaximumSupply_Lps);
            Assert.Equal(150, row_Flat2.MaximumSupply_Lps);
            Assert.Equal(150, row_Flat3.MaximumSupply_Lps);

            Assert.Equal(120, row_Flat1.SupplyHeadroom_Lps);
            Assert.Equal(87, row_Flat2.SupplyHeadroom_Lps);
            Assert.Equal(87, row_Flat3.SupplyHeadroom_Lps);
        }

        /// <summary>
        /// With no product selected there is no capacity and no headroom to show - and no zero either, which
        /// would read as a unit that can move nothing. The outcome distinguishes "no catalogue was offered"
        /// (Iteration 1a, normal) from "a catalogue was offered and refused".
        /// </summary>
        [Fact]
        public void WithNoSelectedProduct_NoCapacityIsShownAndTheOutcomeSaysWhy()
        {
            PartOEquipmentRow row_NoCatalogue = new("MVHR-01", "Flat 1 MVHR", 30, 30, null);

            Assert.False(row_NoCatalogue.HasSelectedProduct);
            Assert.Equal("Not applicable", row_NoCatalogue.SelectionOutcome);
            Assert.True(double.IsNaN(row_NoCatalogue.MaximumSupply_Lps));
            Assert.True(double.IsNaN(row_NoCatalogue.SupplyHeadroom_Lps));

            //The design duty is unaffected by there being no equipment.
            Assert.Equal(30, row_NoCatalogue.DesignSupplyDuty_Lps);

            PartOEquipmentRow row_Refused = new("MVHR-01", "Flat 1 MVHR", 300, 300, null, "No product in the catalogue can serve 300 l/s.");

            Assert.Equal("Refused", row_Refused.SelectionOutcome);

            //And the refusal did not reduce the design duty. An equipment outcome is not an airflow decision.
            Assert.Equal(300, row_Refused.DesignSupplyDuty_Lps);
        }

        // ---------------------------------------------------------------------------------------------
        // Catalogue states
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A catalogue that cannot be read is <see cref="VentilationUnitCatalogueState.Unavailable"/>, and its
        /// description says so without claiming anything about whether a product could serve a dwelling.
        /// </summary>
        [Fact]
        public void AnUnreadableCatalogue_IsUnavailableAndSaysNothingAboutDwellings()
        {
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(Path.Combine(Path.GetTempPath(), string.Format("SAM_NoCatalogue_{0}", Guid.NewGuid())));

            Assert.Equal(VentilationUnitCatalogueState.Unavailable, ventilationUnitCatalogue.State);
            Assert.False(ventilationUnitCatalogue.HasSelectableProducts);
            Assert.Empty(ventilationUnitCatalogue.CapacityDescriptors);

            Assert.Contains("could not be read", ventilationUnitCatalogue.Description);

            //The specific confusion being prevented: an absent catalogue must never read as an engineering
            //answer about the dwellings.
            Assert.Contains("not a statement that no product could serve", ventilationUnitCatalogue.Description);
        }

        /// <summary>
        /// A catalogue that reads but whose only product has no stated maximum airflow is
        /// <see cref="VentilationUnitCatalogueState.NoneSelectable"/> - a real, documented condition, and a
        /// different state from the catalogue being missing. The unselectable product is still reported, so it
        /// is visible rather than vanished.
        /// </summary>
        [Fact]
        public void ACatalogueWhoseProductsHaveNoStatedMaximum_IsNoneSelectableAndNotUnavailable()
        {
            string directory = Path.Combine(Path.GetTempPath(), string.Format("SAM_UnresolvedCatalogue_{0}", Guid.NewGuid()));

            Directory.CreateDirectory(directory);

            try
            {
                //A valid v1 catalogue whose single entry states no maximum airflow. Guessing one from a
                //performance table is the mistake the capacity seam exists to prevent, so the reader keeps the
                //product and leaves it unselectable.
                File.WriteAllText(Path.Combine(directory, "VentilationUnitCatalogue.JSON"), """
                {
                  "Schema": "VentilationUnitCatalogue:v1",
                  "Templates": [
                    {
                      "_type": "SAM.Analytical.VentilationUnitTemplate,SAM.Analytical",
                      "Name": "Test unit with unresolved capacity",
                      "VentilationUnitReference": {
                        "_type": "SAM.Analytical.VentilationUnitReference,SAM.Analytical",
                        "Name": "Test unit",
                        "Manufacturer": "Test",
                        "Model": "Unresolved",
                        "Reference": "TEST-UNRESOLVED"
                      },
                      "Source": "Written by this test. A template needs a traceable source to be valid at all.",
                      "Rank": 10
                    }
                  ]
                }
                """);

                VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read(directory);

                Assert.Equal(VentilationUnitCatalogueState.NoneSelectable, ventilationUnitCatalogue.State);
                Assert.NotEqual(VentilationUnitCatalogueState.Unavailable, ventilationUnitCatalogue.State);

                Assert.False(ventilationUnitCatalogue.HasSelectableProducts);
                Assert.Empty(ventilationUnitCatalogue.CapacityDescriptors);

                //Reported, with the reason, rather than silently absent.
                Assert.NotEmpty(ventilationUnitCatalogue.UnselectableTemplates);

                Assert.Contains("none of which is selectable", ventilationUnitCatalogue.Description);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// The shipped catalogue reads as <see cref="VentilationUnitCatalogueState.Selectable"/> and its
        /// product's maximum is 150/150 l/s - the fan-curve free-air ceiling of the evidenced Nuaire unit,
        /// which is a selection ceiling and not an installed duty.
        /// <para>
        /// Skipped where the catalogue is not installed on the machine running the tests: this asserts what
        /// the shipped resource says, and its absence is an environment fact rather than a defect in this
        /// code. The two states above are what cover the reader's behaviour unconditionally.
        /// </para>
        /// </summary>
        /// <summary>
        /// <b>A second product in the catalogue does not disturb the accepted Iteration 2 answer.</b>
        /// <para>
        /// The shipped catalogue now offers two Nuaire products - the MRXBOX at 150/150 l/s and the XBOXER
        /// XBC15 at 190/190 l/s. Selection is smallest-capable by size, and the XBC15 is larger on both
        /// sides, so at every duty the MRXBOX can also carry the answer is unchanged: at the project's own
        /// 30/30 and 63/63 l/s dwelling duties, and at the 150 l/s boundary itself, the MRXBOX is still what
        /// is selected and the XBC15 is offered and not taken.
        /// </para>
        /// <para>
        /// This is what makes the existing licensed Iteration 2 and 2B acceptance still valid rather than
        /// merely still plausible - the XBC15 becomes selectable only in the new 150-190 l/s band. Skipped
        /// where the catalogue is not installed on the machine running the tests, exactly as the test below
        /// is: the absence of a shipped resource is an environment fact, not a defect in this code.
        /// </para>
        /// </summary>
        [Fact]
        public void TheShippedCatalogue_OffersTwoProducts_AndStillSelectsTheMRXBOXAtTheProjectsDuties()
        {
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            if (ventilationUnitCatalogue.State != VentilationUnitCatalogueState.Selectable)
            {
                return;
            }

            Assert.True(ventilationUnitCatalogue.HasSelectableProducts);
            Assert.Empty(ventilationUnitCatalogue.UnselectableTemplates);

            Assert.Equal(2, ventilationUnitCatalogue.CapacityDescriptors.Count);
            Assert.Contains("2 selectable ventilation unit product(s) available.", ventilationUnitCatalogue.Description);

            Assert.Contains(ventilationUnitCatalogue.CapacityDescriptors, x => x.VentilationUnitReference?.Model == "MRXBOXAB-ECO5-AECV" && x.MaximumSupplyFlowRate_Lps == 150);
            Assert.Contains(ventilationUnitCatalogue.CapacityDescriptors, x => x.VentilationUnitReference?.Model == "XBC15" && x.MaximumSupplyFlowRate_Lps == 190);

            //The project's own dwelling duties, and the boundary. Unchanged answers, all three.
            foreach (double duty_Lps in new double[] { 30, 63, 150 })
            {
                VentilationUnitSelection ventilationUnitSelection = Analytical.Query.SelectSmallestCapableVentilationUnit(ventilationUnitCatalogue.CapacityDescriptors, duty_Lps, duty_Lps);

                Assert.True(ventilationUnitSelection.IsSelected);
                Assert.Equal("MRXBOXAB-ECO5-AECV", ventilationUnitSelection.VentilationUnitReference.Model);

                //The design duty is what it was asked for, and the rest is headroom deliberately untaken.
                Assert.Equal(duty_Lps, ventilationUnitSelection.SupplyDuty_Lps, 6);
                Assert.Equal(150 - duty_Lps, ventilationUnitSelection.SupplyHeadroom_Lps, 6);
            }

            //And the XBC15 is what answers above 150 - the band it was added for.
            Assert.Equal("XBC15", Analytical.Query.SelectSmallestCapableVentilationUnit(ventilationUnitCatalogue.CapacityDescriptors, 160, 160).VentilationUnitReference.Model);
        }

        [Fact]
        public void TheShippedCatalogue_OffersTheNuaireProductAt150Maximum()
        {
            VentilationUnitCatalogue ventilationUnitCatalogue = VentilationUnitCatalogue.Read();

            if (ventilationUnitCatalogue.State != VentilationUnitCatalogueState.Selectable)
            {
                return;
            }

            Assert.True(ventilationUnitCatalogue.HasSelectableProducts);

            Assert.Contains(ventilationUnitCatalogue.CapacityDescriptors, x => x is not null
                && x.MaximumSupplyFlowRate_Lps == 150
                && x.MaximumExtractFlowRate_Lps == 150
                && string.Equals(x.VentilationUnitReference?.Manufacturer, "Nuaire", StringComparison.Ordinal));
        }

        // ---------------------------------------------------------------------------------------------
        // The manual product picker shows a capacity and never the catalogue's internal rank
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>The manual per-dwelling picker reads the engineer-facing label.</b>
        /// <para>
        /// Read off the real template rather than assumed: the assigned-product column's editing template is
        /// loaded and its <c>ComboBox</c> asked what it displays. Left to its own devices a <c>ComboBox</c>
        /// falls back to <c>ToString</c>, which is the diagnostic form and ends with "rank 10" - a catalogue
        /// tie-breaker, presented where an engineer is choosing equipment and could read it as a rating.
        /// </para>
        /// <para>
        /// It reads it through an <c>ItemTemplate</c> over
        /// <see cref="PartOProductLabelConverter"/> rather than through <c>DisplayMemberPath</c>, because a
        /// project test product's identity needs spelling out - "test (project test)" and never the literal
        /// join "Project test test". Same figures, same separator; see <c>Query.PartOProductLabel</c>.
        /// </para>
        /// </summary>
        [WpfFact]
        public void TheManualPicker_DisplaysTheEngineerFacingLabel()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            System.Windows.Controls.DataGrid dataGrid = (System.Windows.Controls.DataGrid)partOPreparationWindow.FindName("dataGrid_Equipment");

            Assert.NotNull(dataGrid);

            System.Windows.Controls.DataGridTemplateColumn dataGridTemplateColumn = null;

            foreach (System.Windows.Controls.DataGridColumn dataGridColumn in dataGrid.Columns)
            {
                if (dataGridColumn is System.Windows.Controls.DataGridTemplateColumn column && Equals(column.Header, "Assigned product"))
                {
                    dataGridTemplateColumn = column;
                }
            }

            Assert.NotNull(dataGridTemplateColumn);

            System.Windows.Controls.ComboBox comboBox = dataGridTemplateColumn.CellEditingTemplate?.LoadContent() as System.Windows.Controls.ComboBox;

            Assert.NotNull(comboBox);

            AssertProductLabelTemplate(comboBox);
        }

        /// <summary>
        /// And the bulk product selector reads the same label, so the two ways of choosing a product cannot
        /// describe the same product differently.
        /// </summary>
        [WpfFact]
        public void TheBulkProductSelector_DisplaysTheSameLabel()
        {
            PartOPreparationWindow partOPreparationWindow = new();

            System.Windows.Controls.ComboBox comboBox = (System.Windows.Controls.ComboBox)partOPreparationWindow.FindName("comboBox_BulkProduct");

            Assert.NotNull(comboBox);

            AssertProductLabelTemplate(comboBox);
        }

        /// <summary>
        /// That a picker renders each product through the Part O product-label converter, and through
        /// nothing else - no <c>DisplayMemberPath</c>, and so no silent fall back to <c>ToString</c>.
        /// </summary>
        private static void AssertProductLabelTemplate(System.Windows.Controls.ComboBox comboBox)
        {
            Assert.True(string.IsNullOrEmpty(comboBox.DisplayMemberPath));

            Assert.NotNull(comboBox.ItemTemplate);

            System.Windows.Controls.TextBlock textBlock = comboBox.ItemTemplate.LoadContent() as System.Windows.Controls.TextBlock;

            Assert.NotNull(textBlock);

            System.Windows.Data.Binding binding = System.Windows.Data.BindingOperations.GetBinding(textBlock, System.Windows.Controls.TextBlock.TextProperty);

            Assert.NotNull(binding);
            Assert.IsType<PartOProductLabelConverter>(binding.Converter);
        }

        /// <summary>
        /// What that label actually says, for the two shipped products: the identity and both maximum
        /// airflows, and no mention of rank in any casing.
        /// </summary>
        [Fact]
        public void ThatLabel_StatesTheCapacityAndNeverTheRank()
        {
            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in new[]
            {
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", "MRXBOXAB-ECO5-AECV", "MR-ECO-COOL-V"), 150, 150, 10),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Nuaire", "XBC15", null), 190, 190, 20),
            })
            {
                string label = ventilationUnitCapacityDescriptor.Label;

                Assert.Contains("Nuaire", label);
                Assert.Contains("l/s", label);
                Assert.Contains(ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps.ToString("0"), label);

                Assert.DoesNotContain("rank", label, StringComparison.OrdinalIgnoreCase);

                //Where the diagnostic form still carries it, so the two are genuinely different strings.
                Assert.Contains("rank", ventilationUnitCapacityDescriptor.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
