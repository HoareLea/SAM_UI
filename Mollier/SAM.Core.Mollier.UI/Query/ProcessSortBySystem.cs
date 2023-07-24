using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public static partial class Query
    {
        public static List<List<UIMollierProcess>> ProcessSortBySystem(List<UIMollierProcess> mollierProcesses)
        {
            List<List<UIMollierProcess>> result = new List<List<UIMollierProcess>>();
            List<List<int>> resultID = new List<List<int>>();//list 0 3 2 4 means that there should be processes in sequence are indexes 0 3 2 4 in mollierprocesses
            List<int> nextProcessID = new List<int>();

            int none = -1; 
            for(int i=0; i<mollierProcesses.Count; i++)
            {
                MollierPoint end = mollierProcesses[i].End;
                nextProcessID.Add(none);

                if (end == null) continue;

                for (int j = 0; j < mollierProcesses.Count; j++)
                {
                    MollierPoint start = mollierProcesses[j].Start;
                    if (start == null || i == j) continue;

                    if(start == end)
                    {
                        
                    }
                }
            }


            for (int i=0; i<mollierProcesses.Count; i++)
            {
                //if there is no end that has the same value as my start then my start is start of the new process
                MollierPoint start = mollierProcesses[i].Start;
                if(start == null)
                {
                    continue;
                }
                int count = 0;
                for(int j=0; j<mollierProcesses.Count; j++)
                {
                    MollierPoint end = mollierProcesses[j].End;
                    if (i == j || end == null)
                    {
                        continue;
                    }
                    if(System.Math.Round(start.HumidityRatio,6) == System.Math.Round(end.HumidityRatio, 6) && System.Math.Round(start.DryBulbTemperature, 2) == System.Math.Round(end.DryBulbTemperature, 2))
                    {
                        count++;
                        break;
                    }   
                }
                if(count != 0)
                {
                    continue;
                }
                resultID.Add(new List<int>());
                result.Add(new List<UIMollierProcess>());
                resultID[result.Count - 1].Add(i);
            }
            List<int> sonID = new List<int>();//son is the process which is after me
            for(int i=0; i < mollierProcesses.Count; i++)
            {
                MollierPoint end = mollierProcesses[i].End;
                if (mollierProcesses[i].MollierProcess is UndefinedProcess || end == null)
                {
                    sonID.Add(-1);
                    continue;
                }
                int count = 0;
                for(int j=0; j < mollierProcesses.Count; j++)
                {
                    MollierPoint start = mollierProcesses[j].Start;
                    if(i == j || start == null)
                    {
                        continue;
                    }
                    if (System.Math.Round(start.HumidityRatio, 6) == System.Math.Round(end.HumidityRatio, 6) && System.Math.Round(start.DryBulbTemperature, 2) == System.Math.Round(end.DryBulbTemperature, 2))
                    {
                        sonID.Add(j);
                        count++;
                        break;
                    }
                }
                if (count == 0)
                {
                    sonID.Add(-1);//it means that i dont have any sons
                }
            }

            for(int i=0; i<result.Count; i++)
            {
                List<int> system = resultID[i];
                result[i].Add(mollierProcesses[system[0]]);
                int ID = sonID[system[0]];
                while(ID != -1)
                {
                    result[i].Add(mollierProcesses[ID]);
                    ID = sonID[ID];
                }
            }
            return result;
        }    
    }
}
