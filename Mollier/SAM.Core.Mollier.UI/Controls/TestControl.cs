using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class TestControl : UserControl
    {
        public TestControl()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("WORKS");
        }
    }
}
