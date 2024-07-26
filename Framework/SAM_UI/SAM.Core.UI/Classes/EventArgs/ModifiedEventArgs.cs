using System;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public class ModifiedEventArgs : EventArgs
    {
        public List<IModification> Modifications { get; }

        public ModifiedEventArgs()
        {
            Modifications = new List<IModification>() { new FullModification() };
        }
        
        public ModifiedEventArgs(IEnumerable<IModification> modifications)
        {
            Modifications = modifications == null ? null : new List<IModification>(modifications);
        }

        public ModifiedEventArgs(IModification modification)
        {
            if(modification != null)
            {
                Modifications = new List<IModification>() { modification };
            }
        }

        public List<T> GetModifications<T>( Func<T, bool> func = null) where T : IModification
        {
            if(Modifications == null || Modifications.Count == 0)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach(IModification modification in Modifications)
            {
                if(!(modification is T))
                {
                    continue;
                }

                T t = (T)modification;

                if(func != null && !func.Invoke(t))
                {
                    continue;
                }

                result.Add(t);
            }

            return result;

        }
    }
}
