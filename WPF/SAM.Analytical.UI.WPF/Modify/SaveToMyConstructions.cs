// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Saves a construction of the classic Constructions editor to "My constructions" as a NEW construction (new Guid, its materials embedded, the
        /// provenance "Constructions editor" attached): asks for a name (<paramref name="promptName"/>: given what is saved and the library's naming
        /// rule, the name or null when cancelled), saves, and says what happened (<paramref name="report"/>). It never touches a model or the editor's own
        /// library, so it adds no Undo step; the library's <c>Changed</c> event refreshes any open alternatives list. Returns null when nothing was saved
        /// (cancelled, or it cannot be saved - then <paramref name="report"/> says why).
        /// </summary>
        internal static UserConstructionSaveResult SaveToMyConstructions(Construction construction, MaterialLibrary materialLibrary, string modelName, UserConstructionLibrary library, Func<UserConstructionSaveSubject, Func<string, string>, string> promptName, Action<string> report)
        {
            if (library == null)
            {
                report?.Invoke("There is no My constructions library to save to.");
                return null;
            }

            List<string> names = new List<string>();
            try
            {
                names = library.Read().Constructions.Select(x => x.Name).ToList();
            }
            catch (Exception)
            {
            }

            UserConstructionSaveSubject subject = UserConstructionSaveSubject.FromConstructionEditor(construction, materialLibrary, modelName, names);
            if (subject.Rejection != null)
            {
                report?.Invoke(subject.Rejection);
                return null;
            }

            string name = promptName?.Invoke(subject, text => UserConstructionLibrary.NameProblem(text, names));
            if (name == null)
            {
                return null;
            }

            UserConstructionSaveResult result = subject.Save(library, name);
            report?.Invoke(result.Succeeded ? string.Format("Saved '{0}' to My constructions.", result.Saved.Name) : result.Error);
            return result;
        }
    }
}
