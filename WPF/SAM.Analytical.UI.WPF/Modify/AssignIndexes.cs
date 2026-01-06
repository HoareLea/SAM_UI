using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Modify
    {
        public static void AssignIndexes(this IEnumerable<DisplayGlazingCalculationResult> displayGlazingCalculationResults)
        {
            if(displayGlazingCalculationResults == null || displayGlazingCalculationResults.Count() == 0)
            {
                return;
            }

            List<Tuple<double, Criteria, DisplayGlazingCalculationResult>> tuples = new List<Tuple<double, Criteria, DisplayGlazingCalculationResult>>();
            foreach(DisplayGlazingCalculationResult displayGlazingCalculationResult_Temp in displayGlazingCalculationResults)
            {
                if(displayGlazingCalculationResult_Temp == null)
                {
                    continue;
                }

                Criteria? criteria_Nullable = displayGlazingCalculationResult_Temp.Criteria;

                Criteria criteria_Temp = Criteria.Undefined;
                if (criteria_Nullable != null && criteria_Nullable.HasValue)
                {
                    criteria_Temp = criteria_Nullable.Value;
                }

                double score = displayGlazingCalculationResult_Temp.GetScore();

                tuples.Add(new Tuple<double, Criteria, DisplayGlazingCalculationResult>(score, criteria_Temp, displayGlazingCalculationResult_Temp));
            }

            if(tuples == null || tuples.Count == 0)
            {
                return;
            }

            int index = 0;

            List<Tuple<double, Criteria, DisplayGlazingCalculationResult>> tuples_Temp = null;

            foreach(Criteria criteria in new Criteria[] { Criteria.All, Criteria.NotAll, Criteria.None, Criteria.Undefined})
            {
                tuples_Temp = tuples.FindAll(x => x.Item2 == criteria);
                if (tuples_Temp != null && tuples_Temp.Count != 0)
                {
                    tuples.RemoveAll(x => tuples_Temp.Contains(x));

                    List<Tuple<double, Criteria, DisplayGlazingCalculationResult>> tuples_Temp_Temp = tuples_Temp.FindAll(x => !double.IsNaN(x.Item1));
                    if (tuples_Temp_Temp != null && tuples_Temp_Temp.Count != 0)
                    {
                        tuples_Temp.RemoveAll(x => tuples_Temp_Temp.Contains(x));
                        tuples_Temp_Temp.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                        foreach (Tuple<double, Criteria, DisplayGlazingCalculationResult> tuple in tuples_Temp_Temp)
                        {
                            tuple.Item3.Index = index;
                            index++;
                        }
                    }

                    foreach (Tuple<double, Criteria, DisplayGlazingCalculationResult> tuple in tuples_Temp)
                    {
                        tuple.Item3.Index = index;
                        index++;
                    }
                }
            }
        }
    }
}