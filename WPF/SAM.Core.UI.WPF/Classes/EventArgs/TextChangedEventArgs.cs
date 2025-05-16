using System;

namespace SAM.Core.UI.WPF
{
    public class TextChangedEventArgs : EventArgs
    {
        public string Text { get; }

        public TextChangedEventArgs(string text)
        {
            Text = text;
        }
    }
}
