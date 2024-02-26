
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
            SAM.Core.Mollier.UI.MollierControlSettings mollierControlSettings1 = new SAM.Core.Mollier.UI.MollierControlSettings();
            SAM.Core.Mollier.VisibilitySettings visibilitySettings1 = new SAM.Core.Mollier.VisibilitySettings();
            SAM.Core.Mollier.MollierModel mollierModel1 = new SAM.Core.Mollier.MollierModel();
            this.TextBox_Pressure = new System.Windows.Forms.TextBox();
            this.Label_Pressure = new System.Windows.Forms.Label();
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
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_AddPoint = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_AddProcess = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripMenuItem_Epsilon = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SHR = new System.Windows.Forms.ToolStripMenuItem();
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
            this.ToolStripMenuItem_ComfortZones = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_OpenSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.resetChartToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Wiki = new System.Windows.Forms.ToolStripMenuItem();
            this.Label_Elevation = new System.Windows.Forms.Label();
            this.TextBox_Elevation = new System.Windows.Forms.TextBox();
            this.CheckBox_Hoover = new System.Windows.Forms.CheckBox();
            this.PointsCheckBox = new System.Windows.Forms.CheckBox();
            this.PercentPointsTextBox = new System.Windows.Forms.TextBox();
            this.PointsLabel = new System.Windows.Forms.Label();
            this.ColorPointComboBox = new System.Windows.Forms.ComboBox();
            this.DivisionAreaLabels_CheckBox = new System.Windows.Forms.CheckBox();
            this.MollierControl_Main = new SAM.Core.Mollier.UI.Controls.MollierControl();
            this.ToolStripMenuItem_DivisionArea = new System.Windows.Forms.ToolStripMenuItem();
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
            // MenuStrip_Main
            // 
            this.MenuStrip_Main.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MenuStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_File,
            this.editToolStripMenuItem,
            this.ToolStripMenuItem_View,
            this.ToolStripMenuItem_Settings,
            this.resetChartToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.MenuStrip_Main.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip_Main.Name = "MenuStrip_Main";
            this.MenuStrip_Main.ShowItemToolTips = true;
            this.MenuStrip_Main.Size = new System.Drawing.Size(2089, 28);
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
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_AddPoint,
            this.ToolStripMenuItem_AddProcess,
            this.ToolStripMenuItem_Edit,
            this.toolStripSeparator1,
            this.ToolStripMenuItem_Epsilon,
            this.ToolStripMenuItem_SHR});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(49, 24);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // ToolStripMenuItem_AddPoint
            // 
            this.ToolStripMenuItem_AddPoint.Name = "ToolStripMenuItem_AddPoint";
            this.ToolStripMenuItem_AddPoint.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_AddPoint.Text = "Add Point";
            this.ToolStripMenuItem_AddPoint.Click += new System.EventHandler(this.ToolStripMenuItem_AddPoint_Click);
            // 
            // ToolStripMenuItem_AddProcess
            // 
            this.ToolStripMenuItem_AddProcess.Name = "ToolStripMenuItem_AddProcess";
            this.ToolStripMenuItem_AddProcess.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_AddProcess.Text = "Add Process";
            this.ToolStripMenuItem_AddProcess.Click += new System.EventHandler(this.ToolStripMenuItem_AddProcess_Click);
            // 
            // ToolStripMenuItem_Edit
            // 
            this.ToolStripMenuItem_Edit.Name = "ToolStripMenuItem_Edit";
            this.ToolStripMenuItem_Edit.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_Edit.Text = "Edit/Delete";
            this.ToolStripMenuItem_Edit.Click += new System.EventHandler(this.ToolStripMenuItem_Edit_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(221, 6);
            // 
            // ToolStripMenuItem_Epsilon
            // 
            this.ToolStripMenuItem_Epsilon.Name = "ToolStripMenuItem_Epsilon";
            this.ToolStripMenuItem_Epsilon.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_Epsilon.Text = "ε";
            this.ToolStripMenuItem_Epsilon.Click += new System.EventHandler(this.ToolStripMenuItem_Epsilon_Click);
            // 
            // ToolStripMenuItem_SHR
            // 
            this.ToolStripMenuItem_SHR.Name = "ToolStripMenuItem_SHR";
            this.ToolStripMenuItem_SHR.Size = new System.Drawing.Size(224, 26);
            this.ToolStripMenuItem_SHR.Text = "SHR";
            this.ToolStripMenuItem_SHR.Click += new System.EventHandler(this.ToolStripMenuItem_SHR_Click);
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
            this.colorThemeToolStripMenuItem,
            this.ToolStripMenuItem_ComfortZones,
            this.ToolStripMenuItem_DivisionArea});
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
            // ToolStripMenuItem_ComfortZones
            // 
            this.ToolStripMenuItem_ComfortZones.Name = "ToolStripMenuItem_ComfortZones";
            this.ToolStripMenuItem_ComfortZones.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_ComfortZones.Text = "Comfort Zones";
            this.ToolStripMenuItem_ComfortZones.CheckedChanged += new System.EventHandler(this.ToolStripMenuItem_ComfortZones_CheckedChanged);
            this.ToolStripMenuItem_ComfortZones.Click += new System.EventHandler(this.ToolStripMenuItem_ComfortZoners_Click);
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
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Wiki});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(55, 24);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // ToolStripMenuItem_Wiki
            // 
            this.ToolStripMenuItem_Wiki.Name = "ToolStripMenuItem_Wiki";
            this.ToolStripMenuItem_Wiki.Size = new System.Drawing.Size(121, 26);
            this.ToolStripMenuItem_Wiki.Text = "Wiki";
            this.ToolStripMenuItem_Wiki.Click += new System.EventHandler(this.ToolStripMenuItem_Wiki_Click);
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
            // CheckBox_Hoover
            // 
            this.CheckBox_Hoover.AutoSize = true;
            this.CheckBox_Hoover.Location = new System.Drawing.Point(357, 32);
            this.CheckBox_Hoover.Name = "CheckBox_Hoover";
            this.CheckBox_Hoover.Size = new System.Drawing.Size(74, 20);
            this.CheckBox_Hoover.TabIndex = 29;
            this.CheckBox_Hoover.Text = "Hoover";
            this.CheckBox_Hoover.UseVisualStyleBackColor = true;
            this.CheckBox_Hoover.CheckedChanged += new System.EventHandler(this.CheckBox_Hoover_CheckedChanged);
            // 
            // PointsCheckBox
            // 
            this.PointsCheckBox.AutoSize = true;
            this.PointsCheckBox.Location = new System.Drawing.Point(437, 33);
            this.PointsCheckBox.Name = "PointsCheckBox";
            this.PointsCheckBox.Size = new System.Drawing.Size(95, 20);
            this.PointsCheckBox.TabIndex = 30;
            this.PointsCheckBox.Text = "Find Points";
            this.PointsCheckBox.UseVisualStyleBackColor = true;
            this.PointsCheckBox.CheckedChanged += new System.EventHandler(this.PointsCheckBox_CheckedChanged);
            // 
            // PercentPointsTextBox
            // 
            this.PercentPointsTextBox.Location = new System.Drawing.Point(687, 31);
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
            this.PointsLabel.Location = new System.Drawing.Point(726, 34);
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
            this.ColorPointComboBox.Location = new System.Drawing.Point(538, 30);
            this.ColorPointComboBox.Name = "ColorPointComboBox";
            this.ColorPointComboBox.Size = new System.Drawing.Size(143, 24);
            this.ColorPointComboBox.TabIndex = 38;
            this.ColorPointComboBox.Visible = false;
            this.ColorPointComboBox.SelectedIndexChanged += new System.EventHandler(this.ColorPointComboBox_SelectedIndexChanged);
            // 
            // DivisionAreaLabels_CheckBox
            // 
            this.DivisionAreaLabels_CheckBox.AutoSize = true;
            this.DivisionAreaLabels_CheckBox.Location = new System.Drawing.Point(802, 34);
            this.DivisionAreaLabels_CheckBox.Name = "DivisionAreaLabels_CheckBox";
            this.DivisionAreaLabels_CheckBox.Size = new System.Drawing.Size(119, 20);
            this.DivisionAreaLabels_CheckBox.TabIndex = 41;
            this.DivisionAreaLabels_CheckBox.Text = "Turn Off Labels";
            this.DivisionAreaLabels_CheckBox.UseVisualStyleBackColor = true;
            this.DivisionAreaLabels_CheckBox.Visible = false;
            this.DivisionAreaLabels_CheckBox.CheckedChanged += new System.EventHandler(this.DivisionAreaLabels_CheckBox_CheckedChanged);
            // 
            // MollierControl_Main
            // 
            this.MollierControl_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MollierControl_Main.EnableHoover = false;
            this.MollierControl_Main.Location = new System.Drawing.Point(10, 64);
            this.MollierControl_Main.Margin = new System.Windows.Forms.Padding(2);
            mollierControlSettings1.ChartType = SAM.Core.Mollier.ChartType.Mollier;
            mollierControlSettings1.DefaultTemplateName = "default";
            mollierControlSettings1.Density_Interval = 0.05D;
            mollierControlSettings1.Density_Line = true;
            mollierControlSettings1.Density_Max = 1.41D;
            mollierControlSettings1.Density_Min = 0.45D;
            mollierControlSettings1.DisableCoolingAuxiliaryProcesses = false;
            mollierControlSettings1.DisableEndProcessPoint = false;
            mollierControlSettings1.DisableLabelEndProcessPoint = false;
            mollierControlSettings1.DisableLabelProcess = false;
            mollierControlSettings1.DisableLabels = false;
            mollierControlSettings1.DisableLabelStartProcessPoint = false;
            mollierControlSettings1.DisablePoint = false;
            mollierControlSettings1.DisablePointBoarder = false;
            mollierControlSettings1.DisableStartProcessPoint = false;
            mollierControlSettings1.DisableUnits = false;
            mollierControlSettings1.DivisionArea = false;
            mollierControlSettings1.DivisionAreaEnthalpy_Interval = 3;
            mollierControlSettings1.DivisionAreaLabels = true;
            mollierControlSettings1.DivisionAreaRelativeHumidity_Interval = 10;
            mollierControlSettings1.Elevation = 0D;
            mollierControlSettings1.Enthalpy_Interval = 1000D;
            mollierControlSettings1.Enthalpy_Line = true;
            mollierControlSettings1.Enthalpy_Max = 140000D;
            mollierControlSettings1.Enthalpy_Min = -20000D;
            mollierControlSettings1.FindPoint = false;
            mollierControlSettings1.FindPoint_Factor = 0.4D;
            mollierControlSettings1.FindPointType = SAM.Core.Mollier.ChartDataType.Enthalpy;
            mollierControlSettings1.GradientPoint = false;
            mollierControlSettings1.HumidityRatio_Interval = 0.005D;
            mollierControlSettings1.HumidityRatio_Max = 0.035D;
            mollierControlSettings1.HumidityRatio_Min = 0D;
            mollierControlSettings1.MollierWindowHeight = -1;
            mollierControlSettings1.MollierWindowWidth = -1;
            mollierControlSettings1.PartialVapourPressure_Axis = true;
            mollierControlSettings1.PartialVapourPressure_Interval = 0.5D;
            mollierControlSettings1.PointBorderColor = System.Drawing.Color.Empty;
            mollierControlSettings1.PointBorderSize = -1;
            mollierControlSettings1.PointColor = System.Drawing.Color.Empty;
            mollierControlSettings1.PointSize = -1;
            mollierControlSettings1.Pressure = 101325D;
            mollierControlSettings1.ProccessLineThickness = -1;
            mollierControlSettings1.PsychrometricWindowHeight = -1;
            mollierControlSettings1.PsychrometricWindowWidth = -1;
            mollierControlSettings1.SpecificVolume_Interval = 0.05D;
            mollierControlSettings1.SpecificVolume_Line = true;
            mollierControlSettings1.SpecificVolume_Max = 1.92D;
            mollierControlSettings1.SpecificVolume_Min = 0.65D;
            mollierControlSettings1.Temperature_Interval = 5D;
            mollierControlSettings1.Temperature_Max = 50D;
            mollierControlSettings1.Temperature_Min = -20D;
            mollierControlSettings1.UIMollierZoneColor = System.Drawing.Color.Red;
            mollierControlSettings1.UIMollierZoneText = "";
            mollierControlSettings1.VisibilitySettings = visibilitySettings1;
            mollierControlSettings1.VisualizeSolver = false;
            mollierControlSettings1.WetBulbTemperature_Interval = 5D;
            mollierControlSettings1.WetBulbTemperature_Line = true;
            mollierControlSettings1.WetBulbTemperature_Max = 30D;
            mollierControlSettings1.WetBulbTemperature_Min = -10D;
            this.MollierControl_Main.MollierControlSettings = mollierControlSettings1;
            this.MollierControl_Main.MollierModel = mollierModel1;
            this.MollierControl_Main.Name = "MollierControl_Main";
            this.MollierControl_Main.Size = new System.Drawing.Size(2054, 1219);
            this.MollierControl_Main.TabIndex = 0;
            // 
            // ToolStripMenuItem_DivisionArea
            // 
            this.ToolStripMenuItem_DivisionArea.Name = "ToolStripMenuItem_DivisionArea";
            this.ToolStripMenuItem_DivisionArea.Size = new System.Drawing.Size(242, 26);
            this.ToolStripMenuItem_DivisionArea.Text = "Division Area";
            this.ToolStripMenuItem_DivisionArea.Click += new System.EventHandler(this.ToolStripMenuItem_DivisionArea_Click);
            // 
            // MollierForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(2089, 1289);
            this.Controls.Add(this.DivisionAreaLabels_CheckBox);
            this.Controls.Add(this.ColorPointComboBox);
            this.Controls.Add(this.PointsLabel);
            this.Controls.Add(this.PercentPointsTextBox);
            this.Controls.Add(this.PointsCheckBox);
            this.Controls.Add(this.CheckBox_Hoover);
            this.Controls.Add(this.TextBox_Elevation);
            this.Controls.Add(this.Label_Elevation);
            this.Controls.Add(this.TextBox_Pressure);
            this.Controls.Add(this.Label_Pressure);
            this.Controls.Add(this.MenuStrip_Main);
            this.Controls.Add(this.MollierControl_Main);
            this.KeyPreview = true;
            this.MainMenuStrip = this.MenuStrip_Main;
            this.Name = "MollierForm";
            this.ShowIcon = false;
            this.Text = "Mollier";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MollierForm_FormClosing);
            this.Load += new System.EventHandler(this.MollierForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MollierForm_KeyDown);
            this.MenuStrip_Main.ResumeLayout(false);
            this.MenuStrip_Main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.MollierControl MollierControl_Main;
        private System.Windows.Forms.TextBox TextBox_Pressure;
        private System.Windows.Forms.Label Label_Pressure;
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
        private System.Windows.Forms.CheckBox CheckBox_Hoover;
        private System.Windows.Forms.ToolStripMenuItem PdfA3_PortraitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PdfA3_LandscapeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsJPGToolStripMenuItem;
        private System.Windows.Forms.CheckBox PointsCheckBox;
        private System.Windows.Forms.TextBox PercentPointsTextBox;
        private System.Windows.Forms.Label PointsLabel;
        private System.Windows.Forms.ComboBox ColorPointComboBox;
        private System.Windows.Forms.CheckBox DivisionAreaLabels_CheckBox;
        private System.Windows.Forms.ToolStripMenuItem saveAsEMFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem a4PortraitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem a4LandscapeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem blueBlackToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFromJSONToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToJSONToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem printToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_PartialVapourPressure;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_ComfortZones;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Wiki;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_AddPoint;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_AddProcess;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Edit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Epsilon;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_SHR;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_DivisionArea;
    }
}

