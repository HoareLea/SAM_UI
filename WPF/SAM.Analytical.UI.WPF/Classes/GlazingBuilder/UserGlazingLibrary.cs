// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
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

    /// <summary>What <see cref="UserGlazingLibrary.Save"/> did. On failure nothing was written.</summary>
    public sealed class UserGlazingSaveResult
    {
        internal UserGlazingSaveResult(ApertureConstruction saved, IEnumerable<string> addedMaterials, IReadOnlyDictionary<string, string> renamedMaterials, string error, GlazingDraftValidation validation)
        {
            Saved = saved;
            AddedMaterials = (addedMaterials ?? Enumerable.Empty<string>()).ToList();
            RenamedMaterials = renamedMaterials ?? new Dictionary<string, string>();
            Error = error;
            Validation = validation;
        }

        public bool Succeeded => Saved != null && Error == null;

        /// <summary>The system as saved (new Guid, final material names, provenance).</summary>
        public ApertureConstruction Saved { get; }

        /// <summary>The materials the library did not have before this save.</summary>
        public IReadOnlyList<string> AddedMaterials { get; }

        /// <summary>Materials saved under another name because the library already had a different material of that name (draft name → saved name).</summary>
        public IReadOnlyDictionary<string, string> RenamedMaterials { get; }

        public string Error { get; }

        /// <summary>The check Save ran (with the library's names); null when Save stopped before it.</summary>
        public GlazingDraftValidation Validation { get; }
    }

    /// <summary>
    /// "My glazing systems": the user's own predefined glazing systems, made by the Glazing System Builder, in ONE ordinary SAM
    /// <see cref="ConstructionManager"/> JSON file (the format every SAM import and "Add source…" already reads) at
    /// <c>Documents\SAM\User Libraries\Glazing Systems.json</c>: the systems plus every material they use.
    /// <para>
    /// <b>Saved systems are immutable</b>: every Save adds a NEW system (new Guid) and never replaces or removes one; names are unique within
    /// the library (trimmed, case-insensitive).
    /// </para>
    /// <para>
    /// <b>Safe writes</b>: an exclusive lock file for the whole read-merge-write (a second SAM_UI instance waits, then sees the first one's
    /// system - no lost update); the file is RE-READ under the lock and merged by Guid; written to a temporary file and swapped in atomically
    /// (<see cref="File.Replace(string, string, string)"/>), keeping the previous file as <c>Glazing Systems.json.bak</c>; a file that exists but
    /// cannot be read is NEVER overwritten. Every failure is returned, nothing is swallowed.
    /// </para>
    /// It has no model and never touches one.
    /// </summary>
    public sealed class UserGlazingLibrary
    {
        public const string LibraryName = "My glazing systems";

        public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(10);

        private readonly GlazingComposeOptions composeOptions;
        private readonly TimeSpan lockTimeout;

        /// <param name="path">The library file; null for <see cref="DefaultPath"/>.</param>
        /// <param name="gasSource">Gas definitions for composing on save (null: SAM's default gas library).</param>
        public UserGlazingLibrary(string path = null, Func<DefaultGasType, GasMaterial> gasSource = null, TimeSpan? lockTimeout = null)
        {
            Path = string.IsNullOrWhiteSpace(path) ? DefaultPath : System.IO.Path.GetFullPath(path);
            composeOptions = new GlazingComposeOptions() { GasSource = gasSource };
            this.lockTimeout = lockTimeout ?? DefaultLockTimeout;
        }

        /// <summary><c>Documents\SAM\User Libraries\Glazing Systems.json</c>.</summary>
        public static string DefaultPath => System.IO.Path.Combine(Core.Query.UserSAMDirectory(), "User Libraries", "Glazing Systems.json");

        public string Path { get; }

        public string BackupPath => Path + ".bak";

        internal string LockPath => Path + ".lock";

        /// <summary>Reads the library as it is on disk now. A missing file is an empty library; an unreadable one says why.</summary>
        public UserGlazingLibraryContent Read()
        {
            if (!File.Exists(Path))
            {
                return new UserGlazingLibraryContent(UserGlazingLibraryState.Missing, null, null);
            }

            string text;
            try
            {
                text = ReadAllTextShared(Path);
            }
            catch (Exception exception)
            {
                return new UserGlazingLibraryContent(UserGlazingLibraryState.Unreadable, null, string.Format(CultureInfo.CurrentCulture, "{0} could not be read: {1}", System.IO.Path.GetFileName(Path), exception.Message));
            }

            ConstructionManager constructionManager = Parse(text, out string error);
            return constructionManager == null
                ? new UserGlazingLibraryContent(UserGlazingLibraryState.Unreadable, null, string.Format(CultureInfo.CurrentCulture, "{0} is not a readable glazing library ({1}); it is left as it is.", System.IO.Path.GetFileName(Path), error))
                : new UserGlazingLibraryContent(UserGlazingLibraryState.Ready, constructionManager, null);
        }

        /// <summary>
        /// Saves <paramref name="draft"/> as a NEW predefined system: composed with a new Guid; checked (with the library's current names);
        /// its materials embedded (an identical one reused; a different one of the same name saved as "name (source)" / "name 2" with the
        /// system's layer renamed with it); the Builder provenance attached (with <paramref name="performance"/>, the values Tas gave the draft,
        /// when known). Nothing is written unless everything succeeds.
        /// </summary>
        public UserGlazingSaveResult Save(GlazingSystemDraft draft, GlazingValues performance = null, DateTime? createdUtc = null)
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

            FileStream lockStream;
            try
            {
                lockStream = AcquireLock();
            }
            catch (Exception exception)
            {
                return Failed(exception.Message);
            }

            using (lockStream)
            {
                // Re-read under the lock: whatever another instance saved meanwhile is kept.
                UserGlazingLibraryContent content = Read();
                if (content.State == UserGlazingLibraryState.Unreadable)
                {
                    return Failed(content.Error);
                }

                ConstructionManager library = content.ConstructionManager;
                List<ApertureConstruction> systems = library.ApertureConstructions ?? new List<ApertureConstruction>();

                GlazingDraftValidation validation = draft.CheckGlazingDraft(composition, systems.Select(x => x?.Name));
                if (validation.HasErrors)
                {
                    return Failed("The system cannot be saved: " + string.Join(" ", validation.Errors.Select(x => x.Message)), validation);
                }

                ApertureConstruction apertureConstruction = composition.ApertureConstruction;
                if (systems.Any(x => x?.Guid == apertureConstruction.Guid))
                {
                    return Failed("A system with this Guid is already saved; saved systems are never replaced.", validation);
                }

                // Materials: embed, reuse identical, rename different ones of the same name - and the layers with them.
                MaterialLibrary materialLibrary = library.MaterialLibrary ?? new MaterialLibrary(LibraryName);
                HashSet<string> before = new HashSet<string>((materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name));
                Dictionary<string, string> names = new Dictionary<string, string>();
                foreach (IMaterial material in composition.MaterialLibrary.GetMaterials() ?? new List<IMaterial>())
                {
                    composition.MaterialSourceLabels.TryGetValue(material.Name, out string sourceLabel);
                    string name = GlazingMaterialMerge.Add(materialLibrary, material, sourceLabel);
                    if (name == null)
                    {
                        return Failed(string.Format(CultureInfo.CurrentCulture, "The material '{0}' could not be added to {1}.", material.Name, LibraryName), validation);
                    }

                    names[material.Name] = name;
                }

                Dictionary<string, string> renamed = names.Where(x => x.Key != x.Value).ToDictionary(x => x.Key, x => x.Value);
                ApertureConstruction saved = new ApertureConstruction(apertureConstruction, Rename(apertureConstruction.PaneConstructionLayers, names), Rename(apertureConstruction.FrameConstructionLayers, names));

                GlazingBuilderProvenance provenance = composition.Provenance;
                provenance.CreatedUtc = (createdUtc ?? DateTime.UtcNow).ToUniversalTime();
                foreach (GlazingBuilderPaneRecord pane in provenance.Panes)
                {
                    pane.Material = pane.Material != null && names.TryGetValue(pane.Material, out string name) ? name : pane.Material;
                }

                foreach (GlazingBuilderGapRecord gap in provenance.Gaps)
                {
                    gap.Material = gap.Material != null && names.TryGetValue(gap.Material, out string name) ? name : gap.Material;
                }

                if (performance != null)
                {
                    provenance.Performance = performance;
                    provenance.PerformanceEngine = "Tas TCD (SAM_Tas ThermalTransmittanceCalculator.CalculateGlazing)";
                    provenance.SamTasVersion = SamTasVersion();
                }

                saved.Add(provenance.ToParameterSet());

                ConstructionManager result = new ConstructionManager(systems.Concat(new[] { saved }), library.Constructions, materialLibrary)
                {
                    Name = LibraryName,
                    Description = "Glazing systems saved by the SAM Glazing System Builder. Each system is immutable; a change is saved as a new system.",
                };

                try
                {
                    Write(result);
                }
                catch (Exception exception)
                {
                    return Failed(string.Format(CultureInfo.CurrentCulture, "{0} could not be written: {1}", System.IO.Path.GetFileName(Path), exception.Message), validation);
                }

                List<string> added = (materialLibrary.GetMaterials() ?? new List<IMaterial>()).Select(x => x.Name).Where(x => !before.Contains(x)).ToList();
                return new UserGlazingSaveResult(saved, added, renamed, null, validation);
            }
        }

        private static List<ConstructionLayer> Rename(List<ConstructionLayer> constructionLayers, Dictionary<string, string> names)
        {
            return constructionLayers?.Select(x => new ConstructionLayer(x.Name != null && names.TryGetValue(x.Name, out string name) ? name : x.Name, x.Thickness)).ToList();
        }

        internal static ConstructionManager Parse(string text, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "the file is empty";
                return null;
            }

            try
            {
                JsonObject jsonObject = JsonNode.Parse(text) as JsonObject;
                string type = (string)jsonObject?["_type"];
                if (jsonObject == null || type == null || !type.Contains(nameof(ConstructionManager)))
                {
                    error = "it is not a SAM construction manager";
                    return null;
                }

                return new ConstructionManager(jsonObject);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return null;
            }
        }

        // Exclusive lock for the read-merge-write. DeleteOnClose removes it when released (also when the process ends).
        private FileStream AcquireLock()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    if (stopwatch.Elapsed > lockTimeout)
                    {
                        throw new IOException(string.Format(CultureInfo.CurrentCulture, "{0} is being saved by another SAM window; try again in a moment.", System.IO.Path.GetFileName(Path)), exception);
                    }

                    Thread.Sleep(50);
                }
            }
        }

        private void Write(ConstructionManager constructionManager)
        {
            string json = constructionManager.ToJsonObject()?.ToJsonString() ?? throw new InvalidOperationException("The library could not be serialised.");

            // Never write something that would not read back.
            if (Parse(json, out string error) == null)
            {
                throw new InvalidOperationException("The library would not read back: " + error);
            }

            string path_Temp = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(path_Temp, json);
                if (File.Exists(Path))
                {
                    File.Replace(path_Temp, Path, BackupPath, true);
                }
                else
                {
                    File.Move(path_Temp, Path);
                }
            }
            finally
            {
                if (File.Exists(path_Temp))
                {
                    File.Delete(path_Temp);
                }
            }
        }

        // A reader shares with a concurrent atomic replace; a brief sharing violation is retried.
        private static string ReadAllTextShared(string path)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
                catch (IOException) when (attempt < 20)
                {
                    Thread.Sleep(25);
                }
            }
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
    }
}
