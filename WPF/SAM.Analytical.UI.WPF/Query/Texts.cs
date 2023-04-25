using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.UI.WPF
{
    public static partial class Query
    {
        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, SpaceParameter spaceParameter)
        {
            return Texts(internalConditionDatas, (Enum)spaceParameter);
        }

        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, InternalConditionParameter internalConditionParameter)
        {
            return Texts(internalConditionDatas, (Enum)internalConditionParameter);
        }

        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, InternalConditionParameter internalConditionParameter, double factor, double tolerance = Core.Tolerance.Distance)
        {
            return Texts(internalConditionDatas, (Enum)internalConditionParameter, factor, tolerance);
        }

        public static List<string> Texts(this IEnumerable<InternalCondition> internalConditions, InternalConditionParameter internalConditionParameter)
        {
            if(internalConditions == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(InternalCondition internalCondition in internalConditions)
            {
                if(internalCondition == null)
                {
                    continue;
                }

                if (!internalCondition.TryGetValue(internalConditionParameter, out string value, true))
                {
                    continue;
                }

                result.Add(value);
            }


            return result;
        }

        public static List<string> Texts(this IEnumerable<InternalCondition> internalConditions, InternalConditionParameter internalConditionParameter, double factor, double tolerance = Core.Tolerance.Distance)
        {
            if (internalConditions == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach (InternalCondition internalCondition in internalConditions)
            {
                if (internalCondition == null)
                {
                    continue;
                }

                if (!internalCondition.TryGetValue(internalConditionParameter, out string value, true))
                {
                    continue;
                }

                if (Core.Query.TryConvert(value, out double @double) && !double.IsNaN(@double))
                {
                    value = Core.Query.Round(@double * factor, tolerance).ToString();
                }

                result.Add(value);
            }


            return result;
        }

        public static List<string> Texts(this IEnumerable<double> values)
        {
            if(values == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(double value in values)
            {
                if (double.IsNaN(value))
                {
                    result.Add(null);
                }
                else
                {
                    result.Add(value.ToString());
                }

            }

            return result;
        }

        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, VentilationSystemParameter ventilationSystemParameter)
        {
            if(internalConditionDatas == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                List<VentilationSystem> ventilationSystems = internalConditionData?.MechanicalSystems<VentilationSystem>(MechanicalSystemCategory.Ventilation);
                if(ventilationSystems == null)
                {
                    continue;
                }

                foreach(VentilationSystem ventilationSystem in ventilationSystems)
                {
                    if(ventilationSystem == null)
                    {
                        continue;
                    }

                    if(!ventilationSystem.TryGetValue(ventilationSystemParameter, out string value, true))
                    {
                        continue;
                    }

                    result.Add(value);
                }
            }

            return result;
        }

        public static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, MechanicalSystemCategory mechanicalSystemCategory)
        {
            if(internalConditionDatas == null)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                List<MechanicalSystem> mechanicalSystems = internalConditionData?.MechanicalSystems<MechanicalSystem>(mechanicalSystemCategory);
                if(mechanicalSystems == null || mechanicalSystems.Count == 0)
                {
                    continue;
                }

                foreach(MechanicalSystem mechanicalSystem in mechanicalSystems)
                {
                    if(mechanicalSystem == null)
                    {
                        continue;
                    }

                    string fullName = mechanicalSystem.FullName;

                    result.Add(fullName);
                }
            }

            return result;
        }

        private static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, Enum @enum)
        {
            if(internalConditionDatas == null)
            {
                return null;
            }

            HashSet<string> result = new HashSet<string>();
            foreach(InternalConditionData internalConditionData in internalConditionDatas)
            {
                if(!internalConditionData.TryGetValue(@enum as dynamic, out string value, true))
                {
                    result.Add(null);
                }
                else
                {
                    result.Add(value);
                }


            }

            return result.ToList();
        }

        private static List<string> Texts(this IEnumerable<InternalConditionData> internalConditionDatas, Enum @enum, double factor, double tolerance = Core.Tolerance.Distance)
        {
            if (internalConditionDatas == null)
            {
                return null;
            }

            HashSet<string> result = new HashSet<string>();
            foreach (InternalConditionData internalConditionData in internalConditionDatas)
            {
                if (!internalConditionData.TryGetValue(@enum as dynamic, out string value, true))
                {
                    result.Add(null);
                }
                else
                {
                    if(Core.Query.TryConvert(value, out double @double) && !double.IsNaN(@double))
                    {
                        value = Core.Query.Round(@double * factor, tolerance).ToString();
                    }
                    
                    result.Add(value);
                }


            }

            return result.ToList();
        }
    }
}