// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for RenameSpacesControl.xaml
    /// </summary>
    public partial class RenameSpacesControl : UserControl
    {
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };

        private class ColumnNameAttribute : Attribute
        {
            public ColumnNameAttribute(string Name) 
            { 
                this.Name = Name; 
            }
            public string Name { get; }
        }

        private class SpaceData
        {
            public Guid Guid { get; }

            [ColumnName("Old name")]
            public string Name_Old { get; } = null;

            [ColumnName("New name")]
            public string Name_New { get; set; } = null;

            public SpaceData(Guid guid, string name_Old, string name_New)
            {
                Guid = guid;
                Name_Old = name_Old;
                Name_New = name_New;
            }

            public SpaceData(Space space, string name_New)
            {
                if(space != null)
                {
                    Guid = space.Guid;
                    Name_Old = space.Name;
                }

                Name_New = name_New;
            }
        }

        public UIAnalyticalModel UIAnalyticalModel { get; set; }
        
        public List<Space> Spaces { get; set; }

        public RenameSpacesControl()
        {
            InitializeComponent();

            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;
            textBox_Trim_Count.TextInput += TextBox_IntegerOnly_TextInput;

            textBox_Trim_Count.TextChanged += TextBox_Trim_Count_TextChanged;
            textBox_Trim_Text.TextChanged += TextBox_Trim_Text_TextChanged;

            radioButton_Rename.Click += RadioButton_Rename_Click;
            radioButton_Trim.Click += RadioButton_Trim_Click;
            radioButton_Replace.Click += RadioButton_Replace_Click;

            Rename = true;

            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void RadioButton_Replace_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetReplace();
            dispatcherTimer.Start();
        }

        private void RadioButton_Trim_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTrim();
            dispatcherTimer.Start();
        }

        private void RadioButton_Rename_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetRename();
            dispatcherTimer.Start();
        }

        public RenameSpacesControl(string name, RenameSpaceOptions renameSpaceOption)
        {
            InitializeComponent();

            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;
            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;

            textBox_Trim_Count.TextChanged += TextBox_Trim_Count_TextChanged;
            textBox_Trim_Text.TextChanged += TextBox_Trim_Text_TextChanged;

            Name = name;
            SetRenameSpaceOption(renameSpaceOption);

            Rename = true;

            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            Refresh();
        }

        private void TextBox_IntegerOnly_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_IntegerOnly(sender, e);
        }

        private List<SpaceData> GetSpaceDatas()
        {
            if(dataGrid?.ItemsSource == null)
            {
                return null;
            }

            return dataGrid.ItemsSource.Cast<SpaceData>().ToList();
        }

        public void Apply()
        {
            AdjacencyCluster adjacencyCluster = UIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if(adjacencyCluster != null)
            {
                Dictionary<Space, string> dictionary = new Dictionary<Space, string>();
                List<SpaceData> spaceDatas = GetSpaceDatas();
                if (spaceDatas != null)
                {
                    foreach (SpaceData spaceData in spaceDatas)
                    {
                        Space space = adjacencyCluster.GetObject<Space>(spaceData.Guid);
                        if(space != null)
                        {
                            dictionary[space] = spaceData.Name_New;
                        }
                    }
                }

                UIAnalyticalModel.RenameSpaces(dictionary);
            }

            //if (Rename)
            //{
            //    UIAnalyticalModel.RenameSpaces(Spaces, textBox_Name.Text, RenameSpaceOption);
            //}
            //else if(Replace)
            //{
            //    UIAnalyticalModel.RenameSpaces(Spaces, textBox_Replace_Old.Text, textBox_Replace_New.Text);
            //}
            //else if (Trim)
            //{
            //    int count = TrimCount;
            //    if(count == -1)
            //    {
            //        UIAnalyticalModel.RenameSpaces(Spaces, TrimPosition, TrimText);
            //    }
            //    else
            //    {
            //        UIAnalyticalModel.RenameSpaces(Spaces, TrimPosition, TrimCount);
            //    }
            //}

            Refresh();
        }

        public Dictionary<Space, string> GetNameDictionary()
        {
            AdjacencyCluster adjacencyCluster = UIAnalyticalModel?.JSAMObject?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster);

            if (Rename)
            {
                return adjacencyCluster.RenameSpaces(Spaces, textBox_Name.Text, RenameSpaceOption);
            }
            else if (Replace)
            {
                return adjacencyCluster.RenameSpaces(Spaces, textBox_Replace_Old.Text, textBox_Replace_New.Text);
            }
            else if (Trim)
            {
                int count = TrimCount;
                if (count == -1)
                {
                    return adjacencyCluster.RenameSpaces(Spaces, TrimPosition, TrimText);
                }
                else
                {
                    return adjacencyCluster.RenameSpaces(Spaces, TrimPosition, TrimCount);
                }
            }

            return null;
        }

        public Position Position
        {
            get
            {
                if(radioButton_Prefix.IsChecked != null && radioButton_Prefix.IsChecked.HasValue && radioButton_Prefix.IsChecked.Value)
                {
                    return Position.Prefix;
                }

                if (radioButton_Sufix.IsChecked != null && radioButton_Sufix.IsChecked.HasValue && radioButton_Sufix.IsChecked.Value)
                {
                    return Position.Sufix;
                }

                return Position.Undefined;
            }

            set
            {
                switch(value)
                {
                    case Position.Prefix:
                        radioButton_Prefix.IsChecked = true;
                        break;

                    case Position.Sufix:
                        radioButton_Sufix.IsChecked = true;
                        break;
                }
            }
        }

        public string Name
        {
            get
            {
                return textBox_Name.Text;
            }

            set
            {
                textBox_Name.Text = value;
            }
        }

        public bool IncludeName
        {
            get
            {
                return checkBox_IncludeName.IsChecked != null && checkBox_IncludeName.IsChecked.HasValue && checkBox_IncludeName.IsChecked.Value;
            }

            set
            {
                checkBox_IncludeName.IsChecked = value;
            }
        }

        public bool IncludeNumber
        {
            get
            {
                return checkBox_IncludeNumber.IsChecked != null && checkBox_IncludeNumber.IsChecked.HasValue && checkBox_IncludeNumber.IsChecked.Value;
            }
            set
            {
                checkBox_IncludeNumber.IsChecked = value;
            }
        }

        public bool IncludeLevel
        {
            get
            {
                return checkBox_IncludeLevel.IsChecked != null && checkBox_IncludeLevel.IsChecked.HasValue && checkBox_IncludeLevel.IsChecked.Value;
            }
            set
            {
                checkBox_IncludeLevel.IsChecked = value;
            }
        }

        public string LevelSpeparator
        {
            get
            {
                return textBox_LevelSeparator.Text;
            }

            set
            {
                textBox_LevelSeparator.Text = value;
            }
        }

        public string NameSpeparator
        {
            get
            {
                return textBox_NameSeparator.Text;
            }

            set
            {
                textBox_NameSeparator.Text = value;
            }
        }

        public int DigitsNumber
        {
            get
            {
                if(!Core.Query.TryConvert(textBox_DigitsNumber.Text, out int result))
                {
                    return 1000;
                }

                return result;
            }

            set
            {
                textBox_DigitsNumber.Text = value.ToString();
            }
        }

        public bool Rename
        {
            get
            {
                return radioButton_Rename.IsChecked != null && radioButton_Rename.IsChecked.HasValue && radioButton_Rename.IsChecked.Value;
            }

            set
            {
                radioButton_Rename.IsChecked = value;
                SetRename();
            }
        }

        public bool Trim
        {
            get
            {
                return radioButton_Trim.IsChecked != null && radioButton_Trim.IsChecked.HasValue && radioButton_Trim.IsChecked.Value;
            }

            set
            {
                radioButton_Trim.IsChecked = value;
                SetTrim();
            }
        }

        public bool Replace
        {
            get
            {
                return radioButton_Replace.IsChecked != null && radioButton_Replace.IsChecked.HasValue && radioButton_Replace.IsChecked.Value;
            }

            set
            {
                radioButton_Replace.IsChecked = value;
                SetReplace();
            }
        }

        public int StartIndex
        {
            get
            {
                bool uniqueNumber = checkBox_UniqueNumber.IsChecked != null && checkBox_UniqueNumber.IsChecked.HasValue && checkBox_UniqueNumber.IsChecked.Value;
                return uniqueNumber ? 1 : -1;
            }

            set
            {
                checkBox_UniqueNumber.IsChecked = value != -1;
            }
        }

        public Position TrimPosition
        {
            get
            {
                if (radioButton_Trim_Prefix.IsChecked != null && radioButton_Trim_Prefix.IsChecked.HasValue && radioButton_Trim_Prefix.IsChecked.Value)
                {
                    return Position.Prefix;
                }

                if (radioButton_Trim_Sufix.IsChecked != null && radioButton_Trim_Sufix.IsChecked.HasValue && radioButton_Trim_Sufix.IsChecked.Value)
                {
                    return Position.Sufix;
                }

                return Position.Undefined;
            }

            set
            {
                switch (value)
                {
                    case Position.Prefix:
                        radioButton_Trim_Prefix.IsChecked = true;
                        break;

                    case Position.Sufix:
                        radioButton_Trim_Sufix.IsChecked = true;
                        break;
                }
            }
        }

        public string TrimText
        {
            get
            {
                return textBox_Trim_Text.Text;
            }
        }

        public int TrimCount
        {
            get
            {
                if (!Core.Query.TryConvert(textBox_Trim_Count.Text, out int result))
                {
                    return -1;
                }

                return result;
            }

            set
            {
                textBox_Trim_Count.Text = value.ToString();
            }
        }

        public RenameSpaceOptions RenameSpaceOption
        {
            get
            {
                return new RenameSpaceOptions()
                {
                    Position = Position,
                    IncludeName = IncludeName,
                    IncludeNumber = IncludeNumber,
                    IncludeLevel = IncludeLevel,
                    LevelSpeparator = LevelSpeparator,
                    NameSeparator = NameSpeparator,
                    DigitsNumber = DigitsNumber,
                    StartIndex = StartIndex,
                };
            }

            set
            {
                SetRenameSpaceOption(value);
            }
        }

        private void SetRenameSpaceOption(RenameSpaceOptions renameSpaceOption)
        {
            if(renameSpaceOption == null)
            {
                return;
            }

            Position = renameSpaceOption.Position;
            IncludeName = renameSpaceOption.IncludeName;
            IncludeNumber = renameSpaceOption.IncludeNumber;
            IncludeLevel = renameSpaceOption.IncludeLevel;
            LevelSpeparator = renameSpaceOption.LevelSpeparator;
            NameSpeparator = renameSpaceOption.NameSeparator;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            textBox_Name.Focus();
        }

        private void SetRename()
        {
            if (radioButton_Rename.IsChecked != null && radioButton_Rename.IsChecked.HasValue && radioButton_Rename.IsChecked.Value)
            {
                radioButton_Trim.IsChecked = false;
                radioButton_Replace.IsChecked = false;

                radioButton_Trim_Prefix.IsEnabled = false;
                radioButton_Trim_Sufix.IsEnabled = false;
                label_Trim_Count.IsEnabled = false;
                textBox_Trim_Count.IsEnabled = false;
                label_Trim_Text.IsEnabled = false;
                textBox_Trim_Text.IsEnabled = false;

                label_Replace_Old.IsEnabled = false;
                textBox_Replace_Old.IsEnabled = false;
                label_Replace_New.IsEnabled = false;
                textBox_Replace_New.IsEnabled = false;

                groupBox_Number.IsEnabled = true;
                checkBox_IncludeName.IsEnabled = true;
                textBox_Name.IsEnabled = true;
                checkBox_UniqueNumber.IsEnabled = true;
            }
        }

        private void SetTrim()
        {
            if (radioButton_Trim.IsChecked != null && radioButton_Trim.IsChecked.HasValue && radioButton_Trim.IsChecked.Value)
            {
                radioButton_Rename.IsChecked = false;
                radioButton_Replace.IsChecked = false;

                radioButton_Trim_Prefix.IsEnabled = true;
                radioButton_Trim_Sufix.IsEnabled = true;
                label_Trim_Count.IsEnabled = true;
                textBox_Trim_Count.IsEnabled = true;
                label_Trim_Text.IsEnabled = true;
                textBox_Trim_Text.IsEnabled = true;

                label_Replace_Old.IsEnabled = false;
                textBox_Replace_Old.IsEnabled = false;
                label_Replace_New.IsEnabled = false;
                textBox_Replace_New.IsEnabled = false;

                groupBox_Number.IsEnabled = false;
                checkBox_IncludeName.IsEnabled = false;
                textBox_Name.IsEnabled = false;
                checkBox_UniqueNumber.IsEnabled = false;
            }
        }

        private void SetReplace()
        {
            if (radioButton_Replace.IsChecked != null && radioButton_Replace.IsChecked.HasValue && radioButton_Replace.IsChecked.Value)
            {
                radioButton_Rename.IsChecked = false;
                radioButton_Trim.IsChecked = false;

                radioButton_Trim_Prefix.IsEnabled = false;
                radioButton_Trim_Sufix.IsEnabled = false;
                label_Trim_Count.IsEnabled = false;
                textBox_Trim_Count.IsEnabled = false;
                label_Trim_Text.IsEnabled = false;
                textBox_Trim_Text.IsEnabled = false;

                label_Replace_Old.IsEnabled = true;
                textBox_Replace_Old.IsEnabled = true;
                label_Replace_New.IsEnabled = true;
                textBox_Replace_New.IsEnabled = true;

                groupBox_Number.IsEnabled = false;
                checkBox_IncludeName.IsEnabled = false;
                textBox_Name.IsEnabled = false;
                checkBox_UniqueNumber.IsEnabled = false;
            }
        }


        private void TextBox_Trim_Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBox_Trim_Count.TextChanged -= TextBox_Trim_Count_TextChanged;
            textBox_Trim_Count.Text = null;
            textBox_Trim_Count.TextChanged += TextBox_Trim_Count_TextChanged;
        }

        private void TextBox_Trim_Count_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBox_Trim_Text.TextChanged -= TextBox_Trim_Text_TextChanged;
            textBox_Trim_Text.Text = null;
            textBox_Trim_Text.TextChanged += TextBox_Trim_Text_TextChanged;
        }

        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            PropertyDescriptor propertyDescriptor = e.PropertyDescriptor as PropertyDescriptor;
            ColumnNameAttribute columnNameAttribute = propertyDescriptor.Attributes[typeof(ColumnNameAttribute)] as ColumnNameAttribute;
            if (columnNameAttribute == null)
            {
                e.Cancel = true;
                return;
            }

            e.Column.Header = columnNameAttribute.Name;
        }

        private void Refresh()
        {
            dataGrid.ItemsSource = null;

            AdjacencyCluster adjacencyCluster = UIAnalyticalModel?.JSAMObject?.AdjacencyCluster;

            Dictionary<Space, string> nameDictionary = GetNameDictionary();
            if(nameDictionary == null || nameDictionary.Count == 0)
            {
                nameDictionary = new Dictionary<Space, string>();
                Spaces?.FindAll(x => x!= null).ForEach(x => nameDictionary[x] = x.Name);
            }

            List<SpaceData> spaceDatas = new List<SpaceData>();
            foreach(KeyValuePair<Space, string> keyValuePair in nameDictionary)
            {
                if(keyValuePair.Key == null)
                {
                    continue;
                }

                Space space = adjacencyCluster?.GetObject<Space>(keyValuePair.Key.Guid);
                if(space == null)
                {
                    space = keyValuePair.Key;
                }

                spaceDatas.Add(new SpaceData(space, keyValuePair.Value));
            }

            dataGrid.ItemsSource = spaceDatas;
            foreach (DataGridColumn column in dataGrid.Columns)
            {
                column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
            }
        }

        private void checkBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            dispatcherTimer.Start();
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dispatcherTimer.Start();
        }
    }
}
