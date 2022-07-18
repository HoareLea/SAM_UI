
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
            this.TextBox_Pressure = new System.Windows.Forms.TextBox();
            this.Label_Pressure = new System.Windows.Forms.Label();
            this.Button_AddPoint = new System.Windows.Forms.Button();
            this.MenuStrip_Main = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem_File = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
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
            this.Label_Elevation = new System.Windows.Forms.Label();
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.TextBox_Elevation = new System.Windows.Forms.TextBox();
            this.MenuStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBox_Pressure
            // 
            this.TextBox_Pressure.Location = new System.Drawing.Point(261, 36);
            this.TextBox_Pressure.Name = "TextBox_Pressure";
            this.TextBox_Pressure.Size = new System.Drawing.Size(63, 20);
            this.TextBox_Pressure.TabIndex = 12;
            this.TextBox_Pressure.Text = "101325";
            this.TextBox_Pressure.TextChanged += new System.EventHandler(this.TextBox_Pressure_TextChanged);
            // 
            // Label_Pressure
            // 
            this.Label_Pressure.AutoSize = true;
            this.Label_Pressure.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label_Pressure.Location = new System.Drawing.Point(182, 39);
            this.Label_Pressure.Name = "Label_Pressure";
            this.Label_Pressure.Size = new System.Drawing.Size(73, 13);
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
            this.ToolStripMenuItem_Settings});
            this.MenuStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip_Main.Name = "MenuStrip_Main";
            this.MenuStrip_Main.Size = new System.Drawing.Size(1374, 24);
            this.MenuStrip_Main.TabIndex = 20;
            this.MenuStrip_Main.Text = "MenuStrip_Main";
            // 
            // ToolStripMenuItem_File
            // 
            this.ToolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Save});
            this.ToolStripMenuItem_File.Name = "ToolStripMenuItem_File";
            this.ToolStripMenuItem_File.Size = new System.Drawing.Size(37, 20);
            this.ToolStripMenuItem_File.Text = "File";
            this.ToolStripMenuItem_File.Click += new System.EventHandler(this.ToolStripMenuItem_File_Click);
            // 
            // ToolStripMenuItem_Save
            // 
            this.ToolStripMenuItem_Save.Name = "ToolStripMenuItem_Save";
            this.ToolStripMenuItem_Save.Size = new System.Drawing.Size(98, 22);
            this.ToolStripMenuItem_Save.Text = "Save";
            this.ToolStripMenuItem_Save.Click += new System.EventHandler(this.ToolStripMenuItem_Save_Click);
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
            this.ToolStripMenuItem_View.Size = new System.Drawing.Size(44, 20);
            this.ToolStripMenuItem_View.Text = "View";
            // 
            // ToolStripMenuItem_Density
            // 
            this.ToolStripMenuItem_Density.Checked = true;
            this.ToolStripMenuItem_Density.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Density.Name = "ToolStripMenuItem_Density";
            this.ToolStripMenuItem_Density.Size = new System.Drawing.Size(191, 22);
            this.ToolStripMenuItem_Density.Text = "Density";
            this.ToolStripMenuItem_Density.Click += new System.EventHandler(this.ToolStripMenuItem_Density_Click);
            // 
            // ToolStripMenuItem_Enthalpy
            // 
            this.ToolStripMenuItem_Enthalpy.Checked = true;
            this.ToolStripMenuItem_Enthalpy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Enthalpy.Name = "ToolStripMenuItem_Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Size = new System.Drawing.Size(191, 22);
            this.ToolStripMenuItem_Enthalpy.Text = "Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Click += new System.EventHandler(this.ToolStripMenuItem_Enthalpy_Click);
            // 
            // ToolStripMenuItem_SpecificVolume
            // 
            this.ToolStripMenuItem_SpecificVolume.Checked = true;
            this.ToolStripMenuItem_SpecificVolume.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_SpecificVolume.Name = "ToolStripMenuItem_SpecificVolume";
            this.ToolStripMenuItem_SpecificVolume.Size = new System.Drawing.Size(191, 22);
            this.ToolStripMenuItem_SpecificVolume.Text = "Specific Volume";
            this.ToolStripMenuItem_SpecificVolume.Click += new System.EventHandler(this.ToolStripMenuItem_SpecificVolume_Click);
            // 
            // ToolStripMenuItem_WetBulbTemperature
            // 
            this.ToolStripMenuItem_WetBulbTemperature.Checked = true;
            this.ToolStripMenuItem_WetBulbTemperature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_WetBulbTemperature.Name = "ToolStripMenuItem_WetBulbTemperature";
            this.ToolStripMenuItem_WetBulbTemperature.Size = new System.Drawing.Size(191, 22);
            this.ToolStripMenuItem_WetBulbTemperature.Text = "Wet Bulb Temperature";
            this.ToolStripMenuItem_WetBulbTemperature.Click += new System.EventHandler(this.ToolStripMenuItem_WetBulbTemperature_Click);
            // 
            // ToolStripMenuItem_ChartType
            // 
            this.ToolStripMenuItem_ChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChartToolStripMenuItem_Mollier,
            this.ChartToolStripMenuItem_Psychrometric});
            this.ToolStripMenuItem_ChartType.Name = "ToolStripMenuItem_ChartType";
            this.ToolStripMenuItem_ChartType.Size = new System.Drawing.Size(191, 22);
            this.ToolStripMenuItem_ChartType.Text = "Chart Type";
            // 
            // ChartToolStripMenuItem_Mollier
            // 
            this.ChartToolStripMenuItem_Mollier.Checked = true;
            this.ChartToolStripMenuItem_Mollier.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ChartToolStripMenuItem_Mollier.Name = "ChartToolStripMenuItem_Mollier";
            this.ChartToolStripMenuItem_Mollier.Size = new System.Drawing.Size(150, 22);
            this.ChartToolStripMenuItem_Mollier.Text = "Mollier";
            this.ChartToolStripMenuItem_Mollier.Click += new System.EventHandler(this.ChartToolStripMenuItem_Mollier_Click);
            // 
            // ChartToolStripMenuItem_Psychrometric
            // 
            this.ChartToolStripMenuItem_Psychrometric.Name = "ChartToolStripMenuItem_Psychrometric";
            this.ChartToolStripMenuItem_Psychrometric.Size = new System.Drawing.Size(150, 22);
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
            this.colorThemeToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.colorThemeToolStripMenuItem.Text = "Color Theme";
            // 
            // defaultToolStripMenuItem
            // 
            this.defaultToolStripMenuItem.Checked = true;
            this.defaultToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.defaultToolStripMenuItem.Name = "defaultToolStripMenuItem";
            this.defaultToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.defaultToolStripMenuItem.Text = "Default";
            this.defaultToolStripMenuItem.Click += new System.EventHandler(this.defaultToolStripMenuItem_Click);
            // 
            // blueToolStripMenuItem
            // 
            this.blueToolStripMenuItem.Name = "blueToolStripMenuItem";
            this.blueToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.blueToolStripMenuItem.Text = "Blue";
            this.blueToolStripMenuItem.Click += new System.EventHandler(this.blueToolStripMenuItem_Click);
            // 
            // grayToolStripMenuItem
            // 
            this.grayToolStripMenuItem.Name = "grayToolStripMenuItem";
            this.grayToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.grayToolStripMenuItem.Text = "Gray";
            this.grayToolStripMenuItem.Click += new System.EventHandler(this.grayToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_Settings
            // 
            this.ToolStripMenuItem_Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_OpenSettings});
            this.ToolStripMenuItem_Settings.Name = "ToolStripMenuItem_Settings";
            this.ToolStripMenuItem_Settings.Size = new System.Drawing.Size(61, 20);
            this.ToolStripMenuItem_Settings.Text = "Settings";
            // 
            // ToolStripMenuItem_OpenSettings
            // 
            this.ToolStripMenuItem_OpenSettings.Name = "ToolStripMenuItem_OpenSettings";
            this.ToolStripMenuItem_OpenSettings.Size = new System.Drawing.Size(116, 22);
            this.ToolStripMenuItem_OpenSettings.Text = "Settings";
            this.ToolStripMenuItem_OpenSettings.Click += new System.EventHandler(this.ToolStripMenuItem_OpenSettings_Click);
            // 
            // Label_Elevation
            // 
            this.Label_Elevation.AutoSize = true;
            this.Label_Elevation.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label_Elevation.Location = new System.Drawing.Point(9, 39);
            this.Label_Elevation.Name = "Label_Elevation";
            this.Label_Elevation.Size = new System.Drawing.Size(71, 13);
            this.Label_Elevation.TabIndex = 21;
            this.Label_Elevation.Text = "Elevation [m]:";
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MollierControl_Main.Blue_Color = "default";
            this.MollierControl_Main.ChartType = SAM.Core.Mollier.ChartType.Mollier;
            this.MollierControl_Main.Default_Color = "default";
            this.MollierControl_Main.Density_line = true;
            this.MollierControl_Main.Enthalpy_line = true;
            this.MollierControl_Main.Gray_Color = "default";
            this.MollierControl_Main.HumidityRatio_Interval = 5D;
            this.MollierControl_Main.HumidityRatio_Max = 35D;
            this.MollierControl_Main.HumidityRatio_Min = 0D;
            this.MollierControl_Main.Location = new System.Drawing.Point(12, 65);
            this.MollierControl_Main.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Pressure = 101325D;
            this.MollierControl_Main.Size = new System.Drawing.Size(1350, 912);
            this.MollierControl_Main.Specific_volume_line = true;
            this.MollierControl_Main.TabIndex = 0;
            this.MollierControl_Main.Temperature_Interval = 5D;
            this.MollierControl_Main.Temperature_Max = 50D;
            this.MollierControl_Main.Temperature_Min = -20D;
            this.MollierControl_Main.Wet_bulb_temperature_line = true;
            // 
            // TextBox_Elevation
            // 
            this.TextBox_Elevation.Location = new System.Drawing.Point(88, 36);
            this.TextBox_Elevation.Name = "TextBox_Elevation";
            this.TextBox_Elevation.Size = new System.Drawing.Size(63, 20);
            this.TextBox_Elevation.TabIndex = 22;
            this.TextBox_Elevation.Text = "101325";
            this.TextBox_Elevation.TextChanged += new System.EventHandler(this.TextBox_Elevation_TextChanged);
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1374, 977);
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
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_WetBulbTemperature;
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
    }
}

