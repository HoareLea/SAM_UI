using System.Windows.Controls;
using System.Windows.Media;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static void Background(this ComboBox comboBox, Brush brush)
        {
            if(comboBox == null || brush == null)
            {
                return;
            }

            TextBox textbox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textbox != null)
            {
                Border border = textbox.Parent as Border;
                if(border != null)
                {
                    border.Background = brush;
                }
            }
        }
    }
}