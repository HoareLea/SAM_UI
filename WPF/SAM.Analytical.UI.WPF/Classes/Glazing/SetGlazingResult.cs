// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What <c>Modify.SetGlazing</c> did, for the window's result line and the GLAZING CHANGE report.</summary>
    public sealed class SetGlazingResult
    {
        internal SetGlazingResult(string error)
        {
            Error = error;
            ApertureGuids = new List<Guid>();
            PanelGuids = new List<Guid>();
            MaterialNamesAdded = new List<string>();
        }

        internal SetGlazingResult(SetGlazingRequest request, ApertureConstruction sourceApertureConstruction, ApertureConstruction apertureConstruction, bool apertureConstructionAdded, IReadOnlyList<string> materialNamesAdded, IReadOnlyList<Guid> apertureGuids, IReadOnlyList<Guid> panelGuids)
        {
            Scope = request.Scope;
            SourceApertureConstruction = sourceApertureConstruction;
            ApertureConstruction = apertureConstruction;
            ApertureConstructionAdded = apertureConstructionAdded;
            MaterialNamesAdded = materialNamesAdded ?? new List<string>();
            ApertureGuids = apertureGuids ?? new List<Guid>();
            PanelGuids = panelGuids ?? new List<Guid>();
            Values = request.Values;
            OldValues = request.OldValues;
            OldUw = request.OldUw;
            NewUw = request.NewUw;
            UwBasis = request.UwBasis;
            TargetUw = request.TargetUw;
            AppliedAt = DateTime.Now;
        }

        /// <summary>True when the change was applied (one Undo step).</summary>
        public bool Succeeded => Error == null;

        /// <summary>Why nothing was applied; null on success.</summary>
        public string Error { get; }

        public ThermalApplyScope Scope { get; }

        /// <summary>The aperture construction the apertures used before.</summary>
        public ApertureConstruction SourceApertureConstruction { get; }

        /// <summary>The aperture construction now in the model (under its model name; a clashing name gets a suffix).</summary>
        public ApertureConstruction ApertureConstruction { get; }

        /// <summary>True when the chosen system was added to the model by this change (it was not in the model).</summary>
        public bool ApertureConstructionAdded { get; }

        /// <summary>The materials added to the model's Material Library by this change (only the chosen system's).</summary>
        public IReadOnlyList<string> MaterialNamesAdded { get; }

        /// <summary>The apertures assigned the system (none for "don't assign").</summary>
        public IReadOnlyList<Guid> ApertureGuids { get; }

        /// <summary>The panels carrying those apertures.</summary>
        public IReadOnlyList<Guid> PanelGuids { get; }

        public int ApertureCount => ApertureGuids.Count;

        public GlazingValues Values { get; }

        public GlazingValues OldValues { get; }

        public double OldUw { get; }

        public double NewUw { get; }

        public GlazingUwBasis UwBasis { get; }

        public double TargetUw { get; }

        /// <summary>When the change was applied (the report says it may since have been undone).</summary>
        public DateTime AppliedAt { get; }
    }
}
