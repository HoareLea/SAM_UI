// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for ApertureWindow.xaml
    /// </summary>
    public partial class ApertureWindow : System.Windows.Window
    {
        public ApertureWindow()
        {
            InitializeComponent();
        }

        public ApertureWindow(IEnumerable<Aperture> apertures)
        {
            InitializeComponent();

            Apertures = apertures == null ? null : new List<Aperture>(apertures);
        }

        public List<Aperture> Apertures
        {
            get
            {
                return apertureControl.Apertures;
            }

            set
            {
                apertureControl.Apertures = value;
            }
        }

        private void button_OK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Aperture> apertures = Apertures;
            if(apertures != null)
            {
                foreach (Aperture aperture in apertures)
                {
                    if(aperture == null || !aperture.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperies) || openingProperies == null)
                    {
                        continue;
                    }

                    double dischargeCoefficient = openingProperies.GetDischargeCoefficient();
                    if(double.IsNaN(dischargeCoefficient) || dischargeCoefficient <= 0 || dischargeCoefficient > 1)
                    {
                        MessageBox.Show("Discharge coefficient value has to be greater than 0 and less than 1");
                        return;
                    }
                }
            }


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
