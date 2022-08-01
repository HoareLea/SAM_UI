using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static Dictionary<MollierPoint, int> NeighborhoodCount(this IEnumerable<MollierPoint> mollierPoints, out double maxCount)
        { 

            maxCount = 0;//maximum points in one area, in the end it takes logarithm of the number of points for greater graph clarity
            Dictionary<MollierPoint, int> result = new Dictionary<MollierPoint, int>();//dictionary: key - mollierpoint, value - number of points in its area

            double deltaRelativeHumidity = 10;//RH interval
            double deltaEnthalpy = 3;//enthalpy interval
            
            //base size
            int RH_size = 100 / System.Convert.ToInt32(deltaRelativeHumidity) + 7;
            int Ent_size = 200 / System.Convert.ToInt32(deltaEnthalpy) + 7;

            //initialize arrays
            List<MollierPoint>[,] rectangles_points = new List<MollierPoint>[RH_size, Ent_size];//for every rh interval and every enthalpy interval it stores the list of points that belong to this area 
            List<MollierPoint> Mollierpoints = mollierPoints.ToList();

            //adding points to appropriate areas example: if deltarh = 10 and delta enthalpy = 3 then rectangles_points[1][2].add(point) means add point to the area which is 10-20 rh and 6-9 enthalpy
            for(int i=0; i<Mollierpoints.Count(); i++)
            {
                MollierPoint point = Mollierpoints[i];
                int RH = System.Convert.ToInt32(System.Math.Floor(point.RelativeHumidity / deltaRelativeHumidity));
                RH = RH == 10 ? 9 : RH;
                int enthalpy = System.Convert.ToInt32(System.Math.Floor(point.Enthalpy / deltaEnthalpy/1000)) + 10;//+ 10 because there might be negative enthalpy so shift by 10
                if (rectangles_points[RH, enthalpy] == null)
                    rectangles_points[RH, enthalpy] = new List<MollierPoint>();
                rectangles_points[RH, enthalpy].Add(point);
            }
            //walk through all areas
            for(int i=0; i<=10; i++)
            {
                for(int j=0; j<=50; j++)
                {
                    //if we found a point, add the point to the result(as a key) and number of other points for this area(as a value), it takes logarithm of the number of points for greater graph clarity
                    if (rectangles_points[i,j] == null)
                    {
                        continue;
                    }
                    int size = rectangles_points[i, j].Count();
                    int value = System.Convert.ToInt32(System.Math.Log(size));
                    if (value > maxCount)
                    {
                        maxCount = value;
                    }
                    for(int index = 0; index < size; index++)
                    {
                        MollierPoint point = rectangles_points[i, j][index];
                        result.Add(point,value);
                    }
                }
            }

            return result;
        }
    }
}
