// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <b>Regression for the zone-identity restamp in <c>Modify.Simulate</c>.</b>
    /// <para>
    /// The post-workflow block used to re-read the .tbd and copy <c>SpaceParameter.ZoneGuid</c> back onto the
    /// model by matching space NAME, taking the first hit and overwriting whatever was already there. Two
    /// separate defects came out of that, both of them silent:
    /// </para>
    /// <para>
    /// <b>1. Duplicate room names collapsed three identities into one.</b> Every flat in a block has a
    /// "Bedroom 2", so <c>Find(x =&gt; x.Name == space.Name)</c> returned one flat's simulated space for all
    /// three design spaces, and all three were stamped with that one zone guid. The Part O fixture is exactly
    /// this shape, and <c>SimulationSpaceMap</c> then either refuses or pairs a result with the wrong room.
    /// </para>
    /// <para>
    /// <b>2. The value was rewritten even when the match was right.</b> The read was
    /// <c>TryGetValue(ZoneGuid, out Guid)</c> against a parameter declared <c>ParameterType.String</c>, so the
    /// raw TAS string was parsed to a <see cref="System.Guid"/> and then converted back on the way in - which
    /// normalises its spelling. <c>Query.SimulationSpaceKey</c> compares the stored strings ordinally, so any
    /// difference in form between the two sides of the round trip stops every space resolving.
    /// </para>
    /// <para>
    /// <b>The fix is not a better matching algorithm.</b> A stamp written <i>for the .tbd being exported</i> is
    /// authoritative and is left alone - <c>WorkflowCalculator.Calculate</c> has already written it through
    /// <c>SAM.Analytical.Tas.Modify.UpdateIds</c>, which prefers the captured guid and only falls back to a
    /// name. The name match is what the model that did not go through the workflow needs, and there it
    /// refuses an ambiguous name rather than guessing, which is the rule <c>Query.ResolvedZone</c> already
    /// states for the same decision.
    /// </para>
    /// <para>
    /// <b>"Authoritative" is about the run, not the stamp</b> - and getting that wrong was a third defect,
    /// found in review. On the Simulate-unticked path the .tbd is minted fresh by <c>Tas.Convert.ToTBD</c>, so
    /// a stamp the model was already carrying names a zone in an <i>earlier</i> file and can identify nothing
    /// in this one; leaving it alone exported a DomOv document whose GUIDs the TAS tool cannot find in the TBD
    /// beside it. The mode flag is therefore the caller's <c>workflowCompleted</c>, and the tests below assert
    /// both modes on the same inputs: a stale stamp is replaced or discarded, a workflow stamp is untouched,
    /// and <b>ambiguity is refused in both</b>.
    /// </para>
    /// <para>
    /// <b>Why the fill-only seam is kept - stated correctly.</b> An earlier justification claimed
    /// <c>Tas.TM59.Convert.ToXml</c> <i>refuses</i> a space with no <c>ZoneGuid</c>. It does not:
    /// <c>Tas.TM59.Convert.ToTM59(Space, TM59Manager, SystemType)</c> falls back to <c>space.Guid</c> and
    /// exports the zone anyway. The last two tests here replace that claim with the behaviour that is
    /// actually load-bearing - the DomOv XML names the <b>SAM space</b> guid without the fill and the
    /// <b>TAS zone</b> guid with it - because a TBD zone guid is minted by <c>building.AddZone()</c> and is
    /// never the guid of the space it was written from. A silently mis-identified zone in a document that
    /// reports success is what the seam prevents, not a failed export.
    /// </para>
    /// </summary>
    public class SimulationZoneIdentityTests
    {
        //Deliberately spelled in a form Guid.ToString() does not produce: braces and upper case. A test using
        //the canonical form would pass even with the round trip through Guid still in place, and defect 2
        //would go unnoticed.
        private const string zoneGuid_Flat1_Bedroom2 = "{6F1B0F2E-0000-4000-8000-000000000001}";
        private const string zoneGuid_Flat2_Bedroom2 = "{6F1B0F2E-0000-4000-8000-000000000002}";
        private const string zoneGuid_Flat3_Bedroom2 = "{6F1B0F2E-0000-4000-8000-000000000003}";

        private static Space Stamped(string name, string zoneGuid)
        {
            Space result = new Space(name);
            result.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, zoneGuid);

            return result;
        }

        private static string ZoneGuid(Space space)
        {
            return space != null && space.TryGetValue(Analytical.Tas.SpaceParameter.ZoneGuid, out string result) ? result : null;
        }

        /// <summary>
        /// Three flats, one "Bedroom 2" each, all three already stamped by the workflow. The restamp must
        /// leave every one of them exactly as it found it - a name match cannot tell them apart, and the
        /// stamps it would overwrite are the authoritative ones.
        /// </summary>
        [Fact]
        public void DuplicateRoomNames_DoNotCollapseTheWorkflowZoneGuids()
        {
            List<Space> spaces_Design =
            [
                Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat2_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat3_Bedroom2),
            ];

            //What Convert.ToSAM(path_TBD) hands back: same three names, and nothing in them says which design
            //space each belongs to.
            List<Space> spaces_Simulation =
            [
                Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat2_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat3_Bedroom2),
            ];

            //true: these stamps were written by the workflow that wrote the .tbd being re-read.
            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, true, out List<string> notes);

            Assert.Empty(written);

            Assert.Equal(zoneGuid_Flat1_Bedroom2, ZoneGuid(spaces_Design[0]));
            Assert.Equal(zoneGuid_Flat2_Bedroom2, ZoneGuid(spaces_Design[1]));
            Assert.Equal(zoneGuid_Flat3_Bedroom2, ZoneGuid(spaces_Design[2]));

            Assert.NotNull(notes);
        }

        /// <summary>
        /// A space the workflow stamped keeps its stamp <b>byte for byte</b>, even where the name match would
        /// have found the one right counterpart. This is defect 2 on its own: the old code would have parsed
        /// and re-emitted the value, changing its spelling while appearing to copy it.
        /// </summary>
        [Fact]
        public void AnExistingStamp_IsNotRewrittenIntoADifferentForm()
        {
            List<Space> spaces_Design = [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)];
            List<Space> spaces_Simulation = [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)];

            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, true, out List<string> _);

            Assert.Empty(written);
            Assert.Equal(zoneGuid_Flat1_Bedroom2, ZoneGuid(spaces_Design[0]));
        }

        /// <summary>
        /// The path that still needs the name match: a model that never went through the workflow, so nothing
        /// has stamped it. One unambiguous name gets the simulated space's guid, copied verbatim.
        /// </summary>
        [Fact]
        public void AnUnstampedSpace_TakesAnUnambiguousNameMatchVerbatim()
        {
            List<Space> spaces_Design = [new Space("Kitchen")];
            List<Space> spaces_Simulation = [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)];

            //false: no workflow ran, which is the only way a space reaches here unstamped.
            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, false, out List<string> _);

            Assert.Single(written);
            Assert.Equal(zoneGuid_Flat1_Bedroom2, ZoneGuid(spaces_Design[0]));
        }

        /// <summary>
        /// An unstamped space whose name is shared by more than one simulated space is refused with a reason,
        /// not given the first one. This is the duplicate-name case on the un-stamped path, where the old code
        /// silently produced a wrong identity rather than an absent one.
        /// </summary>
        [Fact]
        public void AnUnstampedSpace_WithAnAmbiguousName_IsRefusedRatherThanGuessed()
        {
            List<Space> spaces_Design = [new Space("Bedroom 2")];
            List<Space> spaces_Simulation =
            [
                Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat2_Bedroom2),
            ];

            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, false, out List<string> notes);

            Assert.Empty(written);
            Assert.Null(ZoneGuid(spaces_Design[0]));
            Assert.Contains(notes, x => x != null && x.Contains("Bedroom 2"));
        }

        /// <summary>
        /// An unstamped space no simulated space is named after is reported, so a later gap in the export has
        /// a recorded reason rather than looking like a calculation that produced nothing.
        /// </summary>
        [Fact]
        public void AnUnstampedSpace_WithNoCounterpart_IsReported()
        {
            List<Space> spaces_Design = [new Space("Store")];
            List<Space> spaces_Simulation = [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)];

            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, false, out List<string> notes);

            Assert.Empty(written);
            Assert.Contains(notes, x => x != null && x.Contains("Store"));
        }

        /// <summary>
        /// <b>The dependency the fill-only seam actually has, stated as the identity that comes out of the
        /// exporter.</b>
        /// <para>
        /// This is the Simulate-unticked "Domestic Overheating" path: the model reaches
        /// <c>Tas.TM59.Convert</c> with no <c>ZoneGuid</c> on it, because
        /// <c>Tas.Convert.ToTBD(..., updateGuids: true)</c> stamps a <i>copy</i> of the cluster
        /// (<c>AnalyticalModel.AdjacencyCluster</c> is a copying getter) and the stamps never reach the model
        /// the UI holds.
        /// </para>
        /// <para>
        /// <b>Unstamped is not refused.</b> The export succeeds and the zone carries the SAM space guid -
        /// which is not the TAS zone identity, and never can be: a TBD zone's guid is minted by
        /// <c>building.AddZone()</c>. Only after the fill does the exported zone name the TAS zone.
        /// </para>
        /// </summary>
        [Fact]
        public void AnUnstampedSpace_ExportsTheSAMSpaceGuid_UntilTheFillGivesItTheTasIdentity()
        {
            Space space_Design = Room("Bedroom 2");

            AnalyticalModel analyticalModel = Model(space_Design);

            //The two identities are different values, which is the whole reason the fill matters.
            Assert.NotEqual(Guid.Parse(zoneGuid_Flat1_Bedroom2), space_Design.Guid);

            //Before the fill: exported, NOT refused - and carrying the wrong identity.
            SAM.Analytical.Tas.TM59.Zone zone_Before = ExportedZone(analyticalModel);

            Assert.Equal(space_Design.Guid, zone_Before.Guid);
            Assert.NotEqual(Guid.Parse(zoneGuid_Flat1_Bedroom2), zone_Before.Guid);

            //The fill, exactly as Modify.Simulate applies it.
            analyticalModel = Filled(analyticalModel, [Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2)]);

            //After the fill: the TAS zone identity.
            SAM.Analytical.Tas.TM59.Zone zone_After = ExportedZone(analyticalModel);

            Assert.Equal(Guid.Parse(zoneGuid_Flat1_Bedroom2), zone_After.Guid);
            Assert.NotEqual(space_Design.Guid, zone_After.Guid);
        }

        /// <summary>
        /// The same statement about the document that is actually handed to TAS, rather than about the
        /// intermediate object: <c>DomOverheatZoneItem/GUID</c> is the SAM space guid without the fill and the
        /// TAS zone guid with it. Both exports return true - there is no refusal on either side of the fill.
        /// </summary>
        [Fact]
        public void TheDomOvXmlNamesTheTasZone_OnlyAfterTheFill()
        {
            Space space_Design = Room("Bedroom 2");

            AnalyticalModel analyticalModel = Model(space_Design);

            Assert.Equal(GuidElement(space_Design.Guid), ExportedGuidElement(analyticalModel));

            analyticalModel = Filled(analyticalModel, [Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2)]);

            Assert.Equal(GuidElement(Guid.Parse(zoneGuid_Flat1_Bedroom2)), ExportedGuidElement(analyticalModel));
        }

        //A space the TM59 exporter can actually export: Space.ToTM59 returns null without an internal
        //condition, and a refused zone would prove nothing about which identity is written.
        private static Space Room(string name)
        {
            Space result = new Space(name);
            result.InternalCondition = new InternalCondition("1 Bed Apt. Bedroom");

            return result;
        }

        private static AnalyticalModel Model(params Space[] spaces)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            foreach (Space space in spaces)
            {
                adjacencyCluster.AddObject(space);
            }

            return new AnalyticalModel("Block", null, null, null, adjacencyCluster, null, null);
        }

        //The three lines Modify.Simulate runs: restamp the cluster's spaces, put the written ones back, adopt
        //the resulting model. Kept identical so the assertions above are about the production seam.
        private static AnalyticalModel Filled(AnalyticalModel analyticalModel, List<Space> spaces_Simulation)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            //false - this helper models the Simulate-unticked DomOv path, where no workflow stamped the model.
            List<Space> written = Modify.RestampSimulationZoneIdentity(adjacencyCluster.GetSpaces(), spaces_Simulation, false, out List<string> _);
            foreach (Space space_Written in written)
            {
                adjacencyCluster.AddObject(space_Written);
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        private static SAM.Analytical.Tas.TM59.Zone ExportedZone(AnalyticalModel analyticalModel)
        {
            SAM.Analytical.Tas.TM59.Building building = SAM.Analytical.Tas.TM59.Convert.ToTM59(analyticalModel, new TM59Manager());

            //Not refused, and it is the one room.
            Assert.NotNull(building);

            return Assert.Single(building.Zones);
        }

        //As Zone.ToXml spells it.
        private static string GuidElement(Guid guid)
        {
            return "<GUID>{" + guid.ToString().ToUpper() + "}</GUID>";
        }

        private static string ExportedGuidElement(AnalyticalModel analyticalModel)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");

            try
            {
                Assert.True(SAM.Analytical.Tas.TM59.Convert.ToXml(analyticalModel, path, new TM59Manager()));

                string text = File.ReadAllText(path);

                int index = text.IndexOf("<GUID>", StringComparison.Ordinal);
                Assert.True(index >= 0, "The exported document states no zone GUID.");

                int index_End = text.IndexOf("</GUID>", index, StringComparison.Ordinal);
                Assert.True(index_End > index, "The exported zone GUID element is not closed.");

                return text.Substring(index, index_End + "</GUID>".Length - index);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Stale stamps on the non-workflow export path. Tas.Convert.ToTBD deletes any existing .tbd and mints
        // new zone guids, so a stamp the model was already carrying names a zone in a file that no longer
        // exists - and must not be treated as authoritative just because it is present.
        // ------------------------------------------------------------------------------------------------

        //A zone guid from an earlier .tbd. Not in the file being exported, and never will be: TAS mints a new
        //one per export - measured, three exports of one fixture gave three disjoint sets of nine.
        private const string zoneGuid_Stale = "{6F1B0F2E-0000-4000-8000-00000000DEAD}";

        /// <summary>
        /// <b>The same inputs in both modes, side by side.</b> This is the distinction the whole flag exists
        /// for: whether a stamp is authoritative is a fact about the run that produced the .tbd, not about the
        /// stamp being present.
        /// </summary>
        [Fact]
        public void AStampIsAuthoritativeOnlyWhenTheWorkflowWroteTheTbdBeingExported()
        {
            //Workflow path: the stamp is UpdateIds' own, for this .tbd. Untouched, even though the name match
            //would resolve.
            List<Space> spaces_Design_Workflow = [Stamped("Kitchen", zoneGuid_Stale)];
            List<Space> written_Workflow = Modify.RestampSimulationZoneIdentity(spaces_Design_Workflow, [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)], true, out List<string> _);

            Assert.Empty(written_Workflow);
            Assert.Equal(zoneGuid_Stale, ZoneGuid(spaces_Design_Workflow[0]));

            //Non-workflow path: byte-identical input, and now the stamp is an earlier .tbd's and is replaced by
            //the zone of the file actually being exported.
            List<Space> spaces_Design_NoWorkflow = [Stamped("Kitchen", zoneGuid_Stale)];
            List<Space> written_NoWorkflow = Modify.RestampSimulationZoneIdentity(spaces_Design_NoWorkflow, [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)], false, out List<string> _);

            Assert.Single(written_NoWorkflow);
            Assert.Equal(zoneGuid_Flat1_Bedroom2, ZoneGuid(spaces_Design_NoWorkflow[0]));
        }

        /// <summary>
        /// <b>The duplicate-name refusal is not relaxed by the stale-stamp handling.</b> Three flats, one
        /// "Bedroom 2" each, all carrying stale stamps: the name cannot tell them apart, so none of them is
        /// given one of the three candidates. The stale stamps are discarded rather than exported, because
        /// they name zones this TBD does not contain - absent is reportable, wrong is not.
        /// </summary>
        [Fact]
        public void StaleStamps_WithAmbiguousNames_AreDiscardedAndNeverCollapsed()
        {
            List<Space> spaces_Design =
            [
                Stamped("Bedroom 2", zoneGuid_Stale),
                Stamped("Bedroom 2", zoneGuid_Stale),
                Stamped("Bedroom 2", zoneGuid_Stale),
            ];

            List<Space> spaces_Simulation =
            [
                Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat2_Bedroom2),
                Stamped("Bedroom 2", zoneGuid_Flat3_Bedroom2),
            ];

            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, spaces_Simulation, false, out List<string> notes);

            //All three changed - by having the stale stamp removed, not by being given a guess.
            Assert.Equal(3, written.Count);

            Assert.Null(ZoneGuid(spaces_Design[0]));
            Assert.Null(ZoneGuid(spaces_Design[1]));
            Assert.Null(ZoneGuid(spaces_Design[2]));

            //And none of them took any of the three candidates.
            Assert.DoesNotContain(spaces_Design, x => ZoneGuid(x) == zoneGuid_Flat1_Bedroom2);

            Assert.Equal(3, notes.Count);
            Assert.All(notes, x => Assert.Contains("discarded", x));
        }

        /// <summary>
        /// A stale stamp on a space the exported TBD does not contain at all is discarded and reported. It
        /// cannot be replaced - there is nothing to replace it from - and keeping it would export an identity
        /// for a room that is not in the file.
        /// </summary>
        [Fact]
        public void AStaleStamp_WithNoCounterpart_IsDiscardedAndReported()
        {
            List<Space> spaces_Design = [Stamped("Store", zoneGuid_Stale)];

            List<Space> written = Modify.RestampSimulationZoneIdentity(spaces_Design, [Stamped("Kitchen", zoneGuid_Flat1_Bedroom2)], false, out List<string> notes);

            Assert.Single(written);
            Assert.Null(ZoneGuid(spaces_Design[0]));

            Assert.Contains(notes, x => x != null && x.Contains("Store") && x.Contains("discarded"));
        }

        /// <summary>
        /// <b>The defect as the TAS tool sees it.</b> The exported DomOv document must name a zone of the TBD
        /// written beside it, not one from the run before. Before the fix this exported
        /// <c>zoneGuid_Stale</c> - a GUID matching nothing in the TBD, in a document reporting success.
        /// </summary>
        [Fact]
        public void TheDomOvXmlNamesTheExportedTbdsZone_NotAStaleStamp()
        {
            Space space_Design = Room("Bedroom 2");
            space_Design.SetValue(Analytical.Tas.SpaceParameter.ZoneGuid, zoneGuid_Stale);

            AnalyticalModel analyticalModel = Model(space_Design);

            //Control: the stale stamp is what would be exported if it were left alone.
            Assert.Equal(GuidElement(Guid.Parse(zoneGuid_Stale)), ExportedGuidElement(analyticalModel));

            //The newly written .tbd, read back - its zone is zoneGuid_Flat1_Bedroom2.
            analyticalModel = Filled(analyticalModel, [Stamped("Bedroom 2", zoneGuid_Flat1_Bedroom2)]);

            Assert.Equal(GuidElement(Guid.Parse(zoneGuid_Flat1_Bedroom2)), ExportedGuidElement(analyticalModel));
        }
    }
}
