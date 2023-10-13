using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static ComplexReferenceFilter DefaultComplexReferenceFilter(this IComplexReference complexReference, RelationCluster relationCluster)
        {
            ComplexReferenceFilter result = new ComplexReferenceTextFilter() { CaseSensitive = true, ComplexReference = complexReference, RelationCluster = relationCluster, TextComparisonType = TextComparisonType.Contains};

            if (complexReference == null || relationCluster == null)
            {
                return result;
            }

            List<object> values = relationCluster.GetValues(complexReference)?.FindAll(x => x != null);
            if (values != null && values.Count > 0 && values.TrueForAll(x => Core.Query.IsNumeric(x)))
            {
                return new ComplexReferenceNumberFilter() { ComplexReference = complexReference, RelationCluster = relationCluster, NumberComparisonType = NumberComparisonType.Equals };
            }

            return result;
        }
    }
}