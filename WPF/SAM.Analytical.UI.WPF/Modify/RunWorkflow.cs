// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static AnalyticalModel? RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            if (workflowSettings == null)
            {
                workflowSettings = new WorkflowSettings();
            }

            WorkflowCalculator workflowCalculator = new (workflowSettings);

            return workflowCalculator.Calculate(analyticalModel);
        }

        public static Dictionary<string, AnalyticalModel>? RunWorkflow(this IEnumerable<AnalyticalModel> analyticalModels, WorkflowSettings workflowSettings, string directory, bool parallel = true, int? maxDegreeOfParallelism = null, bool saveAnalyticalModels = false)
        {
            if (workflowSettings == null || analyticalModels is null || !analyticalModels.Any())
            {
                return null;
            }

            List<Tuple<string?, string, AnalyticalModel>> tuples = [.. Enumerable.Repeat<Tuple<string?, string, AnalyticalModel>>(null, analyticalModels.Count())];

            Func<int, int, string> getName = (i, count) =>
            {
                int charCount = count.ToString().Length;

                string result = i.ToString();
                while (result.Length < charCount)
                {
                    result = "0" + result;
                }

                return result;
            };

            int count = analyticalModels.Count();

            Action<int> action = i =>
            {
                AnalyticalModel analyticalModel = analyticalModels.ElementAt(i);

                if (!analyticalModel.TryGetValue("CaseDescription", out string caseDescription) || string.IsNullOrWhiteSpace(caseDescription))//CaseDescription
                {
                    caseDescription = i.ToString();
                }

                if (analyticalModel.Name is not string name || string.IsNullOrWhiteSpace(name))
                {
                    name = i.ToString();
                }

                string directory_AnalyticalModel = Path.Combine(directory, i.ToString() == caseDescription ? getName(i, count) : string.Format("{0} {1}", getName(i, count), caseDescription));
                if (!Directory.Exists(directory_AnalyticalModel))
                {
                    Directory.CreateDirectory(directory_AnalyticalModel);
                }

                string path_gbXML = Path.Combine(directory_AnalyticalModel, name + ".gbXML");

                gbXMLSerializer.gbXML gbXML = Analytical.gbXML.Convert.TogbXML(analyticalModel);
                if (gbXML == null)
                {
                    return;
                }

                bool exported = Core.gbXML.Create.gbXML(gbXML, path_gbXML);
                if (!exported)
                {
                    return;
                }

                string path_tbd = Path.Combine(directory_AnalyticalModel, name + ".tbd");

                WorkflowSettings workflowSettings_AnalyticalModel = new(workflowSettings)
                {
                    Path_gbXML = path_gbXML,
                    Path_TBD = path_tbd
                };

                WorkflowCalculator workflowCalculator = new(workflowSettings_AnalyticalModel);

                tuples[i] = new(directory_AnalyticalModel, name, workflowCalculator.Calculate(analyticalModel));
            };

            Dictionary<string, AnalyticalModel> result = [];

            using (ProgressBarWindowManager progressBarWindowManager = new ("Run", "Running"))
            {
                if (parallel)
                {
                    maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount - 1;
                    if(maxDegreeOfParallelism.Value <= 0)
                    {
                        maxDegreeOfParallelism = 1;
                    }

                    Parallel.For(0, count, new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism.Value
                    },
                    action.Invoke);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        progressBarWindowManager.Text = $"Running (Model: {tuples[i]?.Item3?.Name ?? "???"})";
                        action.Invoke(i);
                    }
                }

                progressBarWindowManager.Text = "Converting to SAM";

                foreach (Tuple<string, string, AnalyticalModel> tuple in tuples)
                {
                    if (tuple is null)
                    {
                        continue;
                    }

                    result[tuple.Item1] = tuple.Item3;

                    progressBarWindowManager.Text = $"Converting to SAM ({tuple.Item2})";

                    if (saveAnalyticalModels)
                    {
                        string path_json = Path.Combine(tuple.Item1, tuple.Item2 + ".json");
                        Core.Convert.ToFile(tuple.Item3, path_json);
                    }
                }
            }

            return result;
        }
    }
}
