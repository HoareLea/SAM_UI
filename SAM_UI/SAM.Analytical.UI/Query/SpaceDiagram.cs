// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Mollier;
using SAM.Core.Mollier;
using SAM.Core.Mollier.UI;
using SAM.Core.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        public static void SpaceDiagram(this UIAnalyticalModel uIAnalyticalModel, IWin32Window owner = null)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return;
            }

            Space space = null;

            List<Space> spaces = adjacencyCluster.GetObjects<Space>()?.FindAll(x => x != null);
            if(spaces == null || spaces.Count == 0)
            {
                MessageBox.Show("Could not find any space");
                return;
            }

            spaces.Sort((x, y) => x.Name.CompareTo(y.Name));
            using (ComboBoxForm<Space> comboBoxForm = new ComboBoxForm<Space>("Spaces", spaces, (Space x) => x?.Name))
            {
                comboBoxForm.SelectedItem = spaces.FirstOrDefault();

                if (comboBoxForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                space = comboBoxForm.SelectedItem;
            }

            if(space == null)
            {
                return;
            }

            MollierGroup mollierGroup = null;

            space.SetValue(Mollier.SpaceParameter.AirHandlingUnitCalculationMethod, AirHandlingUnitCalculationMethod.FixedSupplyTemperature);
            adjacencyCluster.AddObject(space);

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            List<VentilationSystem> ventilationSystems = adjacencyCluster.Systems<VentilationSystem>(space);
            if (ventilationSystems == null || ventilationSystems.Count == 0)
            {
                MessageBox.Show("Space has no vantilation system connected");
                return;
            }

            VentilationSystem ventilationSystem = ventilationSystems.Find(x => x != null);

            string unitName = null;
            if (!ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out unitName) || string.IsNullOrWhiteSpace(unitName))
            {
                unitName = null;
            }

            if (!string.IsNullOrEmpty(unitName))
            {
                AirHandlingUnit airHandlingUnit = adjacencyCluster?.GetObjects<AirHandlingUnit>()?.Find(x => x.Name == unitName);
                if (airHandlingUnit != null)
                {
                    AirHandlingUnitResult airHandlingUnitResult = Mollier.Create.AirHandlingUnitResult(analyticalModel, airHandlingUnit.Name, space);
                    if (airHandlingUnitResult != null)
                    {
                        mollierGroup = airHandlingUnitResult.GetValue<MollierGroup>(AirHandlingUnitResultParameter.Processes);
                    }
                }
            }

            List<IMollierProcess> mollierProcesses = mollierGroup?.GetObjects<IMollierProcess>();
            if(mollierProcesses == null)
            {
                return;
            }

            double pressure = Core.Mollier.UI.Query.DefaultPressure(null, mollierProcesses);

            using (MollierForm mollierForm = new MollierForm() { ReadOnly = true, WindowState = FormWindowState.Normal })
            {
                mollierForm.Name = string.IsNullOrWhiteSpace(space.Name) ? mollierForm.Name : space.Name;
                mollierForm.MollierControlSettings = Core.Mollier.UI.Query.DefaultMollierControlSettings();
                mollierForm.Pressure = pressure;
                //mollierForm.AddProcesses(mollierProcesses, false);
                mollierForm.AddMollierObjects(mollierProcesses, false);

                if(mollierForm.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }
            }

            return;
        }
    }
}
