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
        /// Reads glazing systems from a file for "Set glazing" ("Load more glazing..."), without any dialog: a .tcd
        /// (Tas construction database) or a .json (aperture constructions with their materials). The result is
        /// window-local: nothing is written to any model. A .tcd is converted by SAM_Tas and the converted copy is
        /// cached as JSON (<see cref="GlazingSourceCache"/>), so the second load of a large database is nearly instant.
        /// <para>
        /// A TCD construction becomes an aperture construction of <paramref name="apertureType"/> (pane layers; no frame
        /// layers; description and additional heat transfer carried over, as the Edit > Aperture Constructions import
        /// does), keeping the construction's Guid so the same system is the same candidate on every load. Windows take the
        /// constructions with a transparent layer, doors the others. A pane library such as the International Glazing
        /// Database has no constructions: the source then says so in <see cref="GlazingSource.Note"/>.
        /// </para>
        /// Never throws for a bad file: the source comes back empty with a note saying why.
        /// </summary>
        public static GlazingSource ReadGlazingSource(string path, ApertureType apertureType, IProgress<string> progress = null)
        {
            string label = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return Empty(label, "The file could not be found.");
            }

            try
            {
                if (string.Equals(Path.GetExtension(path), ".tcd", StringComparison.OrdinalIgnoreCase))
                {
                    return ReadGlazingSource_TCD(path, label, apertureType, progress);
                }

                progress?.Report(string.Format(CultureInfo.CurrentCulture, "Reading {0}…", label));

                ConstructionManager constructionManager = SAM.Analytical.UI.Query.ImportConstructionManager(path, x => x is Material || x is ApertureConstruction, new ImportOptions() { UserSelection = false, SuppressMessages = true });
                GlazingSource source = new GlazingSource(GlazingSourceKind.Loaded, label, constructionManager ?? new ConstructionManager());
                if (source.GetApertureConstructions(apertureType).Count == 0)
                {
                    source.Note = string.Format(CultureInfo.CurrentCulture, "{0} contains no {1} glazing systems.", label, apertureType.ToString().ToLowerInvariant());
                }

                return source;
            }
            catch (Exception exception)
            {
                return Empty(label, string.Format(CultureInfo.CurrentCulture, "{0} could not be read: {1}", label, exception.Message));
            }
        }

        /// <summary>The same as <see cref="ReadGlazingSource"/>, on its own STA thread (TCD needs one) so the window stays responsive.</summary>
        public static Task<GlazingSource> ReadGlazingSourceAsync(string path, ApertureType apertureType, IProgress<string> progress = null)
        {
            TaskCompletionSource<GlazingSource> completion = new TaskCompletionSource<GlazingSource>(TaskCreationOptions.RunContinuationsAsynchronously);
            Thread thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(ReadGlazingSource(path, apertureType, progress));
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
            })
            {
                IsBackground = true,
                Name = "SAM glazing source reader",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return completion.Task;
        }

        // A .tcd as the SAM_Tas importer converts it, from the JSON cache when this file was converted before (shared by the glazing window and
        // the Thermal Performance sources: the same cache, so a database converted by one is instant in the other). Null when it cannot be read.
        internal static ConstructionManager ReadTcdConstructionManager(string path, string label, IProgress<string> progress)
        {
            ConstructionManager constructionManager = GlazingSourceCache.Read(path);
            if (constructionManager != null)
            {
                progress?.Report(string.Format(CultureInfo.CurrentCulture, "Reading {0} from the cache…", label));
            }
            else
            {
                progress?.Report(string.Format(CultureInfo.CurrentCulture, "Importing {0} from the Tas database for the first time (a large database takes about a minute; it is fast from then on)…", label));
                constructionManager = Tas.Convert.ToSAM_ConstructionManager(path);
                GlazingSourceCache.Write(path, constructionManager);
            }

            return constructionManager;
        }

        // The constructions of a database as aperture constructions of one type (shared with the Thermal Performance sources): windows take the
        // constructions with a transparent layer, doors the others; pane layers only, description and additional heat transfer carried over.
        internal static List<ApertureConstruction> ApertureConstructionsOf(IEnumerable<Construction> constructions, MaterialLibrary materialLibrary, ApertureType apertureType)
        {
            List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
            foreach (Construction construction in constructions ?? new List<Construction>())
            {
                if (construction?.ConstructionLayers == null || construction.ConstructionLayers.Count == 0)
                {
                    continue;
                }

                bool transparent = construction.Transparent(materialLibrary);
                if (transparent != (apertureType != ApertureType.Door))
                {
                    continue;
                }

                ApertureConstruction apertureConstruction = new ApertureConstruction(construction.Guid, construction.Name, apertureType, construction.ConstructionLayers, null);

                if (construction.TryGetValue(ConstructionParameter.Description, out string description) && description != null)
                {
                    apertureConstruction.SetValue(ApertureConstructionParameter.Description, description);
                }

                if (construction.TryGetValue(Tas.ConstructionParameter.AdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer) && additionalHeatTransfer != 0)
                {
                    apertureConstruction.SetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, additionalHeatTransfer);
                    apertureConstruction.SetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, additionalHeatTransfer);
                }

                apertureConstructions.Add(apertureConstruction);
            }

            return apertureConstructions;
        }

        private static GlazingSource ReadGlazingSource_TCD(string path, string label, ApertureType apertureType, IProgress<string> progress)
        {
            ConstructionManager constructionManager = ReadTcdConstructionManager(path, label, progress);

            if (constructionManager == null)
            {
                return Empty(label, string.Format(CultureInfo.CurrentCulture, "{0} could not be read as a Tas construction database.", label));
            }

            MaterialLibrary materialLibrary = constructionManager.MaterialLibrary;
            List<Construction> constructions = constructionManager.Constructions ?? new List<Construction>();

            List<ApertureConstruction> apertureConstructions = ApertureConstructionsOf(constructions, materialLibrary, apertureType);

            GlazingSource source = new GlazingSource(GlazingSourceKind.Loaded, label, new ConstructionManager(apertureConstructions, null, materialLibrary));
            if (apertureConstructions.Count == 0)
            {
                int panes = materialLibrary?.GetMaterials()?.Count(x => x is TransparentMaterial) ?? 0;
                source.Note = constructions.Count == 0 && panes > 0
                    ? string.Format(CultureInfo.CurrentCulture, "{0} contains {1:N0} {2} and no glazing systems; it is a library of single panes, which do not define a Ug on their own.", label, panes, panes == 1 ? "pane" : "panes")
                    : string.Format(CultureInfo.CurrentCulture, "{0} contains no {1} glazing systems.", label, apertureType.ToString().ToLowerInvariant());
            }

            return source;
        }

        private static GlazingSource Empty(string label, string note)
        {
            return new GlazingSource(GlazingSourceKind.Loaded, label, new ConstructionManager()) { Note = note };
        }
    }
}
