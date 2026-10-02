// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF.Tests.Helpers;
using SAM.Core;
using SAM.Core.UI;
using SAM.Geometry.Object;
using SAM.Geometry.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Colouring a 3D view by a parameter is VIEW state: <c>Create.ParameterColouredViewSettings</c> builds a coloured COPY of the
    /// view's settings with a palette legend, through the existing value query / legend / palette code, and touches neither the
    /// original settings nor the model. The Thermal Performance panel's "Colour by U-value" toggle only asks the host for it.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ParameterColouringTests
    {
        private static ThreeDimensionalViewSettings View()
        {
            return new ThreeDimensionalViewSettings(Guid.NewGuid(), "3D View", null, new[] { typeof(Panel), typeof(Aperture) }, null);
        }

        // Walls on the fixture construction store 0.26, the one wall of the other construction 1.50; the windows' panels store nothing.
        private static AnalyticalModel ModelWithStoredU(out ThermalParts parts)
        {
            parts = ThermalFixture.Build();
            AdjacencyCluster adjacencyCluster = parts.Model.AdjacencyCluster;
            foreach (Panel panel in adjacencyCluster.GetPanels().ToList())
            {
                double? u = panel.TypeGuid == parts.Wall.Guid ? 0.26 : panel.TypeGuid == parts.OtherWall.Guid ? 1.5 : (double?)null;
                if (!u.HasValue)
                {
                    continue;
                }

                Panel copy = Analytical.Create.Panel(panel);
                copy.SetValue(PanelParameter.ThermalTransmittance, u.Value);
                adjacencyCluster.AddObject(copy);
            }

            return new AnalyticalModel(parts.Model, adjacencyCluster, parts.Model.MaterialLibrary, parts.Model.ProfileLibrary);
        }

        [Fact]
        public void The_coloured_copy_colours_panels_by_the_UValue_parameter_with_a_palette_legend()
        {
            AnalyticalModel model = ModelWithStoredU(out _);
            ThreeDimensionalViewSettings view = View();

            ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance());

            Assert.NotNull(coloured);
            Assert.NotSame(view, coloured);
            Assert.Equal(view.Guid, coloured.Guid);
            Assert.True(coloured.ContainsType(typeof(Panel)));

            PanelAppearanceSettings settings = coloured.GetValueAppearanceSettings<PanelAppearanceSettings>().Single();
            Assert.Equal("UValue", settings.GetValueAppearanceSettings<ParameterAppearanceSettings>().ParameterName);

            // The legend lists the stored values, lowest first, in the palette's colours: cool for the low U, warm for the high one.
            Legend legend = coloured.Legend;
            Assert.Equal("U-value [W/m²K]", legend.Name);
            Assert.Equal(new[] { "0.26", "1.5" }, legend.LegendItems.Select(x => x.Text).ToArray());
            System.Drawing.Color low = legend.LegendItems[0].Color;
            System.Drawing.Color high = legend.LegendItems[1].Color;
            Assert.Equal(PaletteDefinitions.SamThermal.Colors.First().ToArgb(), low.ToArgb());
            Assert.Equal(PaletteDefinitions.SamThermal.Colors.Last().ToArgb(), high.ToArgb());
        }

        [Fact]
        public void Building_the_coloured_copy_changes_neither_the_original_view_settings_nor_the_model()
        {
            AnalyticalModel model = ModelWithStoredU(out _);
            ThreeDimensionalViewSettings view = View();
            view.AddAppearanceSettings(new PanelAppearanceSettings("Name"));
            string viewBefore = view.ToJsonObject().ToJsonString();
            string modelBefore = model.ToJsonObject().ToJsonString();

            model.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance());

            Assert.Equal(viewBefore, view.ToJsonObject().ToJsonString());
            Assert.Equal(modelBefore, model.ToJsonObject().ToJsonString());
            Assert.Null(view.Legend);
        }

        [Fact]
        public void The_coloured_copy_keeps_the_other_settings_of_the_view()
        {
            AnalyticalModel model = ModelWithStoredU(out _);
            ThreeDimensionalViewSettings view = View();
            view.AddAppearanceSettings(new ApertureAppearanceSettings("Name"));

            ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance());

            Assert.NotNull(coloured.GetValueAppearanceSettings<ApertureAppearanceSettings>().SingleOrDefault());
            Assert.True(coloured.ContainsType(typeof(Aperture)));
        }

        [Fact]
        public void Rendering_the_coloured_copy_keeps_the_palette_colours_of_the_legend_it_is_given()
        {
            AnalyticalModel model = ModelWithStoredU(out _);
            ThreeDimensionalViewSettings coloured = model.ParameterColouredViewSettings(View(), ParameterColouring.PanelThermalTransmittance());
            List<System.Drawing.Color> expected = coloured.Legend.LegendItems.Select(x => x.Color).ToList();

            GeometryObjectModel geometryObjectModel = model.ToSAM_GeometryObjectModel(coloured);

            Assert.NotNull(geometryObjectModel);
            Assert.True(geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings rendered));
            Assert.Equal(expected, rendered.Legend.LegendItems.Select(x => x.Color).ToList());
        }

        [Fact]
        public void A_view_that_is_not_coloured_renders_with_its_saved_settings_exactly_as_before()
        {
            AnalyticalModel model = ModelWithStoredU(out _);
            ThreeDimensionalViewSettings view = View();

            GeometryObjectModel geometryObjectModel = model.ToSAM_GeometryObjectModel(view);

            Assert.True(geometryObjectModel.TryGetValue(GeometryObjectModelParameter.ViewSettings, out ViewSettings rendered));
            Assert.Equal(view.Guid, rendered.Guid);
        }

        [Fact]
        public void A_value_that_changes_is_picked_up_by_the_next_build_of_the_copy()
        {
            AnalyticalModel model = ModelWithStoredU(out ThermalParts parts);
            ThreeDimensionalViewSettings view = View();
            Assert.Equal(2, model.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance()).Legend.LegendItems.Count);

            // The wall panels now store 0.18 instead of 0.26 (what Apply writes): the legend follows the model, there is no cached colouring.
            AdjacencyCluster adjacencyCluster = model.AdjacencyCluster;
            foreach (Panel panel in adjacencyCluster.GetPanels().Where(x => x.TypeGuid == parts.Wall.Guid).ToList())
            {
                Panel copy = Analytical.Create.Panel(panel);
                copy.SetValue(PanelParameter.ThermalTransmittance, 0.18);
                adjacencyCluster.AddObject(copy);
            }

            AnalyticalModel changed = new AnalyticalModel(model, adjacencyCluster, model.MaterialLibrary, model.ProfileLibrary);

            Assert.Equal(new[] { "0.18", "1.5" }, changed.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance()).Legend.LegendItems.Select(x => x.Text).ToArray());
        }

        [Fact]
        public void A_model_without_stored_values_gives_no_legend_and_an_unsupported_element_type_gives_nothing()
        {
            ThermalParts parts = ThermalFixture.Build();
            ThreeDimensionalViewSettings view = View();

            ThreeDimensionalViewSettings coloured = parts.Model.ParameterColouredViewSettings(view, ParameterColouring.PanelThermalTransmittance());

            Assert.NotNull(coloured);
            Assert.Null(coloured.Legend);
            Assert.Null(parts.Model.ParameterColouredViewSettings(view, new ParameterColouring(typeof(Zone), "Name", PaletteDefinitions.SamThermal)));
            Assert.Null(parts.Model.ParameterColouredViewSettings(null, ParameterColouring.PanelThermalTransmittance()));
            Assert.Null(parts.Model.ParameterColouredViewSettings(view, null));
        }

        // ---- The Thermal Performance toggle --------------------------------------------------------------------------

        private static ToggleButton Toggle(ThermalPerformanceControl control)
        {
            return (ToggleButton)control.FindName("toggleButton_ColourByU");
        }

        // What a mouse click does: toggle, then raise Click.
        private static void Click(ToggleButton button)
        {
            typeof(System.Windows.Controls.Primitives.ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, null);
        }

        [WpfFact]
        public void The_toggle_asks_the_host_to_colour_and_to_restore_but_setting_its_state_asks_nothing()
        {
            using (ThermalPerformanceControl control = new ThermalPerformanceControl(new ThermalEditServices(() => new ImmediateUValueEvaluator(), () => new FakeGlazingEvaluator(), () => GlazingFixture.Library())))
            {
                System.Windows.Window window = new System.Windows.Window { Content = control, Width = 380, Height = 600, ShowActivated = false };
                window.Show();
                try
                {
                    List<bool> requests = new List<bool>();
                    control.ColourRequested += (sender, e) => requests.Add(e.On);
                    ToggleButton toggle = Toggle(control);
                    Assert.NotNull(toggle);

                    control.SetColourState(true, false);
                    Assert.True(toggle.IsEnabled);
                    Assert.False(toggle.IsChecked);

                    control.SetColourState(true, true);
                    Assert.True(toggle.IsChecked);
                    Assert.Empty(requests);

                    // The user turns it off, then on.
                    Click(toggle);
                    Click(toggle);
                    Assert.Equal(new[] { false, true }, requests);

                    // No 3D view that shows panels: disabled, unchecked, with the reason.
                    control.SetColourState(false, true, "This view does not show panels.");
                    Assert.False(toggle.IsEnabled);
                    Assert.False(toggle.IsChecked);
                    Assert.Equal("This view does not show panels.", toggle.ToolTip);
                }
                finally
                {
                    window.Close();
                }
            }
        }
    }
}
