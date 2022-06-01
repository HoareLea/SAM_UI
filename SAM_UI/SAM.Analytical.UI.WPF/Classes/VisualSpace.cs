namespace SAM.Analytical.UI.WPF
{
    public class VisualSpace : VisualSAMObject<Space>
    {
        public VisualSpace(Space space)
            :base(space)
        {

        }

        public Space Space
        {
            get
            {
                return jSAMObject;
            }
        }
    }
}
