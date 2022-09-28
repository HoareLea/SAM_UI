using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierProcessForm3 : Form
    {
        private UIMollierProcess UI_MollierProcess;
        private Color processColor;
        private List<string> airStartConditions_List = new List<string>();
        public MollierProcessForm3()
        {
            InitializeComponent();
            this.Size = new Size(212, 145);

            airStartConditions_List.Add("Dry Bulb Temperature");
            airStartConditions_List.Add("Relative Humidity");
            airStartConditions_List.Add("Humidity Ratio");
            airStartConditions_List.Add("Dew Point");
            airStartConditions_List.Add("Wet Bulb Temperature");
            airStartConditions_Changed();
            airStartConditionsFirst_ComboBox.Visible = true;
        }
        public UIMollierProcess GetMollierProcess()
        {
            return UI_MollierProcess;
        }

        private void ComboBox_ChooseProcess_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            
            customizeButton.Visible = true;
            newProcess_MainSettings();

            return;
        }

        private void newProcess_MainSettings()
        {
            
            
        }
        private void airStartConditions_Changed()
        {
            if (airStartConditions_List == null)
            {
                airStartConditions_List = new List<string>();
            }
            List<string> airStartConditions_List_Temp = new List<string>();
            for (int i = 0; i < airStartConditions_List.Count; i++)
            {
                if (airStartConditions_List[i] == airStartConditionsFirst_ComboBox.Text || airStartConditions_List[i] == airStartConditionsSecond_ComboBox.Text)
                {
                    continue;
                }
                airStartConditions_List_Temp.Add(airStartConditions_List[i]);
            }
            airStartConditionsFirst_ComboBox.Items.Clear();
            airStartConditionsSecond_ComboBox.Items.Clear();
            for (int i = 0; i < airStartConditions_List_Temp.Count; i++)
            {
                airStartConditionsFirst_ComboBox.Items.Add(airStartConditions_List_Temp[i]);
                airStartConditionsSecond_ComboBox.Items.Add(airStartConditions_List_Temp[i]);

            }
        }


        private void customizeButton_Click(object sender, System.EventArgs e)
        {

        }

        private void airStartConditionsFirst_ComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            airStartConditionsFirst_ComboBox.Text = airStartConditionsFirst_ComboBox.SelectedItem.ToString();
            airStartConditions_Changed();
        }

        private void airStartConditionsFirst_Value_TextChanged(object sender, System.EventArgs e)
        {

        }

        private void airStartConditionsSecond_ComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            airStartConditionsSecond_ComboBox.Text = airStartConditionsSecond_ComboBox.SelectedItem.ToString();

            airStartConditionsSecond_ComboBox.SelectedText = airStartConditionsSecond_ComboBox.SelectedItem.ToString();
            airStartConditions_Changed();
        }

        private void airStartConditionsSecond_Value_TextChanged(object sender, System.EventArgs e)
        {

        }

        private void Button_OK_Click(object sender, System.EventArgs e)
        {
            if(!Core.Query.TryConvert(airStartConditionsFirst_Value.Text, out double value_1) || !Core.Query.TryConvert(airStartConditionsSecond_Value.Text, out double value_2))
            {
                return;
            }
            MollierPoint start = Query.MollierPointByTwoParameters(airStartConditionsFirst_Value.Text, value_1, airStartConditionsSecond_Value.Text, value_2);

        }
                 

        private void valueSecure(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }
        private void airStartConditionsFirst_Value_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }

        private void airStartConditionsSecond_Value_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }

        private void firstParameterValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }

        private void secondParameterValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }

        private void thirdParameterValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            valueSecure(sender, e);
        }
    }
}
