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
    public partial class ManageMollierObjectsForm : Form
    {
        private List<UIMollierPoint> uIMollierPoints;
        private List<UIMollierProcess> uIMollierProcesses;
        private List<UIMollierZone> uIMollierZones;
        private MollierControlSettings mollierControlSettings;

        public event MollierProcessRemovedEventHandler MollierProcessRemoved;
        public MollierForm MollierForm { get; set; }

        public ManageMollierObjectsForm()
        {
            InitializeComponent();
        }
        public ManageMollierObjectsForm(List<UIMollierPoint> mollierPoints, List<UIMollierProcess> mollierProcesses, List<UIMollierZone> mollierZones, MollierControlSettings mollierControlSettings)
        {
            InitializeComponent();
            this.uIMollierPoints = mollierPoints;
            this.uIMollierProcesses = mollierProcesses;
            this.uIMollierZones = mollierZones;
            this.mollierControlSettings = mollierControlSettings;

            if(uIMollierPoints != null)
            {
                foreach (UIMollierPoint uIMollierPoint in uIMollierPoints)
                {
                    Control pointCustomizeControl = new Controls.PointManageControl(uIMollierPoint, mollierControlSettings);
                   // pointCustomizeControl.Pro
                    pointCustomizeControl.Tag = uIMollierPoint;
                    flowLayoutPanelPoints.Controls.Add(pointCustomizeControl);
                }
            }

            if(uIMollierProcesses != null)
            {
                foreach(UIMollierProcess uIMollierProcess in uIMollierProcesses)
                {
                    Controls.ProcessManageControl processCustomizeControl = new Controls.ProcessManageControl(uIMollierProcess, mollierControlSettings);
                    processCustomizeControl.MollierProcessRemoved += ProcessCustomizeControl_MollierProcessRemoved; 
                    processCustomizeControl.Tag = uIMollierProcess;
                    flowLayoutPanelProcesses.Controls.Add(processCustomizeControl);
                }
            }
        }

        private void ProcessCustomizeControl_MollierProcessRemoved(object sender, MollierProcessRemovedEventArgs e) // sender - kto wysyła -> process manage control
        {                                                                                                          // e - wiadomość -> mollierProcess
            flowLayoutPanelProcesses.Controls.Remove((Controls.ProcessManageControl)sender);

            uIMollierProcesses.Remove(e.MollierProcess);
            MollierProcessRemoved.Invoke(this, e);
        }

        public void RemoveMollierProcess(UIMollierProcess mollierProcess)
        {
            if (mollierProcess != null)
            {
                foreach (UIMollierProcess mollierProcessTemp in uIMollierProcesses)
                {
                    if(mollierProcess.Equals(mollierProcessTemp))
                    {
                        uIMollierProcesses.Remove(mollierProcessTemp);

                        for(int i=0; i<flowLayoutPanelProcesses.Controls.Count; i++)
                        {
                            if(flowLayoutPanelProcesses.Controls[i].Tag == mollierProcessTemp)
                            {
                                flowLayoutPanelProcesses.Controls.RemoveAt(i);
                                break;
                            }
                        }
                        break;
                    }
                }  
            }
        }
        public void AddMollierProcess(UIMollierProcess mollierProcess)
        {
            if(mollierProcess == null)
            {
                return;
            }

            Control processCustomizeControl = new Controls.ProcessManageControl(mollierProcess, mollierControlSettings);
            processCustomizeControl.Tag = mollierProcess;
            uIMollierProcesses.Add(mollierProcess);
            flowLayoutPanelProcesses.Controls.Add(processCustomizeControl);
        }
        public void RemoveMollierPoint(UIMollierPoint mollierPoint)
        {
            if (mollierPoint != null)
            {
                foreach (UIMollierPoint mollierPointsTemp in uIMollierPoints)
                {
                    if (mollierPoint.Equals(mollierPointsTemp))
                    {
                        uIMollierPoints.Remove(mollierPointsTemp);

                        for (int i = 0; i < flowLayoutPanelPoints.Controls.Count; i++)
                        {
                            if (flowLayoutPanelPoints.Controls[i].Tag == mollierPointsTemp)
                            {
                                flowLayoutPanelPoints.Controls.RemoveAt(i);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
        public void AddMollierPoint(UIMollierPoint mollierPoint)
        {
            if (mollierPoint == null)
            {
                return;
            }

            Control pointCustomizeControl = new Controls.PointManageControl(mollierPoint, mollierControlSettings);
            pointCustomizeControl.Tag = mollierPoint;
            uIMollierPoints.Add(mollierPoint);
            flowLayoutPanelPoints.Controls.Add(pointCustomizeControl);
        }
        private void acceptButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
