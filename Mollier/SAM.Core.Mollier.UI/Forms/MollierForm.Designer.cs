
namespace SAM.Core.Mollier.UI
{
    partial class MollierForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SAM.Core.Mollier.UI.MollierControlSettings mollierControlSettings2 = new SAM.Core.Mollier.UI.MollierControlSettings();
            SAM.Core.Mollier.UI.VisibilitySettings visibilitySettings2 = new SAM.Core.Mollier.UI.VisibilitySettings();
            SAM.Core.Mollier.UI.PdfDefaultSettings pdfDefaultSettings2 = new SAM.Core.Mollier.UI.PdfDefaultSettings();
            this.TextBox_Pressure = new System.Windows.Forms.TextBox();
            this.Label_Pressure = new System.Windows.Forms.Label();
            this.Button_AddPoint = new System.Windows.Forms.Button();
            this.MenuStrip_Main = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem_File = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.PdfA4ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PdfA3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsJPGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_View = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Density = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Enthalpy = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SpecificVolume = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_WetBulbTemperature = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_ChartType = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Mollier = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Psychrometric = new System.Windows.Forms.ToolStripMenuItem();
            this.colorThemeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.defaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_OpenSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.resetChartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Label_Elevation = new System.Windows.Forms.Label();
            this.TextBox_Elevation = new System.Windows.Forms.TextBox();
            this.Button_AddProcess = new System.Windows.Forms.Button();
            this.CheckBox_Zone = new System.Windows.Forms.CheckBox();
            this.PointsCheckBox = new System.Windows.Forms.CheckBox();
            this.PercentPointsTextBox = new System.Windows.Forms.TextBox();
            this.PointsLabel = new System.Windows.Forms.Label();
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.MenuStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBox_Pressure
            // 
            this.TextBox_Pressure.Location = new System.Drawing.Point(273, 30);
            this.TextBox_Pressure.Name = "TextBox_Pressure";
            this.TextBox_Pressure.Size = new System.Drawing.Size(63, 22);
            this.TextBox_Pressure.TabIndex = 12;
            this.TextBox_Pressure.Text = "101325";
            this.TextBox_Pressure.TextChanged += new System.EventHandler(this.TextBox_Pressure_TextChanged);
            // 
            // Label_Pressure
            // 
            this.Label_Pressure.AutoSize = true;
            this.Label_Pressure.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label_Pressure.Location = new System.Drawing.Point(175, 33);
            this.Label_Pressure.Name = "Label_Pressure";
            this.Label_Pressure.Size = new System.Drawing.Size(92, 16);
            this.Label_Pressure.TabIndex = 11;
            this.Label_Pressure.Text = "Pressure [Pa]:";
            // 
            // Button_AddPoint
            // 
            this.Button_AddPoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_AddPoint.Location = new System.Drawing.Point(1254, 31);
            this.Button_AddPoint.Name = "Button_AddPoint";
            this.Button_AddPoint.Size = new System.Drawing.Size(108, 28);
            this.Button_AddPoint.TabIndex = 19;
            this.Button_AddPoint.Text = "Add Point";
            this.Button_AddPoint.UseVisualStyleBackColor = true;
            this.Button_AddPoint.Click += new System.EventHandler(this.Button_AddPoint_Click);
            // 
            // MenuStrip_Main
            // 
            this.MenuStrip_Main.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MenuStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_File,
            this.ToolStripMenuItem_View,
            this.ToolStripMenuItem_Settings,
            this.resetChartToolStripMenuItem});
            this.MenuStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip_Main.Name = "MenuStrip_Main";
            this.MenuStrip_Main.ShowItemToolTips = true;
            this.MenuStrip_Main.Size = new System.Drawing.Size(1374, 28);
            this.MenuStrip_Main.TabIndex = 20;
            this.MenuStrip_Main.Text = "MenuStrip_Main";
            // 
            // ToolStripMenuItem_File
            // 
            this.ToolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Save,
            this.saveAsJPGToolStripMenuItem});
            this.ToolStripMenuItem_File.Name = "ToolStripMenuItem_File";
            this.ToolStripMenuItem_File.Size = new System.Drawing.Size(46, 24);
            this.ToolStripMenuItem_File.Text = "File";
            // 
            // ToolStripMenuItem_Save
            // 
            this.ToolStripMenuItem_Save.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PdfA4ToolStripMenuItem,
            this.PdfA3ToolStripMenuItem});
            this.ToolStripMenuItem_Save.Name = "ToolStripMenuItem_Save";
            this.ToolStripMenuItem_Save.Size = new System.Drawing.Size(171, 26);
            this.ToolStripMenuItem_Save.Text = "Save as PDF";
            // 
            // PdfA4ToolStripMenuItem
            // 
            this.PdfA4ToolStripMenuItem.Name = "PdfA4ToolStripMenuItem";
            this.PdfA4ToolStripMenuItem.Size = new System.Drawing.Size(110, 26);
            this.PdfA4ToolStripMenuItem.Text = "A4";
            this.PdfA4ToolStripMenuItem.Click += new System.EventHandler(this.PdfA4ToolStripMenuItem_Click);
            // 
            // PdfA3ToolStripMenuItem
            // 
            this.PdfA3ToolStripMenuItem.Name = "PdfA3ToolStripMenuItem";
            this.PdfA3ToolStripMenuItem.Size = new System.Drawing.Size(110, 26);
            this.PdfA3ToolStripMenuItem.Text = "A3";
            this.PdfA3ToolStripMenuItem.Click += new System.EventHandler(this.PdfA3ToolStripMenuItem_Click);
            // 
            // saveAsJPGToolStripMenuItem
            // 
            this.saveAsJPGToolStripMenuItem.Name = "saveAsJPGToolStripMenuItem";
            this.saveAsJPGToolStripMenuItem.Size = new System.Drawing.Size(171, 26);
            this.saveAsJPGToolStripMenuItem.Text = "Save as JPG";
            this.saveAsJPGToolStripMenuItem.Click += new System.EventHandler(this.saveAsJPGToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_View
            // 
            this.ToolStripMenuItem_View.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Density,
            this.ToolStripMenuItem_Enthalpy,
            this.ToolStripMenuItem_SpecificVolume,
            this.ToolStripMenuItem_WetBulbTemperature,
            this.ToolStripMenuItem_ChartType,
            this.colorThemeToolStripMenuItem});
            this.ToolStripMenuItem_View.Name = "ToolStripMenuItem_View";
            this.ToolStripMenuItem_View.Size = new System.Drawing.Size(55, 24);
            this.ToolStripMenuItem_View.Text = "View";
            // 
            // ToolStripMenuItem_Density
            // 
            this.ToolStripMenuItem_Density.Checked = true;
            this.ToolStripMenuItem_Density.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Density.Name = "ToolStripMenuItem_Density";
            this.ToolStripMenuItem_Density.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Density.Text = "Density";
            this.ToolStripMenuItem_Density.ToolTipText = "Turn Density Line on or off";
            this.ToolStripMenuItem_Density.Click += new System.EventHandler(this.ToolStripMenuItem_Density_Click);
            // 
            // ToolStripMenuItem_Enthalpy
            // 
            this.ToolStripMenuItem_Enthalpy.Checked = true;
            this.ToolStripMenuItem_Enthalpy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Enthalpy.Name = "ToolStripMenuItem_Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Enthalpy.Text = "Enthalpy";
            this.ToolStripMenuItem_Enthalpy.ToolTipText = "Turn Enthalpy Line on or off";
            this.ToolStripMenuItem_Enthalpy.Click += new System.EventHandler(this.ToolStripMenuItem_Enthalpy_Click);
            // 
            // ToolStripMenuItem_SpecificVolume
            // 
            this.ToolStripMenuItem_SpecificVolume.Checked = true;
            this.ToolStripMenuItem_SpecificVolume.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_SpecificVolume.Name = "ToolStripMenuItem_SpecificVolume";
            this.ToolStripMenuItem_SpecificVolume.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_SpecificVolume.Text = "Specific Volume";
            this.ToolStripMenuItem_SpecificVolume.ToolTipText = "Turn Specific Volume Line on or off";
            this.ToolStripMenuItem_SpecificVolume.Click += new System.EventHandler(this.ToolStripMenuItem_SpecificVolume_Click);
            // 
            // ToolStripMenuItem_WetBulbTemperature
            // 
            this.ToolStripMenuItem_WetBulbTemperature.Checked = true;
            this.ToolStripMenuItem_WetBulbTemperature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_WetBulbTemperature.Name = "ToolStripMenuItem_WetBulbTemperature";
            this.ToolStripMenuItem_WetBulbTemperature.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_WetBulbTemperature.Text = "Wet Bulb Temperature";
            this.ToolStripMenuItem_WetBulbTemperature.ToolTipText = "Turn Wet-Bulb Temperature Line on or off";
            this.ToolStripMenuItem_WetBulbTemperature.Click += new System.EventHandler(this.ToolStripMenuItem_WetBulbTemperature_Click);
            // 
            // ToolStripMenuItem_ChartType
            // 
            this.ToolStripMenuItem_ChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChartToolStripMenuItem_Mollier,
            this.ChartToolStripMenuItem_Psychrometric});
            this.ToolStripMenuItem_ChartType.Name = "ToolStripMenuItem_ChartType";
            this.ToolStripMenuItem_ChartType.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_ChartType.Text = "Chart Type";
            // 
            // ChartToolStripMenuItem_Mollier
            // 
            this.ChartToolStripMenuItem_Mollier.Checked = true;
            this.ChartToolStripMenuItem_Mollier.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChartToolStripMenuItem_Mollier.Name = "ChartToolStripMenuItem_Mollier";
            this.ChartToolStripMenuItem_Mollier.Size = new System.Drawing.Size(184, 26);
            this.ChartToolStripMenuItem_Mollier.Text = "Mollier";
            this.ChartToolStripMenuItem_Mollier.Click += new System.EventHandler(this.ChartToolStripMenuItem_Mollier_Click);
            // 
            // ChartToolStripMenuItem_Psychrometric
            // 
            this.ChartToolStripMenuItem_Psychrometric.Name = "ChartToolStripMenuItem_Psychrometric";
            this.ChartToolStripMenuItem_Psychrometric.Size = new System.Drawing.Size(184, 26);
            this.ChartToolStripMenuItem_Psychrometric.Text = "Psychrometric";
            this.ChartToolStripMenuItem_Psychrometric.Click += new System.EventHandler(this.ChartToolStripMenuItem_Psychrometric_Click);
            // 
            // colorThemeToolStripMenuItem
            // 
            this.colorThemeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.defaultToolStripMenuItem,
            this.blueToolStripMenuItem,
            this.grayToolStripMenuItem});
            this.colorThemeToolStripMenuItem.Name = "colorThemeToolStripMenuItem";
            this.colorThemeToolStripMenuItem.Size = new System.Drawing.Size(240, 26);
            this.colorThemeToolStripMenuItem.Text = "Color Theme";
            // 
            // defaultToolStripMenuItem
            // 
            this.defaultToolStripMenuItem.Checked = true;
            this.defaultToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.defaultToolStripMenuItem.Name = "defaultToolStripMenuItem";
            this.defaultToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            this.defaultToolStripMenuItem.Text = "Default";
            this.defaultToolStripMenuItem.Click += new System.EventHandler(this.defaultToolStripMenuItem_Click);
            // 
            // blueToolStripMenuItem
            // 
            this.blueToolStripMenuItem.Name = "blueToolStripMenuItem";
            this.blueToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            this.blueToolStripMenuItem.Text = "Blue";
            this.blueToolStripMenuItem.Click += new System.EventHandler(this.blueToolStripMenuItem_Click);
            // 
            // grayToolStripMenuItem
            // 
            this.grayToolStripMenuItem.Name = "grayToolStripMenuItem";
            this.grayToolStripMenuItem.Size = new System.Drawing.Size(141, 26);
            this.grayToolStripMenuItem.Text = "Gray";
            this.grayToolStripMenuItem.Click += new System.EventHandler(this.grayToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_Settings
            // 
            this.ToolStripMenuItem_Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_OpenSettings});
            this.ToolStripMenuItem_Settings.Name = "ToolStripMenuItem_Settings";
            this.ToolStripMenuItem_Settings.Size = new System.Drawing.Size(76, 24);
            this.ToolStripMenuItem_Settings.Text = "Settings";
            this.ToolStripMenuItem_Settings.Click += new System.EventHandler(this.ToolStripMenuItem_Settings_Click);
            // 
            // ToolStripMenuItem_OpenSettings
            // 
            this.ToolStripMenuItem_OpenSettings.Name = "ToolStripMenuItem_OpenSettings";
            this.ToolStripMenuItem_OpenSettings.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_OpenSettings.Text = "Settings";
            this.ToolStripMenuItem_OpenSettings.Click += new System.EventHandler(this.ToolStripMenuItem_OpenSettings_Click);
            // 
            // resetChartToolStripMenuItem
            // 
            this.resetChartToolStripMenuItem.Name = "resetChartToolStripMenuItem";
            this.resetChartToolStripMenuItem.Size = new System.Drawing.Size(98, 24);
            this.resetChartToolStripMenuItem.Text = "Reset Chart";
            this.resetChartToolStripMenuItem.ToolTipText = "Reset all chart data to the default values";
            this.resetChartToolStripMenuItem.Click += new System.EventHandler(this.resetChartToolStripMenuItem_Click);
            // 
            // Label_Elevation
            // 
            this.Label_Elevation.AutoSize = true;
            this.Label_Elevation.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label_Elevation.Location = new System.Drawing.Point(12, 33);
            this.Label_Elevation.Name = "Label_Elevation";
            this.Label_Elevation.Size = new System.Drawing.Size(88, 16);
            this.Label_Elevation.TabIndex = 21;
            this.Label_Elevation.Text = "Elevation [m]:";
            // 
            // TextBox_Elevation
            // 
            this.TextBox_Elevation.Location = new System.Drawing.Point(106, 30);
            this.TextBox_Elevation.Name = "TextBox_Elevation";
            this.TextBox_Elevation.Size = new System.Drawing.Size(63, 22);
            this.TextBox_Elevation.TabIndex = 22;
            this.TextBox_Elevation.Text = "0";
            this.TextBox_Elevation.TextChanged += new System.EventHandler(this.TextBox_Elevation_TextChanged);
            // 
            // Button_AddProcess
            // 
            this.Button_AddProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_AddProcess.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Button_AddProcess.Location = new System.Drawing.Point(1132, 30);
            this.Button_AddProcess.Name = "Button_AddProcess";
            this.Button_AddProcess.Size = new System.Drawing.Size(116, 28);
            this.Button_AddProcess.TabIndex = 23;
            this.Button_AddProcess.Text = "Add Process";
            this.Button_AddProcess.UseVisualStyleBackColor = true;
            this.Button_AddProcess.Click += new System.EventHandler(this.Button_AddProcess_Click);
            // 
            // CheckBox_Zone
            // 
            this.CheckBox_Zone.AutoSize = true;
            this.CheckBox_Zone.Location = new System.Drawing.Point(357, 32);
            this.CheckBox_Zone.Name = "CheckBox_Zone";
            this.CheckBox_Zone.Size = new System.Drawing.Size(116, 20);
            this.CheckBox_Zone.TabIndex = 29;
            this.CheckBox_Zone.Text = "Comfort Zones";
            this.CheckBox_Zone.UseVisualStyleBackColor = true;
            this.CheckBox_Zone.CheckedChanged += new System.EventHandler(this.CheckBox_Zone_CheckedChanged);
            // 
            // PointsCheckBox
            // 
            this.PointsCheckBox.AutoSize = true;
            this.PointsCheckBox.Location = new System.Drawing.Point(526, 32);
            this.PointsCheckBox.Name = "PointsCheckBox";
            this.PointsCheckBox.Size = new System.Drawing.Size(95, 20);
            this.PointsCheckBox.TabIndex = 30;
            this.PointsCheckBox.Text = "Find Points";
            this.PointsCheckBox.UseVisualStyleBackColor = true;
            this.PointsCheckBox.CheckedChanged += new System.EventHandler(this.PointsCheckBox_CheckedChanged);
            // 
            // PercentPointsTextBox
            // 
            this.PercentPointsTextBox.Location = new System.Drawing.Point(627, 31);
            this.PercentPointsTextBox.Name = "PercentPointsTextBox";
            this.PercentPointsTextBox.Size = new System.Drawing.Size(33, 22);
            this.PercentPointsTextBox.TabIndex = 31;
            this.PercentPointsTextBox.Text = "1";
            this.PercentPointsTextBox.Visible = false;
            this.PercentPointsTextBox.TextChanged += new System.EventHandler(this.PercentPointsTextBox_TextChanged);
            // 
            // PointsLabel
            // 
            this.PointsLabel.AutoSize = true;
            this.PointsLabel.Location = new System.Drawing.Point(657, 34);
            this.PointsLabel.Name = "PointsLabel";
            this.PointsLabel.Size = new System.Drawing.Size(19, 16);
            this.PointsLabel.TabIndex = 32;
            this.PointsLabel.Text = "%";
            this.PointsLabel.Visible = false;
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MollierControl_Main.Location = new System.Drawing.Point(12, 65);
            this.MollierControl_Main.Margin = new System.Windows.Forms.Padding(2);
            mollierControlSettings2.ChartType = SAM.Core.Mollier.ChartType.Mollier;
            mollierControlSettings2.Color = "default";
            mollierControlSettings2.Density_line = true;
            mollierControlSettings2.DisableLabels = false;
            mollierControlSettings2.DisableUnits = false;
            mollierControlSettings2.Elevation = 0D;
            mollierControlSettings2.Enthalpy_line = true;
            mollierControlSettings2.GradientPoint = false;
            mollierControlSettings2.HumidityRatio_Interval = 5D;
            mollierControlSettings2.HumidityRatio_Max = 35D;
            mollierControlSettings2.HumidityRatio_Min = 0D;
            mollierControlSettings2.P_w_Interval = 1D;
            mollierControlSettings2.Pressure = 101325D;
            mollierControlSettings2.SpecificVolume_line = true;
            mollierControlSettings2.Temperature_Interval = 5D;
            mollierControlSettings2.Temperature_Max = 50D;
            mollierControlSettings2.Temperature_Min = -20D;
            mollierControlSettings2.VisibilitySettings = visibilitySettings2;
            mollierControlSettings2.WetBulbTemperature_line = true;
            this.MollierControl_Main.MollierControlSettings = mollierControlSettings2;
            this.MollierControl_Main.Name = "MollierControl_Main";
            pdfDefaultSettings2.A3Height = 297;
            pdfDefaultSettings2.A3Width = 420;
            pdfDefaultSettings2.A4Height = 210;
            pdfDefaultSettings2.A4Width = 297;
            pdfDefaultSettings2.ChartHeight = 728;
            pdfDefaultSettings2.ChartWidth = 1512;
            this.MollierControl_Main.PdfDefaultSettings = pdfDefaultSettings2;
            this.MollierControl_Main.Size = new System.Drawing.Size(1350, 912);
            this.MollierControl_Main.TabIndex = 0;
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1374, 977);
            this.Controls.Add(this.PointsLabel);
            this.Controls.Add(this.PercentPointsTextBox);
            this.Controls.Add(this.PointsCheckBox);
            this.Controls.Add(this.CheckBox_Zone);
            this.Controls.Add(this.Button_AddProcess);
            this.Controls.Add(this.TextBox_Elevation);
            this.Controls.Add(this.Label_Elevation);
            this.Controls.Add(this.Button_AddPoint);
            this.Controls.Add(this.TextBox_Pressure);
            this.Controls.Add(this.Label_Pressure);
            this.Controls.Add(this.MollierControl_Main);
            this.Controls.Add(this.MenuStrip_Main);
            this.MainMenuStrip = this.MenuStrip_Main;
            this.Name = "MollierForm";
            this.ShowIcon = false;
            this.Text = "Mollier";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MollierForm_FormClosing);
            this.Load += new System.EventHandler(this.MollierForm_Load);
            this.MenuStrip_Main.ResumeLayout(false);
            this.MenuStrip_Main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.MollierControl MollierControl_Main;
        private System.Windows.Forms.TextBox TextBox_Pressure;
        private System.Windows.Forms.Label Label_Pressure;
        private System.Windows.Forms.Button Button_AddPoint;
        private System.Windows.Forms.MenuStrip MenuStrip_Main;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_File;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Save;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_View;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Density;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Enthalpy;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_SpecificVolume;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_ChartType;
        private System.Windows.Forms.ToolStripMenuItem ChartToolStripMenuItem_Mollier;
        private System.Windows.Forms.ToolStripMenuItem ChartToolStripMenuItem_Psychrometric;
        private System.Windows.Forms.ToolStripMenuItem colorThemeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem defaultToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem grayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Settings;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_OpenSettings;
        private System.Windows.Forms.Label Label_Elevation;
        private System.Windows.Forms.TextBox TextBox_Elevation;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_WetBulbTemperature;
        private System.Windows.Forms.ToolStripMenuItem resetChartToolStripMenuItem;
        private System.Windows.Forms.Button Button_AddProcess;
        private System.Windows.Forms.CheckBox CheckBox_Zone;
        private System.Windows.Forms.ToolStripMenuItem PdfA4ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PdfA3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsJPGToolStripMenuItem;
        private System.Windows.Forms.CheckBox PointsCheckBox;
        private System.Windows.Forms.TextBox PercentPointsTextBox;
        private System.Windows.Forms.Label PointsLabel;
    }
}

