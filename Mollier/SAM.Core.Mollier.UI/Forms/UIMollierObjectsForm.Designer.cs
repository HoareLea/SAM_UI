namespace SAM.Core.Mollier.UI.Forms
{
    partial class UIMollierObjectsForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this.customizeMollierObjectsTabControl = new System.Windows.Forms.TabControl();
            this.tabCustomizePointPage = new System.Windows.Forms.TabPage();
            this.DataGridView_MollierPoints = new System.Windows.Forms.DataGridView();
            this.Column_Label = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_DryBulbTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_HumidityRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_RelativeHumidity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_WetBulbTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_DewPointTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_SpecificVolume = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_Enthalpy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_Density = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_Edit = new System.Windows.Forms.DataGridViewButtonColumn();
            this.Column_Remove = new System.Windows.Forms.DataGridViewButtonColumn();
            this.tabCustomizeProcessesPage = new System.Windows.Forms.TabPage();
            this.ExhaustAirflow_CheckBox = new System.Windows.Forms.CheckBox();
            this.SupplyAirFlow_CheckBox = new System.Windows.Forms.CheckBox();
            this.SupplyAirflow_ComboBox = new System.Windows.Forms.ComboBox();
            this.ExhaustAirflow_Combobox = new System.Windows.Forms.ComboBox();
            this.ExhaustAirFlow_TextBox = new System.Windows.Forms.TextBox();
            this.Pressure_TextBox = new System.Windows.Forms.TextBox();
            this.SupplyAirFlow_TextBox = new System.Windows.Forms.TextBox();
            this.Pressure_Label = new System.Windows.Forms.Label();
            this.DataGridView_MollierProcesses = new System.Windows.Forms.DataGridView();
            this.Column_MollierProcess_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_Label = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_DryBulbTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcessHumidityRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcessRelativeHumidity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_WetBulbTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_DewPointTemperature = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_SpecificVolume = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierPoint_Enthalpy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_Density = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_MassFlow = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_TotalLoad = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_SensibleLoad = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_LatentLoad = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column_MollierProcess_Edit = new System.Windows.Forms.DataGridViewButtonColumn();
            this.Column_MollierProcess_Remove = new System.Windows.Forms.DataGridViewButtonColumn();
            this.ContextMenuStrip_Main = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStripMenuItem_Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_Remove = new System.Windows.Forms.ToolStripMenuItem();
            this.customizeMollierObjectsTabControl.SuspendLayout();
            this.tabCustomizePointPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_MollierPoints)).BeginInit();
            this.tabCustomizeProcessesPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_MollierProcesses)).BeginInit();
            this.ContextMenuStrip_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // customizeMollierObjectsTabControl
            // 
            this.customizeMollierObjectsTabControl.Controls.Add(this.tabCustomizePointPage);
            this.customizeMollierObjectsTabControl.Controls.Add(this.tabCustomizeProcessesPage);
            this.customizeMollierObjectsTabControl.Location = new System.Drawing.Point(0, 0);
            this.customizeMollierObjectsTabControl.Name = "customizeMollierObjectsTabControl";
            this.customizeMollierObjectsTabControl.SelectedIndex = 0;
            this.customizeMollierObjectsTabControl.Size = new System.Drawing.Size(1521, 546);
            this.customizeMollierObjectsTabControl.TabIndex = 0;
            // 
            // tabCustomizePointPage
            // 
            this.tabCustomizePointPage.Controls.Add(this.DataGridView_MollierPoints);
            this.tabCustomizePointPage.Location = new System.Drawing.Point(4, 25);
            this.tabCustomizePointPage.Name = "tabCustomizePointPage";
            this.tabCustomizePointPage.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomizePointPage.Size = new System.Drawing.Size(1513, 517);
            this.tabCustomizePointPage.TabIndex = 0;
            this.tabCustomizePointPage.Text = "Points";
            this.tabCustomizePointPage.UseVisualStyleBackColor = true;
            // 
            // DataGridView_MollierPoints
            // 
            this.DataGridView_MollierPoints.AllowUserToAddRows = false;
            this.DataGridView_MollierPoints.AllowUserToDeleteRows = false;
            this.DataGridView_MollierPoints.AllowUserToResizeRows = false;
            this.DataGridView_MollierPoints.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataGridView_MollierPoints.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.DataGridView_MollierPoints.ColumnHeadersHeight = 66;
            this.DataGridView_MollierPoints.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column_Label,
            this.Column_DryBulbTemperature,
            this.Column_HumidityRatio,
            this.Column_RelativeHumidity,
            this.Column_WetBulbTemperature,
            this.Column_DewPointTemperature,
            this.Column_SpecificVolume,
            this.Column_Enthalpy,
            this.Column_Density,
            this.Column_Edit,
            this.Column_Remove});
            this.DataGridView_MollierPoints.ContextMenuStrip = this.ContextMenuStrip_Main;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DataGridView_MollierPoints.DefaultCellStyle = dataGridViewCellStyle2;
            this.DataGridView_MollierPoints.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGridView_MollierPoints.Location = new System.Drawing.Point(3, 3);
            this.DataGridView_MollierPoints.Name = "DataGridView_MollierPoints";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataGridView_MollierPoints.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.DataGridView_MollierPoints.RowHeadersVisible = false;
            this.DataGridView_MollierPoints.RowHeadersWidth = 62;
            this.DataGridView_MollierPoints.RowTemplate.Height = 28;
            this.DataGridView_MollierPoints.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataGridView_MollierPoints.Size = new System.Drawing.Size(1507, 511);
            this.DataGridView_MollierPoints.TabIndex = 0;
            this.DataGridView_MollierPoints.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView_CellContentClick);
            // 
            // Column_Label
            // 
            this.Column_Label.DataPropertyName = "Label";
            this.Column_Label.HeaderText = "Label";
            this.Column_Label.MinimumWidth = 8;
            this.Column_Label.Name = "Column_Label";
            this.Column_Label.ReadOnly = true;
            this.Column_Label.Width = 72;
            // 
            // Column_DryBulbTemperature
            // 
            this.Column_DryBulbTemperature.DataPropertyName = "DryBulbTemperature";
            this.Column_DryBulbTemperature.HeaderText = "Dry bulb temperature t [°C]";
            this.Column_DryBulbTemperature.MinimumWidth = 8;
            this.Column_DryBulbTemperature.Name = "Column_DryBulbTemperature";
            this.Column_DryBulbTemperature.ReadOnly = true;
            this.Column_DryBulbTemperature.Width = 116;
            // 
            // Column_HumidityRatio
            // 
            this.Column_HumidityRatio.DataPropertyName = "HumidityRatio";
            this.Column_HumidityRatio.HeaderText = "Humidity ratio \nx [g/kg]";
            this.Column_HumidityRatio.MinimumWidth = 8;
            this.Column_HumidityRatio.Name = "Column_HumidityRatio";
            this.Column_HumidityRatio.ReadOnly = true;
            this.Column_HumidityRatio.Width = 117;
            // 
            // Column_RelativeHumidity
            // 
            this.Column_RelativeHumidity.DataPropertyName = "RelativeHumidity";
            this.Column_RelativeHumidity.HeaderText = "Relative humidity \n φ [%]";
            this.Column_RelativeHumidity.MinimumWidth = 8;
            this.Column_RelativeHumidity.Name = "Column_RelativeHumidity";
            this.Column_RelativeHumidity.ReadOnly = true;
            this.Column_RelativeHumidity.Width = 136;
            // 
            // Column_WetBulbTemperature
            // 
            this.Column_WetBulbTemperature.DataPropertyName = "WetBulbTemperature";
            this.Column_WetBulbTemperature.HeaderText = "Wet bulb temperature\n t_wb [°C]";
            this.Column_WetBulbTemperature.MinimumWidth = 8;
            this.Column_WetBulbTemperature.Name = "Column_WetBulbTemperature";
            this.Column_WetBulbTemperature.ReadOnly = true;
            this.Column_WetBulbTemperature.Width = 159;
            // 
            // Column_DewPointTemperature
            // 
            this.Column_DewPointTemperature.DataPropertyName = "DewPointTemperature";
            this.Column_DewPointTemperature.HeaderText = "Dew point temperature t_tao [°C]";
            this.Column_DewPointTemperature.MinimumWidth = 8;
            this.Column_DewPointTemperature.Name = "Column_DewPointTemperature";
            this.Column_DewPointTemperature.ReadOnly = true;
            this.Column_DewPointTemperature.Width = 141;
            // 
            // Column_SpecificVolume
            // 
            this.Column_SpecificVolume.DataPropertyName = "SpecificVolume";
            this.Column_SpecificVolume.HeaderText = "Specific volume \nv [m3/kg]";
            this.Column_SpecificVolume.MinimumWidth = 8;
            this.Column_SpecificVolume.Name = "Column_SpecificVolume";
            this.Column_SpecificVolume.ReadOnly = true;
            this.Column_SpecificVolume.Width = 128;
            // 
            // Column_Enthalpy
            // 
            this.Column_Enthalpy.DataPropertyName = "Enthalpy";
            this.Column_Enthalpy.HeaderText = "Enthalpy h [kJ/kg*K]";
            this.Column_Enthalpy.MinimumWidth = 8;
            this.Column_Enthalpy.Name = "Column_Enthalpy";
            this.Column_Enthalpy.ReadOnly = true;
            // 
            // Column_Density
            // 
            this.Column_Density.DataPropertyName = "Density";
            this.Column_Density.HeaderText = "Density\n p [kg/m3]";
            this.Column_Density.MinimumWidth = 8;
            this.Column_Density.Name = "Column_Density";
            this.Column_Density.ReadOnly = true;
            this.Column_Density.Width = 92;
            // 
            // Column_Edit
            // 
            this.Column_Edit.HeaderText = "Edit";
            this.Column_Edit.MinimumWidth = 8;
            this.Column_Edit.Name = "Column_Edit";
            this.Column_Edit.ReadOnly = true;
            this.Column_Edit.Width = 38;
            // 
            // Column_Remove
            // 
            this.Column_Remove.HeaderText = "Remove";
            this.Column_Remove.MinimumWidth = 8;
            this.Column_Remove.Name = "Column_Remove";
            this.Column_Remove.ReadOnly = true;
            this.Column_Remove.Width = 66;
            // 
            // tabCustomizeProcessesPage
            // 
            this.tabCustomizeProcessesPage.Controls.Add(this.ExhaustAirflow_CheckBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.SupplyAirFlow_CheckBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.SupplyAirflow_ComboBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.ExhaustAirflow_Combobox);
            this.tabCustomizeProcessesPage.Controls.Add(this.ExhaustAirFlow_TextBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.Pressure_TextBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.SupplyAirFlow_TextBox);
            this.tabCustomizeProcessesPage.Controls.Add(this.Pressure_Label);
            this.tabCustomizeProcessesPage.Controls.Add(this.DataGridView_MollierProcesses);
            this.tabCustomizeProcessesPage.Location = new System.Drawing.Point(4, 25);
            this.tabCustomizeProcessesPage.Name = "tabCustomizeProcessesPage";
            this.tabCustomizeProcessesPage.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomizeProcessesPage.Size = new System.Drawing.Size(1513, 517);
            this.tabCustomizeProcessesPage.TabIndex = 1;
            this.tabCustomizeProcessesPage.Text = "Processes";
            this.tabCustomizeProcessesPage.UseVisualStyleBackColor = true;
            // 
            // ExhaustAirflow_CheckBox
            // 
            this.ExhaustAirflow_CheckBox.AutoSize = true;
            this.ExhaustAirflow_CheckBox.Location = new System.Drawing.Point(604, 19);
            this.ExhaustAirflow_CheckBox.Name = "ExhaustAirflow_CheckBox";
            this.ExhaustAirflow_CheckBox.Size = new System.Drawing.Size(117, 20);
            this.ExhaustAirflow_CheckBox.TabIndex = 10;
            this.ExhaustAirflow_CheckBox.Text = "Exhaust airflow";
            this.ExhaustAirflow_CheckBox.UseVisualStyleBackColor = true;
            this.ExhaustAirflow_CheckBox.Visible = false;
            // 
            // SupplyAirFlow_CheckBox
            // 
            this.SupplyAirFlow_CheckBox.AutoSize = true;
            this.SupplyAirFlow_CheckBox.Checked = true;
            this.SupplyAirFlow_CheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SupplyAirFlow_CheckBox.Location = new System.Drawing.Point(277, 19);
            this.SupplyAirFlow_CheckBox.Name = "SupplyAirFlow_CheckBox";
            this.SupplyAirFlow_CheckBox.Size = new System.Drawing.Size(112, 20);
            this.SupplyAirFlow_CheckBox.TabIndex = 9;
            this.SupplyAirFlow_CheckBox.Text = "Supply airflow";
            this.SupplyAirFlow_CheckBox.UseVisualStyleBackColor = true;
            this.SupplyAirFlow_CheckBox.CheckedChanged += new System.EventHandler(this.SupplyAirFlow_CheckBox_CheckedChanged);
            // 
            // SupplyAirflow_ComboBox
            // 
            this.SupplyAirflow_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SupplyAirflow_ComboBox.FormattingEnabled = true;
            this.SupplyAirflow_ComboBox.Items.AddRange(new object[] {
            "m3/s",
            "m3/h"});
            this.SupplyAirflow_ComboBox.Location = new System.Drawing.Point(469, 15);
            this.SupplyAirflow_ComboBox.Name = "SupplyAirflow_ComboBox";
            this.SupplyAirflow_ComboBox.Size = new System.Drawing.Size(61, 24);
            this.SupplyAirflow_ComboBox.TabIndex = 8;
            this.SupplyAirflow_ComboBox.SelectedIndexChanged += new System.EventHandler(this.SupplyAirflow_ComboBox_SelectedIndexChanged);
            // 
            // ExhaustAirflow_Combobox
            // 
            this.ExhaustAirflow_Combobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ExhaustAirflow_Combobox.FormattingEnabled = true;
            this.ExhaustAirflow_Combobox.Items.AddRange(new object[] {
            "m3/s",
            "m3/h"});
            this.ExhaustAirflow_Combobox.Location = new System.Drawing.Point(801, 15);
            this.ExhaustAirflow_Combobox.Name = "ExhaustAirflow_Combobox";
            this.ExhaustAirflow_Combobox.Size = new System.Drawing.Size(62, 24);
            this.ExhaustAirflow_Combobox.TabIndex = 7;
            this.ExhaustAirflow_Combobox.Visible = false;
            // 
            // ExhaustAirFlow_TextBox
            // 
            this.ExhaustAirFlow_TextBox.Location = new System.Drawing.Point(727, 17);
            this.ExhaustAirFlow_TextBox.Name = "ExhaustAirFlow_TextBox";
            this.ExhaustAirFlow_TextBox.Size = new System.Drawing.Size(68, 22);
            this.ExhaustAirFlow_TextBox.TabIndex = 6;
            this.ExhaustAirFlow_TextBox.Text = "0";
            this.ExhaustAirFlow_TextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.ExhaustAirFlow_TextBox.Visible = false;
            // 
            // Pressure_TextBox
            // 
            this.Pressure_TextBox.Location = new System.Drawing.Point(137, 17);
            this.Pressure_TextBox.Name = "Pressure_TextBox";
            this.Pressure_TextBox.ReadOnly = true;
            this.Pressure_TextBox.Size = new System.Drawing.Size(68, 22);
            this.Pressure_TextBox.TabIndex = 4;
            this.Pressure_TextBox.Text = "0";
            this.Pressure_TextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SupplyAirFlow_TextBox
            // 
            this.SupplyAirFlow_TextBox.Location = new System.Drawing.Point(395, 17);
            this.SupplyAirFlow_TextBox.Name = "SupplyAirFlow_TextBox";
            this.SupplyAirFlow_TextBox.Size = new System.Drawing.Size(68, 22);
            this.SupplyAirFlow_TextBox.TabIndex = 3;
            this.SupplyAirFlow_TextBox.Text = "0";
            this.SupplyAirFlow_TextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.SupplyAirFlow_TextBox.TextChanged += new System.EventHandler(this.SupplyAirFlow_TextBox_TextChanged);
            // 
            // Pressure_Label
            // 
            this.Pressure_Label.AutoSize = true;
            this.Pressure_Label.Location = new System.Drawing.Point(70, 20);
            this.Pressure_Label.Name = "Pressure_Label";
            this.Pressure_Label.Size = new System.Drawing.Size(61, 16);
            this.Pressure_Label.TabIndex = 1;
            this.Pressure_Label.Text = "Pressure";
            // 
            // DataGridView_MollierProcesses
            // 
            this.DataGridView_MollierProcesses.AllowUserToAddRows = false;
            this.DataGridView_MollierProcesses.AllowUserToDeleteRows = false;
            this.DataGridView_MollierProcesses.AllowUserToOrderColumns = true;
            this.DataGridView_MollierProcesses.AllowUserToResizeColumns = false;
            this.DataGridView_MollierProcesses.AllowUserToResizeRows = false;
            this.DataGridView_MollierProcesses.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataGridView_MollierProcesses.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.DataGridView_MollierProcesses.ColumnHeadersHeight = 66;
            this.DataGridView_MollierProcesses.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column_MollierProcess_Name,
            this.Column_MollierProcess_Label,
            this.Column_MollierProcess_DryBulbTemperature,
            this.Column_MollierProcessHumidityRatio,
            this.Column_MollierProcessRelativeHumidity,
            this.Column_MollierProcess_WetBulbTemperature,
            this.Column_MollierProcess_DewPointTemperature,
            this.Column_MollierProcess_SpecificVolume,
            this.Column_MollierPoint_Enthalpy,
            this.Column_MollierProcess_Density,
            this.Column_MollierProcess_MassFlow,
            this.Column_MollierProcess_TotalLoad,
            this.Column_MollierProcess_SensibleLoad,
            this.Column_MollierProcess_LatentLoad,
            this.Column_MollierProcess_Edit,
            this.Column_MollierProcess_Remove});
            this.DataGridView_MollierProcesses.ContextMenuStrip = this.ContextMenuStrip_Main;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DataGridView_MollierProcesses.DefaultCellStyle = dataGridViewCellStyle5;
            this.DataGridView_MollierProcesses.Location = new System.Drawing.Point(6, 45);
            this.DataGridView_MollierProcesses.Name = "DataGridView_MollierProcesses";
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataGridView_MollierProcesses.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.DataGridView_MollierProcesses.RowHeadersVisible = false;
            this.DataGridView_MollierProcesses.RowHeadersWidth = 62;
            this.DataGridView_MollierProcesses.RowTemplate.Height = 28;
            this.DataGridView_MollierProcesses.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.DataGridView_MollierProcesses.Size = new System.Drawing.Size(1507, 462);
            this.DataGridView_MollierProcesses.TabIndex = 0;
            this.DataGridView_MollierProcesses.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView_CellContentClick);
            this.DataGridView_MollierProcesses.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.DataGridView_MollierProcesses_CellPainting);
            // 
            // Column_MollierProcess_Name
            // 
            this.Column_MollierProcess_Name.DataPropertyName = "Name";
            this.Column_MollierProcess_Name.HeaderText = "Name";
            this.Column_MollierProcess_Name.MinimumWidth = 8;
            this.Column_MollierProcess_Name.Name = "Column_MollierProcess_Name";
            this.Column_MollierProcess_Name.ReadOnly = true;
            this.Column_MollierProcess_Name.Width = 74;
            // 
            // Column_MollierProcess_Label
            // 
            this.Column_MollierProcess_Label.DataPropertyName = "Label";
            this.Column_MollierProcess_Label.HeaderText = "Label";
            this.Column_MollierProcess_Label.MinimumWidth = 8;
            this.Column_MollierProcess_Label.Name = "Column_MollierProcess_Label";
            this.Column_MollierProcess_Label.ReadOnly = true;
            this.Column_MollierProcess_Label.Width = 72;
            // 
            // Column_MollierProcess_DryBulbTemperature
            // 
            this.Column_MollierProcess_DryBulbTemperature.DataPropertyName = "DryBulbTemperature";
            this.Column_MollierProcess_DryBulbTemperature.HeaderText = "Dry bulb temperature t [°C]";
            this.Column_MollierProcess_DryBulbTemperature.MinimumWidth = 8;
            this.Column_MollierProcess_DryBulbTemperature.Name = "Column_MollierProcess_DryBulbTemperature";
            this.Column_MollierProcess_DryBulbTemperature.ReadOnly = true;
            this.Column_MollierProcess_DryBulbTemperature.Width = 116;
            // 
            // Column_MollierProcessHumidityRatio
            // 
            this.Column_MollierProcessHumidityRatio.DataPropertyName = "HumidityRatio";
            this.Column_MollierProcessHumidityRatio.HeaderText = "Humidity ratio \nx [g/kg]";
            this.Column_MollierProcessHumidityRatio.MinimumWidth = 8;
            this.Column_MollierProcessHumidityRatio.Name = "Column_MollierProcessHumidityRatio";
            this.Column_MollierProcessHumidityRatio.ReadOnly = true;
            this.Column_MollierProcessHumidityRatio.Width = 117;
            // 
            // Column_MollierProcessRelativeHumidity
            // 
            this.Column_MollierProcessRelativeHumidity.DataPropertyName = "RelativeHumidity";
            this.Column_MollierProcessRelativeHumidity.HeaderText = "Relative humidity \n φ [%]";
            this.Column_MollierProcessRelativeHumidity.MinimumWidth = 8;
            this.Column_MollierProcessRelativeHumidity.Name = "Column_MollierProcessRelativeHumidity";
            this.Column_MollierProcessRelativeHumidity.ReadOnly = true;
            this.Column_MollierProcessRelativeHumidity.Width = 136;
            // 
            // Column_MollierProcess_WetBulbTemperature
            // 
            this.Column_MollierProcess_WetBulbTemperature.DataPropertyName = "WetBulbTemperature";
            this.Column_MollierProcess_WetBulbTemperature.HeaderText = "Wet bulb temperature\n t_wb [°C]";
            this.Column_MollierProcess_WetBulbTemperature.MinimumWidth = 8;
            this.Column_MollierProcess_WetBulbTemperature.Name = "Column_MollierProcess_WetBulbTemperature";
            this.Column_MollierProcess_WetBulbTemperature.ReadOnly = true;
            this.Column_MollierProcess_WetBulbTemperature.Width = 159;
            // 
            // Column_MollierProcess_DewPointTemperature
            // 
            this.Column_MollierProcess_DewPointTemperature.DataPropertyName = "DewPointTemperature";
            this.Column_MollierProcess_DewPointTemperature.HeaderText = "Dew point temperature t_tao [°C]";
            this.Column_MollierProcess_DewPointTemperature.MinimumWidth = 8;
            this.Column_MollierProcess_DewPointTemperature.Name = "Column_MollierProcess_DewPointTemperature";
            this.Column_MollierProcess_DewPointTemperature.ReadOnly = true;
            this.Column_MollierProcess_DewPointTemperature.Width = 141;
            // 
            // Column_MollierProcess_SpecificVolume
            // 
            this.Column_MollierProcess_SpecificVolume.DataPropertyName = "SpecificVolume";
            this.Column_MollierProcess_SpecificVolume.HeaderText = "Specific volume \nv [m3/kg]";
            this.Column_MollierProcess_SpecificVolume.MinimumWidth = 8;
            this.Column_MollierProcess_SpecificVolume.Name = "Column_MollierProcess_SpecificVolume";
            this.Column_MollierProcess_SpecificVolume.ReadOnly = true;
            this.Column_MollierProcess_SpecificVolume.Width = 128;
            // 
            // Column_MollierPoint_Enthalpy
            // 
            this.Column_MollierPoint_Enthalpy.DataPropertyName = "Enthalpy";
            this.Column_MollierPoint_Enthalpy.HeaderText = "Enthalpy h [kJ/kg*K]";
            this.Column_MollierPoint_Enthalpy.MinimumWidth = 8;
            this.Column_MollierPoint_Enthalpy.Name = "Column_MollierPoint_Enthalpy";
            this.Column_MollierPoint_Enthalpy.ReadOnly = true;
            // 
            // Column_MollierProcess_Density
            // 
            this.Column_MollierProcess_Density.DataPropertyName = "Density";
            this.Column_MollierProcess_Density.HeaderText = "Density\n p [kg/m3]";
            this.Column_MollierProcess_Density.MinimumWidth = 8;
            this.Column_MollierProcess_Density.Name = "Column_MollierProcess_Density";
            this.Column_MollierProcess_Density.ReadOnly = true;
            this.Column_MollierProcess_Density.Width = 92;
            // 
            // Column_MollierProcess_MassFlow
            // 
            this.Column_MollierProcess_MassFlow.DataPropertyName = "MassFlow";
            this.Column_MollierProcess_MassFlow.HeaderText = "Mass flow\n m [kg/s]";
            this.Column_MollierProcess_MassFlow.MinimumWidth = 8;
            this.Column_MollierProcess_MassFlow.Name = "Column_MollierProcess_MassFlow";
            this.Column_MollierProcess_MassFlow.ReadOnly = true;
            this.Column_MollierProcess_MassFlow.Width = 91;
            // 
            // Column_MollierProcess_TotalLoad
            // 
            this.Column_MollierProcess_TotalLoad.DataPropertyName = "TotalLoad";
            this.Column_MollierProcess_TotalLoad.HeaderText = "Total load\n Qtot [kW]";
            this.Column_MollierProcess_TotalLoad.MinimumWidth = 8;
            this.Column_MollierProcess_TotalLoad.Name = "Column_MollierProcess_TotalLoad";
            this.Column_MollierProcess_TotalLoad.ReadOnly = true;
            // 
            // Column_MollierProcess_SensibleLoad
            // 
            this.Column_MollierProcess_SensibleLoad.DataPropertyName = "SensibleLoad";
            this.Column_MollierProcess_SensibleLoad.HeaderText = "Sensible load\n Qsens [kW]";
            this.Column_MollierProcess_SensibleLoad.MinimumWidth = 8;
            this.Column_MollierProcess_SensibleLoad.Name = "Column_MollierProcess_SensibleLoad";
            this.Column_MollierProcess_SensibleLoad.ReadOnly = true;
            this.Column_MollierProcess_SensibleLoad.Width = 122;
            // 
            // Column_MollierProcess_LatentLoad
            // 
            this.Column_MollierProcess_LatentLoad.DataPropertyName = "LatentLoad";
            this.Column_MollierProcess_LatentLoad.HeaderText = "LatentLoad\n [?]";
            this.Column_MollierProcess_LatentLoad.MinimumWidth = 8;
            this.Column_MollierProcess_LatentLoad.Name = "Column_MollierProcess_LatentLoad";
            this.Column_MollierProcess_LatentLoad.ReadOnly = true;
            this.Column_MollierProcess_LatentLoad.Width = 109;
            // 
            // Column_MollierProcess_Edit
            // 
            this.Column_MollierProcess_Edit.HeaderText = "Edit";
            this.Column_MollierProcess_Edit.MinimumWidth = 8;
            this.Column_MollierProcess_Edit.Name = "Column_MollierProcess_Edit";
            this.Column_MollierProcess_Edit.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column_MollierProcess_Edit.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Column_MollierProcess_Edit.Width = 61;
            // 
            // Column_MollierProcess_Remove
            // 
            this.Column_MollierProcess_Remove.HeaderText = "Remove";
            this.Column_MollierProcess_Remove.MinimumWidth = 8;
            this.Column_MollierProcess_Remove.Name = "Column_MollierProcess_Remove";
            this.Column_MollierProcess_Remove.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column_MollierProcess_Remove.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Column_MollierProcess_Remove.Width = 89;
            // 
            // ContextMenuStrip_Main
            // 
            this.ContextMenuStrip_Main.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ContextMenuStrip_Main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_Edit,
            this.ToolStripMenuItem_Remove});
            this.ContextMenuStrip_Main.Name = "ContextMenuStrip_Main";
            this.ContextMenuStrip_Main.Size = new System.Drawing.Size(133, 52);
            // 
            // ToolStripMenuItem_Edit
            // 
            this.ToolStripMenuItem_Edit.Name = "ToolStripMenuItem_Edit";
            this.ToolStripMenuItem_Edit.Size = new System.Drawing.Size(132, 24);
            this.ToolStripMenuItem_Edit.Text = "Edit";
            this.ToolStripMenuItem_Edit.Click += new System.EventHandler(this.ToolStripMenuItem_Edit_Click);
            // 
            // ToolStripMenuItem_Remove
            // 
            this.ToolStripMenuItem_Remove.Name = "ToolStripMenuItem_Remove";
            this.ToolStripMenuItem_Remove.Size = new System.Drawing.Size(132, 24);
            this.ToolStripMenuItem_Remove.Text = "Remove";
            this.ToolStripMenuItem_Remove.Click += new System.EventHandler(this.ToolStripMenuItem_Remove_Click);
            // 
            // ManageMollierObjectsForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1523, 552);
            this.Controls.Add(this.customizeMollierObjectsTabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManageMollierObjectsForm";
            this.ShowIcon = false;
            this.Text = "Edit / Remove";
            this.Load += new System.EventHandler(this.ManageMollierObjectsForm_Load);
            this.customizeMollierObjectsTabControl.ResumeLayout(false);
            this.tabCustomizePointPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_MollierPoints)).EndInit();
            this.tabCustomizeProcessesPage.ResumeLayout(false);
            this.tabCustomizeProcessesPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_MollierProcesses)).EndInit();
            this.ContextMenuStrip_Main.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl customizeMollierObjectsTabControl;
        private System.Windows.Forms.TabPage tabCustomizePointPage;
        private System.Windows.Forms.TabPage tabCustomizeProcessesPage;
        private System.Windows.Forms.DataGridView DataGridView_MollierPoints;
        private System.Windows.Forms.DataGridView DataGridView_MollierProcesses;
        private System.Windows.Forms.TextBox SupplyAirFlow_TextBox;
        private System.Windows.Forms.Label Pressure_Label;
        private System.Windows.Forms.TextBox Pressure_TextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_Label;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_DryBulbTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcessHumidityRatio;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcessRelativeHumidity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_WetBulbTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_DewPointTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_SpecificVolume;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierPoint_Enthalpy;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_Density;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_MassFlow;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_TotalLoad;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_SensibleLoad;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_MollierProcess_LatentLoad;
        private System.Windows.Forms.DataGridViewButtonColumn Column_MollierProcess_Edit;
        private System.Windows.Forms.DataGridViewButtonColumn Column_MollierProcess_Remove;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_Label;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_DryBulbTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_HumidityRatio;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_RelativeHumidity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_WetBulbTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_DewPointTemperature;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_SpecificVolume;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_Enthalpy;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column_Density;
        private System.Windows.Forms.DataGridViewButtonColumn Column_Edit;
        private System.Windows.Forms.DataGridViewButtonColumn Column_Remove;
        private System.Windows.Forms.TextBox ExhaustAirFlow_TextBox;
        private System.Windows.Forms.ComboBox SupplyAirflow_ComboBox;
        private System.Windows.Forms.ComboBox ExhaustAirflow_Combobox;
        private System.Windows.Forms.CheckBox ExhaustAirflow_CheckBox;
        private System.Windows.Forms.CheckBox SupplyAirFlow_CheckBox;
        private System.Windows.Forms.ContextMenuStrip ContextMenuStrip_Main;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Edit;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_Remove;
    }
}