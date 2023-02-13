using SAM.Analytical.Windows;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                return selectSAMObjectComboBoxControl_TextMap.GetJSAMObject<TextMap>(internalText);
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
                return selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>(internalText);
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

        private void SetSpaces(IEnumerable<Space> spaces)
        {
            List<Tuple<Space, string>> tuples = GetTuples();

            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            if(spaces == null || spaces.Count() == 0)
            {
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
                    grid.RowDefinitions.Add(rowDefinition);

                    string internalConditionName = string.Empty;
                    if (tuples != null)
                    {
                        int index = tuples.FindIndex(x => x.Item1.Guid == space.Guid);
                        if (index != -1 && hashSet.Contains(tuples[index].Item2))
                        {
                            internalConditionName = tuples[index].Item2;
                        }
                    }

                    CheckBox checkBox = new CheckBox() { MinWidth = 100, Content = space.Name, IsChecked = true, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };

                    ComboBox comboBox = new ComboBox() { MinWidth = 220, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
                    foreach (string internalConditionName_Temp in hashSet)
                    {
                        comboBox.Items.Add(internalConditionName_Temp);
                    }

                    comboBox.Text = internalConditionName;

                    grid.Children.Add(checkBox);
                    grid.Children.Add(comboBox);

                    Grid.SetRow(checkBox, rowIndex);
                    Grid.SetColumn(checkBox, 0);

                    Grid.SetRow(comboBox, rowIndex);
                    Grid.SetColumn(comboBox, 1);
                }

            }

        }

        private void SetInternalConditionLibrary(InternalConditionLibrary internalConditionLibrary)
        {
            selectSAMObjectComboBoxControl_InternalConditionLibrary.SelectionChanged += SelectSAMObjectComboBoxControl_InternalConditionLibrary_SelectionChanged; ;

            selectSAMObjectComboBoxControl_InternalConditionLibrary.Add(internalText, internalConditionLibrary);
            selectSAMObjectComboBoxControl_InternalConditionLibrary.SelectedText = internalText;
        }

        private void SelectSAMObjectComboBoxControl_InternalConditionLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Space> spaces = GetSpaces();
            SetSpaces(spaces);
        }

        private void SetTextMap(TextMap textMap)
        {
            selectSAMObjectComboBoxControl_TextMap.Add(internalText, textMap);
            selectSAMObjectComboBoxControl_TextMap.SelectedText = internalText;
        }

        private void Assign()
        {
            Func<Space, InternalCondition> func = mapFunc;
            if (func == null)
            {
                InternalConditionLibrary internalConditionLibrary = selectSAMObjectComboBoxControl_InternalConditionLibrary.GetJSAMObject<InternalConditionLibrary>();
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
                        comboBox.SelectedItem = comboBox.Items[index];
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
