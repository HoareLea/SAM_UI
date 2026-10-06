// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One room's identity chain through Candidate B, as the pairing record keeps it:
    /// <c>design space -&gt; SystemSpace -&gt; AirSystem -&gt; native TAS zone</c>, with the two design
    /// duties that reached TAS.
    ///
    /// <para><b>The duties recorded are DESIGN airflows and nothing else</b></para>
    /// <para>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c> is the
    /// frozen invariant of SAM #111. What is recorded here is what SAM_Systems materialised from the design
    /// terminals and what the TPD conversion reconciled against - the third and fourth of those numbers do
    /// not appear in this pipeline at all, and the first is a requirement the design was sized against
    /// rather than a duty. Recording the wrong one would make an audit of this pairing agree with itself
    /// and with nothing else.
    /// </para>
    /// <para>
    /// <b>Null is not zero.</b> A room with no supply terminal has no supply duty; a room with a supply
    /// terminal designed at nothing has a duty of zero. They are different rooms and the record keeps them
    /// apart.
    /// </para>
    /// </summary>
    public class PartOIteration3BindingRecord
    {
        public PartOIteration3BindingRecord(
            Guid guid_Space,
            string name_Space,
            Guid guid_Dwelling,
            string name_Dwelling,
            Guid guid_SystemSpace,
            Guid guid_AirSystem,
            string reference_SystemZone,
            double? designFlowRate_Supply_Lps,
            double? designFlowRate_Extract_Lps)
        {
            Guid_Space = guid_Space;
            Name_Space = name_Space;
            Guid_Dwelling = guid_Dwelling;
            Name_Dwelling = name_Dwelling;
            Guid_SystemSpace = guid_SystemSpace;
            Guid_AirSystem = guid_AirSystem;
            Reference_SystemZone = reference_SystemZone;
            DesignFlowRate_Supply_Lps = designFlowRate_Supply_Lps;
            DesignFlowRate_Extract_Lps = designFlowRate_Extract_Lps;
        }

        public Guid Guid_Space { get; }

        /// <summary>Display only. Never joined on.</summary>
        public string Name_Space { get; }

        /// <summary>
        /// The dwelling this room was grouped under, recorded rather than re-derived.
        /// <para>
        /// A review has the reopened model and the record and nothing else - no preparation context, so
        /// no statement of which zones were the assessed dwellings. Re-deriving the grouping from the
        /// model would be a second authority on what a dwelling is, and one that could group a reopened
        /// pairing differently from the run that produced it. So the run writes it down.
        /// </para>
        /// </summary>
        public Guid Guid_Dwelling { get; }

        /// <summary>Display only.</summary>
        public string Name_Dwelling { get; }

        public Guid Guid_SystemSpace { get; }

        public Guid Guid_AirSystem { get; }

        /// <summary>The native TAS zone this room's results were read off.</summary>
        public string Reference_SystemZone { get; }

        /// <summary>The design supply duty that reached TAS [l/s], or null where the room has none.</summary>
        public double? DesignFlowRate_Supply_Lps { get; }

        /// <summary>The design extract duty that reached TAS [l/s], or null where the room has none.</summary>
        public double? DesignFlowRate_Extract_Lps { get; }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                { "Guid_Space", Guid_Space.ToString() },
                { "Name_Space", Name_Space },
                { "Guid_Dwelling", Guid_Dwelling.ToString() },
                { "Name_Dwelling", Name_Dwelling },
                { "Guid_SystemSpace", Guid_SystemSpace.ToString() },
                { "Guid_AirSystem", Guid_AirSystem.ToString() },
                { "Reference_SystemZone", Reference_SystemZone },
            };

            if (DesignFlowRate_Supply_Lps.HasValue)
            {
                result.Add("DesignFlowRate_Supply_Lps", DesignFlowRate_Supply_Lps.Value);
            }

            if (DesignFlowRate_Extract_Lps.HasValue)
            {
                result.Add("DesignFlowRate_Extract_Lps", DesignFlowRate_Extract_Lps.Value);
            }

            return result;
        }

        public static PartOIteration3BindingRecord FromJsonObject(JsonObject jsonObject)
        {
            return jsonObject is null
                ? null
                : new PartOIteration3BindingRecord(
                    PartOIteration3Json.Guid(jsonObject, "Guid_Space"),
                    PartOIteration3Json.Text(jsonObject, "Name_Space"),
                    PartOIteration3Json.Guid(jsonObject, "Guid_Dwelling"),
                    PartOIteration3Json.Text(jsonObject, "Name_Dwelling"),
                    PartOIteration3Json.Guid(jsonObject, "Guid_SystemSpace"),
                    PartOIteration3Json.Guid(jsonObject, "Guid_AirSystem"),
                    PartOIteration3Json.Text(jsonObject, "Reference_SystemZone"),
                    PartOIteration3Json.NullableNumber(jsonObject, "DesignFlowRate_Supply_Lps"),
                    PartOIteration3Json.NullableNumber(jsonObject, "DesignFlowRate_Extract_Lps"));
        }

        public override string ToString()
        {
            return string.Format(
                "{0} ({1}) -> zone {2}: supply {3}, extract {4} l/s",
                Name_Space ?? "?",
                Guid_Space,
                Reference_SystemZone ?? "-",
                DesignFlowRate_Supply_Lps.HasValue ? DesignFlowRate_Supply_Lps.Value.ToString("0.###") : "-",
                DesignFlowRate_Extract_Lps.HasValue ? DesignFlowRate_Extract_Lps.Value.ToString("0.###") : "-");
        }
    }
}
