﻿using System.Collections.Generic;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for RenameSpacesControl.xaml
    /// </summary>
    public partial class RenameSpacesControl : UserControl
    {
        public UIAnalyticalModel UIAnalyticalModel { get; set; }
        public List<Space> Spaces { get; set; }

        public RenameSpacesControl()
        {
            InitializeComponent();

            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;
            textBox_Trim_Count.TextInput += TextBox_IntegerOnly_TextInput;

            Rename = true;
        }

        public RenameSpacesControl(string name, RenameSpaceOption renameSpaceOption)
        {
            InitializeComponent();

            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;
            textBox_DigitsNumber.TextInput += TextBox_IntegerOnly_TextInput;

            Name = name;
            SetRenameSpaceOption(renameSpaceOption);

            Rename = true;
        }

        private void TextBox_IntegerOnly_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_IntegerOnly(sender, e);
        }

        public void Apply()
        {
            if(Rename)
            {
                UIAnalyticalModel.RenameSpaces(Spaces, textBox_Name.Text, RenameSpaceOption);
            }
            else if(Replace)
            {
                UIAnalyticalModel.RenameSpaces(Spaces, textBox_Replace_Old.Text, textBox_Replace_New.Text);
            }
            else if (Trim)
            {
                UIAnalyticalModel.RenameSpaces(Spaces, TrimPosition, TrimCount);
            }
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

        public RenameSpaceOption RenameSpaceOption
        {
            get
            {
                return new RenameSpaceOption()
                {
                    Position = Position,
                    IncludeName = IncludeName,
                    IncludeNumber = IncludeNumber,
                    IncludeLevel = IncludeLevel,
                    LevelSpeparator = LevelSpeparator,
                    NameSeparator = NameSpeparator,
                    DigitsNumber = DigitsNumber,
                };
            }

            set
            {
                SetRenameSpaceOption(value);
            }
        }

        private void SetRenameSpaceOption(RenameSpaceOption renameSpaceOption)
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

        private void radioButton_Rename_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetRename();
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

                label_Replace_Old.IsEnabled = false;
                textBox_Replace_Old.IsEnabled = false;
                label_Replace_New.IsEnabled = false;
                textBox_Replace_New.IsEnabled = false;

                groupBox_Number.IsEnabled = true;
                checkBox_IncludeName.IsEnabled = true;
                textBox_Name.IsEnabled = true;
            }
        }

        private void radioButton_Trim_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTrim();
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

                label_Replace_Old.IsEnabled = false;
                textBox_Replace_Old.IsEnabled = false;
                label_Replace_New.IsEnabled = false;
                textBox_Replace_New.IsEnabled = false;

                groupBox_Number.IsEnabled = false;
                checkBox_IncludeName.IsEnabled = false;
                textBox_Name.IsEnabled = false;
            }
        }

        private void radioButton_Replace_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetReplace();
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

                label_Replace_Old.IsEnabled = true;
                textBox_Replace_Old.IsEnabled = true;
                label_Replace_New.IsEnabled = true;
                textBox_Replace_New.IsEnabled = true;

                groupBox_Number.IsEnabled = false;
                checkBox_IncludeName.IsEnabled = false;
                textBox_Name.IsEnabled = false;
            }
        }
    }
}