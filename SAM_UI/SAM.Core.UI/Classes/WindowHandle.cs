using System;
using System.Windows.Forms;

namespace SAM.Core.UI
{
    public class WindowHandle : IWin32Window
    {
        IntPtr handle;

        public WindowHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
        }
    }
}
