// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.UI
{
    public class RenameSpaceOptions
    {
        public Position Position { get; set; } = Position.Prefix;
        
        public bool IncludeName { get; set; } = true;

        public bool IncludeNumber { get; set; } = true;

        public bool IncludeLevel { get; set; } = true;

        public int StartIndex { get; set; } = -1;

        public string LevelSpeparator { get; set; } = "_";

        public string NameSeparator { get; set; } = " ";

        public int DigitsNumber { get; set; } = 3;

        public RenameSpaceOptions()
        {

        }

        public RenameSpaceOptions(RenameSpaceOptions renameSpaceOption)
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
