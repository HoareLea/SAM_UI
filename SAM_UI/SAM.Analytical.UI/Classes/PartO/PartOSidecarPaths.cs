// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// How a Part O sidecar (the mixed-design state, the Iteration 3 pairing record) names a file of its own run,
    /// so the sidecar still finds it after the project folder is moved, copied or opened on another machine.
    /// <para>
    /// A file the sidecar names is written as <c>Locator_&lt;Name&gt;</c> - relative to the sidecar's own folder, by
    /// the same rule <see cref="SimulationResultProvenance.Locator"/> uses for the results file, so there is one rule.
    /// A locator is only written for a file inside the folder tree the sidecar travels with (its own folder, or the
    /// Part O root for a record that sits in a case folder): a file beyond it - another drive, or another project's
    /// folder that a copied legacy sidecar still points at - is kept as <c>Path_&lt;Name&gt;</c>, absolute, exactly as
    /// before, so a relative path never silently ties a copy to the project it was copied from.
    /// </para>
    /// <para>
    /// Reading prefers the locator, resolved against where the sidecar IS now - never against where it was written -
    /// so a copy of a project reads its own files, not the original's. A sidecar an earlier build wrote has only
    /// <c>Path_&lt;Name&gt;</c>; that stays readable, and is rewritten as a locator the next time the state is saved.
    /// </para>
    /// </summary>
    internal static class PartOSidecarPaths
    {
        /// <summary>
        /// Writes <paramref name="path"/> into <paramref name="jsonObject"/> as <c>Locator_name</c> where it has a
        /// relative form from <paramref name="path_Sidecar"/>'s folder, otherwise as <c>Path_name</c> (absolute).
        /// With no <paramref name="path_Sidecar"/> it writes <c>Path_name</c> as given. A blank path writes a null
        /// <c>Path_name</c>, as the sidecar always has.
        /// </summary>
        internal static void Write(JsonObject jsonObject, string name, string path, string path_Sidecar)
        {
            Write(jsonObject, "Path_" + name, "Locator_" + name, path, path_Sidecar, null);
        }

        /// <summary>
        /// As <see cref="Write(JsonObject, string, string, string)"/>, for a field that already has its own keys, and
        /// where the sidecar's tree is <paramref name="directory_Root"/> (null: the sidecar's own folder).
        /// </summary>
        internal static void Write(JsonObject jsonObject, string key_Path, string key_Locator, string path, string path_Sidecar, string directory_Root)
        {
            string locator = string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(path_Sidecar) ? null : SimulationResultProvenance.Locator(path, path_Sidecar);
            if (locator is not null && !IsWithin(path, directory_Root ?? Path.GetDirectoryName(Path.GetFullPath(path_Sidecar))))
            {
                locator = null;
            }

            if (locator is not null)
            {
                jsonObject[key_Locator] = locator;
                return;
            }

            jsonObject[key_Path] = path;
        }

        private static bool IsWithin(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            string prefix = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return Path.GetFullPath(path).StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The file's full path: <c>Locator_name</c> resolved against <paramref name="path_Sidecar"/>'s folder where
        /// there is one and the sidecar's location is known, otherwise the legacy absolute <c>Path_name</c>
        /// (null where neither is there).
        /// </summary>
        internal static string Read(JsonObject jsonObject, string name, string path_Sidecar)
        {
            return Read(jsonObject, "Path_" + name, "Locator_" + name, path_Sidecar);
        }

        /// <summary>As <see cref="Read(JsonObject, string, string)"/>, for a field that already has its own keys.</summary>
        internal static string Read(JsonObject jsonObject, string key_Path, string key_Locator, string path_Sidecar)
        {
            if (jsonObject is null)
            {
                return null;
            }

            string locator = (string)jsonObject[key_Locator];
            if (!string.IsNullOrWhiteSpace(locator) && !string.IsNullOrWhiteSpace(path_Sidecar))
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(path_Sidecar));
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return Path.GetFullPath(Path.Combine(directory, locator));
                }
            }

            return (string)jsonObject[key_Path];
        }
    }
}
