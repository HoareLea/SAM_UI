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
    /// <summary>What <see cref="UserGlazingLibrary.Read"/> found.</summary>
    public sealed class UserGlazingLibraryContent
    {
        internal UserGlazingLibraryContent(UserGlazingLibraryState state, ConstructionManager constructionManager, string error)
        {
            State = state;
            ConstructionManager = constructionManager ?? new ConstructionManager(new List<ApertureConstruction>(), null, new MaterialLibrary(UserGlazingLibrary.LibraryName));
            Error = error;
        }

        public UserGlazingLibraryState State { get; }

        /// <summary>The saved systems and every material they use (empty when missing or unreadable).</summary>
        public ConstructionManager ConstructionManager { get; }

        /// <summary>Why the file could not be read; null otherwise.</summary>
        public string Error { get; }

        public List<ApertureConstruction> Systems => ConstructionManager.ApertureConstructions ?? new List<ApertureConstruction>();
    }

    /// <summary>
    /// What <see cref="UserGlazingLibrary.Save"/> or <see cref="UserGlazingLibrary.SaveReplacing"/> did. On failure the library was not changed (after a
    /// Save and replace whose library write failed, the replaced system may also be in the archive - it is still active in the library).
    /// </summary>
    public sealed class UserGlazingSaveResult
    {
        internal UserGlazingSaveResult(ApertureConstruction saved, IEnumerable<string> addedMaterials, IReadOnlyDictionary<string, string> renamedMaterials, string error, GlazingDraftValidation validation, ApertureConstruction replaced = null)
        {
            Saved = saved;
            Replaced = replaced;
            AddedMaterials = (addedMaterials ?? Enumerable.Empty<string>()).ToList();
            RenamedMaterials = renamedMaterials ?? new Dictionary<string, string>();
            Error = error;
            Validation = validation;
        }

        public bool Succeeded => Saved != null && Error == null;

        /// <summary>The system as saved (new Guid, final material names, provenance).</summary>
        public ApertureConstruction Saved { get; }

        /// <summary>For <see cref="UserGlazingLibrary.SaveReplacing"/>: the system that was replaced (as it was; it is now in the archive); null for a plain Save.</summary>
        public ApertureConstruction Replaced { get; }

        /// <summary>The materials the library did not have before this save.</summary>
        public IReadOnlyList<string> AddedMaterials { get; }

        /// <summary>Materials saved under another name because the library already had a different material of that name (draft name → saved name).</summary>
        public IReadOnlyDictionary<string, string> RenamedMaterials { get; }

        public string Error { get; }

        /// <summary>The check Save ran (with the library's names); null when Save stopped before it.</summary>
        public GlazingDraftValidation Validation { get; }
    }

    /// <summary>What <see cref="UserGlazingLibrary.Rename"/> or <see cref="UserGlazingLibrary.Remove"/> did. On failure nothing in the library was changed.</summary>
    public sealed class UserGlazingEditResult
    {
        internal UserGlazingEditResult(ApertureConstruction entry, IEnumerable<string> prunedMaterials, bool modified, string error)
        {
            Entry = entry;
            PrunedMaterials = (prunedMaterials ?? Enumerable.Empty<string>()).ToList();
            Modified = modified;
            Error = error;
        }

        public bool Succeeded => Error == null && Entry != null;

        /// <summary>Rename: the system under its new name (same Guid). Remove: the system as it was when it left the library.</summary>
        public ApertureConstruction Entry { get; }

        /// <summary>Remove: the materials no remaining system uses, which left the library with the system (they are in the archive).</summary>
        public IReadOnlyList<string> PrunedMaterials { get; }

        /// <summary>False when the edit succeeded without changing anything (renaming a system to the name it already has).</summary>
        public bool Modified { get; }

        public string Error { get; }
    }

    /// <summary>
    /// "My glazing systems": the user's own predefined glazing systems, made by the Glazing System Builder, in ONE ordinary SAM
    /// <see cref="ConstructionManager"/> JSON file (the format every SAM import and "Add source…" already reads) at
    /// <c>Documents\SAM\User Libraries\Glazing Systems.json</c>: the systems plus every material they use.
    /// <para>
    /// <b>Saved systems are immutable</b>: every Save adds a NEW system (new Guid) and never changes one; names are unique within the library
    /// (trimmed, case-insensitive). The only two things done to a saved system are <see cref="Rename"/>, which changes its label (<c>Name</c>)
    /// and nothing else - same Guid, same layers, materials and provenance - and <see cref="Remove"/>, which MOVES it to the archive
    /// (<c>Glazing Systems.removed.json</c>, see <see cref="UserLibraryArchive"/>) and never deletes it.
    /// </para>
    /// <para>
    /// <b>Safe writes</b> (<see cref="UserLibraryFile"/>): an exclusive lock file for the whole read-edit-write (a second SAM_UI instance waits,
    /// then sees the first one's change - no lost update); the file is RE-READ under the lock and merged by Guid; written to a temporary file and
    /// swapped in atomically (<see cref="File.Replace(string, string, string)"/>), keeping the previous file as <c>Glazing Systems.json.bak</c>;
    /// a file that exists but cannot be read is NEVER overwritten. Every failure is returned, nothing is swallowed.
    /// </para>
    /// It has no model and never touches one.
    /// </summary>
    public sealed class UserGlazingLibrary
    {
        public const string LibraryName = "My glazing systems";

        private const string ArchiveDescription = "Glazing systems removed from My glazing systems. Nothing here is used by SAM; it is kept so a removed system can be recovered.";

        private const string LibraryDescription = "Glazing systems saved by the SAM Glazing System Builder. Each system is immutable; a change is saved as a new system.";

        public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(10);

        private static UserGlazingLibrary shared;

        private readonly GlazingComposeOptions composeOptions;
        private readonly UserLibraryFile file;

        /// <param name="path">The library file; null for <see cref="DefaultPath"/>.</param>
        /// <param name="gasSource">Gas definitions for composing on save (null: SAM's default gas library).</param>
        public UserGlazingLibrary(string path = null, Func<DefaultGasType, GasMaterial> gasSource = null, TimeSpan? lockTimeout = null)
        {
            Path = string.IsNullOrWhiteSpace(path) ? DefaultPath : System.IO.Path.GetFullPath(path);
            composeOptions = new GlazingComposeOptions() { GasSource = gasSource };
            file = new UserLibraryFile(Path, LibraryName, "glazing library", lockTimeout ?? DefaultLockTimeout);
        }

        /// <summary><c>Documents\SAM\User Libraries\Glazing Systems.json</c>.</summary>
        public static string DefaultPath => System.IO.Path.Combine(Core.Query.UserSAMDirectory(), "User Libraries", "Glazing Systems.json");

        /// <summary>
        /// The library of this SAM process (<see cref="DefaultPath"/>), created on first use: every Thermal Performance panel and the Builder share
        /// it, so a Save through it reaches every open candidate list (<see cref="Changed"/>). Tests replace it so they never read the user's file.
        /// </summary>
        public static UserGlazingLibrary Shared
        {
            get => LazyInitializer.EnsureInitialized(ref shared, () => new UserGlazingLibrary());
            internal set => shared = value;
        }

        /// <summary>
        /// Raised after a Save, Rename or Remove changed the library (on the thread that changed it, after the lock is released), so open
        /// candidate lists can read the library again. Raised once per successful change; not raised for a failed one, nor for a Rename that
        /// changed nothing, nor for changes made by another process (nothing watches the file).
        /// </summary>
        public event EventHandler Changed;

        public string Path { get; }

        public string BackupPath => file.BackupPath;

        /// <summary>Where removed systems are kept: <c>Glazing Systems.removed.json</c>, next to the library.</summary>
        public string ArchivePath => UserLibraryArchive.PathFor(Path);

        internal string LockPath => file.LockPath;

        /// <summary>TESTS ONLY: called with the path of a file (the library or its archive) just before it is written; a throw simulates a failed write.</summary>
        internal Action<string> BeforeWrite
        {
            get => file.BeforeWrite;
            set => file.BeforeWrite = value;
        }

        /// <summary>Reads the library as it is on disk now. A missing file is an empty library; an unreadable one says why.</summary>
        public UserGlazingLibraryContent Read()
        {
            UserLibraryFileContent content = file.Read();
            switch (content.State)
            {
                case UserLibraryFileState.Missing:
                    return new UserGlazingLibraryContent(UserGlazingLibraryState.Missing, null, null);

                case UserLibraryFileState.Unreadable:
                    return new UserGlazingLibraryContent(UserGlazingLibraryState.Unreadable, null, content.Error);

                default:
                    return new UserGlazingLibraryContent(UserGlazingLibraryState.Ready, content.ConstructionManager, null);
            }
        }

        /// <summary>
        /// Saves <paramref name="draft"/> as a NEW predefined system: composed with a new Guid; checked (with the library's current names);
        /// its materials embedded (an identical one reused; a different one of the same name saved as "name (source)" / "name 2" with the
        /// system's layer renamed with it); the Builder provenance attached (with <paramref name="performance"/>, the values Tas gave the draft,
        /// when known). Nothing is written unless everything succeeds. A successful Save raises <see cref="Changed"/> once the lock is released.
        /// </summary>
        public UserGlazingSaveResult Save(GlazingSystemDraft draft, GlazingValues performance = null, DateTime? createdUtc = null)
        {
            UserGlazingSaveResult result = SaveLocked(draft, performance, createdUtc, null);
            if (result.Succeeded)
            {
                UserLibraryFile.Notify(Changed, this);
            }

            return result;
        }

        /// <summary>
        /// Saves <paramref name="draft"/> as a NEW system exactly as <see cref="Save"/> does AND moves the saved system <paramref name="replacing"/> to the
        /// archive, in ONE locked transaction ("Save and replace"). The new system has a new Guid; its provenance says what it supersedes (Schema 2:
        /// <c>Supersedes Guid</c> / <c>Supersedes Name</c>) and the replaced system's name may be reused (the name check ignores it). The replaced
        /// system is never changed or deleted: it is archived with its materials; those no remaining system (nor the new one) uses leave the library.
        /// <para>
        /// Order and failure contract: the archive is written first, then the library (see <see cref="UserLibraryArchive"/>). Whatever fails, the
        /// library is left exactly as it was and the replaced system is still in it: the new system is NOT saved unless the old one is archived. After a
        /// failed library write the replaced system is also in the archive (harmless; a retry replaces it there by Guid). A replaced system that is no
        /// longer in the library (removed or renamed away by another window) is an error and nothing is written - save the draft as a new system instead.
        /// A success raises <see cref="Changed"/> once.
        /// </para>
        /// </summary>
        public UserGlazingSaveResult SaveReplacing(GlazingSystemDraft draft, Guid replacing, GlazingValues performance = null, DateTime? createdUtc = null)
        {
            UserGlazingSaveResult result = SaveLocked(draft, performance, createdUtc, replacing);
            if (result.Succeeded)
            {
                UserLibraryFile.Notify(Changed, this);
            }

            return result;
        }

        /// <summary>
        /// Gives the saved system <paramref name="guid"/> another name. ONLY the name changes: the Guid, layers, materials and provenance are exactly
        /// as they were, so everything that cites the Guid (reports, models that already use the system) stays true. The name is trimmed, must not
        /// be empty and must be unique in the library (trimmed, case-insensitive, the system itself excluded - changing only the case is allowed);
        /// the check runs under the lock. An unknown Guid is an error. Nothing is written on failure; renaming to the name the system already has
        /// succeeds without writing. A successful change raises <see cref="Changed"/> once.
        /// </summary>
        public UserGlazingEditResult Rename(Guid guid, string newName)
        {
            string name = newName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return FailedEdit("The system needs a name.");
            }

            ApertureConstruction renamed = null;
            bool modified = false;
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<ApertureConstruction> systems = library.ApertureConstructions ?? new List<ApertureConstruction>();
                ApertureConstruction entry = systems.Find(x => x?.Guid == guid);
                if (entry == null)
                {
                    return UserLibraryEdit.Fail("There is no saved system with this Guid in " + LibraryName + ".");
                }

                if (systems.Any(x => x != null && x.Guid != guid && string.Equals(x.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    return UserLibraryEdit.Fail(string.Format(CultureInfo.CurrentCulture, "A system named '{0}' is already in {1}; choose another name.", name, LibraryName));
                }

                if (entry.Name == name)
                {
                    renamed = entry;
                    return UserLibraryEdit.NoChange();
                }

                renamed = new ApertureConstruction(guid, entry, name);
                modified = true;
                return UserLibraryEdit.Write(Library(systems.Select(x => x?.Guid == guid ? renamed : x), library.Constructions, library.MaterialLibrary));
            });

            if (error != null)
            {
                return FailedEdit(error);
            }

            if (modified)
            {
                UserLibraryFile.Notify(Changed, this);
            }

            return new UserGlazingEditResult(renamed, null, modified, null);
        }

        /// <summary>
        /// Removes the saved system <paramref name="guid"/> from the library by MOVING it to the archive (<see cref="ArchivePath"/>); nothing is
        /// ever deleted. The archive receives the system and every material it uses; the library loses the system and those materials no remaining
        /// system (or opaque construction in the file) uses. Models that already use the system keep their own copy.
        /// <para>
        /// <b>Remove never loses an entry.</b> The archive is written first and the library second: if the archive cannot be written (or is
        /// unreadable) nothing changes; if the library cannot be written after the archive was, the system is still in the library (and also in the
        /// archive, which is harmless - a Guid still in the library counts as not removed, and a retry is idempotent). An unknown Guid is an error.
        /// A successful Remove raises <see cref="Changed"/> once.
        /// </para>
        /// </summary>
        public UserGlazingEditResult Remove(Guid guid)
        {
            ApertureConstruction removed = null;
            List<string> pruned = new List<string>();
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<ApertureConstruction> systems = library.ApertureConstructions ?? new List<ApertureConstruction>();
                ApertureConstruction entry = systems.Find(x => x?.Guid == guid);
                if (entry == null)
                {
                    return UserLibraryEdit.Fail("There is no saved system with this Guid in " + LibraryName + ".");
                }

                MaterialLibrary materialLibrary = library.MaterialLibrary ?? new MaterialLibrary(LibraryName);

                // 1. The archive first (a failure here changes nothing); 2. the library.
                string archiveError = UserLibraryArchive.Archive(file.Companion(ArchivePath, LibraryName + " (removed)", "glazing library"), entry, materialLibrary, LibraryName, ArchiveDescription, RenameInProvenance);
                if (archiveError != null)
                {
                    return UserLibraryEdit.Fail(archiveError);
                }

                List<ApertureConstruction> remaining = systems.Where(x => x != null && x.Guid != guid).ToList();
                IEnumerable<string> referenced = remaining.SelectMany(LibraryMaterialMerge.ReferencedNames).Concat((library.Constructions ?? new List<Construction>()).SelectMany(LibraryMaterialMerge.ReferencedNames));
                pruned = LibraryMaterialMerge.Prune(materialLibrary, LibraryMaterialMerge.ReferencedNames(entry), referenced).Select(x => x.Name).ToList();
                removed = entry;
                return UserLibraryEdit.Write(Library(remaining, library.Constructions, materialLibrary));
            });

            if (error != null)
            {
                return FailedEdit(error);
            }

            UserLibraryFile.Notify(Changed, this);
            return new UserGlazingEditResult(removed, pruned, true, null);
        }

        private static ConstructionManager Library(IEnumerable<ApertureConstruction> systems, List<Construction> constructions, MaterialLibrary materialLibrary)
        {
            return new ConstructionManager(systems, constructions, materialLibrary)
            {
                Name = LibraryName,
                Description = LibraryDescription,
            };
        }

        private UserGlazingSaveResult SaveLocked(GlazingSystemDraft draft, GlazingValues performance, DateTime? createdUtc, Guid? replacing)
        {
            if (draft == null)
            {
                return Failed("There is no glazing system to save.");
            }

            GlazingComposition composition = draft.ComposeGlazingSystem(new GlazingComposeOptions() { Guid = Guid.NewGuid(), GasSource = composeOptions.GasSource });
            if (composition == null || !composition.IsComplete)
            {
                return Failed("The system cannot be saved: " + (composition?.MissingMaterials.Count > 0 ? "material(s) missing: " + string.Join(", ", composition.MissingMaterials) : "it has no panes") + ".", draft.CheckGlazingDraft(composition));
            }

            UserGlazingSaveResult result = null;
            GlazingDraftValidation validation = null;
            string error = file.Transact(content =>
            {
                ConstructionManager library = content.ConstructionManager;
                List<ApertureConstruction> systems = library.ApertureConstructions ?? new List<ApertureConstruction>();

                // Replacing: the system must still be there, and its own name is free for the new one.
                ApertureConstruction replaced = null;
                if (replacing != null)
                {
                    replaced = systems.Find(x => x?.Guid == replacing.Value);
                    if (replaced == null)
                    {
                        return UserLibraryEdit.Fail("The system to replace is no longer in " + LibraryName + " (it was removed or changed in another window); save this one as a new system instead.");
                    }
                }

                validation = draft.CheckGlazingDraft(composition, systems.Where(x => replaced == null || x?.Guid != replaced.Guid).Select(x => x?.Name));
                if (validation.HasErrors)
                {
                    return UserLibraryEdit.Fail("The system cannot be saved: " + string.Join(" ", validation.Errors.Select(x => x.Message)));
                }

                ApertureConstruction apertureConstruction = composition.ApertureConstruction;
                if (systems.Any(x => x?.Guid == apertureConstruction.Guid))
                {
                    return UserLibraryEdit.Fail("A system with this Guid is already saved; saved systems are never replaced.");
                }

                // Materials: embed, reuse identical, rename different ones of the same name - and the layers with them.
                MaterialLibrary materialLibrary = library.MaterialLibrary ?? new MaterialLibrary(LibraryName);
                HashSet<string> before = new HashSet<string>((materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name));
                Dictionary<string, string> names = new Dictionary<string, string>();
                foreach (IMaterial material in composition.MaterialLibrary.GetMaterials() ?? new List<IMaterial>())
                {
                    composition.MaterialSourceLabels.TryGetValue(material.Name, out string sourceLabel);
                    string name = LibraryMaterialMerge.Add(materialLibrary, material, sourceLabel);
                    if (name == null)
                    {
                        return UserLibraryEdit.Fail(string.Format(CultureInfo.CurrentCulture, "The material '{0}' could not be added to {1}.", material.Name, LibraryName));
                    }

                    names[material.Name] = name;
                }

                Dictionary<string, string> renamed = names.Where(x => x.Key != x.Value).ToDictionary(x => x.Key, x => x.Value);
                ApertureConstruction saved = new ApertureConstruction(apertureConstruction, LibraryMaterialMerge.RenameLayers(apertureConstruction.PaneConstructionLayers, names), LibraryMaterialMerge.RenameLayers(apertureConstruction.FrameConstructionLayers, names));

                GlazingBuilderProvenance provenance = composition.Provenance;
                provenance.CreatedUtc = (createdUtc ?? DateTime.UtcNow).ToUniversalTime();
                RenameMaterials(provenance, names);
                if (replaced != null)
                {
                    provenance.SupersedesGuid = replaced.Guid;
                    provenance.SupersedesName = replaced.Name;
                }

                if (performance != null)
                {
                    provenance.Performance = performance;
                    provenance.PerformanceEngine = "Tas TCD (SAM_Tas ThermalTransmittanceCalculator.CalculateGlazing)";
                    provenance.SamTasVersion = SamTasVersion();
                }

                saved.Add(provenance.ToParameterSet());

                List<string> added = (materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name).Where(x => !before.Contains(x)).ToList();
                result = new UserGlazingSaveResult(saved, added, renamed, null, validation, replaced);
                List<ApertureConstruction> remaining = systems.Where(x => replaced == null || x?.Guid != replaced.Guid).ToList();

                if (replaced != null)
                {
                    // 1. The replaced system to the archive first (a failure here changes nothing); 2. the library, without it and with the new one.
                    string archiveError = UserLibraryArchive.Archive(file.Companion(ArchivePath, LibraryName + " (removed)", "glazing library"), replaced, materialLibrary, LibraryName, ArchiveDescription, RenameInProvenance, "Nothing was saved or replaced");
                    if (archiveError != null)
                    {
                        result = null;
                        return UserLibraryEdit.Fail(archiveError);
                    }

                    // What only the replaced system used leaves the library with it; what the new system or another one uses stays.
                    remaining.Add(saved);
                    IEnumerable<string> referenced = remaining.SelectMany(LibraryMaterialMerge.ReferencedNames).Concat((library.Constructions ?? new List<Construction>()).SelectMany(LibraryMaterialMerge.ReferencedNames));
                    LibraryMaterialMerge.Prune(materialLibrary, LibraryMaterialMerge.ReferencedNames(replaced), referenced);
                    return UserLibraryEdit.Write(Library(remaining, library.Constructions, materialLibrary));
                }

                return UserLibraryEdit.Write(Library(systems.Concat(new[] { saved }), library.Constructions, materialLibrary));
            });

            return error == null && result != null ? result : Failed(error, validation);
        }

        // The Builder provenance records which material each pane and gap uses: when a material is kept under another name, the records follow.
        private static void RenameMaterials(GlazingBuilderProvenance provenance, IReadOnlyDictionary<string, string> names)
        {
            foreach (GlazingBuilderPaneRecord pane in provenance.Panes)
            {
                pane.Material = pane.Material != null && names.TryGetValue(pane.Material, out string name) ? name : pane.Material;
            }

            foreach (GlazingBuilderGapRecord gap in provenance.Gaps)
            {
                gap.Material = gap.Material != null && names.TryGetValue(gap.Material, out string name) ? name : gap.Material;
            }
        }

        // The archived system's provenance follows the materials the archive kept under another name (a different material of the same name was there).
        private static ApertureConstruction RenameInProvenance(ApertureConstruction apertureConstruction, IReadOnlyDictionary<string, string> names)
        {
            GlazingBuilderProvenance provenance = GlazingBuilderProvenance.FromApertureConstruction(apertureConstruction);
            if (provenance == null)
            {
                return apertureConstruction;
            }

            RenameMaterials(provenance, names);
            ApertureConstruction result = new ApertureConstruction(apertureConstruction);
            result.Add(provenance.ToParameterSet());
            return result;
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

        private static UserGlazingSaveResult Failed(string error, GlazingDraftValidation validation = null)
        {
            return new UserGlazingSaveResult(null, null, null, error ?? "The system could not be saved.", validation);
        }

        private static UserGlazingEditResult FailedEdit(string error)
        {
            return new UserGlazingEditResult(null, null, false, error ?? "The library could not be changed.");
        }
    }
}
