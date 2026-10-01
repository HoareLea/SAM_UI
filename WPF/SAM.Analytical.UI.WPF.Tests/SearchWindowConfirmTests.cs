// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SAM.Core.UI.WPF;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// <see cref="SearchWindow"/> - the "Select Construction" / "Select Filter" / ... picker - can be
    /// confirmed. Before this it had an OK button with no Click handler and no IsDefault, and nothing wired
    /// to double-click, so every <c>ShowDialog() != true</c> caller (Assign Construction, Assign Aperture
    /// Construction, Assign Internal Condition, the Create Case controls, ...) could only ever be closed with
    /// Cancel and the choice was never applied.
    /// <para>
    /// Each test opens a real modal window and drives it from the dispatcher once it has rendered. A watchdog
    /// closes the window if a confirm path is broken, so a regression fails the assertion instead of hanging
    /// the run.
    /// </para>
    /// </summary>
    [Collection(WpfCollection.Name)]
    public class SearchWindowConfirmTests
    {
        private sealed class Item
        {
            public Item(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private static SearchWindow Create(out List<Item> items, SelectionMode selectionMode = SelectionMode.Single)
        {
            items = new List<Item> { new Item("Brick"), new Item("Concrete"), new Item("Timber") };

            return new SearchWindow(items, x => (x as Item)?.Name) { SelectionMode = selectionMode };
        }

        private static T Field<T>(object instance, string name) where T : class
        {
            FieldInfo fieldInfo = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return fieldInfo?.GetValue(instance) as T;
        }

        private static ListBox ListBoxOf(SearchWindow window)
        {
            return Field<ListBox>(Field<SearchControl>(window, "SearchControl_Main"), "ListBox_Main");
        }

        private static Button OKOf(SearchWindow window)
        {
            return Field<Button>(window, "button_OK");
        }

        private static void Click(Button button)
        {
            // The automation Invoke path raises Button.Click exactly as a mouse click does.
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
        }

        /// <summary>
        /// Shows the window modally and runs <paramref name="drive"/> once it has rendered. Returns what
        /// <c>ShowDialog()</c> returned - the value every production caller tests.
        /// </summary>
        private static bool? Show(SearchWindow window, Action drive)
        {
            // An assertion failing inside the dispatcher callback would take the test host down rather than
            // fail the test, so it is captured, the window is closed, and it is rethrown from the test thread.
            Exception failure = null;
            window.ContentRendered += (sender, e) => window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    drive();
                }
                catch (Exception exception)
                {
                    failure = exception;
                    window.Close();
                }
            }));

            DispatcherTimer watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            watchdog.Tick += (sender, e) =>
            {
                watchdog.Stop();
                window.Close();
            };
            watchdog.Start();

            bool? result;
            try
            {
                result = window.ShowDialog();
            }
            finally
            {
                watchdog.Stop();
            }

            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }

            return result;
        }

        /// <summary>
        /// The reported failure, end to end: select one item, click OK, and the dialog closes with
        /// <c>true</c> and hands that item back.
        /// </summary>
        [WpfFact]
        public void SelectingAnItemAndClickingOK_ClosesWithTrue_AndReturnsTheItem()
        {
            SearchWindow window = Create(out List<Item> items);
            ListBox listBox = ListBoxOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItem = "Concrete";
                Click(OKOf(window));
            });

            Assert.True(result);
            List<Item> selected = window.GetSelectedItems<Item>();
            Assert.Single(selected);
            Assert.Same(items[1], selected[0]);
        }

        /// <summary>
        /// With nothing selected there is nothing to return, so OK is disabled and cannot close the dialog
        /// with <c>true</c>; selecting enables it and clearing the selection disables it again.
        /// </summary>
        [WpfFact]
        public void OK_IsEnabledOnlyWhileAnItemIsSelected()
        {
            SearchWindow window = Create(out _);
            ListBox listBox = ListBoxOf(window);
            Button ok = OKOf(window);

            Assert.False(ok.IsEnabled);

            bool? result = Show(window, () =>
            {
                Assert.False(ok.IsEnabled);

                listBox.SelectedItem = "Brick";
                Assert.True(ok.IsEnabled);

                listBox.SelectedItem = null;
                Assert.False(ok.IsEnabled);

                // Invoking a disabled button is a no-op in WPF, so the dialog stays open until the
                // watchdog (or Cancel) closes it.
                window.Close();
            });

            Assert.NotEqual(true, result);
        }

        /// <summary>
        /// Typing in the search box rebuilds the list; a selected item that the filter removes must take OK
        /// down with it, otherwise OK would confirm an item the user can no longer see.
        /// </summary>
        [WpfFact]
        public void FilteringAwayTheSelectedItem_DisablesOK()
        {
            SearchWindow window = Create(out _);
            ListBox listBox = ListBoxOf(window);
            Button ok = OKOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItem = "Brick";
                Assert.True(ok.IsEnabled);

                // The filter only applies from three characters.
                window.SearchText = "Timber";
                Assert.Single(listBox.Items);
                Assert.False(ok.IsEnabled);

                window.Close();
            });

            Assert.NotEqual(true, result);
        }

        /// <summary>
        /// Double-clicking an item confirms it, as in every other picker. <see cref="Control.MouseDoubleClick"/>
        /// is a direct routed event - it reaches only the element clicked and never bubbles to the window - so
        /// the event is raised on the <see cref="ListBoxItem"/>, exactly where <see cref="Control"/> raises it
        /// for a real double-click, and must still reach the window through the control's own wiring.
        /// </summary>
        [WpfFact]
        public void DoubleClickingAnItem_ClosesWithTrue_AndReturnsTheItem()
        {
            SearchWindow window = Create(out List<Item> items);
            ListBox listBox = ListBoxOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItem = "Timber";
                RaiseDoubleClick((ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem("Timber"));
            });

            Assert.True(result);
            Assert.Same(items[2], Assert.Single(window.GetSelectedItems<Item>()));
        }

        /// <summary>
        /// A double-click that is not on an item - here the list's own empty area, which is how the search box
        /// and the scroll bar behave too - must not confirm, even when an item happens to be selected.
        /// </summary>
        [WpfFact]
        public void DoubleClickingOutsideAnItem_DoesNotConfirm()
        {
            SearchWindow window = Create(out _);
            ListBox listBox = ListBoxOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItem = "Brick";
                RaiseDoubleClick(listBox);

                Assert.True(window.IsVisible);
                window.Close();
            });

            Assert.NotEqual(true, result);
        }

        private static void RaiseDoubleClick(Control target)
        {
            target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Control.MouseDoubleClickEvent,
                Source = target
            });
        }

        /// <summary>
        /// Enter confirms from anywhere in the dialog, including the list, via the default button; Escape and
        /// the Cancel button still cancel. The Enter key itself cannot be injected into a modal window from a
        /// test, so the declaration WPF acts on is asserted instead.
        /// </summary>
        [WpfFact]
        public void OK_IsTheDefaultButton_AndCancelStaysTheCancelButton()
        {
            SearchWindow window = Create(out _);

            Assert.True(OKOf(window).IsDefault);
            Assert.False(OKOf(window).IsCancel);
            Assert.True(Field<Button>(window, "button_Cancel").IsCancel);
        }

        /// <summary>
        /// Cancel is unchanged: it closes the dialog with a result that is not <c>true</c>, so callers do not
        /// apply a selection that was made and then abandoned.
        /// </summary>
        [WpfFact]
        public void Cancel_ClosesWithoutTrue_EvenWithAnItemSelected()
        {
            SearchWindow window = Create(out _);
            ListBox listBox = ListBoxOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItem = "Brick";
                Click(Field<Button>(window, "button_Cancel"));
            });

            Assert.NotEqual(true, result);
        }

        /// <summary>
        /// A multi-select picker confirms with every selected item.
        /// </summary>
        [WpfFact]
        public void MultiSelect_ConfirmsWithAllSelectedItems()
        {
            SearchWindow window = Create(out List<Item> items, SelectionMode.Multiple);
            ListBox listBox = ListBoxOf(window);

            bool? result = Show(window, () =>
            {
                listBox.SelectedItems.Add("Brick");
                listBox.SelectedItems.Add("Timber");
                Click(OKOf(window));
            });

            Assert.True(result);
            List<Item> selected = window.GetSelectedItems<Item>();
            Assert.Equal(2, selected.Count);
            Assert.Contains(items[0], selected);
            Assert.Contains(items[2], selected);
        }
    }
}
