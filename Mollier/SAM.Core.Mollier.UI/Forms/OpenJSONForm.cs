using System;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Forms
{
    public partial class OpenJSONForm : Form
    {
        private bool replace = false;
        public OpenJSONForm()
        {
            InitializeComponent();
        }
        private void MergeButton_Click(object sender, EventArgs e)
        {
            replace = false;
            Close();
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            replace = true;
            Close();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True whether file should be replaced and false if it should be merged</returns>
        public bool ReplaceOrMerge()
        {
            return replace;
        }

        private void OpenJSONForm_Load(object sender, EventArgs e)
        {

        }
    }
}
