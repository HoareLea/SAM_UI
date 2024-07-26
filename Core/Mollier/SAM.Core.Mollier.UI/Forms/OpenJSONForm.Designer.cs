namespace SAM.Core.Mollier.UI.Forms
{
    partial class OpenJSONForm
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
            this.MergeButton = new System.Windows.Forms.Button();
            this.ReplaceButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // MergeButton
            // 
            this.MergeButton.Font = new System.Drawing.Font("Leelawadee UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MergeButton.Location = new System.Drawing.Point(12, 12);
            this.MergeButton.Name = "MergeButton";
            this.MergeButton.Size = new System.Drawing.Size(169, 41);
            this.MergeButton.TabIndex = 0;
            this.MergeButton.Text = "MERGE";
            this.MergeButton.UseVisualStyleBackColor = true;
            this.MergeButton.Click += new System.EventHandler(this.MergeButton_Click);
            // 
            // ReplaceButton
            // 
            this.ReplaceButton.Font = new System.Drawing.Font("Leelawadee UI", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReplaceButton.Location = new System.Drawing.Point(12, 67);
            this.ReplaceButton.Name = "ReplaceButton";
            this.ReplaceButton.Size = new System.Drawing.Size(169, 41);
            this.ReplaceButton.TabIndex = 1;
            this.ReplaceButton.Text = "REPLACE";
            this.ReplaceButton.UseVisualStyleBackColor = true;
            this.ReplaceButton.Click += new System.EventHandler(this.ReplaceButton_Click);
            // 
            // OpenJSONForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(193, 125);
            this.Controls.Add(this.ReplaceButton);
            this.Controls.Add(this.MergeButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpenJSONForm";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.OpenJSONForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button MergeButton;
        private System.Windows.Forms.Button ReplaceButton;
    }
}