// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// <b>The one authority on where an Approved Document O run's files go</b> beneath the output folder a
    /// person chose - the Part O root:
    /// <code>
    /// &lt;root&gt;/Iteration1a|Iteration1b|Iteration2|Iteration2B|Iteration3|MixedDesign/
    ///     PartOCase.json  the marker that makes this a folder SAM created (see Find)
    ///     tas/            every TAS model and result file, together (.xml .t3d .tbd .tpd .tsd, bridges),
    ///                     and the per-run .sam / .partorun.json / .prepared.sam named from the TSD
    ///     reports/        TM59 .txt reports, the Iteration 3 record and its A/B review .txt / .json
    ///     diagnostics/    timing CSVs, route timing, operating-airflow histories
    /// </code>
    ///
    /// <para><b>Why folders, and why per case</b></para>
    /// <para>
    /// The cases reuse file names: 1a, 1b and 2 all write <c>&lt;model&gt;.tsd</c>. In one flat folder a 1b
    /// run overwrote the Iteration 2 results an Iteration 3 pairing references. A folder per case makes the
    /// same file name resolve to a different path in each case, so no case can overwrite another's. The
    /// file names themselves are unchanged.
    /// </para>
    ///
    /// <para><b>Why the TAS files stay together</b></para>
    /// <para>
    /// SAM_Tas derives the <c>.t3d</c>, <c>.tsd</c> and workflow <c>.json</c> as siblings of the TBD it is
    /// given, and a TPD records the TSD it read. Splitting models from results would break those derivations,
    /// so a case's <c>tas</c> folder is the one directory its TAS work happens in. The per-run <c>.sam</c>
    /// and resume sidecars stay beside the TSD as well: every reader derives them from the TSD path
    /// (<c>Query.Path_PartORunModel</c>, <c>PartORunResume</c>), so moving them would add persistence risk
    /// for no gain.
    /// </para>
    ///
    /// <para><b>The chosen folder is the root, exactly</b></para>
    /// <para>
    /// <see cref="Create"/> takes the root as given - never re-rooted above or below it, whatever it is called.
    /// A person who picks <c>D:\Work\Iteration2\tas</c> gets <c>D:\Work\Iteration2\tas\Iteration1a\tas</c>,
    /// not <c>D:\Work\Iteration1a\tas</c>. Only a root read back off an EXISTING run's results
    /// (<see cref="Root"/> - how Iteration 2B and Iteration 3 follow the run they start from) steps out of a
    /// case folder, and only out of one SAM created.
    /// </para>
    ///
    /// <para><b>Recognised by SAM's own marker, never by name alone - which keeps every other folder safe</b></para>
    /// <para>
    /// A directory is read as part of this layout only when it is <c>&lt;root&gt;/&lt;case&gt;/tas</c>,
    /// <c>/reports</c> or <c>/diagnostics</c> by name AND its case folder holds the <see cref="File_Marker"/>
    /// that <see cref="CreateDirectories"/> wrote, naming that same case (<see cref="Find"/>). A folder a person
    /// made or named - <c>Iteration2</c>, <c>tas</c>, <c>Iteration3\reports</c> - carries no marker, so it is a
    /// legacy flat folder: its reports stay beside its results, as they always were, nothing is filed out of
    /// it, and nothing here moves or rewrites a file in it. A new run started from a legacy run treats that flat
    /// folder as its root, so it writes into its own case folder beneath it and cannot overwrite the legacy
    /// files either.
    /// </para>
    /// </summary>
    public sealed class PartOOutputPaths
    {
        /// <summary>The case folder's TAS subfolder.</summary>
        public const string Folder_Tas = "tas";

        /// <summary>The case folder's report subfolder.</summary>
        public const string Folder_Reports = "reports";

        /// <summary>The case folder's diagnostics subfolder.</summary>
        public const string Folder_Diagnostics = "diagnostics";

        /// <summary>
        /// The marker <see cref="CreateDirectories"/> writes into a case folder - what makes it a folder SAM created,
        /// as opposed to one that is merely called the same. Storage only.
        /// </summary>
        public const string File_Marker = "PartOCase.json";

        /// <summary>The marker's schema.</summary>
        public const string Schema_Marker = "SAM.PartOOutputCase/1";

        /// <summary>What a SAM_Tas timing file's name ends with - the workflow's, the TPD's, the route's and the bridge's.</summary>
        public const string Suffix_Timing = ".timing.csv";

        private PartOOutputPaths(string directory_Root, PartOOutputCase partOOutputCase)
        {
            Directory_Root = directory_Root;
            Case = partOOutputCase;
            Directory_Case = Path.Combine(directory_Root, Folder(partOOutputCase));
            Directory_Tas = Path.Combine(Directory_Case, Folder_Tas);
            Directory_Reports = Path.Combine(Directory_Case, Folder_Reports);
            Directory_Diagnostics = Path.Combine(Directory_Case, Folder_Diagnostics);
            Path_Marker = Path.Combine(Directory_Case, File_Marker);
        }

        /// <summary>The case these paths are for.</summary>
        public PartOOutputCase Case { get; }

        /// <summary>The Part O root - the output folder a person chose.</summary>
        public string Directory_Root { get; }

        /// <summary>The case's own folder beneath the root.</summary>
        public string Directory_Case { get; }

        /// <summary>Where every TAS file of the case is written, together.</summary>
        public string Directory_Tas { get; }

        /// <summary>Where the case's reports are written.</summary>
        public string Directory_Reports { get; }

        /// <summary>Where the case's diagnostic output is written.</summary>
        public string Directory_Diagnostics { get; }

        /// <summary>The case folder's <see cref="File_Marker"/>.</summary>
        public string Path_Marker { get; }

        /// <summary>The folder name of one case.</summary>
        public static string Folder(PartOOutputCase partOOutputCase)
        {
            return partOOutputCase switch
            {
                PartOOutputCase.Iteration1a => "Iteration1a",
                PartOOutputCase.Iteration1b => "Iteration1b",
                PartOOutputCase.Iteration2 => "Iteration2",
                PartOOutputCase.Iteration2B => "Iteration2B",
                PartOOutputCase.Iteration3 => "Iteration3",
                PartOOutputCase.MixedDesign => "MixedDesign",
                _ => throw new ArgumentOutOfRangeException(nameof(partOOutputCase)),
            };
        }

        /// <summary>
        /// One case's paths beneath a Part O root, or null where no root is given. The root is used <b>exactly as
        /// given</b>: whatever it is called, the case folder is created directly inside it. To follow an existing
        /// run's root instead, pass <see cref="Root"/> of its results folder.
        /// </summary>
        public static PartOOutputPaths Create(string directory_Root, PartOOutputCase partOOutputCase)
        {
            string root = Trim(directory_Root);

            if (string.IsNullOrWhiteSpace(root) || !Enum.IsDefined(typeof(PartOOutputCase), partOOutputCase))
            {
                return null;
            }

            return new PartOOutputPaths(root, partOOutputCase);
        }

        /// <summary>
        /// The layout a directory is part of, or null where it is not - which is every folder SAM did not create
        /// as a case folder, however it is named. It is: <c>&lt;root&gt;/&lt;case&gt;/tas</c>, <c>/reports</c> or
        /// <c>/diagnostics</c> by name, without regard to case, whose case folder holds a readable
        /// <see cref="File_Marker"/> naming that same case.
        /// </summary>
        public static PartOOutputPaths Find(string directory)
        {
            string directory_Sub = Trim(directory);
            if (string.IsNullOrWhiteSpace(directory_Sub))
            {
                return null;
            }

            string name_Sub = Path.GetFileName(directory_Sub);
            if (!string.Equals(name_Sub, Folder_Tas, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name_Sub, Folder_Reports, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name_Sub, Folder_Diagnostics, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string directory_Case = Trim(Path.GetDirectoryName(directory_Sub));
            if (string.IsNullOrWhiteSpace(directory_Case))
            {
                return null;
            }

            PartOOutputCase? partOOutputCase = CaseOfFolder(Path.GetFileName(directory_Case));
            if (partOOutputCase is null)
            {
                return null;
            }

            string directory_Root = Trim(Path.GetDirectoryName(directory_Case));
            if (string.IsNullOrWhiteSpace(directory_Root))
            {
                return null;
            }

            //Named like a case folder is not enough: SAM's own marker, for this case, must be there.
            if (CaseOfMarker(Path.Combine(directory_Case, File_Marker)) != partOOutputCase)
            {
                return null;
            }

            return new PartOOutputPaths(directory_Root, partOOutputCase.Value);
        }

        /// <summary>
        /// The layout a file is written into - the one its own directory is part of - or null for a file in a
        /// legacy flat folder. See <see cref="Find"/>.
        /// </summary>
        public static PartOOutputPaths FindForFile(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : Find(Path.GetDirectoryName(path));
        }

        /// <summary>
        /// The Part O root of an EXISTING run's results folder - how a run started from it (Iteration 2B, Iteration 3)
        /// follows it: the root of the SAM-created case folder it is in, or - for a legacy flat folder, however it
        /// is named - the folder itself. Steps out of exactly one case folder, never further.
        /// </summary>
        public static string Root(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            return Find(directory)?.Directory_Root ?? Trim(directory);
        }

        /// <summary>
        /// Where the reports of a results file go: its case's <c>reports</c> folder where the results are in
        /// this layout, and beside the results - exactly as before - where they are in a legacy flat folder.
        /// Null where there is no results path.
        /// </summary>
        public static string Directory_Reports_ForResults(string path_Results)
        {
            if (string.IsNullOrWhiteSpace(path_Results))
            {
                return null;
            }

            return FindForFile(path_Results)?.Directory_Reports ?? Path.GetDirectoryName(path_Results);
        }

        /// <summary>
        /// Where the diagnostic output of a file in a case's <c>tas</c> folder goes: that case's
        /// <c>diagnostics</c> folder, or - legacy flat folder - beside the file, as before.
        /// </summary>
        public static string Directory_Diagnostics_ForFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return FindForFile(path)?.Directory_Diagnostics ?? Path.GetDirectoryName(path);
        }

        /// <summary>
        /// The case a Part O preparation is - Iteration 1a, 1b or 2 - from the base provision it was prepared
        /// over and whether a manufacturer catalogue was offered. Null where that cannot be said: a context of
        /// another iteration, or a resumed record that does not state whether a catalogue was offered.
        /// </summary>
        public static PartOOutputCase? CaseOf(PartOPreparationContext partOPreparationContext)
        {
            if (partOPreparationContext is null)
            {
                return null;
            }

            switch (partOPreparationContext.PartOIteration)
            {
                case PartOIteration.BaseNaturalVentilation:
                    return PartOOutputCase.Iteration1b;

                case PartOIteration.BasePassive:
                    bool? catalogueOffered = partOPreparationContext.VentilationUnitCatalogueOffered;
                    if (catalogueOffered is null)
                    {
                        return null;
                    }

                    return catalogueOffered.Value ? PartOOutputCase.Iteration2 : PartOOutputCase.Iteration1a;

                default:
                    return null;
            }
        }

        /// <summary>
        /// A run's TAS case again, writing into this case's <c>tas</c> folder - how Iteration 2B's rounds and
        /// Iteration 3's Candidate B leave the run they start from where it is. Only the output directory differs
        /// (<see cref="PartOSimulationContext.Copy"/>). Null where there is no case.
        /// </summary>
        public PartOSimulationContext SimulationContext(PartOSimulationContext partOSimulationContext)
        {
            return partOSimulationContext?.Copy(outputDirectory: Directory_Tas);
        }

        /// <summary>
        /// Creates the case folder, its three subfolders and its <see cref="File_Marker"/> - which is what makes it
        /// recognisable as this layout from then on. Idempotent; an existing marker is never rewritten. Throws what
        /// <see cref="Directory.CreateDirectory(string)"/> and <see cref="File.WriteAllText(string, string)"/>
        /// throw - the caller decides whether that is a refusal.
        /// </summary>
        public void CreateDirectories()
        {
            Directory.CreateDirectory(Directory_Tas);
            Directory.CreateDirectory(Directory_Reports);
            Directory.CreateDirectory(Directory_Diagnostics);

            if (!File.Exists(Path_Marker))
            {
                JsonObject jsonObject = new()
                {
                    ["Schema"] = Schema_Marker,
                    ["Case"] = Folder(Case),
                };

                File.WriteAllText(Path_Marker, jsonObject.ToJsonString());
            }
        }

        /// <summary>
        /// <see cref="CreateDirectories"/>, as a refusal rather than an exception: null where the folders exist,
        /// otherwise why they could not be created.
        /// </summary>
        public string TryCreateDirectories()
        {
            try
            {
                CreateDirectories();

                return null;
            }
            catch (Exception exception)
            {
                return string.Format("The Part O output folder '{0}' could not be created. ({1})", Directory_Case, exception.Message);
            }
        }

        /// <summary>
        /// Creates the directory a file is about to be written into, where it is a folder of this layout that
        /// does not exist yet. A legacy folder is never created here - a run writing into one always wrote into
        /// a folder that already existed. Never throws: the write that follows reports the failure in its own
        /// words.
        /// </summary>
        public static void EnsureDirectoryForFile(string path)
        {
            if (FindForFile(path) is null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Moves the timing files SAM_Tas wrote into a case's <c>tas</c> folder - it writes
        /// <c>&lt;name&gt;.timing.csv</c> and <c>&lt;name&gt;.route.timing.csv</c> beside the TBD, TPD and
        /// bridge it is given - into that case's <c>diagnostics</c> folder, keeping their names.
        /// <para>
        /// <b>Only inside this layout.</b> A legacy flat folder is left exactly as it is. Nothing reads these
        /// files back, and a move that fails leaves the file where SAM_Tas put it, so this never fails a run.
        /// </para>
        /// </summary>
        /// <param name="directory_Tas">A case's <c>tas</c> folder.</param>
        /// <returns>Where each moved file now is.</returns>
        public static List<string> FileDiagnostics(string directory_Tas)
        {
            List<string> result = [];

            PartOOutputPaths partOOutputPaths = Find(directory_Tas);
            if (partOOutputPaths is null || !string.Equals(Path.GetFileName(Trim(directory_Tas)), Folder_Tas, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            try
            {
                if (!Directory.Exists(directory_Tas))
                {
                    return result;
                }

                foreach (string path in Directory.GetFiles(directory_Tas, "*" + Suffix_Timing))
                {
                    if (!path.EndsWith(Suffix_Timing, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        Directory.CreateDirectory(partOOutputPaths.Directory_Diagnostics);

                        string path_Destination = Path.Combine(partOOutputPaths.Directory_Diagnostics, Path.GetFileName(path));
                        File.Move(path, path_Destination, true);

                        result.Add(path_Destination);
                    }
                    catch
                    {
                        //Left where SAM_Tas wrote it: a timing file is never worth failing a run over.
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private static PartOOutputCase? CaseOfFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            foreach (PartOOutputCase partOOutputCase in Enum.GetValues(typeof(PartOOutputCase)))
            {
                if (string.Equals(folder, Folder(partOOutputCase), StringComparison.OrdinalIgnoreCase))
                {
                    return partOOutputCase;
                }
            }

            return null;
        }

        /// <summary>The case a marker names, or null where there is none, or it cannot be read, or it is not one.</summary>
        private static PartOOutputCase? CaseOfMarker(string path_Marker)
        {
            try
            {
                if (!File.Exists(path_Marker))
                {
                    return null;
                }

                if (JsonNode.Parse(File.ReadAllText(path_Marker)) is not JsonObject jsonObject)
                {
                    return null;
                }

                if (!string.Equals(jsonObject["Schema"]?.GetValue<string>(), Schema_Marker, StringComparison.Ordinal))
                {
                    return null;
                }

                return CaseOfFolder(jsonObject["Case"]?.GetValue<string>());
            }
            catch
            {
                return null;
            }
        }

        private static string Trim(string directory)
        {
            return string.IsNullOrWhiteSpace(directory) ? directory : Path.TrimEndingDirectorySeparator(directory.Trim());
        }
    }
}
