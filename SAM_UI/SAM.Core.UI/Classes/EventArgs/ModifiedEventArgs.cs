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
    }
}
