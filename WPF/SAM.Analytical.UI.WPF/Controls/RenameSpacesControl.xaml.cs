using System.Collections.Generic;
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

            textBox_DigitsNumber.TextInput += TextBox_DigitsNumber_TextInput;
        }

        public RenameSpacesControl(string name, RenameSpaceOption renameSpaceOption)
        {
            InitializeComponent();

            textBox_DigitsNumber.TextInput += TextBox_DigitsNumber_TextInput;

            Name = name;
            SetRenameSpaceOption(renameSpaceOption);
        }

        private void TextBox_DigitsNumber_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Core.Windows.EventHandler.ControlText_IntegerOnly(sender, e);
        }

        public void Rename()
        {
            UIAnalyticalModel.RenameSpaces(Spaces, textBox_Name.Text, RenameSpaceOption);
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
    }
}
