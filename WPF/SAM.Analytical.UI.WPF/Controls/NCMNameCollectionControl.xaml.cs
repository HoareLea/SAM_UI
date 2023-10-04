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
        public NCMNameCollectionControl()
        {
            InitializeComponent();

            ListBox_Main.SelectionChanged += ListBox_Main_SelectionChanged;
        }

        public NCMNameCollectionControl(IEnumerable<NCMName> nCMNames)
        {
            InitializeComponent();

            SetNCMNames(nCMNames);

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

            TextBox_Name.Text = nCMName.Name;
            TextBox_Version.Text = nCMName.Version;
            TextBox_Description.Text = nCMName.Description;
            TextBox_Group.Text = nCMName.Group;

            TextBox_Name.TextChanged += TextBox_TextChanged;
            TextBox_Version.TextChanged += TextBox_TextChanged;
            TextBox_Description.TextChanged += TextBox_TextChanged;
            TextBox_Group.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = SelectedTextBox;
            if(textBox == null)
            {
                return;
            }

            textBox.Tag = GetNCMName();
        }

        private NCMName GetNCMName()
        {
            return new NCMName(TextBox_Name.Text, TextBox_Version.Text, TextBox_Description.Text, TextBox_Group.Text);
        }

        private TextBox SelectedTextBox
        {
            get
            {
                if (ListBox_Main.SelectedItems == null)
                {
                    return null;
                }

                foreach (object @object in ListBox_Main.SelectedItems)
                {
                    TextBox textBox = @object as TextBox;
                    if (textBox == null)
                    {
                        continue;
                    }

                    NCMName nCMName = textBox.Tag as NCMName;
                    if (nCMName == null)
                    {
                        continue;
                    }

                    return textBox;
                }

                return null;
            }

        }

        private void SetNCMNames(IEnumerable<NCMName> nCMNames)
        {
            ListBox_Main.Items.Clear();

            if (nCMNames == null || nCMNames.Count() == 0)
            {
                return;
            }

            foreach (NCMName nCMName in nCMNames)
            {
                if (nCMName == null)
                {
                    continue;
                }

                string text = string.IsNullOrWhiteSpace(nCMName.FullName) ? "???" : nCMName.FullName;

                ListBox_Main.Items.Add(new TextBox() { Text = text, Tag = nCMName, IsReadOnly = true });
            }
        }

        public NCMNameCollection GetNCMNameCollection()
        {
            NCMNameCollection result = new NCMNameCollection();

            foreach (object @object in ListBox_Main.Items)
            {
                TextBox textBox = @object as TextBox;
                if (textBox == null)
                {
                    continue;
                }

                NCMName nCMName = textBox.Tag as NCMName;
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
                return SelectedTextBox?.Tag as NCMName;
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
                SetNCMNames(value);
            }
        }
    }
}
