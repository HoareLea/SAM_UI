namespace SAM.Core.Mollier.UI.Controls
{
    partial class ProcessManageControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.processNameLabel = new System.Windows.Forms.Label();
            this.start_X = new System.Windows.Forms.Label();
            this.start_Y = new System.Windows.Forms.Label();
            this.end_X = new System.Windows.Forms.Label();
            this.end_Y = new System.Windows.Forms.Label();
            this.settingsButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.start_Name = new System.Windows.Forms.Label();
            this.end_Name = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // processNameLabel
            // 
            this.processNameLabel.AutoSize = true;
            this.processNameLabel.Location = new System.Drawing.Point(13, 27);
            this.processNameLabel.Name = "processNameLabel";
            this.processNameLabel.Size = new System.Drawing.Size(108, 20);
            this.processNameLabel.TabIndex = 0;
            this.processNameLabel.Text = "ProcessName";
            // 
            // start_X
            // 
            this.start_X.AutoSize = true;
            this.start_X.Location = new System.Drawing.Point(357, 27);
            this.start_X.Name = "start_X";
            this.start_X.Size = new System.Drawing.Size(14, 20);
            this.start_X.TabIndex = 1;
            this.start_X.Text = "-";
            // 
            // start_Y
            // 
            this.start_Y.AutoSize = true;
            this.start_Y.Location = new System.Drawing.Point(419, 27);
            this.start_Y.Name = "start_Y";
            this.start_Y.Size = new System.Drawing.Size(14, 20);
            this.start_Y.TabIndex = 2;
            this.start_Y.Text = "-";
            // 
            // end_X
            // 
            this.end_X.AutoSize = true;
            this.end_X.Location = new System.Drawing.Point(654, 27);
            this.end_X.Name = "end_X";
            this.end_X.Size = new System.Drawing.Size(14, 20);
            this.end_X.TabIndex = 3;
            this.end_X.Text = "-";
            // 
            // end_Y
            // 
            this.end_Y.AutoSize = true;
            this.end_Y.Location = new System.Drawing.Point(717, 27);
            this.end_Y.Name = "end_Y";
            this.end_Y.Size = new System.Drawing.Size(14, 20);
            this.end_Y.TabIndex = 4;
            this.end_Y.Text = "-";
            // 
            // settingsButton
            // 
            this.settingsButton.Location = new System.Drawing.Point(845, 20);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(77, 34);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.BackColor = System.Drawing.Color.Firebrick;
            this.removeButton.Location = new System.Drawing.Point(928, 20);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(50, 34);
            this.removeButton.TabIndex = 6;
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // start_Name
            // 
            this.start_Name.AutoSize = true;
            this.start_Name.Location = new System.Drawing.Point(300, 27);
            this.start_Name.Name = "start_Name";
            this.start_Name.Size = new System.Drawing.Size(14, 20);
            this.start_Name.TabIndex = 7;
            this.start_Name.Text = "-";
            // 
            // end_Name
            // 
            this.end_Name.AutoSize = true;
            this.end_Name.Location = new System.Drawing.Point(594, 27);
            this.end_Name.Name = "end_Name";
            this.end_Name.Size = new System.Drawing.Size(14, 20);
            this.end_Name.TabIndex = 8;
            this.end_Name.Text = "-";
            // 
            // ProcessManageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.end_Name);
            this.Controls.Add(this.start_Name);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.end_Y);
            this.Controls.Add(this.end_X);
            this.Controls.Add(this.start_Y);
            this.Controls.Add(this.start_X);
            this.Controls.Add(this.processNameLabel);
            this.Name = "ProcessManageControl";
            this.Size = new System.Drawing.Size(990, 75);
            this.Load += new System.EventHandler(this.ProcessCustomizeControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label processNameLabel;
        private System.Windows.Forms.Label start_X;
        private System.Windows.Forms.Label start_Y;
        private System.Windows.Forms.Label end_X;
        private System.Windows.Forms.Label end_Y;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Label start_Name;
        private System.Windows.Forms.Label end_Name;
    }
}
