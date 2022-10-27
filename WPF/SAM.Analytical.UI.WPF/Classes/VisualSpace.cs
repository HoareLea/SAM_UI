using SAM.Core.UI.WPF;

namespace SAM.Analytical.UI.WPF
{
    public class VisualSpace : VisualJSAMObject<Space>
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
