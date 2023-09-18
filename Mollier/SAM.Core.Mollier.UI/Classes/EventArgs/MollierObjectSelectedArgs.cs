using System;
using System.Collections.Generic;

namespace SAM.Core.Mollier.UI
{
    public class MollierObjectSelectedArgs : EventArgs
    {
        public IMollierObject MollierObject { get; private set; }

        public MollierObjectSelectedArgs(IMollierObject mollierObject)
            :base()
        {
            MollierObject = mollierObject;
        }


    }
}
