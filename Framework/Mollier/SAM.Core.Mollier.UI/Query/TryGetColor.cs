using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static bool TryGetColor(this System.Drawing.Color? initialColor, List<int> customColors, out System.Drawing.Color selectedColor)
        {
            selectedColor = System.Drawing.Color.Empty;

            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (initialColor != null && initialColor.HasValue)
                {
                    colorDialog.Color = initialColor.Value;
                }

                colorDialog.FullOpen = true;
                colorDialog.AnyColor = true;

                if (customColors != null)
                {
                    int argb = colorDialog.Color.ToArgb();
                    if (colorDialog.Color.A != 0 || colorDialog.Color.R != 0 || colorDialog.Color.G != 0 || colorDialog.Color.B != 0)
                    {
                        if (!customColors.Contains(argb))
                        {
                            customColors.Insert(0, argb);
                        }
                    }
                }

                if (customColors != null)
                {
                    colorDialog.CustomColors = customColors.ToArray();
                }

                if (colorDialog.ShowDialog() != DialogResult.OK)
                {
                    return false;
                }

                if (customColors != null)
                {
                    customColors.Clear();

                    foreach(int customColor in colorDialog.CustomColors)
                    {
                        System.Drawing.Color color = System.Drawing.Color.FromArgb(customColor);
                        if(color.A == 0 && color.R == 255 && color.G == 255 && color.B == 255)
                        {
                            continue;
                        }

                        customColors.Add(customColor);
                    }
                }

                selectedColor = colorDialog.Color;
                return true;
            }

            return false;
        }
    }
}

