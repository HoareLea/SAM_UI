using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        // ui mollier gr, this i aIEnumerable element at
        public static List<List<UIMollierProcess>> Group(this List<UIMollierProcess> mollierProcesses)
        {
            List<List<UIMollierProcess>> result = new List<List<UIMollierProcess>>();
            List<List<int>> resultID = new List<List<int>>();//list 0 3 2 4 means that there should be processes in sequence are indexes 0 3 2 4 in mollierprocesses
          
            List<int> nextProcessID = new List<int>();
            List<bool> visited = new List<bool>();
            List<int> previousProcessCount = new List<int>();

            int none = -1; 
            for (int i = 0; i < mollierProcesses.Count; i++)
            {
                nextProcessID.Add(none);
                visited.Add(false);
                previousProcessCount.Add(0);
            }


            for(int i=0; i<mollierProcesses.Count; i++)
            {
                MollierPoint end = mollierProcesses[i].End;
                if (end == null) continue;

                for (int j = 0; j < mollierProcesses.Count; j++)
                {
                    MollierPoint start = mollierProcesses[j].Start;
                    if (start == null || i == j) continue;

                    if(start.DryBulbTemperature == end.DryBulbTemperature && 
                        System.Math.Round(start.HumidityRatio, 5) == System.Math.Round(end.HumidityRatio, 5)) // do zminy zeby byla jakas odleglosc okreslona
                    {
                        nextProcessID[i] = j;
                        previousProcessCount[j]++;
                        break;  
                    }
                }
            }

            List<int> startingProcessesID = new List<int>();

            for(int i=0; i<mollierProcesses.Count; i++)
            {
                if (previousProcessCount[i] == 0)
                {
                    startingProcessesID.Add(i);
                }
            }

            // wrap to one method those 2 below

            for(int i=0; i< startingProcessesID.Count; i++)
            {
                int startingProcessID = startingProcessesID[i];
                if (visited[startingProcessID] == true) continue;

                List<UIMollierProcess> newProcessSystem = new List<UIMollierProcess>();
                int currentProcessID = startingProcessID;

                while (visited[currentProcessID] == false)
                {
                    newProcessSystem.Add(mollierProcesses[currentProcessID]);
                    visited[currentProcessID] = true;

                    if (nextProcessID[currentProcessID] == none) break;
                    currentProcessID = nextProcessID[currentProcessID];
                }
                result.Add(newProcessSystem);
            }


            for(int i=0; i<mollierProcesses.Count; i++)
            {
                if (visited[i] == true) continue;

                List<UIMollierProcess> newProcessSystem = new List<UIMollierProcess>();
                int currentProcessID = i;

                while (visited[currentProcessID] == false)
                {
                    newProcessSystem.Add(mollierProcesses[currentProcessID]);
                    visited[currentProcessID] = true;

                    if (nextProcessID[currentProcessID] == none) break;
                    currentProcessID = nextProcessID[currentProcessID];
                }
                result.Add(newProcessSystem);
            }

            return result;

        }

    }
}
