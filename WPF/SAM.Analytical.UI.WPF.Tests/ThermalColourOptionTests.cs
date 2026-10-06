// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.Spatial;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Thermal Performance "Colour by" selector: which stored thermal properties a 3D view can be coloured by (U of panels, U of windows
    /// and doors, g, light transmittance), that each is the parameter the panel's rows show, that the options only ask the host for a
    /// colouring and never touch the model, and how the selector reflects what the active view can show.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalColourOptionTests
    {
        private static readonly Guid WindowGuid = new Guid("c1000000-0000-4000-8000-000000000001");
        private static readonly Guid DoorGuid = new Guid("c1000000-0000-4000-8000-000000000002");

        private static ThreeDimensionalViewSettings View(params Type[] types)
        {
            return new ThreeDimensionalViewSettings(Guid.NewGuid(), "3D View", null, types.Length == 0 ? new[] { typeof(Panel), typeof(Aperture) } : types, null);
        }

        private static Aperture Aperture(ApertureConstruction apertureConstruction, int index, double? u, double? g, double? lt)
        {
            double x = index * 5;
            Aperture aperture = Analytical.Create.Aperture(apertureConstruction, new Face3D(new Polygon3D(new List<Point3D>()
            {
                new Point3D(x + 1, 0, 0.75), new Point3D(x + 3, 0, 0.75), new Point3D(x + 3, 0, 2.25), new Point3D(x + 1, 0, 2.25),
            })));

            if (u.HasValue)
            {
                aperture.SetValue(ApertureParameter.ThermalTransmittance, u.Value);
            }

            if (g.HasValue)
            {
                aperture.SetValue(ApertureParameter.TotalSolarEnergyTransmittance, g.Value);
            }

            if (lt.HasValue)
            {
                aperture.SetValue(ApertureParameter.LightTransmittance, lt.Value);
            }

            return aperture;
        }

        // Two walls (stored U 0.26 and 0.30), a window with U 1.243 / g 0.4 / LT 0.8 on each of them (the second one g 0.6), and a door that stores a U only.
        private static AnalyticalModel Model()
        {
            Construction wall = UValueFixture.Wall();
            ApertureConstruction window = GlazingFixture.System(WindowGuid, "GLZ", ApertureType.Window, GlazingFixture.Clear);
            ApertureConstruction door = GlazingFixture.System(DoorGuid, "DOOR", ApertureType.Door, GlazingFixture.Clear);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Panel panel_1 = UValueFixture.Panel(wall, PanelType.WallExternal, 0);
            panel_1.SetValue(PanelParameter.ThermalTransmittance, 0.26);
            panel_1.AddAperture(Aperture(window, 0, 1.243, 0.4, 0.8));
            adjacencyCluster.AddObject(panel_1);

            Panel panel_2 = UValueFixture.Panel(wall, PanelType.WallExternal, 1);
            panel_2.SetValue(PanelParameter.ThermalTransmittance, 0.30);
            panel_2.AddAperture(Aperture(window, 1, 1.243, 0.6, 0.8));
            adjacencyCluster.AddObject(panel_2);

            Panel panel_3 = UValueFixture.Panel(wall, PanelType.WallExternal, 2);
            panel_3.SetValue(PanelParameter.ThermalTransmittance, 0.26);
            panel_3.AddAperture(Aperture(door, 2, 2.0, null, null));
            adjacencyCluster.AddObject(panel_3);

            MaterialLibrary materials = UValueFixture.Materials();
            foreach (IMaterial material in GlazingFixture.ModelMaterials().GetMaterials())
            {
                materials.Add(material);
            }

            return new AnalyticalModel("Colour", null, null, null, adjacencyCluster, materials, new ProfileLibrary("Profiles"));
        }

        private static string[] LegendTexts(AnalyticalModel model, ThreeDimensionalViewSettings view, ThermalColourOption option)
        {
            return model.ParameterColouredViewSettings(view, option.Colouring).Legend.LegendItems.Select(x => x.Text).ToArray();
        }

        // ---- The options: which parameter each one is -------------------------------------------------------------------

        [Fact]
        public void Each_option_colours_the_parameter_it_stands_for_on_the_element_type_that_stores_it()
        {
            // Documented mapping (documentation/Thermal-ColourSelector.md): label -> element type -> parameter name (from the enum) -> palette.
            Assert.Equal((typeof(Panel), "UValue"), (ThermalColourOption.PanelUValue.Colouring.ElementType, ThermalColourOption.PanelUValue.Colouring.ParameterName));
            Assert.Equal((typeof(Aperture), "UValue"), (ThermalColourOption.ApertureUValue.Colouring.ElementType, ThermalColourOption.ApertureUValue.Colouring.ParameterName));
            Assert.Equal((typeof(Aperture), "GValue"), (ThermalColourOption.GValue.Colouring.ElementType, ThermalColourOption.GValue.Colouring.ParameterName));
            Assert.Equal((typeof(Aperture), "Light Transmittance"), (ThermalColourOption.LightTransmittance.Colouring.ElementType, ThermalColourOption.LightTransmittance.Colouring.ParameterName));

            Assert.Equal(Core.Query.Name(PanelParameter.ThermalTransmittance), ThermalColourOption.PanelUValue.Colouring.ParameterName);
            Assert.Equal(Core.Query.Name(ApertureParameter.ThermalTransmittance), ThermalColourOption.ApertureUValue.Colouring.ParameterName);
            Assert.Equal(Core.Query.Name(ApertureParameter.TotalSolarEnergyTransmittance), ThermalColourOption.GValue.Colouring.ParameterName);
            Assert.Equal(Core.Query.Name(ApertureParameter.LightTransmittance), ThermalColourOption.LightTransmittance.Colouring.ParameterName);
        }

        [Fact]
        public void The_selector_lists_Off_first_with_readable_labels_and_no_parameter_or_enum_names()
        {
            Assert.Same(ThermalColourOption.Off, ThermalColourOption.All[0]);
            Assert.Null(ThermalColourOption.Off.Colouring);
            Assert.Equal(new[] { "Off", "U-value (panels)", "U-value (windows & doors)", "g-value (windows)", "Light transmittance (windows)" }, ThermalColourOption.All.Select(x => x.Label).ToArray());
            Assert.Equal(ThermalColourOption.All.Count, ThermalColourOption.All.Select(x => x.Key).Distinct().Count());

            foreach (ThermalColourOption option in ThermalColourOption.All)
            {
                Assert.DoesNotContain("Parameter", option.Label);
                Assert.DoesNotContain("TotalSolar", option.Label);
                Assert.DoesNotContain("ThermalTransmittance", option.Label);
                Assert.False(string.IsNullOrWhiteSpace(option.ToolTip));
            }
        }

        [Fact]
        public void The_window_U_option_says_it_is_the_stored_glazing_value_not_the_frame_inclusive_Uw()
        {
            Assert.Contains("Ug", ThermalColourOption.ApertureUValue.ToolTip);
            Assert.Contains("Uw", ThermalColourOption.ApertureUValue.ToolTip);
            Assert.DoesNotContain("Uw", ThermalColourOption.ApertureUValue.Label);
        }

        [Fact]
        public void An_option_is_found_again_from_its_colouring_and_a_colouring_that_is_not_thermal_has_none()
        {
            foreach (ThermalColourOption option in ThermalColourOption.All)
            {
                Assert.Same(option, ThermalColourOption.Of(option.Colouring));
            }

            Assert.Same(ThermalColourOption.Off, ThermalColourOption.Of(null));
            Assert.Null(ThermalColourOption.Of(new ParameterColouring(typeof(Panel), "Name", PaletteDefinitions.SamThermal)));
        }

        [Fact]
        public void An_option_is_available_when_the_view_shows_the_elements_it_colours()
        {
            ThreeDimensionalViewSettings panelsOnly = View(typeof(Panel));
            ThreeDimensionalViewSettings aperturesOnly = View(typeof(Aperture));

            Assert.True(ThermalColourOption.Off.IsAvailableIn(null));
            Assert.True(ThermalColourOption.PanelUValue.IsAvailableIn(panelsOnly.ContainsType));
            Assert.False(ThermalColourOption.PanelUValue.IsAvailableIn(aperturesOnly.ContainsType));
            Assert.False(ThermalColourOption.GValue.IsAvailableIn(panelsOnly.ContainsType));
            Assert.True(ThermalColourOption.GValue.IsAvailableIn(aperturesOnly.ContainsType));
            Assert.False(ThermalColourOption.LightTransmittance.IsAvailableIn(null));
        }

        // ---- What a view coloured by each option shows ---------------------------------------------------------------

        [Fact]
        public void Windows_are_coloured_by_the_stored_U_g_and_light_transmittance_the_panel_rows_show()
        {
            AnalyticalModel model = Model();
            ThreeDimensionalViewSettings view = View();

            // The panel's own rows read the same stored parameters.
            List<ThermalPerformanceGroup> groups = Query.ThermalPerformanceGroups(model, null, ThermalPerformanceMode.WholeEnvelope);
            ThermalPerformanceRow windowRow = groups.Single(x => x.Title == "Windows").Rows.Single();
            Assert.Equal("U 1.243 · g varies · LT 0.80", windowRow.PerformanceText);

            Assert.Equal(new[] { "1.243", "2" }, LegendTexts(model, view, ThermalColourOption.ApertureUValue));
            Assert.Equal(new[] { "0.4", "0.6" }, LegendTexts(model, view, ThermalColourOption.GValue));
            Assert.Equal(new[] { "0.8" }, LegendTexts(model, view, ThermalColourOption.LightTransmittance));
            Assert.Equal(new[] { "0.26", "0.3" }, LegendTexts(model, view, ThermalColourOption.PanelUValue));
        }

        [Fact]
        public void An_aperture_option_colours_the_apertures_and_leaves_the_views_panel_appearance_alone()
        {
            AnalyticalModel model = Model();
            ThreeDimensionalViewSettings view = View();
            view.AddAppearanceSettings(new PanelAppearanceSettings("Name"));

            ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(view, ThermalColourOption.GValue.Colouring);

            Assert.Equal("GValue", coloured.GetValueAppearanceSettings<ApertureAppearanceSettings>().Single().GetValueAppearanceSettings<ParameterAppearanceSettings>().ParameterName);
            Assert.Equal("Name", coloured.GetValueAppearanceSettings<PanelAppearanceSettings>().Single().GetValueAppearanceSettings<ParameterAppearanceSettings>().ParameterName);
            Assert.Equal("g-value [0-1]", coloured.Legend.Name);
        }

        [Fact]
        public void Switching_from_one_option_to_another_replaces_the_colouring_of_the_saved_view_each_time()
        {
            AnalyticalModel model = Model();
            ThreeDimensionalViewSettings view = View();
            string viewBefore = view.ToJsonObject().ToJsonString();
            string modelBefore = model.ToJsonObject().ToJsonString();

            string[] order = new[] { "U-value (panels)", "g-value (windows)", "Light transmittance (windows)", "U-value (windows & doors)", "U-value (panels)" };
            foreach (string label in order)
            {
                ThermalColourOption option = ThermalColourOption.All.Single(x => x.Label == label);
                ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(view, option.Colouring);

                Assert.Equal(option.Colouring.Title, coloured.Legend.Name);
                ParameterAppearanceSettings settings = option.Colouring.ElementType == typeof(Panel)
                    ? coloured.GetValueAppearanceSettings<PanelAppearanceSettings>().Single().GetValueAppearanceSettings<ParameterAppearanceSettings>()
                    : coloured.GetValueAppearanceSettings<ApertureAppearanceSettings>().Single().GetValueAppearanceSettings<ParameterAppearanceSettings>();
                Assert.Equal(option.Colouring.ParameterName, settings.ParameterName);
            }

            Assert.Equal(viewBefore, view.ToJsonObject().ToJsonString());
            Assert.Equal(modelBefore, model.ToJsonObject().ToJsonString());
        }

        [Fact]
        public void Rendering_a_window_coloured_view_keeps_the_palette_colours_of_its_legend()
        {
            AnalyticalModel model = Model();

            foreach (ThermalColourOption option in new[] { ThermalColourOption.ApertureUValue, ThermalColourOption.GValue })
            {
                ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(View(), option.Colouring);
                List<System.Drawing.Color> expected = coloured.Legend.LegendItems.Select(x => x.Color).ToList();

                GeometryObjectModel geometryObjectModel = model.ToSAM_GeometryObjectModel(coloured);

                Assert.True(geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings rendered));
                Assert.Equal(expected, rendered.Legend.LegendItems.Select(x => x.Color).ToList());
                Assert.Equal(2, expected.Distinct().Count());
            }
        }

        [Fact]
        public void The_sequence_of_options_changes_nothing_in_the_model_or_the_saved_view_and_each_call_is_repeatable()
        {
            AnalyticalModel model = Model();
            ThreeDimensionalViewSettings view = View();
            string modelBefore = model.ToJsonObject().ToJsonString();

            foreach (ThermalColourOption option in ThermalColourOption.All.Where(x => x.Colouring != null))
            {
                string first = model.ParameterColouredViewSettings(view, option.Colouring).ToJsonObject().ToJsonString();
                string second = model.ParameterColouredViewSettings(view, option.Colouring).ToJsonObject().ToJsonString();
                Assert.Equal(first, second);
            }

            Assert.Equal(modelBefore, model.ToJsonObject().ToJsonString());
            Assert.Null(view.Legend);
        }

        // ---- The selector control ------------------------------------------------------------------------------------

        private static ComboBox Selector(ThermalPerformanceControl control)
        {
            return (ComboBox)control.FindName("comboBox_ColourBy");
        }

        [WpfFact]
        public void The_selector_asks_the_host_for_the_chosen_option_but_setting_its_state_asks_nothing()
        {
            using (ThermalPerformanceControl control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()))))
            {
                System.Windows.Window window = new System.Windows.Window { Content = control, Width = 380, Height = 600, ShowActivated = false };
                window.Show();
                try
                {
                    List<ThermalColourOption> requests = new List<ThermalColourOption>();
                    control.ColourRequested += (sender, e) => requests.Add(e.Option);
                    ComboBox selector = Selector(control);
                    Assert.NotNull(selector);

                    // Showing the state of a view asks nothing.
                    control.SetColourState(true, ThermalColourOption.Off);
                    Assert.True(selector.IsEnabled);
                    Assert.Equal("Off", selector.SelectedItem.ToString());
                    control.SetColourState(true, ThermalColourOption.GValue);
                    Assert.Equal("g-value (windows)", selector.SelectedItem.ToString());
                    Assert.Empty(requests);
                    Assert.Equal(ThermalColourOption.All.Count, selector.Items.Count);

                    // The user chooses (the same SelectionChanged a click raises).
                    selector.SelectedIndex = ThermalColourOption.All.ToList().IndexOf(ThermalColourOption.LightTransmittance);
                    selector.SelectedIndex = ThermalColourOption.All.ToList().IndexOf(ThermalColourOption.PanelUValue);
                    selector.SelectedIndex = 0;
                    Assert.Equal(new[] { ThermalColourOption.LightTransmittance, ThermalColourOption.PanelUValue, ThermalColourOption.Off }, requests);
                }
                finally
                {
                    window.Close();
                }
            }
        }

        [WpfFact]
        public void The_selector_is_disabled_with_a_reason_in_a_view_that_is_not_3D_and_lists_unavailable_options_as_disabled()
        {
            using (ThermalPerformanceControl control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library(), () => new FakeConstructionUValueEvaluator(), () => null, () => new ThermalSourceCatalog(new InMemoryThermalSourceStore()))))
            {
                System.Windows.Window window = new System.Windows.Window { Content = control, Width = 380, Height = 600, ShowActivated = false };
                window.Show();
                try
                {
                    ComboBox selector = Selector(control);

                    // A 3D view that shows panels only: the window options are listed but cannot be chosen.
                    ThreeDimensionalViewSettings panelsOnly = View(typeof(Panel));
                    control.SetColourState(true, ThermalColourOption.PanelUValue, x => x.IsAvailableIn(panelsOnly.ContainsType));
                    List<bool> enabled = selector.Items.Cast<object>().Select(x => (bool)x.GetType().GetProperty("IsAvailable").GetValue(x)).ToList();
                    Assert.Equal(new[] { true, true, false, false, false }, enabled);

                    // No 3D view: disabled, Off, with the reason.
                    control.SetColourState(false, ThermalColourOption.GValue, null, "Colouring is available in a 3D view.");
                    Assert.False(selector.IsEnabled);
                    Assert.Equal("Off", selector.SelectedItem.ToString());
                    Assert.Equal("Colouring is available in a 3D view.", selector.ToolTip);
                }
                finally
                {
                    window.Close();
                }
            }
        }
    }
}
