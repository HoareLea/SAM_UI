using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Aspose.Imaging;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;

namespace SAM.Core.Mollier.UI
{
    public static class ExportPDF
    {
        public static bool Export(this Chart chart, Control control, ChartExportType chartExportType, MollierControlSettings mollierControlSettings, string path = "")
        {
            //string pageType = string.Format("{0}_{1}", pageSize, pageOrientation);

            if (string.IsNullOrEmpty(path))
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    string name = mollierControlSettings.ChartType == ChartType.Mollier ? "Mollier" : "Psychrometric";
                    switch (chartExportType)
                    {
                        case ChartExportType.PDF:
                            saveFileDialog.Filter = "PDF document (*.pdf)|*.pdf|All files (*.*)|*.*";
                            //name += "_" + pageType;
                            break;
                        case ChartExportType.JPG:
                            saveFileDialog.Filter = "JPG document (*.jpg)|*.jpg|All files (*.*)|*.*";
                            break;
                        case ChartExportType.EMF:
                            saveFileDialog.Filter = "EMF document (*.emf)|*.emf|All files (*.*)|*.*";
                            break;
                    }
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;
                    saveFileDialog.FileName = name;
                    if (saveFileDialog.ShowDialog(control) != DialogResult.OK)
                    {
                        return false;
                    }
                    path = saveFileDialog.FileName;
                }
            }
            if (chartExportType == ChartExportType.JPG)
            {
                chart.SaveImage(path, ChartImageFormat.Jpeg);
            }
            if (chartExportType == ChartExportType.EMF)
            {
                chart.SaveImage(path, ChartImageFormat.Emf);
            }

            string path2 = "F:\\EMFtest";
            var exportPDF = System.IO.Path.Combine(path2, "Document.pdf");
            using (var writer = new PdfWriter(exportPDF))
            {
                using(var pdf = new PdfDocument(writer))
                {
                    var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                    //Image
                    //VectorImage vectorImage = VectorImage.Load(path2);


                    //ImageData imageData = ImageDataFactory.Create(path2);

                    //var image = new iText.Layout.Element.Image(imageData);

                    //doc.Add(image);
                }
            }


            return true;
        }
    }
}
