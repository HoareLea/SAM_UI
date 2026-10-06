// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// Whether the resultant temperatures the provider produced and the ones the TM59 assessment then
        /// read out of the <b>same</b> result file are the same numbers, for every bound room and every
        /// hour.
        ///
        /// <para><b>Why this is worth a whole pass over the year</b></para>
        /// <para>
        /// Two independent readers resolve a room to a series in Candidate B's result file: SAM_Tas'
        /// provider, which resolves through the route's own native zone bindings, and the TM59
        /// assessment, which resolves through <c>SimulationSpaceMap</c> and <c>SpaceParameter.ZoneGuid</c>.
        /// If those two ever resolve a room differently, <b>both answers are complete, finite and
        /// plausible</b> - a real room's real temperatures, attributed to the wrong room. No completeness
        /// check, no statistic and no TM59 verdict can see that. Comparing the two readings can, and it
        /// is the only thing that can.
        /// </para>
        /// <para>
        /// <b>Exact equality, deliberately.</b> These are not two calculations of the same quantity; they
        /// are two reads of the same stored numbers. A tolerance here would be an invitation for a real
        /// mis-resolution to hide under it.
        /// </para>
        /// <para>
        /// The walk is linear in (rooms x hours) and allocates nothing per hour.
        /// </para>
        /// </summary>
        /// <param name="resultantTemperatureResults">What the provider answered.</param>
        /// <param name="partOIteration3Assessment">Candidate B's assessment, with its capture.</param>
        /// <param name="guids_Space_Bound">Every room the route bound.</param>
        /// <param name="dictionary_Room">Names, for readable refusals. Never used to join.</param>
        /// <param name="count_Values">How many values were compared.</param>
        internal static List<string> PartOIteration3ProviderIdentityRefusals(
            ResultantTemperatureResults resultantTemperatureResults,
            PartOIteration3Assessment partOIteration3Assessment,
            IEnumerable<Guid> guids_Space_Bound,
            Dictionary<Guid, PartOIteration3Room> dictionary_Room,
            out long count_Values)
        {
            List<string> result = [];

            count_Values = 0;

            if (resultantTemperatureResults is null || partOIteration3Assessment is null)
            {
                result.Add("Candidate B produced no resultant temperatures or no assessment, so the two could not be compared.");

                return result;
            }

            int startHour = resultantTemperatureResults.StartHour;
            int expectedCount = resultantTemperatureResults.ExpectedCount;

            foreach (Guid guid_Space in guids_Space_Bound ?? [])
            {
                string name = Name(dictionary_Room, guid_Space);

                ResultantTemperatureResult resultantTemperatureResult = resultantTemperatureResults.Result(guid_Space);

                if (resultantTemperatureResult is null)
                {
                    result.Add(string.Format("Room '{0}' ({1}) is bound to the explicit ventilation route but the resultant temperature provider returned no series for it.", name, guid_Space));

                    continue;
                }

                double[] values = partOIteration3Assessment.ResultantTemperature(guid_Space);

                if (values is null)
                {
                    result.Add(string.Format(
                        "Room '{0}' ({1}) has a provider resultant temperature series, but the TM59 assessment of Candidate B's own results read none for it - so the two are reading the file differently.",
                        name,
                        guid_Space));

                    continue;
                }

                if (values.Length != expectedCount)
                {
                    result.Add(string.Format(
                        "Room '{0}' ({1}): the provider produced {2} value(s) for hours {3}..{4} and the TM59 assessment read {5} from the same file.",
                        name,
                        guid_Space,
                        expectedCount,
                        startHour,
                        resultantTemperatureResults.EndHour,
                        values.Length));

                    continue;
                }

                for (int i = 0; i < values.Length; i++)
                {
                    if (!resultantTemperatureResult.TryGetValue(startHour + i, out double value_Provider))
                    {
                        result.Add(string.Format("Room '{0}' ({1}): the provider has no resultant temperature at hour {2}.", name, guid_Space, startHour + i));

                        break;
                    }

                    if (value_Provider != values[i])
                    {
                        result.Add(string.Format(
                            "Room '{0}' ({1}) at hour {2}: the provider produced {3:R} and the TM59 assessment read {4:R} from the same result file. The two are resolving this room to different results, so the comparison was refused rather than reported.",
                            name,
                            guid_Space,
                            startHour + i,
                            value_Provider,
                            values[i]));

                        break;
                    }

                    count_Values++;
                }
            }

            if (result.Count != 0)
            {
                count_Values = 0;
            }

            return result;
        }

        private static string Name(Dictionary<Guid, PartOIteration3Room> dictionary_Room, Guid guid_Space)
        {
            return dictionary_Room is not null && dictionary_Room.TryGetValue(guid_Space, out PartOIteration3Room partOIteration3Room) ? partOIteration3Room.Name_Space : "?";
        }
    }
}
