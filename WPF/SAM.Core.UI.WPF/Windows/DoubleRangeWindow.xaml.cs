// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Windows;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for DoubleRangeWindow.xaml
    /// </summary>
    public partial class DoubleRangeWindow : Window
    {
        public event RangeChangedEventHandler<double> RangeChanged;

        public DoubleRangeWindow(double max, double min)
        {
            InitializeComponent();

            Max = max;
            Min = min;

            doubleRangeControl.RangeChanged += DoubleRangeControl_RangeChanged;
        }

        private void DoubleRangeControl_RangeChanged(object sender, RangeChangedEventArgs<double> e)
        {
            RangeChanged?.Invoke(this, e);
        }

        public Range<double> Range
        {
            get
            {
                return doubleRangeControl.Range;
            }

            set
            {
                doubleRangeControl.Range = value;
            }
        }

        public double Max
        {
            get
            {
                return doubleRangeControl.Max;
            }

            set
            {
                doubleRangeControl.Max = value;
            }
        }

        public double Min
        {
            get
            {
                return doubleRangeControl.Min;
            }

            set
            {
                doubleRangeControl.Min = value;
            }
        }
    }
}
