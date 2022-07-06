using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierPointForm : Form
    {
        public MollierPointForm()
        {
            InitializeComponent();
        }

        private void TextBox_DryBulbTemperature_TextChanged(object sender, EventArgs e)
        {

        }
        public MollierPoint get_point(double pressure)
        {
            if(!Core.Query.TryConvert(TextBox_DryBulbTemperature.Text, out double dryBulbTemperature))
            {
                return null;
            }

            if (!Core.Query.TryConvert(TextBox_HumidityRatio.Text, out double humidtyRatio))
            {
                return null;
            }

            return new MollierPoint(dryBulbTemperature, humidtyRatio / 1000, pressure);
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();

        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {

            DialogResult = DialogResult.Cancel;
            Close();
        }
        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
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
    }
}
