using SAM.Geometry.Mollier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public partial class MollierForm : Form
    {
        private static string mollierControlSettingsPath = System.IO.Path.Combine(Core.Query.UserSAMTemporaryDirectory(), typeof(MollierControlSettings).Name);

        private ToolTip toolTip = new ToolTip();

        private Forms.MollierPointForm mollierPointForm = null;
        private Forms.MollierProcessForm mollierProcessForm = null;
        private Forms.UIMollierObjectsForm manageMollierObjectsForm = null;

        private UIMollierPoint previousUIMollierPoint = null;

        public event MollierPointSelectedEventHandler MollierPointSelected;

        private List<int> customColors = new List<int>();

        public MollierForm()
        {
            InitializeComponent();

            LoadMollierControlSettings();

            ColorPointComboBox.Text = "Enthalpy";
        }

        private void MollierForm_Load(object sender, EventArgs e)
        {
            MollierControl_Main.MollierPointSelected += MollierControl_Main_MollierPointSelected;

            MollierControl_Main.SizeChanged += MollierControl_Main_SizeChanged;

            Shown += MollierForm_Shown;

            //MollierControl_Main.Regenerate();
        }

        private void MollierControl_Main_SizeChanged(object sender, EventArgs e)
        {
            MollierControl_Main.Regenerate();
        }

        private void MollierControl_Main_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            if(mollierProcessForm != null)
            {
                mollierProcessForm.Show();
            }

            MollierPointSelected?.Invoke(this, e);
        }

        private void resetChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reset();
        }
      
        private void TextBox_Pressure_TextChanged(object sender, EventArgs e)
        {
           if (!Core.Query.TryConvert(TextBox_Pressure.Text, out double pressure))
            {
                return;
            }

            if (Limit.Pressure_Min > pressure || pressure > Limit.Pressure_Max)
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

        private void ToolStripMenuItem_OpenSettings_Click(object sender, EventArgs e)
        {
            using (MollierControlSettingsForm mollierSettingsForm = new MollierControlSettingsForm(MollierControl_Main))
            {
                mollierSettingsForm.ApplyClicked += MollierSettingsForm_ApplyClicked;
                mollierSettingsForm.CustomColors = customColors;

                if (mollierSettingsForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                customColors = mollierSettingsForm.CustomColors;

                SaveMollierControlSettings();
            }
        }

        private void MollierSettingsForm_ApplyClicked(object sender, EventArgs e)
        {
            MollierControlSettingsForm mollierControlSettingsForm = sender as MollierControlSettingsForm;
            if(mollierControlSettingsForm == null)
            {
                return;
            }

            LoadMollierControlSettings(MollierControl_Main.MollierControlSettings);
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

                Core.Convert.ToFile(mollierControlSettings, mollierControlSettingsPath);
            }
        }

        private void MollierForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMollierControlSettings();
        }
        
        public bool Clear()
        {
            bool clear = MollierControl_Main.ClearObjects(false);

            if (manageMollierObjectsForm != null)
            {
                manageMollierObjectsForm.Refresh(MollierControl_Main.MollierModel);
            }

            return clear;
        }

        public void SaveAs(string path)
        {
            MollierControl_Main.Save(ChartExportType.EMF, path: path);
        }
        
        private MollierPoint GetMollierPoint()
        {
            if(!Core.Query.TryConvert(TextBox_Pressure.Text, out double pressure))
            {
                pressure = 101235;
            }

            double dryBulbTemperature = 35;
            double relativeHumidity = 50;
            double humidityRatio = double.NaN;

            if(previousUIMollierPoint != null && previousUIMollierPoint != null && previousUIMollierPoint.IsValid())
            {
                if(!double.IsNaN(previousUIMollierPoint.DryBulbTemperature))
                {
                    dryBulbTemperature = previousUIMollierPoint.DryBulbTemperature;
                }

                if (!double.IsNaN(previousUIMollierPoint.HumidityRatio))
                {
                    humidityRatio = previousUIMollierPoint.HumidityRatio;
                }
            }

            return double.IsNaN(humidityRatio) ? Mollier.Create.MollierPoint_ByRelativeHumidity(dryBulbTemperature, relativeHumidity, pressure) : new MollierPoint(dryBulbTemperature, humidityRatio, pressure);
        }

        private void ShowMollier()
        {
            if (ChartToolStripMenuItem_Mollier.Checked)
            {
                return;
            }
            ChartToolStripMenuItem_Mollier.Checked = !ChartToolStripMenuItem_Mollier.Checked;
            ChartToolStripMenuItem_Psychrometric.Checked = !ChartToolStripMenuItem_Mollier.Checked;


            MollierControl_Main.SizeChanged -= MollierControl_Main_SizeChanged;

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
            LoadMollierControlSettings(mollierControlSettings);

            MollierControl_Main.SizeChanged += MollierControl_Main_SizeChanged;

            MollierControl_Main.Regenerate();
        }

        private void ShowPsychrometric()
        {
            if (ChartToolStripMenuItem_Psychrometric.Checked)
            {
                return;
            }
            ChartToolStripMenuItem_Psychrometric.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;
            ChartToolStripMenuItem_Mollier.Checked = !ChartToolStripMenuItem_Psychrometric.Checked;

            MollierControl_Main.SizeChanged -= MollierControl_Main_SizeChanged;

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
            LoadMollierControlSettings(mollierControlSettings);

            MollierControl_Main.SizeChanged += MollierControl_Main_SizeChanged;

            MollierControl_Main.Regenerate();
        }

        private void Reset()
        {
            MollierControlSettings mollierControlSettings = new MollierControlSettings();
            mollierControlSettings.Pressure = MollierControl_Main.MollierControlSettings.Pressure;
            LoadMollierControlSettings(mollierControlSettings);
            MollierControl_Main.Regenerate();
        }

        private void Save()
        {
            List<IJSAMObject> mollierObjects = new List<IJSAMObject>();

            List<UIMollierProcess> uIMollierProcesses = MollierControl_Main.UIMollierObjects<UIMollierProcess>();
            if (uIMollierProcesses != null)
            {
                mollierObjects.AddRange(uIMollierProcesses.Cast<IMollierObject>());
            }

            List<UIMollierPoint> uIMollierPoints = MollierControl_Main.UIMollierObjects<UIMollierPoint>();
            if (uIMollierPoints != null)
            {
                mollierObjects.AddRange(uIMollierPoints.Cast<IMollierObject>());
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

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            mollierObjects.Add(MollierControlSettings);
            Core.Convert.ToFile(mollierObjects, path);
        }

        private void AddPoint()
        {
            using (Forms.MollierPointForm mollierPointForm = new Forms.MollierPointForm())
            {
                mollierPointForm.MollierPoint = GetMollierPoint();

                DialogResult dialogResult = mollierPointForm.ShowDialog();
                if (dialogResult != DialogResult.OK)
                {
                    return;
                }

                previousUIMollierPoint = mollierPointForm.UIMollierPoint;
                AddMollierObjects(new UIMollierPoint[] { previousUIMollierPoint });
            }
        }

        private void AddProcess()
        {
            if (mollierProcessForm == null)
            {
                mollierProcessForm = new Forms.MollierProcessForm();
                mollierProcessForm.MollierForm = this;
                mollierProcessForm.FormClosing += MollierProcessForm_FormClosing;
            }

            mollierProcessForm.PreviousMollierPoint = GetMollierPoint();
            mollierProcessForm.Show();
        }

        private void Edit()
        {
            if (manageMollierObjectsForm == null)
            {
                manageMollierObjectsForm = new Forms.UIMollierObjectsForm(MollierControl_Main.MollierModel, MollierControlSettings);

                manageMollierObjectsForm.FormClosing += ManageMollierObjectsForm_Closing;
                manageMollierObjectsForm.MollierModelEdited += ManageMollierObjectsForm_MollierModelEdited;
                manageMollierObjectsForm.MollierObjectSelected += ManageMollierObjectsForm_MollierObjectSelected;
            }
            manageMollierObjectsForm?.Show();
        }

        private void Epsilon()
        {
            MollierControl_Main.MollierPointSelected -= MollierControl_Main_MollierPointSelected_Epsilon;
            MollierControl_Main.MollierPointSelected += MollierControl_Main_MollierPointSelected_Epsilon;
        }

        private void SHR()
        {
            MollierControl_Main.MollierPointSelected -= MollierControl_Main_MollierPointSelected_SensibleHeatRatio;
            MollierControl_Main.MollierPointSelected += MollierControl_Main_MollierPointSelected_SensibleHeatRatio;
        }

        private void DivisionArea()
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if (ToolStripMenuItem_DivisionArea.Checked)
            {
                List<UIMollierPoint> mollierPoints = MollierControl_Main.UIMollierObjects<UIMollierPoint>();
                if (mollierPoints == null || mollierPoints.Count == 0)
                {
                    MessageBox.Show("There are no points");
                    ToolStripMenuItem_DivisionArea.Checked = false;
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
            MollierControl_Main.Regenerate();
        }

        private void CoolingAuxiliaryProcessesVisibility()
        {
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.DisableCoolingAuxiliaryProcesses = !mollierControlSettings.DisableCoolingAuxiliaryProcesses;

            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }

        private void MollierProcessForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UIMollierProcess uIMollierProcess = mollierProcessForm?.GetUIMollierProcess();
            if(uIMollierProcess == null)
            {
                mollierProcessForm = null;
                return;
            }

            if(mollierProcessForm.DialogResult != DialogResult.OK)
            {
                mollierProcessForm = null;
                return;
            }

            mollierProcessForm = null;

            //if(uIMollierProcess.UIMollierAppearance.Color == System.Drawing.Color.Empty)
            //{
            //    MollierControlSettings mollierControlSettings = System.IO.File.Exists(mollierControlSettingsPath) ? Core.Convert.ToSAM<MollierControlSettings>(mollierControlSettingsPath).FirstOrDefault() : null;
                
                
            //    System.Drawing.Color? color = mollierControlSettings?.VisibilitySettings?.GetColor(mollierControlSettings?.DefaultTemplateName, ChartParameterType.Line, uIMollierProcess);
            //    if(color != null && color.HasValue)
            //    {
            //        uIMollierProcess.UIMollierAppearance.Color = color.Value;
            //        if (uIMollierProcess.UIMollierPointAppearance_Start.Color == System.Drawing.Color.Empty)
            //        {
            //            uIMollierProcess.UIMollierPointAppearance_Start.Color = color.Value;
            //        }

            //        if (uIMollierProcess.UIMollierPointAppearance_End.Color == System.Drawing.Color.Empty)
            //        {
            //            uIMollierProcess.UIMollierPointAppearance_End.Color = color.Value;
            //        }
            //    }

            //}

            previousUIMollierPoint = uIMollierProcess.GetUIMollierPoint_End();
            List<IMollierProcess> mollierProcesses = new List<IMollierProcess>() { uIMollierProcess };

            AddMollierObjects(mollierProcesses);
        }

        private void MollierPointForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(mollierPointForm == null || mollierPointForm.DialogResult != DialogResult.OK)
            {
                return;
            }

            MollierPoint mollierPoint = mollierPointForm.MollierPoint;
            if(mollierPoint == null)
            {
                return;
            }

            MollierControl_Main.AddMollierObjects(new MollierPoint[] { mollierPoint });
        }

        //disable some function for data reading only
        public bool ReadOnly
        {
            set
            {
                //TextBox_Pressure.ReadOnly = value;
                //TextBox_Elevation.ReadOnly = value;
                //Button_AddPoint.Visible = !value;
                //Button_AddProcess.Visible = !value;
            }
        }

        public bool AddMollierObjects<T>(IEnumerable<T> mollierObjects, bool checkPressure = true) where T: IMollierObject
        {
            if(mollierObjects == null)
            {
                return false;
            }

            MollierControl_Main.AddMollierObjects(mollierObjects, checkPressure);
            if (manageMollierObjectsForm != null)
            {
                manageMollierObjectsForm.Refresh(MollierControl_Main.MollierModel);
            }
            return true;
        }

        //function that sets all values from the control to the Form 
        public void LoadMollierControlSettings(MollierControlSettings mollierControlSettings = null)
        {
            if (mollierControlSettings == null)
            {
                mollierControlSettings = System.IO.File.Exists(mollierControlSettingsPath) ? Core.Convert.ToSAM<MollierControlSettings>(mollierControlSettingsPath).FirstOrDefault() : new MollierControlSettings();
            }

            if(mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature) == System.Drawing.Color.Empty)
            {
                MollierControlSettings mollierControlSettings_Default = new MollierControlSettings();

                mollierControlSettings.VisibilitySettings.Add(mollierControlSettings.DefaultTemplateName, mollierControlSettings_Default.VisibilitySettings.GetVisibilitySetting(mollierControlSettings.DefaultTemplateName, ChartParameterType.Line, ChartDataType.DryBulbTemperature));
                mollierControlSettings.VisibilitySettings.Add(mollierControlSettings.DefaultTemplateName, mollierControlSettings_Default.VisibilitySettings.GetVisibilitySetting(mollierControlSettings.DefaultTemplateName, ChartParameterType.BoldLine, ChartDataType.DryBulbTemperature));
            }

            ChartToolStripMenuItem_Mollier.Checked = mollierControlSettings.ChartType == ChartType.Mollier;
            ChartToolStripMenuItem_Psychrometric.Checked = mollierControlSettings.ChartType == ChartType.Psychrometric;
            ToolStripMenuItem_Density.Checked = mollierControlSettings.Density_Line;
            ToolStripMenuItem_Enthalpy.Checked = mollierControlSettings.Enthalpy_Line;
            ToolStripMenuItem_SpecificVolume.Checked = mollierControlSettings.SpecificVolume_Line;
            ToolStripMenuItem_WetBulbTemperature.Checked = mollierControlSettings.WetBulbTemperature_Line;
            ToolStripMenuItem_PartialVapourPressure.Checked = mollierControlSettings.PartialVapourPressure_Axis;
            defaultToolStripMenuItem.Checked = mollierControlSettings.DefaultTemplateName == "default";
            blueToolStripMenuItem.Checked = mollierControlSettings.DefaultTemplateName == "blue";
            grayToolStripMenuItem.Checked = mollierControlSettings.DefaultTemplateName == "gray";
            blueBlackToolStripMenuItem.Checked = mollierControlSettings.DefaultTemplateName == "blue-black";
            if(MollierControl_Main != null)
            {
                MollierControl_Main.MollierControlSettings = mollierControlSettings;
            }

            MollierControlSettings mollierControlSettings_Temp = MollierControlSettings;
            if(mollierControlSettings_Temp != null)
            {
                TextBox_Pressure.Text = mollierControlSettings_Temp.Pressure.ToString();
            }

            int width = Size.Width;
            int height = Size.Height;
            switch (mollierControlSettings.ChartType)
            {
                case ChartType.Psychrometric:
                    width = mollierControlSettings == null || mollierControlSettings.PsychrometricWindowWidth == -1 ? Size.Width : mollierControlSettings.PsychrometricWindowWidth;
                    height = mollierControlSettings == null || mollierControlSettings.PsychrometricWindowHeight == -1 ? Size.Height : mollierControlSettings.PsychrometricWindowHeight;
                    if (mollierControlSettings.PsychrometricWindowWidth == -1 || mollierControlSettings.PsychrometricWindowHeight == -1)
                    {
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                    }

                    break;

                case ChartType.Mollier:
                    width = mollierControlSettings == null || mollierControlSettings.MollierWindowWidth == -1 ? Size.Width : mollierControlSettings.MollierWindowWidth;
                    height = mollierControlSettings == null || mollierControlSettings.MollierWindowHeight == -1 ? Size.Height : mollierControlSettings.MollierWindowHeight;
                    if (mollierControlSettings.MollierWindowWidth == -1 || mollierControlSettings.MollierWindowHeight == -1)
                    {
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                    }

                    break;
            }

            Size = new System.Drawing.Size(width, height);
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
            mollierControlSettings.DefaultTemplateName = "blue";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
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
            mollierControlSettings.DefaultTemplateName = "gray";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
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
            mollierControlSettings.DefaultTemplateName = "default";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
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
            mollierControlSettings.DefaultTemplateName = "blue-black";
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }
       
        private void ChartToolStripMenuItem_Mollier_Click(object sender, EventArgs e)
        {
            ShowMollier();
        }
        
        private void ChartToolStripMenuItem_Psychrometric_Click(object sender, EventArgs e)
        {
            ShowPsychrometric();
        }

        private void ToolStripMenuItem_Density_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Density.Checked = !ToolStripMenuItem_Density.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Density_Line = ToolStripMenuItem_Density.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }
        
        private void ToolStripMenuItem_Enthalpy_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_Enthalpy.Checked = !ToolStripMenuItem_Enthalpy.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.Enthalpy_Line = ToolStripMenuItem_Enthalpy.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }
        
        private void ToolStripMenuItem_SpecificVolume_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_SpecificVolume.Checked = !ToolStripMenuItem_SpecificVolume.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.SpecificVolume_Line = ToolStripMenuItem_SpecificVolume.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }
        
        private void ToolStripMenuItem_WetBulbTemperature_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_WetBulbTemperature.Checked = !ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.WetBulbTemperature_Line = ToolStripMenuItem_WetBulbTemperature.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }

        public MollierControlSettings MollierControlSettings
        {
            get
            {
                return MollierControl_Main?.MollierControlSettings;
            }

            set
            {
                if(MollierControl_Main != null)
                {
                    MollierControl_Main.MollierControlSettings = value;
                }
            }
        }

        private void CheckBox_Hoover_CheckedChanged(object sender, EventArgs e)
        {
            MollierControl_Main.EnableHoover = CheckBox_Hoover.Checked;
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
                List<UIMollierPoint> mollierPoints = MollierControl_Main.UIMollierObjects<UIMollierPoint>();
                if (mollierPoints == null || mollierPoints.Count < 4)
                {
                    MessageBox.Show("The minimum number of points on the chart required to run this method is 4.", "Error");
                    PointsCheckBox.Checked = false;
                    PercentPointsTextBox.Visible = false;
                    PointsLabel.Visible = false;
                    ColorPointComboBox.Visible = false;
                }
                else
                {
                    mollierControlSettings.FindPoint = true;
                    mollierControlSettings.FindPoint_Factor = 0.4;
                    mollierControlSettings.FindPointType = ChartDataType.Enthalpy;
                    PercentPointsTextBox.Visible = true;
                    PointsLabel.Visible = true;
                    ColorPointComboBox.Visible = true;
                }
            }
            else
            {
                mollierControlSettings.FindPoint = false;
                PercentPointsTextBox.Visible = false;
                PointsLabel.Visible = false;
                ColorPointComboBox.Visible = false;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();

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
                mollierControlSettings.FindPoint_Factor = value;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();

        }

        private void ColorPointComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!ColorPointComboBox.Visible)
            {
                return;
            }

            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            if(ColorPointComboBox.Text == "Enthalpy")
            {
                mollierControlSettings.FindPointType = ChartDataType.Enthalpy;
            }
            else if(ColorPointComboBox.Text == "Temperature")
            {
                mollierControlSettings.FindPointType = ChartDataType.DryBulbTemperature;
            }
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }

        private void DivisionAreaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DivisionArea();
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
            MollierControl_Main.Regenerate();
        }

        private void saveAsEMFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MollierControl_Main.Size = new Size(100, 100);
            //Refresh();
            SaveAs(null);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
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

            List<IMollierObject> mollierObjects = Core.Convert.ToSAM<IMollierObject>(path);
            if(mollierObjects == null || mollierObjects.Count == 0)
            {
                return;
            }
            Forms.OpenJSONForm.Action action = Forms.OpenJSONForm.Action.Undefined;


            using (Forms.OpenJSONForm openJSONForm = new Forms.OpenJSONForm())
            {
                //DialogResult dialogResult = openJSONForm.ShowDialog();
                if(openJSONForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                action = openJSONForm.GetAction();
            }

            switch(action)
            {
                case Forms.OpenJSONForm.Action.Undefined:
                    return;

                case Forms.OpenJSONForm.Action.Replace:
                    Clear();
                    MollierControlSettings mollierControlSettings = System.IO.File.Exists(path) ? Core.Convert.ToSAM<MollierControlSettings>(path).Find(x => x != null) : null;
                    if (mollierControlSettings != null)
                    {
                        MollierControl_Main.MollierControlSettings = mollierControlSettings;
                        LoadMollierControlSettings(mollierControlSettings);
                    }
                    break;
            }

            LoadMollierObjects(mollierObjects);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        public void LoadMollierObjects(IEnumerable<IMollierObject> mollierObjects)
        {

            //if(mollierObjects == null || mollierObjects.Count() == 0)
            //{
            //    return;
            //}

            //List<IMollierProcess> mollierProcesses = new List<IMollierProcess>();
            //List<IMollierPoint> mollierPoints = new List<IMollierPoint>();
            //foreach(IMollierObject mollierObject in mollierObjects)
            //{
            //    if(mollierObject is IMollierProcess)
            //    {
            //        mollierProcesses.Add((IMollierProcess)mollierObject);
            //    }
            //    else if(mollierObject is IMollierPoint)
            //    {
            //        mollierPoints.Add((IMollierPoint)mollierObject);
            //    }
            //    else if(mollierObject is IMollierGroup)
            //    {
            //        LoadMollierObjects((IMollierGroup)mollierObject);
            //    }
            //}

            //double pressure = Query.DefaultPressure(mollierPoints, mollierProcesses);

            //Pressure = double.IsNaN(pressure) ? Standard.Pressure : pressure;

            //AddProcesses(mollierProcesses, false);
            //AddPoints(mollierPoints, false);

            AddMollierObjects(mollierObjects);
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

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MollierControl_Main.ClearObjects();
            PointsCheckBox.Checked = false;
        }

        private void Button_Mollier_Click(object sender, EventArgs e)
        {
            ShowMollier();
        }

        private void Button_Psychrometric_Click(object sender, EventArgs e)
        {
            ShowPsychrometric();
        }

        private void Button_Reset_Click(object sender, EventArgs e)
        {
            Reset();
        }

        private void MollierControl_Main_MollierPointSelected_Epsilon(object sender, MollierPointSelectedEventArgs e)
        {
            AddProcess_ByEpsilonAndHumidityRatioDifference(e);
        }

        private void MollierControl_Main_MollierPointSelected_SensibleHeatRatio(object sender, MollierPointSelectedEventArgs e)
        {
            AddProcess_BySensibleHeatRatio(e);
        }

        private void AddProcess_BySensibleHeatRatio(MollierPointSelectedEventArgs e)
        {
            MollierControl_Main.MollierPointSelected -= MollierControl_Main_MollierPointSelected_SensibleHeatRatio;

            MollierPoint mollierPoint = e.MollierPoint;
            if (mollierPoint == null)
            {
                return;
            }

            if(double.IsNaN(mollierPoint.RelativeHumidity))
            {
                MessageBox.Show("Select point with relative humidity less than 100%");
                return;
            }

            double sensibleHeatRatio = double.NaN;
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Sensible Heat Ratio", "Sensible Heat Ratio (SHR) [0-1]"))
            {
                textBoxForm.Value = 0.85;
                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                sensibleHeatRatio = textBoxForm.Value;
            }

            if (double.IsNaN(sensibleHeatRatio))
            {
                return;
            }

            MollierSensibleHeatRatioLine mollierSensibleHeatRatioLine = new MollierSensibleHeatRatioLine(mollierPoint, sensibleHeatRatio);

            //MollierControlSettings mollierControlSettings = MollierControlSettings;
            //if (mollierControlSettings == null)
            //{
            //    mollierControlSettings = new MollierControlSettings();
            //}

            //ChartType chartType = mollierControlSettings.ChartType;
            //double pressure = mollierControlSettings.Pressure;

            //double angle = System.Math.Atan(sensibleHeatRatio);
            //Line2D line2D = Geometry.Planar.Create.Line2D(mollierPoint.ToSAM(mollierControlSettings.ChartType), angle);
            //Rectangle2D chartArea = Geometry.Planar.Create.Rectangle2D(Query.CornerPoints(mollierControlSettings));

            //Segment2D processSegment = Geometry.Planar.Create.Segment2D(line2D, chartArea);
            //if(processSegment == null)
            //{
            //    return;
            //}
            //UndefinedProcess undefinedProcess = Mollier.Create.UndefinedProcess(processSegment.Start.ToMollier(chartType, pressure), 
            //                                                                    processSegment.End.ToMollier(chartType, pressure));

            //if (undefinedProcess == null)
            //{
            //    return;
            //}
                
            //UIMollierProcess uIMollierProcess = new UIMollierProcess(undefinedProcess, System.Drawing.Color.LightGray);

            //AddProcesses(new IMollierProcess[] { uIMollierProcess }, false);
            AddMollierObjects(new IMollierCurve[] { new UIMollierCurve(mollierSensibleHeatRatioLine, System.Drawing.Color.LightGray) }, false);
        }

        private void AddProcess_ByEpsilonAndEnthalpyDifference(MollierPointSelectedEventArgs e)
        {
            MollierControl_Main.MollierPointSelected -= MollierControl_Main_MollierPointSelected_Epsilon;

            MollierPoint mollierPoint = e.MollierPoint;
            if (mollierPoint == null)
            {
                return;
            }

            double epsilon = double.NaN;
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Epsilon", "Epsilon [ε=Δh/Δx]"))
            {
                textBoxForm.Value = 2501;
                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                epsilon = textBoxForm.Value;
            }

            if (double.IsNaN(epsilon))
            {
                return;
            }

            double enthalpyDifference = double.NaN;
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Enthalpy Difference", "Enthalpy Difference (kJ/kg)"))
            {
                textBoxForm.Value = 10;
                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                enthalpyDifference = textBoxForm.Value;
            }

            if (double.IsNaN(enthalpyDifference))
            {
                return;
            }

            RoomProcess roomProcess = Mollier.Create.RoomProcess_ByEpsilonAndEnthalpyDifference(mollierPoint, epsilon, enthalpyDifference * 1000);
            if (roomProcess == null)
            {
                return;
            }

            UIMollierProcess uIMollierProcess = new UIMollierProcess(roomProcess, System.Drawing.Color.LightGray);

            //AddProcesses(new IMollierProcess[] { uIMollierProcess }, false);
            AddMollierObjects(new IMollierProcess[] { uIMollierProcess }, false);
        }

        private void AddProcess_ByEpsilonAndHumidityRatioDifference(MollierPointSelectedEventArgs e)
        {
            MollierControl_Main.MollierPointSelected -= MollierControl_Main_MollierPointSelected_Epsilon;

            MollierPoint mollierPoint = e.MollierPoint;
            if (mollierPoint == null)
            {
                return;
            }

            if (double.IsNaN(mollierPoint.RelativeHumidity))
            {
                MessageBox.Show("Select point with relative humidity less than 100%");
                return;
            }

            double epsilon = double.NaN;
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Epsilon", "Epsilon [ε=Δh/Δx]"))
            {
                textBoxForm.Value = 2501;
                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                epsilon = textBoxForm.Value;
            }

            if (double.IsNaN(epsilon))
            {
                return;
            }

            double humidityRatio = double.NaN;
            //using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Humidity Ratio", "Humidity Ratio [g/kg] at which process\nends process line length"))
            using (Windows.Forms.TextBoxForm<double> textBoxForm = new Windows.Forms.TextBoxForm<double>("Humidity Ratio", "Humidity Ratio [g/kg] of the end of the process"))
            {
                textBoxForm.Value = 10;
                textBoxForm.Size = new System.Drawing.Size((int)(textBoxForm.Size.Width * 1.2), textBoxForm.Size.Height);

                if (textBoxForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                humidityRatio = textBoxForm.Value;
            }

            if (double.IsNaN(humidityRatio))
            {
                return;
            }

            RoomProcess roomProcess = Mollier.Create.RoomProcess_ByEpsilonAndHumidityRatioDifference(mollierPoint, epsilon, (humidityRatio / 1000) - mollierPoint.HumidityRatio);
            if (roomProcess == null)
            {
                return;
            }

            UIMollierProcess uIMollierProcess = new UIMollierProcess(roomProcess, System.Drawing.Color.LightGray);

            //AddProcesses(new IMollierProcess[] { uIMollierProcess }, false);
            AddMollierObjects(new IMollierProcess[] { uIMollierProcess }, false);
        }

        private void ManageMollierObjectsForm_MollierObjectSelected(object sender, MollierObjectSelectedArgs e)
        {
            MollierControl_Main.Select(e.MollierObject);
        }

        private void ManageMollierObjectsForm_MollierModelEdited(object sender, MollierModelEditedEventArgs e)
        {
            MollierControl_Main.MollierModel = e.MollierModel;
            MollierControl_Main.Regenerate();
        }

        private void ManageMollierObjectsForm_Closing(object sender, FormClosingEventArgs e)
        {
            if (manageMollierObjectsForm == null || manageMollierObjectsForm.DialogResult != DialogResult.OK)
            {
                manageMollierObjectsForm = null;
                return;
            }

            manageMollierObjectsForm = null;
        }

        private void ToolStripMenuItem_PartialVapourPressure_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_PartialVapourPressure.Checked = !ToolStripMenuItem_PartialVapourPressure.Checked;
            MollierControlSettings mollierControlSettings = MollierControl_Main.MollierControlSettings;
            mollierControlSettings.PartialVapourPressure_Axis = ToolStripMenuItem_PartialVapourPressure.Checked;
            MollierControl_Main.MollierControlSettings = mollierControlSettings;
            MollierControl_Main.Regenerate();
        }

        private void ToolStripMenuItem_ComfortZoners_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_ComfortZones.Checked = !ToolStripMenuItem_ComfortZones.Checked;
        }

        private void ToolStripMenuItem_ComfortZones_CheckedChanged(object sender, EventArgs e)
        {
            if (ToolStripMenuItem_ComfortZones.Checked)
            {
                MollierControl_Main.AddMollierObjects(Query.MollierZones());
            }
            else
            {
                MollierControl_Main.RemoveZones(Query.MollierZones());
            }
        }

        private void MollierForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Control)
            {
                if (e.KeyCode == Keys.E)
                {
                    SaveAs(null);
                    return;
                }

                if (e.KeyCode == Keys.S)
                {
                    Save();
                    return;
                }

                if (e.KeyCode == Keys.M)
                {
                    ShowMollier();
                    return;
                }

                if (e.KeyCode == Keys.P)
                {
                    ShowPsychrometric();
                    return;
                }

                if (e.KeyCode == Keys.D)
                {
                    Edit();
                    return;
                }

                if (e.KeyCode == Keys.R)
                {
                    AddProcess();
                    return;
                }

                if (e.KeyCode == Keys.O)
                {
                    AddPoint();
                    return;
                }

                if (e.KeyCode == Keys.C)
                {
                    CoolingAuxiliaryProcessesVisibility();
                    return;
                }
            }
        }

        private void ToolStripMenuItem_Wiki_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/HoareLea/SAM_Mollier/wiki/HomeUI");
        }

        private void ToolStripMenuItem_AddPoint_Click(object sender, EventArgs e)
        {
            AddPoint();
        }

        private void ToolStripMenuItem_AddProcess_Click(object sender, EventArgs e)
        {
            AddProcess();
        }

        private void ToolStripMenuItem_Edit_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void ToolStripMenuItem_Epsilon_Click(object sender, EventArgs e)
        {
            Epsilon();
        }

        private void ToolStripMenuItem_SHR_Click(object sender, EventArgs e)
        {
            SHR();
        }

        private void ToolStripMenuItem_DivisionArea_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem_DivisionArea.Checked = !ToolStripMenuItem_DivisionArea.Checked;
            DivisionArea();
        }

        private void MollierForm_Shown(object sender, EventArgs e)
        {
            MollierControl_Main.Regenerate();
        }
    }
}
