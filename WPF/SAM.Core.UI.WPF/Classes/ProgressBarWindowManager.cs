using System;
using System.Threading;
using System.Windows.Threading;

namespace SAM.Core.UI.WPF
{
    public class ProgressBarWindowManager: IDisposable
    {
        public string Title { get; set; }
        public string Text { get; set; }

        private ProgressBarWindow progressBarWindow;

        public ProgressBarWindowManager()
        {

        }

        public ProgressBarWindowManager(string title, string text, bool showDialog = true)
        {
            Title = title;
            Text = text;

            if(showDialog)
            {
                Show(title, text);
            }
        }

        private static void Close(System.Windows.Window window)
        {
            if (window == null)
            {
                return;
            }

            if (window.Dispatcher.CheckAccess())
            {
                window.Close();
            }
            else
            {
                window.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(window.Close));
            }

            window = null;
        }

        public void Show(string title, string text)
        {
            Close(progressBarWindow);

            Thread thread = new Thread(new ThreadStart(() =>
            {
                progressBarWindow = new ProgressBarWindow(title, text);
                progressBarWindow.ShowDialog();
                Dispatcher.Run();
            }));

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Show()
        {
            Show(Title, Text);
        }

        public void Close()
        {
            Close(progressBarWindow);
        }

        public void Dispose()
        {
            Close();
        }
        ~ProgressBarWindowManager()
        {
            Dispose();
        }
    }
}
