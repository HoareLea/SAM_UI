namespace SAM.Core.Mollier.UI.Controls
{
    partial class HeatingProcessControl
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
            this.processCalculateType_ComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.flowLayoutPanel_Main = new System.Windows.Forms.FlowLayoutPanel();
            this.MollierPointControl_Start = new SAM.Core.Mollier.UI.Controls.MollierPointControl();
            this.SuspendLayout();
            // 
            // processCalculateType_ComboBox
            // 
            this.processCalculateType_ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.processCalculateType_ComboBox.FormattingEnabled = true;
            this.processCalculateType_ComboBox.Items.AddRange(new object[] {
            "Dry Bulb Temperature",
            "Enthalpy Difference",
            "Dry Bulb Temperature Difference"});
            this.processCalculateType_ComboBox.Location = new System.Drawing.Point(261, 23);
            this.processCalculateType_ComboBox.Name = "processCalculateType_ComboBox";
            this.processCalculateType_ComboBox.Size = new System.Drawing.Size(266, 24);
            this.processCalculateType_ComboBox.TabIndex = 0;
            this.processCalculateType_ComboBox.SelectedIndexChanged += new System.EventHandler(this.processCalculateType_ComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(257, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Calculation Type";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(14, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(155, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Air Start Conditions";
            // 
            // flowLayoutPanel_Main
            // 
            this.flowLayoutPanel_Main.Location = new System.Drawing.Point(261, 53);
            this.flowLayoutPanel_Main.Name = "flowLayoutPanel_Main";
            this.flowLayoutPanel_Main.Size = new System.Drawing.Size(360, 177);
            this.flowLayoutPanel_Main.TabIndex = 8;
            // 
            // MollierPointControl_Start
            // 
            this.MollierPointControl_Start.Location = new System.Drawing.Point(0, 23);
            this.MollierPointControl_Start.Name = "MollierPointControl_Start";
            this.MollierPointControl_Start.Size = new System.Drawing.Size(198, 220);
            this.MollierPointControl_Start.TabIndex = 5;
            this.MollierPointControl_Start.Load += new System.EventHandler(this.MollierPointControl_Start_Load);
            // 
            // HeatingProcessControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.flowLayoutPanel_Main);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.MollierPointControl_Start);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.processCalculateType_ComboBox);
            this.Name = "HeatingProcessControl";
            this.Size = new System.Drawing.Size(655, 280);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox processCalculateType_ComboBox;
        private System.Windows.Forms.Label label1;
        private MollierPointControl MollierPointControl_Start;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Main;
    }
}
