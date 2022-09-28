﻿using SAM.Core.Mollier.UI.Controls;
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
    public partial class PointListOptionForm : Form
    {
        private MollierControl mollierControl;
        private MollierPoint resultPoint;
        public PointListOptionForm()
        {
            InitializeComponent();
        }
        public MollierPoint MollierPoint
        {
            get
            {
                return resultPoint;
            }
        }
        public PointListOptionForm(MollierControl mollierControl)
        {
            InitializeComponent();
            this.mollierControl = mollierControl;
            List<UIMollierProcess> mollierProcesses = mollierControl.UIMollierProcesses;
            if(mollierProcesses == null)
            {
                return;
            }
            //flowLayoutPanel1?.Dispose();
            foreach (UIMollierProcess UI_MollierProcess in mollierProcesses)
            {
                MollierPoint start = UI_MollierProcess.Start;
                string start_Label = UI_MollierProcess.Start_Label;
                if (start_Label != null && start_Label != "")
                {
                    flowLayoutPanel1.Controls.Add(new ListPointsOptionControl(start, start_Label, this));
                }
                MollierPoint end = UI_MollierProcess.End;
                string end_Label = UI_MollierProcess.End_Label;
                if (end_Label != null && end_Label != "")
                {
                    flowLayoutPanel1.Controls.Add(new ListPointsOptionControl(end, end_Label, this));
                }
            }

        }
        public void ChosenPoint(MollierPoint mollierPoint)
        {
            resultPoint = mollierPoint;
            if(resultPoint != null)
            {
                DialogResult = DialogResult.OK;
            }
            Close();
        }
    }
}