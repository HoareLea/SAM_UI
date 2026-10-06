// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// One Approved Document O base provision the user may pick: the iteration, and the ventilation strategy
    /// word that iteration is defined over.
    /// <para>
    /// <b>A picker, not a vocabulary.</b> The words here are the canonical short spellings
    /// <c>SAM.Analytical.Query.PartOVentilationMode</c> already reads - <c>NV</c> and <c>MVHR</c> - and they
    /// are the only ones offered. <c>Query.PartOVentilationMode</c> also accepts longer synonyms
    /// (<c>NaturalVentilation</c> among them), and one of those synonyms is the trap: it prepares
    /// successfully and is then refused by every space at assessment, because
    /// <c>VentilationStrategyMap</c> compares the raw word against a closed set that does not contain it.
    /// Offering no free text is how the UI keeps that combination unreachable. It changes nothing about what
    /// the analytical API accepts.
    /// </para>
    /// <para>
    /// <b>Iteration and strategy travel together because SAM requires them to agree.</b>
    /// <c>Modify.PreparePartOIteration</c> refuses a pairing whose iteration is not the base configuration
    /// for the stated route, and rightly so - the iteration's assumptions are part of the permanent
    /// <c>OverheatingScenario.Key</c>. Rather than restate that rule, <see cref="Options"/> asks SAM
    /// (<c>Query.PartOIterationVentilationMode</c>) which route each iteration is defined over and pairs it
    /// with the canonical word for that route, so the UI cannot offer a combination that would refuse.
    /// <c>PartOVentilationStrategyOptionTests</c> pins the two words by round-tripping them back through
    /// <c>Query.PartOVentilationMode</c>, so a change to either side of the mapping fails a test rather than
    /// reaching a user.
    /// </para>
    /// <para>
    /// <b>Only the two base provisions appear.</b> <c>AcousticRestricted</c> and <c>ActiveTrimCooling</c> are
    /// named in <c>PartOIteration</c> but their operating assumptions are not written, and preparing either
    /// refuses. Offering them would be offering a guaranteed refusal.
    /// </para>
    /// </summary>
    public class PartOVentilationStrategyOption
    {
        private PartOVentilationStrategyOption(PartOIteration partOIteration, PartOVentilationMode partOVentilationMode, string ventilationStrategy, string text)
        {
            PartOIteration = partOIteration;
            PartOVentilationMode = partOVentilationMode;
            VentilationStrategy = ventilationStrategy;
            Text = text;
        }

        /// <summary>The base iteration this option prepares.</summary>
        public PartOIteration PartOIteration { get; }

        /// <summary>The route SAM says that iteration is defined over. Reported, never re-derived here.</summary>
        public PartOVentilationMode PartOVentilationMode { get; }

        /// <summary>
        /// The canonical word handed to <c>Modify.PreparePartOIteration</c> for every assessed zone. This is
        /// the value, and the only value - there is no free-text path to this field.
        /// </summary>
        public string VentilationStrategy { get; }

        /// <summary>What the picker shows.</summary>
        public string Text { get; }

        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// The base provisions the UI offers, in assessment order (1a then 1b).
        /// <para>
        /// Built by asking SAM which route each iteration is defined over. An iteration SAM cannot state a
        /// route for is left out rather than shown with a guessed one.
        /// </para>
        /// </summary>
        public static List<PartOVentilationStrategyOption> Options
        {
            get
            {
                List<PartOVentilationStrategyOption> result = [];

                foreach (PartOIteration partOIteration in new PartOIteration[] { PartOIteration.BasePassive, PartOIteration.BaseNaturalVentilation })
                {
                    //Qualified: SAM.Analytical.UI declares a Query of its own, which would win unqualified.
                    PartOVentilationMode partOVentilationMode = Analytical.Query.PartOIterationVentilationMode(partOIteration, out string _);

                    string ventilationStrategy = CanonicalVentilationStrategy(partOVentilationMode);
                    if (ventilationStrategy is null)
                    {
                        continue;
                    }

                    result.Add(new PartOVentilationStrategyOption(partOIteration, partOVentilationMode, ventilationStrategy, DisplayText(partOVentilationMode)));
                }

                return result;
            }
        }

        /// <summary>
        /// The canonical short word for a route, or null where this assembly has none for it.
        /// <para>
        /// Two entries, both of them words <c>Query.PartOVentilationMode</c> maps back to the very mode they
        /// are listed under - which is what the tests assert rather than trust. Null for anything else, so a
        /// route added to <c>PartOVentilationMode</c> without a word here drops out of the picker instead of
        /// appearing with a wrong one.
        /// </para>
        /// </summary>
        private static string CanonicalVentilationStrategy(PartOVentilationMode partOVentilationMode)
        {
            return partOVentilationMode switch
            {
                PartOVentilationMode.NaturalVentilation => "NV",
                PartOVentilationMode.MVHR => "MVHR",
                _ => null,
            };
        }

        /// <summary>
        /// What the picker shows for one base provision.
        /// <para>
        /// <b>The canonical route word is not repeated here.</b> It used to be appended in brackets, which
        /// on the mechanical route produced the literal reading "base MVHR (MVHR)" - a duplication that
        /// looked like a defect. The word itself is stated once, beside the picker, by each window's own
        /// route line, and it is still carried unchanged on <see cref="VentilationStrategy"/> - which is
        /// what the preparation actually reads. Display text only.
        /// </para>
        /// </summary>
        private static string DisplayText(PartOVentilationMode partOVentilationMode)
        {
            //The iteration number is not derivable from the enum - BasePassive is 1a and predates the name
            //"BaseMVHR" it should have had - so it is spelled out for the user's benefit only, and spelled
            //ONCE: these are the same two constants the Prepare & Run picker uses, so the two windows
            //cannot name the same base provision differently.
            return partOVentilationMode == PartOVentilationMode.MVHR
                ? PartOWorkflowScenario.Text_Iteration1a
                : PartOWorkflowScenario.Text_Iteration1b;
        }
    }
}
