using System.Collections.Generic;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Calculates color of points depends on their density on the chart
        /// </summary>
        /// <param name="mollierPoints">Mollier points</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>Dictionary with color for all points</returns>
        public static Dictionary<UIMollierPoint, Color> ColorDictionary(this IEnumerable<UIMollierPoint> mollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierPoints == null)
            {
                return null;
            }
            Dictionary<UIMollierPoint, Color> result = new Dictionary<UIMollierPoint, Color>();   

            PointGradientVisibilitySetting pointGradientVisibilitySetting = mollierControlSettings.VisibilitySettings.GetVisibilitySetting("User", ChartParameterType.Point) as PointGradientVisibilitySetting;
            if(pointGradientVisibilitySetting == null)
            {
                Color color = mollierControlSettings.VisibilitySettings.GetColor(mollierControlSettings.DefaultTemplateName, ChartParameterType.Point, ChartDataType.Undefined);
                foreach (UIMollierPoint point in mollierPoints)
                {
                    if(point.UIMollierAppearance.Color == Color.Empty)
                    {
                        point.UIMollierAppearance.Color = color;
                    }
                    if(!result.ContainsKey(point))
                    {
                        result.Add(point, Color.Empty);
                    }
                }
                return result;
            }

            double enthalpyMin = mollierControlSettings.Enthalpy_Min / 1000;
            double enthalpyMax = mollierControlSettings.Enthalpy_Max / 1000;
            double relativeHumidityMin = 0;
            double relativeHumidityMax = 100;
            int enthalpyInterval = mollierControlSettings.DivisionAreaEnthalpy_Interval;
            int relativeHumidityInterval = mollierControlSettings.DivisionAreaRelativeHumidity_Interval;

            int gridEnthalpyMax = (int)((enthalpyMax + System.Math.Abs(enthalpyMin)) / enthalpyInterval) + 1;
            int gridRelativeHumidityMax = (int)((relativeHumidityMax + System.Math.Abs(relativeHumidityMin)) / relativeHumidityInterval) + 1;
            List<UIMollierPoint>[,] grid = new List<UIMollierPoint>[gridRelativeHumidityMax, gridEnthalpyMax];

            int maxPointsNumberInOneArea = 0;
            foreach (UIMollierPoint uImollierPoint in mollierPoints)
            {
                double enthalpy = uImollierPoint.Enthalpy / 1000;
                double relativeHumidity = uImollierPoint.RelativeHumidity;
                if(enthalpy == enthalpyMax)
                {
                    enthalpy -= enthalpyInterval;
                }
                if (relativeHumidity == relativeHumidityMax)
                {
                    relativeHumidity -= relativeHumidityInterval;
                }

                int enthalpyIndex = (int)((enthalpy + System.Math.Abs(enthalpyMin)) / enthalpyInterval);
                int relativeHumidityIndex = (int)((relativeHumidity + System.Math.Abs(relativeHumidityMin)) / relativeHumidityInterval);

                if(grid[relativeHumidityIndex, enthalpyIndex] == null)
                {
                    grid[relativeHumidityIndex, enthalpyIndex] = new List<UIMollierPoint>();
                }

                grid[relativeHumidityIndex, enthalpyIndex].Add(uImollierPoint);
                maxPointsNumberInOneArea = System.Math.Max(grid[relativeHumidityIndex, enthalpyIndex].Count, maxPointsNumberInOneArea);
            }

            for (int i = 0; i < gridRelativeHumidityMax; i++)
            {
                for (int j = 0; j < gridEnthalpyMax; j++)
                {
                    if (grid[i, j] == null)
                    {
                        continue;
                    }
                    double pointsInThisArea = grid[i, j].Count;
                    foreach(UIMollierPoint mollierPoint in grid[i, j])
                    {
                        if(maxPointsNumberInOneArea == 0 || maxPointsNumberInOneArea == 1)
                        {
                            result.Add(mollierPoint, Core.Query.Lerp(pointGradientVisibilitySetting.Color, pointGradientVisibilitySetting.GradientColor, 0));
                            continue;
                        }
                        double ratio = System.Math.Log(pointsInThisArea) / System.Math.Log(maxPointsNumberInOneArea);

                        result.Add(mollierPoint, Core.Query.Lerp(pointGradientVisibilitySetting.Color, pointGradientVisibilitySetting.GradientColor, ratio));
                    }
                }
            }

            return result;
        }
    }
}
