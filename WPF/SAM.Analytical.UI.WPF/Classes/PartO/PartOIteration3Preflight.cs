// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// What one Iteration 3 method would do to the reference case's ventilation units, answered BEFORE any
    /// TAS work - see <see cref="Query.PartOIteration3Preflight"/>.
    /// </summary>
    public class PartOIteration3Preflight
    {
        internal PartOIteration3Preflight(PartOIteration3BehaviourMode partOIteration3BehaviourMode, IEnumerable<PartOIteration3PreflightUnit> units, IEnumerable<string> refusals)
        {
            BehaviourMode = partOIteration3BehaviourMode;

            if (units is not null)
            {
                Units.AddRange(units);
            }

            if (refusals is not null)
            {
                Refusals.AddRange(refusals.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        public PartOIteration3BehaviourMode BehaviourMode { get; }

        /// <summary>Every ventilation unit the system case changes, by name.</summary>
        public List<PartOIteration3PreflightUnit> Units { get; } = [];

        /// <summary>Why the method cannot run, in the resolution's own words. Empty where it can.</summary>
        public List<string> Refusals { get; } = [];

        /// <summary>Whether nothing found so far stops the method running.</summary>
        public bool CanRun => Refusals.Count == 0;

        /// <summary>
        /// The units grouped by what they will run as - "3 × Nuaire X + cooling module Y" - so a block of a
        /// thousand flats on one product is one line rather than a thousand.
        /// </summary>
        public List<string> Summary()
        {
            List<string> result = [];

            foreach (IGrouping<string, PartOIteration3PreflightUnit> grouping in Units.GroupBy(x => x.Product).OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(string.Format("{0} × {1}", grouping.Count(), grouping.Key));
            }

            return result;
        }
    }

    /// <summary>One ventilation unit in a pre-flight: what it will run as, or why it cannot.</summary>
    public class PartOIteration3PreflightUnit
    {
        internal PartOIteration3PreflightUnit(string name, string product, string refusal)
        {
            Name = name;
            Product = product;
            Refusal = refusal;
        }

        /// <summary>The air handling unit's name.</summary>
        public string Name { get; }

        /// <summary>What the unit will run as, in words - the product, or "design airflows, no product".</summary>
        public string Product { get; }

        /// <summary>Why this unit stops the method, or null where it is ready.</summary>
        public string Refusal { get; }

        public bool IsReady => Refusal is null;

        public string StatusText => IsReady ? "Ready" : "Cannot run";

        public override string ToString()
        {
            return string.Format("{0}: {1}{2}", Name, Product, IsReady ? string.Empty : " - " + Refusal);
        }
    }
}
