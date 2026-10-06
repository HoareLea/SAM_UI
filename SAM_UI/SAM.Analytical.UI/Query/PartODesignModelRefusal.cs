// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        /// <summary>
        /// The first sentence of every refusal to start a Part O case from a Part O result. The Hub, the Prepare
        /// Iteration command and Mixed Design all say it the same way.
        /// </summary>
        public const string PartODesignModelRefusal_Lead = "This is a Part O result. Part O cases run from a design model — open the design model.";

        /// <summary>
        /// Why a Part O case (1a, 1b, 2, or Mixed Design) must not start from <paramref name="analyticalModel"/>
        /// because it is a Part O <b>output</b>, not a design model. Returns null for a design model.
        ///
        /// <para><b>Why this refuses (PR-4, the approved model-state architecture)</b></para>
        /// <para>
        /// Every case derives from the design model. A result opened with File &gt; Open carries its run's
        /// prepared systems, scenarios and provenance. A case derived from it would inherit that state, and the
        /// next result would carry two runs. Before PR-5 nothing records which design model produced a result,
        /// so this refuses and redirects instead of guessing. Reviewing the opened result is unaffected, because
        /// review reads the restored run and not this query.
        /// </para>
        ///
        /// <para><b>Part O signals only</b></para>
        /// <para>
        /// The model is refused when it carries overheating scenarios or a simulation result provenance (only a
        /// Part O run stamps either), or when SAM's baseline validator finds Part O preparation state
        /// (<c>PartOMaterialisationRefusalReason.MaterialisedBaseline</c>). That covers Part O MVHR systems,
        /// per-space Part F conditions written by a preparation, an isolation context and a materialisation
        /// record. SAM owns the Part O system type, so the rule is asked of SAM rather than restated here.
        /// </para>
        /// <para>
        /// Results from an ordinary energy simulation, and design days TAS wrote into the cluster, are <b>not</b>
        /// Part O signals and do not refuse. Mixed Design's own baseline check still refuses them for its purposes.
        /// </para>
        /// </summary>
        public static string PartODesignModelRefusal(AnalyticalModel analyticalModel)
        {
            return analyticalModel is null ? null : PartODesignModelRefusal(analyticalModel, Analytical.Query.PartOBaselineFindings(analyticalModel));
        }

        /// <summary>
        /// <see cref="PartODesignModelRefusal(AnalyticalModel)"/> for an opened result whose file is known: where the result carries
        /// a <c>PartOBaselineReference</c> (PR-5) the refusal also says which case it is and which design model it was derived
        /// from, found by identity (<see cref="PartODerivedFromSentence"/>). Nothing is opened or adopted: the design is named, never
        /// loaded as the open model. A result with no reference is refused exactly as before.
        /// </summary>
        public static string PartODesignModelRefusal(AnalyticalModel analyticalModel, string path_Model)
        {
            return PartODesignModelRefusal(analyticalModel, path_Model, analyticalModel is null ? null : Analytical.Query.PartOBaselineFindings(analyticalModel));
        }

        /// <summary>
        /// <see cref="PartODesignModelRefusal(AnalyticalModel, string)"/> over findings the caller already holds.
        /// </summary>
        public static string PartODesignModelRefusal(AnalyticalModel analyticalModel, string path_Model, IEnumerable<PartOMaterialisationRefusal> partOBaselineFindings)
        {
            string result = PartODesignModelRefusal(analyticalModel, partOBaselineFindings);
            if (result is null)
            {
                return null;
            }

            string sentence = PartODerivedFromSentence(analyticalModel, path_Model);

            return sentence is null ? result : result.Replace(" Review Results still shows", " " + sentence + " Review Results still shows");
        }

        /// <summary>
        /// One or two sentences saying which Part O case an opened result is and which model it was derived from (PR-5), or null where it
        /// carries no valid <c>PartOBaselineReference</c> - a legacy result is not guessed about. The design is found by identity (guid and
        /// state, then the recorded locators), never by name, and opening the result never adopts it.
        /// </summary>
        public static string PartODerivedFromSentence(AnalyticalModel analyticalModel, string path_Model)
        {
            if (analyticalModel is null || !analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.PartOBaselineReference, out PartOBaselineReference partOBaselineReference) || partOBaselineReference is null || !partOBaselineReference.IsValid)
            {
                return null;
            }

            string text_Case = Core.Query.Description(partOBaselineReference.Case);

            if (partOBaselineReference.Design is null)
            {
                return string.Format("It is the {0} result; the design model it came from is not recorded.", text_Case);
            }

            PartOBaselineResolution partOBaselineResolution = Analytical.Query.PartOModelResolution(partOBaselineReference.Design, path_Model);

            string name = string.IsNullOrWhiteSpace(partOBaselineReference.Design.Name) ? "the design model" : string.Format("the design model '{0}'", partOBaselineReference.Design.Name);

            switch (partOBaselineResolution.Status)
            {
                case PartOBaselineResolutionStatus.Resolved:
                    return string.Format("It is the {0} result, derived from {1} ({2}) - open that file to run Part O cases.", text_Case, name, System.IO.Path.GetFileName(partOBaselineResolution.Path));

                case PartOBaselineResolutionStatus.Changed:
                    return string.Format("It is the {0} result, derived from {1} ({2}), which has changed since - open that file to run from its current state.", text_Case, name, System.IO.Path.GetFileName(partOBaselineResolution.Path));

                default:
                    return string.Format("It is the {0} result. {1}", text_Case, partOBaselineResolution.Description);
            }
        }

        /// <summary>
        /// <see cref="PartODesignModelRefusal(AnalyticalModel)"/> over findings the caller already holds from
        /// <c>Analytical.Query.PartOBaselineFindings</c> of the same model - Mixed Design keeps them - so the model
        /// is not validated twice.
        /// </summary>
        public static string PartODesignModelRefusal(AnalyticalModel analyticalModel, IEnumerable<PartOMaterialisationRefusal> partOBaselineFindings)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            List<string> reasons = [];

            if (analyticalModel.HasValue(Analytical.AnalyticalModelParameter.OverheatingScenarios) || analyticalModel.HasValue(Analytical.AnalyticalModelParameter.SimulationResultProvenance))
            {
                reasons.Add("it records the overheating scenarios or the simulation results of a Part O run");
            }

            foreach (PartOMaterialisationRefusal partOMaterialisationRefusal in partOBaselineFindings ?? [])
            {
                if (partOMaterialisationRefusal?.Reason == PartOMaterialisationRefusalReason.MaterialisedBaseline)
                {
                    reasons.Add("it carries systems or conditions a Part O preparation built");

                    break;
                }
            }

            if (reasons.Count == 0)
            {
                return null;
            }

            return string.Format(
                "{0} ({1}.) Review Results still shows this result's assessment. If this file is the only copy of your working model, Results > Part O > Remove Results... saves a cleaned copy you can run from.",
                PartODesignModelRefusal_Lead,
                Capitalise(string.Join("; ", reasons)));
        }

        private static string Capitalise(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
