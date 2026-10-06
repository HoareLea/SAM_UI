// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SAM.Core.UI.WPF
{
    /// <summary>
    /// Attached behaviours for <see cref="DataGrid"/>.
    /// </summary>
    public static class DataGridBehavior
    {
        /// <summary>
        /// When set to true on a <see cref="DataGrid"/>, entering edit mode on a cell selects the
        /// whole editing text so that typing replaces it (matching the old WinForms DataGridView
        /// behaviour) instead of inserting at the caret. Works for both <c>DataGridTextColumn</c>
        /// and <c>DataGridTemplateColumn</c> cells that host a <see cref="TextBox"/>.
        /// </summary>
        public static readonly System.Windows.DependencyProperty SelectAllOnEditProperty = System.Windows.DependencyProperty.RegisterAttached(
            "SelectAllOnEdit",
            typeof(bool),
            typeof(DataGridBehavior),
            new PropertyMetadata(false, OnSelectAllOnEditChanged));

        public static bool GetSelectAllOnEdit(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(SelectAllOnEditProperty);
        }

        public static void SetSelectAllOnEdit(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(SelectAllOnEditProperty, value);
        }

        private static void OnSelectAllOnEditChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not DataGrid dataGrid)
            {
                return;
            }

            // Detach first to avoid double subscription when the property is re-set.
            dataGrid.RemoveHandler(UIElement.GotKeyboardFocusEvent, (KeyboardFocusChangedEventHandler)OnGotKeyboardFocus);

            if (e.NewValue is bool value && value)
            {
                // handledEventsToo: true so we still see focus changes even when the DataGrid marks
                // the event handled internally.
                dataGrid.AddHandler(UIElement.GotKeyboardFocusEvent, (KeyboardFocusChangedEventHandler)OnGotKeyboardFocus, true);
            }
        }

        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // The element that just received keyboard focus - the editing TextBox when a cell enters
            // edit mode (directly for DataGridTextColumn, or nested for template columns).
            TextBox textBox = e.NewFocus as TextBox ?? FindVisualChild<TextBox>(e.NewFocus as DependencyObject);
            if (textBox == null)
            {
                return;
            }

            // Immediate select-all: correct for keyboard / type-to-edit entry (the select happens
            // before the typed character is inserted, so the character replaces the whole value).
            textBox.SelectAll();

            // When the edit was started with the mouse, the mouse-UP that follows this focus change
            // repositions the caret and clears the selection. A dispatcher callback queued now would
            // run before that mouse-up is even delivered, so we cannot simply defer. Instead, hook
            // the very next mouse-up on this TextBox and re-select once, then detach.
            MouseButtonEventHandler onMouseUp = null;
            KeyboardFocusChangedEventHandler onLostFocus = null;

            onMouseUp = (mouseSender, mouseArgs) =>
            {
                textBox.PreviewMouseLeftButtonUp -= onMouseUp;
                textBox.LostKeyboardFocus -= onLostFocus;
                // Re-select after the caret has been positioned by the mouse-up.
                textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()), DispatcherPriority.Input);
            };

            onLostFocus = (focusSender, focusArgs) =>
            {
                // Keyboard / F2 entry: no mouse-up arrives, so clean up when focus leaves.
                textBox.PreviewMouseLeftButtonUp -= onMouseUp;
                textBox.LostKeyboardFocus -= onLostFocus;
            };

            textBox.PreviewMouseLeftButtonUp += onMouseUp;
            textBox.LostKeyboardFocus += onLostFocus;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            if (parent is T typed)
            {
                return typed;
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                T result = FindVisualChild<T>(VisualTreeHelper.GetChild(parent, i));
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
