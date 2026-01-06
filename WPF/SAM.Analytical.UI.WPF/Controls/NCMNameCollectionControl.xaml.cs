using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for NCMNameCollectionControl.xaml
    /// </summary>
    public partial class NCMNameCollectionControl : UserControl
    {
        public event MouseButtonEventHandler NCMNameDoubleClicked;

        private List<NCMName> nCMNames;

        public NCMNameCollectionControl()
        {
            InitializeComponent();

            SearchControl_Main.SelectionMode = SelectionMode.Single;

            SearchControl_Main.SelectedIndexChanged += SearchControl_Main_SelectionChanged;
        }

        public NCMNameCollectionControl(IEnumerable<NCMName> nCMNames, NCMNameCollectionOptions nCMNameCollectionOptions = null)
        {
            InitializeComponent();

            SearchControl_Main.SelectionMode = SelectionMode.Single;

            this.nCMNames = nCMNames == null ? null : new List<NCMName>(nCMNames);

            SetGroups(this.nCMNames);
            SetNCMNames(this.nCMNames);

            SearchControl_Main.SelectedIndexChanged += SearchControl_Main_SelectionChanged;

            SetNCMNameCollectionOptions(nCMNameCollectionOptions);
        }

        private void ListBox_Main_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetNCMName(SelectedNCMName);
        }

        private void SetNCMName(NCMName nCMName)
        {
            TextBox_Name.TextChanged -= TextBox_TextChanged;
            TextBox_Version.TextChanged -= TextBox_TextChanged;
            TextBox_Description.TextChanged -= TextBox_TextChanged;
            TextBox_Group.TextChanged -= TextBox_TextChanged;

            TextBox_Name.Text = nCMName?.Name;
            TextBox_Version.Text = nCMName?.Version;
            TextBox_Description.Text = nCMName?.Description;
            TextBox_Group.Text = nCMName?.Group;

            TextBox_Name.TextChanged += TextBox_TextChanged;
            TextBox_Version.TextChanged += TextBox_TextChanged;
            TextBox_Description.TextChanged += TextBox_TextChanged;
            TextBox_Group.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchControl_Main.SearchObjectWrapper.Add(GetNCMName());
        }

        private NCMName GetNCMName()
        {
            return new NCMName(TextBox_Name.Text, TextBox_Version.Text, TextBox_Description.Text, TextBox_Group.Text);
        }

        private void SetNCMNameCollectionOptions(NCMNameCollectionOptions nCMNameCollectionOptions)
        {
            if(nCMNameCollectionOptions == null)
            {
                return;
            }

            TextBox_Description.IsEnabled = nCMNameCollectionOptions.Editable;
            TextBox_Name.IsEnabled = nCMNameCollectionOptions.Editable;
            TextBox_Group.IsEnabled = nCMNameCollectionOptions.Editable;
            TextBox_Version.IsEnabled = nCMNameCollectionOptions.Editable;
        }

        public NCMNameCollectionOptions NCMNameCollectionOptions
        {
            set
            {
                SetNCMNameCollectionOptions(value);
            }
        }

        private void SetNCMNames(IEnumerable<NCMName> nCMNames)
        {
            if (nCMNames == null || nCMNames.Count() == 0)
            {
                return;
            }

            string group = ComboBox_Group.SelectedItem as string;
            if(group == "<All>")
            {
                group = null;
            }

            List<NCMName> nCMNames_Temp = new List<NCMName>();
            foreach (NCMName nCMName in nCMNames)
            {
                if (nCMName == null)
                {
                    continue;
                }

                if(!string.IsNullOrEmpty(group))
                {
                    if(group != nCMName.Group)
                    {
                        continue;
                    }
                }

                nCMNames_Temp.Add(nCMName);
            }

            SearchControl_Main.SearchObjectWrapper = new SearchObjectWrapper(nCMNames_Temp, x => (x as NCMName)?.FullName, false);
        }

        private void SetGroups(IEnumerable<NCMName> nCMNames)
        {
            ComboBox_Group.SelectionChanged -= ComboBox_Group_SelectionChanged;

            object selectedItem = ComboBox_Group.SelectedItem;

            ComboBox_Group.Items.Clear();

            string all = "<All>";

            if (nCMNames != null && nCMNames.Count() > 0)
            {
                List<string> groups = nCMNames.ToList().ConvertAll(x => x?.Group).FindAll(x => !string.IsNullOrWhiteSpace(x));
                groups = groups.Distinct().ToList();
                groups.Sort();
                
                ComboBox_Group.Items.Add(all);
                foreach (string group in groups)
                {
                    ComboBox_Group.Items.Add(group);
                }
            }

            if(selectedItem != null)
            {
                ComboBox_Group.SelectedItem = selectedItem;
            }
            else
            {
                ComboBox_Group.SelectedItem = all;
            }

            ComboBox_Group.SelectionChanged += ComboBox_Group_SelectionChanged;
        }

        public NCMNameCollection GetNCMNameCollection()
        {
            List<NCMName> nCMNames = SearchControl_Main.GetSelectedItems<NCMName>();
            if(nCMNames == null)
            {
                return null;
            }

            NCMNameCollection result = new NCMNameCollection(nCMNames);
            return result;
        }

        public NCMName SelectedNCMName
        {
            get
            {
                return SearchControl_Main.GetSelectedItems<NCMName>()?.FirstOrDefault();
            }
        }

        public NCMNameCollection NCMNameCollection
        {
            get
            {
                return GetNCMNameCollection();
            }

            set
            {
                nCMNames = value == null ? null : new List<NCMName>(value);
                SetGroups(nCMNames);
                SetNCMNames(nCMNames);
            }
        }

        private void ComboBox_Group_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetNCMNames(nCMNames);
        }

        private void SearchControl_Main_SelectionChanged(object sender, EventArgs e)
        {
            SetNCMName(GetNCMNameCollection()?.FirstOrDefault());
        }

        private void SearchControl_Main_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NCMNameDoubleClicked?.Invoke(this, e);
        }
    }
}
