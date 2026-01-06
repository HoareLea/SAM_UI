// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.UI.WPF
{
    public static partial class Create
    {
        public static IFilterControl IFilterControl(this IUIFilter uIFilter)
        {
            if (uIFilter == null)
            {
                return null;
            }

            if (uIFilter is UITextFilter)
            {
                TextFilterControl textFilterControl = new TextFilterControl((UITextFilter)uIFilter);
                return textFilterControl;
            }
            else if (uIFilter is UILogicalFilter)
            {
                LogicalFilterControl logicalFilterControl = new LogicalFilterControl((UILogicalFilter)uIFilter);
                return logicalFilterControl;
            }
            else if (uIFilter is UIRelationFilter)
            {
                RelationFilterControl relationFilterControl = new RelationFilterControl((UIRelationFilter)uIFilter);
                return relationFilterControl;
            }
            else if (uIFilter is UINumberFilter)
            {
                NumberFilterControl numberFilterControl = new NumberFilterControl((UINumberFilter)uIFilter);
                return numberFilterControl;
            }
            else if (uIFilter is UIEnumFilter)
            {
                EnumFilterControl enumFilterControl = new EnumFilterControl((UIEnumFilter)uIFilter);
                return enumFilterControl;
            }
            else if (uIFilter is UIComplexReferenceFilter)
            {
                ComplexReferenceFilterControl complexReferenceFilterControl = new ComplexReferenceFilterControl((UIComplexReferenceFilter)uIFilter);
                return complexReferenceFilterControl;
            }

            return null;
        }
    }
}
