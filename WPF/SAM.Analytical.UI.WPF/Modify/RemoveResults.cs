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

            Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
            foreach (Core.Result result in results)
            {
                if(result is Geometry.SolarCalculator.SolarFaceSimulationResult)
                {
                    dictionary["Solar Face Simulation Result"] = typeof(Geometry.SolarCalculator.SolarFaceSimulationResult);
                }
            }

            List<Type> types = new List<Type>();
            using (TreeViewForm<string> treeViewForm = new TreeViewForm<string>("Select Result Types", dictionary.Keys))
            {
                if(treeViewForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                foreach(string typeName in treeViewForm.SelectedItems)
                {
                    if(dictionary.TryGetValue(typeName, out Type type) && type != null)
                    {
                        types.Add(type);
                    }
                }
            }

            if(types == null || types.Count == 0)
            {
                return;
            }

            List<Core.SAMObject> sAMObjects = new List<Core.SAMObject>();
            foreach (Core.Result result in results)
            {
                if(types.Contains(result.GetType()))
                {
                    sAMObjects.Add(result);
                }
            }

            List<Guid> guids = analyticalModel.Remove(sAMObjects);
            sAMObjects.RemoveAll(x => !guids.Contains(x.Guid));

            if(sAMObjects == null || sAMObjects.Count == 0)
            {
                return;
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel, new AnalyticalModelModification(sAMObjects));

        }
    }
}