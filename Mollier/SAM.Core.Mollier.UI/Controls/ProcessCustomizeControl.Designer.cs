namespace SAM.Core.Mollier.UI.Controls
{
    partial class ProcessCustomizeControl
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
            this.button1 = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
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
            this.start_X.Location = new System.Drawing.Point(326, 27);
            this.start_X.Name = "start_X";
            this.start_X.Size = new System.Drawing.Size(14, 20);
            this.start_X.TabIndex = 1;
            this.start_X.Text = "-";
            // 
            // start_Y
            // 
            this.start_Y.AutoSize = true;
            this.start_Y.Location = new System.Drawing.Point(383, 27);
            this.start_Y.Name = "start_Y";
            this.start_Y.Size = new System.Drawing.Size(14, 20);
            this.start_Y.TabIndex = 2;
            this.start_Y.Text = "-";
            // 
            // end_X
            // 
            this.end_X.AutoSize = true;
            this.end_X.Location = new System.Drawing.Point(488, 27);
            this.end_X.Name = "end_X";
            this.end_X.Size = new System.Drawing.Size(14, 20);
            this.end_X.TabIndex = 3;
            this.end_X.Text = "-";
            // 
            // end_Y
            // 
            this.end_Y.AutoSize = true;
            this.end_Y.Location = new System.Drawing.Point(545, 27);
            this.end_Y.Name = "end_Y";
            this.end_Y.Size = new System.Drawing.Size(14, 20);
            this.end_Y.TabIndex = 4;
            this.end_Y.Text = "-";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(657, 20);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(77, 34);
            this.button1.TabIndex = 5;
            this.button1.Text = "settings";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // removeButton
            // 
            this.removeButton.BackColor = System.Drawing.Color.Firebrick;
            this.removeButton.Location = new System.Drawing.Point(740, 20);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(50, 34);
            this.removeButton.TabIndex = 6;
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // ProcessCustomizeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.end_Y);
            this.Controls.Add(this.end_X);
            this.Controls.Add(this.start_Y);
            this.Controls.Add(this.start_X);
            this.Controls.Add(this.processNameLabel);
            this.Name = "ProcessCustomizeControl";
            this.Size = new System.Drawing.Size(836, 75);
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button removeButton;
    }
}
