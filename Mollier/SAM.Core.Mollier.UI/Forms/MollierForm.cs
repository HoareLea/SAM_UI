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
            List<MollierPoint> mollierPoints = new List<MollierPoint>();
            mollierPoints.Add(new MollierPoint(20, 0.01, 101325));
            MollierControl_Main.Add_points(mollierPoints);
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

        private void density_box_CheckedChanged(object sender, EventArgs e)
        {
            MollierControl_Main.Density_line = density_box.Checked;
        }


        private void enthalpy_box_CheckedChanged(object sender, EventArgs e)
        {
            MollierControl_Main.Enthalpy_line = enthalpy_box.Checked;
        }

        private void specific_volume_box_CheckedChanged(object sender, EventArgs e)
        {
            MollierControl_Main.Specific_volume_line = specific_volume_box.Checked;
        }

        private void wet_bulb_temperature_box_CheckedChanged(object sender, EventArgs e)
        {
            MollierControl_Main.Wet_bulb_temperature_line = wet_bulb_temperature_box.Checked;
        }

        private void Add_new_point_Click(object sender, EventArgs e)
        {
            MollierPoint mollierPoint = null;

            using (Forms.MollierPointForm mollierPointForm = new Forms.MollierPointForm())
            {
                DialogResult dialogResult = mollierPointForm.ShowDialog();
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }

                mollierPoint = mollierPointForm.get_point(MollierControl_Main.Pressure);
            }

            MollierControl_Main.Add_points(new MollierPoint[] { mollierPoint });

        }
    }
}
