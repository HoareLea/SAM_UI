using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        public static void SetIJSAMObject(this Model3D model3D, IJSAMObject jSAMObject)
        {
            if(model3D == null)
            {
                return;
            }

            model3D.SetValue(DependencyProperty.IJSAMObjectProperty, jSAMObject);
        }
    }
}