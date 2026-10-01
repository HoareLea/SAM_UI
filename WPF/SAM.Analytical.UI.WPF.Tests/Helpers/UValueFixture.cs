// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
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
    /// The U-value PR2a fixture: the real SIM_EXT_SLD wall (Air 50 / cement board 12 / mineral wool 80 / Air 50 /
    /// rainscreen 3, gas conductivities below the wool's, as in the model) on panels, plus a stand-in for the Tas
    /// TCD calls whose physics reproduces the values measured on the real calculator in the PR2a spike:
    /// U = 1 / (R + t / 0.025) with R = 0.65 m²K/W gives 0.26 at 80 mm, 1.45 at 1 mm and 0.0246 at 1000 mm.
    /// </summary>
    internal static class UValueFixture
    {
        public const string Air = "Air";
        public const string Board = "Cement Particleboard";
        public const string Wool = "I01_Mineral Wool";
        public const string Rainscreen = "Rainscreen";
        public const string WallName = "SIM_EXT_SLD";
        public const int WoolIndex = 2;

        // Every layer but the wool, plus surface resistances [m²K/W].
        public const double OtherResistance = 0.65;
        public const double WoolConductivity = 0.025;

        public static MaterialLibrary Materials()
        {
            MaterialLibrary result = new MaterialLibrary("Fixture");
            result.Add(Analytical.Create.GasMaterial(Air, string.Empty, Air, string.Empty, 0.024, 1000, 1.2, 1.8E-5, 0.05, 1, 5));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), Board, Board, "Fixture", 0.2, 1000, 1200));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), Wool, Wool, "Fixture", WoolConductivity, 1030, 20));
            result.Add(new OpaqueMaterial(Guid.NewGuid(), Rainscreen, Rainscreen, "Fixture", 50, 450, 7800));
            return result;
        }

        public static Construction Wall(string name = WallName, string defaultPanelType = "Wall")
        {
            Construction result = new Construction(Guid.NewGuid(), name, new List<ConstructionLayer>()
            {
                new ConstructionLayer(Air, 0.05),
                new ConstructionLayer(Board, 0.012),
                new ConstructionLayer(Wool, 0.08),
                new ConstructionLayer(Air, 0.05),
                new ConstructionLayer(Rainscreen, 0.003),
            });

            if (defaultPanelType != null)
            {
                result.SetValue(ConstructionParameter.DefaultPanelType, defaultPanelType);
            }

            return result;
        }

        public static Panel Panel(Construction construction, PanelType panelType, int index)
        {
            Face3D face3D = new Face3D(new Polygon3D(new List<Point3D>()
            {
                new Point3D(index * 5, 0, 0),
                new Point3D(index * 5 + 4, 0, 0),
                new Point3D(index * 5 + 4, 0, 3),
                new Point3D(index * 5, 0, 3),
            }));

            return Analytical.Create.Panel(construction, panelType, face3D);
        }

        /// <summary>A model with <paramref name="walls"/> external wall panels and <paramref name="roofs"/> roof panels on one construction.</summary>
        public static AnalyticalModel Model(out Construction construction, int walls = 12, int roofs = 0, params Construction[] others)
        {
            construction = Wall();
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            int index = 0;
            for (int i = 0; i < walls; i++)
            {
                adjacencyCluster.AddObject(Panel(construction, PanelType.WallExternal, index++));
            }

            for (int i = 0; i < roofs; i++)
            {
                adjacencyCluster.AddObject(Panel(construction, PanelType.Roof, index++));
            }

            foreach (Construction other in others)
            {
                adjacencyCluster.AddObject(Panel(other, PanelType.WallExternal, index++));
            }

            // With panels the construction lives only inside them (as in the real model); without, it is a stored object.
            if (walls + roofs == 0)
            {
                adjacencyCluster.AddObject(construction);
            }

            return new AnalyticalModel("UValue", null, null, null, adjacencyCluster, Materials(), new ProfileLibrary("Profiles"));
        }

        public static List<Guid> PanelGuids(AnalyticalModel analyticalModel, Construction construction)
        {
            return analyticalModel.AdjacencyCluster.GetPanels(construction).ConvertAll(x => x.Guid);
        }

        /// <summary>U-value of the fixture wall with the wool at <paramref name="thickness"/> [m].</summary>
        public static double U(double thickness)
        {
            return 1 / (OtherResistance + thickness / WoolConductivity);
        }

        /// <summary>The wool thickness giving <paramref name="thermalTransmittance"/> [m].</summary>
        public static double Thickness(double thermalTransmittance)
        {
            return WoolConductivity * (1 / thermalTransmittance - OtherResistance);
        }

        /// <summary>
        /// A <see cref="TasUValueEvaluator"/> on the fixture physics instead of TCD. <paramref name="fakeTas"/> counts the
        /// calls; zero debounce so tests do not wait.
        /// </summary>
        public static TasUValueEvaluator Evaluator(FakeTas fakeTas, TimeSpan? debounce = null)
        {
            return new TasUValueEvaluator(debounce ?? TimeSpan.Zero, fakeTas.ThermalTransmittances, fakeTas.LayerThickness);
        }
    }

    /// <summary>Stand-in for the two Tas TCD calls the evaluator makes, on <see cref="UValueFixture"/> physics.</summary>
    internal sealed class FakeTas
    {
        public int ThermalTransmittanceCalls;
        public int LayerThicknessCalls;
        public bool Unavailable;
        public ApartmentState LastApartment = ApartmentState.Unknown;

        public List<ThermalTransmittanceCalculationResult> ThermalTransmittances(ConstructionManager constructionManager, IEnumerable<Guid> guids)
        {
            Interlocked.Increment(ref ThermalTransmittanceCalls);
            LastApartment = Thread.CurrentThread.GetApartmentState();
            if (Unavailable)
            {
                return new List<ThermalTransmittanceCalculationResult>();
            }

            List<ThermalTransmittanceCalculationResult> result = new List<ThermalTransmittanceCalculationResult>();
            foreach (Guid guid in guids)
            {
                Construction construction = constructionManager.Constructions.Find(x => x.Guid == guid);
                double u = UValueFixture.U(construction.ConstructionLayers[UValueFixture.WoolIndex].Thickness);
                ThermalTransmittances thermalTransmittances = new ThermalTransmittances(u, u, u, u, u, u, 0);
                result.Add(new ThermalTransmittanceCalculationResult(guid, "Fake", 0, 0, 0, 0, 0, 0, 0, 0, thermalTransmittances));
            }

            return result;
        }

        public LayerThicknessCalculationResult LayerThickness(ConstructionManager constructionManager, LayerThicknessCalculationData data)
        {
            Interlocked.Increment(ref LayerThicknessCalls);
            LastApartment = Thread.CurrentThread.GetApartmentState();
            if (Unavailable)
            {
                return new LayerThicknessCalculationResult("Fake", data.ConstructionName, data.LayerIndex, double.NaN, double.NaN, data.ThermalTransmittance, double.NaN);
            }

            Construction construction = constructionManager.GetConstructions(data.ConstructionName).First();
            double initial = UValueFixture.U(construction.ConstructionLayers[UValueFixture.WoolIndex].Thickness);
            double thickness = UValueFixture.Thickness(data.ThermalTransmittance);
            if (!data.ThicknessRange.In(thickness))
            {
                return new LayerThicknessCalculationResult("Fake", data.ConstructionName, data.LayerIndex, double.NaN, initial, data.ThermalTransmittance, double.NaN);
            }

            return new LayerThicknessCalculationResult("Fake", data.ConstructionName, data.LayerIndex, thickness, initial, data.ThermalTransmittance, Math.Round(UValueFixture.U(thickness), 3));
        }
    }

    /// <summary>
    /// An <see cref="IUValueEvaluator"/> whose results the test releases by hand, to prove stale results are dropped.
    /// </summary>
    internal sealed class ManualUValueEvaluator : IUValueEvaluator
    {
        public sealed class Call
        {
            public UValueEvaluationRequest Request;
            public CancellationToken CancellationToken;
            public TaskCompletionSource<UValueEvaluation> Completion = new TaskCompletionSource<UValueEvaluation>();
        }

        private readonly TasUValueEvaluator inner = UValueFixture.Evaluator(new FakeTas());

        public List<Call> Calls { get; } = new List<Call>();

        public Task<UValueEvaluation> EvaluateAsync(UValueEvaluationRequest request, CancellationToken cancellationToken)
        {
            Call call = new Call() { Request = request, CancellationToken = cancellationToken };
            Calls.Add(call);
            return call.Completion.Task;
        }

        /// <summary>Completes <paramref name="call"/> with the fixture result for its request, ignoring its token (as a COM call would).</summary>
        public void Complete(Call call)
        {
            call.Completion.SetResult(inner.Evaluate(call.Request));
        }
    }

    /// <summary>An <see cref="IUValueEvaluator"/> that answers at once on the fixture physics and records the requests.</summary>
    internal sealed class ImmediateUValueEvaluator : IUValueEvaluator
    {
        private readonly TasUValueEvaluator inner;

        public ImmediateUValueEvaluator(FakeTas fakeTas = null)
        {
            FakeTas = fakeTas ?? new FakeTas();
            inner = UValueFixture.Evaluator(FakeTas);
        }

        public FakeTas FakeTas { get; }

        public List<UValueEvaluationRequest> Requests { get; } = new List<UValueEvaluationRequest>();

        public Task<UValueEvaluation> EvaluateAsync(UValueEvaluationRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(inner.Evaluate(request));
        }
    }
}
