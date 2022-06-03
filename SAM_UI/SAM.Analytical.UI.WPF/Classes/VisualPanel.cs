using System.Windows.Media;

namespace SAM.Analytical.UI.WPF
{
    public class VisualPanel : VisualJSAMObject<Panel>
    {
        public VisualPanel(Panel panel)
            :base(panel)
        {

        }

        public Panel Panel
        {
            get
            {
                return jSAMObject;
            }
        }

        public override bool SetHighlight(bool highlight)
        {
            if (Children.Count != 0)
            {
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    if (Children[i] is VisualEdges)
                    {
                        Children.RemoveAt(i);
                    }
                }
            }

            if (highlight)
            {
                VisualEdges visualEdges = jSAMObject?.GetFace3D(true)?.ToMedia3D_VisualEdges(Color.FromRgb(0, 0, 255), 0.01);

                if (visualEdges != null)
                {
                    Children.Add(visualEdges);
                }
            }

            return base.SetHighlight(highlight);
        }
    }
}
