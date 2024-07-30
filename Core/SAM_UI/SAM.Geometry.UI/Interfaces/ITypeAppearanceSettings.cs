namespace SAM.Geometry.UI
{
    public interface ITypeAppearanceSettings : Core.UI.IAppearanceSettings
    {
        Z GetValueAppearanceSettings<Z>() where Z : ValueAppearanceSettings;
    }
}
