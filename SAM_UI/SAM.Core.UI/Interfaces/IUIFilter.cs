namespace SAM.Core.UI
{
    public interface IUIFilter : IFilter
    {
        string Name { get; }
        System.Type Type { get; }
    }
}
