// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Stage E0-1 Gate 0, through the PRODUCT route (draft → compose → <see cref="DraftGlazingEvaluator"/> → real Tas TCD). Opt-in: set
    /// <c>SAM_E0_PILKINGTON_TCD</c> to the Pilkington IGDB v76 subset (e.g. <c>International Glazing Database_v76-Pilkington.tcd</c> from the
    /// Tas Data databases folder) on a machine with Tas; otherwise the test does nothing. It re-proves the orientation findings the Builder
    /// depends on: the Builder's OUTSIDE pane is outside, a pane's External face faces outside, and Reverse equals IGDB's own reversed entry.
    /// </summary>
    public class GlazingBuilderRealTasTests
    {
        private readonly ITestOutputHelper output;

        public GlazingBuilderRealTasTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        [Trait("Category", "Tas")]
        public async Task Gate0_Orientation_ThroughTheBuilderRoute()
        {
            string path = Environment.GetEnvironmentVariable("SAM_E0_PILKINGTON_TCD");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            ConstructionManager panes = RunSta(() => Tas.Convert.ToSAM_ConstructionManager(path));
            MaterialLibrary materials = panes.MaterialLibrary;
            IMaterial Pane(string name) => materials.GetMaterial(name) ?? throw new InvalidOperationException(name + " is not in " + Path.GetFileName(path));

            DraftPane P(string name, bool reversed = false) => new DraftPane(Pane(name), double.NaN, Path.GetFileName(path), Path.GetFileName(path)) { Reversed = reversed };
            DraftGap Argon16() => new DraftGap(DefaultGasType.Argon, 0.016);

            using DraftGlazingEvaluator evaluator = new DraftGlazingEvaluator(null, TimeSpan.Zero);
            async Task<GlazingValues> Values(string label, params DraftLayer[] layers)
            {
                GlazingSystemDraft draft = new GlazingSystemDraft() { Name = label, IntendedPanelType = PanelType.WallExternal };
                draft.Add(layers);
                DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(draft);
                Assert.True(evaluation.State == DraftGlazingEvaluationState.Calculated, label + ": " + evaluation.Reason);
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-55} Ug {1:0.0000}  g {2:0.0000}  LT {3:0.0000}", label, evaluation.Values.Ug, evaluation.Values.G, evaluation.Values.LightTransmittance));
                return evaluation.Values;
            }

            // Absorption decides the absolute side: a tinted pane OUTSIDE gives the lower g.
            GlazingValues tintOutside = await Values("tint6 | Ar16 | clear6 (tint outside)", P("ArcticBlue6mm.NSG"), Argon16(), P("OptifloatClear6mm.NSG"));
            GlazingValues tintInside = await Values("clear6 | Ar16 | tint6 (tint inside)", P("OptifloatClear6mm.NSG"), Argon16(), P("ArcticBlue6mm.NSG"));
            Assert.True(tintOutside.G < tintInside.G - 0.1);
            Assert.Equal(tintOutside.Ug, tintInside.Ug, 3);

            // External = the face towards the outside: the coated External face of the INSIDE pane faces the cavity (low Ug).
            GlazingValues coatingOn3 = await Values("clear4 | Ar16 | S1Plus (coating on surface 3)", P("OptifloatClear4mm.NSG"), Argon16(), P("OptithermS1Plus4mm.NSG"));
            GlazingValues coatingOn1 = await Values("S1Plus | Ar16 | clear4 (coating on surface 1)", P("OptithermS1Plus4mm.NSG"), Argon16(), P("OptifloatClear4mm.NSG"));
            Assert.InRange(coatingOn3.Ug, 0.95, 1.15);
            Assert.InRange(coatingOn1.Ug, 2.4, 2.8);

            // Reverse = IGDB's own "... Reversed" entry, to the last digit Tas reports.
            GlazingValues reversed = await Values("S1Plus Reverse | Ar16 | clear4 (Builder Reverse)", P("OptithermS1Plus4mm.NSG", reversed: true), Argon16(), P("OptifloatClear4mm.NSG"));
            GlazingValues igdbReversed = await Values("S1Plus Reversed entry | Ar16 | clear4 (IGDB)", P("OptithermS1Plus4mm.NSG Reversed"), Argon16(), P("OptifloatClear4mm.NSG"));
            Assert.Equal(igdbReversed.Ug, reversed.Ug, 4);
            Assert.Equal(igdbReversed.G, reversed.G, 4);
            Assert.Equal(igdbReversed.LightTransmittance, reversed.LightTransmittance, 4);
            Assert.InRange(reversed.Ug, 0.95, 1.15);
            Assert.True(reversed.G < coatingOn3.G, "a low-e coating on surface 2 gives a lower g than on surface 3");
        }

        /// <summary>
        /// User-library PR3, through the product route and real Tas: a system saved with a REVERSED low-e pane, opened for editing (the pane reopens as the
        /// original with Reverse on) and composed again gives the same Ug / g / light transmittance as the draft it was saved from; an edit (a narrower
        /// gap) changes Ug; and the edited system saved with Save and replace, opened again, reproduces the edited values. Opt-in like Gate 0
        /// (<c>SAM_E0_PILKINGTON_TCD</c>).
        /// </summary>
        [Fact]
        [Trait("Category", "Tas")]
        public async Task EditedSystem_RoundTripAndReplace_ThroughTheBuilderRoute()
        {
            string path = Environment.GetEnvironmentVariable("SAM_E0_PILKINGTON_TCD");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            ConstructionManager panes = RunSta(() => Tas.Convert.ToSAM_ConstructionManager(path));
            MaterialLibrary materials = panes.MaterialLibrary;
            IMaterial Pane(string name) => materials.GetMaterial(name) ?? throw new InvalidOperationException(name + " is not in " + Path.GetFileName(path));
            DraftPane P(string name, bool reversed = false) => new DraftPane(Pane(name), double.NaN, Path.GetFileName(path), Path.GetFileName(path)) { Reversed = reversed };

            string directory = Path.Combine(Path.GetTempPath(), "SAM-UL-PR3-realtas", Guid.NewGuid().ToString("N"));
            try
            {
                UserGlazingLibrary library = new UserGlazingLibrary(Path.Combine(directory, "Glazing Systems.json"));
                using DraftGlazingEvaluator evaluator = new DraftGlazingEvaluator(null, TimeSpan.Zero);

                async Task<GlazingValues> Values(GlazingSystemDraft draft, string label)
                {
                    DraftGlazingEvaluation evaluation = await evaluator.EvaluateAsync(draft);
                    Assert.True(evaluation.State == DraftGlazingEvaluationState.Calculated, label + ": " + evaluation.Reason);
                    output.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-45} Ug {1:0.0000}  g {2:0.0000}  LT {3:0.0000}", label, evaluation.Values.Ug, evaluation.Values.G, evaluation.Values.LightTransmittance));
                    return evaluation.Values;
                }

                GlazingSystemDraft original = new GlazingSystemDraft() { Name = "Edit round trip", IntendedPanelType = PanelType.WallExternal };
                original.Add(P("OptithermS1Plus4mm.NSG", reversed: true), new DraftGap(DefaultGasType.Argon, 0.016), P("OptifloatClear4mm.NSG"));
                GlazingValues values_Original = await Values(original, "S1Plus reversed | Ar16 | clear4");

                UserGlazingSaveResult saved = library.Save(original, values_Original);
                Assert.True(saved.Succeeded, saved.Error);

                // Reopened for editing and composed again: the reversed pane is the original pane with Reverse on, and Tas gives the same answer.
                GlazingSystemDraft reopened = GlazingBuilderViewModel.SeedDraft(saved.Saved, GlazingSource.FromUserLibrary(library));
                Assert.True(((DraftPane)reopened.Layers[0]).Reversed);
                Assert.Equal("OptithermS1Plus4mm.NSG", ((DraftPane)reopened.Layers[0]).OriginalName);
                GlazingValues values_Reopened = await Values(reopened, "reopened for editing, unchanged");
                Assert.Equal(values_Original.Ug, values_Reopened.Ug, 4);
                Assert.Equal(values_Original.G, values_Reopened.G, 4);
                Assert.Equal(values_Original.LightTransmittance, values_Reopened.LightTransmittance, 4);

                // An edit: a 12 mm gap changes Ug; saved with Save and replace and opened again, it reproduces the edited values.
                ((DraftGap)reopened.Layers[1]).Thickness = 0.012;
                reopened.Name = "Edit round trip";                       // the old name, reused: allowed only when replacing
                GlazingValues values_Edited = await Values(reopened, "edited: Ar 12 mm");
                Assert.NotEqual(values_Original.Ug, values_Edited.Ug, 2);

                UserGlazingSaveResult replaced = library.SaveReplacing(reopened, saved.Saved.Guid, values_Edited);
                Assert.True(replaced.Succeeded, replaced.Error);
                Assert.Equal(new[] { replaced.Saved.Guid }, library.Read().Systems.Select(x => x.Guid));

                GlazingSystemDraft again = GlazingBuilderViewModel.SeedDraft(replaced.Saved, GlazingSource.FromUserLibrary(library));
                GlazingValues values_Again = await Values(again, "replacement opened again");
                Assert.Equal(values_Edited.Ug, values_Again.Ug, 4);
                Assert.Equal(values_Edited.G, values_Again.G, 4);
                Assert.Equal(values_Edited.LightTransmittance, values_Again.LightTransmittance, 4);
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception)
                {
                }
            }
        }

        private static T RunSta<T>(Func<T> func)
        {
            T result = default;
            Exception error = null;
            Thread thread = new Thread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception exception)
                {
                    error = exception;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (error != null)
            {
                throw error;
            }

            return result;
        }
    }
}
