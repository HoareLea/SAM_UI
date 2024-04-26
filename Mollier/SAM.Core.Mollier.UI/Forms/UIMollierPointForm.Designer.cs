namespace SAM.Core.Mollier.UI.Forms
{
    partial class UIMollierPointForm
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
            this.CustomizePointControl_Main = new SAM.Core.Mollier.UI.Controls.UIMollierPointControl();
            this.SuspendLayout();
            // 
            // OK_Button
            // 
            this.OK_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK_Button.Location = new System.Drawing.Point(126, 165);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(102, 31);
            this.OK_Button.TabIndex = 2;
            this.OK_Button.Text = "OK";
            this.OK_Button.UseVisualStyleBackColor = true;
            this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
            // 
            // Cancel_Button
            // 
            this.Cancel_Button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel_Button.Location = new System.Drawing.Point(234, 165);
            this.Cancel_Button.Name = "Cancel_Button";
            this.Cancel_Button.Size = new System.Drawing.Size(102, 31);
            this.Cancel_Button.TabIndex = 3;
            this.Cancel_Button.Text = "Cancel";
            this.Cancel_Button.UseVisualStyleBackColor = true;
            this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
            // 
            // Button_Apply
            // 
            this.Button_Apply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Apply.Location = new System.Drawing.Point(18, 165);
            this.Button_Apply.Name = "Button_Apply";
            this.Button_Apply.Size = new System.Drawing.Size(102, 31);
            this.Button_Apply.TabIndex = 2;
            this.Button_Apply.Text = "Apply";
            this.Button_Apply.UseVisualStyleBackColor = true;
            this.Button_Apply.Click += new System.EventHandler(this.Button_Apply_Click);
            // 
            // CustomizePointControl_Main
            // 
            this.CustomizePointControl_Main.Location = new System.Drawing.Point(12, 12);
            this.CustomizePointControl_Main.MollierControl = null;
            this.CustomizePointControl_Main.Name = "CustomizePointControl_Main";
            this.CustomizePointControl_Main.Size = new System.Drawing.Size(320, 132);
            this.CustomizePointControl_Main.TabIndex = 4;
            this.CustomizePointControl_Main.UIMollierPoint = null;
            // 
            // UIMollierPointForm
            // 
            this.AcceptButton = this.OK_Button;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.Cancel_Button;
            this.ClientSize = new System.Drawing.Size(344, 204);
            this.ControlBox = false;
            this.Controls.Add(this.CustomizePointControl_Main);
            this.Controls.Add(this.Button_Apply);
            this.Controls.Add(this.OK_Button);
            this.Controls.Add(this.Cancel_Button);
            this.Name = "UIMollierPointForm";
            this.ShowIcon = false;
            this.Text = "Edit Mollier Point";
            this.Load += new System.EventHandler(this.CustomizePointForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.Button Cancel_Button;
        private Controls.UIMollierPointControl CustomizePointControl_Main;
        private System.Windows.Forms.Button Button_Apply;
    }
}