using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Core.Mollier.UI
{
    public class UserVisibilitySetting : VisibilitySetting
    {

        public UserVisibilitySetting(Color color)
            :base(color)
        {

        }

        public bool FromJObject(JObject jObject)
        {
            throw new NotImplementedException();
        }

        public JObject ToJObject()
        {
            throw new NotImplementedException();
        }
    }
}
