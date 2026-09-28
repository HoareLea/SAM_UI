// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Threading;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// The one progress window of a long Part O operation - see <see cref="PartOProgressHost"/>, which owns
    /// its thread. It reads <see cref="PartOProgressState"/> on a timer, so the thread doing the work never has
    /// to reach it, and a stage the work announces appears within half a second.
    /// </summary>
    public partial class PartOProgressWindow : System.Windows.Window
    {
        private readonly DispatcherTimer dispatcherTimer;

        private PartOProgressState partOProgressState;

        private bool cancellable = true;

        private bool cancelRequested;

        public PartOProgressWindow()
        {
            InitializeComponent();

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500),
            };

            dispatcherTimer.Tick += (s, e) => Render();

            Loaded += (s, e) =>
            {
                Render();
                dispatcherTimer.Start();
            };

            Closed += (s, e) => dispatcherTimer.Stop();
        }

        /// <summary>Raised on this window's thread when Cancel is pressed. Once.</summary>
        public event EventHandler CancelRequested;

        public string Heading
        {
            get => textBlock_Heading.Text;
            set => textBlock_Heading.Text = value;
        }

        public string Subheading
        {
            get => textBlock_Subheading.Text;
            set
            {
                textBlock_Subheading.Text = value;
                textBlock_Subheading.Visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows Cancel. Either way the note stays, because it also answers whether the operation can be
        /// stopped at all - "It cannot be cancelled" is information, not an absence.
        /// </summary>
        public bool Cancellable
        {
            get => cancellable;
            set
            {
                cancellable = value;
                button_Cancel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                Render();
            }
        }

        public PartOProgressState State
        {
            get => partOProgressState;
            set
            {
                partOProgressState = value;
                Render();
            }
        }

        /// <summary>Re-reads the state. Public so a test can render a window without waiting on its timer.</summary>
        public void Render()
        {
            if (partOProgressState is null)
            {
                return;
            }

            itemsControl_Stages.ItemsSource = Create.ProgressStageRows(partOProgressState);

            string detail = partOProgressState.Detail;
            textBlock_Detail.Text = detail ?? string.Empty;
            textBlock_Detail.Visibility = string.IsNullOrWhiteSpace(detail) ? Visibility.Collapsed : Visibility.Visible;

            //Determinate only on a real count; otherwise the bar moves without claiming a position.
            double? fraction = partOProgressState.Fraction;
            progressBar.IsIndeterminate = !fraction.HasValue;
            progressBar.Value = fraction ?? 0;

            string percent = partOProgressState.Percent;
            textBlock_Percent.Text = percent ?? string.Empty;
            textBlock_Percent.Visibility = percent is null ? Visibility.Collapsed : Visibility.Visible;

            textBlock_Elapsed.Text = partOProgressState.ElapsedText;

            //Offered only where a click is certain to be acted on. Withdrawn, it stays in view - disabled, with
            //the note saying when it is offered - so the answer to "can I stop this?" is still on screen.
            bool cancelAvailable = partOProgressState.CancelAvailable;

            if (!cancelRequested)
            {
                button_Cancel.IsEnabled = cancelAvailable;
                button_Cancel.ToolTip = cancelAvailable ? null : "Offered while a TAS simulation is being prepared or run.";
            }

            textBlock_Note.Text = PartOProgressState.Note(fraction.HasValue, cancellable, cancelRequested, cancelAvailable);
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            //Also refused here, not only by the disabled button: a click already queued when Cancel was
            //withdrawn must not latch a request nothing will observe.
            if (cancelRequested || partOProgressState is null || !partOProgressState.CancelAvailable)
            {
                return;
            }

            cancelRequested = true;

            button_Cancel.IsEnabled = false;
            button_Cancel.Content = "Cancelling…";

            Render();

            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
