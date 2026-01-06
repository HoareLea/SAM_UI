// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Newtonsoft.Json.Linq;
using SAM.Geometry.UI;

namespace SAM.Core.UI
{
    public abstract class RelationClusterAppearanceSettings : ValueAppearanceSettings
    {
        public RelationCluster RelationCluster { get; set; }

        public RelationClusterAppearanceSettings()
            :base()
        {

        }
        
        public RelationClusterAppearanceSettings(RelationClusterAppearanceSettings relationClusterAppearanceSettings)
            :base(relationClusterAppearanceSettings)
        {
            if(relationClusterAppearanceSettings != null)
            {
                RelationCluster = relationClusterAppearanceSettings.RelationCluster;
            }
        }

        public RelationClusterAppearanceSettings(JObject jObject)
            :base(jObject)
        {

        }
    }
}
