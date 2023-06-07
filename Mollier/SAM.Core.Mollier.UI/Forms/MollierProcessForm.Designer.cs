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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.processTypeLabel = new System.Windows.Forms.Label();
            this.MollierProcessType_ComboBox = new System.Windows.Forms.ComboBox();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.OK_Button = new System.Windows.Forms.Button();
            this.Customize_Button = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.processTypeLabel);
            this.splitContainer1.Panel1.Controls.Add(this.MollierProcessType_ComboBox);
            this.splitContainer1.Size = new System.Drawing.Size(616, 371);
            this.splitContainer1.SplitterDistance = 73;
            this.splitContainer1.TabIndex = 0;
            // 
            // processTypeLabel
            // 
            this.processTypeLabel.AutoSize = true;
            this.processTypeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.processTypeLabel.Location = new System.Drawing.Point(255, 11);
            this.processTypeLabel.Name = "processTypeLabel";
            this.processTypeLabel.Size = new System.Drawing.Size(112, 20);
            this.processTypeLabel.TabIndex = 4;
            this.processTypeLabel.Text = "Process Type";
            // 
            // MollierProcessType_ComboBox
            // 
            this.MollierProcessType_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MollierProcessType_ComboBox.FormattingEnabled = true;
            this.MollierProcessType_ComboBox.Items.AddRange(new object[] {
            "Heating",
            "Cooling",
            "Heat Recovery",
            "Mixing",
            "Adiabatic Humidification (by water spray)",
            "Isotermic Humidification (by steam)",
            "Room Process"});
            this.MollierProcessType_ComboBox.Location = new System.Drawing.Point(195, 34);
            this.MollierProcessType_ComboBox.Name = "MollierProcessType_ComboBox";
            this.MollierProcessType_ComboBox.Size = new System.Drawing.Size(234, 24);
            this.MollierProcessType_ComboBox.TabIndex = 0;
            this.MollierProcessType_ComboBox.SelectedIndexChanged += new System.EventHandler(this.MollierProcessType_ComboBox_SelectedIndexChanged);
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel_Button.Location = new System.Drawing.Point(496, 377);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(108, 30);
            this.Cancel_Button.TabIndex = 2;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // OK_Button
            // 
            this.OK_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK_Button.Location = new System.Drawing.Point(382, 377);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(108, 30);
            this.OK_Button.TabIndex = 1;
            this.OK_Button.Text = "OK";
            this.OK_Button.UseVisualStyleBackColor = true;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // Customize_Button
            // 
            this.Customize_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Customize_Button.Location = new System.Drawing.Point(268, 377);
            this.Customize_Button.Name = "Customize_Button";
            this.Customize_Button.Size = new System.Drawing.Size(108, 30);
            this.Customize_Button.TabIndex = 3;
            this.Customize_Button.Text = "Customize";
            this.Customize_Button.UseVisualStyleBackColor = true;
            this.Customize_Button.Click += new System.EventHandler(this.Customize_Button_Click);
            // 
            // MollierProcessForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(616, 419);
            this.Controls.Add(this.Customize_Button);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierProcessForm";
            this.ShowIcon = false;
            this.Text = "Mollier Process";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button Cancel_Button;
        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.ComboBox MollierProcessType_ComboBox;
        private System.Windows.Forms.Label processTypeLabel;
        private System.Windows.Forms.Button Customize_Button;
    }
}