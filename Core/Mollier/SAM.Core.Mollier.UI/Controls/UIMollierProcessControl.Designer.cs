namespace SAM.Core.Mollier.UI.Controls
{
    partial class UIMollierProcessControl
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
            this.Label_ProcessColor = new System.Windows.Forms.Label();
            this.Button_ProcessColor = new System.Windows.Forms.Button();
            this.Button_ProcessColor_Clear = new System.Windows.Forms.Button();
            this.Label_LabelLocation = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.UIMollierProcessPointControl_Start = new SAM.Core.Mollier.UI.Controls.UIMollierProcessPointControl();
            this.UIMollierProcessPointControl_Process = new SAM.Core.Mollier.UI.Controls.UIMollierProcessPointControl();
            this.UIMollierProcessPointControl_End = new SAM.Core.Mollier.UI.Controls.UIMollierProcessPointControl();
            this.SuspendLayout();
            // 
            // Label_ProcessColor
            // 
            this.Label_ProcessColor.AutoSize = true;
            this.Label_ProcessColor.Location = new System.Drawing.Point(13, 26);
            this.Label_ProcessColor.Name = "Label_ProcessColor";
            this.Label_ProcessColor.Size = new System.Drawing.Size(90, 16);
            this.Label_ProcessColor.TabIndex = 0;
            this.Label_ProcessColor.Text = "Process color";
            // 
            // Button_ProcessColor
            // 
            this.Button_ProcessColor.Location = new System.Drawing.Point(109, 23);
            this.Button_ProcessColor.Name = "Button_ProcessColor";
            this.Button_ProcessColor.Size = new System.Drawing.Size(75, 23);
            this.Button_ProcessColor.TabIndex = 1;
            this.Button_ProcessColor.UseVisualStyleBackColor = true;
            this.Button_ProcessColor.Click += new System.EventHandler(this.Button_ProcessColor_Click);
            // 
            // Button_ProcessColor_Clear
            // 
            this.Button_ProcessColor_Clear.Location = new System.Drawing.Point(190, 23);
            this.Button_ProcessColor_Clear.Name = "Button_ProcessColor_Clear";
            this.Button_ProcessColor_Clear.Size = new System.Drawing.Size(30, 23);
            this.Button_ProcessColor_Clear.TabIndex = 3;
            this.Button_ProcessColor_Clear.Text = "c";
            this.Button_ProcessColor_Clear.UseVisualStyleBackColor = true;
            this.Button_ProcessColor_Clear.Click += new System.EventHandler(this.Button_ProcessColor_Clear_Click);
            // 
            // Label_LabelLocation
            // 
            this.Label_LabelLocation.AutoSize = true;
            this.Label_LabelLocation.Location = new System.Drawing.Point(253, 26);
            this.Label_LabelLocation.Name = "Label_LabelLocation";
            this.Label_LabelLocation.Size = new System.Drawing.Size(91, 16);
            this.Label_LabelLocation.TabIndex = 5;
            this.Label_LabelLocation.Text = "Label location";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(395, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 16);
            this.label1.TabIndex = 10;
            this.label1.Text = "Label color";
            // 
            // UIMollierProcessPointControl_Start
            // 
            this.UIMollierProcessPointControl_Start.LabelName = "Start Label";
            this.UIMollierProcessPointControl_Start.Location = new System.Drawing.Point(16, 52);
            this.UIMollierProcessPointControl_Start.MollierControl = null;
            this.UIMollierProcessPointControl_Start.Name = "UIMollierProcessPointControl_Start";
            this.UIMollierProcessPointControl_Start.Size = new System.Drawing.Size(486, 30);
            this.UIMollierProcessPointControl_Start.TabIndex = 11;
            // 
            // UIMollierProcessPointControl_Process
            // 
            this.UIMollierProcessPointControl_Process.LabelName = "Process Label";
            this.UIMollierProcessPointControl_Process.Location = new System.Drawing.Point(16, 88);
            this.UIMollierProcessPointControl_Process.MollierControl = null;
            this.UIMollierProcessPointControl_Process.Name = "UIMollierProcessPointControl_Process";
            this.UIMollierProcessPointControl_Process.Size = new System.Drawing.Size(486, 30);
            this.UIMollierProcessPointControl_Process.TabIndex = 12;
            // 
            // UIMollierProcessPointControl_End
            // 
            this.UIMollierProcessPointControl_End.LabelName = "End Label";
            this.UIMollierProcessPointControl_End.Location = new System.Drawing.Point(16, 124);
            this.UIMollierProcessPointControl_End.MollierControl = null;
            this.UIMollierProcessPointControl_End.Name = "UIMollierProcessPointControl_End";
            this.UIMollierProcessPointControl_End.Size = new System.Drawing.Size(486, 30);
            this.UIMollierProcessPointControl_End.TabIndex = 13;
            // 
            // UIMollierProcessControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.UIMollierProcessPointControl_End);
            this.Controls.Add(this.UIMollierProcessPointControl_Process);
            this.Controls.Add(this.UIMollierProcessPointControl_Start);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Label_LabelLocation);
            this.Controls.Add(this.Button_ProcessColor_Clear);
            this.Controls.Add(this.Button_ProcessColor);
            this.Controls.Add(this.Label_ProcessColor);
            this.Name = "UIMollierProcessControl";
            this.Size = new System.Drawing.Size(513, 174);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label_ProcessColor;
        private System.Windows.Forms.Button Button_ProcessColor;
        private System.Windows.Forms.Button Button_ProcessColor_Clear;
        private System.Windows.Forms.Label Label_LabelLocation;
        private System.Windows.Forms.Label label1;
        private UIMollierProcessPointControl UIMollierProcessPointControl_Start;
        private UIMollierProcessPointControl UIMollierProcessPointControl_Process;
        private UIMollierProcessPointControl UIMollierProcessPointControl_End;
    }
}
