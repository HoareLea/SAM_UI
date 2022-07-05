using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierForm : Form
    {
        public MollierForm()
        {
            InitializeComponent();
        }

        private void MollierForm_Load(object sender, EventArgs e)
        {

        }

        private void textBox_pressure_TextChanged(object sender, EventArgs e)
        {
            if (!Core.Query.TryConvert(textBox_pressure.Text, out double pressure))
            {
                return;
            }

            if (pressure < 90000 || pressure > 110000)
            {
                return;
            }

            MollierControl_Main.Pressure = pressure;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MollierControl_Main.ChartType = ChartType.Mollier;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MollierControl_Main.ChartType = ChartType.Psychrometric;
        }
    }
}
