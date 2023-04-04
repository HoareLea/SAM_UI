namespace SAM.Analytical.UI
{
    public class RenameSpaceOption
    {
        public Position Position { get; set; } = Position.Prefix;
        
        public bool IncludeName { get; set; } = true;

        public bool IncludeNumber { get; set; } = true;

        public bool IncludeLevel { get; set; } = true;

        public int StartIndex { get; set; } = -1;

        public string LevelSpeparator { get; set; } = "_";

        public string NameSeparator { get; set; } = " ";

        public int DigitsNumber { get; set; } = 3;

        public RenameSpaceOption()
        {

        }

        public RenameSpaceOption(RenameSpaceOption renameSpaceOption)
        {
            if(renameSpaceOption != null)
            {
                Position = renameSpaceOption.Position;
                IncludeName = renameSpaceOption.IncludeName;
                IncludeNumber = renameSpaceOption.IncludeNumber;
                IncludeLevel = renameSpaceOption.IncludeLevel;
                StartIndex = renameSpaceOption.StartIndex;
                LevelSpeparator = renameSpaceOption.LevelSpeparator;
                NameSeparator = renameSpaceOption.NameSeparator;
                DigitsNumber = renameSpaceOption.DigitsNumber;
            }
        }

    }
}
