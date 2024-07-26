﻿using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for MapInternalConditionsWindow.xaml
    /// </summary>
    public partial class MapInternalConditionsWindow : System.Windows.Window
    {
        public MapInternalConditionsWindow()
        {
            InitializeComponent();
        }

        public MapInternalConditionsWindow(IEnumerable<Space> spaces, TextMap textMap = null, InternalConditionLibrary internalConditionLibrary = null)
        {
            InitializeComponent();

            mapInternalConditionsControl.TextMap = textMap;
            mapInternalConditionsControl.InternalConditionLibrary = internalConditionLibrary;
            mapInternalConditionsControl.Spaces = spaces == null ? null : new List<Space>(spaces);
        }

        public List<Space> Spaces
        {
            get
            {
                return mapInternalConditionsControl.Spaces;
            }
        }

        public List<Space> GetSpaces(bool selected = false)
        {
            return mapInternalConditionsControl.GetSpaces(selected);
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void button_Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
