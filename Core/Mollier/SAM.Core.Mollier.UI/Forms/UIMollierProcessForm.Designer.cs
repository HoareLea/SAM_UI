namespace SAM.Core.Mollier.UI
{
    partial class UIMollierProcessForm
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
            this.OK_Button = new System.Windows.Forms.Button();
            this.Cancel_Button = new System.Windows.Forms.Button();
            this.Button_Apply = new System.Windows.Forms.Button();
            this.UIMollierProcessControl_Main = new SAM.Core.Mollier.UI.Controls.UIMollierProcessControl();
            this.SuspendLayout();
            // 
            // OK_Button
            // 
            this.OK_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK_Button.Location = new System.Drawing.Point(308, 204);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(102, 31);
            this.OK_Button.TabIndex = 4;
            this.OK_Button.Text = "OK";
            this.OK_Button.UseVisualStyleBackColor = true;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel_Button.Location = new System.Drawing.Point(416, 204);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(102, 31);
            this.Cancel_Button.TabIndex = 5;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // Button_Apply
            // 
            this.Button_Apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Apply.Location = new System.Drawing.Point(200, 204);
            this.Button_Apply.Name = "Button_Apply";
            this.Button_Apply.Size = new System.Drawing.Size(102, 31);
            this.Button_Apply.TabIndex = 4;
            this.Button_Apply.Text = "Apply";
            this.Button_Apply.UseVisualStyleBackColor = true;
            this.Button_Apply.Click += new System.EventHandler(this.Button_Apply_Click);
            // 
            // UIMollierProcessControl_Main
            // 
            this.UIMollierProcessControl_Main.Location = new System.Drawing.Point(12, 12);
            this.UIMollierProcessControl_Main.MollierControl = null;
            this.UIMollierProcessControl_Main.Name = "UIMollierProcessControl_Main";
            this.UIMollierProcessControl_Main.Size = new System.Drawing.Size(513, 174);
            this.UIMollierProcessControl_Main.TabIndex = 6;
            this.UIMollierProcessControl_Main.UIMollierProcess = null;
            // 
            // UIMollierProcessForm
            // 
            this.AcceptButton = this.OK_Button;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel_Button;
            this.ClientSize = new System.Drawing.Size(530, 247);
            this.ControlBox = false;
            this.Controls.Add(this.UIMollierProcessControl_Main);
            this.Controls.Add(this.Button_Apply);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.Cancel_Button);
            this.Name = "UIMollierProcessForm";
            this.ShowIcon = false;
            this.Text = "Mollier Process";
            this.Load += new System.EventHandler(this.UIMollierProcessForm_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.Button Cancel_Button;
        private System.Windows.Forms.Button Button_Apply;
        private Controls.UIMollierProcessControl UIMollierProcessControl_Main;
    }
}