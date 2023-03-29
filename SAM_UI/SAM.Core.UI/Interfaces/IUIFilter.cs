namespace SAM.Core.UI
{
    public interface IUIFilter : IFilter
    {
        string Name { get; set; }
        System.Type Type { get; }
    }
}
