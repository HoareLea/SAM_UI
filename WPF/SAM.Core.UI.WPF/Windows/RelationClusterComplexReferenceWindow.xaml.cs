using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Interaction logic for RelationClusterComplexReferenceWindow.xaml
    /// </summary>
    public partial class RelationClusterComplexReferenceWindow : Window
    {
        public RelationClusterComplexReferenceWindow()
        {
            InitializeComponent();
        }

        public RelationCluster RelationCluster
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.RelationCluster;
            }

            set
            {
                RelationClusterComplexReferenceControl_Main.RelationCluster = value;
            }
        }
    }
}
