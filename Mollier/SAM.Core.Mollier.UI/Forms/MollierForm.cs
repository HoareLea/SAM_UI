using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierForm : Form
    {
        private static string mollierControlSettingsPath = System.IO.Path.Combine(SAM.Core.Query.UserSAMTemporaryDirectory(), typeof(MollierControlSettings).Name);

        public MollierForm()
        {
            InitializeComponent();
        }

        private void MollierForm_Load(object sender, EventArgs e)
        {
            if(System.IO.File.Exists(mollierControlSettingsPath))
            {
                MollierControlSettings mollierControlSettings = Convert.ToSAM<MollierControlSettings>(mollierControlSettingsPath).FirstOrDefault();
                if (mollierControlSettings != null)
                {
                    TextBox_Pressure.Text = mollierControlSettings.Pressure.ToString();
                    //TODO: add checkboxes
                    MollierControl_Main.MollierControlSettings = mollierControlSettings;
                }
            }
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
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Pressure = pressure;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
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

                mollierPoint = mollierPointForm.get_point(MollierControl_Main.MollierControlSettings.Pressure);
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

            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                mollierControlSettings.ChartType = ChartType.Mollier; 
            }
            else if (ChartToolStripMenuItem_Psychrometric.Checked)
            {
                mollierControlSettings.ChartType = ChartType.Psychrometric;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ChartToolStripMenuItem_Psychrometric_Click(object sender, EventArgs e)
        {
            ChartToolStripMenuItem_Psychrometric.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;
            ChartToolStripMenuItem_Mollier.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;

            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                mollierControlSettings.ChartType = ChartType.Mollier;
            }
            else if (ChartToolStripMenuItem_Psychrometric.Checked)
            {
                mollierControlSettings.ChartType = ChartType.Psychrometric;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_Density_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Density.Checked = !ToolStripMenuItem_Density.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.density_line = ToolStripMenuItem_Density.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_WetBulbTemperature_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem_WetBulbTemperature.Checked = !ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.wetBulbTemperature_line = ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_Enthalpy_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Enthalpy.Checked = !ToolStripMenuItem_Enthalpy.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.enthalpy_line = ToolStripMenuItem_Enthalpy.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_SpecificVolume_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_SpecificVolume.Checked = !ToolStripMenuItem_SpecificVolume.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.specificVolume_line = ToolStripMenuItem_SpecificVolume.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
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

        public bool AddPoints(IEnumerable<MollierPoint> mollierPoints)
        {
            if (mollierPoints == null)
            {
                return false;
            }

            MollierControl_Main.AddPoints(mollierPoints);
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
        private void ToolStripMenuItem_OpenSettings_Click(object sender, EventArgs e)
        {
            using (MollierControlSettingsForm mollierSettingsForm = new MollierControlSettingsForm(MollierControl_Main))
            {
                if (mollierSettingsForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                SaveMollierControlSettings();
            }
        }

        private void SaveMollierControlSettings()
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (mollierControlSettings != null)
            {
                string directoryPath = System.IO.Path.GetDirectoryName(mollierControlSettingsPath);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                }

                Convert.ToFile(mollierControlSettings, mollierControlSettingsPath);
            }
        }

        private void MollierForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMollierControlSettings();
        }

        private void TextBox_Elevation_TextChanged(object sender, EventArgs e)
        {

        }

        private void ToolStripMenuItem_Settings_Click(object sender, EventArgs e)
        {

        }
    }
}
