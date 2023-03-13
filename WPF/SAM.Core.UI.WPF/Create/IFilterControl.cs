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

            return null;
        }
    }
}
