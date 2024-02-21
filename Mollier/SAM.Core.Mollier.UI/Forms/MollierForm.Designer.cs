
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
            SAM.Core.Mollier.VisibilitySettings visibilitySettings2 = new SAM.Core.Mollier.VisibilitySettings();
            SAM.Core.Mollier.MollierModel mollierModel2 = new SAM.Core.Mollier.MollierModel();
            this.TextBox_Pressure = new System.Windows.Forms.TextBox();
            this.Label_Pressure = new System.Windows.Forms.Label();
            this.Button_AddPoint = new System.Windows.Forms.Button();
            this.MenuStrip_Main = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem_File = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFromJSONToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToJSONToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.PdfA3_PortraitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PdfA3_LandscapeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.a4PortraitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.a4LandscapeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsJPGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsEMFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.printToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_View = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Density = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Enthalpy = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SpecificVolume = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_WetBulbTemperature = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_PartialVapourPressure = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_ChartType = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Mollier = new System.Windows.Forms.ToolStripMenuItem();
            this.ChartToolStripMenuItem_Psychrometric = new System.Windows.Forms.ToolStripMenuItem();
            this.colorThemeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.defaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.grayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blueBlackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.ColorPointComboBox = new System.Windows.Forms.ComboBox();
            this.DivisionAreaCheckBox = new System.Windows.Forms.CheckBox();
            this.DivisionAreaLabels_CheckBox = new System.Windows.Forms.CheckBox();
            this.Button_Reset = new System.Windows.Forms.Button();
            this.Button_Psychrometric = new System.Windows.Forms.Button();
            this.Button_Mollier = new System.Windows.Forms.Button();
            this.Button_Epsilon = new System.Windows.Forms.Button();
            this.Button_SensibleHeatRatio = new System.Windows.Forms.Button();
            this.manageMollierObjectsButton = new System.Windows.Forms.Button();
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
            this.Button_AddPoint.Location = new System.Drawing.Point(1969, 30);
            this.Button_AddPoint.Name = "Button_AddPoint";
            this.Button_AddPoint.Size = new System.Drawing.Size(100, 28);
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
            this.MenuStrip_Main.Size = new System.Drawing.Size(2089, 30);
            this.MenuStrip_Main.TabIndex = 20;
            this.MenuStrip_Main.Text = "MenuStrip_Main";
            // 
            // ToolStripMenuItem_File
            // 
            this.ToolStripMenuItem_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openFromJSONToolStripMenuItem,
            this.exportToJSONToolStripMenuItem,
            this.ToolStripMenuItem_Save,
            this.saveAsJPGToolStripMenuItem,
            this.saveAsEMFToolStripMenuItem,
            this.printToolStripMenuItem});
            this.ToolStripMenuItem_File.Name = "ToolStripMenuItem_File";
            this.ToolStripMenuItem_File.Size = new System.Drawing.Size(46, 24);
            this.ToolStripMenuItem_File.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.newToolStripMenuItem.Text = "New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openFromJSONToolStripMenuItem
            // 
            this.openFromJSONToolStripMenuItem.Name = "openFromJSONToolStripMenuItem";
            this.openFromJSONToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.openFromJSONToolStripMenuItem.Text = "Open";
            this.openFromJSONToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // exportToJSONToolStripMenuItem
            // 
            this.exportToJSONToolStripMenuItem.Name = "exportToJSONToolStripMenuItem";
            this.exportToJSONToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.exportToJSONToolStripMenuItem.Text = "Save";
            this.exportToJSONToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_Save
            // 
            this.ToolStripMenuItem_Save.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PdfA3_PortraitToolStripMenuItem,
            this.PdfA3_LandscapeToolStripMenuItem,
            this.a4PortraitToolStripMenuItem,
            this.a4LandscapeToolStripMenuItem});
            this.ToolStripMenuItem_Save.Name = "ToolStripMenuItem_Save";
            this.ToolStripMenuItem_Save.Size = new System.Drawing.Size(173, 26);
            this.ToolStripMenuItem_Save.Text = "Save as PDF";
            // 
            // PdfA3_PortraitToolStripMenuItem
            // 
            this.PdfA3_PortraitToolStripMenuItem.Name = "PdfA3_PortraitToolStripMenuItem";
            this.PdfA3_PortraitToolStripMenuItem.Size = new System.Drawing.Size(186, 26);
            this.PdfA3_PortraitToolStripMenuItem.Text = "A3_Portrait";
            this.PdfA3_PortraitToolStripMenuItem.Click += new System.EventHandler(this.PdfA3_PortraitToolStripMenuItem_Click);
            // 
            // PdfA3_LandscapeToolStripMenuItem
            // 
            this.PdfA3_LandscapeToolStripMenuItem.Name = "PdfA3_LandscapeToolStripMenuItem";
            this.PdfA3_LandscapeToolStripMenuItem.Size = new System.Drawing.Size(186, 26);
            this.PdfA3_LandscapeToolStripMenuItem.Text = "A3_Landscape";
            this.PdfA3_LandscapeToolStripMenuItem.Click += new System.EventHandler(this.PdfA3_LandscapeToolStripMenuItem_Click);
            // 
            // a4PortraitToolStripMenuItem
            // 
            this.a4PortraitToolStripMenuItem.Name = "a4PortraitToolStripMenuItem";
            this.a4PortraitToolStripMenuItem.Size = new System.Drawing.Size(186, 26);
            this.a4PortraitToolStripMenuItem.Text = "A4_Portrait";
            this.a4PortraitToolStripMenuItem.Click += new System.EventHandler(this.a4PortraitToolStripMenuItem_Click);
            // 
            // a4LandscapeToolStripMenuItem
            // 
            this.a4LandscapeToolStripMenuItem.Name = "a4LandscapeToolStripMenuItem";
            this.a4LandscapeToolStripMenuItem.Size = new System.Drawing.Size(186, 26);
            this.a4LandscapeToolStripMenuItem.Text = "A4_Landscape";
            this.a4LandscapeToolStripMenuItem.Click += new System.EventHandler(this.a4LandscapeToolStripMenuItem_Click);
            // 
            // saveAsJPGToolStripMenuItem
            // 
            this.saveAsJPGToolStripMenuItem.Name = "saveAsJPGToolStripMenuItem";
            this.saveAsJPGToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.saveAsJPGToolStripMenuItem.Text = "Save as JPG";
            this.saveAsJPGToolStripMenuItem.Click += new System.EventHandler(this.saveAsJPGToolStripMenuItem_Click);
            // 
            // saveAsEMFToolStripMenuItem
            // 
            this.saveAsEMFToolStripMenuItem.Name = "saveAsEMFToolStripMenuItem";
            this.saveAsEMFToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.saveAsEMFToolStripMenuItem.Text = "Save as EMF";
            this.saveAsEMFToolStripMenuItem.Click += new System.EventHandler(this.saveAsEMFToolStripMenuItem_Click);
            // 
            // printToolStripMenuItem
            // 
            this.printToolStripMenuItem.Name = "printToolStripMenuItem";
            this.printToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
            this.printToolStripMenuItem.Text = "Print";
            this.printToolStripMenuItem.Click += new System.EventHandler(this.printToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_View
            // 
            this.ToolStripMenuItem_View.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Density,
            this.ToolStripMenuItem_Enthalpy,
            this.ToolStripMenuItem_SpecificVolume,
            this.ToolStripMenuItem_WetBulbTemperature,
            this.ToolStripMenuItem_PartialVapourPressure,
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
            this.ToolStripMenuItem_Density.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_Density.Text = "Density";
            this.ToolStripMenuItem_Density.ToolTipText = "Turn Density Line on or off";
            this.ToolStripMenuItem_Density.Click += new System.EventHandler(this.ToolStripMenuItem_Density_Click);
            // 
            // ToolStripMenuItem_Enthalpy
            // 
            this.ToolStripMenuItem_Enthalpy.Checked = true;
            this.ToolStripMenuItem_Enthalpy.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_Enthalpy.Name = "ToolStripMenuItem_Enthalpy";
            this.ToolStripMenuItem_Enthalpy.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_Enthalpy.Text = "Enthalpy";
            this.ToolStripMenuItem_Enthalpy.ToolTipText = "Turn Enthalpy Line on or off";
            this.ToolStripMenuItem_Enthalpy.Click += new System.EventHandler(this.ToolStripMenuItem_Enthalpy_Click);
            // 
            // ToolStripMenuItem_SpecificVolume
            // 
            this.ToolStripMenuItem_SpecificVolume.Checked = true;
            this.ToolStripMenuItem_SpecificVolume.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_SpecificVolume.Name = "ToolStripMenuItem_SpecificVolume";
            this.ToolStripMenuItem_SpecificVolume.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_SpecificVolume.Text = "Specific Volume";
            this.ToolStripMenuItem_SpecificVolume.ToolTipText = "Turn Specific Volume Line on or off";
            this.ToolStripMenuItem_SpecificVolume.Click += new System.EventHandler(this.ToolStripMenuItem_SpecificVolume_Click);
            // 
            // ToolStripMenuItem_WetBulbTemperature
            // 
            this.ToolStripMenuItem_WetBulbTemperature.Checked = true;
            this.ToolStripMenuItem_WetBulbTemperature.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_WetBulbTemperature.Name = "ToolStripMenuItem_WetBulbTemperature";
            this.ToolStripMenuItem_WetBulbTemperature.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_WetBulbTemperature.Text = "Wet Bulb Temperature";
            this.ToolStripMenuItem_WetBulbTemperature.ToolTipText = "Turn Wet-Bulb Temperature Line on or off";
            this.ToolStripMenuItem_WetBulbTemperature.Click += new System.EventHandler(this.ToolStripMenuItem_WetBulbTemperature_Click);
            // 
            // ToolStripMenuItem_PartialVapourPressure
            // 
            this.ToolStripMenuItem_PartialVapourPressure.Checked = true;
            this.ToolStripMenuItem_PartialVapourPressure.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToolStripMenuItem_PartialVapourPressure.Name = "ToolStripMenuItem_PartialVapourPressure";
            this.ToolStripMenuItem_PartialVapourPressure.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_PartialVapourPressure.Text = "Partial Vapour Pressure";
            this.ToolStripMenuItem_PartialVapourPressure.ToolTipText = "Turn Partial Vapour Pressure axis on or off";
            this.ToolStripMenuItem_PartialVapourPressure.Click += new System.EventHandler(this.ToolStripMenuItem_PartialVapourPressure_Click);
            // 
            // ToolStripMenuItem_ChartType
            // 
            this.ToolStripMenuItem_ChartType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ChartToolStripMenuItem_Mollier,
            this.ChartToolStripMenuItem_Psychrometric});
            this.ToolStripMenuItem_ChartType.Name = "ToolStripMenuItem_ChartType";
            this.ToolStripMenuItem_ChartType.Size = new System.Drawing.Size(242, 26);
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
            this.grayToolStripMenuItem,
            this.blueBlackToolStripMenuItem});
            this.colorThemeToolStripMenuItem.Name = "colorThemeToolStripMenuItem";
            this.colorThemeToolStripMenuItem.Size = new System.Drawing.Size(242, 26);
            this.colorThemeToolStripMenuItem.Text = "Color Theme";
            // 
            // defaultToolStripMenuItem
            // 
            this.defaultToolStripMenuItem.Checked = true;
            this.defaultToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.defaultToolStripMenuItem.Name = "defaultToolStripMenuItem";
            this.defaultToolStripMenuItem.Size = new System.Drawing.Size(162, 26);
            this.defaultToolStripMenuItem.Text = "Default";
            this.defaultToolStripMenuItem.Click += new System.EventHandler(this.defaultToolStripMenuItem_Click);
            // 
            // blueToolStripMenuItem
            // 
            this.blueToolStripMenuItem.Name = "blueToolStripMenuItem";
            this.blueToolStripMenuItem.Size = new System.Drawing.Size(162, 26);
            this.blueToolStripMenuItem.Text = "Blue";
            this.blueToolStripMenuItem.Click += new System.EventHandler(this.blueToolStripMenuItem_Click);
            // 
            // grayToolStripMenuItem
            // 
            this.grayToolStripMenuItem.Name = "grayToolStripMenuItem";
            this.grayToolStripMenuItem.Size = new System.Drawing.Size(162, 26);
            this.grayToolStripMenuItem.Text = "Gray";
            this.grayToolStripMenuItem.Click += new System.EventHandler(this.grayToolStripMenuItem_Click);
            // 
            // blueBlackToolStripMenuItem
            // 
            this.blueBlackToolStripMenuItem.Name = "blueBlackToolStripMenuItem";
            this.blueBlackToolStripMenuItem.Size = new System.Drawing.Size(162, 26);
            this.blueBlackToolStripMenuItem.Text = "Blue-Black";
            this.blueBlackToolStripMenuItem.Click += new System.EventHandler(this.blueBlackToolStripMenuItem_Click);
            // 
            // ToolStripMenuItem_Settings
            // 
            this.ToolStripMenuItem_Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_OpenSettings});
            this.ToolStripMenuItem_Settings.Name = "ToolStripMenuItem_Settings";
            this.ToolStripMenuItem_Settings.Size = new System.Drawing.Size(76, 24);
            this.ToolStripMenuItem_Settings.Text = "Settings";
            // 
            // ToolStripMenuItem_OpenSettings
            // 
            this.ToolStripMenuItem_OpenSettings.Name = "ToolStripMenuItem_OpenSettings";
            this.ToolStripMenuItem_OpenSettings.Size = new System.Drawing.Size(145, 26);
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
            this.Button_AddProcess.Location = new System.Drawing.Point(1863, 30);
            this.Button_AddProcess.Name = "Button_AddProcess";
            this.Button_AddProcess.Size = new System.Drawing.Size(100, 28);
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
            this.PointsCheckBox.Location = new System.Drawing.Point(594, 32);
            this.PointsCheckBox.Name = "PointsCheckBox";
            this.PointsCheckBox.Size = new System.Drawing.Size(95, 20);
            this.PointsCheckBox.TabIndex = 30;
            this.PointsCheckBox.Text = "Find Points";
            this.PointsCheckBox.UseVisualStyleBackColor = true;
            this.PointsCheckBox.CheckedChanged += new System.EventHandler(this.PointsCheckBox_CheckedChanged);
            // 
            // PercentPointsTextBox
            // 
            this.PercentPointsTextBox.Location = new System.Drawing.Point(844, 30);
            this.PercentPointsTextBox.Name = "PercentPointsTextBox";
            this.PercentPointsTextBox.Size = new System.Drawing.Size(33, 22);
            this.PercentPointsTextBox.TabIndex = 31;
            this.PercentPointsTextBox.Text = "0.4";
            this.PercentPointsTextBox.Visible = false;
            this.PercentPointsTextBox.TextChanged += new System.EventHandler(this.PercentPointsTextBox_TextChanged);
            this.PercentPointsTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.PercentPointsTextBox.MouseLeave += new System.EventHandler(this.PercentPointsTextBox_MouseLeave);
            this.PercentPointsTextBox.MouseHover += new System.EventHandler(this.PercentPointsTextBox_MouseHover);
            // 
            // PointsLabel
            // 
            this.PointsLabel.AutoSize = true;
            this.PointsLabel.Location = new System.Drawing.Point(883, 33);
            this.PointsLabel.Name = "PointsLabel";
            this.PointsLabel.Size = new System.Drawing.Size(19, 16);
            this.PointsLabel.TabIndex = 32;
            this.PointsLabel.Text = "%";
            this.PointsLabel.Visible = false;
            // 
            // ColorPointComboBox
            // 
            this.ColorPointComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ColorPointComboBox.FormattingEnabled = true;
            this.ColorPointComboBox.Items.AddRange(new object[] {
            "Temperature",
            "Enthalpy"});
            this.ColorPointComboBox.Location = new System.Drawing.Point(695, 29);
            this.ColorPointComboBox.Name = "ColorPointComboBox";
            this.ColorPointComboBox.Size = new System.Drawing.Size(143, 24);
            this.ColorPointComboBox.TabIndex = 38;
            this.ColorPointComboBox.Visible = false;
            this.ColorPointComboBox.SelectedIndexChanged += new System.EventHandler(this.ColorPointComboBox_SelectedIndexChanged);
            // 
            // DivisionAreaCheckBox
            // 
            this.DivisionAreaCheckBox.AutoSize = true;
            this.DivisionAreaCheckBox.Location = new System.Drawing.Point(479, 32);
            this.DivisionAreaCheckBox.Name = "DivisionAreaCheckBox";
            this.DivisionAreaCheckBox.Size = new System.Drawing.Size(109, 20);
            this.DivisionAreaCheckBox.TabIndex = 40;
            this.DivisionAreaCheckBox.Text = "Division Area";
            this.DivisionAreaCheckBox.UseVisualStyleBackColor = true;
            this.DivisionAreaCheckBox.CheckedChanged += new System.EventHandler(this.DivisionAreaCheckBox_CheckedChanged);
            // 
            // DivisionAreaLabels_CheckBox
            // 
            this.DivisionAreaLabels_CheckBox.AutoSize = true;
            this.DivisionAreaLabels_CheckBox.Location = new System.Drawing.Point(479, 8);
            this.DivisionAreaLabels_CheckBox.Name = "DivisionAreaLabels_CheckBox";
            this.DivisionAreaLabels_CheckBox.Size = new System.Drawing.Size(119, 20);
            this.DivisionAreaLabels_CheckBox.TabIndex = 41;
            this.DivisionAreaLabels_CheckBox.Text = "Turn Off Labels";
            this.DivisionAreaLabels_CheckBox.UseVisualStyleBackColor = true;
            this.DivisionAreaLabels_CheckBox.Visible = false;
            this.DivisionAreaLabels_CheckBox.CheckedChanged += new System.EventHandler(this.DivisionAreaLabels_CheckBox_CheckedChanged);
            // 
            // Button_Reset
            // 
            this.Button_Reset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Reset.Location = new System.Drawing.Point(1757, 30);
            this.Button_Reset.Name = "Button_Reset";
            this.Button_Reset.Size = new System.Drawing.Size(100, 28);
            this.Button_Reset.TabIndex = 42;
            this.Button_Reset.Text = "Default Reset";
            this.Button_Reset.UseVisualStyleBackColor = true;
            this.Button_Reset.Click += new System.EventHandler(this.Button_Reset_Click);
            // 
            // Button_Psychrometric
            // 
            this.Button_Psychrometric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Psychrometric.Location = new System.Drawing.Point(1623, 30);
            this.Button_Psychrometric.Name = "Button_Psychrometric";
            this.Button_Psychrometric.Size = new System.Drawing.Size(128, 28);
            this.Button_Psychrometric.TabIndex = 43;
            this.Button_Psychrometric.Text = "Psychrometrics";
            this.Button_Psychrometric.UseVisualStyleBackColor = true;
            this.Button_Psychrometric.Click += new System.EventHandler(this.Button_Psychrometric_Click);
            // 
            // Button_Mollier
            // 
            this.Button_Mollier.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Mollier.Location = new System.Drawing.Point(1517, 30);
            this.Button_Mollier.Name = "Button_Mollier";
            this.Button_Mollier.Size = new System.Drawing.Size(100, 28);
            this.Button_Mollier.TabIndex = 44;
            this.Button_Mollier.Text = "Mollier";
            this.Button_Mollier.UseVisualStyleBackColor = true;
            this.Button_Mollier.Click += new System.EventHandler(this.Button_Mollier_Click);
            // 
            // Button_Epsilon
            // 
            this.Button_Epsilon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Epsilon.Location = new System.Drawing.Point(1461, 30);
            this.Button_Epsilon.Name = "Button_Epsilon";
            this.Button_Epsilon.Size = new System.Drawing.Size(50, 28);
            this.Button_Epsilon.TabIndex = 45;
            this.Button_Epsilon.Text = "ε";
            this.Button_Epsilon.UseVisualStyleBackColor = true;
            this.Button_Epsilon.Click += new System.EventHandler(this.Button_Epsilon_Click);
            // 
            // Button_SensibleHeatRatio
            // 
            this.Button_SensibleHeatRatio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_SensibleHeatRatio.Location = new System.Drawing.Point(1405, 30);
            this.Button_SensibleHeatRatio.Name = "Button_SensibleHeatRatio";
            this.Button_SensibleHeatRatio.Size = new System.Drawing.Size(50, 28);
            this.Button_SensibleHeatRatio.TabIndex = 46;
            this.Button_SensibleHeatRatio.Text = "SHR";
            this.Button_SensibleHeatRatio.UseVisualStyleBackColor = true;
            this.Button_SensibleHeatRatio.Click += new System.EventHandler(this.Button_SensibleHeatRatio_Click);
            // 
            // manageMollierObjectsButton
            // 
            this.manageMollierObjectsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.manageMollierObjectsButton.Location = new System.Drawing.Point(1280, 30);
            this.manageMollierObjectsButton.Name = "manageMollierObjectsButton";
            this.manageMollierObjectsButton.Size = new System.Drawing.Size(119, 30);
            this.manageMollierObjectsButton.TabIndex = 47;
            this.manageMollierObjectsButton.Text = "Edit / Delete";
            this.manageMollierObjectsButton.UseVisualStyleBackColor = true;
            this.manageMollierObjectsButton.Click += new System.EventHandler(this.customizeMollierObjectsButton_Click);
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MollierControl_Main.Location = new System.Drawing.Point(10, 64);
            this.MollierControl_Main.Margin = new System.Windows.Forms.Padding(2);
            mollierControlSettings2.ChartType = SAM.Core.Mollier.ChartType.Mollier;
            mollierControlSettings2.DefaultTemplateName = "default";
            mollierControlSettings2.Density_Interval = 0.02D;
            mollierControlSettings2.Density_Line = true;
            mollierControlSettings2.Density_Max = 1.41D;
            mollierControlSettings2.Density_Min = 0.45D;
            mollierControlSettings2.DisableLabels = false;
            mollierControlSettings2.DisableUnits = false;
            mollierControlSettings2.DivisionArea = false;
            mollierControlSettings2.DivisionAreaEnthalpy_Interval = 3;
            mollierControlSettings2.DivisionAreaLabels = true;
            mollierControlSettings2.DivisionAreaRelativeHumidity_Interval = 10;
            mollierControlSettings2.Elevation = 0D;
            mollierControlSettings2.Enthalpy_Interval = 1000D;
            mollierControlSettings2.Enthalpy_Line = true;
            mollierControlSettings2.Enthalpy_Max = 140000D;
            mollierControlSettings2.Enthalpy_Min = -20000D;
            mollierControlSettings2.FindPoint = false;
            mollierControlSettings2.FindPoint_Factor = 0.4D;
            mollierControlSettings2.FindPointType = SAM.Core.Mollier.ChartDataType.Enthalpy;
            mollierControlSettings2.GradientPoint = false;
            mollierControlSettings2.HumidityRatio_Interval = 0.005D;
            mollierControlSettings2.HumidityRatio_Max = 0.035D;
            mollierControlSettings2.HumidityRatio_Min = 0D;
            mollierControlSettings2.PartialVapourPressure_Axis = true;
            mollierControlSettings2.PartialVapourPressure_Interval = 1D;
            mollierControlSettings2.Pressure = 101325D;
            mollierControlSettings2.SpecificVolume_Interval = 0.05D;
            mollierControlSettings2.SpecificVolume_Line = true;
            mollierControlSettings2.SpecificVolume_Max = 1.92D;
            mollierControlSettings2.SpecificVolume_Min = 0.65D;
            mollierControlSettings2.Temperature_Interval = 5D;
            mollierControlSettings2.Temperature_Max = 50D;
            mollierControlSettings2.Temperature_Min = -20D;
            mollierControlSettings2.UIMollierZoneColor = System.Drawing.Color.Red;
            mollierControlSettings2.UIMollierZoneText = "";
            mollierControlSettings2.VisibilitySettings = visibilitySettings2;
            mollierControlSettings2.VisualizeSolver = false;
            mollierControlSettings2.WetBulbTemperature_Interval = 5D;
            mollierControlSettings2.WetBulbTemperature_Line = true;
            mollierControlSettings2.WetBulbTemperature_Max = 30D;
            mollierControlSettings2.WetBulbTemperature_Min = -10D;
            this.MollierControl_Main.MollierControlSettings = mollierControlSettings2;
            this.MollierControl_Main.MollierModel = mollierModel2;
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Size = new System.Drawing.Size(2054, 1219);
            this.MollierControl_Main.TabIndex = 0;
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(2089, 1289);
            this.Controls.Add(this.manageMollierObjectsButton);
            this.Controls.Add(this.Button_SensibleHeatRatio);
            this.Controls.Add(this.Button_Epsilon);
            this.Controls.Add(this.Button_Mollier);
            this.Controls.Add(this.Button_Psychrometric);
            this.Controls.Add(this.Button_Reset);
            this.Controls.Add(this.DivisionAreaLabels_CheckBox);
            this.Controls.Add(this.DivisionAreaCheckBox);
            this.Controls.Add(this.ColorPointComboBox);
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
        private System.Windows.Forms.ToolStripMenuItem PdfA3_PortraitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PdfA3_LandscapeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsJPGToolStripMenuItem;
        private System.Windows.Forms.CheckBox PointsCheckBox;
        private System.Windows.Forms.TextBox PercentPointsTextBox;
        private System.Windows.Forms.Label PointsLabel;
        private System.Windows.Forms.ComboBox ColorPointComboBox;
        private System.Windows.Forms.CheckBox DivisionAreaCheckBox;
        private System.Windows.Forms.CheckBox DivisionAreaLabels_CheckBox;
        private System.Windows.Forms.ToolStripMenuItem saveAsEMFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem a4PortraitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem a4LandscapeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blueBlackToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFromJSONToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToJSONToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.Button Button_Reset;
        private System.Windows.Forms.Button Button_Psychrometric;
        private System.Windows.Forms.Button Button_Mollier;
        private System.Windows.Forms.Button Button_Epsilon;
        private System.Windows.Forms.Button Button_SensibleHeatRatio;
        private System.Windows.Forms.Button manageMollierObjectsButton;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_PartialVapourPressure;
    }
}

