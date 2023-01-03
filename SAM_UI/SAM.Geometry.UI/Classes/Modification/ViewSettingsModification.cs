using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ViewSettingsModification : Modification
    {
        public List<IViewSettings> ViewSettings { get; }

        public ViewSettingsModification(IViewSettings viewSettings)
        {
            if(viewSettings != null)
            {
                ViewSettings = new List<IViewSettings>() { viewSettings };
            }
        }

        public ViewSettingsModification(IEnumerable<IViewSettings> viewSettings)
        {
            ViewSettings = viewSettings == null ? null : new List<IViewSettings>(viewSettings);
        }
    }
}
