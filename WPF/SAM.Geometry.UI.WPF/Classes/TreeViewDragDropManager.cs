// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAM.Geometry.UI.WPF
{
    public class TreeViewDragDropManager
    {
        private TreeView treeView;
        
        private Point lastMouseDown;
        
        private TreeViewItem selectedTreeViewItem;
        private TreeViewItem targetTreeViewItem;

        public event TreeViewItemDroppedEventHandler TreeViewItemDropped;

        public TreeViewDragDropManager(TreeView treeView)
        {
            SetTreeView(treeView);
        }

        public TreeView TreeView
        {
            get
            {
                return GetTreeView();
            }
        }

        private void SetTreeView(TreeView treeView)
        {
            if(this.treeView == treeView)
            {
                return;
            }

           if(this.treeView != null)
            {
                treeView.DragOver -= TreeView_DragOver;
                treeView.Drop -= TreeView_Drop;
                treeView.MouseMove -= TreeView_MouseMove;
                treeView.PreviewMouseDown -= TreeView_PreviewMouseDown; 
                treeView.PreviewMouseUp += TreeView_PreviewMouseUp;
            }
            
            this.treeView = treeView;
            treeView.AllowDrop = true;
            treeView.DragOver += TreeView_DragOver;
            treeView.Drop += TreeView_Drop;
            treeView.MouseMove += TreeView_MouseMove;
            treeView.PreviewMouseDown += TreeView_PreviewMouseDown;
            treeView.PreviewMouseUp += TreeView_PreviewMouseUp;
        }

        private void TreeView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            targetTreeViewItem = null;
            selectedTreeViewItem = null;
        }

        private void TreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            targetTreeViewItem = null;
            selectedTreeViewItem = null;

            if (e.ChangedButton == MouseButton.Left)
            {
                lastMouseDown = e.GetPosition(treeView);
                selectedTreeViewItem = Core.UI.WPF.Query.DependencyObject<TreeViewItem>(treeView, lastMouseDown);
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(treeView);

                if ((Math.Abs(currentPosition.X - lastMouseDown.X) > 10.0) || (Math.Abs(currentPosition.Y - lastMouseDown.Y) > 10.0))
                {
                    //selectedTreeViewItem = (TreeViewItem)treeView.SelectedItem;
                    if (selectedTreeViewItem != null)
                    {
                        DragDropEffects dragDropEffects = DragDrop.DoDragDrop(treeView, treeView.SelectedValue, DragDropEffects.Move);
                        //Checking target is not null and item is dragging(moving)
                        if ((dragDropEffects == DragDropEffects.Move) && (targetTreeViewItem != null))
                        {
                            // A Move drop was accepted
                            if (IsValid(selectedTreeViewItem, targetTreeViewItem))
                            {
                                TreeViewItemDroppedEventArgs treeViewItemDroppedEventArgs = new TreeViewItemDroppedEventArgs(selectedTreeViewItem, targetTreeViewItem);
                                TreeViewItemDropped?.Invoke(treeView, treeViewItemDroppedEventArgs);
                                if (treeViewItemDroppedEventArgs.EventResult == EventResult.Succeeded)
                                {
                                    CopyItem(selectedTreeViewItem, targetTreeViewItem);
                                    targetTreeViewItem = null;
                                    selectedTreeViewItem = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;

            // Verify that this is a valid drop and then store the drop target
            TreeViewItem treeViewItem = Core.UI.WPF.Query.DependencyObject<TreeViewItem>(e.OriginalSource as UIElement);
            if (treeViewItem != null && selectedTreeViewItem != null)
            {
                targetTreeViewItem = treeViewItem;
                e.Effects = DragDropEffects.Move;
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            Point currentPosition = e.GetPosition(treeView);

            if ((Math.Abs(currentPosition.X - lastMouseDown.X) > 10.0) || (Math.Abs(currentPosition.Y - lastMouseDown.Y) > 10.0))
            {
                // Verify that this is a valid drop and then store the drop target
                TreeViewItem treeViewItem = Core.UI.WPF.Query.DependencyObject<TreeViewItem>(e.OriginalSource as UIElement);
                if (IsValid(selectedTreeViewItem, treeViewItem))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private TreeView GetTreeView()
        {
            return treeView;
        }

        private static bool IsValid(TreeViewItem sourceTreeViewItem, TreeViewItem targetTreeViewItem)
        {
            if(sourceTreeViewItem == null || targetTreeViewItem == null)
            {
                return false;
            }

            if(sourceTreeViewItem == targetTreeViewItem)
            {
                return false;
            }

            return true;
        }
        
        private void CopyItem(TreeViewItem sourceTreeViewItem, TreeViewItem targetTreeViewItem)
        {
            //adding dragged TreeViewItem in target TreeViewItem
            AddChild(sourceTreeViewItem, targetTreeViewItem);

            //finding Parent TreeViewItem of dragged TreeViewItem 
            TreeViewItem treeViewItem = Core.UI.WPF.Query.ParentDependencyObject<TreeViewItem>(sourceTreeViewItem);
            // if parent is null then remove from TreeView else remove from Parent TreeViewItem
            if (treeViewItem == null)
            {
                treeView.Items.Remove(sourceTreeViewItem);
            }
            else
            {
                treeViewItem.Items.Remove(sourceTreeViewItem);
            }
        }
        
        private static void AddChild(TreeViewItem sourceTreeViewItem, TreeViewItem targetTreeViewItem)
        {
            if(sourceTreeViewItem == null || targetTreeViewItem == null)
            {
                return;
            }

            // add item in target TreeViewItem 
            TreeViewItem treeViewItem_New = new TreeViewItem();
            treeViewItem_New.Header = sourceTreeViewItem.Header;
            treeViewItem_New.Tag = sourceTreeViewItem.Tag;

            targetTreeViewItem.Items.Add(treeViewItem_New);
            foreach (TreeViewItem treeViewItem in sourceTreeViewItem.Items)
            {
                AddChild(treeViewItem, treeViewItem_New);
            }
        }
        

    }
}
