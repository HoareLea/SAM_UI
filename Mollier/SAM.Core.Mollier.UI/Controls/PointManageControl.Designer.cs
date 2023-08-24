namespace SAM.Core.Mollier.UI.Controls
{
    partial class PointManageControl
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
            this.label_Name = new System.Windows.Forms.Label();
            this.label_Y = new System.Windows.Forms.Label();
            this.label_X = new System.Windows.Forms.Label();
            this.removeButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_Name
            // 
            this.label_Name.AutoSize = true;
            this.label_Name.Location = new System.Drawing.Point(60, 27);
            this.label_Name.Name = "label_Name";
            this.label_Name.Size = new System.Drawing.Size(14, 20);
            this.label_Name.TabIndex = 0;
            this.label_Name.Text = "-";
            // 
            // label_Y
            // 
            this.label_Y.AutoSize = true;
            this.label_Y.Location = new System.Drawing.Point(190, 27);
            this.label_Y.Name = "label_Y";
            this.label_Y.Size = new System.Drawing.Size(14, 20);
            this.label_Y.TabIndex = 1;
            this.label_Y.Text = "-";
            // 
            // label_X
            // 
            this.label_X.AutoSize = true;
            this.label_X.Location = new System.Drawing.Point(137, 27);
            this.label_X.Name = "label_X";
            this.label_X.Size = new System.Drawing.Size(14, 20);
            this.label_X.TabIndex = 3;
            this.label_X.Text = "-";
            // 
            // removeButton
            // 
            this.removeButton.BackColor = System.Drawing.Color.Firebrick;
            this.removeButton.Location = new System.Drawing.Point(379, 20);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(50, 34);
            this.removeButton.TabIndex = 8;
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // settingsButton
            // 
            this.settingsButton.Location = new System.Drawing.Point(296, 20);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(77, 34);
            this.settingsButton.TabIndex = 7;
            this.settingsButton.Text = "settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
            // PointManageControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.label_X);
            this.Controls.Add(this.label_Y);
            this.Controls.Add(this.label_Name);
            this.Name = "PointManageControl";
            this.Size = new System.Drawing.Size(561, 75);
            this.Load += new System.EventHandler(this.PointManageControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Name;
        private System.Windows.Forms.Label label_Y;
        private System.Windows.Forms.Label label_X;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button settingsButton;
    }
}
