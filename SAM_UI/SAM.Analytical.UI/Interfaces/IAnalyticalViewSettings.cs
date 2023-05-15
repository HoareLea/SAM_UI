namespace SAM.Analytical.UI
{
    public interface IAnalyticalViewSettings : Geometry.UI.IViewSettings
    {
        SpaceAppearanceSettings SpaceAppearanceSettings { get; set; }
        PanelAppearanceSettings PanelAppearanceSettings { get; set; }
        ApertureAppearanceSettings ApertureAppearanceSettings { get; set; }
    }
}
