// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.UI.WPF;
using SAM.Core.Windows.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static AnalyticalModel? RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings)
        {
            return RunWorkflow(analyticalModel, workflowSettings, CancellationToken.None, out bool _);
        }

        /// <summary>
        /// Runs the TAS workflow with a progress dialog that carries a Cancel button. Cancellation is
        /// cooperative and between-step (see <see cref="WorkflowCalculator.CancellationToken"/>): it aborts
        /// before the next step but cannot interrupt the in-flight TAS COM simulate/sizing call.
        /// <para>
        /// The dialog runs on its own UI thread (<see cref="ProgressWindowHost"/>) rather than on the calling
        /// one. The workflow blocks its caller for minutes at a time, and Windows ghosts a window whose thread
        /// has stopped pumping and then discards clicks on the ghost — so a Cancel button on the calling
        /// thread silently loses the click and the run carries on to completion. The job itself stays on the
        /// calling thread, so no TAS COM object changes apartment.
        /// </para>
        /// On cancellation the method returns null and sets <paramref name="cancelled"/> true; on any other
        /// failure it returns null with <paramref name="cancelled"/> false.
        /// </summary>
        /// <param name="externalCancellationToken">
        /// Lets the caller share one cancel across its own COM pre-step and this one, so a single Cancel click
        /// aborts either.
        /// </param>
        /// <param name="analyticalModel_Owned">
        /// True only where the caller has already taken a deep working copy that nothing else holds, so the
        /// workflow may mutate it directly instead of taking a second one. Threaded straight to
        /// <c>WorkflowCalculator.Calculate(AnalyticalModel, bool)</c> - see there for what the promise means
        /// and what breaks if it is made falsely. False, the default, keeps every existing caller's
        /// behaviour: the workflow takes its own copy and a failed or cancelled run leaves the caller's
        /// model untouched.
        /// </param>
        public static AnalyticalModel? RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken externalCancellationToken, out bool cancelled, bool analyticalModel_Owned = false)
        {
            cancelled = false;

            if (analyticalModel == null)
            {
                return null;
            }

            if (workflowSettings == null)
            {
                workflowSettings = new WorkflowSettings();
            }

            AnalyticalModel? result = analyticalModel;

            //Inside a Part O operation that already shows its own progress window: report the steps there
            //and take its Cancel, rather than opening a second dialog over it. The workflow, its settings and
            //its cancellation rules are exactly those below.
            PartOProgressHost? partOProgressHost = PartOProgressHost.Current;
            if (partOProgressHost is not null)
            {
                using (CancellationTokenSource cancellationTokenSource_PartO = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, partOProgressHost.Token))
                {
                    //Cancel is offered while the workflow runs - it observes the token at entry, at every
                    //step and before returning - and withdrawn in the finally, BEFORE the final check below,
                    //so no click can land after the last look. Where the host always offers Cancel (Prepare &
                    //Run) this changes nothing.
                    IDisposable cancelScope = partOProgressHost.AllowCancel();

                    try
                    {
                        WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings)
                        {
                            CancellationToken = cancellationTokenSource_PartO.Token
                        };

                        workflowCalculator.Updating += (s, e) => partOProgressHost.Detail(e.Description);

                        result = workflowCalculator.Calculate(analyticalModel, analyticalModel_Owned);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        result = null;
                    }
                    finally
                    {
                        cancelScope.Dispose();

                        partOProgressHost.Detail(null);
                    }

                    //The same final observation as below: a Cancel that landed after the workflow's own last
                    //check still reports the run as cancelled.
                    if (!cancelled && cancellationTokenSource_PartO.IsCancellationRequested)
                    {
                        cancelled = true;
                        result = null;
                    }
                }

                return result;
            }

            using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken))
            {
                // Not a using: the dialog has to be torn down BEFORE the final cancellation check below, and a
                // using would dispose it after. The Cancel button lives on the dialog's own UI thread, so it
                // can be clicked at any instant - including after a check placed here. Checking then disposing
                // only moves that race; disposing then checking closes it, because Dispose closes the window
                // and joins its thread, so afterwards no further CancelRequested can arrive and any in-flight
                // one has already run.
                ProgressWindowHost progressWindowHost = new ProgressWindowHost("Tas Workflow", 1, true, Analytical.Tas.Query.CancelNote(null));

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();

                    WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings)
                    {
                        CancellationToken = cancellationTokenSource.Token
                    };

                    workflowCalculator.StepsCounted += (s, e) =>
                    {
                        progressWindowHost.Max = e.Count;
                    };

                    workflowCalculator.Updating += (s, e) =>
                    {
                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote(e.Description);
                        progressWindowHost.Update(e.Description);
                    };

                    result = workflowCalculator.Calculate(analyticalModel, analyticalModel_Owned);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    result = null;
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // Past this point no cancel can be raised, so this observation is final. It catches a click
                // that landed after WorkflowCalculator's own last check - without it the caller would go on to
                // treat a cancelled run as a successful one.
                //
                // "Final" holds only once the host confirms it shut down cleanly. If it could not, the dialog
                // thread is still live and a click it has queued may never have been observed, so success
                // cannot be claimed - the safe direction is to report the run as cancelled. The expensive
                // artifacts (.tbd/.tsd) are on disk either way; what is given up is the in-memory handoff,
                // which a rerun reproduces.
                if (!cancelled && (cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted))
                {
                    cancelled = true;
                    result = null;
                }
            }

            return result;
        }

        public static Dictionary<string, AnalyticalModel>? RunWorkflow(this IEnumerable<AnalyticalModel> analyticalModels, WorkflowSettings workflowSettings, string directory, bool parallel = true, int? maxDegreeOfParallelism = null, bool saveAnalyticalModels = false)
        {
            return RunWorkflow(analyticalModels, workflowSettings, directory, CancellationToken.None, out bool _, parallel, maxDegreeOfParallelism, saveAnalyticalModels);
        }

        /// <summary>
        /// Runs the TAS workflow over several models, with a cancellable progress dialog hosted off the
        /// calling thread for the same reason as the single-model overload above.
        /// <para>
        /// Cancellation here is coarser than in a single run: each model's workflow observes the shared token
        /// between its own steps, and <see cref="Parallel.For(int, int, ParallelOptions, Action{int})"/> stops
        /// handing out new models. Models already in flight run to the end of their current step first, so
        /// with a wide degree of parallelism the wait after clicking Cancel is as long as the slowest of them.
        /// </para>
        /// On cancellation the models that did finish are still returned, so a partial batch is not thrown
        /// away — <paramref name="cancelled"/> is what tells the caller the set is incomplete.
        /// </summary>
        public static Dictionary<string, AnalyticalModel>? RunWorkflow(this IEnumerable<AnalyticalModel> analyticalModels, WorkflowSettings workflowSettings, string directory, CancellationToken externalCancellationToken, out bool cancelled, bool parallel = true, int? maxDegreeOfParallelism = null, bool saveAnalyticalModels = false)
        {
            cancelled = false;

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

            Dictionary<string, AnalyticalModel> result = [];

            using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken))
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

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

                    WorkflowCalculator workflowCalculator = new(workflowSettings_AnalyticalModel)
                    {
                        CancellationToken = cancellationToken
                    };

                    tuples[i] = new(directory_AnalyticalModel, name, workflowCalculator.Calculate(analyticalModel));
                };

                // Not a using, and disposed in the finally below: see the single-model overload for why the
                // host has to be torn down before the token is observed for the last time.
                ProgressWindowHost progressWindowHost = new ProgressWindowHost("Run", count, true, Analytical.Tas.Query.CancelNote(null));

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();

                    if (parallel)
                    {
                        maxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount - 1;
                        if (maxDegreeOfParallelism.Value <= 0)
                        {
                            maxDegreeOfParallelism = 1;
                        }

                        Parallel.For(0, count, new ParallelOptions()
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism.Value,
                            CancellationToken = cancellationToken
                        },
                        i =>
                        {
                            action.Invoke(i);

                            // Reported after the model is done rather than before: with several running at
                            // once a "starting" message would name whichever model happened to be dispatched
                            // last, which is not what the bar is counting.
                            progressWindowHost.Update(string.Format("Running (Model: {0})", tuples[i]?.Item2 ?? "???"));
                        });
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            action.Invoke(i);

                            // The name is only known once the model has been processed - tuples[i] is null
                            // until action fills it in. Reading it before the call (as this used to) always
                            // rendered "???".
                            progressWindowHost.Update(string.Format("Running (Model: {0})", tuples[i]?.Item2 ?? "???"));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                catch (AggregateException aggregateException) when (aggregateException.InnerExceptions.All(x => x is OperationCanceledException))
                {
                    // Parallel.For surfaces a cancellation as OperationCanceledException only when the token
                    // is the one on its ParallelOptions; a calculator throwing on the same token a moment
                    // earlier comes back wrapped instead.
                    cancelled = true;
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // As in the single-model overload: a host that could not confirm a clean shutdown may be
                // sitting on an unobserved click, so the batch cannot be reported as complete.
                if (!cancelled && (cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted))
                {
                    cancelled = true;
                }
            }

            // Whatever finished is still worth returning; cancelled tells the caller the batch is partial.
            using (ProgressBarWindowManager progressBarWindowManager = new("Run", "Converting to SAM"))
            {
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
