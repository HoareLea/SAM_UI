// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>What <see cref="UserConstructionLibrary.Read"/> found.</summary>
    public sealed class UserConstructionLibraryContent
    {
        internal UserConstructionLibraryContent(UserConstructionLibraryState state, ConstructionManager constructionManager, string error)
        {
            State = state;
            ConstructionManager = constructionManager ?? new ConstructionManager(new List<ApertureConstruction>(), null, new MaterialLibrary(UserConstructionLibrary.LibraryName));
            Error = error;
        }

        public UserConstructionLibraryState State { get; }

        /// <summary>The saved constructions and every material they use (empty when missing or unreadable).</summary>
        public ConstructionManager ConstructionManager { get; }

        /// <summary>Why the file could not be read; null otherwise.</summary>
        public string Error { get; }

        public List<Construction> Constructions => ConstructionManager.Constructions ?? new List<Construction>();
    }

    /// <summary>What <see cref="UserConstructionLibrary.Save"/> did. On failure the library was not changed.</summary>
    public sealed class UserConstructionSaveResult
    {
        internal UserConstructionSaveResult(Construction saved, IEnumerable<string> addedMaterials, IReadOnlyDictionary<string, string> renamedMaterials, string error)
        {
            Saved = saved;
            AddedMaterials = (addedMaterials ?? Enumerable.Empty<string>()).ToList();
            RenamedMaterials = renamedMaterials ?? new Dictionary<string, string>();
            Error = error;
        }

        public bool Succeeded => Saved != null && Error == null;

        /// <summary>The construction as saved (new Guid, final material names, provenance).</summary>
        public Construction Saved { get; }

        /// <summary>The materials the library did not have before this save.</summary>
        public IReadOnlyList<string> AddedMaterials { get; }

        /// <summary>Materials saved under another name because the library already had a different material of that name (given name → saved name).</summary>
        public IReadOnlyDictionary<string, string> RenamedMaterials { get; }

        public string Error { get; }
    }

    /// <summary>What <see cref="UserConstructionLibrary.Rename"/> or <see cref="UserConstructionLibrary.Remove"/> did. On failure nothing in the library was changed.</summary>
    public sealed class UserConstructionEditResult
    {
        internal UserConstructionEditResult(Construction entry, IEnumerable<string> prunedMaterials, bool modified, string error)
        {
            Entry = entry;
            PrunedMaterials = (prunedMaterials ?? Enumerable.Empty<string>()).ToList();
            Modified = modified;
            Error = error;
        }

        public bool Succeeded => Error == null && Entry != null;

        /// <summary>Rename: the construction under its new name (same Guid). Remove: the construction as it was when it left the library.</summary>
        public Construction Entry { get; }

        /// <summary>Remove: the materials no remaining construction uses, which left the library with the construction (they are in the archive).</summary>
        public IReadOnlyList<string> PrunedMaterials { get; }

        /// <summary>False when the edit succeeded without changing anything (renaming a construction to the name it already has).</summary>
        public bool Modified { get; }

        public string Error { get; }
    }

    /// <summary>
    /// "My constructions": the user's own opaque constructions, saved from the Thermal Performance panel or the classic Constructions editor, in ONE
    /// ordinary SAM <see cref="ConstructionManager"/> JSON file (the format every SAM import and "Add source…" already reads) at
    /// <c>Documents\SAM\User Libraries\Constructions.json</c>: the constructions plus every material they use. It is the opaque twin of
    /// <see cref="UserGlazingLibrary"/> and rests on the same engine (<see cref="UserLibraryFile"/>, <see cref="LibraryMaterialMerge"/>,
    /// <see cref="UserLibraryArchive"/>); it has its own file and its own type, and the two libraries never share a file.
    /// <para>
    /// <b>Saved constructions are immutable</b>: every Save adds a NEW construction (new Guid) and never changes one; names are unique within the
    /// library (trimmed, case-insensitive). The only two things done to a saved construction are <see cref="Rename"/>, which changes its label
    /// (<c>Name</c>) and nothing else - same Guid, same layers, materials and provenance - and <see cref="Remove"/>, which MOVES it to the archive
    /// (<c>Constructions.removed.json</c>) and never deletes it.
    /// </para>
    /// <para>
    /// Safe writes, the lock, the unreadable-file rule and the change notification are the engine's (see <see cref="UserLibraryFile"/>). It has no
    /// model and never touches one: saving, renaming or removing here adds no Undo step.
    /// </para>
    /// </summary>
    public sealed class UserConstructionLibrary
    {
        public const string LibraryName = "My constructions";

        private const string ArchiveDescription = "Constructions removed from My constructions. Nothing here is used by SAM; it is kept so a removed construction can be recovered.";

        private const string LibraryDescription = "Opaque constructions saved by SAM. Each construction is immutable; a change is saved as a new construction.";

        public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(10);

        private static UserConstructionLibrary shared;

        private readonly UserLibraryFile file;

        /// <param name="path">The library file; null for <see cref="DefaultPath"/>.</param>
        public UserConstructionLibrary(string path = null, TimeSpan? lockTimeout = null)
        {
            Path = string.IsNullOrWhiteSpace(path) ? DefaultPath : System.IO.Path.GetFullPath(path);
            file = new UserLibraryFile(Path, LibraryName, "construction library", lockTimeout ?? DefaultLockTimeout);
        }

        /// <summary><c>Documents\SAM\User Libraries\Constructions.json</c>.</summary>
        public static string DefaultPath => System.IO.Path.Combine(Core.Query.UserSAMDirectory(), "User Libraries", "Constructions.json");

        /// <summary>
        /// The library of this SAM process (<see cref="DefaultPath"/>), created on first use: every Thermal Performance panel and the "My library" window
        /// share it, so a Save through it reaches every open alternatives list (<see cref="Changed"/>). Tests replace it so they never read the user's file.
        /// </summary>
        public static UserConstructionLibrary Shared
        {
            get => LazyInitializer.EnsureInitialized(ref shared, () => new UserConstructionLibrary());
            internal set => shared = value;
        }

        /// <summary>
        /// Raised after a Save, Rename or Remove changed the library (on the thread that changed it, after the lock is released), so open lists can
        /// read the library again. Raised once per successful change; not raised for a failed one, nor for a Rename that changed nothing, nor for
        /// changes made by another process (nothing watches the file).
        /// </summary>
        public event EventHandler Changed;

        public string Path { get; }

        public string BackupPath => file.BackupPath;

        /// <summary>Where removed constructions are kept: <c>Constructions.removed.json</c>, next to the library.</summary>
        public string ArchivePath => UserLibraryArchive.PathFor(Path);

        internal string LockPath => file.LockPath;

        /// <summary>TESTS ONLY: called with the path of a file (the library or its archive) just before it is written; a throw simulates a failed write.</summary>
        internal Action<string> BeforeWrite
        {
            get => file.BeforeWrite;
            set => file.BeforeWrite = value;
        }

        /// <summary>Reads the library as it is on disk now. A missing file is an empty library; an unreadable one says why.</summary>
        public UserConstructionLibraryContent Read()
        {
            UserLibraryFileContent content = file.Read();
            switch (content.State)
            {
                case UserLibraryFileState.Missing:
                    return new UserConstructionLibraryContent(UserConstructionLibraryState.Missing, null, null);

                case UserLibraryFileState.Unreadable:
                    return new UserConstructionLibraryContent(UserConstructionLibraryState.Unreadable, null, content.Error);

                default:
                    return new UserConstructionLibraryContent(UserConstructionLibraryState.Ready, content.ConstructionManager, null);
            }
        }

        /// <summary>
        /// Saves <paramref name="construction"/> as a NEW construction named <paramref name="name"/>: a new Guid, the materials it uses embedded from
        /// <paramref name="materials"/> (an identical one reused; a different one of the same name saved as "name (source)" / "name 2" with the
        /// construction's layers renamed with it), the <paramref name="provenance"/> attached (its creation time set now). The input must be an OPAQUE
        /// construction whose materials are all in <paramref name="materials"/>; a transparent or gas construction is rejected. The name is trimmed and
        /// must be unique in the library (ignoring case). Nothing is written unless everything succeeds; a successful Save raises
        /// <see cref="Changed"/> once, after the lock is released. The input and <paramref name="provenance"/> are never changed.
        /// </summary>
        public UserConstructionSaveResult Save(Construction construction, MaterialLibrary materials, string name, UserConstructionProvenance provenance = null, DateTime? createdUtc = null)
        {
            UserConstructionSaveResult result = SaveLocked(construction, materials, name, provenance, createdUtc);
            if (result.Succeeded)
            {
                UserLibraryFile.Notify(Changed, this);
            }

            return result;
        }

        /// <summary>
        /// Why <paramref name="construction"/> cannot be saved (no layers, a material missing from <paramref name="materials"/>, transparent or gas); null when it
        /// can. The rule <see cref="Save"/> applies, so a window can say it before it asks for a name.
        /// </summary>
        public static string Rejection(Construction construction, MaterialLibrary materials)
        {
            List<ConstructionLayer> layers = construction?.ConstructionLayers?.Where(x => x != null).ToList();
            if (construction == null || layers == null || layers.Count == 0)
            {
                return "There is no construction to save: it has no layers.";
            }

            foreach (string layerName in layers.Select(x => x.Name).Distinct())
            {
                if (layerName == null || materials?.GetMaterial(layerName) == null)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} cannot be saved: its material '{1}' is not in the material library.", construction.Name, layerName);
                }
            }

            MaterialType materialType = Analytical.Query.MaterialType(layers, materials);
            if (materialType != MaterialType.Opaque)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0} cannot be saved to {1}: only opaque constructions can ({2} constructions belong with glazing systems).", construction.Name, LibraryName, materialType == MaterialType.Undefined ? "unclassified" : materialType.ToString().ToLowerInvariant());
            }

            return null;
        }

        /// <summary>
        /// Gives the saved construction <paramref name="guid"/> another name. ONLY the name changes: the Guid, layers, materials and provenance are exactly
        /// as they were, so everything that cites the Guid (reports, models that already use the construction) stays true. The name is trimmed, must not
        /// be empty and must be unique in the library (trimmed, case-insensitive, the construction itself excluded - changing only the case is allowed);
        /// the check runs under the lock. An unknown Guid is an error. Nothing is written on failure; renaming to the name the construction already has
        /// succeeds without writing. A successful change raises <see cref="Changed"/> once.
        /// </summary>
        public UserConstructionEditResult Rename(Guid guid, string newName)
        {
            string name = newName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return FailedEdit("The construction needs a name.");
            }

            Construction renamed = null;
            bool modified = false;
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<Construction> constructions = library.Constructions ?? new List<Construction>();
                Construction entry = constructions.Find(x => x?.Guid == guid);
                if (entry == null)
                {
                    return UserLibraryEdit.Fail("There is no saved construction with this Guid in " + LibraryName + ".");
                }

                if (constructions.Any(x => x != null && x.Guid != guid && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    return UserLibraryEdit.Fail(NameTakenMessage(name));
                }

                if (entry.Name == name)
                {
                    renamed = entry;
                    return UserLibraryEdit.NoChange();
                }

                renamed = new Construction(guid, entry, name);
                modified = true;
                return UserLibraryEdit.Write(Library(library.ApertureConstructions, constructions.Select(x => x?.Guid == guid ? renamed : x), library.MaterialLibrary));
            });

            if (error != null)
            {
                return FailedEdit(error);
            }

            if (modified)
            {
                UserLibraryFile.Notify(Changed, this);
            }

            return new UserConstructionEditResult(renamed, null, modified, null);
        }

        /// <summary>
        /// Removes the saved construction <paramref name="guid"/> from the library by MOVING it to the archive (<see cref="ArchivePath"/>); nothing is
        /// ever deleted. The archive receives the construction and every material it uses; the library loses the construction and those materials no
        /// remaining construction (or aperture construction in the file) uses. Models that already use the construction keep their own copy.
        /// <para>
        /// <b>Remove never loses an entry.</b> The archive is written first and the library second: if the archive cannot be written (or is
        /// unreadable) nothing changes; if the library cannot be written after the archive was, the construction is still in the library (and also in
        /// the archive, which is harmless - a Guid still in the library counts as not removed, and a retry is idempotent). An unknown Guid is an error.
        /// A successful Remove raises <see cref="Changed"/> once.
        /// </para>
        /// </summary>
        public UserConstructionEditResult Remove(Guid guid)
        {
            Construction removed = null;
            List<string> pruned = new List<string>();
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<Construction> constructions = library.Constructions ?? new List<Construction>();
                Construction entry = constructions.Find(x => x?.Guid == guid);
                if (entry == null)
                {
                    return UserLibraryEdit.Fail("There is no saved construction with this Guid in " + LibraryName + ".");
                }

                MaterialLibrary materialLibrary = library.MaterialLibrary ?? new MaterialLibrary(LibraryName);

                // 1. The archive first (a failure here changes nothing); 2. the library.
                string archiveError = UserLibraryArchive.Archive(file.Companion(ArchivePath, LibraryName + " (removed)", "construction library"), entry, materialLibrary, LibraryName, ArchiveDescription);
                if (archiveError != null)
                {
                    return UserLibraryEdit.Fail(archiveError);
                }

                List<Construction> remaining = constructions.Where(x => x != null && x.Guid != guid).ToList();
                IEnumerable<string> referenced = remaining.SelectMany(LibraryMaterialMerge.ReferencedNames).Concat((library.ApertureConstructions ?? new List<ApertureConstruction>()).SelectMany(LibraryMaterialMerge.ReferencedNames));
                pruned = LibraryMaterialMerge.Prune(materialLibrary, LibraryMaterialMerge.ReferencedNames(entry), referenced).Select(x => x.Name).ToList();
                removed = entry;
                return UserLibraryEdit.Write(Library(library.ApertureConstructions, remaining, materialLibrary));
            });

            if (error != null)
            {
                return FailedEdit(error);
            }

            UserLibraryFile.Notify(Changed, this);
            return new UserConstructionEditResult(removed, pruned, true, null);
        }

        /// <summary>The library's naming rule for a typed name against the names listed now (a window shows it as you type; Save and Rename check again, under the lock): null when the name can be used.</summary>
        public static string NameProblem(string text, IEnumerable<string> otherNames)
        {
            string name = text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "The construction needs a name.";
            }

            return (otherNames ?? Enumerable.Empty<string>()).Any(x => string.Equals(x?.Trim(), name, StringComparison.OrdinalIgnoreCase)) ? NameTakenMessage(name) : null;
        }

        private static string NameTakenMessage(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, "A construction named '{0}' is already in {1}; choose another name.", name, LibraryName);
        }

        private static ConstructionManager Library(IEnumerable<ApertureConstruction> apertureConstructions, IEnumerable<Construction> constructions, MaterialLibrary materialLibrary)
        {
            return new ConstructionManager(apertureConstructions, constructions, materialLibrary)
            {
                Name = LibraryName,
                Description = LibraryDescription,
            };
        }

        private UserConstructionSaveResult SaveLocked(Construction construction, MaterialLibrary materials, string name, UserConstructionProvenance provenance, DateTime? createdUtc)
        {
            string rejection = Rejection(construction, materials);
            if (rejection != null)
            {
                return Failed(rejection);
            }

            string name_Trimmed = name?.Trim();
            if (string.IsNullOrEmpty(name_Trimmed))
            {
                return Failed("The construction needs a name.");
            }

            UserConstructionSaveResult result = null;
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<Construction> constructions = library.Constructions ?? new List<Construction>();

                if (constructions.Any(x => x != null && string.Equals(x.Name?.Trim(), name_Trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    return UserLibraryEdit.Fail(NameTakenMessage(name_Trimmed));
                }

                // The saved construction is a NEW one: it keeps what the source carries (layers, default panel type, ...) but not its identity.
                Construction renamed = new Construction(Guid.NewGuid(), construction, name_Trimmed);
                if (constructions.Any(x => x?.Guid == renamed.Guid))
                {
                    return UserLibraryEdit.Fail("A construction with this Guid is already saved; saved constructions are never replaced.");
                }

                // Materials: embed, reuse identical, rename different ones of the same name - and the layers with them.
                MaterialLibrary materialLibrary = library.MaterialLibrary ?? new MaterialLibrary(LibraryName);
                HashSet<string> before = new HashSet<string>((materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name));
                Dictionary<string, string> names = new Dictionary<string, string>();
                foreach (string layerName in LibraryMaterialMerge.ReferencedNames(renamed).Distinct())
                {
                    IMaterial material = materials.GetMaterial(layerName);
                    string saved_Name = LibraryMaterialMerge.Add(materialLibrary, material, provenance?.SavedFromSource);
                    if (saved_Name == null)
                    {
                        return UserLibraryEdit.Fail(string.Format(CultureInfo.CurrentCulture, "The material '{0}' could not be added to {1}.", layerName, LibraryName));
                    }

                    names[layerName] = saved_Name;
                }

                Dictionary<string, string> renamedMaterials = names.Where(x => x.Key != x.Value).ToDictionary(x => x.Key, x => x.Value);
                Construction saved = new Construction(renamed, LibraryMaterialMerge.RenameLayers(renamed.ConstructionLayers, names));

                UserConstructionProvenance provenance_Saved = provenance?.Clone() ?? new UserConstructionProvenance();
                provenance_Saved.SchemaVersion = UserConstructionProvenance.CurrentSchemaVersion;
                provenance_Saved.CreatedUtc = (createdUtc ?? DateTime.UtcNow).ToUniversalTime();
                if (!string.IsNullOrWhiteSpace(provenance_Saved.Engine) && string.IsNullOrWhiteSpace(provenance_Saved.SamTasVersion))
                {
                    provenance_Saved.SamTasVersion = SamTasVersion();
                }

                saved.Add(provenance_Saved.ToParameterSet());

                List<string> added = (materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name).Where(x => !before.Contains(x)).ToList();
                result = new UserConstructionSaveResult(saved, added, renamedMaterials, null);
                return UserLibraryEdit.Write(Library(library.ApertureConstructions, constructions.Concat(new[] { saved }), materialLibrary));
            });

            return error == null && result != null ? result : Failed(error);
        }

        private static string SamTasVersion()
        {
            try
            {
                System.Reflection.Assembly assembly = typeof(Tas.ThermalTransmittanceCalculator).Assembly;
                return assembly.GetName().Version?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static UserConstructionSaveResult Failed(string error)
        {
            return new UserConstructionSaveResult(null, null, null, error ?? "The construction could not be saved.");
        }

        private static UserConstructionEditResult FailedEdit(string error)
        {
            return new UserConstructionEditResult(null, null, false, error ?? "The library could not be changed.");
        }
    }
}
