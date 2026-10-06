// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        /// <summary>
        /// Makes each space's TAS zone identity (<c>SAM.Analytical.Tas.SpaceParameter.ZoneGuid</c>) describe
        /// <b>the .tbd that is about to be exported</b>, taking it from the simulated space it is named after.
        /// <para>
        /// <b>Two modes, and the difference is which .tbd the stamps already on the model belong to.</b> That
        /// is not a property of the stamp - it is a fact about the run, so the caller states it through
        /// <paramref name="stampsWrittenForThisTBD"/> and this method never guesses:
        /// </para>
        /// <para>
        /// <b>A workflow ran.</b> <c>WorkflowCalculator.Calculate</c> stamped every space through
        /// <c>SAM.Analytical.Tas.Modify.UpdateIds</c> against the very file being re-read here, resolving each
        /// space to its zone by the guid it captured before clearing - preferring that over the name, and
        /// refusing rather than guessing where neither identity matches (<c>Query.ResolvedZone</c>). Those
        /// stamps are authoritative <i>and</i> current, so they are <b>never touched</b>: overwriting one from
        /// here replaced a strong identity with a weak one, and did so silently. Only unstamped spaces are
        /// filled.
        /// </para>
        /// <para>
        /// <b>No workflow ran.</b> The .tbd was written moments ago by <c>Tas.Convert.ToTBD</c>, which deletes
        /// any existing file and mints fresh zone guids in <c>building.AddZone()</c> - measured: the same
        /// fixture exported three times gave three entirely different sets of nine guids. A stamp the model
        /// was already carrying therefore names a zone in some <i>earlier</i> .tbd and cannot identify one in
        /// this file at all. Treating it as authoritative is what let
        /// <c>Tas.TM59.Convert.ToXml</c> write a DomOv document naming zones the TAS tool cannot find in the
        /// TBD beside it. In this mode every space is re-derived from the newly read spaces: an unambiguous
        /// name match <b>replaces</b> the stamp, and anything less <b>discards</b> it with a note.
        /// </para>
        /// <para>
        /// <b>Neither mode relaxes the ambiguity rule</b>, which is the other half of the defect this method
        /// replaced. Three flats each containing a "Bedroom 2" are refused in both modes - the mode decides
        /// whether a <i>stale</i> stamp survives, never whether a name may be guessed at.
        /// </para>
        /// <para>
        /// <b>Why the name match survives at all - and it is not a refusal.</b> A model can reach
        /// <see cref="Simulate(UIAnalyticalModel)"/>'s TBD block without having gone through the workflow:
        /// tick "Domestic Overheating" with <b>Simulate</b> unticked and the .tbd is written by
        /// <c>Tas.Convert.ToTBD</c>. That call is made with <c>updateGuids: true</c>, but
        /// <c>AnalyticalModel.AdjacencyCluster</c> hands out a <i>copy</i>, so the zone guids
        /// <c>Tas.Modify.Update</c> stamps go onto a throwaway cluster and never reach the model held here.
        /// Nothing on that path stamps the model, so it arrives at the export either unstamped or - if it was
        /// saved after an earlier simulation - carrying stamps for a .tbd that no longer exists. Both are
        /// handled by the second mode above; neither is authoritative.
        /// </para>
        /// <para>
        /// <c>Tas.TM59.Convert.ToXml</c> does <b>not</b> refuse such a space.
        /// <c>Tas.TM59.Convert.ToTM59(Space, TM59Manager, SystemType)</c> falls back to <c>space.Guid</c>
        /// when <c>ZoneGuid</c> is absent or empty
        /// (<c>SAM.Analytical.Tas.TM59/Convert/ToTM59/Zone.cs</c>), and a SAM space guid is not a TAS zone
        /// guid - a TBD zone's guid is minted by <c>building.AddZone()</c> and has no relationship to the
        /// space it was written from. So without this fill the DomOv XML exports successfully and silently
        /// names zones the external TAS TM59 tool cannot find, which is worse than a refusal, not better.
        /// <c>SimulationZoneIdentityTests</c> pins both halves: the fallback that happens without the fill,
        /// and the TAS identity that appears with it.
        /// </para>
        /// <para>
        /// <b>Only the DomOv export depends on this.</b> "Create SAP" is handed
        /// <c>analyticalModel_TBD</c> - the model read back out of the .tbd, whose spaces
        /// <c>Tas.Convert.ToSAM</c> stamps itself - and "Create Part L"
        /// (<c>Tas.Create.TBD_ByPartL</c> -&gt; <c>UpdateInternalConditionByPartL</c> /
        /// <c>UpdateZoneGroupsByPartL</c> / <c>UpdateZoneGroups</c>) reads no <c>ZoneGuid</c> at all. Neither
        /// is a reason to keep this seam.
        /// </para>
        /// <para>
        /// <b>Ambiguity refuses; it does not take the first hit.</b> Every flat in a block has a "Bedroom 2",
        /// and <c>Find(x =&gt; x.Name == space.Name)</c> answered all three design spaces with one flat's
        /// simulated space - collapsing three identities into one and attributing two dwellings' results to a
        /// third. Two simulated spaces stating the <i>same</i> guid are not a conflict: the same answer twice
        /// is still one answer, the rule <c>VentilationStrategyMap</c> already applies to a repeated claim.
        /// </para>
        /// <para>
        /// <b>The value is copied as a string, verbatim.</b> The previous code read
        /// <c>TryGetValue(ZoneGuid, out Guid)</c> from a parameter declared <c>ParameterType.String</c>, so the
        /// raw TAS value was parsed to a <see cref="Guid"/> and converted back on the way in - which
        /// re-spells it (braces and case are lost to <c>Guid.ToString()</c>). <c>Query.SimulationSpaceKey</c>
        /// compares the stored strings ordinally, so a re-spelt stamp stops matching the TSD side even when
        /// the space it came from was the right one.
        /// </para>
        /// <para>
        /// Pure and static so it can be exercised without an installed TAS - the .tbd read that produces
        /// <paramref name="spaces_Simulation"/> is the only part of the block that needs one.
        /// </para>
        /// </summary>
        /// <param name="spaces_Design">
        /// The model's spaces. Mutated in place where a stamp is written or discarded; the caller is
        /// responsible for putting the returned spaces back into the cluster.
        /// </param>
        /// <param name="spaces_Simulation">The spaces read back from the .tbd that is about to be exported.</param>
        /// <param name="stampsWrittenForThisTBD">
        /// <b>Whether the stamps already on <paramref name="spaces_Design"/> identify zones in the very .tbd
        /// <paramref name="spaces_Simulation"/> was read from.</b> The caller passes its own
        /// <c>workflowCompleted</c>, which is the authoritative answer - see the class note.
        /// <para>
        /// True (a workflow ran): an existing stamp is authoritative and current, and is never touched.
        /// False (no workflow ran, so <c>Tas.Convert.ToTBD</c> minted this .tbd's zone guids just now): an
        /// existing stamp belongs to some earlier .tbd and cannot identify a zone in this one, so it is
        /// replaced from an unambiguous name match and discarded where there is none.
        /// </para>
        /// </param>
        /// <param name="notes">
        /// One sentence per space left without a usable identity, and why. Never null. A space that already
        /// carried a current stamp produces no note - that is the normal case, not an event.
        /// </param>
        /// <returns>
        /// The spaces that were changed - written or cleared - in the order given. Empty where nothing needed
        /// either.
        /// </returns>
        internal static List<Space> RestampSimulationZoneIdentity(IEnumerable<Space> spaces_Design, IEnumerable<Space> spaces_Simulation, bool stampsWrittenForThisTBD, out List<string> notes)
        {
            notes = [];

            List<Space> result = [];

            if (spaces_Design is null)
            {
                return result;
            }

            //Ordinal, and the exact name: the same comparison Modify.UpdateIds indexes its zones by, so this
            //fallback cannot resolve a pairing that one would have rejected.
            Dictionary<string, List<Space>> dictionary_Simulation = new(StringComparer.Ordinal);

            foreach (Space space_Simulation in spaces_Simulation ?? [])
            {
                if (space_Simulation?.Name is not string name_Simulation || string.IsNullOrWhiteSpace(name_Simulation))
                {
                    continue;
                }

                if (!dictionary_Simulation.TryGetValue(name_Simulation, out List<Space> spaces_Named))
                {
                    spaces_Named = [];
                    dictionary_Simulation[name_Simulation] = spaces_Named;
                }

                spaces_Named.Add(space_Simulation);
            }

            foreach (Space space in spaces_Design)
            {
                if (space is null)
                {
                    continue;
                }

                bool stamped = space.TryGetValue(Analytical.Tas.SpaceParameter.ZoneGuid, out string zoneGuid_Existing) && !string.IsNullOrWhiteSpace(zoneGuid_Existing);

                //The whole point, and only where the stamp describes THIS .tbd. A stamped space is then
                //already identified, by a stronger identity than a name, and by the very run that wrote the
                //file being exported. Where no workflow ran, the same stamp identifies a zone in a .tbd that
                //has just been replaced, so it is not authoritative about anything and falls through below.
                if (stamped && stampsWrittenForThisTBD)
                {
                    continue;
                }

                string name = space.Name;

                if (string.IsNullOrWhiteSpace(name) || !dictionary_Simulation.TryGetValue(name, out List<Space> spaces_Named) || spaces_Named.Count == 0)
                {
                    notes.Add(Note(string.Format("Space '{0}' has no identity in the exported TBD: no simulated space is named after it.", name ?? "?"), stamped));

                    if (Discard(space, stamped))
                    {
                        result.Add(space);
                    }

                    continue;
                }

                //Collected rather than counted: two simulated spaces naming the same zone are one answer, and
                //only genuinely different answers are ambiguous.
                HashSet<string> zoneGuids = new(StringComparer.Ordinal);
                foreach (Space space_Named in spaces_Named)
                {
                    if (space_Named.TryGetValue(Analytical.Tas.SpaceParameter.ZoneGuid, out string zoneGuid_Named) && !string.IsNullOrWhiteSpace(zoneGuid_Named))
                    {
                        zoneGuids.Add(zoneGuid_Named);
                    }
                }

                if (zoneGuids.Count == 0)
                {
                    notes.Add(Note(string.Format("Space '{0}' has no identity in the exported TBD: the {1} simulated space(s) named after it state none either.", name, spaces_Named.Count), stamped));

                    if (Discard(space, stamped))
                    {
                        result.Add(space);
                    }

                    continue;
                }

                if (zoneGuids.Count != 1)
                {
                    notes.Add(Note(string.Format("Space '{0}' shares its name with {1} simulated spaces stating {2} different zones, so which one it is has not been established. It is left unstamped rather than given one of them - naming the rooms of each dwelling distinctly, or simulating the model so the workflow stamps it, settles this.", name, spaces_Named.Count, zoneGuids.Count), stamped));

                    if (Discard(space, stamped))
                    {
                        result.Add(space);
                    }

                    continue;
                }

                foreach (string zoneGuid in zoneGuids)
                {
                    //Already right, and the string is identical: nothing to write, and nothing for the caller
                    //to put back. Rare where this .tbd was minted moments ago - TAS does not reuse a zone guid
                    //between exports - but a no-op is cheaper to allow than to reason about.
                    if (stamped && string.Equals(zoneGuid_Existing, zoneGuid, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    //Written as the string it was read as. See the class note on re-spelling. This REPLACES a
                    //stale stamp where one was there - the identity it named is not in the file being exported.
                    space.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, zoneGuid);

                    result.Add(space);
                }
            }

            return result;
        }

        /// <summary>
        /// The reason a space was left without an identity, with the fate of an earlier run's stamp appended
        /// where there was one to discard. Two sentences rather than two message templates, so the two halves
        /// cannot drift apart.
        /// </summary>
        private static string Note(string reason, bool stamped)
        {
            return stamped
                ? reason + " An earlier run's stamp was discarded rather than exported, because it names a zone this TBD does not contain."
                : reason;
        }

        /// <summary>
        /// Drops a stale stamp, following <c>Modify.UpdateIds</c>'s own rule: it clears every space's stamp
        /// before re-resolving, so a failed resolution leaves the space unstamped rather than carrying an
        /// identity that no longer resolves.
        /// <para>
        /// <b>Absent beats wrong.</b> An absent stamp is a state every consumer already handles and reports -
        /// <c>Query.ResolvedZone</c> falls back to the name, <c>Query.SimulationSpaceKey</c> reads null, the
        /// DomOv exporter falls back to <c>space.Guid</c> and this method's own note says so. A stamp naming a
        /// zone in a discarded .tbd looks exactly like a good one and is silently unmatchable.
        /// </para>
        /// </summary>
        /// <returns>Whether anything was removed, and so whether the caller must put this space back.</returns>
        private static bool Discard(Space space, bool stamped)
        {
            if (!stamped)
            {
                return false;
            }

            space.RemoveValue(Analytical.Tas.SpaceParameter.ZoneGuid);

            return true;
        }
    }
}
