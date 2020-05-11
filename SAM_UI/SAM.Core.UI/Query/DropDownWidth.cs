using System.Windows.Forms;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static int DropDownWidth(this ComboBox comboBox)
        {
            if (comboBox == null || comboBox.Items == null)
                return -1;
            
            int max = 0;
            int temp = 0;
            Label label = new Label();

            foreach (object aItem in comboBox.Items)
            {
                label.Text = aItem.ToString();
                temp = label.PreferredWidth;
                if (temp > max)
                    max = temp;
            }
            label.Dispose();
            return max;
        }
    }
}