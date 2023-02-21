using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    public static partial class Modify
    {
        public static string RenameSpace(this AdjacencyCluster adjacencyCluster, Space space, string name, RenameSpaceOption renameSpaceOption)
        {
            if(adjacencyCluster == null || space == null)
            {
                return null;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null)
            {
                return null;
            }

            int spaceIndex =spaces.FindIndex(x => x.Guid == space.Guid);
            if(spaceIndex == -1)
            {
                return null;
            }

            Space space_Temp = spaces[spaceIndex];
            if(space_Temp == null)
            {
                return null;
            }

            spaces.RemoveAt(spaceIndex);

            if (renameSpaceOption == null)
            {
                renameSpaceOption = new RenameSpaceOption();
            }

            if(!renameSpaceOption.IncludeName && !renameSpaceOption.IncludeNumber)
            {
                return null;
            }

            if(!renameSpaceOption.IncludeName)
            {
                name = space_Temp.Name;
            }

            string fullName = null;

            if (renameSpaceOption.IncludeNumber)
            {
                int max = int.MaxValue;
                if (renameSpaceOption.DigitsNumber > 0 && renameSpaceOption.DigitsNumber < 5)
                {
                    string numberText = "1";
                    for (int i = 0; i < renameSpaceOption.DigitsNumber; i++)
                    {
                        numberText += "0";
                    }

                    max = int.Parse(numberText);
                }

                for (int index = 1; index < max; index++)
                {
                    List<string> values = new List<string>();
                    if (renameSpaceOption.IncludeLevel)
                    {
                        if (space.TryGetValue(SpaceParameter.LevelName, out string levelName) && !string.IsNullOrEmpty(levelName))
                        {
                            levelName = levelName.Trim();
                            if (levelName.ToUpper().StartsWith("LEVEL"))
                            {
                                levelName = levelName.Substring(5).Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(levelName))
                            {
                                values.Add(levelName);
                            }
                        }
                    }

                    if(!string.IsNullOrEmpty(renameSpaceOption.LevelSpeparator))
                    {
                        values.Add(renameSpaceOption.LevelSpeparator);
                    }

                    string number = index.ToString();
                    while (number.Length < renameSpaceOption.DigitsNumber)
                    {
                        number = "0" + number;
                    }

                    values.Add(number);

                    fullName = string.Join(string.Empty, values);
                    if (name != null)
                    {

                        if (renameSpaceOption.Position == Position.Prefix)
                        {
                            fullName = string.Format("{0}{1}{2}", string.Join(string.Empty, values), renameSpaceOption.NameSeparator == null ? string.Empty : renameSpaceOption.NameSeparator, name);
                        }
                        else if (renameSpaceOption.Position == Position.Sufix)
                        {
                            fullName = string.Format("{0}{1}{2}", name, renameSpaceOption.NameSeparator == null ? string.Empty : renameSpaceOption.NameSeparator, string.Join(string.Empty, values));
                        }
                    }

                    if (spaces.Find(x => x.Name == fullName) == null)
                    {
                        break;
                    }

                    fullName = null;
                }
            }
            else
            {
                fullName = name;
            }

            if(fullName == null)
            {
                return null;
            }


            space_Temp = new Space(space_Temp, fullName, space_Temp.Location);
            adjacencyCluster.AddObject(space_Temp);

            return fullName;
        }
    }
}