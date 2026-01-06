using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ViewSettingsModification : Modification
    {
        public List<IViewSettings> ViewSettings { get; }

        public bool UpdateGeometry { get; } = false;

        public ViewSettingsModification(IViewSettings viewSettings)
        {
            if(viewSettings != null)
            {
                ViewSettings = new List<IViewSettings>() { viewSettings };
            }
        }

        public ViewSettingsModification(IViewSettings viewSettings, bool updateGeometry)
        {
            if (viewSettings != null)
            {
                ViewSettings = new List<IViewSettings>() { viewSettings };
            }

            UpdateGeometry = updateGeometry;
        }

        public ViewSettingsModification(IEnumerable<IViewSettings> viewSettings)
        {
            ViewSettings = viewSettings == null ? null : new List<IViewSettings>(viewSettings);
        }

        public ViewSettingsModification(IEnumerable<IViewSettings> viewSettings, bool updateGeometry)
        {
            ViewSettings = viewSettings == null ? null : new List<IViewSettings>(viewSettings);

            UpdateGeometry = updateGeometry;
        }
    }
}
