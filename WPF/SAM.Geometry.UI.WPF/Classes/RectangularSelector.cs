using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SAM.Geometry.UI.WPF
{
    public class RectangularSelector
    {
        public event EventHandler Selected;
        public event EventHandler Selecting;

        private Panel panel;

        private Point? startPoint;
        private Point? endPoint;

        private System.Windows.Media.Brush stroke = System.Windows.Media.Brushes.Black;
        private double strokeThickness = 2;
        private System.Windows.Media.DoubleCollection strokeDashArray = new System.Windows.Media.DoubleCollection(new double[] { 2, 2 });
        private RectangularSelectorMode rectangularSelectorMode = RectangularSelectorMode.Extended;

        private List<Line> lines;

        public RectangularSelector(Panel panel)
        {
            this.panel = panel;
            if(panel != null)
            {
                panel.PreviewMouseMove += Panel_PreviewMouseMove;
            }

            lines = new List<Line>();
        }

        public bool IsActive()
        {
            if (startPoint == null || endPoint == null || !startPoint.HasValue || !endPoint.HasValue)
            {
                return false;
            }

            if (double.IsNaN(startPoint.Value.X) || double.IsNaN(startPoint.Value.Y) || double.IsNaN(endPoint.Value.X) || double.IsNaN(endPoint.Value.Y))
            {
                return false;
            }

            if(startPoint == endPoint)
            {
                return false;
            }

            return true;
        }

        public Planar.Rectangle2D Rectangle2D
        {
            get
            {
                if(!IsActive())
                {
                    return null;
                }

                return new Planar.BoundingBox2D(Convert.ToSAM(startPoint.Value), Convert.ToSAM(endPoint.Value));
            }
        }

        public Rect Rect
        {
            get
            {
                if(!IsActive())
                {
                    return Rect.Empty;
                }

                return new Rect(startPoint.Value, endPoint.Value);
            }
        }

        public SelectionType SelectionType
        {
            get
            {
                if(!IsActive())
                {
                    return SelectionType.Undefined;
                }

                if (rectangularSelectorMode != RectangularSelectorMode.Extended)
                {
                    return SelectionType.Inside;
                }

                double x1 = startPoint.Value.X;
                double x2 = endPoint.Value.X;

                return x1 > x2 ? SelectionType.InsideOrIntersect : SelectionType.Inside;
            }
        }

        private void Panel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            endPoint = null;

            lines.ForEach(x => panel.Children.Remove(x));
            lines.Clear();

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if(startPoint == null && !startPoint.HasValue)
                {
                    startPoint = e.GetPosition(panel);
                }
            }

            if (startPoint == null || !startPoint.HasValue)
            {
                return;
            }

            endPoint = e.GetPosition(panel);

            if (e.LeftButton == MouseButtonState.Released)
            {
                if (IsActive())
                {
                    Selected?.Invoke(this, new EventArgs());
                }

                startPoint = null;
                endPoint = null;

                return;
            }

            if (!IsActive())
            {
                return;
            }

            Selecting?.Invoke(this, new EventArgs());

            double x1 = startPoint.Value.X;
            double y1 = startPoint.Value.Y;
            double x2 = endPoint.Value.X;
            double y2 = endPoint.Value.Y;

            System.Windows.Media.DoubleCollection strokeDashArray_Temp = x1 > x2 ? strokeDashArray : null;
            if(rectangularSelectorMode != RectangularSelectorMode.Extended)
            {
                strokeDashArray_Temp = null;
            }

            Line line = null;

            line = new Line();
            line.Stroke = stroke;
            line.StrokeDashArray = strokeDashArray_Temp;
            line.StrokeThickness = strokeThickness;
            line.HorizontalAlignment = HorizontalAlignment.Stretch;
            line.VerticalAlignment = VerticalAlignment.Stretch;
            line.X1 = x1;
            line.Y1 = y2;
            line.X2 = x2;
            line.Y2 = y2;
            lines.Add(line);

            line = new Line();
            line.Stroke = stroke;
            line.StrokeDashArray = strokeDashArray_Temp;
            line.StrokeThickness = strokeThickness;
            line.HorizontalAlignment = HorizontalAlignment.Stretch;
            line.VerticalAlignment = VerticalAlignment.Stretch;
            line.X1 = x2;
            line.Y1 = y2;
            line.X2 = x2;
            line.Y2 = y1;
            lines.Add(line);

            line = new Line();
            line.Stroke = stroke;
            line.StrokeDashArray = strokeDashArray_Temp;
            line.StrokeThickness = strokeThickness;
            line.HorizontalAlignment = HorizontalAlignment.Stretch;
            line.VerticalAlignment = VerticalAlignment.Stretch;
            line.X1 = x2;
            line.Y1 = y1;
            line.X2 = x1;
            line.Y2 = y1;
            lines.Add(line);

            line = new Line();
            line.Stroke = stroke;
            line.StrokeDashArray = strokeDashArray_Temp;
            line.StrokeThickness = strokeThickness;
            line.HorizontalAlignment = HorizontalAlignment.Stretch;
            line.VerticalAlignment = VerticalAlignment.Stretch;
            line.X1 = x1;
            line.Y1 = y1;
            line.X2 = x1;
            line.Y2 = y2;
            lines.Add(line);

            lines.ForEach(x => panel.Children.Add(x));
        }
    }
}
