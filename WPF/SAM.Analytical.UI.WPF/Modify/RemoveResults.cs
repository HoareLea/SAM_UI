// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Windows.Forms;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void RemoveResults(this UIAnalyticalModel uIAnalyticalModel)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if(analyticalModel == null)
            {
                return;
            }

            List<Core.Result> results = analyticalModel.GetResults<Core.Result>();
            if(results == null || results.Count == 0)
            {
                return;
            }

            List<Tuple<string, string, List<Core.Result>>> tuples = new List<Tuple<string, string, List<Core.Result>>>();

            //Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
            foreach (Core.Result result in results)
            {
                string name = result.DateTime.ToString("yyyy/MM/dd HH:mm");
                if(!string.IsNullOrWhiteSpace(result.Source))
                {
                    name = string.Format("{0} - {1}", name, result.Source);
                }

                if(string.IsNullOrWhiteSpace(name))
                {
                    name = "???";
                }

                string group = null;

                if (result is Geometry.SolarCalculator.SolarFaceSimulationResult)
                {
                    group = "Solar Face Simulation Result";
                }
                else if (result is SpaceSimulationResult)
                {
                    group = "Space Simulation Result";
                }
                else if (result is SurfaceSimulationResult)
                {
                    group = "Surface Simulation Result";
                }
                else if (result is AnalyticalModelSimulationResult)
                {
                    group = "Analytical Model Simulation Result";
                }

                if(string.IsNullOrWhiteSpace(group))
                {
                    continue;
                }

                Tuple<string, string, List<Core.Result>> tuple = tuples.Find(x => x.Item1 == group && x.Item2 == name);
                if(tuple == null)
                {
                    tuple = new Tuple<string, string, List<Core.Result>>(group, name, new List<Core.Result>());
                    tuples.Add(tuple);
                }

                tuple.Item3.Add(result);
            }

            List<Core.Result> results_Selected = new List<Core.Result>();
            using (TreeViewForm<Tuple<string, string, List<Core.Result>>> treeViewForm = new TreeViewForm<Tuple<string, string, List<Core.Result>>>("Select Result Types", tuples, x => x.Item2, x => x.Item1, @checked: x => true))
            {
                if(treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                foreach(Tuple<string, string, List<Core.Result>> tuple in treeViewForm.SelectedItems)
                {
                    if(tuple?.Item3 != null)
                    {
                        results_Selected.AddRange(tuple.Item3);
                    }
                }
            }

            if(results_Selected == null || results_Selected.Count == 0)
            {
                return;
            }

            List<Guid> guids = analyticalModel.Remove(results_Selected);
            results_Selected.RemoveAll(x => !guids.Contains(x.Guid));

            if(results_Selected == null || results_Selected.Count == 0)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new AnalyticalModelModification(results_Selected));

        }
    }
}
