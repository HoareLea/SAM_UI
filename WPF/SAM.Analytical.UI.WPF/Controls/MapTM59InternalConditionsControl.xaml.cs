// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapTM59InternalConditionsControl.xaml
    /// </summary>
    public partial class MapTM59InternalConditionsControl : UserControl
    {
        // Preferred Zone Type categories to auto-select when more than one is available and the
        // user has not chosen one yet - "Flats" is what Modify.MapInternalConditionsByTM59 assumes.
        private static readonly string[] PreferredZoneCategories = { "Flats", "Flat", "Apartments", "Dwellings", "Units" };

        private AdjacencyCluster adjacencyCluster;

        // The shared semantic classification layer, read only, used to show the user what each space is
        // recognised as before that recognition reaches Approved Document F, Approved Document O or
        // TM59 - all three read the same classification. Built once and reused because the resolver
        // caches per Space; null where the shared vocabulary resource is missing, in which case the
        // column is simply not shown rather than blocking the dialog.
        private readonly SpaceSemanticsResolver spaceSemanticsResolver = Analytical.Query.DefaultSpaceSemanticsResolver();

        // True while the control is being wired up by its owner and required inputs (in particular
        // InternalConditionLibrary) may still be legitimately null - SetMapFunc must not treat that
        // transient state as a resource failure. WPF constructs this control via the parameterless
        // ctor when it appears in XAML (MapTM59InternalConditionsWindow.xaml), then the window's own
        // ctor assigns AdjacencyCluster, TextMap, InternalConditionLibrary and Spaces as four separate
        // property-setter calls, each of which calls Load()/SetMapFunc() again - only the LAST of
        // those calls (once FinishInitialization runs) reflects the fully-assigned state, so every
        // call before that must be a safe no-op rather than a false failure report.
        private bool initializing = true;

        // Tracks whether the *previous* real check failed, so a genuinely new failure (one that
        // follows a period of success) is always reported - this is not a permanent one-shot latch;
        // it only suppresses repeat notifications for the same ongoing failure (e.g. every Zone Type
        // change while the library is still invalid).
        private bool lastCheckFailed = false;

        public MapTM59InternalConditionsControl()
        {
            InitializeComponent();

            mapInternalConditionsControl.AutoMapOnLoad = true;
            mapInternalConditionsControl.MapSourceChanged += MapInternalConditionsControl_MapSourceChanged;

            LoadZones();
            SetMapFunc();
        }

        public MapTM59InternalConditionsControl(IEnumerable<Space> spaces, AdjacencyCluster adjacencyCluster, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            mapInternalConditionsControl.AutoMapOnLoad = true;
            mapInternalConditionsControl.MapSourceChanged += MapInternalConditionsControl_MapSourceChanged;

            this.adjacencyCluster = adjacencyCluster;

            mapInternalConditionsControl.TextMap = textMap;
            mapInternalConditionsControl.InternalConditionLibrary = internalConditionLibrary;

            // FinishInitialization must run BEFORE Spaces is assigned - see the matching ordering (and
            // the reason for it) in MapTM59InternalConditionsWindow's constructor. Without this, rows
            // would be built against the null-returning "not ready yet" placeholder MapFunc and never
            // get TM59-preselected.
            FinishInitialization();

            mapInternalConditionsControl.Spaces = spaces?.ToList();
        }

        /// <summary>
        /// Called by the owning window once every required input has been assigned. Before this runs,
        /// SetMapFunc treats a missing resource as "not ready yet" rather than "failed" - see the
        /// `initializing` field. Safe to call more than once; only the first call matters.
        /// </summary>
        public void FinishInitialization()
        {
            initializing = false;
            LoadZones();
            SetMapFunc();
        }

        private void MapInternalConditionsControl_MapSourceChanged(object sender, EventArgs e)
        {
            SetMapFunc();
        }

        private void Load()
        {
            LoadZones();
            SetMapFunc();
        }

        private void LoadZones()
        {
            string value = comboBox_ZoneType.Text;

            comboBox_ZoneType.Items.Clear();

            List<Zone> zones = adjacencyCluster?.GetZones();
            if(zones == null || zones.Count == 0)
            {
                return;
            }

            HashSet<string> categories = new HashSet<string>();
            foreach(Zone zone in zones)
            {
                if(zone.TryGetValue(ZoneParameter.ZoneCategory, out string category) && !string.IsNullOrWhiteSpace(category))
                {
                    categories.Add(category);
                }
            }

            foreach(string category in categories)
            {
                comboBox_ZoneType.Items.Add(category);
            }

            if(!string.IsNullOrWhiteSpace(value))
            {
                comboBox_ZoneType.Text = value;
                return;
            }

            // No prior selection to restore - pick a sensible default so preselection (AutoMapOnLoad)
            // has a Zone Type to work with as soon as the dialog opens, instead of requiring the user
            // to pick one first.
            string defaultCategory = SelectDefaultZoneCategory(categories);
            if (defaultCategory != null)
            {
                comboBox_ZoneType.SelectedItem = defaultCategory;
            }
        }

        private static string SelectDefaultZoneCategory(HashSet<string> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return null;
            }

            if (categories.Count == 1)
            {
                return categories.First();
            }

            foreach (string preferred in PreferredZoneCategories)
            {
                string match = categories.FirstOrDefault(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            // More than one category and none of them look like a residential-unit category -
            // leave unselected rather than guessing; the user must pick one.
            return null;
        }

        public TextMap TextMap
        {
            get
            {
                return mapInternalConditionsControl.TextMap;
            }

            set
            {
                // MapSourceChanged only fires for a user's file-browse ComboBox selection, not this
                // property assignment - RemapAutomatic must be called explicitly here too, or a host
                // assigning TextMap programmatically (after Spaces has already populated the control)
                // would leave automatic rows showing/returning mappings from the previous TextMap.
                // A no-op before Spaces is assigned, since there are no rows yet to rebuild.
                mapInternalConditionsControl.TextMap = value;
                SetMapFunc();
                mapInternalConditionsControl.RemapAutomatic();
            }
        }

        public InternalConditionLibrary InternalConditionLibrary
        {
            get
            {
                return mapInternalConditionsControl.InternalConditionLibrary;
            }

            set
            {
                // Same reasoning as the TextMap setter above.
                mapInternalConditionsControl.InternalConditionLibrary = value;
                SetMapFunc();
                mapInternalConditionsControl.RemapAutomatic();
            }
        }

        /// <summary>Forwards the inner control's MappingChanged - see its doc comment for when this fires.</summary>
        public event EventHandler MappingChanged
        {
            add { mapInternalConditionsControl.MappingChanged += value; }
            remove { mapInternalConditionsControl.MappingChanged -= value; }
        }

        public List<Space> Spaces
        {
            get
            {
                return mapInternalConditionsControl.Spaces;
            }

            set
            {
                mapInternalConditionsControl.Spaces = value;
            }
        }

        public AdjacencyCluster AdjacencyCluster
        {
            get
            {
                return adjacencyCluster;
            }

            set
            {
                SetAdjacencyCluster(value);
            }
        }

        private void SetAdjacencyCluster(AdjacencyCluster adjacencyCluster)
        {
            this.adjacencyCluster = adjacencyCluster;
            Load();
        }

        private void comboBox_ZoneType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SetMapFunc must run BEFORE GroupFunc is assigned: the GroupFunc setter immediately
            // triggers a row rebuild (SetSpaces), which would otherwise preselect using the previous
            // MapFunc's stale Zone Type closure.
            SetMapFunc();

            mapInternalConditionsControl.GroupFunc = new Func<Space, string>(x =>
            {
                Zone zone = adjacencyCluster?.GetZones(x, comboBox_ZoneType.SelectedItem as string)?.FirstOrDefault();
                if(zone == null)
                {
                    return null;
                }

                return zone.Name;
            });
        }

        public void SetMapFunc()
        {
            if (initializing)
            {
                // Owner is still wiring the control up (see the `initializing` field) - a missing
                // library/text map here is expected, not a failure. Keep spaces mappable-looking
                // (Assign stays enabled) without guessing at a result; FinishInitialization will
                // run the real check once every input is actually in place.
                mapInternalConditionsControl.MapFunc = new Func<Space, InternalCondition>(x => null);
                mapInternalConditionsControl.MapDiagnosticFunc = null;
                return;
            }

            TextMap textMap = TextMap;
            InternalConditionLibrary internalConditionLibrary = InternalConditionLibrary;
            string zoneType = comboBox_ZoneType.SelectedItem as string;

            SetSharedClassificationFuncs(zoneType);

            // TM59Manager's TextMap ctor falls back to the TM59 default resource when textMap is null,
            // so a missing selection here does not silently produce an inert (always-blank) manager.
            TM59Manager tM59Manager = new TM59Manager(textMap);

            bool usable = ResourcesAreUsable(tM59Manager, internalConditionLibrary);
            mapInternalConditionsControl.AssignEnabled = usable;

            if (!usable)
            {
                ReportResourceFailure();

                mapInternalConditionsControl.MapFunc = new Func<Space, InternalCondition>(x => null);
                mapInternalConditionsControl.MapDiagnosticFunc = null;
                return;
            }

            lastCheckFailed = false;

            mapInternalConditionsControl.MapFunc = new Func<Space, InternalCondition>(x =>
            {
                return tM59Manager.GetInternalConditionResult(adjacencyCluster, internalConditionLibrary, x, zoneType)?.InternalCondition;
            });

            mapInternalConditionsControl.MapDiagnosticFunc = new Func<Space, string>(x =>
            {
                return tM59Manager.GetInternalConditionResult(adjacencyCluster, internalConditionLibrary, x, zoneType)?.Diagnostic;
            });
        }

        /// <summary>
        /// Points the inner control at the shared semantic classification and the zone dwelling flag, so
        /// each row shows what the space is, how that was decided, and whether its zone is a dwelling.
        /// <para>
        /// Display only. This performs no regulatory calculation - Approved Document F and Approved
        /// Document O rules stay in the SAM analytical layer and are not duplicated here.
        /// </para>
        /// </summary>
        private void SetSharedClassificationFuncs(string zoneType)
        {
            if (spaceSemanticsResolver == null)
            {
                mapInternalConditionsControl.SpaceSemanticsFunc = null;
                mapInternalConditionsControl.IsDwellingFunc = null;
                return;
            }

            mapInternalConditionsControl.SpaceSemanticsFunc = new Func<Space, SpaceSemantics>(x => spaceSemanticsResolver.Resolve(x));

            mapInternalConditionsControl.IsDwellingFunc = new Func<Space, bool?>(x =>
            {
                Zone zone = adjacencyCluster?.GetZones(x, zoneType)?.FirstOrDefault();
                if (zone == null)
                {
                    return null;
                }

                //TryGetValue, not GetValue: a zone that has never had the parameter set must read back as
                //null, not as false. The two mean different things to a zoned Part F calculation.
                return zone.TryGetValue(ZoneParameter.IsDwelling, out bool isDwelling) ? isDwelling : (bool?)null;
            });
        }

        private static bool ResourcesAreUsable(TM59Manager tM59Manager, InternalConditionLibrary internalConditionLibrary)
        {
            if (tM59Manager == null || internalConditionLibrary == null)
            {
                return false;
            }

            List<InternalCondition> internalConditions = internalConditionLibrary.GetInternalConditions();
            return internalConditions != null && internalConditions.Count != 0;
        }

        /// <summary>
        /// Reports a genuine resource failure once per failure episode - not a permanent one-shot
        /// latch: if the control later recovers (a valid library/text map gets selected) and then
        /// fails again, that new failure is reported too. Only repeat notifications for the SAME
        /// ongoing failure (e.g. every Zone Type change while still invalid) are suppressed.
        /// </summary>
        private void ReportResourceFailure()
        {
            if (lastCheckFailed)
            {
                return;
            }

            lastCheckFailed = true;
            System.Windows.MessageBox.Show(
                "The TM59 InternalCondition TextMap or InternalConditionLibrary could not be loaded, " +
                "so spaces cannot be mapped automatically. Select a valid Text Map and InternalCondition " +
                "Library above, or assign conditions manually.",
                "TM59 - Map Internal Conditions");
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            return mapInternalConditionsControl.GetSpaces(selected);
        }
    }
}
