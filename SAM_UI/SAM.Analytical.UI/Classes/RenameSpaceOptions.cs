namespace SAM.Analytical.UI
{
    public class RenameSpaceOption
    {
        public Position Position { get; set; } = Position.Prefix;
        
        public bool IncludeName { get; set; } = true;

        public bool IncludeNumber { get; set; } = true;

        public bool IncludeLevel { get; set; } = true;

        public string LevelSpeparator { get; set; } = "_";

        public string NameSeparator { get; set; } = " ";

        public int DigitsNumber { get; set; } = 3;

        public RenameSpaceOption()
        {

        }

    }
}
