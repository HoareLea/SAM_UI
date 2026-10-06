// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// Interaction logic for PartFVentilationControl.xaml
    /// </summary>
    public partial class PartFVentilationControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartFVentilationControl"/> class.
        /// </summary>
        public PartFVentilationControl()
        {
            InitializeComponent();

            SetbackFlowRateFactor = PartFData.DefaultSetbackFlowRateFactor;
        }

        /// <summary>
        /// Gets the selected zone category. Null or empty means the complete model is one dwelling,
        /// which is the normal single house workflow and is not a problem.
        /// </summary>
        public string? SelectedZoneCategory
        {
            get
            {
                return comboBox_ZoneCategory.SelectedItem as string;
            }
        }

        /// <summary>
        /// Gets or sets the setback operating factor: setback flow rate = continuous design flow rate x factor.
        /// <para>
        /// An unparseable or out-of-range entry reads back as
        /// <see cref="PartFData.DefaultSetbackFlowRateFactor"/> rather than as a value that would
        /// produce a setback rate above the continuous design rate, or one that is not a number. The calculator
        /// validates it again, so a bad entry cannot reach the calculation either way.
        /// </para>
        /// </summary>
        public double SetbackFlowRateFactor
        {
            get
            {
                if (!double.TryParse(textBox_SetbackFlowRateFactor.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                    && !double.TryParse(textBox_SetbackFlowRateFactor.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                {
                    return PartFData.DefaultSetbackFlowRateFactor;
                }

                return PartFData.IsValidSetbackFlowRateFactor(result) ? result : PartFData.DefaultSetbackFlowRateFactor;
            }

            set
            {
                double value_Temp = PartFData.IsValidSetbackFlowRateFactor(value) ? value : PartFData.DefaultSetbackFlowRateFactor;

                textBox_SetbackFlowRateFactor.Text = value_Temp.ToString(CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Gets or sets the list of zone categories. A blank entry is included first so the user can
        /// choose single house mode explicitly rather than having a category forced on them.
        /// </summary>
        public List<string>? ZoneCategories
        {
            get
            {
                List<string> result = [];
                foreach (object @object in comboBox_ZoneCategory.Items)
                {
                    if (@object is string string_Item)
                    {
                        result.Add(string_Item);
                    }
                }

                return result;
            }
            set
            {
                string? value_Temp = comboBox_ZoneCategory.SelectedItem as string;
                comboBox_ZoneCategory.Items.Clear();

                //Single house mode has to be reachable from the dialog. Without a blank entry the first
                //category was selected automatically, so a single house model was silently sized per zone.
                comboBox_ZoneCategory.Items.Add(string.Empty);

                if (value != null)
                {
                    foreach (string value_New in value)
                    {
                        if (!string.IsNullOrWhiteSpace(value_New))
                        {
                            comboBox_ZoneCategory.Items.Add(value_New);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(value_Temp))
                {
                    comboBox_ZoneCategory.SelectedItem = value_Temp;
                }
                else if (comboBox_ZoneCategory.Items.Count != 0)
                {
                    comboBox_ZoneCategory.SelectedItem = comboBox_ZoneCategory.Items[0];
                }
            }
        }
    }
}
