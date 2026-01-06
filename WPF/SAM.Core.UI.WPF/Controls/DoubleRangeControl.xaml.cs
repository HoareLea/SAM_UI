// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for DoubleRangeControl.xaml
    /// </summary>
    public partial class DoubleRangeControl : UserControl
    {
        public event RangeChangedEventHandler<double> RangeChanged;

        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer() { Interval = new TimeSpan(0, 0, 2) };

        public DoubleRangeControl()
        {
            dispatcherTimer.Tick += DispatcherTimer_Tick;

            InitializeComponent();

            slider_1.ValueChanged += slider_ValueChanged;
            slider_2.ValueChanged += slider_ValueChanged;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            RangeChanged?.Invoke(this, new RangeChangedEventArgs<double>(Range));
        }

        public double Max
        {
            get
            {
                return GetMax();
            }
            set
            {
                SetMax(value);
            }

        }

        public double Min
        {
            get
            {
                return GetMin();
            }
            set
            {
                SetMin(value);
            }

        }

        public Range<double> Range
        {
            get
            {
                return GetRange();
            }

            set
            {
                SetRange(value);
            }
        }

        private double GetMax()
        {
            return Math.Max(slider_1.Maximum, slider_2.Maximum);
        }

        private void SetMax(double value)
        {
            slider_1.Maximum = value;
            slider_2.Maximum = value;
        }

        private double GetMin()
        {
            return Math.Min(slider_1.Minimum, slider_2.Minimum);
        }

        private void SetMin(double value)
        {
            slider_1.Minimum = value;
            slider_2.Minimum = value;
        }

        private Range<double> GetRange()
        {
            return new Range<double>(slider_1.Value, slider_2.Value);
        }

        private void SetRange(Range<double> range)
        {
            if(range == null)
            {
                return;
            }

            slider_1.ValueChanged -= slider_ValueChanged;
            slider_2.ValueChanged -= slider_ValueChanged;

            if (slider_1.Value > slider_2.Value)
            {
                slider_1.Value = range.Max;
                slider_2.Value = range.Min;
            }
            else
            {
                slider_1.Value = range.Min;
                slider_2.Value = range.Max;
            }

            slider_1.ValueChanged += slider_ValueChanged;
            slider_2.ValueChanged += slider_ValueChanged;
        }

        private void slider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            dispatcherTimer.Start();
        }
    }
}
