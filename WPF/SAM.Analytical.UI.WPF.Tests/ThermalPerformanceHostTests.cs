// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The Thermal Performance panel has one control and one view-model and two possible hosts: a column of the analytical window
    /// (docked, the default) or a modeless window of its own (floating, e.g. on a second monitor). These tests keep the host a
    /// pure choice of parent: the same control changes parent and keeps its state, asks for the other host through one event, and
    /// a floating window reopens where it was left only when that is still reachable on a monitor.
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class ThermalPerformanceHostTests
    {
        private static readonly Rect Primary = new Rect(0, 0, 1920, 1040);
        private static readonly Rect Second = new Rect(1920, 0, 1920, 1040);
        private static readonly Rect Owner = new Rect(0, 0, 1920, 1040);
        private static readonly Size Default = new Size(340, 600);

        private static void Flush()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        // ---- Where a floating window opens ------------------------------------------------------------------------

        [Fact]
        public void A_first_floating_window_opens_beside_the_right_edge_of_the_analytical_window_on_its_monitor()
        {
            Rect bounds = Query.ThermalFloatingBounds(null, Owner, new[] { Primary, Second }, Default);

            Assert.Equal(Default.Width, bounds.Width);
            Assert.Equal(Owner.Right - Default.Width - 24, bounds.Left);
            Assert.True(Primary.Contains(bounds));
        }

        [Fact]
        public void A_window_left_on_the_second_monitor_reopens_there()
        {
            Rect remembered = new Rect(2400, 120, 360, 700);

            Assert.Equal(remembered, Query.ThermalFloatingBounds(remembered, Owner, new[] { Primary, Second }, Default));
        }

        [Fact]
        public void A_window_left_on_a_monitor_that_is_gone_reopens_on_the_monitor_of_the_analytical_window()
        {
            Rect remembered = new Rect(2400, 120, 360, 700);

            Rect bounds = Query.ThermalFloatingBounds(remembered, Owner, new[] { Primary }, Default);

            Assert.True(Primary.Contains(bounds));
            Assert.Equal(remembered.Width, bounds.Width);
            Assert.Equal(remembered.Height, bounds.Height);
        }

        [Fact]
        public void A_window_that_sticks_out_but_can_still_be_grabbed_is_left_where_it_is()
        {
            Rect remembered = new Rect(1800, 100, 360, 700); // 120 px of its title strip are on the primary monitor

            Assert.Equal(remembered, Query.ThermalFloatingBounds(remembered, Owner, new[] { Primary }, Default));
        }

        [Fact]
        public void A_window_taller_than_the_monitor_is_shrunk_to_fit_it()
        {
            Rect bounds = Query.ThermalFloatingBounds(new Rect(5000, 5000, 400, 3000), Owner, new[] { Primary }, Default);

            Assert.True(Primary.Contains(bounds));
            Assert.Equal(Primary.Height, bounds.Height);
        }

        [Fact]
        public void Without_monitor_information_the_window_still_opens_inside_the_analytical_window()
        {
            Rect bounds = Query.ThermalFloatingBounds(null, Owner, new List<Rect>(), Default);

            Assert.True(Owner.Contains(bounds));
        }

        // ---- One control, two hosts ------------------------------------------------------------------------------------

        [WpfFact]
        public void The_same_control_moves_between_a_grid_and_the_floating_window_and_keeps_its_view_model_and_mode()
        {
            ThermalPerformanceControl control = new ThermalPerformanceControl();
            ThermalPerformanceViewModel viewModel = control.ViewModel;
            Grid grid = new Grid();
            grid.Children.Add(control);

            control.ViewModel.Mode = ThermalPerformanceMode.WholeEnvelope;

            // Float: the control leaves the grid, the window adopts it.
            grid.Children.Remove(control);
            control.IsFloating = true;
            ThermalPerformanceWindow window = new ThermalPerformanceWindow(control) { Left = 0, Top = 0 };
            window.Show();
            Flush();

            Assert.Same(window, System.Windows.Window.GetWindow(control));
            Assert.Same(viewModel, control.ViewModel);
            Assert.Equal(ThermalPerformanceMode.WholeEnvelope, control.ViewModel.Mode);

            // Dock: the window lets go, the grid takes it back.
            window.ClosingByHost = true;
            window.Content = null;
            window.Close();
            control.IsFloating = false;
            grid.Children.Add(control);
            Flush();

            Assert.Same(grid, control.Parent);
            Assert.Same(viewModel, control.ViewModel);
            Assert.Equal(ThermalPerformanceMode.WholeEnvelope, control.ViewModel.Mode);
        }

        [WpfFact]
        public void The_host_button_asks_for_the_other_host_and_its_wording_follows()
        {
            ThermalPerformanceControl control = new ThermalPerformanceControl();
            System.Windows.Window window = new System.Windows.Window { Content = control, Left = 0, Top = 0, ShowActivated = false };
            window.Show();
            Flush();

            Button button = (Button)control.FindName("button_Host");
            List<ThermalPerformanceHost> asked = new List<ThermalPerformanceHost>();
            control.HostRequested += (sender, e) => asked.Add(e.Host);

            Assert.Equal("Undock", button.Content);
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();

            control.IsFloating = true;
            Assert.Equal("Dock", button.Content);
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            Flush();

            Assert.Equal(new[] { ThermalPerformanceHost.Floating, ThermalPerformanceHost.Docked }, asked);

            window.Close();
        }

        [WpfFact]
        public void Closing_the_floating_window_by_hand_is_reported_but_closing_it_from_the_host_is_not()
        {
            ThermalPerformanceWindow byUser = new ThermalPerformanceWindow(new ThermalPerformanceControl()) { Left = 0, Top = 0 };
            int reported = 0;
            byUser.ClosedByUser += (sender, e) => reported++;
            byUser.Show();
            byUser.Close();

            ThermalPerformanceWindow byHost = new ThermalPerformanceWindow(new ThermalPerformanceControl()) { Left = 0, Top = 0 };
            byHost.ClosedByUser += (sender, e) => reported++;
            byHost.Show();
            byHost.ClosingByHost = true;
            byHost.Close();

            Assert.Equal(1, reported);
        }
    }
}
