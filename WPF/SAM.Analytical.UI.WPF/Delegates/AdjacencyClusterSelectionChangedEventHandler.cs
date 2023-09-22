using SAM.Core;

namespace SAM.Analytical.UI.WPF
{
    public delegate void AdjacencyClusterSelectionChangedEventHandler<T>(object sender, AdjacencyClusterSelectionChangedEventArgs<T> e) where T: SAMObject;
}
