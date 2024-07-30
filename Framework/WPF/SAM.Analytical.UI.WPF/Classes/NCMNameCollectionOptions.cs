namespace SAM.Analytical.UI.WPF
{
    public class NCMNameCollectionOptions
    {
        public bool Editable { get; set; } = false;

        public NCMNameCollectionOptions(bool editable = false)
        {
            Editable = editable;
        }
    }
}
