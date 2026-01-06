// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierProcessControl : UserControl
    {
        private Color color_Empty = Color.Empty;

        private UIMollierProcess uIMollierProcess;

        public event MollierPointSelectingEventHandler MollierPointSelecting;
        public event MollierPointSelectedEventHandler MollierPointSelected;
        private event EventHandler ApplyClicked;

        public UIMollierProcessControl()
        {
            InitializeComponent();

            if (Button_ProcessColor != null)
            {
                color_Empty = Button_ProcessColor.BackColor;
            }

            UIMollierProcessPointControl_Start.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;
            UIMollierProcessPointControl_Process.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;
            UIMollierProcessPointControl_End.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;

            UIMollierProcessPointControl_Start.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
            UIMollierProcessPointControl_Process.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
            UIMollierProcessPointControl_End.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
        }

        public UIMollierProcessControl(UIMollierProcess uIMollierProcess)
        {
            InitializeComponent();

            if (Button_ProcessColor != null)
            {
                color_Empty = Button_ProcessColor.BackColor;
            }

            UIMollierProcessPointControl_Start.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;
            UIMollierProcessPointControl_Process.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;
            UIMollierProcessPointControl_End.MollierPointSelected += UIMollierProcessPointControl_MollierPointSelected;

            UIMollierProcessPointControl_Start.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
            UIMollierProcessPointControl_Process.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
            UIMollierProcessPointControl_End.MollierPointSelecting += UIMollierProcessPointControl_MollierPointSelecting;
        }

        private void UIMollierProcessPointControl_MollierPointSelecting(object sender, EventArgs e)
        {
            MollierPointSelecting?.Invoke(this, e);
        }

        private void UIMollierProcessPointControl_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            MollierPointSelected?.Invoke(this, e);
        }

        private void Button_ProcessColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                Button_ProcessColor.BackColor = colorDialog.Color;
            }
        }

        public UIMollierProcess UIMollierProcess
        {
            get
            {
                return GetUIMollierProcess();
            }

            set
            {
                SetUIMollierProcess(value);
            }
        }

        private UIMollierProcess GetUIMollierProcess()
        {
            UIMollierProcess result =  uIMollierProcess == null ? null : new UIMollierProcess(uIMollierProcess);
            if(result != null)
            {
                result.UIMollierPointAppearance_Start = UIMollierProcessPointControl_Start?.UIMollierProcessPoint?.UIMollierAppearance as UIMollierPointAppearance;
                result.UIMollierAppearance = UIMollierProcessPointControl_Process?.UIMollierProcessPoint?.UIMollierAppearance;
                result.UIMollierPointAppearance_End = UIMollierProcessPointControl_End?.UIMollierProcessPoint?.UIMollierAppearance as UIMollierPointAppearance;

                result.UIMollierAppearance.Color = Button_ProcessColor.BackColor == color_Empty ? Color.Empty : Button_ProcessColor.BackColor;
            }

            return result;
        }

        private void SetUIMollierProcess(UIMollierProcess uIMollierProcess)
        {
            this.uIMollierProcess = uIMollierProcess;

            UIMollierProcessPointControl_Start.UIMollierProcessPoint = uIMollierProcess == null ? null : new UIMollierProcessPoint(uIMollierProcess, ProcessReferenceType.Start);
            UIMollierProcessPointControl_Process.UIMollierProcessPoint = uIMollierProcess == null ? null : new UIMollierProcessPoint(uIMollierProcess, ProcessReferenceType.Process);
            UIMollierProcessPointControl_End.UIMollierProcessPoint = uIMollierProcess == null ? null : new UIMollierProcessPoint(uIMollierProcess, ProcessReferenceType.End);

            Button_ProcessColor.BackColor = color_Empty;

            UIMollierAppearance uIMollierAppearance = uIMollierProcess?.UIMollierAppearance as UIMollierAppearance;
            if(uIMollierAppearance != null)
            {
                if(uIMollierAppearance.Color != Color.Empty)
                {
                    Button_ProcessColor.BackColor = uIMollierAppearance.Color;
                }
            }
        }

        private void Button_ProcessColor_Clear_Click(object sender, EventArgs e)
        {
            Button_ProcessColor.BackColor = color_Empty;
        }

        public MollierControl MollierControl
        {
            get
            {
                return UIMollierProcessPointControl_Start.MollierControl;
            }

            set
            {
                UIMollierProcessPointControl_Start.MollierControl = value;
                UIMollierProcessPointControl_Process.MollierControl = value;
                UIMollierProcessPointControl_End.MollierControl = value;
            }
        }
    }
}
