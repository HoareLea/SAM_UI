using Newtonsoft.Json.Linq;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using SAM.Geometry.UI;

namespace SAM.Analytical.UI
{
    public class AnalyticalTwoDimensionalViewSettings : TwoDimensionalViewSettings, IAnalyticalViewSettings
    {
        public SpaceAppearanceSettings SpaceAppearanceSettings { get; set; }
        
        public PanelAppearanceSettings PanelAppearanceSettings { get; set; }

        public ApertureAppearanceSettings ApertureAppearanceSettings { get; set; }

        public AnalyticalTwoDimensionalViewSettings(Guid guid, string name, Plane plane, AppearanceSettings appearanceSettings, IEnumerable<Type> types, TextAppearance textAppearance)
            :base(guid, name, plane, appearanceSettings, types, textAppearance)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(Plane plane, string name, AppearanceSettings appearanceSettings, IEnumerable<Type> types, TextAppearance textAppearance)
            : base(Guid.NewGuid(), name, plane, appearanceSettings, types, textAppearance)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(TwoDimensionalViewSettings twoDimensionalViewSettings)
            : base(twoDimensionalViewSettings)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(JObject jObject)
            : base(jObject)
        {

        }

        public AnalyticalTwoDimensionalViewSettings(AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
            :base(analyticalTwoDimensionalViewSettings)
        {
            if (analyticalTwoDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalTwoDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalTwoDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public AnalyticalTwoDimensionalViewSettings(string name, AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
            : base(name, analyticalTwoDimensionalViewSettings)
        {
            if (analyticalTwoDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalTwoDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalTwoDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public AnalyticalTwoDimensionalViewSettings(Guid guid, string name, AnalyticalTwoDimensionalViewSettings analyticalTwoDimensionalViewSettings)
            : base(guid, name, analyticalTwoDimensionalViewSettings)
        {
            if (analyticalTwoDimensionalViewSettings?.SpaceAppearanceSettings != null)
            {
                SpaceAppearanceSettings = new SpaceAppearanceSettings(analyticalTwoDimensionalViewSettings.SpaceAppearanceSettings);
                PanelAppearanceSettings = new PanelAppearanceSettings(analyticalTwoDimensionalViewSettings.PanelAppearanceSettings);
                ApertureAppearanceSettings = new ApertureAppearanceSettings(analyticalTwoDimensionalViewSettings.ApertureAppearanceSettings);
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("SpaceAppearanceSettings"))
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

            if(SpaceAppearanceSettings != null)
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
