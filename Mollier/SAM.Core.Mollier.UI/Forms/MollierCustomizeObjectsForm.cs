using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class MollierCustomizeObjectsForm : Form
    {
        private List<UIMollierPoint> uIMollierPoints;
        private List<UIMollierProcess> uIMollierProcesses;
        private List<UIMollierZone> uIMollierZones;
        private MollierControlSettings mollierControlSettings;

        public MollierForm MollierForm { get; set; }

        public MollierCustomizeObjectsForm()
        {
            InitializeComponent();
        }
        public MollierCustomizeObjectsForm(List<UIMollierPoint> mollierPoints, List<UIMollierProcess> mollierProcesses, List<UIMollierZone> mollierZones, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierPoints = mollierPoints;
            this.uIMollierProcesses = mollierProcesses;
            this.uIMollierZones = mollierZones;
            this.mollierControlSettings = mollierControlSettings;

            if(uIMollierProcesses != null)
            {
                foreach(UIMollierProcess uIMollierProcess in uIMollierProcesses)
                {
                    flowLayoutPanelProcesses.Controls.Add(new Controls.ProcessCustomizeControl(uIMollierProcess, mollierControlSettings));
                }
            }
        }


        private void MollierObjectsControlForm_Load(object sender, EventArgs e)
        { 

        }
        public void removeMollierProcess(UIMollierProcess mollierProcesses)
        {
            if (mollierProcesses != null)
            {
                foreach (UIMollierProcess mollierProcess in uIMollierProcesses)
                {
                    //if(mollierProcess.Start ==)
                }
            }
        }

        private void acceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
