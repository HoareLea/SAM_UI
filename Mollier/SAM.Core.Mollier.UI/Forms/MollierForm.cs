using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierForm : Form
    {
        private static string mollierControlSettingsPath = System.IO.Path.Combine(Core.Query.UserSAMTemporaryDirectory(), typeof(MollierControlSettings).Name);

        public MollierForm()    
        {
            InitializeComponent();

            if (System.IO.File.Exists(mollierControlSettingsPath))
            {
                MollierControlSettings mollierControlSettings = Convert.ToSAM<MollierControlSettings>(mollierControlSettingsPath).FirstOrDefault();
                if (mollierControlSettings != null)
                {
                    TextBox_Pressure.Text = mollierControlSettings.Pressure.ToString();
                    TextBox_Elevation.Text = mollierControlSettings.Elevation.ToString();
                    ChartToolStripMenuItem_Mollier.Checked = mollierControlSettings.ChartType == ChartType.Mollier;
                    ChartToolStripMenuItem_Psychrometric.Checked = mollierControlSettings.ChartType == ChartType.Psychrometric;
                    ToolStripMenuItem_Density.Checked = mollierControlSettings.Density_line;
                    ToolStripMenuItem_Enthalpy.Checked = mollierControlSettings.Enthalpy_line;
                    ToolStripMenuItem_SpecificVolume.Checked = mollierControlSettings.SpecificVolume_line;
                    ToolStripMenuItem_WetBulbTemperature.Checked = mollierControlSettings.WetBulbTemperature_line;
                    defaultToolStripMenuItem.Checked = mollierControlSettings.Color == "default";
                    blueToolStripMenuItem.Checked = mollierControlSettings.Color == "blue"; 
                    grayToolStripMenuItem.Checked = mollierControlSettings.Color == "gray";
                    MollierControl_Main.MollierControlSettings = mollierControlSettings;
                }
            }
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

            if (UI.Controls.MollierControl.MinPressure > pressure || pressure > UI.Controls.MollierControl.MaxPressure)
            {
                return;
            }
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Pressure = pressure;
            mollierControlSettings.Elevation = System.Math.Round(Core.Query.Calculate_BinarySearch(x => Mollier.Query.Pressure(x), pressure, -1000, 5000));
            TextBox_Pressure.Text = mollierControlSettings.Pressure.ToString();
            
            TextBox_Elevation.TextChanged -= new EventHandler(this.TextBox_Elevation_TextChanged);
            TextBox_Elevation.Text = mollierControlSettings.Elevation.ToString();
            TextBox_Elevation.TextChanged += new EventHandler(this.TextBox_Elevation_TextChanged);
            
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }
        private void TextBox_Elevation_TextChanged(object sender, EventArgs e)
        {
            if (!Core.Query.TryConvert(TextBox_Elevation.Text, out double elevation))
            {
                return;
            }

            if (elevation < -1000 || elevation > 5000)
            {
                return;
            }

            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Elevation = elevation;
            mollierControlSettings.Pressure = System.Math.Round(Mollier.Query.Pressure(elevation));
            TextBox_Elevation.Text = mollierControlSettings.Elevation.ToString();

            TextBox_Pressure.TextChanged -= new EventHandler(this.TextBox_Pressure_TextChanged);
            TextBox_Pressure.Text = mollierControlSettings.Pressure.ToString();
            TextBox_Pressure.TextChanged += new EventHandler(this.TextBox_Pressure_TextChanged);
            

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
            mollierControlSettings.Density_line = ToolStripMenuItem_Density.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_WetBulbTemperature_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem_WetBulbTemperature.Checked = !ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.WetBulbTemperature_line = ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_Enthalpy_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Enthalpy.Checked = !ToolStripMenuItem_Enthalpy.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Enthalpy_line = ToolStripMenuItem_Enthalpy.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void ToolStripMenuItem_SpecificVolume_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_SpecificVolume.Checked = !ToolStripMenuItem_SpecificVolume.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.SpecificVolume_line = ToolStripMenuItem_SpecificVolume.Checked;
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
                TextBox_Elevation.ReadOnly = value;
                Button_AddPoint.Visible = !value;
            }
        }

        public double Pressure
        {
            get 
            {
                return MollierControl_Main.MollierControlSettings.Pressure;
            }
            set
            {
                MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
                mollierControlSettings.Pressure = value;
                TextBox_Pressure.Text = value.ToString();
                MollierControl_Main.MollierControlSettings = mollierControlSettings;
            }
        }
        public bool AddProcess(IMollierProcess mollierProcess, bool checkPressure = true)
        {
            if(mollierProcess == null)
            {
                return false;
            }
            MollierControl_Main.AddProcess(mollierProcess, checkPressure);
            return true;
        }

        public bool AddPoints(IEnumerable<MollierPoint> mollierPoints, bool checkPressure = true)
        {
            if (mollierPoints == null)
            {
                return false;
            }

            MollierControl_Main.AddPoints(mollierPoints, checkPressure);
            return true;
        }

        public bool Clear()
        {
           return MollierControl_Main.Clear();
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (defaultToolStripMenuItem.Checked)
            {
                defaultToolStripMenuItem.Checked = false;
            }
            if (grayToolStripMenuItem.Checked)
            {
                grayToolStripMenuItem.Checked = false;
            }
            blueToolStripMenuItem.Checked = true;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Color = "blue";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void grayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (defaultToolStripMenuItem.Checked)
            {
                defaultToolStripMenuItem.Checked = false;
            }
            if (blueToolStripMenuItem.Checked)
            {
                blueToolStripMenuItem.Checked = false;
            }
            grayToolStripMenuItem.Checked = true;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Color = "gray";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (blueToolStripMenuItem.Checked)
            {
                blueToolStripMenuItem.Checked = false;
            }
            if (grayToolStripMenuItem.Checked)
            {
                grayToolStripMenuItem.Checked = false;
            }
            defaultToolStripMenuItem.Checked = true;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Color = "default";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
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
                    TextBox_Pressure.Text = MollierControl_Main.MollierControlSettings.Pressure.ToString();
                    TextBox_Elevation.Text = MollierControl_Main.MollierControlSettings.Elevation.ToString();
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


        private void ToolStripMenuItem_Settings_Click(object sender, EventArgs e)
        {

        }

        private void ToolStripMenuItem_SetElevation_Click(object sender, EventArgs e)
        {
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Set elevation", "Elevation [m]:"))
            {
                textBoxForm.Value = 0;
                if(textBoxForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                double pressure = System.Math.Round(Mollier.Query.Pressure(textBoxForm.Value));


                TextBox_Pressure.Text = pressure.ToString();
            }
        }

    }
}
