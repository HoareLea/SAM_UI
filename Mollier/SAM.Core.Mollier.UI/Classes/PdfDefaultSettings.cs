using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Mollier.UI
{
    public class PdfDefaultSettings : IJSAMObject
    {
        public int A4Width { get; set; } = 297;
        public int A4Height { get; set; } = 210;
        public int A3Width { get; set; } = 420;
        public int A3Height { get; set; } = 297;
        public int chartWidth { get; set; } = 1512;
        public int chartHeight { get; set; } = 728;

        public PdfDefaultSettings()
        {

        }
        public PdfDefaultSettings (PdfDefaultSettings pdfDefaultSettings)
        {
            A4Width = pdfDefaultSettings.A4Width;
            A4Height = pdfDefaultSettings.A4Height;
            A3Width = pdfDefaultSettings.A3Width;
            A3Height = pdfDefaultSettings.A3Height;
            chartWidth = pdfDefaultSettings.chartWidth;
            chartHeight = pdfDefaultSettings.chartHeight;
        }
        public PdfDefaultSettings(JObject jObject)
        {
            FromJObject(jObject);
        }
        public bool FromJObject(JObject jObject)
        {
            if (jObject.ContainsKey("A4Width"))
            {
                A4Width = jObject.Value<int>("A4Width");
            }
            if (jObject.ContainsKey("A4Height"))
            {
                A4Height = jObject.Value<int>("A4Height");
            }
            if (jObject.ContainsKey("A3Width"))
            {
                A3Width = jObject.Value<int>("A3Width");
            }
            if (jObject.ContainsKey("A3Height"))
            {
                A3Height = jObject.Value<int>("A3Height");
            }
            if (jObject.ContainsKey("chartWidth"))
            {
                chartWidth = jObject.Value<int>("chartWidth");
            }
            if (jObject.ContainsKey("chartHeight"))
            {
                chartHeight = jObject.Value<int>("chartHeight");
            }
            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();

            if (!double.IsNaN(A4Width))
            {
                jObject.Add("A4Width", A4Width);
            }
            if (!double.IsNaN(A4Height))
            {
                jObject.Add("A4Height", A4Height);
            }
            if (!double.IsNaN(A3Width))
            {
                jObject.Add("A3Width", A3Width);
            }
            if (!double.IsNaN(A3Height))
            {
                jObject.Add("A3Height", A3Height);
            }
            if (!double.IsNaN(chartWidth))
            {
                jObject.Add("chartWidth", chartWidth);
            }
            if (!double.IsNaN(chartHeight))
            {
                jObject.Add("chartHeight", chartHeight);
            }

            return jObject;
        }
    }
}
