// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows.Forms;
using System.Drawing;

namespace SAM.Core.UI
{
    public class HooverTimer
    {
        public event MouseEventHandler Update;

        private Timer timer = new Timer();
        private Control control;

        private double maxDistance = 4;

        private Point point;
        private MouseEventArgs mouseEventArgs;

        public HooverTimer(Control control, int interval)
            :base()
        {
            timer.Tick += Timer_Tick;
            Control = control;
            timer.Interval = interval;
        }

        public int Interval
        {
            get
            {
                return timer.Interval;
            }

            set
            {
                timer.Interval = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return timer.Enabled;
            }

            set
            {
                timer.Enabled = value;
            }
        }

        public Control Control
        {
            get
            {
                return control;
            }

            set
            {
                if(control != null)
                {
                    control.MouseMove -= Control_MouseMove;
                    control.MouseLeave -= Control_MouseLeave;
                    control.MouseEnter -= Control_MouseEnter;
                }

                control = value;

                if(control != null)
                {
                    control.MouseMove += Control_MouseMove;
                    control.MouseLeave += Control_MouseLeave;
                    control.MouseEnter += Control_MouseEnter;
                }
            }
        }

        private void Timer_Tick(object sender, System.EventArgs e)
        {
            if(mouseEventArgs == null)
            {
                return;
            }

            Point point_Temp = mouseEventArgs.Location;
            if(point == Point.Empty)
            {
                point = point_Temp;
                return;
            }

            int x = point_Temp.X - point.X;
            int y = point_Temp.Y - point.Y;

            double distance = System.Math.Sqrt((x * x) + (y * y));
            if (distance > maxDistance)
            {
                point = point_Temp;
                return;
            }

            Update?.Invoke(control, mouseEventArgs); 
            timer.Stop();
            timer.Enabled = false;
        }

        private void Control_MouseEnter(object sender, System.EventArgs e)
        {
            timer.Enabled = true;
            timer.Start();

            point = Point.Empty;
        }

        private void Control_MouseLeave(object sender, System.EventArgs e)
        {
            timer.Stop();
            timer.Enabled = false;

            point = Point.Empty;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            mouseEventArgs = e;

            if (!timer.Enabled)
            {
                timer.Enabled = true;
                timer.Start();
            }

            if(point.IsEmpty)
            {
                point = e.Location;
            }
        }
    }
}
