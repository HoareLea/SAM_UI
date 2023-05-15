using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class AnalyticalThreeDimensionalViewSettings : ThreeDimensionalViewSettings, IAnalyticalViewSettings
    {
        public SpaceAppearanceSettings SpaceAppearanceSettings { get; set; }

        public PanelAppearanceSettings PanelAppearanceSettings { get; set; }

        public ApertureAppearanceSettings ApertureAppearanceSettings { get; set; }

        public AnalyticalThreeDimensionalViewSettings(Guid guid, string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types)
            :base(guid, name, appearanceSettings, types)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(ThreeDimensionalViewSettings threeDimensionalViewSettings)
            : base(threeDimensionalViewSettings)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public AnalyticalThreeDimensionalViewSettings(AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
            :base(analyticalThreeDimensionalViewSettings)
        {
            if (analyticalThreeDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalThreeDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalThreeDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public AnalyticalThreeDimensionalViewSettings(string name, AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
            : base(name, analyticalThreeDimensionalViewSettings)
        {
            if (analyticalThreeDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalThreeDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalThreeDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public AnalyticalThreeDimensionalViewSettings(Guid guid, string name, AnalyticalThreeDimensionalViewSettings analyticalThreeDimensionalViewSettings)
            : base(guid, name, analyticalThreeDimensionalViewSettings)
        {
            if (analyticalThreeDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalThreeDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalThreeDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalThreeDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("SpaceAppearanceSettings"))
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(jObject.Value<JObject>("SpaceAppearanceSettings"));
            }

            if (jObject.ContainsKey("PanelAppearanceSettings"))
            {
                PanelAppearanceSettings = new PanelAppearanceSettings(jObject.Value<JObject>("PanelAppearanceSettings"));
            }

            if (jObject.ContainsKey("ApertureAppearanceSettings"))
            {
                ApertureAppearanceSettings = new ApertureAppearanceSettings(jObject.Value<JObject>("ApertureAppearanceSettings"));
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            if (SpaceAppearanceSettings != null)
            {
                jObject.Add("SpaceAppearanceSettings", SpaceAppearanceSettings.ToJObject());
            }

            if (PanelAppearanceSettings != null)
            {
                jObject.Add("PanelAppearanceSettings", PanelAppearanceSettings.ToJObject());
            }

            if (ApertureAppearanceSettings != null)
            {
                jObject.Add("ApertureAppearanceSettings", ApertureAppearanceSettings.ToJObject());
            }

            return jObject;
        }
    }
}
