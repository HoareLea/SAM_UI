using System;
using System.Collections.Generic;
using System.Windows;

namespace SAM.Geometry.UI.WPF
{
    /// <summary>
    /// Interaction logic for GeometryWindow.xaml
    /// </summary>
    public partial class GeometryWindow : Window
    {
        public GeometryWindow()
        {
            InitializeComponent();
        }

        public ViewControl ViewControl
        {
            get
            {
                return viewControl;
            }
        }
    }
}
