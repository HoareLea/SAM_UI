
namespace SAM.Analytical.UI
{
    partial class AnalyticalForm
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
            this.Button_LoadLibrary = new System.Windows.Forms.Button();
            this.SplitContainer_Main = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer_Main)).BeginInit();
            this.SplitContainer_Main.Panel1.SuspendLayout();
            this.SplitContainer_Main.SuspendLayout();
            this.SuspendLayout();
            // 
            // Button_LoadLibrary
            // 
            this.Button_LoadLibrary.Location = new System.Drawing.Point(3, 12);
            this.Button_LoadLibrary.Name = "Button_LoadLibrary";
            this.Button_LoadLibrary.Size = new System.Drawing.Size(247, 28);
            this.Button_LoadLibrary.TabIndex = 0;
            this.Button_LoadLibrary.Text = "Load Library";
            this.Button_LoadLibrary.UseVisualStyleBackColor = true;
            this.Button_LoadLibrary.Click += new System.EventHandler(this.Button_LoadLibrary_Click);
            // 
            // SplitContainer_Main
            // 
            this.SplitContainer_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer_Main.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SplitContainer_Main.Location = new System.Drawing.Point(0, 0);
            this.SplitContainer_Main.Name = "SplitContainer_Main";
            // 
            // SplitContainer_Main.Panel1
            // 
            this.SplitContainer_Main.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.SplitContainer_Main.Panel1.Controls.Add(this.Button_LoadLibrary);
            this.SplitContainer_Main.Size = new System.Drawing.Size(1010, 677);
            this.SplitContainer_Main.SplitterDistance = 253;
            this.SplitContainer_Main.TabIndex = 1;
            // 
            // AnalyticalForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1010, 677);
            this.Controls.Add(this.SplitContainer_Main);
            this.Name = "AnalyticalForm";
            this.Text = "SAM Analytical";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.AnalyticalForm_FormClosed);
            this.Load += new System.EventHandler(this.AnalyticalForm_Load);
            this.SplitContainer_Main.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer_Main)).EndInit();
            this.SplitContainer_Main.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Button_LoadLibrary;
        private System.Windows.Forms.SplitContainer SplitContainer_Main;
    }
}

