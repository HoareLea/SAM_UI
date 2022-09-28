using SAM.Core.Mollier.UI.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI.Controls
{
    public partial class ListPointsOptionControl : UserControl
    {
        private MollierPoint mollierPoint;
        private PointListOptionForm parent;
        private string label;
        public ListPointsOptionControl()
        {
            InitializeComponent();
        }

        public ListPointsOptionControl(MollierPoint mollierPoint, string label, PointListOptionForm pointListOptionForm)
        {
            InitializeComponent();
            parent = pointListOptionForm;
            this.mollierPoint = mollierPoint;
            this.label = label;
            if(label != null && label != "")
            {
                nameLabel.Text = label;
            }
        }

    
        private void OK_Button_Click(object sender, EventArgs e)
        {
            parent.ChosenPoint(mollierPoint);
        }
    }
}
