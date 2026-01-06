// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.UI
{
    public static partial class Query
    {
        public static ComplexReferenceFilter DefaultComplexReferenceFilter(this IComplexReference complexReference, ISAMObjectRelationCluster sAMObjectRelationCluster)
        {
            ComplexReferenceFilter result = new ComplexReferenceTextFilter() { CaseSensitive = true, ComplexReference = complexReference, TextComparisonType = TextComparisonType.Contains, SAMObjectRelationCluster = sAMObjectRelationCluster };

            if (complexReference == null || sAMObjectRelationCluster == null)
            {
                return result;
            }

            List<object> values = sAMObjectRelationCluster.GetValues(complexReference)?.FindAll(x => x != null);
            if (values != null && values.Count > 0 && values.TrueForAll(x => Core.Query.IsNumeric(x) && !(x is System.Enum)))
            {
                return new ComplexReferenceNumberFilter() { ComplexReference = complexReference, NumberComparisonType = NumberComparisonType.Equals, SAMObjectRelationCluster = sAMObjectRelationCluster };
            }

            return result;
        }
    }
}
