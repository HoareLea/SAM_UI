using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Mollier.UI
{

    public static partial class Query
    {
        /// <summary>
        /// Count the most frequenly appearing value of pressure in both lists
        /// </summary>
        /// <param name="mollierPoints">List of Mollier Points</param>
        /// <param name="mollierProcesses">List of Mollier Processes</param>
        /// <returns>Returns the most frequenly appearing value of pressure in both lists</returns>
        public static double DefaultPressure(IEnumerable<MollierPoint> mollierPoints = null, IEnumerable<IMollierProcess> mollierProcesses = null)
        {
            List<double> pressures = new List<double>();
            if (mollierProcesses != null)
            {
                foreach (MollierProcess process in mollierProcesses)
                {
                    pressures.Add(process.Pressure);
                }
            }
            if (mollierPoints != null)
            {
                foreach (MollierPoint point in mollierPoints)
                {
                    pressures.Add(point.Pressure);
                }
            }

            if(pressures == null || pressures.Count == 0)
            {
                return Standard.Pressure;
            }

            pressures.Sort();
            int count = 1, number = 1;
            double pressure = pressures[0];
            for (int i = 1; i < pressures.Count(); i++)
            {
                if (pressures[i] == pressures[i - 1])
                {
                    count++;
                }
                else
                {
                    if (count > number)
                    {
                        number = count;
                        pressure = pressures[i - 1];
                    }
                    count = 1;
                }
            }

            if(double.IsNaN(pressure))
            {
                return Standard.Pressure;
            }

            return pressure;
        }
    }
}

