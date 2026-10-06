// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using SAM.Geometry.UI;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Whether a model replacement changed the <b>analytical model</b>, or only how it is drawn.
        ///
        /// <para><b>What this is for</b></para>
        /// <para>
        /// Every replacement of the loaded model raises one <c>Modified</c> event, and the session state that
        /// depends on the model being unchanged - <see cref="PartORun"/> above all - is dropped by it. That is
        /// correct for an edit, an import, an undo, a redo or a second simulation. It is <b>not</b> correct for
        /// a view change: hiding a space, isolating one, activating a saved view, editing appearances or the
        /// legend, grouping views, moving a section plane, or switching the active view all replace the model
        /// object too, because SAM stores view settings <i>on</i> the model
        /// (<c>AnalyticalModelParameter.UIGeometrySettings</c>). Nothing about a space, a panel, an aperture,
        /// an airflow, a zone or an overheating scenario moves when one of those happens.
        /// </para>
        /// <para>
        /// So a person who prepared an Approved Document O iteration and then looked at the model - which the
        /// expert workflow requires them to do, because preparing and simulating are two separate commands -
        /// lost the prepared run to the act of looking, was told the model had changed when it had not, and
        /// then ran a full-year TAS simulation that could no longer complete anything. The guided
        /// <c>Prepare &amp; Run</c> command never met it: it prepares, simulates and assesses inside one
        /// gesture, with no point at which a view can be touched.
        /// </para>
        ///
        /// <para><b>Presentation-only means exactly one type, and it is proved at every call site</b></para>
        /// <para>
        /// <c>ViewSettingsModification</c> is the modification every view-settings write announces itself
        /// with, and every one of those writes sets <c>AnalyticalModelParameter.UIGeometrySettings</c> and
        /// nothing else - <c>Modify.Hide</c>, <c>Isolate</c>, <c>RemoveOverrides</c>,
        /// <c>ActivateViewSettings</c>, <c>EditViewSettings</c>, <c>EnableViewSettings</c>,
        /// <c>EditLegend</c>, <c>SetGroup</c>, <c>CopyViewSettings</c>, <c>CopyViewSettingsCamera</c>,
        /// <c>DuplicateViewSettings</c>, <c>RemoveViewSettings</c>, <c>SetActiveGuid</c> and the
        /// section-plane range in <c>AnalyticalWindow</c>. It is deliberately <b>not</b> read off
        /// <c>IModification.Undoable</c>, which answers a different question: an appearance edit is undoable
        /// and is still presentation-only, and a camera move is not undoable and is also presentation-only.
        /// </para>
        ///
        /// <para><b>Anything else is a model change, including nothing</b></para>
        /// <para>
        /// A null or empty modification set says nothing about what moved, and a modification type added
        /// later says nothing either. Both are answered "yes, the model changed": dropping a run that did not
        /// need dropping costs a preparation, and keeping one that did would pair a preparation's overheating
        /// scenarios with a different model. The safe way to be wrong is to drop it.
        /// </para>
        /// </summary>
        /// <param name="modifications">What the replacement announced itself as.</param>
        /// <returns>Whether the replacement may have changed the analytical model.</returns>
        public static bool IsModelChange(IEnumerable<IModification> modifications)
        {
            if (modifications is null)
            {
                return true;
            }

            bool any = false;

            foreach (IModification modification in modifications)
            {
                any = true;

                if (modification is not ViewSettingsModification)
                {
                    return true;
                }
            }

            //An empty set is not a statement that nothing moved - it is the absence of one.
            return !any;
        }
    }
}
