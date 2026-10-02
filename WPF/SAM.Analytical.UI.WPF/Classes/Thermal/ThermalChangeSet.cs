// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// One or more thermal changes that are applied together as ONE model change and therefore ONE Undo step: any number of
    /// opaque changes (<see cref="SetUValueRequest"/>, one per construction), opaque alternatives that assign an existing construction
    /// (<see cref="SetConstructionRequest"/>, one per construction) and glazing changes
    /// (<see cref="SetGlazingRequest"/>, one per aperture construction), each with its own explicit scope (the Guids of the
    /// elements it reaches, pinned in the request when the row was edited). It holds requests only: the U-value and glazing
    /// calculations stay in <c>Modify.SetUValue</c> / <c>Modify.SetGlazing</c>, which <c>Modify.ProposeThermalChange</c> composes.
    /// </summary>
    public sealed class ThermalChangeSet
    {
        private readonly List<SetUValueRequest> uValueRequests = new List<SetUValueRequest>();
        private readonly List<SetConstructionRequest> constructionRequests = new List<SetConstructionRequest>();
        private readonly List<SetGlazingRequest> glazingRequests = new List<SetGlazingRequest>();

        /// <summary>The opaque changes, applied first, in the order they were added.</summary>
        public IReadOnlyList<SetUValueRequest> UValueRequests => uValueRequests;

        /// <summary>The opaque changes that assign an existing construction, applied after the generated ones and before glazing.</summary>
        public IReadOnlyList<SetConstructionRequest> ConstructionRequests => constructionRequests;

        /// <summary>The glazing changes, applied after the opaque ones, in the order they were added.</summary>
        public IReadOnlyList<SetGlazingRequest> GlazingRequests => glazingRequests;

        /// <summary>
        /// Refresh the thermal parameters stored on the model (the whole-model Tas run) as part of the same change. Set on its own
        /// it is the "Recalculate" action: a change set that changes nothing but the stored values.
        /// </summary>
        public bool RecalculateStoredValues { get; set; }

        public int Count => uValueRequests.Count + constructionRequests.Count + glazingRequests.Count;

        /// <summary>True when applying it would change nothing.</summary>
        public bool IsEmpty => Count == 0 && !RecalculateStoredValues;

        public ThermalChangeSet Add(SetUValueRequest request)
        {
            if (request != null)
            {
                uValueRequests.Add(request);
            }

            return this;
        }

        public ThermalChangeSet Add(SetConstructionRequest request)
        {
            if (request != null)
            {
                constructionRequests.Add(request);
            }

            return this;
        }

        public ThermalChangeSet Add(SetGlazingRequest request)
        {
            if (request != null)
            {
                glazingRequests.Add(request);
            }

            return this;
        }

        /// <summary>
        /// Why the set cannot be applied as it is, or null: two changes to the same construction would overwrite each other, so the
        /// set refuses them rather than let the second silently win.
        /// </summary>
        internal string Conflict()
        {
            if (uValueRequests.GroupBy(x => x.ConstructionGuid).Any(x => x.Count() > 1))
            {
                return "Two opaque changes are for the same construction.";
            }

            if (constructionRequests.GroupBy(x => x.SourceConstructionGuid).Any(x => x.Count() > 1) || constructionRequests.Any(x => uValueRequests.Any(y => y.ConstructionGuid == x.SourceConstructionGuid)))
            {
                return "Two opaque changes are for the same construction.";
            }

            if (glazingRequests.GroupBy(x => x.SourceApertureConstructionGuid).Any(x => x.Count() > 1))
            {
                return "Two glazing changes are for the same aperture construction.";
            }

            return null;
        }
    }

    /// <summary>The outcome of building or applying a <see cref="ThermalChangeSet"/>.</summary>
    public sealed class ThermalChangeResult
    {
        internal ThermalChangeResult(string error)
        {
            Error = error;
            UValueResults = new List<SetUValueResult>();
            ConstructionResults = new List<SetConstructionResult>();
            GlazingResults = new List<SetGlazingResult>();
        }

        internal ThermalChangeResult(ThermalChangeSet changeSet, IReadOnlyList<SetUValueResult> uValueResults, IReadOnlyList<SetGlazingResult> glazingResults, IReadOnlyList<SetConstructionResult> constructionResults = null)
        {
            ChangeSet = changeSet;
            ConstructionResults = constructionResults ?? new List<SetConstructionResult>();
            UValueResults = uValueResults ?? new List<SetUValueResult>();
            GlazingResults = glazingResults ?? new List<SetGlazingResult>();
            AppliedAt = DateTime.Now;
        }

        public bool Succeeded => Error == null;

        public string Error { get; }

        /// <summary>The set this is the result of; its requests line up with <see cref="UValueResults"/> and <see cref="GlazingResults"/>.</summary>
        public ThermalChangeSet ChangeSet { get; }

        public IReadOnlyList<SetUValueResult> UValueResults { get; }

        /// <summary>The results of the existing-construction alternatives, lined up with <see cref="ThermalChangeSet.ConstructionRequests"/>.</summary>
        public IReadOnlyList<SetConstructionResult> ConstructionResults { get; }

        public IReadOnlyList<SetGlazingResult> GlazingResults { get; }

        public DateTime AppliedAt { get; }

        /// <summary>True when the stored thermal parameters were refreshed as part of this change.</summary>
        public bool Recalculated { get; internal set; }

        /// <summary>Where each per-Apply report was saved ("Report saved: ..."), or why it was not; empty when none was written.</summary>
        public IReadOnlyList<string> ReportLines { get; internal set; } = new List<string>();

        public int PanelCount => UValueResults.Sum(x => x.PanelCount) + ConstructionResults.Sum(x => x.PanelCount);

        public int ApertureCount => GlazingResults.Sum(x => x.ApertureCount);

        /// <summary>The one-line result shown after Apply, e.g. "12 panels now SIM_EXT_SLD U0.18 · 20 apertures now Double glazing. One Undo reverts it."</summary>
        public string Text
        {
            get
            {
                if (!Succeeded)
                {
                    return Error;
                }

                List<string> parts = new List<string>();
                foreach (SetUValueResult result in UValueResults)
                {
                    parts.Add(result.Scope == ThermalApplyScope.DontAssign
                        ? string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} created, not assigned", result.Construction.Name)
                        : string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1} now {2}", result.PanelCount, result.PanelCount == 1 ? "panel" : "panels", result.Construction.Name));
                }

                foreach (SetConstructionResult result in ConstructionResults)
                {
                    parts.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1} now {2}", result.PanelCount, result.PanelCount == 1 ? "panel" : "panels", result.Construction.Name));
                }

                foreach (SetGlazingResult result in GlazingResults)
                {
                    parts.Add(result.Scope == ThermalApplyScope.DontAssign
                        ? string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} added, not assigned", result.ApertureConstruction.Name)
                        : string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1} now {2}", result.ApertureCount, result.ApertureCount == 1 ? "aperture" : "apertures", result.ApertureConstruction.Name));
                }

                if (parts.Count == 0 && Recalculated)
                {
                    parts.Add("Stored thermal values recalculated");
                }

                return string.Join(" · ", parts) + ". One Undo reverts it.";
            }
        }
    }
}
