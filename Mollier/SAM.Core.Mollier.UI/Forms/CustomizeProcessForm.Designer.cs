namespace SAM.Core.Mollier.UI.Forms
{
    partial class CustomizeProcessForm
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
            this.customizeProcessControl = new SAM.Core.Mollier.UI.Controls.UIMollierProcessControl();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.OK_Button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // customizeProcessControl
            // 
            this.customizeProcessControl.Location = new System.Drawing.Point(12, 12);
            this.customizeProcessControl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.customizeProcessControl.Name = "customizeProcessControl";
            this.customizeProcessControl.Size = new System.Drawing.Size(253, 146);
            this.customizeProcessControl.TabIndex = 1;
            this.customizeProcessControl.UIMollierProcess = null;
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Location = new System.Drawing.Point(224, 159);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(102, 31);
            this.Cancel_Button.TabIndex = 3;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // OK_Button
            // 
            this.OK_Button.Location = new System.Drawing.Point(116, 159);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(102, 31);
            this.OK_Button.TabIndex = 2;
            this.OK_Button.Text = "OK";
            this.OK_Button.UseVisualStyleBackColor = true;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // CustomizeProcessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(338, 202);
            this.ControlBox = false;
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.Cancel_Button);
            this.Controls.Add(this.customizeProcessControl);
            this.Name = "CustomizeProcessForm";
            this.ShowIcon = false;
            this.Text = "Modify Process";
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.UIMollierProcessControl customizeProcessControl;
        private System.Windows.Forms.Button Cancel_Button;
        private System.Windows.Forms.Button OK_Button;
    }
}