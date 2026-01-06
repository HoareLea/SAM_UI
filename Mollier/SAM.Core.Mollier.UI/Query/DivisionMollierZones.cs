// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Mollier;
using System.Collections.Generic;
using System.Drawing;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        /// <summary>
        /// Creates mollier zones in place of points with color that depends on their density
        /// in a given area
        /// </summary>
        /// <param name="mollierPoints">Mollier points</param>
        /// <param name="mollierControlSettings">Mollier control settings</param>
        /// <returns>List of divided zones</returns>
        public static List<UIMollierZone> DivisionMollierZones(this IEnumerable<UIMollierPoint> mollierPoints, MollierControlSettings mollierControlSettings)
        {
            if(mollierPoints == null || mollierControlSettings == null)
            {
                return null;
            }
            List<UIMollierZone> result = new List<UIMollierZone>();

            double enthalpyMin = mollierControlSettings.Enthalpy_Min / 1000;
            double enthalpyMax = mollierControlSettings.Enthalpy_Max / 1000;
            double relativeHumidityMin = 0;
            double relativeHumidityMax = 100;
            int enthalpyInterval = mollierControlSettings.DivisionAreaEnthalpy_Interval;
            int relativeHumidityInterval = mollierControlSettings.DivisionAreaRelativeHumidity_Interval;
            double pressure = mollierControlSettings.Pressure;

            int gridRelativeHumidityMax = (int)((relativeHumidityMax + System.Math.Abs(relativeHumidityMin)) / relativeHumidityInterval) + 1;
            int gridEnthalpyMax = (int)((enthalpyMax + System.Math.Abs(enthalpyMin)) / enthalpyInterval) + 1;
            int[,] grid = new int[gridRelativeHumidityMax, gridEnthalpyMax];

            int maxPointsNumberInOneArea = 0;
            foreach(MollierPoint mollierPoint in mollierPoints)
            {
                double enthalpy = mollierPoint.Enthalpy / 1000;
                double relativeHumidity = mollierPoint.RelativeHumidity == 100 ? 90 : mollierPoint.RelativeHumidity;
                
                // Getting grid index
                int relativeHumidityIndex = (int)((relativeHumidity + System.Math.Abs(relativeHumidityMin)) / relativeHumidityInterval);
                int enthalpyIndex = (int)((enthalpy + System.Math.Abs(enthalpyMin)) / enthalpyInterval);

                grid[relativeHumidityIndex, enthalpyIndex]++;
                maxPointsNumberInOneArea = System.Math.Max(grid[relativeHumidityIndex, enthalpyIndex], maxPointsNumberInOneArea);
            }

            for(int i=0; i<gridRelativeHumidityMax; i++)
            {
                for(int j=0; j<gridEnthalpyMax; j++)
                {
                    if (grid[i,j] == 0)
                    {
                        continue;
                    }

                    List<MollierPoint> zonePoints = new List<MollierPoint>();
                    
                    //Creating Zone from corner Points getting from grid indicess 
                    double relativeHumidity = i * relativeHumidityInterval - System.Math.Abs(relativeHumidityMin);
                    double enthalpy = (j * enthalpyInterval - System.Math.Abs(enthalpyMin)) * 1000;
                    zonePoints.Add(Mollier.Create.MollierPoint_ByEnthalpyAndRelativeHumidity(enthalpy, relativeHumidity, pressure));

                    relativeHumidity = i * relativeHumidityInterval - System.Math.Abs(relativeHumidityMin);
                    enthalpy = ((j + 1) * enthalpyInterval - System.Math.Abs(enthalpyMin)) * 1000;
                    zonePoints.Add(Mollier.Create.MollierPoint_ByEnthalpyAndRelativeHumidity(enthalpy, relativeHumidity, pressure));

                    relativeHumidity = (i + 1) * relativeHumidityInterval - System.Math.Abs(relativeHumidityMin);
                    enthalpy = ((j + 1) * enthalpyInterval - System.Math.Abs(enthalpyMin)) * 1000;
                    zonePoints.Add(Mollier.Create.MollierPoint_ByEnthalpyAndRelativeHumidity(enthalpy, relativeHumidity, pressure));

                    relativeHumidity = (i + 1) * relativeHumidityInterval - System.Math.Abs(relativeHumidityMin);
                    enthalpy = (j * enthalpyInterval - System.Math.Abs(enthalpyMin)) * 1000;
                    zonePoints.Add(Mollier.Create.MollierPoint_ByEnthalpyAndRelativeHumidity(enthalpy, relativeHumidity, pressure));


                    MollierZone mollierZone = new MollierZone(zonePoints);

                    double ratio = maxPointsNumberInOneArea == 0 || maxPointsNumberInOneArea == 1 ? 0 : System.Math.Log(grid[i, j]) / System.Math.Log(maxPointsNumberInOneArea);
                    Color color = Core.Query.Lerp(Color.Red, Color.Blue, ratio);
                    string text = grid[i, j].ToString();

                    result.Add(new UIMollierZone(mollierZone, color, text));
                }
            }


            return result;
        }
    
    }
}
