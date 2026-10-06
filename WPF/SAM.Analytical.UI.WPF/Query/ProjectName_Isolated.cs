// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        /// <summary>
        /// What an isolated run's project is called: <c>&lt;project&gt;-ISO-&lt;token&gt;</c>.
        ///
        /// <para><b>Why the name has to change at all</b></para>
        /// <para>
        /// Every artifact a Part O run leaves is derived from its project name - the TBD, the TSD, the
        /// <c>.sam</c> (<see cref="Path_PartORunModel(string)"/>) and the TM59 report. A full run and an
        /// isolated run of the same building would otherwise derive the same paths and silently overwrite
        /// each other, and so would two different isolated selections. The token is a function of the
        /// selected space guids (<see cref="PartOIsolationContext.Token"/>), so one selection always names
        /// the same run and any other selection names a different one - including two dwellings that happen
        /// to share a display name, which is exactly the case a readable suffix could not separate.
        /// </para>
        ///
        /// <para><b>Naming only. Never provenance.</b></para>
        /// <para>
        /// Nothing reads isolation state back out of this name, and nothing should: a file can be renamed,
        /// and a renamed file must not be able to change what a run is understood to have been. The
        /// authority is <see cref="PartOIsolationContext"/> stamped on the model, which travels into the
        /// run's <c>.sam</c>. This suffix exists so a person can tell two output sets apart in a folder, and
        /// so the two sets exist at all.
        /// </para>
        /// <para>
        /// Applied to the model's name, which is what the Simulate dialog offers as the project name - so
        /// the isolated identity reaches every artifact through the naming path that already exists, rather
        /// than through a second one beside it.
        /// </para>
        ///
        /// <para><b>And on the guided route it is no longer overwritable</b></para>
        /// <para>
        /// This paragraph used to end "a person may still overwrite it in that dialog; that is their choice
        /// to make", and that stopped being true: <c>SimulateControl.LockPartOSettings</c> locks the project
        /// name on the Part O route. The reason it locks is one that did not exist when the sentence was
        /// written - <see cref="PartOSimulationContext.Iteration_ProjectName"/> reads an Iteration 2B round
        /// number back OUT of the project name, so a hand-edited name can restart the numbering at
        /// <c>-Opt01</c> and overwrite a previous optimisation's evidence.
        /// </para>
        /// <para>
        /// <b>The paragraph above is unaffected by that.</b> Nothing reads ISOLATION back out of the name,
        /// and the <c>.sam</c> remains the authority for what a run was; the lock is about the round number,
        /// not about the scope token. The two facts simply live in the same string.
        /// </para>
        /// <para>
        /// A different name is set where the name actually comes from - the model, through Edit - Properties
        /// - before the iteration is prepared, rather than typed over the derived one afterwards. The
        /// ordinary <c>Simulate - Energy Simulation</c> command still opens this dialog unlocked, because
        /// nothing on that path derives an identity from the name.
        /// </para>
        /// </summary>
        /// <param name="projectName">The name the model already carries.</param>
        /// <param name="scopeToken">The isolation scope's token.</param>
        /// <returns>The isolated project name, or the original where there is no token to add.</returns>
        internal static string ProjectName_Isolated(string projectName, string scopeToken)
        {
            if (string.IsNullOrWhiteSpace(scopeToken))
            {
                return projectName;
            }

            string suffix = string.Format("-ISO-{0}", scopeToken);

            //Applied once. Re-preparing an already isolated model - which every Iteration 2B round does -
            //must not grow the name a suffix per round.
            if (!string.IsNullOrWhiteSpace(projectName) && projectName.EndsWith(suffix, System.StringComparison.Ordinal))
            {
                return projectName;
            }

            return string.Format("{0}{1}", projectName, suffix);
        }
    }
}
