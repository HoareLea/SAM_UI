// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// Everything <c>SAM.Analytical.Modify.PreparePartOIteration</c> was given for a run, kept so the
    /// <b>same</b> preparation can be repeated over a changed design without asking the user again.
    /// <para>
    /// <b>Why an optimisation needs this at all.</b> Iteration 2B changes design airflow and then has to
    /// rebuild the Part O analytical state around it - the transfer air movements, the network, the unit
    /// duties - by re-preparing. Re-preparing with a different route, a different dwelling scope or a
    /// different catalogue would make each iteration a different engineering case, and the TM59 results
    /// across the run would no longer be comparable. This is how "the same case throughout" is enforced
    /// rather than hoped for.
    /// </para>
    /// <para>
    /// <b>Not the design.</b> Nothing here is an airflow, a requirement or a capacity - it is the set of
    /// choices a person made in the preparation dialog. The design the preparation runs over is whatever
    /// model it is handed, which is exactly what an optimisation round changes between iterations.
    /// </para>
    /// <para>
    /// <b>The catalogue is carried, never re-read.</b> An optimisation must be checked against the products
    /// the run actually started with; re-reading the catalogue mid-run could change what the selected unit
    /// is understood to be rated at, halfway through.
    /// </para>
    /// </summary>
    public class PartOPreparationContext
    {
        /// <param name="partOIteration">The base provision the run is defined over.</param>
        /// <param name="zones">The dwelling zones in scope, as <c>Query.PartFDwellingZones</c> returned them.</param>
        /// <param name="dictionary_VentilationStrategy">The canonical ventilation route stated per zone.</param>
        /// <param name="ventilationUnitCapacityDescriptors">
        /// The products offered to selection, or null for a run with no equipment selection - the same
        /// distinction the preparation itself reads, where null means Iteration 1a and an empty list means a
        /// catalogue that offers nothing.
        /// </param>
        public PartOPreparationContext(PartOIteration partOIteration, IEnumerable<Zone> zones, Dictionary<Guid, string> dictionary_VentilationStrategy, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            PartOIteration = partOIteration;

            foreach (Zone zone in zones ?? [])
            {
                if (zone is not null)
                {
                    Zones.Add(zone);
                }
            }

            foreach (KeyValuePair<Guid, string> keyValuePair in dictionary_VentilationStrategy ?? [])
            {
                VentilationStrategies[keyValuePair.Key] = keyValuePair.Value;
            }

            if (ventilationUnitCapacityDescriptors is not null)
            {
                VentilationUnitCapacityDescriptors = [];

                foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in ventilationUnitCapacityDescriptors)
                {
                    if (ventilationUnitCapacityDescriptor is not null)
                    {
                        VentilationUnitCapacityDescriptors.Add(ventilationUnitCapacityDescriptor);
                    }
                }
            }
        }

        /// <summary>The base provision - Iteration 1a or 1b - the run is defined over.</summary>
        public PartOIteration PartOIteration { get; }

        /// <summary>The dwelling zones in scope.</summary>
        public List<Zone> Zones { get; } = [];

        /// <summary>The canonical ventilation route stated for each of them.</summary>
        public Dictionary<Guid, string> VentilationStrategies { get; } = [];

        /// <summary>
        /// The products offered to selection. <b>Null means no equipment selection ran</b> - which is
        /// Iteration 1a, and is not an Iteration 2B starting point.
        /// </summary>
        public List<VentilationUnitCapacityDescriptor> VentilationUnitCapacityDescriptors { get; }

        /// <summary>Whether a product catalogue was offered at all - the Iteration 2 precondition for 2B.</summary>
        public bool HasVentilationUnitCatalogue => VentilationUnitCapacityDescriptors is not null && VentilationUnitCapacityDescriptors.Count != 0;

        /// <summary>
        /// Whether this run was offered a product catalogue - the Iteration 1a / Iteration 2 distinction - for
        /// NAMING the run. Null where it is not known.
        /// <para>
        /// A live preparation answers from <see cref="HasVentilationUnitCatalogue"/>. A context resumed from a
        /// saved run carries no descriptors (they are a capability lookup for Iteration 2B, which a resumed run
        /// never starts), so it answers from what the saved record states - and a record saved before that was
        /// recorded cannot say, so it answers null rather than reading "no descriptors" as "Iteration 1a".
        /// </para>
        /// <para>
        /// <b>Naming only.</b> Anything that acts on the catalogue reads <see cref="HasVentilationUnitCatalogue"/>
        /// and the descriptors themselves, which a resumed context never pretends to have.
        /// </para>
        /// </summary>
        public bool? VentilationUnitCatalogueOffered => IsResumed ? ventilationUnitCatalogueOffered_Saved : HasVentilationUnitCatalogue;

        /// <summary>Whether this context was rebuilt from a saved run rather than made by a preparation in this session.</summary>
        public bool IsResumed { get; private set; }

        private bool? ventilationUnitCatalogueOffered_Saved;

        /// <summary>
        /// The context a saved run's record describes: its iteration and zones, no routes and no descriptors,
        /// and whether a catalogue was offered as the record states it (null where the record predates that).
        /// </summary>
        public static PartOPreparationContext Resumed(PartOIteration partOIteration, IEnumerable<Zone> zones, bool? ventilationUnitCatalogueOffered)
        {
            return new PartOPreparationContext(partOIteration, zones, [], null)
            {
                IsResumed = true,
                ventilationUnitCatalogueOffered_Saved = ventilationUnitCatalogueOffered,
            };
        }

        /// <summary>
        /// The Iteration 2B optimisation this run was set up to allow afterwards, or null where none was
        /// asked for.
        /// <para>
        /// <b>Not a preparation input.</b> The preparation neither reads it nor is affected by it - Iteration
        /// 2B is an optimisation performed ON an Iteration 2 design, never a base provision, and it is
        /// deliberately not one of the choices in the base-provision list. It rides here because it is a
        /// choice made at the same moment, about the same run, and has to survive until there are results to
        /// optimise from.
        /// </para>
        /// </summary>
        public PartOOptimisationSettings OptimisationSettings { get; set; }

        /// <summary>
        /// How this run's equipment was chosen, and which products it was allowed to be chosen from.
        ///
        /// <para><b>Deliberately NOT the same list as <see cref="VentilationUnitCapacityDescriptors"/></b></para>
        /// <para>
        /// That list is the whole selectable catalogue and is a <i>capability lookup</i>: Iteration 2B and
        /// the capacity envelope read it through
        /// <c>Analytical.Query.SelectedVentilationUnitCapacityDescriptor</c> to find what each dwelling's
        /// ALREADY SELECTED product is rated at. Narrowing it to the permitted pool would break exactly the
        /// case this feature exists to support - a dwelling manually assigned a product that has since left
        /// the pool would become "capacity unknown", and 2B would lose the ceiling it stops at.
        /// </para>
        /// <para>
        /// So the pool lives here, applied once when the preparation chose products, and the catalogue stays
        /// whole. Null reads as the historic default.
        /// </para>
        /// </summary>
        public PartOEquipmentSelection EquipmentSelection { get; set; }

        /// <summary>
        /// The project's own test ventilation unit as this run was prepared, or null where the project
        /// stated none.
        ///
        /// <para><b>Recorded so that a preparation is not reused for a different what-if</b></para>
        /// <para>
        /// Its capacity is already <i>in</i> <see cref="VentilationUnitCapacityDescriptors"/> - that is how
        /// Iteration 2B resolves the ceiling of a dwelling assigned to it, and the whole reason the product
        /// persists on the project. This property records the statement itself so
        /// <c>PartOWorkflowInspection</c> can tell that a what-if re-rated from 165 to 175 l/s is a
        /// different what-if: reusing a preparation made under the old rating would simulate the old
        /// ceiling while the dialog reported the new one.
        /// </para>
        /// <para>
        /// It is <b>not</b> an equipment selection and does not belong inside
        /// <see cref="EquipmentSelection"/>, which holds identities and never a capacity.
        /// </para>
        /// </summary>
        public PartOProjectTestVentilationUnit ProjectTestVentilationUnit { get; set; }

        /// <summary>
        /// Whether this run's thermal model is the <b>isolated</b> derived model of the dwellings in scope
        /// rather than the whole building.
        /// <para>
        /// <b>Recorded, not re-applied.</b> An Iteration 2B round re-prepares the model the previous round
        /// left behind, and that model is already isolated - so a round must NOT isolate again. Doing so
        /// would rebuild the cut and the shading context on a model that already has them, and the geometry
        /// would no longer be bit-for-bit what the canonical TBD was converted from, turning the warm start
        /// off for every round. This says what the run is, so a report can state its scope; the isolated
        /// geometry itself is carried forward by the model.
        /// </para>
        /// <para>
        /// The authority for whether a given MODEL is isolated remains
        /// <c>AnalyticalModelParameter.PartOIsolationContext</c> stamped on the model, which is what
        /// survives into the run's <c>.sam</c> and back out of it in a later session. This is the session's
        /// record of what was asked for.
        /// </para>
        /// </summary>
        public bool Isolated { get; set; }
    }
}
