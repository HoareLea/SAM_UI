namespace SAM.Core.Mollier.UI.Forms
{
    partial class MollierPointForm
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
            SAM.Geometry.Mollier.UIMollierAppearance uiMollierAppearance1 = new SAM.Geometry.Mollier.UIMollierAppearance();
            this.Button_OK = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.MollierPointControl_Main = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.UIMollierAppearanceControl_Main = new SAM.Core.Mollier.UI.Controls.UIMollierAppearanceControl();
            this.SuspendLayout();
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(77, 308);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 23);
            this.Button_OK.TabIndex = 3;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            this.Button_OK.Click += new System.EventHandler(this.Button_OK_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button_Cancel.Location = new System.Drawing.Point(158, 308);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 23);
            this.Button_Cancel.TabIndex = 4;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // MollierPointControl_Main
            // 
            this.MollierPointControl_Main.AutoSize = true;
            this.MollierPointControl_Main.Location = new System.Drawing.Point(3, 12);
            this.MollierPointControl_Main.Name = "MollierPointControl_Main";
            this.MollierPointControl_Main.Pressure = 101325D;
            this.MollierPointControl_Main.PressureEnabled = true;
            this.MollierPointControl_Main.PressureVisible = true;
            this.MollierPointControl_Main.SelectMollierPointVisible = false;
            this.MollierPointControl_Main.Size = new System.Drawing.Size(230, 202);
            this.MollierPointControl_Main.TabIndex = 1;
            // 
            // UIMollierAppearanceControl_Main
            // 
            this.UIMollierAppearanceControl_Main.AutoSize = true;
            this.UIMollierAppearanceControl_Main.Location = new System.Drawing.Point(3, 221);
            this.UIMollierAppearanceControl_Main.Name = "UIMollierAppearanceControl_Main";
            this.UIMollierAppearanceControl_Main.Size = new System.Drawing.Size(242, 57);
            this.UIMollierAppearanceControl_Main.TabIndex = 2;
            uiMollierAppearance1.Color = System.Drawing.Color.Empty;
            uiMollierAppearance1.Label = "";
            uiMollierAppearance1.Size = 1;
            uiMollierAppearance1.Visible = true;
            this.UIMollierAppearanceControl_Main.UIMollierAppearance = uiMollierAppearance1;
            // 
            // MollierPointForm
            // 
            this.AcceptButton = this.Button_OK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.Button_Cancel;
            this.ClientSize = new System.Drawing.Size(245, 343);
            this.Controls.Add(this.UIMollierAppearanceControl_Main);
            this.Controls.Add(this.MollierPointControl_Main);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.Button_OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierPointForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Point";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button Button_OK;
        private System.Windows.Forms.Button Button_Cancel;
        private Controls.MollierPointControl MollierPointControl_Main;
        private Controls.UIMollierAppearanceControl UIMollierAppearanceControl_Main;
    }
}