using SAM.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SAM.Core.UI.WPF
{
    public static partial class Modify
    {
        
        
        public static void AddParameterNames(this ComboBox comboBox, IEnumerable<object> objects, Type type = null, IEnumerable<string> parameterNames_ToBeRemoved = null, IEnumerable<string> parameterNames_ToBeAdded = null)
        {
            if(comboBox == null || objects == null)
            {
                return;
            }

            HashSet<string> parameterNames = new HashSet<string>();
            foreach (object @object in objects)
            {
                Core.Query.UserFriendlyNames(@object)?.ForEach(x => parameterNames.Add(x));

            }

            if(parameterNames_ToBeAdded != null)
            {
                foreach (string parameterName in parameterNames_ToBeAdded)
                {
                    parameterNames.Add(parameterName);
                }
            }

            List<string> parameterNames_ToBeRemoved_Temp = new List<string>();
            if(parameterNames_ToBeRemoved != null)
            {
                parameterNames_ToBeRemoved_Temp.AddRange(parameterNames_ToBeRemoved);
            }

            if (type != null)
            {
                foreach (Enum @enum in Core.Query.Enums(type))
                {
                    ParameterProperties parameterProperties = ParameterProperties.Get(@enum);
                    if (parameterProperties == null)
                    {
                        continue;
                    }

                    string name = parameterProperties.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    ParameterValue parameterValue = ParameterValue.Get<ParameterValue>(@enum);
                    if (parameterValue != null)
                    {
                        if (parameterValue.ParameterType == ParameterType.IJSAMObject)
                        {
                            parameterNames_ToBeRemoved_Temp.Add(name);
                            continue;
                        }
                    }

                    parameterNames.Add(name);
                }
            }

            parameterNames_ToBeRemoved_Temp.ForEach(x => parameterNames.Remove(x));

            if (parameterNames != null && parameterNames.Count != 0)
            {
                List<string> parameterNames_Sorted = parameterNames.ToList();
                parameterNames_Sorted.Sort();

                foreach (string parameterName in parameterNames_Sorted)
                {
                    comboBox.Items.Add(parameterName);
                }
            }
        }
    }
}