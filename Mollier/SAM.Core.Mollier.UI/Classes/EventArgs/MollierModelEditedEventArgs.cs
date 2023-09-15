using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class MollierModelEditedEventArgs : EventArgs
    {
        public MollierModel MollierModel { get; private set; }

        public MollierModelEditedEventArgs(MollierModel mollierModel)
            :base()
        {
            MollierModel = mollierModel;
        }


    }
}
