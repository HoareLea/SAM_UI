// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static List<MechanicalSystem> AddMechanicalSystems(this UIAnalyticalModel uIAnalyticalModel, IEnumerable<Space> spaces = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            SystemTypeLibrary systemTypeLibrary = Analytical.Query.DefaultSystemTypeLibrary();
            if(systemTypeLibrary == null)
            {
                return null;
            }

            AddMechanicalSystemsWindow addMechanicalSystemsWindow = new AddMechanicalSystemsWindow();
            addMechanicalSystemsWindow.SupplyUnitName = "AHU1";
            addMechanicalSystemsWindow.ExhaustUnitName = "AHU1";
            addMechanicalSystemsWindow.VentilationRiserName = "RV1";
            addMechanicalSystemsWindow.HeatingRiserName = "RH1";
            addMechanicalSystemsWindow.CoolingRiserName = "RC1";

            bool? showDialogResult = addMechanicalSystemsWindow.ShowDialog();
            if(showDialogResult == null || !showDialogResult.HasValue || !showDialogResult.Value)
            {
                return null;
            }

            string supplyUnitName = addMechanicalSystemsWindow.SupplyUnitName;
            string exhaustUnitName = addMechanicalSystemsWindow.ExhaustUnitName;
            string ventilationRiserName = addMechanicalSystemsWindow.VentilationRiserName;
            string heatingRiserName = addMechanicalSystemsWindow.HeatingRiserName;
            string coolingRiserName = addMechanicalSystemsWindow.CoolingRiserName;

            List<MechanicalSystem> result = Analytical.Modify.AddMechanicalSystems(adjacencyCluster, systemTypeLibrary, spaces, supplyUnitName, exhaustUnitName, ventilationRiserName, heatingRiserName, coolingRiserName);

            uIAnalyticalModel.JSAMObject = new AnalyticalModel(analyticalModel, adjacencyCluster, analyticalModel.MaterialLibrary, analyticalModel.ProfileLibrary);

            return result;
        }
    }
}
