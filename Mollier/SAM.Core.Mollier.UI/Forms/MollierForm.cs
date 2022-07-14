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

        private void TextBox_Pressure_TextChanged(object sender, EventArgs e)
        {
            if (!Core.Query.TryConvert(TextBox_Pressure.Text, out double pressure))
            {
                return;
            }

            if (pressure < 90000 || pressure > 110000)
            {
                return;
            }

            MollierControl_Main.Pressure = pressure;
        }

        private void Button_AddPoint_Click(object sender, EventArgs e)
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

            MollierControl_Main.AddPoints(new MollierPoint[] { mollierPoint });

        }

        private void ToolStripMenuItem_Save_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save();
        }

        private void ChartToolStripMenuItem_Mollier_Click(object sender, EventArgs e)
        {
            ChartToolStripMenuItem_Mollier.Checked = !ChartToolStripMenuItem_Mollier.Checked;
            ChartToolStripMenuItem_Psychrometric.Checked = !ChartToolStripMenuItem_Mollier.Checked;

            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                MollierControl_Main.ChartType = ChartType.Mollier;
            }
            else if (ChartToolStripMenuItem_Psychrometric.Checked)
            {
                MollierControl_Main.ChartType = ChartType.Psychrometric;
            }
        }

        private void ChartToolStripMenuItem_Psychrometric_Click(object sender, EventArgs e)
        {
            ChartToolStripMenuItem_Psychrometric.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;
            ChartToolStripMenuItem_Mollier.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;

            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                MollierControl_Main.ChartType = ChartType.Mollier;
            }
            else if (ChartToolStripMenuItem_Psychrometric.Checked)
            {
                MollierControl_Main.ChartType = ChartType.Psychrometric;
            }
        }

        private void ToolStripMenuItem_Density_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Density.Checked = !ToolStripMenuItem_Density.Checked;
            MollierControl_Main.Density_line = ToolStripMenuItem_Density.Checked;
        }

        private void ToolStripMenuItem_WetBulbTemperature_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_WetBulbTemperature.Checked = !ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControl_Main.Wet_bulb_temperature_line = ToolStripMenuItem_WetBulbTemperature.Checked;
        }

        private void ToolStripMenuItem_Enthalpy_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Enthalpy.Checked = !ToolStripMenuItem_Enthalpy.Checked;
            MollierControl_Main.Enthalpy_line = ToolStripMenuItem_Enthalpy.Checked;
        }

        private void ToolStripMenuItem_SpecificVolume_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_SpecificVolume.Checked = !ToolStripMenuItem_SpecificVolume.Checked;
            MollierControl_Main.Specific_volume_line = ToolStripMenuItem_SpecificVolume.Checked;
        }

        private void ToolStripMenuItem_File_Click(object sender, EventArgs e)
        {

        }

        public bool ReadOnly
        {
            set
            {
                TextBox_Pressure.ReadOnly = value;
                Button_AddPoint.Visible = !value;
            }
        }

        public bool AddProcess(IMollierProcess mollierProcess)
        {
            if(mollierProcess == null)
            {
                return false;
            }

            MollierControl_Main.AddProcess(mollierProcess);
            return true;
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (defaultToolStripMenuItem.Checked)
            {
                defaultToolStripMenuItem.Checked = !defaultToolStripMenuItem.Checked;
                blueToolStripMenuItem.Checked = !blueToolStripMenuItem.Checked;
                MollierControl_Main.Blue_Color = "blue";
            }
            if (grayToolStripMenuItem.Checked)
            {
                grayToolStripMenuItem.Checked = !grayToolStripMenuItem.Checked;
                blueToolStripMenuItem.Checked = !blueToolStripMenuItem.Checked;
                MollierControl_Main.Blue_Color = "blue";
            }
        }

        private void grayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (defaultToolStripMenuItem.Checked)
            {
                defaultToolStripMenuItem.Checked = !defaultToolStripMenuItem.Checked;
                grayToolStripMenuItem.Checked = !grayToolStripMenuItem.Checked;
                MollierControl_Main.Gray_Color = "gray";
            }
            if (blueToolStripMenuItem.Checked)
            {
                blueToolStripMenuItem.Checked = !blueToolStripMenuItem.Checked;
                grayToolStripMenuItem.Checked = !grayToolStripMenuItem.Checked;
                MollierControl_Main.Gray_Color = "gray";
            }
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (blueToolStripMenuItem.Checked)
            {
                blueToolStripMenuItem.Checked = !blueToolStripMenuItem.Checked;
                defaultToolStripMenuItem.Checked = !defaultToolStripMenuItem.Checked;
                MollierControl_Main.Default_Color = "default";
            }
            if (grayToolStripMenuItem.Checked)
            {
                grayToolStripMenuItem.Checked = !grayToolStripMenuItem.Checked;
                defaultToolStripMenuItem.Checked = !defaultToolStripMenuItem.Checked;
                MollierControl_Main.Gray_Color = "default";
            }
        }
        private void graphSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        

        //private void TemperatureMaximumValueTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(TemperatureMaximumValueTextbox.Text, out int temperature_Max))
        //    {
        //        return;
        //    }
        //    if (temperature_Max > 50 || temperature_Max < -20)
        //    {
        //        return;
        //    }
        //    MollierControl_Main.Temperature_Max = temperature_Max;
        //}

        //private void TemperatureMinimumValueTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(TemperatureMinimumValueTextbox.Text, out int temperature_Min))
        //    {
        //        return;
        //    }
        //   if (temperature_Min < -20 || temperature_Min > 50)
        //    {
        //       return;
        //    }
        //    MollierControl_Main.Temperature_Min = temperature_Min;
        //}

        //private void TemperatureIntervalTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(TemperatureIntervalTextbox.Text, out int temperature_interval))
        //    {
        //        return;
        //    }
 
        //    MollierControl_Main.Temperature_Interval = temperature_interval;
        //}

        //private void HumidityRatioMinimumValueTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(HumidityRatioMinimumValueTextbox.Text, out int humidityRatio_Min))
        //    {
        //        return;
        //    }
        //    if (humidityRatio_Min < -20 || humidityRatio_Min > 50)
        //    {
        //        return;
        //    }

        //    MollierControl_Main.HumidityRatio_Min = humidityRatio_Min;
        //}

        //private void HumidityRatioMaximumValueTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(HumidityRatioMaximumValueTextbox.Text, out int humidityRatio_Max))
        //    {
        //        return;
        //    }
        //    if (humidityRatio_Max > 50 || humidityRatio_Max < -20)
        //    {
        //        return;
        //    }
        //    MollierControl_Main.HumidityRatio_Max = humidityRatio_Max;
        //}

        //private void HumidityRatioIntervalTextbox_TextChanged(object sender, EventArgs e)
        //{
        //    if (!Core.Query.TryConvert(HumidityRatioIntervalTextbox.Text, out int humidityRatio_Interval))
        //    {
        //        return;
        //    }
            
        //    MollierControl_Main.HumidityRatio_Interval = humidityRatio_Interval;
        //}

        private void ToolStripMenuItem_OpenSettings_Click(object sender, EventArgs e)
        {
            using (MollierSettingsForm mollierSettingsForm = new MollierSettingsForm(MollierControl_Main))
            {
                mollierSettingsForm.HumidityRatio_Max = MollierControl_Main.HumidityRatio_Max;


                if (mollierSettingsForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }
        }
    }
}
