using System;
using System.Collections.Generic;

namespace SAM.Core.UI
{
    public class OpenedEventArgs : ModifiedEventArgs
    {
        public OpenedEventArgs()
            :base(new FullModification())
        {

        }
    }
}
