using System.Windows;

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

        public IComplexReference ComplexReference
        {
            get
            {
                return RelationClusterComplexReferenceControl_Main.ComplexReference;
            }
        }
    }
}
