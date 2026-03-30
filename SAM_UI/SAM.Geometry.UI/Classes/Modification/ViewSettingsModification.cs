// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.UI;
using System.Collections.Generic;

namespace SAM.Geometry.UI
{
    public class ViewSettingsModification : Modification
    {
        public List<IViewSettings> ViewSettings { get; }

        public bool UpdateGeometry { get; } = false;

        public bool UpdateCamera { get; } = false;

        public ViewSettingsModification(IViewSettings viewSettings)
        {
            if(viewSettings != null)
            {
                ViewSettings = [viewSettings];
            }
        }

        public ViewSettingsModification(IViewSettings viewSettings, bool updateGeometry)
        {
            if (viewSettings != null)
            {
                ViewSettings = [viewSettings];
            }

            UpdateGeometry = updateGeometry;
        }

        public ViewSettingsModification(IViewSettings viewSettings, bool updateGeometry, bool updateCamera)
        {
            if (viewSettings != null)
            {
                ViewSettings = [viewSettings];
            }

            UpdateGeometry = updateGeometry;
            UpdateCamera = updateCamera;
        }

        public ViewSettingsModification(IEnumerable<IViewSettings> viewSettings)
        {
            ViewSettings = viewSettings == null ? null : [.. viewSettings];
        }

        public ViewSettingsModification(IEnumerable<IViewSettings> viewSettings, bool updateGeometry)
        {
            ViewSettings = viewSettings == null ? null : [.. viewSettings];

            UpdateGeometry = updateGeometry;
        }
    }
}
