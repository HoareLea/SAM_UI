using SAM.Geometry;
using SAM.Geometry.Spatial;
using System;

namespace SAM.Analytical.UI
{
    public class PanelSection3D : SAMGeometry3DObjectCollection, ISection3DObject
    {
        private Panel panel;

        public PanelSection3D(Panel panel)
        {
            this.panel = panel;
        }
    }
}
