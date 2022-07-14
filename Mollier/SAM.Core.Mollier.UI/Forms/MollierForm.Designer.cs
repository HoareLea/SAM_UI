
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
            this.graphSettingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.temperatureAxisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minimumValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.maximumValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.intervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.humidityRatioAxisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minimumValueToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.maximumValueToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.intervalToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.TemperatureMinimumValueTextbox = new System.Windows.Forms.TextBox();
            this.TemperatureMaximumValueTextbox = new System.Windows.Forms.TextBox();
            this.HumidityRatioMinimumValueTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.HumidityRatioMaximumValueTextbox = new System.Windows.Forms.TextBox();
            this.HumidityRatioIntervalTextbox = new System.Windows.Forms.TextBox();
            this.TemperatureIntervalTextbox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.MenuStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBox_Pressure
            // 
            this.TextBox_Pressure.Location = new System.Drawing.Point(110, 37);
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
            this.Label_Pressure.Location = new System.Drawing.Point(6, 40);
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
            this.graphSettingToolStripMenuItem});
            this.MenuStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip_Main.Name = "MenuStrip_Main";
            this.MenuStrip_Main.Size = new System.Drawing.Size(1374, 30);
            this.MenuStrip_Main.TabIndex = 20;
            this.MenuStrip_Main.Text = "MenuStrip_Main";
            // 
            // ToolStripMenuItem_File
            // 
            this.ToolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Save});
            this.ToolStripMenuItem_File.Name = "ToolStripMenuItem_File";
            this.ToolStripMenuItem_File.Size = new System.Drawing.Size(46, 26);
            this.ToolStripMenuItem_File.Text = "File";
            this.ToolStripMenuItem_File.Click += new System.EventHandler(this.ToolStripMenuItem_File_Click);
            // 
            // ToolStripMenuItem_Save
            // 
            this.ToolStripMenuItem_Save.Name = "ToolStripMenuItem_Save";
            this.ToolStripMenuItem_Save.Size = new System.Drawing.Size(123, 26);
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
            this.ToolStripMenuItem_View.Size = new System.Drawing.Size(55, 26);
            this.ToolStripMenuItem_View.Text = "View";
            // 
            // ToolStripMenuItem_Density
            // 
            this.ToolStripMenuItem_Density.Checked = true;
            this.ToolStripMenuItem_Density.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Density.Name = "ToolStripMenuItem_Density";
            this.ToolStripMenuItem_Density.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Density.Text = "Density";
            this.ToolStripMenuItem_Density.Click += new System.EventHandler(this.ToolStripMenuItem_Density_Click);
            // 
            // ToolStripMenuItem_Enthalpy
            // 
            this.ToolStripMenuItem_Enthalpy.Checked = true;
            this.ToolStripMenuItem_Enthalpy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Enthalpy.Name = "ToolStripMenuItem_Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_Enthalpy.Text = "Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Click += new System.EventHandler(this.ToolStripMenuItem_Enthalpy_Click);
            // 
            // ToolStripMenuItem_SpecificVolume
            // 
            this.ToolStripMenuItem_SpecificVolume.Checked = true;
            this.ToolStripMenuItem_SpecificVolume.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_SpecificVolume.Name = "ToolStripMenuItem_SpecificVolume";
            this.ToolStripMenuItem_SpecificVolume.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_SpecificVolume.Text = "Specific Volume";
            this.ToolStripMenuItem_SpecificVolume.Click += new System.EventHandler(this.ToolStripMenuItem_SpecificVolume_Click);
            // 
            // ToolStripMenuItem_WetBulbTemperature
            // 
            this.ToolStripMenuItem_WetBulbTemperature.Checked = true;
            this.ToolStripMenuItem_WetBulbTemperature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_WetBulbTemperature.Name = "ToolStripMenuItem_WetBulbTemperature";
            this.ToolStripMenuItem_WetBulbTemperature.Size = new System.Drawing.Size(240, 26);
            this.ToolStripMenuItem_WetBulbTemperature.Text = "Wet Bulb Temperature";
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
            // graphSettingToolStripMenuItem
            // 
            this.graphSettingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.temperatureAxisToolStripMenuItem,
            this.humidityRatioAxisToolStripMenuItem});
            this.graphSettingToolStripMenuItem.Name = "graphSettingToolStripMenuItem";
            this.graphSettingToolStripMenuItem.Size = new System.Drawing.Size(120, 26);
            this.graphSettingToolStripMenuItem.Text = "Graph Settings";
            this.graphSettingToolStripMenuItem.Click += new System.EventHandler(this.graphSettingToolStripMenuItem_Click);
            // 
            // temperatureAxisToolStripMenuItem
            // 
            this.temperatureAxisToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minimumValueToolStripMenuItem,
            this.maximumValueToolStripMenuItem,
            this.intervalToolStripMenuItem});
            this.temperatureAxisToolStripMenuItem.Name = "temperatureAxisToolStripMenuItem";
            this.temperatureAxisToolStripMenuItem.Size = new System.Drawing.Size(223, 26);
            this.temperatureAxisToolStripMenuItem.Text = "Temperature Axis";
            // 
            // minimumValueToolStripMenuItem
            // 
            this.minimumValueToolStripMenuItem.Name = "minimumValueToolStripMenuItem";
            this.minimumValueToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.minimumValueToolStripMenuItem.Text = "Minimum Value";
            // 
            // maximumValueToolStripMenuItem
            // 
            this.maximumValueToolStripMenuItem.Name = "maximumValueToolStripMenuItem";
            this.maximumValueToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.maximumValueToolStripMenuItem.Text = "Maximum Value";
            // 
            // intervalToolStripMenuItem
            // 
            this.intervalToolStripMenuItem.Name = "intervalToolStripMenuItem";
            this.intervalToolStripMenuItem.Size = new System.Drawing.Size(198, 26);
            this.intervalToolStripMenuItem.Text = "Interval";
            // 
            // humidityRatioAxisToolStripMenuItem
            // 
            this.humidityRatioAxisToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minimumValueToolStripMenuItem1,
            this.maximumValueToolStripMenuItem1,
            this.intervalToolStripMenuItem1});
            this.humidityRatioAxisToolStripMenuItem.Name = "humidityRatioAxisToolStripMenuItem";
            this.humidityRatioAxisToolStripMenuItem.Size = new System.Drawing.Size(223, 26);
            this.humidityRatioAxisToolStripMenuItem.Text = "Humidity Ratio Axis";
            // 
            // minimumValueToolStripMenuItem1
            // 
            this.minimumValueToolStripMenuItem1.Name = "minimumValueToolStripMenuItem1";
            this.minimumValueToolStripMenuItem1.Size = new System.Drawing.Size(198, 26);
            this.minimumValueToolStripMenuItem1.Text = "Minimum Value";
            // 
            // maximumValueToolStripMenuItem1
            // 
            this.maximumValueToolStripMenuItem1.Name = "maximumValueToolStripMenuItem1";
            this.maximumValueToolStripMenuItem1.Size = new System.Drawing.Size(198, 26);
            this.maximumValueToolStripMenuItem1.Text = "Maximum Value";
            // 
            // intervalToolStripMenuItem1
            // 
            this.intervalToolStripMenuItem1.Name = "intervalToolStripMenuItem1";
            this.intervalToolStripMenuItem1.Size = new System.Drawing.Size(198, 26);
            this.intervalToolStripMenuItem1.Text = "Interval";
            // 
            // TemperatureMinimumValueTextbox
            // 
            this.TemperatureMinimumValueTextbox.Location = new System.Drawing.Point(336, 40);
            this.TemperatureMinimumValueTextbox.Name = "TemperatureMinimumValueTextbox";
            this.TemperatureMinimumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureMinimumValueTextbox.TabIndex = 24;
            this.TemperatureMinimumValueTextbox.Text = "-20";
            this.TemperatureMinimumValueTextbox.TextChanged += new System.EventHandler(this.TemperatureMinimumValueTextbox_TextChanged);
            // 
            // TemperatureMaximumValueTextbox
            // 
            this.TemperatureMaximumValueTextbox.Location = new System.Drawing.Point(441, 40);
            this.TemperatureMaximumValueTextbox.Name = "TemperatureMaximumValueTextbox";
            this.TemperatureMaximumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureMaximumValueTextbox.TabIndex = 25;
            this.TemperatureMaximumValueTextbox.Text = "50";
            this.TemperatureMaximumValueTextbox.TextChanged += new System.EventHandler(this.TemperatureMaximumValueTextbox_TextChanged);
            // 
            // HumidityRatioMinimumValueTextbox
            // 
            this.HumidityRatioMinimumValueTextbox.Location = new System.Drawing.Point(776, 40);
            this.HumidityRatioMinimumValueTextbox.Name = "HumidityRatioMinimumValueTextbox";
            this.HumidityRatioMinimumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioMinimumValueTextbox.TabIndex = 26;
            this.HumidityRatioMinimumValueTextbox.Text = "0";
            this.HumidityRatioMinimumValueTextbox.TextChanged += new System.EventHandler(this.HumidityRatioMinimumValueTextbox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(424, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 16);
            this.label1.TabIndex = 27;
            this.label1.Text = "Temperature Axis";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(339, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 16);
            this.label2.TabIndex = 28;
            this.label2.Text = "Minimum Value";
            // 
            // HumidityRatioMaximumValueTextbox
            // 
            this.HumidityRatioMaximumValueTextbox.Location = new System.Drawing.Point(881, 40);
            this.HumidityRatioMaximumValueTextbox.Name = "HumidityRatioMaximumValueTextbox";
            this.HumidityRatioMaximumValueTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioMaximumValueTextbox.TabIndex = 29;
            this.HumidityRatioMaximumValueTextbox.Text = "35";
            this.HumidityRatioMaximumValueTextbox.TextChanged += new System.EventHandler(this.HumidityRatioMaximumValueTextbox_TextChanged);
            // 
            // HumidityRatioIntervalTextbox
            // 
            this.HumidityRatioIntervalTextbox.Location = new System.Drawing.Point(986, 40);
            this.HumidityRatioIntervalTextbox.Name = "HumidityRatioIntervalTextbox";
            this.HumidityRatioIntervalTextbox.Size = new System.Drawing.Size(99, 22);
            this.HumidityRatioIntervalTextbox.TabIndex = 30;
            this.HumidityRatioIntervalTextbox.Text = "5";
            this.HumidityRatioIntervalTextbox.TextChanged += new System.EventHandler(this.HumidityRatioIntervalTextbox_TextChanged);
            // 
            // TemperatureIntervalTextbox
            // 
            this.TemperatureIntervalTextbox.Location = new System.Drawing.Point(546, 40);
            this.TemperatureIntervalTextbox.Name = "TemperatureIntervalTextbox";
            this.TemperatureIntervalTextbox.Size = new System.Drawing.Size(99, 22);
            this.TemperatureIntervalTextbox.TabIndex = 31;
            this.TemperatureIntervalTextbox.Text = "5";
            this.TemperatureIntervalTextbox.TextChanged += new System.EventHandler(this.TemperatureIntervalTextbox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(441, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 16);
            this.label3.TabIndex = 32;
            this.label3.Text = "Maximum Value";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(568, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 16);
            this.label4.TabIndex = 33;
            this.label4.Text = "Interval";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label5.Location = new System.Drawing.Point(861, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(119, 16);
            this.label5.TabIndex = 34;
            this.label5.Text = "HumidityRatio Axis";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label6.Location = new System.Drawing.Point(777, 21);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(98, 16);
            this.label6.TabIndex = 35;
            this.label6.Text = "Minimum Value";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(881, 21);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 16);
            this.label7.TabIndex = 36;
            this.label7.Text = "Maximum Value";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label8.Location = new System.Drawing.Point(1011, 21);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(50, 16);
            this.label8.TabIndex = 37;
            this.label8.Text = "Interval";
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
            this.MollierControl_Main.Location = new System.Drawing.Point(12, 65);
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Pressure = 101325D;
            this.MollierControl_Main.Size = new System.Drawing.Size(1350, 912);
            this.MollierControl_Main.Specific_volume_line = true;
            this.MollierControl_Main.TabIndex = 0;
            this.MollierControl_Main.Wet_bulb_temperature_line = true;
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1374, 977);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TemperatureIntervalTextbox);
            this.Controls.Add(this.HumidityRatioIntervalTextbox);
            this.Controls.Add(this.HumidityRatioMaximumValueTextbox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.HumidityRatioMinimumValueTextbox);
            this.Controls.Add(this.TemperatureMaximumValueTextbox);
            this.Controls.Add(this.TemperatureMinimumValueTextbox);
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
        private System.Windows.Forms.ToolStripMenuItem graphSettingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem temperatureAxisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minimumValueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem maximumValueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem intervalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem humidityRatioAxisToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minimumValueToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem maximumValueToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem intervalToolStripMenuItem1;
        private System.Windows.Forms.TextBox TemperatureMinimumValueTextbox;
        private System.Windows.Forms.TextBox TemperatureMaximumValueTextbox;
        private System.Windows.Forms.TextBox HumidityRatioMinimumValueTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox HumidityRatioMaximumValueTextbox;
        private System.Windows.Forms.TextBox HumidityRatioIntervalTextbox;
        private System.Windows.Forms.TextBox TemperatureIntervalTextbox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
    }
}

