using SAM.Analytical.Classes;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Create
    {
        public static AnalyticalModel AnalyticalModel(this AnalyticalModel analyticalModel, Case @case)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            if (@case is ISelectiveCase selectiveCase && selectiveCase.CaseSelection is FilterSelection filterSelection)
            {
                List<IJSAMObject> jSAMObjects = filterSelection.IJSAMObjects<IJSAMObject>(analyticalModel)?.FindAll(x => x != null);
                if(jSAMObjects.TrueForAll(x => x is SAMObject))
                {
                    selectiveCase.CaseSelection = new ObjectReferenceCaseSelection(jSAMObjects.ConvertAll(x => new ObjectReference((SAMObject)x)));
                }
                else
                {
                    selectiveCase.CaseSelection = new SAMObjectCaseSelection(jSAMObjects);
                }
            }

            return Analytical.Create.AnalyticalModel(analyticalModel, @case);
        }
    }
}
