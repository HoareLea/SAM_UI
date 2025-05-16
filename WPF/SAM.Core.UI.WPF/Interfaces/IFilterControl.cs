namespace SAM.Core.UI.WPF
{
    public interface IFilterControl
    {
        IUIFilter UIFilter { get; }

        event FilterChangedEventHandler FilterChanged;

        event FilterRemovingEventHandler FilterRemoving;
    }
}
