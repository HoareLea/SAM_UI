using System.Windows.Forms.DataVisualization.Charting;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static void SaveAsPDF(this Chart mollierChart, int fontSize, string size, int newWidth, int newHeight, int chartWidth, int chartHeight)
        {
            mollierChart.Visible = false;
            Document doc = size == "A3" ? new Document(iTextSharp.text.PageSize.A3, 0, 0, 0, 0) : new Document(iTextSharp.text.PageSize.A4, 0, 0, 0, 0);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                mollierChart.Serializer.Save(memoryStream);

                Chart chart_new = new Chart();
                chart_new.Serializer.Load(memoryStream);
            }

            mollierChart.Visible = false;

            mollierChart.Width = newWidth;
            mollierChart.Height = newHeight;


            //labels
            mollierChart.ChartAreas[0].AxisX.LabelAutoFitMinFontSize = fontSize;
            mollierChart.ChartAreas[0].AxisY.LabelAutoFitMinFontSize = fontSize;
            mollierChart.ChartAreas[0].AxisX2.LabelAutoFitMinFontSize = fontSize;
            mollierChart.ChartAreas[0].AxisY2.LabelAutoFitMinFontSize = fontSize;
            mollierChart.ChartAreas[2].AxisX2.LabelStyle.Font = new System.Drawing.Font("Arial", fontSize);
            mollierChart.ChartAreas[2].AxisY.LabelStyle.Font = new System.Drawing.Font("Arial", fontSize);
            //titles
            mollierChart.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", fontSize);
            mollierChart.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", fontSize);
            mollierChart.ChartAreas[2].AxisX2.TitleFont = new System.Drawing.Font("Arial", fontSize);
            mollierChart.ChartAreas[2].AxisY.TitleFont = new System.Drawing.Font("Arial", fontSize);
            mollierChart.ChartAreas[0].AxisY2.TitleFont = new System.Drawing.Font("Arial", fontSize);
            foreach (Series series in mollierChart.Series)
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
                    series.Points[0].YValues[0] = mollierChart.ChartAreas[0].AxisY.Maximum > 0.1 ? series.Points[0].YValues[0] + 5 : series.Points[0].YValues[0];
                }
                if (series.Tag == "ColorPointLabelSquare")
                {
                    series.MarkerSize = size == "A3" ? 40 : 30;
                }
            }
            doc.Open();
            var chartimagepath = new MemoryStream();

            mollierChart.AntiAliasing = AntiAliasingStyles.All;
            mollierChart.SaveImage(chartimagepath, ChartImageFormat.Tiff);
            var Chart_Image = Image.GetInstance(chartimagepath.GetBuffer());

            Chart_Image.Rotation = (float)(System.Math.PI / 2);
            Chart_Image.ScalePercent(20f);
            Chart_Image.SetAbsolutePosition(0, -10);//set the position of Image

            //RETURN CHART TO DEFAULT VALUES

            mollierChart.Width = chartWidth;
            mollierChart.Height = chartHeight;
            foreach (Series series in mollierChart.Series)
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
                    series.Points[0].YValues[0] = mollierChart.ChartAreas[0].AxisY.Maximum > 0.1 ? series.Points[0].YValues[0] - 5 : series.Points[0].YValues[0];
                }
                if (series.Tag == "ColorPointLabelSquare")
                {
                    series.MarkerSize = 15;
                }

            }
            //titles
            mollierChart.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", 8);
            mollierChart.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", 8);
            mollierChart.ChartAreas[2].AxisX2.TitleFont = new System.Drawing.Font("Arial", 8);
            mollierChart.ChartAreas[2].AxisY.TitleFont = new System.Drawing.Font("Arial", 8);
            mollierChart.ChartAreas[0].AxisY2.TitleFont = new System.Drawing.Font("Arial", 8);
            //labels
            mollierChart.ChartAreas[0].AxisX.LabelAutoFitMinFontSize = 6;
            mollierChart.ChartAreas[0].AxisY.LabelAutoFitMinFontSize = 6;
            mollierChart.ChartAreas[0].AxisX2.LabelAutoFitMinFontSize = 6;
            mollierChart.ChartAreas[0].AxisY2.LabelAutoFitMinFontSize = 6;
            mollierChart.ChartAreas[2].AxisX2.LabelStyle.Font = new System.Drawing.Font("Arial", 8);
            mollierChart.ChartAreas[2].AxisY.LabelStyle.Font = new System.Drawing.Font("Arial", 8);
            mollierChart.Visible = true;
            doc.Add(Chart_Image);
            mollierChart.Visible = true;
            doc.Close();
        }
    }
}

