// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Which of a prepared model's authored ventilation systems the Iteration 3 materialisation is given -
    /// SAM #114's production answer, decided by identity and by nothing else.
    /// <para>
    /// Since PR-1 the decision is SAM's <c>PartOSystemsMaterialisationScope</c> (shared with Mixed Design); this
    /// type carries it in Iteration 3's own wording, which its records persist.
    /// </para>
    ///
    /// <para><b>Fail closed, with no partial-scope cluster</b></para>
    /// <para>
    /// <see cref="AdjacencyCluster"/> is null whenever <see cref="Refusals"/> is non-empty, enforced in
    /// the constructor. A caller that reads the working copy without reading the refusals cannot
    /// materialise a design that leaves a real mechanical duty out.
    /// </para>
    ///
    /// <para><b>The working copy is a copy, and the design is untouched</b></para>
    /// <para>
    /// The systems that are out of scope are removed from a copy of the cluster, never from the model.
    /// The model is the design, and PR4 is an orchestration: rewriting the design to fit a stage is
    /// exactly what <c>Modify.PreparePartOIteration</c> is built not to do, and what it keeps the authored
    /// natural, uncontrolled and legacy systems for.
    /// </para>
    /// <para>
    /// <b>Only the PR1 input is scoped.</b> Candidate B's thermal source is built from the prepared model
    /// itself, so no natural ventilation, uncontrolled ventilation, opening or infiltration is ever
    /// removed from anything that is simulated. What this copy exists for is a single question -
    /// "which systems is SAM_Systems asked to materialise" - and that question is not a thermal input.
    /// </para>
    /// </summary>
    public class PartOIteration3SystemScope
    {
        private readonly List<Guid> guids_Retained = [];

        private readonly List<Guid> guids_Removed = [];

        private readonly List<string> notes = [];

        private readonly List<string> refusals = [];

        internal PartOIteration3SystemScope(
            AdjacencyCluster adjacencyCluster,
            IEnumerable<Guid> guids_Retained,
            IEnumerable<Guid> guids_Removed,
            IEnumerable<string> notes,
            IEnumerable<string> refusals)
        {
            foreach (Guid guid in guids_Retained ?? [])
            {
                this.guids_Retained.Add(guid);
            }

            foreach (Guid guid in guids_Removed ?? [])
            {
                this.guids_Removed.Add(guid);
            }

            foreach (string note in notes ?? [])
            {
                if (!string.IsNullOrWhiteSpace(note))
                {
                    this.notes.Add(note);
                }
            }

            foreach (string refusal in refusals ?? [])
            {
                if (!string.IsNullOrWhiteSpace(refusal))
                {
                    this.refusals.Add(refusal);
                }
            }

            //Fail closed, structurally - see the class summary.
            AdjacencyCluster = this.refusals.Count == 0 ? adjacencyCluster : null;

            if (this.refusals.Count != 0)
            {
                this.guids_Retained.Clear();
                this.guids_Removed.Clear();
                this.notes.Clear();
            }
        }

        /// <summary>
        /// The working copy handed to <c>SAM.Analytical.Systems.Create.MechanicalVentilation</c>, or
        /// <b>null</b> whenever <see cref="Refusals"/> is non-empty.
        /// </summary>
        public AdjacencyCluster AdjacencyCluster { get; }

        /// <summary>The systems kept - exactly the ones the Part O preparation built, by identity.</summary>
        public List<Guid> Guids_Retained => [.. guids_Retained];

        /// <summary>The authored systems removed from the working copy, by identity.</summary>
        public List<Guid> Guids_Removed => [.. guids_Removed];

        /// <summary>Why each removed system carried no mechanical duty Candidate B has to recreate.</summary>
        public List<string> Notes => [.. notes];

        /// <summary>Why no scope could be taken. Ordered, and never empty on a refusal.</summary>
        public List<string> Refusals => [.. refusals];

        /// <summary>Whether a working copy was produced at all.</summary>
        public bool IsScoped => AdjacencyCluster is not null && refusals.Count == 0;

        public override string ToString()
        {
            return IsScoped
                ? string.Format("System scope: {0} retained, {1} removed.", guids_Retained.Count, guids_Removed.Count)
                : string.Format("System scope REFUSED ({0} reason(s)).", refusals.Count);
        }
    }
}
