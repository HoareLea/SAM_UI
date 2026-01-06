// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using SAM.Geometry.Planar;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static Solver2DData Solver2DData(UIMollierPoint mollierPoint, ChartType chartType, Vector2D scaleVector, double axesRatio)
        {
            UIMollierPointAppearance uIMollierPointAppearance = mollierPoint.UIMollierAppearance as UIMollierPointAppearance;
            if(uIMollierPointAppearance == null)
            {
                return null;
            }

            UIMollierLabelAppearance uIMollierLabelAppearance = uIMollierPointAppearance.UIMollierLabelAppearance;
            if(uIMollierLabelAppearance == null)
            {
                return null;
            }

            string text = uIMollierLabelAppearance.Text;
            if(string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (uIMollierLabelAppearance.Vector2D != null)
            {
                return null;
            }

            Point2D point = Convert.ToSAM(mollierPoint, chartType);

            Point2D labelCenter = getLabelCenter(point, chartType, scaleVector);

            Rectangle2D labelRectangle = textToRectangle(labelCenter, text, chartType, scaleVector, axesRatio);

            Solver2DData result = new Solver2DData(labelRectangle, point.GetScaledY(axesRatio));
            result.Tag = mollierPoint;

            Solver2DSettings solver2DSettings = new Solver2DSettings()
            {
                IterationCount = 10,
                StartingDistance = 0.2,
                ShiftDistance = 0.1 * scaleVector.X,
            };

            result.Solver2DSettings = solver2DSettings;

            return result;
        }
    }
}
