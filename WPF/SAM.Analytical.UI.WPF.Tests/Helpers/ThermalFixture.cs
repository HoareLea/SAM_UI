// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>The parts of a mixed model: walls on two constructions and windows of the current glazing system.</summary>
    internal sealed class ThermalParts
    {
        public AnalyticalModel Model;
        public Construction Wall;
        public Construction OtherWall;
        public List<Guid> WallPanels;
        public List<Guid> Windows;
    }

    /// <summary>A model with opaque and glazing elements together, for the thermal change set and the editing session.</summary>
    internal static class ThermalFixture
    {
        public const double Thickness_U030 = 0.06708333333;

        /// <summary>12 walls on one construction, 4 on another ("OTHER_WALL"), and 6 windows of the current glazing system.</summary>
        public static ThermalParts Build(int walls = 12, int windows = 6)
        {
            AnalyticalModel uModel = UValueFixture.Model(out Construction wall, walls, 0, UValueFixture.Wall("OTHER_WALL"));
            AnalyticalModel gModel = GlazingFixture.Model(windows);

            AdjacencyCluster adjacencyCluster = uModel.AdjacencyCluster;
            foreach (Panel panel in gModel.AdjacencyCluster.GetPanels())
            {
                adjacencyCluster.AddObject(panel);
            }

            MaterialLibrary materials = GlazingFixture.ModelMaterials();
            foreach (IMaterial material in uModel.MaterialLibrary.GetMaterials())
            {
                materials.Add(material);
            }

            AnalyticalModel model = new AnalyticalModel("Mixed", null, null, null, adjacencyCluster, materials, new ProfileLibrary("Profiles"));
            Construction other = model.AdjacencyCluster.GetConstructions().Find(x => x.Name == "OTHER_WALL");

            return new ThermalParts()
            {
                Model = model,
                Wall = wall,
                OtherWall = other,
                WallPanels = UValueFixture.PanelGuids(model, wall),
                Windows = GlazingFixture.ApertureGuids(model, GlazingFixture.CurrentGuid),
            };
        }

        public static SetUValueRequest Opaque(Construction construction, ThermalApplyScope scope = ThermalApplyScope.AllUsing, IEnumerable<Guid> selected = null)
        {
            return new SetUValueRequest()
            {
                ConstructionGuid = construction.Guid,
                LayerIndex = UValueFixture.WoolIndex,
                Thickness = Thickness_U030,
                InitialThermalTransmittance = 0.26,
                CalculatedThermalTransmittance = 0.3,
                TargetThermalTransmittance = 0.3,
                HeatFlowDirection = HeatFlowDirection.Horizontal,
                Mode = UValueApplyMode.NewConstruction,
                Scope = scope,
                SelectedPanelGuids = selected,
            };
        }
    }
}
