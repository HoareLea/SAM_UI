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
            maxCount = 0;
            Dictionary<MollierPoint, int> result = new Dictionary<MollierPoint, int>();

            //List<int>[]rectangles  = new List<int>[150];//rectangles[7] means first enthalpy, 80%RH - 8th countd from 0, rectangles[25] means third enthalpy in sequence and 60% RH, 
            //mollierPoints?.ToList();
            //List<MollierPoint> mollierPoints1 = new List<MollierPoint>(mollierPoints);


            //Tuple<double, double, List<MollierPoint>> tuple = new Tuple<double, double, List<MollierPoint>>( 10, 10, new List<MollierPoint>());
            //List<Tuple<double, double, List<MollierPoint>>> tuples = new List<Tuple<double, double, List<MollierPoint>>>();
            //tuples.Add(tuple);

            //tuples.Find(x => x.Item1 == 10).Item3;

            //List<List<List<MollierPoint>>> data = new List<List<List<MollierPoint>>>();

            //lista_kwadratow[10][16].add(punkt)
            //BRUT CODE

            List<MollierPoint>[,] rectangles_points = new List<MollierPoint>[11, 61];

            //int index = 0;

            double deltaRelativeHumidity = 25;
            double deltaEnthalpy = 15;
            DateTime dateTime = DateTime.Now;
            List<MollierPoint> Mollierpoints = mollierPoints.ToList();
            for(int i=0; i<Mollierpoints.Count(); i++)
            {
                MollierPoint point = Mollierpoints[i];
                int RH = System.Convert.ToInt32(System.Math.Floor(point.RelativeHumidity / deltaRelativeHumidity));
                RH = RH == 10 ? 9 : RH;
                int enthalpy = System.Convert.ToInt32(System.Math.Floor(point.Enthalpy / deltaEnthalpy/1000)) + 10;
                if (rectangles_points[RH, enthalpy] == null)
                    rectangles_points[RH, enthalpy] = new List<MollierPoint>();
                rectangles_points[RH, enthalpy].Add(point);
            }
            for(int i=0; i<=10; i++)
            {
                for(int j=0; j<=60; j++)
                {
                    if (rectangles_points[i,j] == null)
                    {
                        continue;
                    }
                    int size = rectangles_points[i, j].Count();
                    if (size > maxCount)
                    {
                        maxCount = size;
                    }
                    for(int index = 0; index < size; index++)
                    {
                        MollierPoint point = rectangles_points[i, j][index];
                        result.Add(point,size);
                    }
                }
            }
            double time = (DateTime.Now - dateTime).TotalMilliseconds;
            //VER 2
            //foreach (MollierPoint point in mollierPoints)
            //{
            //    double rh_Start = System.Math.Floor(point.RelativeHumidity / deltaRelativeHumidity) * deltaRelativeHumidity;
            //    rh_Start = rh_Start == 100 ? 90 : rh_Start;
            //    double rh_End = rh_Start + deltaRelativeHumidity;

            //    double enthalpy_Start = System.Math.Floor(point.Enthalpy / deltaEnthalpy) * deltaEnthalpy;
            //    double enthalpy_End = enthalpy_Start + deltaEnthalpy;

            //    int res = 0;
            //    foreach (MollierPoint secondPoint in mollierPoints)
            //    {
            //        if (point == secondPoint)
            //        {
            //            continue;
            //        }

            //        double rh = secondPoint.RelativeHumidity;
            //        if (rh_Start > rh || rh > rh_End)
            //        {
            //            continue;
            //        }

            //        double enthalpy = secondPoint.Enthalpy;
            //        if (enthalpy_Start > enthalpy || enthalpy > enthalpy_End)
            //        {
            //            continue;
            //        }

            //        res++;
            //    }

            //    if (res > maxCount)
            //        maxCount = res;

            //    result.Add(point, res);
            //}

            //VER 1
            //foreach(MollierPoint point in mollierPoints)
            //{
            //    double RH = System.Math.Floor(point.RelativeHumidity / 10);
            //    double enthalpy = System.Math.Floor((System.Math.Floor(point.Enthalpy/1000) + 1) / 3);
            //    int res = -1;
            //    foreach(MollierPoint secondPoint in mollierPoints)
            //    {
            //        double secondRH = System.Math.Floor(secondPoint.RelativeHumidity / 10);
            //        double secondEnthalpy = System.Math.Floor((System.Math.Floor(secondPoint.Enthalpy / 1000) + 1) / 3);
            //        if (RH == secondRH && enthalpy == secondEnthalpy)
            //        {
            //            res++;
            //        }
            //    }
            //    if (res > maxCount)
            //        maxCount = res;
            //    result.Add(point, res);
            //}

            MessageBox.Show(time.ToString());



            //foreach (MollierPoint point in mollierPoints)
            //{
            //    int RH = System.Convert.ToInt32(point.RelativeHumidity/10);// value from 0 to 9, for example RH 13  returns 1
            //    int enthalpy = System.Convert.ToInt32((point.Enthalpy + 31))/3;//+ 2 beacause from 77 to 80 enthalpy so started with % 3 == 2, enthalpy + 30 to make sure its positive
            //    rectangles[enthalpy * 10 + RH].Add(index);
            //    index++;
            //}
            //for(int i=0; i<150; i++)//150 - enthalpy max - min
            //{
            //    int size = rectangles[i].Count;
            //    if (size == 0)
            //        continue;
            //    for(int j=0; j<size; j++)
            //    {
            //        index = rectangles[i][j];

            //        result.Add(mollierPoints, )
            //    }
            //}

            return result;
        }
    }
}
