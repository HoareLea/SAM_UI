// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What <c>Modify.SetGlazing</c> applies: one chosen glazing system (a complete aperture construction, with the
    /// materials the model lacks) assigned to the apertures that use the current aperture construction. Built by
    /// <see cref="GlazingViewModel.CreateRequest"/>.
    /// </summary>
    public sealed class SetGlazingRequest
    {
        /// <summary>The aperture construction the apertures use now, by identity (never by name).</summary>
        public Guid SourceApertureConstructionGuid { get; set; }

        /// <summary>The chosen system. Identity is its Guid; it is added to the model unless the model has it already.</summary>
        public ApertureConstruction ApertureConstruction { get; set; }

        /// <summary>The materials of the chosen system that the model does not have; only these are added.</summary>
        public IEnumerable<IMaterial> MaterialsToAdd { get; set; }

        public ThermalApplyScope Scope { get; set; } = ThermalApplyScope.AllUsing;

        /// <summary>The selected apertures; used by <see cref="ThermalApplyScope.SelectedOnly"/>.</summary>
        public IEnumerable<Guid> SelectedApertureGuids { get; set; }

        /// <summary>Values of the chosen system as shown in the comparison (set on the apertures, and for the report).</summary>
        public GlazingValues Values { get; set; }

        /// <summary>Overall U-value before / after, as shown (for the report); NaN if not available.</summary>
        public double OldUw { get; set; } = double.NaN;

        public double NewUw { get; set; } = double.NaN;

        /// <summary>The pane / frame values of the system the apertures use now (for the report).</summary>
        public GlazingValues OldValues { get; set; }

        public GlazingUwBasis UwBasis { get; set; }

        /// <summary>The Uw maximum the user asked for, if any (for the report).</summary>
        public double TargetUw { get; set; } = double.NaN;

        /// <summary>The source the chosen system was calculated in (its materials); Tas calculates the aperture parameters from it on apply.</summary>
        public GlazingSource Source { get; set; }
    }
}
