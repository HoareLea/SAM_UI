namespace SAM.Core.Mollier.UI
{
    public interface IUserVisibilitySetting : IVisibilitySetting
    {
        ChartDataType ChartDataType { get; }
        ChartParameterType ChartParameterType { get; }
    }
}
