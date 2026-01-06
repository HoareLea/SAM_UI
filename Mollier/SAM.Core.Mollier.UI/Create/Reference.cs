using SAM.Geometry.Mollier;

namespace SAM.Core.Mollier.UI
{
    public static partial class Create
    {
        public static IReference Reference(this UIMollierPoint uIMollierPoint)
        {
            if (uIMollierPoint == null)
            {
                return null;
            }
            
            if(uIMollierPoint is UIMollierProcessPoint)
            {
                return ((UIMollierProcessPoint)uIMollierPoint).Reference;
            }


            return Geometry.Mollier.Create.Reference(uIMollierPoint);
        }
    }
}
