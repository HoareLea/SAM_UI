using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for NCMNameCollectionControl.xaml
    /// </summary>
    public partial class NCMNameCollectionControl : UserControl
    {
        private List<NCMName> nCMNames;

        public NCMNameCollectionControl()
        {
            InitializeComponent();

            ListBox_Main.SelectionChanged += ListBox_Main_SelectionChanged;
        }

        public NCMNameCollectionControl(IEnumerable<NCMName> nCMNames)
        {
            InitializeComponent();

            this.nCMNames = nCMNames == null ? null : new List<NCMName>(nCMNames);

            SetGroups(this.nCMNames);
            SetNCMNames(this.nCMNames);

            ListBox_Main.SelectionChanged += ListBox_Main_SelectionChanged;
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
            Label label = SelectedLabel;
            if(label == null)
            {
                return;
            }

            label.Tag = GetNCMName();
        }

        private NCMName GetNCMName()
        {
            return new NCMName(TextBox_Name.Text, TextBox_Version.Text, TextBox_Description.Text, TextBox_Group.Text);
        }

        private Label SelectedLabel
        {
            get
            {
                if (ListBox_Main.SelectedItems == null)
                {
                    return null;
                }

                foreach (object @object in ListBox_Main.SelectedItems)
                {
                    Label label = @object as Label;
                    if (label == null)
                    {
                        continue;
                    }

                    NCMName nCMName = label.Tag as NCMName;
                    if (nCMName == null)
                    {
                        continue;
                    }

                    return label;
                }

                return null;
            }

        }

        private void SetNCMNames(IEnumerable<NCMName> nCMNames)
        {
            string selectedText = (ListBox_Main.SelectedItem as Label)?.Content as string; 

            ListBox_Main.Items.Clear();

            if (nCMNames == null || nCMNames.Count() == 0)
            {
                return;
            }

            string group = ComboBox_Group.SelectedItem as string;
            if(group == "<All>")
            {
                group = null;
            }

            Label selectedLabel = null;
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

                string text = string.IsNullOrWhiteSpace(nCMName.FullName) ? "???" : nCMName.FullName;

                text = text.Replace("_", "__");
                Label label = new Label() { Content = text, Tag = nCMName };

                ListBox_Main.Items.Add(label);

                if(text == selectedText)
                {
                    selectedLabel = label;
                }
            }

            if(selectedLabel != null)
            {
                ListBox_Main.SelectedItem = selectedLabel;
            }
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
            NCMNameCollection result = new NCMNameCollection();

            foreach (object @object in ListBox_Main.Items)
            {
                Label label = @object as Label;
                if (label == null)
                {
                    continue;
                }

                NCMName nCMName = label.Tag as NCMName;
                if (nCMName == null)
                {
                    continue;
                }

                result.Add(nCMName);
            }

            return result;
        }

        public NCMName SelectedNCMName
        {
            get
            {
                return SelectedLabel?.Tag as NCMName;
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
    }
}
