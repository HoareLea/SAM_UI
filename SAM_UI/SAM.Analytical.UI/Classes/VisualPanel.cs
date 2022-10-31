using SAM.Geometry.Spatial;

namespace SAM.Analytical.UI
{
    public class VisualPanel : SAMGeometry3DObjectCollection
    {
        private Panel panel;

        public VisualPanel(Panel panel)
        {
            this.panel = panel;
        }
    }
}
