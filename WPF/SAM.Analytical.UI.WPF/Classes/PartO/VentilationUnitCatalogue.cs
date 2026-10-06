// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The manufacturer ventilation-unit catalogue as this session sees it: the products selection may choose
    /// from, the ones it may not and why, and which of the three <see cref="VentilationUnitCatalogueState"/>
    /// the read landed in.
    /// <para>
    /// <b>Reading and reporting only.</b> The file is parsed by
    /// <c>SAM.Analytical.Systems.Query.VentilationUnitTemplates</c>; what counts as a usable capacity and why
    /// a template is unselectable are <c>SAM.Analytical.Query.CapacityDescriptors</c> and
    /// <c>SAM.Analytical.Query.UnselectableVentilationUnitTemplates</c>. Nothing here parses the schema, and
    /// nothing here chooses a unit - the same division the Grasshopper catalogue component keeps, for the same
    /// reason: this class cannot drift from what <c>VentilationUnitCatalogueTests</c> exercises because it
    /// decides nothing.
    /// </para>
    /// <para>
    /// <b>Selection is not performed here and must not be.</b>
    /// <see cref="CapacityDescriptors"/> is handed to <c>Modify.PreparePartOIteration</c>, which runs its own
    /// smallest-capable-unit rule per dwelling against the realized terminal network's duty. Calling
    /// <c>Query.SelectSmallestCapableVentilationUnit</c> from the UI would make a second selection authority
    /// out of a presentation layer.
    /// </para>
    /// <para>
    /// <b>The capacity a descriptor carries is the equipment's maximum, never a dwelling's design airflow.</b>
    /// The two products this repository ships state 150/150 l/s and 190/190 l/s supply and extract, each the
    /// highest free-air point of its own published fan data. A dwelling whose design duty is 30 l/s and which
    /// selects one still has a 30 l/s design duty; the rest is headroom.
    /// </para>
    /// </summary>
    public class VentilationUnitCatalogue
    {
        private VentilationUnitCatalogue(
            VentilationUnitCatalogueState state,
            List<VentilationUnitCapacityDescriptor> capacityDescriptors,
            List<KeyValuePair<VentilationUnitTemplate, string>> unselectableTemplates,
            List<VentilationUnitTemplate> templates,
            string directory,
            string path,
            string schema,
            string sha256)
        {
            State = state;
            CapacityDescriptors = capacityDescriptors ?? [];
            UnselectableTemplates = unselectableTemplates ?? [];
            Templates = templates ?? [];
            Directory = directory;
            Path = path;
            Schema = schema;
            Sha256 = sha256;
        }

        /// <summary>Which of the three outcomes the read landed in.</summary>
        public VentilationUnitCatalogueState State { get; }

        /// <summary>
        /// Every template this catalogue holds, selectable or not - PR5A (SAM#111 plan §K.1). A resolver
        /// that needs a selected product's certified data (<c>SAM.Analytical.Query.VentilationUnitOperatingParameters</c>)
        /// needs the whole template, not the capacity-only descriptor <see cref="CapacityDescriptors"/>
        /// carries; this is read-only, exactly as <see cref="CapacityDescriptors"/> is - nothing here
        /// chooses a unit, and nothing here is a second selection authority.
        /// </summary>
        public List<VentilationUnitTemplate> Templates { get; }

        /// <summary>The resolved directory actually read, never a display label or an unresolved setting.</summary>
        public string Directory { get; }

        /// <summary>The exact catalogue file read.</summary>
        public string Path { get; }

        /// <summary>The schema tag stated by that file. Null where the catalogue could not be parsed.</summary>
        public string Schema { get; }

        /// <summary>SHA-256 of the exact catalogue bytes, in uppercase hexadecimal. Null where unavailable.</summary>
        public string Sha256 { get; }

        /// <summary>
        /// The products selection may choose from. Empty unless <see cref="State"/> is
        /// <see cref="VentilationUnitCatalogueState.Selectable"/>.
        /// <para>
        /// This is what <c>Modify.PreparePartOIteration</c>'s <c>ventilationUnitCapacityDescriptors</c>
        /// parameter takes. Handing it an empty list is not the same as handing it none, so a caller that
        /// wants Iteration 1a behaviour passes null - see <see cref="HasSelectableProducts"/>.
        /// </para>
        /// </summary>
        public List<VentilationUnitCapacityDescriptor> CapacityDescriptors { get; }

        /// <summary>
        /// The products in the catalogue that selection cannot use, each with the reason. Present in both
        /// <see cref="VentilationUnitCatalogueState.NoneSelectable"/> and
        /// <see cref="VentilationUnitCatalogueState.Selectable"/> - a catalogue can hold both kinds at once.
        /// </summary>
        public List<KeyValuePair<VentilationUnitTemplate, string>> UnselectableTemplates { get; }

        /// <summary>Whether there is anything to offer selection.</summary>
        public bool HasSelectableProducts => State == VentilationUnitCatalogueState.Selectable && CapacityDescriptors.Count != 0;

        /// <summary>
        /// One sentence saying what was found, for the preparation window and its report. Written so the three
        /// states cannot be confused with each other or with an engineering answer about a dwelling.
        /// </summary>
        public string Description
        {
            get
            {
                switch (State)
                {
                    case VentilationUnitCatalogueState.Unavailable:
                        return "The manufacturer ventilation unit catalogue could not be read, so no product is available to select. This is not a statement that no product could serve these dwellings - it is the absence of the catalogue that would say. The iteration can still be prepared without equipment selection.";

                    case VentilationUnitCatalogueState.NoneSelectable:
                        return string.Format("The ventilation unit catalogue was read and holds {0} product(s), none of which is selectable yet - each one's maximum airflow is unresolved. The iteration can still be prepared without equipment selection.", UnselectableTemplates.Count);

                    default:
                        return UnselectableTemplates.Count == 0
                            ? string.Format("{0} selectable ventilation unit product(s) available.", CapacityDescriptors.Count)
                            : string.Format("{0} selectable ventilation unit product(s) available; {1} in the catalogue are not selectable yet.", CapacityDescriptors.Count, UnselectableTemplates.Count);
                }
            }
        }

        /// <summary>
        /// Reads the installed catalogue.
        /// </summary>
        /// <param name="directory">
        /// Where to read from. Null uses the installed SAM library location, which is the same default
        /// <c>SAM.Analytical.Systems.Query.VentilationUnitTemplates</c> resolves. Supplied so a test can point
        /// at a known folder instead of depending on what is installed on the machine.
        /// </param>
        public static VentilationUnitCatalogue Read(string directory = null)
        {
            string directory_Resolved = string.IsNullOrWhiteSpace(directory)
                ? Analytical.Systems.Query.DefaultVentilationUnitDirectory()
                : System.IO.Path.GetFullPath(directory);

            string path = string.IsNullOrWhiteSpace(directory_Resolved)
                ? null
                : System.IO.Path.Combine(directory_Resolved, Analytical.Systems.Query.VentilationUnitCatalogueFileName);

            string schema = null;
            string sha256 = null;

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    using (SHA256 algorithm = SHA256.Create())
                    {
                        sha256 = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
                    }
                    schema = (Analytical.Systems.Query.VentilationUnitCatalogue(directory_Resolved)?["Schema"] as JsonValue)?.GetValue<string>();
                }
                catch
                {
                    //The catalogue reader below remains the one validity authority. Metadata is evidence
                    //only, and an unreadable file will be reported as Unavailable by that reader.
                }
            }

            //Null is the reader's own "missing, unreadable or unusable" answer, including a schema it does not
            //accept. It is deliberately NOT collapsed with an empty template list below: one means nothing is
            //known, the other means nothing is offered.
            List<VentilationUnitTemplate> ventilationUnitTemplates = Analytical.Systems.Query.VentilationUnitTemplates(directory_Resolved);
            if (ventilationUnitTemplates is null)
            {
                return new VentilationUnitCatalogue(VentilationUnitCatalogueState.Unavailable, null, null, null, directory_Resolved, path, schema, sha256);
            }

            //Qualified: SAM.Analytical.UI.WPF declares a Query of its own.
            List<VentilationUnitCapacityDescriptor> capacityDescriptors = Analytical.Query.CapacityDescriptors(ventilationUnitTemplates);
            List<KeyValuePair<VentilationUnitTemplate, string>> unselectableTemplates = Analytical.Query.UnselectableVentilationUnitTemplates(ventilationUnitTemplates);

            //An empty catalogue file reads as NoneSelectable rather than Unavailable: it was read, and it
            //offers nothing. Both are "no selection this run", but only Unavailable is a fault to chase.
            return new VentilationUnitCatalogue(
                capacityDescriptors is null || capacityDescriptors.Count == 0 ? VentilationUnitCatalogueState.NoneSelectable : VentilationUnitCatalogueState.Selectable,
                capacityDescriptors,
                unselectableTemplates,
                ventilationUnitTemplates,
                directory_Resolved,
                path,
                schema,
                sha256);
        }
    }
}
