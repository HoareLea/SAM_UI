using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Create
    {
        public static GeometryModel3D GeometryModel3D_Text(string text, Brush brush, bool isDoubleSided, double height, Point3D basePoint, bool isBasePointCenterPoint, Vector3D vectorOver, Vector3D vectorUp, string fontFamilyName = "Arial")
        {
            // First we need a textbox containing the text of our label
            TextBlock textBlock = new TextBlock(new Run(text));
            textBlock.Foreground = brush; // setting the text color
            textBlock.FontFamily = new FontFamily(fontFamilyName); // setting the font to be used
            
            // Now use that TextBox as the brush for a material
            DiffuseMaterial diffuseMaterial = new DiffuseMaterial();
            // Allows the application of a 2-D brush, 
            // like a SolidColorBrush or TileBrush, to a diffusely-lit 3-D model. 
            // we are creating the brush from the TextBlock
            diffuseMaterial.Brush = new VisualBrush(textBlock);

            //calculation of text width (assumming that characters are square):
            double width = Windows.Query.Width(text, new System.Drawing.Font(fontFamilyName, (float)height), height);//text.Length * height;
            
            // we need to find the four corners
            // p0: the lower left corner;  p1: the upper left
            // p2: the lower right; p3: the upper right
            Point3D p0 = basePoint;
            
            // when the base point is the center point we have to set it up in different way
            if (isBasePointCenterPoint)
            {
                p0 = basePoint - width / 2 * vectorOver - height / 2 * vectorUp;
            }

            Point3D p1 = p0 + vectorUp * 1 * height;
            Point3D p2 = p0 + vectorOver * width;
            Point3D p3 = p0 + vectorUp * 1 * height + vectorOver * width;

            // we are going to create object in 3D now:
            // this object will be painted using the (text) brush created before
            // the object is rectangle made of two triangles (on each side).
            MeshGeometry3D meshGeometry3D = new MeshGeometry3D();
            meshGeometry3D.Positions = new Point3DCollection();
            meshGeometry3D.Positions.Add(p0);    // 0
            meshGeometry3D.Positions.Add(p1);    // 1
            meshGeometry3D.Positions.Add(p2);    // 2
            meshGeometry3D.Positions.Add(p3);    // 3
            
            // when we want to see the text on both sides:
            if (isDoubleSided)
            {
                meshGeometry3D.Positions.Add(p0);    // 4
                meshGeometry3D.Positions.Add(p1);    // 5
                meshGeometry3D.Positions.Add(p2);    // 6
                meshGeometry3D.Positions.Add(p3);    // 7
            }
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(3);
            meshGeometry3D.TriangleIndices.Add(1);
            meshGeometry3D.TriangleIndices.Add(0);
            meshGeometry3D.TriangleIndices.Add(2);
            meshGeometry3D.TriangleIndices.Add(3);
            
            // when we want to see the text on both sides:
            if (isDoubleSided)
            {
                meshGeometry3D.TriangleIndices.Add(4);
                meshGeometry3D.TriangleIndices.Add(5);
                meshGeometry3D.TriangleIndices.Add(7);
                meshGeometry3D.TriangleIndices.Add(4);
                meshGeometry3D.TriangleIndices.Add(7);
                meshGeometry3D.TriangleIndices.Add(6);
            }
            
            // texture coordinates must be set to
            // stretch the TextBox brush to cover 
            // the full side of the 3D label.
            meshGeometry3D.TextureCoordinates.Add(new Point(0, 1));
            meshGeometry3D.TextureCoordinates.Add(new Point(0, 0));
            meshGeometry3D.TextureCoordinates.Add(new Point(1, 1));
            meshGeometry3D.TextureCoordinates.Add(new Point(1, 0));
            // when the label is double sided:
            if (isDoubleSided)
            {
                meshGeometry3D.TextureCoordinates.Add(new Point(1, 1));
                meshGeometry3D.TextureCoordinates.Add(new Point(1, 0));
                meshGeometry3D.TextureCoordinates.Add(new Point(0, 1));
                meshGeometry3D.TextureCoordinates.Add(new Point(0, 0));
            }

            return new GeometryModel3D(meshGeometry3D, diffuseMaterial);
            // Now it is time to create ModelVisual3D (that we are goint ot return):
            //ModelVisual3D result = new ModelVisual3D();
            // we are setting the content:
            // our 3D rectangle object covered with materila that is made of label (TextBox with text)
            //result.Content = new GeometryModel3D(meshGeometry3D, diffuseMaterial);
            //return result;
        }
    }


}
