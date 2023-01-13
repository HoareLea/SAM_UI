using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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
        private static string selectText = "Select...";
        private static string internalText = "<Internal>";

        private TextMap textMap_Loaded;
        private InternalConditionLibrary internalConditionLibrary_Loaded;

        private TextMap textMap;
        private InternalConditionLibrary internalConditionLibrary;

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

            TextMap = textMap;
            InternalConditionLibrary = internalConditionLibrary;

            SetSpaces(spaces);

            Load();
        }

        private void Load()
        {
            LoadInternalConditionLibrary();
            LoadTextMap();
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
                return textMap;
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
                return internalConditionLibrary;
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
            if (wrapPanel.Children == null || wrapPanel.Children.Count == 0)
            {
                return null;
            }

            List<Space> result = new List<Space>();
            foreach(DockPanel dockPanel in wrapPanel.Children)
            {
                if(selected)
                {
                    if(!(dockPanel.Children[0] as CheckBox).IsChecked.Value)
                    {
                        continue;
                    }
                }

                Space space = dockPanel.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                InternalCondition internalCondition = null;

                string internalConditionName = (dockPanel.Children[1] as ComboBox).Text;
                if(!string.IsNullOrWhiteSpace(internalConditionName))
                {
                    internalCondition = internalConditionLibrary.GetInternalConditions(internalConditionName)?.FirstOrDefault();
                }

                space = new Space(space);

                if(internalCondition != null)
                {
                    space.InternalCondition = internalCondition;
                }

                result.Add(space);
            }

            return result;
        }

        private List<Tuple<Space, string>> GetTuples()
        {
            if (wrapPanel.Children == null || wrapPanel.Children.Count == 0)
            {
                return null;
            }

            List<Tuple<Space, string>> result = new List<Tuple<Space, string>>();
            foreach (DockPanel dockPanel in wrapPanel.Children)
            {
                Space space = dockPanel.Tag as Space;
                if (space == null)
                {
                    continue;
                }

                string internalConditionName = (dockPanel.Children[1] as ComboBox).Text;
                if(string.IsNullOrWhiteSpace(internalConditionName))
                {
                    internalConditionName = null;
                }

                result.Add(new Tuple<Space, string>(space, internalConditionName));
            }

            return result;
        }

        private void SetSpaces(IEnumerable<Space> spaces)
        {
            List<Tuple<Space, string>> tuples = GetTuples();

            wrapPanel.Children.Clear();

            if(spaces == null || spaces.Count() == 0)
            {
                return;
            }

            HashSet<string> hashSet = new HashSet<string>();
            hashSet.Add(string.Empty);
            if(internalConditionLibrary_Loaded != null)
            {
                List<InternalCondition> internalConditons = internalConditionLibrary_Loaded.GetInternalConditions();
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
                    DockPanel dockPanel = new DockPanel() { Width = 330, Height = 35 };
                    System.Windows.Controls.Label label = new System.Windows.Controls.Label() { Width = 100, Content = key, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
                    dockPanel.Children.Add(label);
                    wrapPanel.Children.Add(dockPanel);
                }

                foreach (Space space in spaces_Group)
                {
                    string internalConditionName = string.Empty;
                    if (tuples != null)
                    {
                        int index = tuples.FindIndex(x => x.Item1.Guid == space.Guid);
                        if (index != -1 && hashSet.Contains(tuples[index].Item2))
                        {
                            internalConditionName = tuples[index].Item2;
                        }
                    }

                    DockPanel dockPanel = new DockPanel() { Width = 330, Height = 30, Tag = space };

                    CheckBox checkBox = new CheckBox() { Width = 100, Content = space.Name, IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
                    dockPanel.Children.Add(checkBox);

                    ComboBox comboBox = new ComboBox() { MinWidth = 150, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Height = 25 };
                    foreach (string internalConditionName_Temp in hashSet)
                    {
                        comboBox.Items.Add(internalConditionName_Temp);
                    }

                    comboBox.Text = internalConditionName;

                    dockPanel.Children.Add(comboBox);

                    wrapPanel.Children.Add(dockPanel);
                }

            }

        }

        private void LoadInternalConditionLibrary()
        {
            Load(comboBox_InternalConditionLibrary, internalConditionLibrary);
        }

        private void LoadTextMap()
        {
            Load(comboBox_TextMap, textMap);
        }

        private void SetInternalConditionLibrary(InternalConditionLibrary internalConditionLibrary)
        {
            if(this.internalConditionLibrary == internalConditionLibrary)
            {
                return;
            }

            this.internalConditionLibrary = internalConditionLibrary;
            internalConditionLibrary_Loaded = this.internalConditionLibrary;

            LoadInternalConditionLibrary();

            List<Space> spaces = GetSpaces();
            SetSpaces(spaces);
        }

        private void SetTextMap(TextMap textMap)
        {
            if (this.textMap == textMap)
            {
                return;
            }

            this.textMap = textMap;
            textMap_Loaded = this.textMap;

            LoadTextMap();
        }

        private void Assign()
        {
            Func<Space, InternalCondition> func = mapFunc;
            if(func == null)
            {
                func = Query.DefaultMapFunc(internalConditionLibrary_Loaded, textMap_Loaded);
            }

            foreach(DockPanel dockPanel in wrapPanel.Children)
            {
                if(dockPanel == null || dockPanel.Children.Count < 1)
                {
                    continue;
                }

                CheckBox checkBox = dockPanel.Children[0] as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                if (!checkBox.IsChecked.Value)
                {
                    continue;
                }

                Space space = dockPanel.Tag as Space;
                if(space == null)
                {
                    continue;
                }

                InternalCondition internalCondition = func.Invoke(space);
                if(internalCondition?.Name != null)
                {
                    (dockPanel.Children[1] as ComboBox).Text = internalCondition.Name;
                }
            }
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
            foreach (DockPanel dockPanel in wrapPanel.Children)
            {
                if(dockPanel == null || dockPanel.Children.Count < 1)
                {
                    continue;
                }

                CheckBox checkBox = dockPanel.Children[0] as CheckBox;
                if(checkBox == null)
                {
                    continue;
                }

                checkBox.IsChecked = isChecked;
            }
        }

        private void comboBox_InternalConditionLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InternalConditionLibrary internalConditionLibrary = GetJSAMObject(comboBox_InternalConditionLibrary, this.internalConditionLibrary);

            if(internalConditionLibrary == null)
            {
                comboBox_InternalConditionLibrary.SelectedItem = comboBox_InternalConditionLibrary.Text;
                return;
            }

            internalConditionLibrary_Loaded = internalConditionLibrary;
        }

        private void comboBox_TextMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextMap textMap = GetJSAMObject(comboBox_TextMap, this.textMap);

            if (textMap == null)
            {
                comboBox_TextMap.SelectedItem = comboBox_TextMap.Text;
                return;
            }

            textMap_Loaded = textMap;
        }

        private static T GetJSAMObject<T>(ComboBox comboBox, T jSAMObject) where T : IJSAMObject
        {
            string text = comboBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(text))
            {
                return default(T);
            }

            T result = default(T);
            if (text.Equals(selectText))
            {
                string path = null;
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    string directory = Analytical.Query.ResourcesDirectory();
                    if (System.IO.Directory.Exists(directory))
                    {
                        openFileDialog.InitialDirectory = directory;
                    }

                    openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        comboBox.SelectedItem = comboBox.Text;
                        return result;
                    }

                    path = openFileDialog.FileName;
                    comboBox.Items.Add(path);
                    comboBox.Text = path;
                }

                List<T> ts = Core.Convert.ToSAM<T>(path);
                if (ts != null && ts.Count != 0)
                {
                    result = ts[0];
                }
            }
            else if (text.Equals(internalText))
            {
                result = jSAMObject;
            }
            else
            {
                List<T> ts = Core.Convert.ToSAM<T>(text);
                if (ts != null && ts.Count != 0)
                {
                    result = ts[0];
                }
            }

            return result;
        }

        private static void Load(ComboBox comboBox, object @object)
        {
            string selectedValue = comboBox?.Text;

            HashSet<string> hashSet = new HashSet<string>();
            foreach (string item in comboBox.Items)
            {
                hashSet.Add(item);
            }

            if (@object == null)
            {
                hashSet.Remove(internalText);
            }
            else
            {
                hashSet.Add(internalText);
            }

            if (!hashSet.Contains(selectText))
            {
                hashSet.Add(selectText);
            }

            List<string> values = hashSet.ToList();

            if (values.Contains(internalText))
            {
                values.Remove(internalText);
                values.Add(internalText);
            }

            if (values.Contains(selectText))
            {
                values.Remove(selectText);
                values.Add(selectText);
            }

            comboBox.Items.Clear();
            if (comboBox.Items.Count != 0)
            {
                comboBox.Items.Clear();
            }

            foreach (string value in values)
            {
                comboBox.Items.Add(value);
            }

            if (!string.IsNullOrWhiteSpace(selectedValue))
            {
                comboBox.Text = selectedValue;
            }

            if (string.IsNullOrWhiteSpace(comboBox.Text) && @object != null && comboBox.Items.Contains(internalText))
            {
                comboBox.Text = internalText;
            }
        }
    }
}
