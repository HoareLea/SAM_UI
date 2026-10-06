// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The <b>canonical TBD</b> an Iteration 2B optimisation's rounds are started from instead of converting
    /// the same geometry again - and the fingerprint that says whether it is still the right one.
    ///
    /// <para><b>Why this exists</b></para>
    /// <para>
    /// A 2B optimisation runs the same thermal case ten times over ten designs, and between rounds only the
    /// <i>ventilation</i> state changes: the design airflow on each terminal, the balanced system duty, and
    /// the transfer/mechanical network <c>PreparePartOIteration</c> rebuilds from them. The geometry, the
    /// zones, the surfaces, the apertures, the constructions and the shading calculation are identical every
    /// round - and on a real model they are the great majority of the work. Measured on the licensed
    /// acceptance model, the conversion is 41.6 s of a 64.2 s round while the full-year simulation itself is
    /// 3.6 s.
    /// </para>
    ///
    /// <para><b>What it is not</b></para>
    /// <para>
    /// <b>Not a cache.</b> A canonical TBD is created by run 0 of one optimisation and used only by that
    /// optimisation's own rounds; nothing here is written to disk as state, keyed, or found again in a later
    /// session. That is deliberate and it is the strongest guarantee available: the classic failure of a
    /// reused conversion is a stale one surviving a model edit, and a baseline that cannot outlive the run
    /// that made it cannot go stale that way at all.
    /// </para>
    /// <para>
    /// <b>Not the round's TBD.</b> Every round is given its own copy - <c>Run0 -> Opt01</c>,
    /// <c>Run0 -> Opt02</c> - and never the previous round's, so no round can inherit another's leftover
    /// state and the baseline itself is only ever read. Chaining <c>Opt01 -> Opt02 -> Opt03</c> would
    /// accumulate exactly the stale state this design exists to prevent.
    /// </para>
    ///
    /// <para><b>What the fingerprint proves, and what it does not</b></para>
    /// <para>
    /// It is taken from the model and the TAS case the canonical was converted from, and compared against
    /// the model and case each round is about to simulate. It covers what the conversion reads: the space,
    /// zone, panel and aperture identities and counts, the zone topology, the constructions and aperture
    /// constructions, a digest of the panel geometry, and the workflow settings that change the prepared
    /// TBD - the solar method, the weather, the day range, sizing, unmet hours, aperture widths and the
    /// construction-layer update.
    /// </para>
    /// <para>
    /// It does <b>not</b> prove that no conceivable model change could be missed - a fingerprint over
    /// enumerated inputs never can. What makes the reuse safe is the two rules above plus this check
    /// together: the baseline belongs to one run, the only thing that changes inside that run is the
    /// ventilation state, and the fingerprint catches a violation of that invariant. <b>Any mismatch falls
    /// back to the full conversion</b>, and it says which category changed rather than only that something
    /// did.
    /// </para>
    ///
    /// <para><b>Design airflow is deliberately absent from the fingerprint</b></para>
    /// <para>
    /// Terminals, systems, air movements and duties are the things that <i>do</i> change every round, and
    /// they are what the warm-started run re-applies. Including them would make every round incompatible
    /// with the baseline and turn the warm start off entirely - which is why the fingerprint enumerates what
    /// the conversion reads rather than hashing the model wholesale.
    /// </para>
    /// </summary>
    public class PartOCanonicalTBD
    {
        private PartOCanonicalTBD(string path_TBD, string fingerprint, long length, DateTime dateTime)
        {
            Path_TBD = path_TBD;
            Fingerprint = fingerprint;
            Length = length;
            DateTime = dateTime;
        }

        /// <summary>The converted TBD every round of this optimisation is copied from. <b>Only ever read.</b></summary>
        public string Path_TBD { get; }

        /// <summary>
        /// What the model and the TAS case looked like when this TBD was converted - see the class
        /// documentation for what it covers.
        /// </summary>
        public string Fingerprint { get; }

        /// <summary>Its length when it was adopted, so a file replaced underneath the run is caught.</summary>
        public long Length { get; }

        /// <summary>And its write time, for the same reason.</summary>
        public DateTime DateTime { get; }

        /// <summary>
        /// Adopts an existing converted TBD as this optimisation's canonical baseline, or explains why it
        /// cannot be one.
        /// </summary>
        /// <param name="path_TBD">The TBD the full conversion produced.</param>
        /// <param name="analyticalModel">The model it was converted from.</param>
        /// <param name="partOSimulationContext">The TAS case it was converted under.</param>
        /// <param name="refusal">Why it cannot be adopted, or null.</param>
        /// <returns>The canonical baseline, or null.</returns>
        public static PartOCanonicalTBD Adopt(string path_TBD, AnalyticalModel analyticalModel, PartOSimulationContext partOSimulationContext, out string refusal)
        {
            refusal = null;

            if (string.IsNullOrWhiteSpace(path_TBD))
            {
                refusal = "No TBD path was given, so there is no converted model to start later rounds from.";

                return null;
            }

            if (!File.Exists(path_TBD))
            {
                refusal = string.Format("The converted TBD '{0}' is not on disk, so later rounds have nothing to start from and each will convert the model in full.", path_TBD);

                return null;
            }

            FileInfo fileInfo = new(path_TBD);

            //A TBD of no length is not a conversion. Caught here rather than at the copy, so the run says
            //why it is converting in full instead of discovering it eleven times over.
            if (fileInfo.Length == 0)
            {
                refusal = string.Format("The converted TBD '{0}' is empty, so it is not a usable baseline and each round will convert the model in full.", path_TBD);

                return null;
            }

            string fingerprint = Fingerprint_Model(analyticalModel, partOSimulationContext, out string refusal_Fingerprint);
            if (fingerprint is null)
            {
                refusal = refusal_Fingerprint;

                return null;
            }

            return new PartOCanonicalTBD(path_TBD, fingerprint, fileInfo.Length, fileInfo.LastWriteTimeUtc);
        }

        /// <summary>
        /// Whether this baseline may be used for the model and case about to be simulated - <b>and, where it
        /// may not, which category changed</b>.
        /// <para>
        /// Checked <b>every round</b> rather than once, because a check made only at adoption would not
        /// notice a file replaced part way through, and because a round that has to convert in full must be
        /// able to say so on its own record.
        /// </para>
        /// </summary>
        /// <param name="refusal">Why not, or null where it may.</param>
        public bool IsValidFor(AnalyticalModel analyticalModel, PartOSimulationContext partOSimulationContext, out string refusal)
        {
            refusal = null;

            if (!File.Exists(Path_TBD))
            {
                refusal = string.Format("The canonical TBD '{0}' is no longer on disk, so this round converted the model in full instead of starting from it.", Path_TBD);

                return false;
            }

            FileInfo fileInfo = new(Path_TBD);

            //Replaced underneath a running optimisation - by another SAM session, or by anything else with
            //the file open. Never reused on the strength of its path alone.
            if (fileInfo.Length != Length || fileInfo.LastWriteTimeUtc != DateTime)
            {
                refusal = string.Format("The canonical TBD '{0}' has been rewritten since this optimisation adopted it, so it is no longer known to be the conversion of this model. This round converted the model in full instead.", Path_TBD);

                return false;
            }

            string fingerprint = Fingerprint_Model(analyticalModel, partOSimulationContext, out string refusal_Fingerprint);
            if (fingerprint is null)
            {
                refusal = refusal_Fingerprint;

                return false;
            }

            if (!string.Equals(fingerprint, Fingerprint, StringComparison.Ordinal))
            {
                refusal = string.Format(
                    "The model or the TAS case has changed in a way the canonical TBD's conversion depends on, so it cannot be reused and this round converted the model in full. Was '{0}'; is now '{1}'.",
                    Describe(Fingerprint),
                    Describe(fingerprint));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Everything the conversion reads, as one string - and <b>nothing a design airflow round writes</b>.
        /// <para>
        /// Grouped by category and labelled, so a mismatch can be reported as "the apertures changed" rather
        /// than as two opaque hashes. Every collection is sorted, so the fingerprint is a function of the
        /// model and not of the order the cluster happened to enumerate it in.
        /// </para>
        /// </summary>
        /// <returns>The fingerprint, or null where the model cannot be fingerprinted at all.</returns>
        private static string Fingerprint_Model(AnalyticalModel analyticalModel, PartOSimulationContext partOSimulationContext, out string refusal)
        {
            refusal = null;

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                refusal = "The model carries no adjacency cluster, so it cannot be compared with the canonical TBD's and this round converted the model in full.";

                return null;
            }

            if (partOSimulationContext is null)
            {
                refusal = "The TAS case this round would run is not recorded, so it cannot be compared with the canonical TBD's and this round converted the model in full.";

                return null;
            }

            StringBuilder stringBuilder = new();

            // ---- The spaces and the zone topology --------------------------------------------------------

            List<string> descriptions = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                if (space is not null)
                {
                    //The NAME as well as the guid: TAS matches a zone to a space by name - see
                    //SAM_Tas Modify.UpdateZones - so a rename is a change the conversion depends on even
                    //though the identity is the same.
                    descriptions.Add(string.Format("{0}|{1}", space.Guid, space.Name));
                }
            }

            Append(stringBuilder, "spaces", descriptions);

            descriptions = [];

            foreach (Zone zone in adjacencyCluster.GetZones() ?? [])
            {
                if (zone is null)
                {
                    continue;
                }

                List<string> guids_Space = [];

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    if (space is not null)
                    {
                        guids_Space.Add(space.Guid.ToString());
                    }
                }

                guids_Space.Sort(StringComparer.Ordinal);

                descriptions.Add(string.Format("{0}|{1}|{2}", zone.Guid, zone.Name, string.Join(",", guids_Space)));
            }

            Append(stringBuilder, "zones", descriptions);

            // ---- The surfaces, their constructions, and their geometry -----------------------------------

            descriptions = [];

            List<string> descriptions_Aperture = [];

            foreach (Panel panel in adjacencyCluster.GetPanels() ?? [])
            {
                if (panel is null)
                {
                    continue;
                }

                //The adiabatic flag is part of what the CONVERSION reads, not only of what the model says:
                //SAM_Tas Modify.UpdateAdiabatic nulls the link of every TBD surface that matches an
                //adiabatic panel, so two models identical but for this flag convert to different TBDs. It
                //matters most for an isolated run, whose isolation cut is expressed entirely through it.
                descriptions.Add(string.Format(
                    "{0}|{1}|{2}|{3}|{4}",
                    panel.Guid,
                    panel.PanelType,
                    panel.Construction?.Name ?? "-",
                    Analytical.Query.Adiabatic(panel),
                    Digest(panel.GetFace3D(false))));

                foreach (Aperture aperture in panel.Apertures ?? [])
                {
                    if (aperture is null)
                    {
                        continue;
                    }

                    descriptions_Aperture.Add(string.Format(
                        "{0}|{1}|{2}|{3}",
                        aperture.Guid,
                        aperture.ApertureType,
                        aperture.ApertureConstruction?.Name ?? "-",
                        Digest(aperture.GetFace3D())));
                }
            }

            Append(stringBuilder, "panels", descriptions);
            Append(stringBuilder, "apertures", descriptions_Aperture);

            // ---- The TAS case ----------------------------------------------------------------------------

            //Everything the workflow settings carry that changes the prepared TBD. The PROJECT NAME is
            //deliberately absent: it is the one thing every round must change, and it decides which files a
            //round writes rather than what is in them.
            Append(stringBuilder, "case",
            [
                string.Format("solar={0}", partOSimulationContext.SolarCalculationMethod),
                string.Format("weather={0}", partOSimulationContext.WeatherData?.Name ?? "-"),
                string.Format("from={0}", partOSimulationContext.SimulateFrom),
                string.Format("to={0}", partOSimulationContext.SimulateTo),
                string.Format("sizing={0}", partOSimulationContext.Sizing),
                string.Format("unmetHours={0}", partOSimulationContext.UnmetHours),
                string.Format("useWidths={0}", partOSimulationContext.UseWidths),
                string.Format("updateConstructionLayers={0}", partOSimulationContext.UpdateConstructionLayersByPanelType),
            ]);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// One category of the fingerprint, as <c>name=count:hash</c> - so a mismatch names the category and
        /// the counts, which is what tells an engineer whether a wall was added or a construction renamed.
        /// </summary>
        private static void Append(StringBuilder stringBuilder, string name, List<string> descriptions)
        {
            descriptions.Sort(StringComparer.Ordinal);

            stringBuilder.Append(string.Format("{0}={1}:{2};", name, descriptions.Count, Hash(string.Join("\n", descriptions))));
        }

        /// <summary>
        /// A surface's geometry, rounded to a millimetre. <b>Rounded, not exact</b>: a coordinate that
        /// round-trips through a serialization at the last bit is the same wall, and treating it as a
        /// different one would turn the warm start off for no engineering reason. A millimetre is far below
        /// anything the conversion resolves and far above float noise.
        /// </summary>
        private static string Digest(SAM.Geometry.Spatial.Face3D face3D)
        {
            if (face3D is null)
            {
                return "-";
            }

            StringBuilder stringBuilder = new();

            List<SAM.Geometry.Spatial.Point3D> point3Ds = SAM.Geometry.Spatial.Query.Point3Ds(face3D);
            if (point3Ds is null)
            {
                return "-";
            }

            foreach (SAM.Geometry.Spatial.Point3D point3D in point3Ds)
            {
                if (point3D is null)
                {
                    continue;
                }

                stringBuilder.Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.###},{1:0.###},{2:0.###} ",
                    point3D.X,
                    point3D.Y,
                    point3D.Z));
            }

            return Hash(stringBuilder.ToString());
        }

        /// <summary>
        /// A stable, non-cryptographic digest. <b>Deliberately not <c>string.GetHashCode</c></b>, which is
        /// randomized per process since .NET Core - a fingerprint compared across two points in the same
        /// process would work and one compared against anything recorded would not, which is the kind of
        /// difference that shows up as an unreproducible bug rather than a test failure.
        /// </summary>
        private static string Hash(string text)
        {
            //FNV-1a, 64 bit. Collision resistance is not a security property here: the two strings being
            //compared are both produced by this class from a model each caller already holds, so the
            //question is only whether an accidental change collides - and 64 bits answers that.
            ulong result = 14695981039346656037;

            foreach (byte value in Encoding.UTF8.GetBytes(text ?? string.Empty))
            {
                result ^= value;
                result *= 1099511628211;
            }

            return result.ToString("x16", CultureInfo.InvariantCulture);
        }

        /// <summary>The category counts of a fingerprint, which is the readable part of it.</summary>
        private static string Describe(string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                return "-";
            }

            List<string> result = [];

            foreach (string part in fingerprint.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                int index = part.LastIndexOf(':');

                result.Add(index < 0 ? part : part.Substring(0, index));
            }

            return string.Join(", ", result);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Path.GetFileName(Path_TBD), Describe(Fingerprint));
        }
    }
}
