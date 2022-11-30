using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static void Opacity(this Model3DGroup model3DGroup, double opacity)
        {
            if(model3DGroup == null)
            {
                return;
            }

            foreach(Model3D model3D in model3DGroup.Children)
            {
                if(model3D is Model3DGroup)
                {
                    Opacity((Model3DGroup)model3D, opacity);
                    continue;
                }


                GeometryModel3D geometryModel3D = model3D as GeometryModel3D;
                if(geometryModel3D == null)
                {
                    continue;
                }

                DiffuseMaterial diffuseMaterial = geometryModel3D.Material as DiffuseMaterial;
                if(diffuseMaterial == null)
                {
                    continue;
                }

                SolidColorBrush solidColorBrush = diffuseMaterial.Brush as SolidColorBrush;
                if (solidColorBrush == null)
                {
                    continue;
                }

                if (solidColorBrush.Opacity != opacity)
                {
                    solidColorBrush.Opacity = opacity;
                }
            }
        }
    }
}