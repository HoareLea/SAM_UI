﻿using System.Windows.Controls;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Query
    {
        public static Brush Background(this ComboBox comboBox)
        {
            if (comboBox == null)
            {
                return null;
            }

            TextBox textbox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textbox != null)
            {
                Border border = textbox.Parent as Border;
                if (border != null)
                {
                    return border.Background;
                }
            }

            return null;
        }
    }
}