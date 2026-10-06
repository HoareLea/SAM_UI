// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// ONE opaque construction ready to be saved to "My constructions": the construction, the materials its layers name, the name suggested for
    /// it, a sentence saying what it is (shown when the name is asked for) and the provenance the library will attach. It is built from whatever a
    /// row (or the classic Constructions editor) offers - the generated variant, a chosen alternative or the current construction - and holds only
    /// copies: it never refers to a model, and saving it (<see cref="Save"/>) can neither change a model nor add an Undo step.
    /// </summary>
    public sealed class UserConstructionSaveSubject
    {
        internal UserConstructionSaveSubject(string error)
        {
            Error = error;
        }

        internal UserConstructionSaveSubject(Construction construction, MaterialLibrary materials, string description, string suggestedName, UserConstructionProvenance provenance)
        {
            Construction = construction;
            Materials = materials;
            Description = description;
            SuggestedName = suggestedName;
            Provenance = provenance ?? new UserConstructionProvenance();
        }

        /// <summary>Why there is nothing to save (the generated variant has no U-value yet, the construction is gone); null when there is.</summary>
        public string Error { get; }

        public bool IsAvailable => Error == null && Construction != null;

        public Construction Construction { get; }

        /// <summary>The materials the construction's layers name (the source's own, plus the adjusted material of a generated variant).</summary>
        public MaterialLibrary Materials { get; }

        /// <summary>What is saved, e.g. "the generated variant SIM_EXT_SLD U0.18 (U 0.180 W/m²K)".</summary>
        public string Description { get; }

        /// <summary>The name the prompt starts with: the construction's own name, made unique in the library.</summary>
        public string SuggestedName { get; }

        public UserConstructionProvenance Provenance { get; }

        /// <summary>Why this cannot be saved as it is (a material missing, transparent or gas); null when it can.</summary>
        public string Rejection => IsAvailable ? UserConstructionLibrary.Rejection(Construction, Materials) : Error;

        /// <summary>Saves it to <paramref name="library"/> under <paramref name="name"/> (a new Guid, the materials embedded, the provenance attached).</summary>
        public UserConstructionSaveResult Save(UserConstructionLibrary library, string name)
        {
            if (library == null)
            {
                return new UserConstructionSaveResult(null, null, null, "There is no My constructions library to save to.");
            }

            if (!IsAvailable)
            {
                return new UserConstructionSaveResult(null, null, null, Error ?? "There is nothing to save.");
            }

            return library.Save(Construction, Materials, name, Provenance);
        }

        /// <summary>The name made unique among <paramref name="existing"/> names (ignoring case and spaces at the ends): the name itself, else "name 2", "name 3"…</summary>
        public static string UniqueName(string name, IEnumerable<string> existing)
        {
            string name_Base = string.IsNullOrWhiteSpace(name) ? "Construction" : name.Trim();
            HashSet<string> names = new HashSet<string>((existing ?? Enumerable.Empty<string>()).Where(x => x != null).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            string result = name_Base;
            for (int index = 2; names.Contains(result); index++)
            {
                result = string.Format(CultureInfo.CurrentCulture, "{0} {1}", name_Base, index);
            }

            return result;
        }

        /// <summary>The construction of the classic Constructions editor that is selected (an authored or imported one): saved as it is in the editor, which edits a copy.</summary>
        public static UserConstructionSaveSubject FromConstructionEditor(Construction construction, MaterialLibrary materials, string modelName, IEnumerable<string> existingNames = null)
        {
            if (construction == null)
            {
                return new UserConstructionSaveSubject("Select one construction to save.");
            }

            UserConstructionProvenance provenance = new UserConstructionProvenance()
            {
                SavedFrom = UserConstructionOrigin.ConstructionEditor,
                BasedOnName = construction.Name,
                BasedOnGuid = construction.Guid,
                OriginModelName = modelName,
                Route = "Saved from the Constructions editor; no U-value was calculated",
            };

            return new UserConstructionSaveSubject(construction, materials, string.Format(CultureInfo.CurrentCulture, "the construction {0}", construction.Name), UniqueName(construction.Name, existingNames), provenance);
        }

        /// <summary>The heat-flow basis in words, e.g. "Horizontal heat flow, external surfaces (WallExternal, from the panels)".</summary>
        internal static string BasisText(UValueHeatFlowBasis basis, HeatFlowDirection direction, bool external, bool chosenByHand)
        {
            if (direction == HeatFlowDirection.Undefined)
            {
                return null;
            }

            string source = chosenByHand
                ? "chosen by hand"
                : basis == null
                    ? null
                    : string.Format(CultureInfo.CurrentCulture, "{0}, {1}", basis.PanelType, basis.FromPanels ? "from the panels" : "from the construction's Default Panel Type");

            return string.Format(CultureInfo.CurrentCulture, "{0} heat flow, {1} surfaces{2}", direction, external ? "external" : "internal", source == null ? string.Empty : " (" + source + ")");
        }
    }
}
