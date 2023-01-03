using System;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public class ClosedEventArgs : ModifiedEventArgs
    {
        public ClosedEventArgs()
            :base(new FullModification())
        {

        }
    }
}
