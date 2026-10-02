// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.UI;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Applies a <see cref="ThermalChangeSet"/> as ONE Undo step: the opaque and glazing changes are built on one chain of
        /// model clones by the existing <see cref="SetUValue(AnalyticalModel, SetUValueRequest, out SetUValueResult)"/>,
        /// <see cref="SetConstruction(AnalyticalModel, SetConstructionRequest, out SetConstructionResult)"/> and
        /// <see cref="SetGlazing(AnalyticalModel, SetGlazingRequest, ThermalTransmittanceCalculationResult, out SetGlazingResult)"/>
        /// cores (no calculation is repeated here), the whole-model thermal-parameter refresh runs ONCE, and <c>SetJSAMObject</c> is
        /// called exactly once - never on failure, so a failed or empty set leaves the model and its history untouched.
        /// </summary>
        public static ThermalChangeResult ApplyThermalChange(this UIAnalyticalModel uIAnalyticalModel, ThermalChangeSet changeSet)
        {
            return ApplyThermalChange(uIAnalyticalModel, changeSet, x => Tas.Modify.UpdateThermalParameters(x), CalculateGlazingParameters);
        }

        /// <summary>
        /// <see cref="ApplyThermalChange(UIAnalyticalModel, ThermalChangeSet)"/>, then the existing per-Apply reports (reports stay
        /// per Apply for now): for each opaque change the U-VALUE CHANGE report and for each glazing change the glazing report,
        /// with the scoped check of the changed model, saved next to the model. The reports read the model and write only files.
        /// </summary>
        public static ThermalChangeResult ApplyThermalChangeWithReports(this UIAnalyticalModel uIAnalyticalModel, ThermalChangeSet changeSet)
        {
            ThermalChangeResult result = ApplyThermalChange(uIAnalyticalModel, changeSet);
            if (!result.Succeeded)
            {
                return result;
            }

            AnalyticalModel analyticalModel = uIAnalyticalModel.JSAMObject;
            List<string> lines = new List<string>();

            foreach (SetUValueResult uValueResult in result.UValueResults)
            {
                string text = Query.UValueChangeReportText(uValueResult, Query.UValueCheckSummary(analyticalModel, uValueResult), uIAnalyticalModel.Path);
                lines.Add(SaveUValueChangeReport(uIAnalyticalModel.Path, uValueResult.AppliedAt, text, out string path, out string refusal) ? "Report saved: " + path : refusal);
            }

            foreach (SetGlazingResult glazingResult in result.GlazingResults)
            {
                string text = Query.GlazingChangeReportText(glazingResult, Query.GlazingCheckSummary(analyticalModel, glazingResult), uIAnalyticalModel.Path);
                lines.Add(SaveGlazingChangeReport(uIAnalyticalModel.Path, glazingResult.AppliedAt, text, out string path, out string refusal) ? "Report saved: " + path : refusal);
            }

            result.ReportLines = lines;
            return result;
        }

        /// <param name="updateThermalParameters">The Tas thermal-parameter refresh (a whole-model TCD run). Tests pass a stand-in so they need no Tas.</param>
        /// <param name="calculate">The Tas calculation of a chosen glazing system's aperture parameters. Tests pass a stand-in.</param>
        internal static ThermalChangeResult ApplyThermalChange(this UIAnalyticalModel uIAnalyticalModel, ThermalChangeSet changeSet, Action<AnalyticalModel> updateThermalParameters, Func<SetGlazingRequest, ThermalTransmittanceCalculationResult> calculate)
        {
            AnalyticalModel analyticalModel = uIAnalyticalModel?.JSAMObject;
            if (analyticalModel == null)
            {
                return new ThermalChangeResult("There is no model to change.");
            }

            if (changeSet == null || changeSet.IsEmpty)
            {
                return new ThermalChangeResult("There is nothing to apply.");
            }

            ThermalChangeResult result;
            AnalyticalModel analyticalModel_New = analyticalModel;
            if (changeSet.Count != 0)
            {
                analyticalModel_New = ProposeThermalChange(analyticalModel, changeSet, calculate, out result);
                if (analyticalModel_New == null || result == null || !result.Succeeded)
                {
                    return result ?? new ThermalChangeResult("The thermal change could not be applied.");
                }
            }
            else
            {
                result = new ThermalChangeResult(changeSet, new List<SetUValueResult>(), new List<SetGlazingResult>(), new List<SetConstructionResult>());
            }

            // Once, whatever the number of changes. The opaque cores leave it to the caller (as Modify.SetUValue does); the glazing core
            // does not need it (the whole-model run covers panel constructions only), so a glazing-only change does not pay for it.
            if (changeSet.UValueRequests.Count != 0 || changeSet.ConstructionRequests.Count != 0 || changeSet.RecalculateStoredValues)
            {
                updateThermalParameters?.Invoke(analyticalModel_New);
                result.Recalculated = true;
            }

            uIAnalyticalModel.SetJSAMObject(analyticalModel_New, new FullModification());

            return result;
        }

        /// <summary>
        /// The model-only part: the model with every change of <paramref name="changeSet"/> made, or null on failure (with
        /// <paramref name="result"/> saying why). <paramref name="analyticalModel"/> is not modified; no Tas call is made unless
        /// <paramref name="calculate"/> makes one (null: a glazing change takes the Ug / g / light values of its request), and the
        /// whole-model thermal-parameter refresh is not run - which makes it the cheap build of the PROPOSED model that the check
        /// before Apply reads.
        /// </summary>
        internal static AnalyticalModel ProposeThermalChange(this AnalyticalModel analyticalModel, ThermalChangeSet changeSet, Func<SetGlazingRequest, ThermalTransmittanceCalculationResult> calculate, out ThermalChangeResult result)
        {
            result = null;

            if (analyticalModel == null)
            {
                result = new ThermalChangeResult("There is no model to change.");
                return null;
            }

            if (changeSet == null || changeSet.Count == 0)
            {
                result = new ThermalChangeResult("There is nothing to apply.");
                return null;
            }

            string conflict = changeSet.Conflict();
            if (conflict != null)
            {
                result = new ThermalChangeResult(conflict);
                return null;
            }

            AnalyticalModel analyticalModel_New = analyticalModel;

            List<SetUValueResult> uValueResults = new List<SetUValueResult>();
            foreach (SetUValueRequest request in changeSet.UValueRequests)
            {
                analyticalModel_New = SetUValue(analyticalModel_New, request, out SetUValueResult uValueResult);
                if (analyticalModel_New == null || uValueResult == null || !uValueResult.Succeeded)
                {
                    result = new ThermalChangeResult(uValueResult?.Error ?? "The U-value change could not be applied.");
                    return null;
                }

                uValueResults.Add(uValueResult);
            }

            List<SetConstructionResult> constructionResults = new List<SetConstructionResult>();
            foreach (SetConstructionRequest request in changeSet.ConstructionRequests)
            {
                analyticalModel_New = SetConstruction(analyticalModel_New, request, out SetConstructionResult constructionResult);
                if (analyticalModel_New == null || constructionResult == null || !constructionResult.Succeeded)
                {
                    result = new ThermalChangeResult(constructionResult?.Error ?? "The construction change could not be applied.");
                    return null;
                }

                constructionResults.Add(constructionResult);
            }

            List<SetGlazingResult> glazingResults = new List<SetGlazingResult>();
            foreach (SetGlazingRequest request in changeSet.GlazingRequests)
            {
                ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult = null;
                if (request?.ApertureConstruction != null && request.Scope != ThermalApplyScope.DontAssign)
                {
                    thermalTransmittanceCalculationResult = calculate?.Invoke(request);
                }

                analyticalModel_New = SetGlazing(analyticalModel_New, request, thermalTransmittanceCalculationResult, out SetGlazingResult glazingResult);
                if (analyticalModel_New == null || glazingResult == null || !glazingResult.Succeeded)
                {
                    result = new ThermalChangeResult(glazingResult?.Error ?? "The glazing change could not be applied.");
                    return null;
                }

                glazingResults.Add(glazingResult);
            }

            result = new ThermalChangeResult(changeSet, uValueResults, glazingResults, constructionResults);
            return analyticalModel_New;
        }
    }
}
