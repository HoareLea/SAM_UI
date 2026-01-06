// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using SAM.Geometry.Mollier;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class UIMollierPointControl : UserControl
    {
        private static string locationSetText = "SET";
        
        private Color color_Empty = Color.Empty;

        public event MollierPointSelectingEventHandler MollierPointSelecting;
        public event MollierPointSelectedEventHandler MollierPointSelected;

        private MollierControl mollierControl = null;

        private UIMollierPoint uIMollierPoint;
        
        public UIMollierPointControl()
        {
            InitializeComponent();

            color_Empty = PointColor_Button.BackColor;
            SetLocationVisibility(false);
        }
        
        public UIMollierPointControl(UIMollierPoint uIMollierPoint)
        {
            InitializeComponent();

            color_Empty = PointColor_Button.BackColor;

            if (uIMollierPoint != null)
            {
                SetUIMollierPoint(uIMollierPoint);
            }

            SetLocationVisibility(false);
        }

        public UIMollierPoint UIMollierPoint
        {
            get
            {
                if(uIMollierPoint == null || uIMollierPoint.UIMollierAppearance == null)
                {
                    return null;
                }

                UIMollierAppearance uIMollierAppearance = uIMollierPoint.UIMollierAppearance as UIMollierAppearance;

                uIMollierAppearance.Color = PointColor_Button.BackColor == color_Empty ? Color.Empty : PointColor_Button.BackColor;
                uIMollierAppearance.Label = PointLabel_TextBox.Text;

                UIMollierLabelAppearance uIMollierLabelAppearance = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance).UIMollierLabelAppearance;
                if(Button_LabelLocation.Text != locationSetText)
                {
                    uIMollierLabelAppearance.Vector2D = null;
                }

                if (LabelColor_Button.BackColor == color_Empty)
                {
                    if (uIMollierLabelAppearance != null)
                    {
                        uIMollierLabelAppearance.Color = Color.Empty;
                    }
                }
                else
                {
                    if (uIMollierLabelAppearance == null)
                    {
                        uIMollierLabelAppearance = new UIMollierLabelAppearance();
                    }

                    uIMollierLabelAppearance.Color = LabelColor_Button.BackColor;
                }

                uIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;

                uIMollierPoint.UIMollierAppearance = uIMollierLabelAppearance;

                return uIMollierPoint;
            }
            set
            {
                SetUIMollierPoint(value);
            }
        }

        public MollierControl MollierControl
        {
            get
            {
                return mollierControl;
            }

            set
            {
                mollierControl = value;
                SetLocationVisibility(mollierControl != null);
            }
        }

        private void SetLocationVisibility(bool visible)
        {
            Label_LabelLocation.Visible = visible;
            Button_LabelLocation.Visible = visible;
            Button_LabelLocationClear.Visible = visible;
        }
        
        private void PointColor_Button_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                PointColor_Button.BackColor = colorDialog.Color;
            }
        }
        
        private void SetUIMollierPoint(UIMollierPoint uIMollierPoint)
        {
            if(uIMollierPoint == null)
            {
                return;
            }

            this.uIMollierPoint = uIMollierPoint;

            if (uIMollierPoint.UIMollierAppearance != null)
            {
                PointLabel_TextBox.Text = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance)?.Label;
                PointColor_Button.BackColor = uIMollierPoint.UIMollierAppearance.Color == Color.Empty ? color_Empty : uIMollierPoint.UIMollierAppearance.Color;
                if ((uIMollierPoint.UIMollierAppearance as UIMollierAppearance)?.UIMollierLabelAppearance != null)
                {
                    UIMollierLabelAppearance uIMollierLabelAppearance = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance)?.UIMollierLabelAppearance;

                    LabelColor_Button.BackColor = uIMollierLabelAppearance.Color == Color.Empty ? color_Empty : uIMollierLabelAppearance.Color;

                    if(uIMollierLabelAppearance.Vector2D != null)
                    {
                        Button_LabelLocation.Text = locationSetText;
                    }
                }
                else
                {
                    LabelColor_Button.BackColor = color_Empty;
                }
            }
        }

        private void LabelColor_Button_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                LabelColor_Button.BackColor = colorDialog.Color;
            }
        }

        private void Button_PointClear_Click(object sender, EventArgs e)
        {
            PointColor_Button.BackColor = color_Empty;
        }

        private void Button_LabelClear_Click(object sender, EventArgs e)
        {
            LabelColor_Button.BackColor = color_Empty;
        }

        private void Button_LabelLocation_Click(object sender, EventArgs e)
        {
            MollierPointSelecting?.Invoke(this, EventArgs.Empty);

            mollierControl.MollierPointSelected += MollierControl_MollierPointSelected;
        }

        private void MollierControl_MollierPointSelected(object sender, MollierPointSelectedEventArgs e)
        {
            MollierPointSelected?.Invoke(this, e);

            mollierControl.MollierPointSelected -= MollierControl_MollierPointSelected;

            if(uIMollierPoint.UIMollierAppearance == null)
            {
                uIMollierPoint.UIMollierAppearance = new UIMollierPointAppearance();
            }

            UIMollierAppearance uIMollierAppearance = uIMollierPoint.UIMollierAppearance as UIMollierAppearance;

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierAppearance?.UIMollierLabelAppearance;
            if (uIMollierLabelAppearance == null)
            {
                uIMollierLabelAppearance = new UIMollierLabelAppearance();
            }

            //ChartType chartType = mollierControl.MollierControlSettings.ChartType;

            //double humidityRatio = e.MollierPoint.HumidityRatio;
            //double dryBulbTemperature = e.MollierPoint.DryBulbTemperature;
            //double diagramTemperature = e.MollierPoint.DryBulbTemperature;

            //double x = chartType == ChartType.Mollier ? humidityRatio * 1000 : dryBulbTemperature;
            //double y = chartType == ChartType.Mollier ? diagramTemperature : humidityRatio * 1000;

            //Point2D point2D_Selected = mollierControl.GetValueToPixelPosition(new Point2D(x, y));

            //humidityRatio = uIMollierPoint.HumidityRatio;
            //dryBulbTemperature = uIMollierPoint.DryBulbTemperature;
            //diagramTemperature = uIMollierPoint.DryBulbTemperature;

            //x = chartType == ChartType.Mollier ? humidityRatio * 1000 : dryBulbTemperature;
            //y = chartType == ChartType.Mollier ? diagramTemperature : humidityRatio * 1000;

            //Point2D point2D = mollierControl.GetValueToPixelPosition(new Point2D(x, y));

            Point2D point2D_Selected = Convert.ToSAM(e.MollierPoint, mollierControl.MollierControlSettings.ChartType);
            Point2D point2D = Convert.ToSAM(uIMollierPoint, mollierControl.MollierControlSettings.ChartType);

            //point2D = mollierControl.GetValueToPixelPosition(point2D);
            //point2D_Selected = mollierControl.GetValueToPixelPosition(point2D_Selected);

            //Point2D point2D_Selected = mollierControl.GetPoint2D(e.MollierPoint);
            //Point2D point2D = mollierControl.GetPoint2D(uIMollierPoint);

            uIMollierLabelAppearance.Vector2D = point2D_Selected - point2D;

            uIMollierAppearance.UIMollierLabelAppearance = uIMollierLabelAppearance;

            uIMollierPoint.UIMollierAppearance = uIMollierAppearance;

            Button_LabelLocation.Text = locationSetText;
        }

        private void Button_LabelLocationClear_Click(object sender, EventArgs e)
        {
            UIMollierLabelAppearance uIMollierLabelAppearance = (uIMollierPoint.UIMollierAppearance as UIMollierAppearance)?.UIMollierLabelAppearance;
            if(uIMollierLabelAppearance == null)
            {
                return;
            }

            uIMollierLabelAppearance.Vector2D = null;

            (uIMollierPoint.UIMollierAppearance as UIMollierAppearance).UIMollierLabelAppearance = uIMollierLabelAppearance;
            Button_LabelLocation.Text = null;
        }
    }
}
