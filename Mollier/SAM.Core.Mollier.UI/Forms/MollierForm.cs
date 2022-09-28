using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierForm : Form
    {
        private static string mollierControlSettingsPath = System.IO.Path.Combine(Core.Query.UserSAMTemporaryDirectory(), typeof(MollierControlSettings).Name);
        private static string mollierControlPath = System.IO.Path.Combine(Core.Query.UserSAMTemporaryDirectory(), typeof(Control).Name);
        private ToolTip toolTip = new ToolTip();
        private void MollierForm_Load(object sender, EventArgs e)
        {
            ColorPointComboBox.Text = "Enthalpy";
        }

        //Chart's initialization and reset to default values
        public MollierForm()
        {
            InitializeComponent();

            MollierControlSettings mollierControlSettings = System.IO.File.Exists(mollierControlSettingsPath) ? Convert.ToSAM<MollierControlSettings>(mollierControlSettingsPath).FirstOrDefault() : null ;
            default_chart(mollierControlSettings);
        }
        private void resetChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings= new MollierControlSettings();
            mollierControlSettings.Pressure = MollierControl_Main.MollierControlSettings.Pressure;
            default_chart(mollierControlSettings);
        }
      
        //operation of changing pressure or elevation value
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
            
            TextBox_Elevation.TextChanged -= new EventHandler(TextBox_Elevation_TextChanged);
            TextBox_Elevation.Text = mollierControlSettings.Elevation.ToString();
            TextBox_Elevation.TextChanged += new EventHandler(TextBox_Elevation_TextChanged);
            
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

            TextBox_Pressure.TextChanged -= new EventHandler(TextBox_Pressure_TextChanged);
            TextBox_Pressure.Text = mollierControlSettings.Pressure.ToString();
            TextBox_Pressure.TextChanged += new EventHandler(TextBox_Pressure_TextChanged);
            

            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        //pressure changing
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

        //opening stripmenu's or saving the graph data
        private void ToolStripMenuItem_OpenSettings_Click(object sender, EventArgs e)
        {
            using (MollierControlSettingsForm mollierSettingsForm = new MollierControlSettingsForm(MollierControl_Main))
            {
                if (mollierSettingsForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                //TextBox_Pressure.Text = MollierControl_Main.MollierControlSettings.Pressure.ToString();
                //TextBox_Elevation.Text = MollierControl_Main.MollierControlSettings.Elevation.ToString();
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
        public bool Clear()
        {
           return MollierControl_Main.Clear();
        }

        public void SaveAs(string path)
        {
            MollierControl_Main.Save(ChartExportType.EMF, path: path);
        }
        
        //operation of the add process and add point buttons
        private void Button_AddPoint_Click(object sender, EventArgs e)
        {
            MollierPoint mollierPoint = null;
            
            using (Forms.MollierPointForm mollierPointForm = new Forms.MollierPointForm())
            {
                mollierPointForm.SelectPointClicked += MollierPointForm_SelectPointClicked;
                DialogResult dialogResult = mollierPointForm.ShowDialog();
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }
                mollierPoint = mollierPointForm.get_point();
            }
            if(mollierPoint == null)
            {
                return;
            }
            MollierControl_Main.AddPoints(new MollierPoint[] { mollierPoint });

        }

        private void MollierPointForm_SelectPointClicked(object sender, EventArgs e)
        {
            Forms.MollierPointForm mollierPointForm = (Forms.MollierPointForm)sender;
            mollierPointForm.Visible = false;


            MollierPoint mollierPoint = MollierControl_Main.SelectPoint();

           // mollierPointForm.MollierPoint = mollierPoint;
        }

        private void Button_AddProcess_Click(object sender, EventArgs e)
        {
            UIMollierProcess UI_MollierProcess = null;
            using (Forms.MollierProcessForm mollierProcessForm = new Forms.MollierProcessForm(MollierControl_Main))    
            {
                DialogResult dialogResult = mollierProcessForm.ShowDialog();
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }
                UI_MollierProcess = mollierProcessForm.UI_MollierProcess;
            }
            if(UI_MollierProcess == null)
            {
                return;
            }
            List<IMollierProcess> mollierProcesses = new List<IMollierProcess>();
            mollierProcesses.Add(UI_MollierProcess);
            AddProcesses(mollierProcesses);            
        }

        //disable some function for data reading only
        public bool ReadOnly
        {
            set
            {
                TextBox_Pressure.ReadOnly = value;
                TextBox_Elevation.ReadOnly = value;
                Button_AddPoint.Visible = !value;
                Button_AddProcess.Visible = !value;
            }
        }

        //adding processes or points to the chart
        //public bool AddProcess(IMollierProcess mollierProcess, bool checkPressure = true)
        //{
        //    if(mollierProcess == null)
        //    {
        //        return false;
        //    }
        //    MollierControl_Main.AddProcess(mollierProcess, checkPressure);
        //    return true;
        //}
        public bool AddProcesses(IEnumerable<IMollierProcess> mollierProcesses, bool checkPressure = true)
        {
            if(mollierProcesses == null)
            {
                return false;
            }
            MollierControl_Main.AddProcesses(mollierProcesses, checkPressure);
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

        //function that sets all values from the control to the Form 
        public void default_chart(MollierControlSettings mollierControlSettings)
        {
            if (mollierControlSettings == null)
            {
                mollierControlSettings = new MollierControlSettings();
            }
            ChartToolStripMenuItem_Mollier.Checked = mollierControlSettings.ChartType == ChartType.Mollier;
            ChartToolStripMenuItem_Psychrometric.Checked = mollierControlSettings.ChartType == ChartType.Psychrometric;
            ToolStripMenuItem_Density.Checked = mollierControlSettings.Density_line;
            ToolStripMenuItem_Enthalpy.Checked = mollierControlSettings.Enthalpy_line;
            ToolStripMenuItem_SpecificVolume.Checked = mollierControlSettings.SpecificVolume_line;
            ToolStripMenuItem_WetBulbTemperature.Checked = mollierControlSettings.WetBulbTemperature_line;
            defaultToolStripMenuItem.Checked = mollierControlSettings.Color == "default";
            blueToolStripMenuItem.Checked = mollierControlSettings.Color == "blue";
            grayToolStripMenuItem.Checked = mollierControlSettings.Color == "gray";
            blueBlackToolStripMenuItem.Checked = mollierControlSettings.Color == "blue-black";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        //buttons which enable to change color, chart or disable line
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
            if (blueBlackToolStripMenuItem.Checked)
            {
                blueBlackToolStripMenuItem.Checked = false;
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
            if (blueBlackToolStripMenuItem.Checked)
            {
                blueBlackToolStripMenuItem.Checked = false;
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
            if (blueBlackToolStripMenuItem.Checked)
            {
                blueBlackToolStripMenuItem.Checked = false;
            }
            defaultToolStripMenuItem.Checked = true;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Color = "default";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }
        private void blueBlackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (blueToolStripMenuItem.Checked)
            {
                blueToolStripMenuItem.Checked = false;
            }
            if (grayToolStripMenuItem.Checked)
            {
                grayToolStripMenuItem.Checked = false;
            }
            if (defaultToolStripMenuItem.Checked)
            {
                defaultToolStripMenuItem.Checked = false;
            }
            blueBlackToolStripMenuItem.Checked = true;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Color = "blue-black";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }
        private void ChartToolStripMenuItem_Mollier_Click(object sender, EventArgs e)
        {
            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                return;
            }
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
            if(ChartToolStripMenuItem_Psychrometric.Checked)
            {
                return;
            }
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
        private void ToolStripMenuItem_WetBulbTemperature_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem_WetBulbTemperature.Checked = !ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.WetBulbTemperature_line = ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        public MollierControlSettings MollierControlSettings
        {
            get
            {
                return MollierControl_Main.MollierControlSettings;
            }

            set
            {
                MollierControl_Main.MollierControlSettings = value;
            }
        }

        private void CheckBox_Zone_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBox_Zone.Checked)
            {
                List<MollierControlZone> mollierZones = Query.MollierZones();
                foreach (MollierZone zone in mollierZones)
                {
                    MollierControl_Main.AddZone(zone);
                }
            }
            else
            {
                MollierControl_Main.AddZone(null);
            }
        }

        private void saveAsJPGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save(ChartExportType.JPG);
        }

        private void PdfA3_PortraitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save(ChartExportType.PDF, PageSize.A3, PageOrientation.Portrait);
        }

        private void PdfA3_LandscapeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save(ChartExportType.PDF, PageSize.A3, PageOrientation.Landscape);
        }

        private void a4PortraitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save(ChartExportType.PDF, PageSize.A4, PageOrientation.Portrait);
        }

        private void a4LandscapeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Save(ChartExportType.PDF, PageSize.A4, PageOrientation.Landscape);
        }
        private void PointsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (PointsCheckBox.Checked)
            {
                mollierControlSettings.FindPoint = true;
                mollierControlSettings.Percent = 0.4;
                mollierControlSettings.FindPointType = "Enthalpy";
            }
            else
            {
                mollierControlSettings.FindPoint = false;
            }
            PercentPointsTextBox.Visible = !PercentPointsTextBox.Visible;
            PointsLabel.Visible = !PointsLabel.Visible;
            ColorPointComboBox.Visible = !ColorPointComboBox.Visible;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;

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
        private void PercentPointsTextBox_TextChanged(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (Core.Query.TryConvert(PercentPointsTextBox.Text, out double value))
            {
                mollierControlSettings.Percent = value;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            
        }

        private void ColorPointComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.FindPointType = ColorPointComboBox.Text;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }


        private void DivisionAreaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (DivisionAreaCheckBox.Checked)
            {
                if (!MollierControl_Main.HasMollierPoints)
                {
                    MessageBox.Show("There are no points");
                    DivisionAreaCheckBox.Checked = false;
                    return;
                }
                mollierControlSettings.DivisionArea = true;
                DivisionAreaLabels_CheckBox.Visible = true;
            }
            else
            {

                mollierControlSettings.DivisionArea = false;
                DivisionAreaLabels_CheckBox.Visible = false;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void DivisionAreaLabels_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (DivisionAreaLabels_CheckBox.Checked)
            {
                mollierControlSettings.DivisionAreaLabels = false;
            }
            else
            {
                mollierControlSettings.DivisionAreaLabels = true;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
        }

        private void saveAsEMFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MollierControl_Main.Size = new Size(100, 100);
            //Refresh();
            SaveAs(null);
        }

        private void openFromJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                path = openFileDialog.FileName;
            }

            if(string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            List<IMollierObject> mollierObjects = Convert.ToSAM<IMollierObject>(path);
            if(mollierObjects == null || mollierObjects.Count == 0)
            {
                return;
            }
            bool replace = false;
            
            using (Forms.OpenJSONForm openJSONForm = new Forms.OpenJSONForm())
            {
                //DialogResult dialogResult = openJSONForm.ShowDialog();
                if(openJSONForm.ShowDialog() == DialogResult.None)
                {
                    return;
                }
                replace = openJSONForm.ReplaceOrMerge();
            }

            if (replace)
            {
                Clear();
                MollierControlSettings mollierControlSettings = System.IO.File.Exists(path) ? Convert.ToSAM<MollierControlSettings>(path).Find(x => x != null) : null;
                if(mollierControlSettings != null)
                {
                    MollierControl_Main.MollierControlSettings = mollierControlSettings;
                    default_chart(mollierControlSettings);
                }
            }
            LoadMollierObjects(mollierObjects);
    
        }

        private void exportToJSONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<IJSAMObject> mollierObjects = new List<IJSAMObject>();

            List<UIMollierProcess> uIMollierProcesses = MollierControl_Main.UIMollierProcesses;
            if(uIMollierProcesses != null)
            {
                mollierObjects.AddRange(uIMollierProcesses.Cast<IMollierObject>());
            }

            List<MollierPoint> mollierPoints = MollierControl_Main.MollierPoints;
            if (mollierPoints != null)
            {
                mollierObjects.AddRange(mollierPoints.Cast<IMollierObject>());
            }

            string path = null;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = MollierControl_Main.MollierControlSettings.ChartType == ChartType.Mollier ? "Mollier.json" : "Psychrometric.json";
                if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                path = saveFileDialog.FileName;
            }

            if(string.IsNullOrWhiteSpace(path))
            {
                return;    
            }

            mollierObjects.Add(MollierControlSettings);
            Convert.ToFile(mollierObjects, path);
        }

        public void LoadMollierObjects(IEnumerable<IMollierObject> mollierObjects)
        {

            if(mollierObjects == null || mollierObjects.Count() == 0)
            {
                return;
            }

            List<IMollierProcess> mollierProcesses = new List<IMollierProcess>();
            List<MollierPoint> mollierPoints = new List<MollierPoint>();
            foreach(IMollierObject mollierObject in mollierObjects)
            {
                if(mollierObject is IMollierProcess)
                {
                    mollierProcesses.Add((IMollierProcess)mollierObject);
                }
                else if(mollierObject is MollierPoint)
                {
                    mollierPoints.Add((MollierPoint)mollierObject);
                }
            }

            double pressure = Query.DefaultPressure(mollierPoints, mollierProcesses);

            Pressure = double.IsNaN(pressure) ? Standard.Pressure : pressure;

            AddProcesses(mollierProcesses, false);
            AddPoints(mollierPoints, false);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.Print();
        }
        private void PercentPointsTextBox_MouseHover(object sender, EventArgs e)
        {
            toolTip.Show("0 is the highest value, 100 is the lowest value", PercentPointsTextBox);
            toolTip.Active = true;
        }

        private void PercentPointsTextBox_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Active = false;
        }

        private void MollierForm_ResizeEnd(object sender, EventArgs e)
        {
            MollierControl_Main.generate_graph();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.ClearObjects();
        }
    }
}
