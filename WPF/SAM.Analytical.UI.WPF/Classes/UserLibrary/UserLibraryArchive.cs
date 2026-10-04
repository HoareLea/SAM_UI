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
    /// Where a user library's REMOVED entries go. Remove never deletes: it moves the entry to <c>&lt;name&gt;.removed.json</c> next to the library
    /// (<c>Glazing Systems.removed.json</c>), a plain <see cref="ConstructionManager"/> in the same format, so it can be opened like any source
    /// and an entry can be recovered by hand. The archive is a LOG, not a second library:
    /// <list type="bullet">
    /// <item><description><b>It is only ever written while the library's own lock is held</b> (the caller's <see cref="UserLibraryFile.Transact"/>),
    /// so it needs no lock of its own and two removals never interleave.</description></item>
    /// <item><description><b>Archive first, then the library.</b> The contract this order gives is: <i>Remove never loses an entry</i> - either it
    /// fully succeeded (the entry is in the archive and gone from the library) or the entry is still in the library. If the archive write fails
    /// nothing has changed. If the library write fails after the archive succeeded the entry is in BOTH files, which is harmless: a Guid still
    /// in the library counts as not removed, and a retry is idempotent (the archive replaces the entry by Guid).</description></item>
    /// <item><description>An archive that exists but cannot be read is never overwritten - Remove stops, nothing is written.</description></item>
    /// <item><description>The archived entry carries EVERY material it references (not only the ones the library no longer needs), so the archive
    /// can be opened on its own. A different material of the same name already in the archive (a different removed entry's) is kept under a
    /// new name and the archived entry's layers follow it, exactly as when saving.</description></item>
    /// </list>
    /// </summary>
    internal static class UserLibraryArchive
    {
        internal const string Suffix = ".removed";

        /// <summary><c>Glazing Systems.json</c> → <c>Glazing Systems.removed.json</c>.</summary>
        internal static string PathFor(string libraryPath)
        {
            string extension = System.IO.Path.GetExtension(libraryPath);
            string name = System.IO.Path.GetFileNameWithoutExtension(libraryPath) + Suffix + (string.IsNullOrEmpty(extension) ? ".json" : extension);
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(libraryPath) ?? string.Empty, name);
        }

        /// <summary>
        /// Writes <paramref name="entry"/> and the materials it references (taken from <paramref name="sourceMaterials"/>, the library it is removed
        /// from) into the archive, replacing an archived entry of the same Guid. Returns null when the archive was written, otherwise why not (the
        /// archive is then as it was). Call it under the library's lock, BEFORE the library itself is written.
        /// </summary>
        /// <param name="nothingDone">What did not happen when this fails ("Nothing was removed", "Nothing was saved or replaced").</param>
        /// <param name="onMaterialsRenamed">
        /// Called with the archived entry and the renames (library material name → archive material name) when a material had to be kept under a
        /// new name; returns the entry to archive (e.g. with its provenance labels following). Null: only the layers follow.
        /// </param>
        internal static string Archive(UserLibraryFile archiveFile, ApertureConstruction entry, MaterialLibrary sourceMaterials, string libraryName, string description, Func<ApertureConstruction, IReadOnlyDictionary<string, string>, ApertureConstruction> onMaterialsRenamed = null, string nothingDone = "Nothing was removed")
        {
            if (archiveFile == null || entry == null)
            {
                return "There is nothing to archive.";
            }

            UserLibraryFileContent content = archiveFile.Read();
            if (content.State == UserLibraryFileState.Unreadable)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}, because the archive of removed entries cannot be used: {1}", nothingDone, content.Error);
            }

            ConstructionManager archive = content.ConstructionManager;
            MaterialLibrary materialLibrary = archive.MaterialLibrary ?? new MaterialLibrary(libraryName);

            Dictionary<string, string> names = new Dictionary<string, string>();
            foreach (string name in LibraryMaterialMerge.ReferencedNames(entry).Distinct())
            {
                IMaterial material = sourceMaterials?.GetMaterial(name);
                if (material == null)
                {
                    continue;
                }

                string archived = LibraryMaterialMerge.Add(materialLibrary, material);
                if (archived == null)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0}, because the material '{1}' could not be added to the archive.", nothingDone, name);
                }

                names[name] = archived;
            }

            ApertureConstruction result = entry;
            Dictionary<string, string> renamed = names.Where(x => x.Key != x.Value).ToDictionary(x => x.Key, x => x.Value);
            if (renamed.Count != 0)
            {
                result = new ApertureConstruction(entry, LibraryMaterialMerge.RenameLayers(entry.PaneConstructionLayers, renamed), LibraryMaterialMerge.RenameLayers(entry.FrameConstructionLayers, renamed));
                result = onMaterialsRenamed?.Invoke(result, renamed) ?? result;
            }

            List<ApertureConstruction> entries = (archive.ApertureConstructions ?? new List<ApertureConstruction>()).Where(x => x != null && x.Guid != entry.Guid).ToList();
            entries.Add(result);

            ConstructionManager updated = new ConstructionManager(entries, archive.Constructions, materialLibrary)
            {
                Name = archive.Name ?? libraryName + " (removed)",
                Description = archive.Description ?? description,
            };

            try
            {
                archiveFile.Write(updated);
            }
            catch (Exception exception)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}, because the archive {1} could not be written: {2}", nothingDone, archiveFile.FileName, exception.Message);
            }

            return null;
        }
    }
}
