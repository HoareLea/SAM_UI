// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using UserControl = System.Windows.Controls.UserControl;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapInternalConditionsControl.xaml
    /// </summary>
    public partial class MapInternalConditionsControl : UserControl
    {
        private static string internalText = Core.UI.WPF.Query.DefaultInternalText();

        private Func<Space, InternalCondition> mapFunc = null;
        private Func<Space, string> groupFunc = null;

        private readonly HashSet<Guid> dirtySpaceGuids = new HashSet<Guid>();
        private bool suppressDirtyTracking = false;
        private bool internalConditionLibrarySelectionChangedSubscribed = false;
        private bool textMapSelectionChangedSubscribed = false;

        /// <summary>
        /// When true, SetSpaces preselects each row from MapFunc if it has no restored value.
        /// Default is false so the shared "Map IC" dialog keeps its existing blank-until-Assign
        /// behaviour; only the TM59 dialog opts in.
        /// </summary>
        public bool AutoMapOnLoad { get; set; } = false;

        /// <summary>
        /// Disables the Assign button - set by a caller (e.g. the TM59 dialog) when a required
        /// resource genuinely failed to load, so the user cannot trigger a mapping that can only
        /// ever produce blanks. Does not affect OK/Cancel/Select All/Select None.
        /// </summary>
        public bool AssignEnabled
        {
            get => button_Assign.IsEnabled;
            set => button_Assign.IsEnabled = value;
        }

        /// <summary>
        /// Optional: explains why MapFunc returned null (or made a noteworthy automatic choice) for a
        /// row, e.g. "4 bedrooms - TM59 defines no matching size, assign manually". Shown as the row's
        /// ComboBox tooltip when AutoMapOnLoad preselects it. Never required - a null return is fine.
        /// </summary>
        public Func<Space, string> MapDiagnosticFunc { get; set; } = null;

        /// <summary>Raised when the selected TextMap or InternalConditionLibrary changes (via file browse).</summary>
        public event EventHandler MapSourceChanged;

        /// <summary>
        /// Optional: the shared semantic classification of a space - what the space is, independently of
        /// which standard is assessing it. When set, each row shows the classification and how it was
        /// reached, so the user can see and correct a bad room recognition before it reaches Approved
        /// Document F, Approved Document O or CIBSE TM59, all of which read the same classification.
        /// <para>
        /// Display only. No regulatory calculation is performed here - that stays in the SAM analytical
        /// layer.
        /// </para>
        /// </summary>
        public Func<Space, SpaceSemantics> SpaceSemanticsFunc { get; set; } = null;

        /// <summary>
        /// Optional: whether the zone containing a space is a dwelling, per its Is Dwelling parameter.
        /// Null where the zone carries no value, which is itself worth showing - an unmarked zone is
        /// excluded from a zoned Part F calculation when other zones in its category are marked.
        /// </summary>
        public Func<Space, bool?> IsDwellingFunc { get; set; } = null;

        /// <summary>
        /// Raised whenever a row's mapped condition changes - a manual edit, Assign filling in blanks,
        /// or a remap that clears/resolves rows. Callers that show a live "N space(s) need manual
        /// review" style status should refresh it from this, not only once at construction.
        /// </summary>
        public event EventHandler MappingChanged;

        /// <summary>
        /// Clears every row's dirty (manually-edited) flag without changing what is currently
        /// displayed. Ordinary Zone Type/TextMap/InternalConditionLibrary changes deliberately do NOT
        /// call this - a manual edit is meant to survive a source change. Call it explicitly only when
        /// a source change should discard prior manual edits too (e.g. starting the mapping over).
        /// </summary>
        public void ClearDirtyState()
        {
            dirtySpaceGuids.Clear();
        }

        public MapInternalConditionsControl()
        {
            InitializeComponent();

            Load();
        }

        public MapInternalConditionsControl(IEnumerable<Space> spaces, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            selectSAMObjectComboBoxControl_InternalConditionLibrary.Add(internalText, internalConditionLibrary);
            selectSAMObjectComboBoxControl_TextMap.Add(internalText, textMap);

            SetSpaces(spaces);

            Load();
        }

        private void Load()
        {
            selectSAMObjectComboBoxControl_InternalConditionLibrary.SelectedText = internalText;
            selectSAMObjectComboBoxControl_InternalConditionLibrary.ValidateFunc = new Func<IJSAMObject, bool>(x => x is InternalConditionLibrary);

            selectSAMObjectComboBoxControl_TextMap.SelectedText = internalText;
            selectSAMObjectComboBoxControl_TextMap.ValidateFunc = new Func<IJSAMObject, bool>(x => x is TextMap);

            if (!textMapSelectionChangedSubscribed)
            {
                selectSAMObjectComboBoxControl_TextMap.SelectionChanged += SelectSAMObjectComboBoxControl_TextMap_SelectionChanged;
                textMapSelectionChangedSubscribed = true;
            }
        }

        private void SelectSAMObjectComboBoxControl_TextMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Raise BEFORE rebuilding rows: a TM59 listener's SetMapFunc must rebuild its resolver
            // against the newly-selected TextMap first, or RemapAutomatic below would re-preselect
            // every automatic row using the now-stale MapFunc closure.
            MapSourceChanged?.Invoke(this, EventArgs.Empty);
            RemapAutomatic();
        }

        /// <summary>
        /// Rebuilds every row from the current MapFunc, clearing (and so recalculating) automatic
        /// values while preserving manual (dirty) ones that are still valid - see SetSpaces. Call
        /// after a genuine source change (Zone Type, TextMap, InternalConditionLibrary) once MapFunc
        /// itself has already been updated to reflect that change.
        /// </summary>
        public void RemapAutomatic()
        {
            SetSpaces(GetSpaces());
        }

        public Func<Space, InternalCondition> MapFunc
        {
            get
            {
                return mapFunc;
            }

            set
            {
                mapFunc = value;
            }
        }

        public Func<Space, string> GroupFunc
        {
            get
            {
                return groupFunc;
            }

            set
            {
                groupFunc = value;
                SetSpaces(Spaces);
            }
        }

        public TextMap TextMap
        {
            get
            {
                // Returns the currently SELECTED item (not always the <Internal> default) so a
                // user-loaded override is honoured by callers such as MapTM59InternalConditionsControl.SetMapFunc.
                return selectSAMObjectComboBoxControl_TextMap.GetJSAMObject<TextMap>();
            }

            set
            {
                SetTextMap(value);
            }
        }

        public InternalConditionLibrary InternalConditionLibrary
        {
            get
            {
                // Returns the currently SELECTED item (not always the <Internal> default) - see TextMap getter.
                return selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();
            }

            set
            {
                SetInternalConditionLibrary(value);
            }
        }

        public List<Space> Spaces
        {
            get
            {
                return GetSpaces();
            }

            set
            {
                SetSpaces(value);
            }
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            if(grid == null || grid.Children == null || grid.Children.Count == 0)
            {
                return null;
            }

            InternalConditionLibrary internalConditionLibrary = selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();

            Dictionary<Space, ComboBox> dictionary = new Dictionary<Space, ComboBox>();
            foreach (UIElement uIElement in grid.Children)
            {
                int rowIndex = Grid.GetRow(uIElement);
                if (rowIndex == -1)
                {
                    continue;
                }

                if (!(grid.RowDefinitions[rowIndex].Tag is Space))
                {
                    continue;
                }

                if (!(uIElement is ComboBox))
                {
                    continue;
                }

                dictionary[(Space)grid.RowDefinitions[rowIndex].Tag] = uIElement as ComboBox;
            }

            if (selected)
            {
                foreach (UIElement uIElement in grid.Children)
                {
                    int rowIndex = Grid.GetRow(uIElement);
                    if (rowIndex == -1)
                    {
                        continue;
                    }

                    if (!(grid.RowDefinitions[rowIndex].Tag is Space))
                    {
                        continue;
                    }

                    CheckBox checkBox = uIElement as CheckBox;
                    if (checkBox == null)
                    {
                        continue;
                    }

                    if (!(uIElement is CheckBox))
                    {
                        continue;
                    }

                    if (!checkBox.IsChecked.Value)
                    {
                        dictionary.Remove((Space)grid.RowDefinitions[rowIndex].Tag);
                    }
                }
            }

            List<Space> result = new List<Space>();
            foreach(KeyValuePair<Space, ComboBox> keyValuePair in dictionary)
            {
                InternalCondition internalCondition = null;

                string internalConditionName = keyValuePair.Value.Text;
                if (!string.IsNullOrWhiteSpace(internalConditionName))
                {
                    internalCondition = internalConditionLibrary?.GetInternalConditions(internalConditionName)?.FirstOrDefault();
                }

                Space space = new Space(keyValuePair.Key);

                if (internalCondition != null)
                {
                    space.InternalCondition = internalCondition;
                    space.UpdateAreaPerPerson();
                }
                else
                {
                    // A row left blank (no resolved condition, e.g. AutoMapOnLoad found no defensible
                    // automatic mapping) must not silently keep whatever InternalCondition the
                    // underlying Space previously had - the returned Space has to reflect exactly what
                    // is shown, not stale state from before a remap invalidated it.
                    space.InternalCondition = null;
                }

                result.Add(space);
            }

            return result;


            //return null;

            //if (wrapPanel.Children == null || wrapPanel.Children.Count == 0)
            //{
            //    return null;
            //}

            //InternalConditionLibrary internalConditionLibrary = selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();

            //List<Space> result = new List<Space>();
            //foreach(DockPanel dockPanel in wrapPanel.Children)
            //{
            //    if(dockPanel == null)
            //    {
            //        continue;
            //    }

            //    if (selected)
            //    {
            //        if(dockPanel.Children.Count == 0)
            //        {
            //            continue;
            //        }

            //        CheckBox checkBox = dockPanel.Children[0] as CheckBox;
            //        if (checkBox == null)
            //        {
            //            continue;
            //        }

            //        if (!checkBox.IsChecked.Value)
            //        {
            //            continue;
            //        }
            //    }

            //    Space space = dockPanel.Tag as Space;
            //    if(space == null)
            //    {
            //        continue;
            //    }

            //    InternalCondition internalCondition = null;

            //    string internalConditionName = (dockPanel.Children[1] as ComboBox).Text;
            //    if(!string.IsNullOrWhiteSpace(internalConditionName))
            //    {
            //        internalCondition = internalConditionLibrary?.GetInternalConditions(internalConditionName)?.FirstOrDefault();
            //    }

            //    space = new Space(space);

            //    if(internalCondition != null)
            //    {
            //        space.InternalCondition = internalCondition;
            //        space.UpdateAreaPerPerson();
            //    }

            //    result.Add(space);
            //}

            //return result;
        }

        private List<Tuple<Space, string>> GetTuples()
        {
            List<Tuple<Space, string>> result = new List<Tuple<Space, string>>();
            foreach (UIElement uIElement in grid.Children)
            {
                int rowIndex = Grid.GetRow(uIElement);
                if (rowIndex == -1)
                {
                    continue;
                }

                if (!(grid.RowDefinitions[rowIndex].Tag is Space))
                {
                    continue;
                }

                if (!(uIElement is ComboBox))
                {
                    continue;
                }

                string internalConditionName = (uIElement as ComboBox).Text;
                if (string.IsNullOrWhiteSpace(internalConditionName))
                {
                    internalConditionName = null;
                }

                result.Add(new Tuple<Space, string>((Space)grid.RowDefinitions[rowIndex].Tag, internalConditionName));
            }

            return result;

            //if (wrapPanel.Children == null || wrapPanel.Children.Count == 0)
            //{
            //    return null;
            //}

            //List<Tuple<Space, string>> result = new List<Tuple<Space, string>>();
            //foreach (DockPanel dockPanel in wrapPanel.Children)
            //{
            //    Space space = dockPanel.Tag as Space;
            //    if (space == null)
            //    {
            //        continue;
            //    }

            //    string internalConditionName = (dockPanel.Children[1] as ComboBox).Text;
            //    if(string.IsNullOrWhiteSpace(internalConditionName))
            //    {
            //        internalConditionName = null;
            //    }

            //    result.Add(new Tuple<Space, string>(space, internalConditionName));
            //}

            //return result;
        }

        /// <summary>
        /// Captures each row's current CheckBox.IsChecked state, keyed by Space.Guid, so a rebuild
        /// (SetSpaces, via RemapAutomatic) can restore it - without this, every rebuilt row defaults
        /// back to checked, silently re-including any space the user had excluded beforehand.
        /// </summary>
        private Dictionary<Guid, bool> GetCheckedStateByGuid()
        {
            Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();

            if (grid == null || grid.Children == null)
            {
                return result;
            }

            foreach (UIElement uIElement in grid.Children)
            {
                int rowIndex = Grid.GetRow(uIElement);
                if (rowIndex == -1)
                {
                    continue;
                }

                if (!(grid.RowDefinitions[rowIndex].Tag is Space space))
                {
                    continue;
                }

                if (uIElement is CheckBox checkBox)
                {
                    result[space.Guid] = checkBox.IsChecked == true;
                }
            }

            return result;
        }

        private void SetSpaces(IEnumerable<Space> spaces)
        {
            List<Tuple<Space, string>> tuples = GetTuples();
            Dictionary<Guid, bool> checkedStateByGuid = GetCheckedStateByGuid();

            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            if(spaces == null || spaces.Count() == 0)
            {
                MappingChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            InternalConditionLibrary internalConditionLibrary = selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();

            HashSet<string> hashSet = new HashSet<string>();
            hashSet.Add(string.Empty);
            if(internalConditionLibrary != null)
            {
                List<InternalCondition> internalConditons = internalConditionLibrary.GetInternalConditions();
                if(internalConditons != null && internalConditons.Count != 0)
                {
                    foreach(InternalCondition internalCondition in internalConditons)
                    {
                        string name = internalCondition?.Name;
                        if(string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        hashSet.Add(name);
                    }
                }
            }

            Dictionary<string, List<Space>> dictionary = new Dictionary<string, List<Space>>();
            if(groupFunc == null)
            {
                dictionary[string.Empty] = spaces.ToList();
            }
            else
            {
                List<Tuple<Space, string>> tuples_Group = spaces.ToList().ConvertAll(x => new Tuple<Space, string>(x, groupFunc.Invoke(x)));
                foreach(Space space in spaces)
                {
                    if(space == null)
                    {
                        continue;
                    }

                    string group = groupFunc.Invoke(space);
                    if(group == null)
                    {
                        group = string.Empty;
                    }

                    if (!dictionary.TryGetValue(group, out List<Space> spaces_Group) || spaces_Group == null)
                    {
                        spaces_Group = new List<Space>();
                        dictionary[group] = spaces_Group;
                    }

                    spaces_Group.Add(space);
                }
            }

            List<string> keys = dictionary.Keys.ToList();
            keys.Sort();

            foreach(string key in keys)
            {
                List<Space> spaces_Group = dictionary[key];
                spaces_Group.Sort((x, y) => x.Name.CompareTo(y.Name));

                if(!string.IsNullOrEmpty(key))
                {
                    int rowIndex = grid.RowDefinitions.Count;

                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Tag = key;
                    grid.RowDefinitions.Add(rowDefinition);

                    Label label = new Label() { Height = 35, Width = 300, Content = key, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Left };

                    grid.Children.Add(label);

                    Grid.SetRow(label, rowIndex);
                    Grid.SetColumn(label, 0);
                }

                foreach (Space space in spaces_Group)
                {
                    int rowIndex = grid.RowDefinitions.Count;

                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Tag = space;
                    rowDefinition.Height = new GridLength(30);
                    grid.RowDefinitions.Add(rowDefinition);

                    // In AutoMapOnLoad (TM59) mode, a genuine source change (Zone Type, TextMap or
                    // InternalConditionLibrary) must invalidate stale AUTOMATIC values so they get
                    // recalculated below, while a deliberate MANUAL edit (dirty) is restored as long
                    // as it is still a valid condition in the (possibly new) library. The general
                    // "Map IC" dialog (AutoMapOnLoad == false) has no such distinction and keeps its
                    // existing behaviour of restoring every prior value unconditionally.
                    bool isDirty = dirtySpaceGuids.Contains(space.Guid);
                    bool restoreFromTuples = !AutoMapOnLoad || isDirty;

                    string internalConditionName = string.Empty;
                    if (tuples != null && restoreFromTuples)
                    {
                        int index = tuples.FindIndex(x => x.Item1.Guid == space.Guid);
                        if (index != -1)
                        {
                            string previousValue = tuples[index].Item2;

                            // GetTuples() normalizes a deliberately-cleared row's blank ComboBox.Text to
                            // null - that is always restorable as blank (never invalid), unlike a real
                            // condition name, which is only restored if still valid in the (possibly
                            // changed) library. Without this, a manually-cleared dirty row was
                            // indistinguishable below from a never-touched blank one, and the
                            // auto-preselect block a few lines down would silently overwrite the user's
                            // deliberate clear with a fresh automatic guess.
                            if (previousValue == null || hashSet.Contains(previousValue))
                            {
                                internalConditionName = previousValue ?? string.Empty;
                            }
                        }
                    }

                    // Preserve a prior exclusion (Select None / individually unchecked) across a rebuild;
                    // a space not seen before (not in checkedStateByGuid) defaults to checked, as before.
                    bool isChecked = !checkedStateByGuid.TryGetValue(space.Guid, out bool wasChecked) || wasChecked;
                    CheckBox checkBox = new CheckBox() { MinWidth = 100, Content = space.Name, IsChecked = isChecked, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };

                    ComboBox comboBox = new ComboBox() { MinWidth = 220, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
                    foreach (string internalConditionName_Temp in hashSet)
                    {
                        comboBox.Items.Add(internalConditionName_Temp);
                    }

                    suppressDirtyTracking = true;
                    try
                    {
                        comboBox.Text = internalConditionName;

                        // !isDirty here (not just the blank check) is what actually protects a
                        // deliberately-cleared dirty row - it stays blank exactly because it is dirty,
                        // not because its blank happens to also look like an untouched row's blank.
                        if (AutoMapOnLoad && mapFunc != null && !isDirty && string.IsNullOrEmpty(internalConditionName))
                        {
                            InternalCondition proposed = mapFunc.Invoke(space);
                            if (proposed?.Name != null && hashSet.Contains(proposed.Name))
                            {
                                comboBox.Text = proposed.Name;
                            }

                            string diagnostic = MapDiagnosticFunc?.Invoke(space);
                            if (!string.IsNullOrWhiteSpace(diagnostic))
                            {
                                comboBox.ToolTip = diagnostic;
                            }
                        }
                    }
                    finally
                    {
                        suppressDirtyTracking = false;
                    }

                    comboBox.SelectionChanged += ComboBox_SelectionChanged;

                    grid.Children.Add(checkBox);
                    grid.Children.Add(comboBox);

                    Grid.SetRow(checkBox, rowIndex);
                    Grid.SetColumn(checkBox, 0);

                    Grid.SetRow(comboBox, rowIndex);
                    Grid.SetColumn(comboBox, 1);

                    // The semantics cell must reflect what this row currently PROPOSES, not what the
                    // space's InternalCondition still is - auto-map (just above) and a manual combo edit
                    // both only change comboBox.Text, they never mutate the underlying Space. Reading
                    // SpaceSemanticsFunc off the untouched space left the column and its conflict tooltip
                    // describing the space's previous mapping (or none at all) even when the row visibly
                    // shows a different, proposed one. ComboBox_SelectionChanged keeps this in sync for a
                    // later manual edit, once the row exists.
                    TextBlock textBlock = SpaceSemanticsTextBlock(BuildSpaceForSemantics(space, comboBox.Text));
                    if (textBlock != null)
                    {
                        grid.Children.Add(textBlock);

                        Grid.SetRow(textBlock, rowIndex);
                        Grid.SetColumn(textBlock, 2);
                    }
                }

            }

            // Each row's ComboBox.SelectionChanged handler (see below) is only attached AFTER that
            // row's own text/auto-preselect assignment above, so it never fires for the rebuild itself
            // - only for later interactive edits. A caller tracking "N space(s) need manual review"
            // needs to see the rebuilt state (e.g. after RemapAutomatic on a Zone Type/TextMap/Library
            // change) regardless, so this fires once, unconditionally, after every rebuild completes.
            MappingChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Builds the shared-classification cell for one row: what the space is, and - in the tooltip -
        /// how that was decided, which standards read it, whether the zone is a dwelling, and any
        /// warning. Returns null when no SpaceSemanticsFunc is set, so the general "Map IC" dialog keeps
        /// its original two-column layout untouched.
        /// </summary>
        private TextBlock SpaceSemanticsTextBlock(Space space)
        {
            if (SpaceSemanticsFunc == null)
            {
                return null;
            }

            SpaceSemantics spaceSemantics = SpaceSemanticsFunc.Invoke(space);

            bool unresolved = spaceSemantics == null || spaceSemantics.SpaceUse == SpaceUse.Undefined;
            bool conflict = spaceSemantics != null && spaceSemantics.HasSourceConflict;
            bool overridden = spaceSemantics != null && spaceSemantics.Source == SpaceSemanticsSource.UserOverride;

            //The four states the user has to be able to tell apart at a glance: unresolved (must act),
            //conflict (should check), explicit override (deliberate, not automatic), and ordinary
            //automatic mapping. Suffixes carry the state as text too, so it is not colour-only.
            string suffix = string.Empty;
            if (unresolved)
            {
                suffix = string.Empty;
            }
            else if (overridden)
            {
                suffix = "  (override)";
            }
            else if (conflict)
            {
                suffix = "  (conflict)";
            }

            TextBlock result = new TextBlock()
            {
                Text = (unresolved ? "Unclassified" : Core.Query.Description(spaceSemantics.SpaceUse)) + suffix,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                //Unresolved and conflicting rows are the ones the user has to act on, so they are the
                //ones that are coloured. An ordinary automatic mapping stays neutral rather than turning
                //the column into a traffic light the eye has to filter.
                Foreground = unresolved ? Brushes.Firebrick : conflict ? Brushes.DarkOrange : overridden ? Brushes.SteelBlue : Brushes.DimGray,
                FontStyle = unresolved ? FontStyles.Italic : FontStyles.Normal,
                FontWeight = conflict || overridden ? FontWeights.SemiBold : FontWeights.Normal,
            };

            List<string> lines = new List<string>
            {
                "Space: " + space.Name,
            };

            if (space.InternalCondition?.Name != null)
            {
                lines.Add("Internal condition: " + space.InternalCondition.Name);
            }

            if (unresolved)
            {
                lines.Add(string.Empty);
                lines.Add("No shared classification could be established. Rename the space, add a synonym to the space use text map, or set a Space Use Override.");

                if (!string.IsNullOrWhiteSpace(spaceSemantics?.Diagnostic))
                {
                    lines.Add(string.Empty);
                    lines.Add(spaceSemantics.Diagnostic);
                }
            }
            else
            {
                lines.Add("Classification: " + Core.Query.Description(spaceSemantics.SpaceUse));
                lines.Add("Matched by: " + Core.Query.Description(spaceSemantics.Source) + (overridden ? " (explicit override, not automatic)" : " (automatic)"));

                if (!string.IsNullOrWhiteSpace(spaceSemantics.MatchedAlias))
                {
                    lines.Add("Matched on: " + spaceSemantics.MatchedAlias);
                }

                //Both source values, always - so neither is hidden by the one that won.
                lines.Add(string.Empty);
                lines.Add("From space name        : " + (spaceSemantics.SpaceUse_Name == SpaceUse.Undefined ? "(nothing)" : Core.Query.Description(spaceSemantics.SpaceUse_Name)));
                lines.Add("From internal condition: " + (spaceSemantics.SpaceUse_InternalCondition == SpaceUse.Undefined ? "(nothing)" : Core.Query.Description(spaceSemantics.SpaceUse_InternalCondition)));

                //A conflict's specific diagnostic (which value came from where, and why the name won)
                //is already appended below, unconditionally, whenever Diagnostic is set - which the
                //resolver does for every conflict. A second, generic "these two sources disagree" line
                //here said the same thing twice; show the one concise diagnostic only.

                lines.Add(string.Empty);
                lines.Add("May be consumed by: Approved Document F (in use), and available to Approved Document O and CIBSE TM59.");
                lines.Add("Roles: " + Roles(spaceSemantics));

                if (!string.IsNullOrWhiteSpace(spaceSemantics.Diagnostic))
                {
                    lines.Add(string.Empty);
                    lines.Add(spaceSemantics.Diagnostic);
                }
            }

            bool? isDwelling = IsDwellingFunc?.Invoke(space);
            if (IsDwellingFunc != null)
            {
                lines.Add(string.Empty);
                lines.Add("Zone is a dwelling: " + DescribeDwellingStatus(isDwelling));
            }

            //A plain string ToolTip sizes itself to its longest line with no wrapping - the conflict
            //diagnostic and dwelling explanation are full sentences that would otherwise stretch the
            //tooltip far wider than the screen. Cap the width and let WPF wrap instead.
            result.ToolTip = new ToolTip
            {
                Content = new TextBlock
                {
                    Text = string.Join("\n", lines),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 420,
                },
            };

            return result;
        }

        /// <summary>
        /// Wording for the "Zone is a dwelling" tooltip line, matching
        /// PartFCalculator.SelectDwellingZones exactly: with Is Dwelling not set, the outcome depends on
        /// whether any OTHER zone in the category carries the flag - a mixed category excludes this zone,
        /// but a category where nothing is marked falls back to legacy category-only mode and includes
        /// every zone, this one included. Kept as its own method so the wording can be unit tested
        /// without a WPF thread.
        /// </summary>
        public static string DescribeDwellingStatus(bool? isDwelling)
        {
            if (!isDwelling.HasValue)
            {
                return "not set - if any zone in this category has Is Dwelling set, unmarked zones like this one are excluded from the calculation; if none do, every zone in the category is included (legacy category-only mode)";
            }

            return isDwelling.Value ? "yes" : "no";
        }

        /// <summary>The independent semantic roles that a space use carries, as a readable list.</summary>
        private static string Roles(SpaceSemantics spaceSemantics)
        {
            List<string> result = new List<string>();

            if (spaceSemantics.IsHabitable) result.Add("habitable");
            if (spaceSemantics.IsBedroomEquivalent) result.Add("counts as a bedroom");
            if (spaceSemantics.IsLivingSpace) result.Add("living");
            if (spaceSemantics.IsCookingSpace) result.Add("cooking");
            if (spaceSemantics.IsWetRoom) result.Add("wet room");
            if (spaceSemantics.IsCirculation) result.Add("circulation");
            if (spaceSemantics.IsCommunal) result.Add("communal");
            if (!spaceSemantics.IsDwellingSpace) result.Add("not part of a dwelling");
            if (spaceSemantics.HasSupplyRole) result.Add("supply terminal");
            if (spaceSemantics.HasExtractRole) result.Add("extract terminal");

            return result.Count == 0 ? "none" : string.Join(", ", result);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            int rowIndex = comboBox == null ? -1 : Grid.GetRow(comboBox);
            if (rowIndex == -1 || rowIndex >= grid.RowDefinitions.Count)
            {
                return;
            }

            // Dirty tracking only reflects a genuine user edit, so it stays gated behind
            // suppressDirtyTracking (set around SetSpaces'/Assign's own programmatic writes). Notifying
            // that the mapping changed is not - a caller's "unresolved space" status needs to refresh
            // regardless of whether the change was manual (a user edit) or automatic (Assign, or a
            // remap that created or resolved blanks), so this fires unconditionally.
            if (grid.RowDefinitions[rowIndex].Tag is Space space)
            {
                if (!suppressDirtyTracking)
                {
                    dirtySpaceGuids.Add(space.Guid);
                }

                // A manual edit (or a proposed value the user accepts by leaving it) changes what this
                // row is telling Part F/O/TM59 the space is, so the semantics cell and its conflict
                // tooltip must be recomputed against the newly selected text - not left describing
                // whatever was on screen when the row was first built.
                RefreshSpaceSemanticsCell(rowIndex, space, comboBox);
            }

            MappingChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// A Space carrying <paramref name="internalConditionText"/> as its InternalCondition, for
        /// resolving the semantics cell against what a row currently shows rather than what the
        /// underlying Space object still has - see the callers. Public so the null/blank/named cases
        /// can be unit tested directly, without a live grid.
        /// </summary>
        public static Space BuildSpaceForSemantics(Space space, string internalConditionText)
        {
            return new Space(space)
            {
                InternalCondition = string.IsNullOrWhiteSpace(internalConditionText) ? null : new InternalCondition(internalConditionText)
            };
        }

        /// <summary>Rebuilds the column-2 semantics cell for one row after its ComboBox selection changes.</summary>
        private void RefreshSpaceSemanticsCell(int rowIndex, Space space, ComboBox comboBox)
        {
            UIElement existing = null;
            foreach (UIElement child in grid.Children)
            {
                if (Grid.GetRow(child) == rowIndex && Grid.GetColumn(child) == 2)
                {
                    existing = child;
                    break;
                }
            }

            if (existing != null)
            {
                grid.Children.Remove(existing);
            }

            TextBlock textBlock = SpaceSemanticsTextBlock(BuildSpaceForSemantics(space, comboBox.Text));
            if (textBlock != null)
            {
                grid.Children.Add(textBlock);
                Grid.SetRow(textBlock, rowIndex);
                Grid.SetColumn(textBlock, 2);
            }
        }

        private void SetInternalConditionLibrary(InternalConditionLibrary internalConditionLibrary)
        {
            if (!internalConditionLibrarySelectionChangedSubscribed)
            {
                selectSAMObjectComboBoxControl_InternalConditionLibrary.SelectionChanged += SelectSAMObjectComboBoxControl_InternalConditionLibrary_SelectionChanged;
                internalConditionLibrarySelectionChangedSubscribed = true;
            }

            selectSAMObjectComboBoxControl_InternalConditionLibrary.Add(internalText, internalConditionLibrary);
            selectSAMObjectComboBoxControl_InternalConditionLibrary.SelectedText = internalText;
        }

        private void SelectSAMObjectComboBoxControl_InternalConditionLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Raise BEFORE rebuilding rows - see SelectSAMObjectComboBoxControl_TextMap_SelectionChanged.
            MapSourceChanged?.Invoke(this, EventArgs.Empty);
            RemapAutomatic();
        }

        private void SetTextMap(TextMap textMap)
        {
            selectSAMObjectComboBoxControl_TextMap.Add(internalText, textMap);
            selectSAMObjectComboBoxControl_TextMap.SelectedText = internalText;
        }

        private void Assign()
        {
            InternalConditionLibrary internalConditionLibrary = selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();
            if(internalConditionLibrary == null || internalConditionLibrary.GetInternalConditions() == null || internalConditionLibrary.GetInternalConditions().Count == 0)
            {
                MessageBox.Show("Internal Conditions are missing! Select different InternalConditionLibrary to map internal conditions.");

                return;
            }

            Func<Space, InternalCondition> func = mapFunc;
            if (func == null)
            {
                TextMap textMap = selectSAMObjectComboBoxControl_TextMap.GetJSAMObject<TextMap>();

                func = Query.DefaultMapFunc(internalConditionLibrary, textMap);
            }

            Dictionary<Space, ComboBox> dictionary = new Dictionary<Space, ComboBox>();
            foreach (UIElement uIElement in grid.Children)
            {
                int rowIndex = Grid.GetRow(uIElement);
                if(rowIndex == -1)
                {
                    continue;
                }

                if (!(grid.RowDefinitions[rowIndex].Tag is Space))
                {
                    continue;
                }

                if(uIElement is ComboBox)
                {
                    dictionary[(Space)grid.RowDefinitions[rowIndex].Tag] = (ComboBox)uIElement;
                }
            }

            foreach(KeyValuePair<Space, ComboBox> keyValuePair in dictionary)
            {
                Space space = keyValuePair.Key;

                // Dirty-row skipping is TM59 (AutoMapOnLoad) behaviour only: a row the user has edited
                // by hand keeps its current value, so Assign only fills in rows nobody has touched.
                // The general "Map IC" dialog (AutoMapOnLoad == false) keeps its original Assign
                // behaviour of overwriting every row regardless of prior edits. Either way, OK applies
                // whatever is currently shown (GetSpaces reads ComboBox.Text directly, unaffected).
                if (AutoMapOnLoad && dirtySpaceGuids.Contains(space.Guid))
                {
                    continue;
                }

                InternalCondition internalCondition = func.Invoke(space);
                if (internalCondition?.Name != null)
                {
                    ComboBox comboBox = keyValuePair.Value;

                    int index = -1;
                    for (int i = 0; i < comboBox.Items.Count; i++)
                    {
                        if (internalCondition.Name.Equals(comboBox.Items[i].ToString()))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index != -1)
                    {
                        suppressDirtyTracking = true;
                        try
                        {
                            comboBox.SelectedItem = comboBox.Items[index];
                        }
                        finally
                        {
                            suppressDirtyTracking = false;
                        }
                    }

                }
            }


            //foreach(DockPanel dockPanel in wrapPanel.Children)
            //{
            //    if(dockPanel == null || dockPanel.Children.Count < 1)
            //    {
            //        continue;
            //    }

            //    CheckBox checkBox = dockPanel.Children[0] as CheckBox;
            //    if(checkBox == null)
            //    {
            //        continue;
            //    }

            //    if (!checkBox.IsChecked.Value)
            //    {
            //        continue;
            //    }

            //    Space space = dockPanel.Tag as Space;
            //    if(space == null)
            //    {
            //        continue;
            //    }

            //    InternalCondition internalCondition = func.Invoke(space);
            //    if(internalCondition?.Name != null)
            //    {
            //        ComboBox comboBox = dockPanel.Children[1] as ComboBox;

            //        int index = -1;
            //        for(int i=0; i < comboBox.Items.Count; i++)
            //        {
            //            if (internalCondition.Name.Equals(comboBox.Items[i].ToString()))
            //            {
            //                index = i;
            //                break;
            //            }
            //        }

            //        if(index != -1)
            //        {
            //            comboBox.SelectedItem = comboBox.Items[index];
            //        }

            //    }
            //}
        }

        private void Button_Assign_Click(object sender, RoutedEventArgs e)
        {
            Assign();
        }

        private void button_SelectNone_Click(object sender, RoutedEventArgs e)
        {
            CheckAll(false);
        }

        private void button_SelectAll_Click(object sender, RoutedEventArgs e)
        {
            CheckAll(true);
        }

        private void CheckAll(bool isChecked)
        {
            foreach(UIElement uIElement in grid.Children)
            {
                int columnIndex = (int)uIElement.GetValue(Grid.ColumnProperty);
                
                if(columnIndex == 0 && uIElement is CheckBox)
                {
                    ((CheckBox)uIElement).IsChecked = isChecked;
                }
            }
        }
    }
}
