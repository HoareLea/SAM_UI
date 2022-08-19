using System.Windows.Forms.DataVisualization.Charting;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static void SaveAsPDF(Chart MollierChart, string path, int fontSize, string size, int newWidth, int newHeight, int chartWidth, int chartHeight)
        {
            MollierChart.Visible = false;
            Document doc = size == "A3" ? new Document(iTextSharp.text.PageSize.A3, 0, 0, 0, 0) : new Document(iTextSharp.text.PageSize.A4, 0, 0, 0, 0);
            PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                MollierChart.Serializer.Save(memoryStream);

                Chart chart_new = new Chart();
                chart_new.Serializer.Load(memoryStream);
            }

            MollierChart.Visible = false;

            MollierChart.Width = newWidth;
            MollierChart.Height = newHeight;


            //labels
            MollierChart.ChartAreas[0].AxisX.LabelAutoFitMinFontSize = fontSize;
            MollierChart.ChartAreas[0].AxisY.LabelAutoFitMinFontSize = fontSize;
            MollierChart.ChartAreas[0].AxisX2.LabelAutoFitMinFontSize = fontSize;
            MollierChart.ChartAreas[0].AxisY2.LabelAutoFitMinFontSize = fontSize;
            MollierChart.ChartAreas[2].AxisX2.LabelStyle.Font = new System.Drawing.Font("Arial", fontSize);
            MollierChart.ChartAreas[2].AxisY.LabelStyle.Font = new System.Drawing.Font("Arial", fontSize);
            //titles
            MollierChart.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", fontSize);
            MollierChart.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", fontSize);
            MollierChart.ChartAreas[2].AxisX2.TitleFont = new System.Drawing.Font("Arial", fontSize);
            MollierChart.ChartAreas[2].AxisY.TitleFont = new System.Drawing.Font("Arial", fontSize);
            MollierChart.ChartAreas[0].AxisY2.TitleFont = new System.Drawing.Font("Arial", fontSize);
            foreach (Series series in MollierChart.Series)
            {
                series.Font = new System.Drawing.Font("Arial", fontSize);//change font to make it more visibility
                if (series.Tag is MollierProcess || series.Tag == "ColorPoint")
                {
                    series.BorderWidth = size == "A3" ? 15 : 10;
                    series.MarkerSize = size == "A3" ? 28 : 22;
                    series.MarkerBorderWidth = size == "A3" ? 8 : 6;
                }
                if (series.Tag == "dashLine")
                {
                    series.BorderWidth = size == "A3" ? 6 : 4;
                }
                if (series.Tag == "SecondPoint")
                {
                    series.MarkerSize = size == "A3" ? 15 : 10;
                }
                if (series.Tag == "ColorPointLabel")
                {
                    series.Points[0].YValues[0] = MollierChart.ChartAreas[0].AxisY.Maximum > 0.1 ? series.Points[0].YValues[0] + 5 : series.Points[0].YValues[0];
                }
                if (series.Tag == "ColorPointLabelSquare")
                {
                    series.MarkerSize = size == "A3" ? 40 : 30;
                }
            }
            doc.Open();
            var chartimagepath = new MemoryStream();

            MollierChart.AntiAliasing = AntiAliasingStyles.All;
            MollierChart.SaveImage(chartimagepath, ChartImageFormat.Tiff);
            var Chart_Image = Image.GetInstance(chartimagepath.GetBuffer());

            Chart_Image.Rotation = (float)(System.Math.PI / 2);
            Chart_Image.ScalePercent(20f);
            Chart_Image.SetAbsolutePosition(0, -10);//set the position of Image

            //RETURN CHART TO DEFAULT VALUES

            MollierChart.Width = chartWidth;
            MollierChart.Height = chartHeight;
            foreach (Series series in MollierChart.Series)
            {
                series.Font = new System.Drawing.Font("Arial", 8);//change font to make it more visibility
                if (series.Tag is MollierProcess || series.Tag == "ColorPoint")
                {
                    series.MarkerSize = 8;
                    series.MarkerBorderWidth = 2;
                    series.BorderWidth = 5;
                }
                if (series.Tag == "dashLine")
                {
                    series.BorderWidth = 3;
                }
                if (series.Tag == "SecondPoint")
                {
                    series.MarkerSize = 5;
                }
                if (series.Tag == "ColorPointLabel")
                {
                    series.Points[0].YValues[0] = MollierChart.ChartAreas[0].AxisY.Maximum > 0.1 ? series.Points[0].YValues[0] - 5 : series.Points[0].YValues[0];
                }
                if (series.Tag == "ColorPointLabelSquare")
                {
                    series.MarkerSize = 15;
                }

            }
            //titles
            MollierChart.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", 8);
            MollierChart.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", 8);
            MollierChart.ChartAreas[2].AxisX2.TitleFont = new System.Drawing.Font("Arial", 8);
            MollierChart.ChartAreas[2].AxisY.TitleFont = new System.Drawing.Font("Arial", 8);
            MollierChart.ChartAreas[0].AxisY2.TitleFont = new System.Drawing.Font("Arial", 8);
            //labels
            MollierChart.ChartAreas[0].AxisX.LabelAutoFitMinFontSize = 6;
            MollierChart.ChartAreas[0].AxisY.LabelAutoFitMinFontSize = 6;
            MollierChart.ChartAreas[0].AxisX2.LabelAutoFitMinFontSize = 6;
            MollierChart.ChartAreas[0].AxisY2.LabelAutoFitMinFontSize = 6;
            MollierChart.ChartAreas[2].AxisX2.LabelStyle.Font = new System.Drawing.Font("Arial", 8);
            MollierChart.ChartAreas[2].AxisY.LabelStyle.Font = new System.Drawing.Font("Arial", 8);
            MollierChart.Visible = true;
            doc.Add(Chart_Image);
            MollierChart.Visible = true;
            doc.Close();
        }
    }
}

