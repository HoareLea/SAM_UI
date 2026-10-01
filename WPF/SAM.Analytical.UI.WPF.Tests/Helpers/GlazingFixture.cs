// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// The U-value PR3 fixture: a model whose external wall panels carry windows of one glazing system ("GLZ": clear /
    /// argon / clear in a frame), a default-library stand-in with a better system that SHARES the name "GLZ" (as the real
    /// default library does five times), a pane-only system, a system whose material is missing and a door, and a
    /// stand-in for the Tas glazing calculation that answers from a table.
    /// </summary>
    internal static class GlazingFixture
    {
        public const string Clear = "Clear6";
        public const string LowE = "LowE6";
        public const string Argon = "Argon12";
        public const string FrameMaterial = "Frame";
        public const string CurrentName = "GLZ";

        public static readonly Guid CurrentGuid = new Guid("a0000000-0000-4000-8000-000000000001");
        public static readonly Guid BetterGuid = new Guid("a0000000-0000-4000-8000-000000000002");
        public static readonly Guid PaneOnlyGuid = new Guid("a0000000-0000-4000-8000-000000000003");
        public static readonly Guid MissingMaterialGuid = new Guid("a0000000-0000-4000-8000-000000000004");
        public static readonly Guid DoorGuid = new Guid("a0000000-0000-4000-8000-000000000005");
        public static readonly Guid DifferentMaterialGuid = new Guid("a0000000-0000-4000-8000-000000000006");
        public static readonly Guid LoadedGuid = new Guid("a0000000-0000-4000-8000-000000000007");
        public static readonly Guid SolidGuid = new Guid("a0000000-0000-4000-8000-000000000008");
        public static readonly Guid SolidDoorGuid = new Guid("a0000000-0000-4000-8000-000000000009");

        // Ug, g, light, Uf (NaN: no frame). The Tas stand-in answers these by system Guid.
        public static readonly Dictionary<Guid, GlazingValues> Values = new Dictionary<Guid, GlazingValues>()
        {
            [CurrentGuid] = new GlazingValues(1.40, 0.60, 0.78, 2.00),
            [BetterGuid] = new GlazingValues(1.10, 0.50, 0.70, 2.00),
            [PaneOnlyGuid] = new GlazingValues(1.30, 0.62, 0.80, double.NaN),
            [MissingMaterialGuid] = new GlazingValues(0.90, 0.40, 0.70, 2.00),
            [DoorGuid] = new GlazingValues(2.00, 0, 0, 2.00),
            [DifferentMaterialGuid] = new GlazingValues(1.05, 0.55, 0.75, 2.00),
            [LoadedGuid] = new GlazingValues(0.85, 0.45, 0.72, 1.80),
        };

        /// <summary>The Tas stand-in's answer for a system; a system the table does not list (a loaded one) gets ordinary values.</summary>
        public static GlazingValues ValuesOf(Guid guid)
        {
            return Values.TryGetValue(guid, out GlazingValues values) ? values : new GlazingValues(1.50, 0.55, 0.75, 2.00);
        }

        public static IMaterial ClearGlass(double conductivity = 1)
        {
            return Analytical.Create.TransparentMaterial(Clear, string.Empty, Clear, "Clear", conductivity, 0.006, 1, 0.8, 0.88, 0.07, 0.07, 0.08, 0.08, 0.84, 0.84, false);
        }

        public static IMaterial LowEGlass()
        {
            return Analytical.Create.TransparentMaterial(LowE, string.Empty, LowE, "Low-e", 1, 0.006, 1, 0.6, 0.8, 0.1, 0.1, 0.1, 0.1, 0.84, 0.1, false);
        }

        public static IMaterial ArgonGas()
        {
            return Analytical.Create.GasMaterial(Argon, string.Empty, Argon, "Argon", 0.016, 520, 1.78, 2.2E-5, 0.012, 1, double.NaN);
        }

        public static IMaterial FrameOpaque()
        {
            return new OpaqueMaterial(Guid.NewGuid(), FrameMaterial, FrameMaterial, "Frame", 0.17, 1000, 700);
        }

        public static MaterialLibrary ModelMaterials()
        {
            MaterialLibrary result = new MaterialLibrary("Model");
            result.Add(ClearGlass());
            result.Add(ArgonGas());
            result.Add(FrameOpaque());
            return result;
        }

        public static ApertureConstruction System(Guid guid, string name, ApertureType type, string outer, bool frame = true, string description = null)
        {
            ApertureConstruction result = new ApertureConstruction(guid, name, type,
                new List<ConstructionLayer>() { new ConstructionLayer(outer, 0.006), new ConstructionLayer(Argon, 0.012), new ConstructionLayer(Clear, 0.006) },
                frame ? new List<ConstructionLayer>() { new ConstructionLayer(FrameMaterial, 0.05) } : null);

            if (description != null)
            {
                result.SetValue(ApertureConstructionParameter.Description, description);
            }

            return result;
        }

        public static ApertureConstruction Current() => System(CurrentGuid, CurrentName, ApertureType.Window, Clear);

        /// <summary>A system without glass: a timber panel in a frame.</summary>
        public static ApertureConstruction Solid(Guid guid, string name, ApertureType type)
        {
            return new ApertureConstruction(guid, name, type, new List<ConstructionLayer>() { new ConstructionLayer(FrameMaterial, 0.05) }, new List<ConstructionLayer>() { new ConstructionLayer(FrameMaterial, 0.05) });
        }

        /// <summary>The default-library stand-in: a better system with the SAME name, a pane-only one, one with a missing material, a door, and a twin of the current system.</summary>
        public static GlazingSource Library()
        {
            MaterialLibrary materials = ModelMaterials();
            materials.Add(LowEGlass());

            List<ApertureConstruction> systems = new List<ApertureConstruction>()
            {
                Current(),
                System(BetterGuid, CurrentName, ApertureType.Window, LowE, true, "Low-e double glazing"),
                System(PaneOnlyGuid, "GLZ_Pane", ApertureType.Window, Clear, false),
                System(MissingMaterialGuid, "GLZ_Missing", ApertureType.Window, "Mystery"),
                System(DoorGuid, "DOOR", ApertureType.Door, Clear),
                Solid(SolidGuid, "SOLID", ApertureType.Window),
                Solid(SolidDoorGuid, "SOLID_DOOR", ApertureType.Door),
            };

            return new GlazingSource(GlazingSourceKind.Library, "Default library", new ConstructionManager(systems, null, materials));
        }

        /// <summary>A loaded source: a system with its own "Clear6" that DIFFERS from the model's, and a good one built from materials the model has.</summary>
        public static GlazingSource Loaded()
        {
            MaterialLibrary materials = new MaterialLibrary("Loaded");
            materials.Add(ClearGlass(conductivity: 0.5));
            materials.Add(ArgonGas());
            materials.Add(FrameOpaque());

            return new GlazingSource(GlazingSourceKind.Loaded, "loaded.json", new ConstructionManager(new List<ApertureConstruction>() { System(DifferentMaterialGuid, "GLZ_Differs", ApertureType.Window, Clear) }, null, materials));
        }

        public static GlazingSource LoadedGood()
        {
            MaterialLibrary materials = ModelMaterials();
            materials.Add(LowEGlass());

            return new GlazingSource(GlazingSourceKind.Loaded, "good.json", new ConstructionManager(new List<ApertureConstruction>() { System(LoadedGuid, "GLZ_Good", ApertureType.Window, LowE) }, null, materials));
        }

        /// <summary>A panel with a 2.0 x 1.5 m window (frame 0.05 m: pane 2.66 m², frame 0.34 m²) on a 4 x 3 m wall.</summary>
        public static Panel PanelWithWindow(ApertureConstruction apertureConstruction, int index, out Aperture aperture)
        {
            double x = index * 5;
            Construction construction = new Construction(new Guid("b0000000-0000-4000-8000-000000000001"), "Wall", new List<ConstructionLayer>() { new ConstructionLayer(FrameMaterial, 0.2) });
            Panel panel = Analytical.Create.Panel(construction, PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D>()
            {
                new Point3D(x, 0, 0), new Point3D(x + 4, 0, 0), new Point3D(x + 4, 0, 3), new Point3D(x, 0, 3),
            })));

            aperture = Analytical.Create.Aperture(apertureConstruction, new Face3D(new Polygon3D(new List<Point3D>()
            {
                new Point3D(x + 1, 0, 0.75), new Point3D(x + 3, 0, 0.75), new Point3D(x + 3, 0, 2.25), new Point3D(x + 1, 0, 2.25),
            })));

            panel.AddAperture(aperture);
            return panel;
        }

        /// <summary>A model with <paramref name="windows"/> windows of the current system and <paramref name="others"/> windows of another system.</summary>
        public static AnalyticalModel Model(int windows = 20, ApertureConstruction other = null, int others = 0)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            ApertureConstruction current = Current();
            int index = 0;
            for (int i = 0; i < windows; i++)
            {
                adjacencyCluster.AddObject(PanelWithWindow(current, index++, out Aperture _));
            }

            for (int i = 0; i < others; i++)
            {
                adjacencyCluster.AddObject(PanelWithWindow(other, index++, out Aperture _));
            }

            return new AnalyticalModel("Glazing", null, null, null, adjacencyCluster, ModelMaterials(), new ProfileLibrary("Profiles"));
        }

        public static List<Guid> ApertureGuids(AnalyticalModel analyticalModel, Guid apertureConstructionGuid)
        {
            return (analyticalModel.AdjacencyCluster.GetApertures() ?? new List<Aperture>()).Where(x => x.TypeGuid == apertureConstructionGuid).Select(x => x.Guid).ToList();
        }

        public static GlazingViewModel ViewModel(AnalyticalModel analyticalModel, IEnumerable<Guid> selected = null, IGlazingEvaluator evaluator = null, GlazingSource library = null)
        {
            return new GlazingViewModel(analyticalModel, CurrentGuid, selected, evaluator ?? new FakeGlazingEvaluator(), library ?? Library());
        }
    }

    /// <summary>The Tas glazing calculation answered from <see cref="GlazingFixture.Values"/>; counts its calls.</summary>
    internal sealed class FakeGlazingEvaluator : IGlazingEvaluator
    {
        public List<GlazingEvaluationRequest> Requests { get; } = new List<GlazingEvaluationRequest>();

        public bool Fail { get; set; }

        public Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Fail)
            {
                return Task.FromResult(new GlazingEvaluation(null, 5, "Tas could not calculate the glazing values."));
            }

            Dictionary<Guid, GlazingValues> values = new Dictionary<Guid, GlazingValues>();
            foreach (GlazingEvaluationBatch batch in request.Batches)
            {
                foreach (Guid guid in batch.Guids)
                {
                    values[guid] = GlazingFixture.ValuesOf(guid);
                }
            }

            return Task.FromResult(new GlazingEvaluation(values, 7));
        }

        public int GuidsRequested => Requests.Sum(x => x.Batches.Sum(y => y.Guids.Count));
    }

    /// <summary>An evaluator whose answers the test releases by hand, to prove results arrive in any order.</summary>
    internal sealed class ManualGlazingEvaluator : IGlazingEvaluator
    {
        public sealed class Call
        {
            public GlazingEvaluationRequest Request;
            public TaskCompletionSource<GlazingEvaluation> Completion = new TaskCompletionSource<GlazingEvaluation>();
        }

        public List<Call> Calls { get; } = new List<Call>();

        public Task<GlazingEvaluation> EvaluateAsync(GlazingEvaluationRequest request, CancellationToken cancellationToken)
        {
            Call call = new Call() { Request = request };
            Calls.Add(call);
            return call.Completion.Task;
        }

        public void Release(Call call)
        {
            Dictionary<Guid, GlazingValues> values = new Dictionary<Guid, GlazingValues>();
            foreach (Guid guid in call.Request.Batches.SelectMany(x => x.Guids))
            {
                values[guid] = GlazingFixture.ValuesOf(guid);
            }

            call.Completion.SetResult(new GlazingEvaluation(values, 1));
        }
    }
}
