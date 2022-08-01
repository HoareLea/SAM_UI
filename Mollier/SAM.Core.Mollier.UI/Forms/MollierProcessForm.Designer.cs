namespace SAM.Core.Mollier.UI.Forms
{
    partial class MollierProcessForm
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
            this.Button_OK = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.TextBox_HumidityRatio = new System.Windows.Forms.TextBox();
            this.TextBox_Temperature = new System.Windows.Forms.TextBox();
            this.BottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.TopToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.RightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.LeftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
            this.ContentPanel = new System.Windows.Forms.ToolStripContentPanel();
            this.ComboBox_ChooseProcess = new System.Windows.Forms.ComboBox();
            this.Label_DryBulbTemperature = new System.Windows.Forms.Label();
            this.Label_HumidityRatio = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(242, 202);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 23);
            this.Button_OK.TabIndex = 5;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button_Cancel.Location = new System.Drawing.Point(323, 202);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 23);
            this.Button_Cancel.TabIndex = 6;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            // 
            // TextBox_HumidityRatio
            // 
            this.TextBox_HumidityRatio.Location = new System.Drawing.Point(196, 64);
            this.TextBox_HumidityRatio.Name = "TextBox_HumidityRatio";
            this.TextBox_HumidityRatio.Size = new System.Drawing.Size(137, 22);
            this.TextBox_HumidityRatio.TabIndex = 7;
            // 
            // TextBox_Temperature
            // 
            this.TextBox_Temperature.Location = new System.Drawing.Point(196, 32);
            this.TextBox_Temperature.Name = "TextBox_Temperature";
            this.TextBox_Temperature.Size = new System.Drawing.Size(137, 22);
            this.TextBox_Temperature.TabIndex = 8;
            // 
            // BottomToolStripPanel
            // 
            this.BottomToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.BottomToolStripPanel.Name = "BottomToolStripPanel";
            this.BottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.BottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.BottomToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // TopToolStripPanel
            // 
            this.TopToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.TopToolStripPanel.Name = "TopToolStripPanel";
            this.TopToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.TopToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.TopToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // RightToolStripPanel
            // 
            this.RightToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.RightToolStripPanel.Name = "RightToolStripPanel";
            this.RightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.RightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.RightToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // LeftToolStripPanel
            // 
            this.LeftToolStripPanel.Location = new System.Drawing.Point(0, 0);
            this.LeftToolStripPanel.Name = "LeftToolStripPanel";
            this.LeftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.LeftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.LeftToolStripPanel.Size = new System.Drawing.Size(0, 0);
            // 
            // ContentPanel
            // 
            this.ContentPanel.Size = new System.Drawing.Size(150, 150);
            // 
            // ComboBox_ChooseProcess
            // 
            this.ComboBox_ChooseProcess.FormattingEnabled = true;
            this.ComboBox_ChooseProcess.Items.AddRange(new object[] {
            "Heat",
            "Cool",
            "Mix",
            "Heat recovery",
            "Humidify"});
            this.ComboBox_ChooseProcess.Location = new System.Drawing.Point(196, 104);
            this.ComboBox_ChooseProcess.Name = "ComboBox_ChooseProcess";
            this.ComboBox_ChooseProcess.Size = new System.Drawing.Size(137, 24);
            this.ComboBox_ChooseProcess.TabIndex = 10;
            this.ComboBox_ChooseProcess.Text = "Choose process";
            // 
            // Label_DryBulbTemperature
            // 
            this.Label_DryBulbTemperature.AutoSize = true;
            this.Label_DryBulbTemperature.Location = new System.Drawing.Point(12, 32);
            this.Label_DryBulbTemperature.Name = "Label_DryBulbTemperature";
            this.Label_DryBulbTemperature.Size = new System.Drawing.Size(163, 16);
            this.Label_DryBulbTemperature.TabIndex = 11;
            this.Label_DryBulbTemperature.Text = "Dry Bulb Temperature [°C]";
            // 
            // Label_HumidityRatio
            // 
            this.Label_HumidityRatio.AutoSize = true;
            this.Label_HumidityRatio.Location = new System.Drawing.Point(43, 64);
            this.Label_HumidityRatio.Name = "Label_HumidityRatio";
            this.Label_HumidityRatio.Size = new System.Drawing.Size(132, 16);
            this.Label_HumidityRatio.TabIndex = 12;
            this.Label_HumidityRatio.Text = "Humidity Ratio [g/kg]";
            // 
            // MollierProcessForm
            // 
            this.AcceptButton = this.Button_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Button_Cancel;
            this.ClientSize = new System.Drawing.Size(410, 237);
            this.Controls.Add(this.Label_HumidityRatio);
            this.Controls.Add(this.Label_DryBulbTemperature);
            this.Controls.Add(this.ComboBox_ChooseProcess);
            this.Controls.Add(this.TextBox_Temperature);
            this.Controls.Add(this.TextBox_HumidityRatio);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.Button_OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierProcessForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Process";
            this.Load += new System.EventHandler(this.MollierProcessForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Button_OK;
        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.TextBox TextBox_HumidityRatio;
        private System.Windows.Forms.TextBox TextBox_Temperature;
        private System.Windows.Forms.ToolStripPanel BottomToolStripPanel;
        private System.Windows.Forms.ToolStripPanel TopToolStripPanel;
        private System.Windows.Forms.ToolStripPanel RightToolStripPanel;
        private System.Windows.Forms.ToolStripPanel LeftToolStripPanel;
        private System.Windows.Forms.ToolStripContentPanel ContentPanel;
        private System.Windows.Forms.ComboBox ComboBox_ChooseProcess;
        private System.Windows.Forms.Label Label_DryBulbTemperature;
        private System.Windows.Forms.Label Label_HumidityRatio;
    }
}