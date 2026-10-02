// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Reads ONE source for the Thermal Performance panel ("Add source..."): a .tcd (Tas construction database) or a .json (constructions,
        /// aperture constructions and their materials), without any dialog. It is the existing glazing reader's import - the same SAM_Tas
        /// converter and the same JSON cache (<see cref="GlazingSourceCache"/>), so a database converted by either is instant in the other -
        /// but it keeps EVERYTHING the file has instead of one aperture type: its opaque constructions (candidates for walls, roofs and floors),
        /// its transparent constructions as window systems and its other constructions as door systems (candidates for glazing rows), and the
        /// materials they name. Construction Guids are kept, so the same construction is the same candidate on every load.
        /// <para>
        /// The result is a pool only: nothing is written to any model, and an IGDB-style pane library comes back with a note saying it has no
        /// systems (panes are not composed into systems here). Never throws for a bad file: the source comes back empty with a note saying why.
        /// </para>
        /// </summary>
        public static GlazingSource ReadThermalSource(string path, IProgress<string> progress = null)
        {
            string label = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Empty(label, "The file could not be found.");
            }

            try
            {
                ConstructionManager constructionManager;
                if (string.Equals(Path.GetExtension(path), ".tcd", StringComparison.OrdinalIgnoreCase))
                {
                    constructionManager = ReadTcdConstructionManager(path, label, progress);
                    if (constructionManager == null)
                    {
                        return Empty(label, string.Format(CultureInfo.CurrentCulture, "{0} could not be read as a Tas construction database.", label));
                    }
                }
                else
                {
                    progress?.Report(string.Format(CultureInfo.CurrentCulture, "Reading {0}…", label));
                    constructionManager = SAM.Analytical.UI.Query.ImportConstructionManager(path, x => x is Material || x is Construction || x is ApertureConstruction, new ImportOptions() { UserSelection = false, SuppressMessages = true });
                    if (constructionManager == null)
                    {
                        return Empty(label, string.Format(CultureInfo.CurrentCulture, "{0} contains no constructions.", label));
                    }
                }

                MaterialLibrary materialLibrary = constructionManager.MaterialLibrary;
                List<Construction> constructions = (constructionManager.Constructions ?? new List<Construction>()).Where(x => x?.ConstructionLayers != null && x.ConstructionLayers.Count != 0).ToList();

                // The file's own aperture constructions (a .json may have them, with frames) and the systems derived from its constructions; the
                // first of a Guid wins. A TCD construction is a window when it has a transparent layer, else a door.
                List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
                HashSet<Guid> guids = new HashSet<Guid>();
                foreach (ApertureConstruction apertureConstruction in (constructionManager.ApertureConstructions ?? new List<ApertureConstruction>()).Concat(ApertureConstructionsOf(constructions, materialLibrary, ApertureType.Window)).Concat(ApertureConstructionsOf(constructions, materialLibrary, ApertureType.Door)))
                {
                    if (apertureConstruction != null && guids.Add(apertureConstruction.Guid))
                    {
                        apertureConstructions.Add(apertureConstruction);
                    }
                }

                GlazingSource source = new GlazingSource(GlazingSourceKind.Loaded, label, new ConstructionManager(apertureConstructions, constructions, materialLibrary));
                if (constructions.Count == 0 && apertureConstructions.Count == 0)
                {
                    int panes = materialLibrary?.GetMaterials()?.Count(x => x is TransparentMaterial) ?? 0;
                    source.Note = panes > 0
                        ? string.Format(CultureInfo.CurrentCulture, "{0} contains {1:N0} {2} and no constructions or glazing systems; it is a library of single panes, which do not define a U-value on their own.", label, panes, panes == 1 ? "pane" : "panes")
                        : string.Format(CultureInfo.CurrentCulture, "{0} contains no constructions or glazing systems.", label);
                }

                return source;
            }
            catch (Exception exception)
            {
                return Empty(label, string.Format(CultureInfo.CurrentCulture, "{0} could not be read: {1}", label, exception.Message));
            }
        }

        /// <summary>The same as <see cref="ReadThermalSource"/>, on its own STA thread (TCD needs one) so the panel stays responsive.</summary>
        public static Task<GlazingSource> ReadThermalSourceAsync(string path, IProgress<string> progress = null)
        {
            TaskCompletionSource<GlazingSource> completion = new TaskCompletionSource<GlazingSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            Thread thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(ReadThermalSource(path, progress));
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
            })
            {
                IsBackground = true,
                Name = "SAM thermal source reader",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return completion.Task;
        }
    }
}
