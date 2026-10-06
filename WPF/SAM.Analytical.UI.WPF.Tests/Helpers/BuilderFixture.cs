// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// Stage E0-1 (Glazing System Builder domain) fixture: pane materials shaped like the IGDB panes used in the Gate 0 probe (a clear pane, a
    /// body-tinted pane, an asymmetric low-e pane whose External face is the coated one, and its IGDB-style "Reversed" twin), gas definitions
    /// with the properties of SAM's default gases (so no test depends on the machine's resources), a seed system with a frame, and a stand-in
    /// for the Tas glazing calculation that counts calls and can be held, emptied or failed.
    /// </summary>
    internal static class BuilderFixture
    {
        public const string Source = "IGDB-test.tcd";
        public const string Clear = "Clear4";
        public const string Tint = "Tint6";
        public const string LowE = "LowE4";
        public const string FrameMaterial = "Timber frame";

        public static TransparentMaterial ClearPane(string name = Clear, double conductivity = 1)
        {
            return Analytical.Create.TransparentMaterial(name, string.Empty, name, "Clear float", conductivity, 0.004, 9999, 0.85, 0.90, 0.076, 0.076, 0.082, 0.082, 0.84, 0.84, false);
        }

        public static TransparentMaterial TintPane()
        {
            return Analytical.Create.TransparentMaterial(Tint, string.Empty, Tint, "Body tinted", 1, 0.006, 9999, 0.35, 0.54, 0.05, 0.05, 0.057, 0.057, 0.84, 0.84, false);
        }

        /// <summary>External = the coated face (as IGDB stores Pilkington low-e panes): solar reflectance 0.40 / 0.31, emissivity 0.025 / 0.84.</summary>
        public static TransparentMaterial LowEPane()
        {
            return Analytical.Create.TransparentMaterial(LowE, string.Empty, LowE, "Low-e", 1, 0.004, 9999, 0.52, 0.82, 0.40, 0.31, 0.12, 0.14, 0.025, 0.84, false);
        }

        /// <summary>The IGDB "… Reversed" entry of <see cref="LowEPane"/>: the External and Internal values swapped.</summary>
        public static TransparentMaterial LowEPaneReversedEntry()
        {
            return Analytical.Create.TransparentMaterial(LowE + " Reversed", string.Empty, LowE + " Reversed", "Low-e", 1, 0.004, 9999, 0.52, 0.82, 0.31, 0.40, 0.14, 0.12, 0.84, 0.025, false);
        }

        public static OpaqueMaterial Frame()
        {
            OpaqueMaterial result = new OpaqueMaterial(Guid.NewGuid(), FrameMaterial, FrameMaterial, "Frame", 0.13, 500, 1600);
            result.SetValue(Core.MaterialParameter.DefaultThickness, 0.07);
            return result;
        }

        /// <summary>Gas definitions with the default SAM gases' properties (λ, ρ, cp, μ).</summary>
        public static GasMaterial Gas(DefaultGasType gasType)
        {
            switch (gasType)
            {
                case DefaultGasType.Air:
                    return Analytical.Create.GasMaterial("Test Air", string.Empty, "Test Air", "Air", 0.02496, 1008, 1.232, 1.761E-05, 0.012, 1, double.NaN, DefaultGasType.Air);
                case DefaultGasType.Argon:
                    return Analytical.Create.GasMaterial("Test Argon", string.Empty, "Test Argon", "Argon", 0.01684, 519, 1.699, 2.164E-05, 0.012, 1, double.NaN, DefaultGasType.Argon);
                case DefaultGasType.Krypton:
                    return Analytical.Create.GasMaterial("Test Krypton", string.Empty, "Test Krypton", "Krypton", 0.0090, 245, 3.56, 2.4E-05, 0.012, 1, double.NaN, DefaultGasType.Krypton);
                case DefaultGasType.Xenon:
                    return Analytical.Create.GasMaterial("Test Xenon", string.Empty, "Test Xenon", "Xenon", 0.0054, 161, 5.68, 2.3E-05, 0.012, 1, double.NaN, DefaultGasType.Xenon);
                default:
                    return null;
            }
        }

        public static GlazingComposeOptions Options(Guid? guid = null) => new GlazingComposeOptions() { Guid = guid, GasSource = Gas };

        public static GlazingComposition Compose(GlazingSystemDraft draft, Guid? guid = null) => draft.ComposeGlazingSystem(Options(guid));

        public static GlazingDraftValidation Check(GlazingSystemDraft draft, IEnumerable<string> savedNames = null) => draft.CheckGlazingDraft(Compose(draft), savedNames);

        public static DraftPane Pane(IMaterial material, bool reversed = false, string source = Source)
        {
            return new DraftPane(material, double.NaN, source, source) { Reversed = reversed };
        }

        public static DraftGap Gap(double millimetres = 16, DefaultGasType gasType = DefaultGasType.Argon) => new DraftGap(gasType, millimetres / 1000);

        /// <summary>Outside → inside: clear | Argon 16 | low-e (coating facing the cavity: External of the inside pane).</summary>
        public static GlazingSystemDraft Double(string name = "E0 Double")
        {
            GlazingSystemDraft result = new GlazingSystemDraft() { Name = name, IntendedPanelType = PanelType.WallExternal };
            result.Add(Pane(ClearPane()), Gap(), Pane(LowEPane()));
            result.Frame = DraftFrame.CopyFrom(Seed(), SeedMaterials(), "Default library");
            return result;
        }

        public static GlazingSystemDraft Triple(string name = "E0 Triple")
        {
            GlazingSystemDraft result = new GlazingSystemDraft() { Name = name, IntendedPanelType = PanelType.WallExternal };
            result.Add(Pane(ClearPane()), Gap(12), Pane(ClearPane()), Gap(12), Pane(LowEPane()));
            result.Frame = DraftFrame.CopyFrom(Seed(), SeedMaterials(), "Default library");
            return result;
        }

        public static readonly Guid SeedGuid = new Guid("e0000000-0000-4000-8000-000000000001");

        /// <summary>A complete system to copy a frame from (SAM order inside → outside), with a 60 mm Default Frame Width.</summary>
        public static ApertureConstruction Seed(bool frameWidth = true)
        {
            ApertureConstruction result = new ApertureConstruction(SeedGuid, "SEED_GLZ", ApertureType.Window,
                new List<ConstructionLayer>() { new ConstructionLayer(Clear, 0.004), new ConstructionLayer("Seed air", 0.012), new ConstructionLayer(Clear, 0.004) },
                new List<ConstructionLayer>() { new ConstructionLayer(FrameMaterial, 0.07) });
            result.SetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, 10.0);
            if (frameWidth)
            {
                result.SetValue(ApertureConstructionParameter.DefaultFrameWidth, 0.06);
            }

            return result;
        }

        public static MaterialLibrary SeedMaterials()
        {
            MaterialLibrary result = new MaterialLibrary("Seed");
            result.Add(ClearPane());
            result.Add(Frame());
            result.Add(Gas(DefaultGasType.Air));
            result.Add(TintPane());
            return result;
        }

        /// <summary>A fresh temporary folder for one test's library file.</summary>
        public static string TempDirectory()
        {
            string result = Path.Combine(Path.GetTempPath(), "SAM-E0-1-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(result);
            return result;
        }

        public static UserGlazingLibrary Library(string directory, TimeSpan? lockTimeout = null)
        {
            return new UserGlazingLibrary(Path.Combine(directory, "Glazing Systems.json"), Gas, lockTimeout);
        }
    }

    /// <summary>
    /// The Tas glazing calculation stand-in for the Builder: answers Ug from the number of panes (and g / LT / Uf fixed), records every request
    /// (with the layer names it was asked for), and can hold a call until released, answer nothing, report an error or throw.
    /// </summary>
    internal sealed class FakeDraftTas : IGlazingEvaluator
    {
        private readonly object gate = new object();
        private TaskCompletionSource<bool> hold;

        public List<List<string>> Requests { get; } = new List<List<string>>();

        public int Calls { get { lock (gate) { return Requests.Count; } } }

        public bool Empty { get; set; }

        public string Error { get; set; }

        public bool Throw { get; set; }

        public double Ug { get; set; } = double.NaN;

        /// <summary>The next calls wait until <see cref="Release"/>.</summary>
        public void Hold()
        {
            hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Release()
        {
            hold?.TrySetResult(true);
        }

        public async Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, CancellationToken cancellationToken)
        {
            GlazingEvaluationBatch batch = request.Batches.Single();
            ApertureConstruction apertureConstruction = batch.Source.ConstructionManager.ApertureConstructions.Single(x => x.Guid == batch.Guids.Single());
            lock (gate)
            {
                Requests.Add(apertureConstruction.PaneConstructionLayers.Select(x => x.Name).ToList());
            }

            TaskCompletionSource<bool> wait = hold;
            if (wait != null)
            {
                await wait.Task;
            }

            if (Throw)
            {
                throw new InvalidOperationException("TCD is not registered.");
            }

            Dictionary<Guid, GlazingValues> values = new Dictionary<Guid, GlazingValues>();
            if (!Empty && Error == null)
            {
                int panes = apertureConstruction.PaneConstructionLayers.Count(x => batch.Source.ConstructionManager.MaterialLibrary.GetMaterial(x.Name) is TransparentMaterial);
                double ug = double.IsNaN(Ug) ? 5.7 / panes : Ug;
                values[apertureConstruction.Guid] = new GlazingValues(ug, 0.5, 0.7, apertureConstruction.HasFrameConstructionLayers() ? 1.8 : double.NaN);
            }

            return new GlazingEvaluation(values, 1, Error);
        }
    }
}
