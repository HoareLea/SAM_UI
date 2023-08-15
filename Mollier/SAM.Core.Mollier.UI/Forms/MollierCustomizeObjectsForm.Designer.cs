namespace SAM.Core.Mollier.UI.Forms
{
    partial class MollierCustomizeObjectsForm
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
            this.customizeMollierObjectsTabControl = new System.Windows.Forms.TabControl();
            this.tabCustomizePointPage = new System.Windows.Forms.TabPage();
            this.acceptButton = new System.Windows.Forms.Button();
            this.tabCustomizeProcessesPage = new System.Windows.Forms.TabPage();
            this.processesEndLabel_Y = new System.Windows.Forms.Label();
            this.processesEndLabel_X = new System.Windows.Forms.Label();
            this.processesStartLabel_Y = new System.Windows.Forms.Label();
            this.processesStartLabel_X = new System.Windows.Forms.Label();
            this.processesNameLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanelProcesses = new System.Windows.Forms.FlowLayoutPanel();
            this.processesStartLabel = new System.Windows.Forms.Label();
            this.processesEndLabel = new System.Windows.Forms.Label();
            this.customizeMollierObjectsTabControl.SuspendLayout();
            this.tabCustomizeProcessesPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // customizeMollierObjectsTabControl
            // 
            this.customizeMollierObjectsTabControl.Controls.Add(this.tabCustomizePointPage);
            this.customizeMollierObjectsTabControl.Controls.Add(this.tabCustomizeProcessesPage);
            this.customizeMollierObjectsTabControl.Location = new System.Drawing.Point(0, 0);
            this.customizeMollierObjectsTabControl.Name = "customizeMollierObjectsTabControl";
            this.customizeMollierObjectsTabControl.SelectedIndex = 0;
            this.customizeMollierObjectsTabControl.Size = new System.Drawing.Size(784, 414);
            this.customizeMollierObjectsTabControl.TabIndex = 0;
            // 
            // tabCustomizePointPage
            // 
            this.tabCustomizePointPage.Location = new System.Drawing.Point(4, 29);
            this.tabCustomizePointPage.Name = "tabCustomizePointPage";
            this.tabCustomizePointPage.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomizePointPage.Size = new System.Drawing.Size(776, 381);
            this.tabCustomizePointPage.TabIndex = 0;
            this.tabCustomizePointPage.Text = "Points";
            this.tabCustomizePointPage.UseVisualStyleBackColor = true;
            // 
            // acceptButton
            // 
            this.acceptButton.Location = new System.Drawing.Point(597, 420);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(119, 41);
            this.acceptButton.TabIndex = 1;
            this.acceptButton.Text = "OK";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.acceptButton_Click);
            // 
            // tabCustomizeProcessesPage
            // 
            this.tabCustomizeProcessesPage.Controls.Add(this.processesEndLabel_Y);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesEndLabel_X);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesStartLabel_Y);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesStartLabel_X);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesNameLabel);
            this.tabCustomizeProcessesPage.Controls.Add(this.flowLayoutPanelProcesses);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesStartLabel);
            this.tabCustomizeProcessesPage.Controls.Add(this.processesEndLabel);
            this.tabCustomizeProcessesPage.Location = new System.Drawing.Point(4, 29);
            this.tabCustomizeProcessesPage.Name = "tabCustomizeProcessesPage";
            this.tabCustomizeProcessesPage.Padding = new System.Windows.Forms.Padding(3);
            this.tabCustomizeProcessesPage.Size = new System.Drawing.Size(776, 381);
            this.tabCustomizeProcessesPage.TabIndex = 1;
            this.tabCustomizeProcessesPage.Text = "Processes";
            this.tabCustomizeProcessesPage.UseVisualStyleBackColor = true;
            // 
            // processesEndLabel_Y
            // 
            this.processesEndLabel_Y.AutoSize = true;
            this.processesEndLabel_Y.Location = new System.Drawing.Point(375, 24);
            this.processesEndLabel_Y.Name = "processesEndLabel_Y";
            this.processesEndLabel_Y.Size = new System.Drawing.Size(20, 20);
            this.processesEndLabel_Y.TabIndex = 6;
            this.processesEndLabel_Y.Text = "Y";
            // 
            // processesEndLabel_X
            // 
            this.processesEndLabel_X.AutoSize = true;
            this.processesEndLabel_X.Location = new System.Drawing.Point(335, 24);
            this.processesEndLabel_X.Name = "processesEndLabel_X";
            this.processesEndLabel_X.Size = new System.Drawing.Size(20, 20);
            this.processesEndLabel_X.TabIndex = 5;
            this.processesEndLabel_X.Text = "X";
            // 
            // processesStartLabel_Y
            // 
            this.processesStartLabel_Y.AutoSize = true;
            this.processesStartLabel_Y.Location = new System.Drawing.Point(265, 24);
            this.processesStartLabel_Y.Name = "processesStartLabel_Y";
            this.processesStartLabel_Y.Size = new System.Drawing.Size(20, 20);
            this.processesStartLabel_Y.TabIndex = 4;
            this.processesStartLabel_Y.Text = "Y";
            // 
            // processesStartLabel_X
            // 
            this.processesStartLabel_X.AutoSize = true;
            this.processesStartLabel_X.Location = new System.Drawing.Point(228, 24);
            this.processesStartLabel_X.Name = "processesStartLabel_X";
            this.processesStartLabel_X.Size = new System.Drawing.Size(20, 20);
            this.processesStartLabel_X.TabIndex = 3;
            this.processesStartLabel_X.Text = "X";
            // 
            // processesNameLabel
            // 
            this.processesNameLabel.AutoSize = true;
            this.processesNameLabel.Location = new System.Drawing.Point(51, 23);
            this.processesNameLabel.Name = "processesNameLabel";
            this.processesNameLabel.Size = new System.Drawing.Size(51, 20);
            this.processesNameLabel.TabIndex = 0;
            this.processesNameLabel.Text = "Name";
            // 
            // flowLayoutPanelProcesses
            // 
            this.flowLayoutPanelProcesses.AutoScroll = true;
            this.flowLayoutPanelProcesses.Location = new System.Drawing.Point(4, 47);
            this.flowLayoutPanelProcesses.Name = "flowLayoutPanelProcesses";
            this.flowLayoutPanelProcesses.Size = new System.Drawing.Size(763, 322);
            this.flowLayoutPanelProcesses.TabIndex = 1;
            // 
            // processesStartLabel
            // 
            this.processesStartLabel.AutoSize = true;
            this.processesStartLabel.Location = new System.Drawing.Point(236, 4);
            this.processesStartLabel.Name = "processesStartLabel";
            this.processesStartLabel.Size = new System.Drawing.Size(44, 20);
            this.processesStartLabel.TabIndex = 1;
            this.processesStartLabel.Text = "Start";
            // 
            // processesEndLabel
            // 
            this.processesEndLabel.AutoSize = true;
            this.processesEndLabel.Location = new System.Drawing.Point(345, 3);
            this.processesEndLabel.Name = "processesEndLabel";
            this.processesEndLabel.Size = new System.Drawing.Size(38, 20);
            this.processesEndLabel.TabIndex = 2;
            this.processesEndLabel.Text = "End";
            // 
            // MollierCustomizeObjectsForm
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(797, 474);
            this.Controls.Add(this.acceptButton);
            this.Controls.Add(this.customizeMollierObjectsTabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MollierCustomizeObjectsForm";
            this.Text = "MollierObjectsControlForm";
            this.Load += new System.EventHandler(this.MollierObjectsControlForm_Load);
            this.customizeMollierObjectsTabControl.ResumeLayout(false);
            this.tabCustomizeProcessesPage.ResumeLayout(false);
            this.tabCustomizeProcessesPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl customizeMollierObjectsTabControl;
        private System.Windows.Forms.TabPage tabCustomizePointPage;
        private System.Windows.Forms.TabPage tabCustomizeProcessesPage;
        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelProcesses;
        private System.Windows.Forms.Label processesEndLabel_Y;
        private System.Windows.Forms.Label processesEndLabel_X;
        private System.Windows.Forms.Label processesStartLabel_Y;
        private System.Windows.Forms.Label processesStartLabel_X;
        private System.Windows.Forms.Label processesNameLabel;
        private System.Windows.Forms.Label processesStartLabel;
        private System.Windows.Forms.Label processesEndLabel;
    }
}