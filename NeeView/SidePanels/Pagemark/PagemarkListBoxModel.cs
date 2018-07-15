﻿using NeeLaboratory.ComponentModel;
using NeeView.Collections.Generic;
using NeeView.Windows;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NeeView
{
    public class PagemarkListBoxModel : BindableBase
    {
        // Fields

        private TreeListNode<IPagemarkEntry> _selectedItem;
        private Toast _toast;

        // Constructors

        public PagemarkListBoxModel()
        {
            PagemarkCollection.Current.PagemarkChanged += PagemarkCollection_PagemarkChanged;
        }


        // Events

        public event CollectionChangeEventHandler Changed;
        public event EventHandler SelectedItemChanged;


        // Properties

        public PagemarkList PagemarkList => PagemarkList.Current;

        public PagemarkCollection PagemarkCollection => PagemarkCollection.Current;

        public TreeListNode<IPagemarkEntry> SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != null && _selectedItem != value)
                {
                    _selectedItem.IsSelected = false;
                }

                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem != null)
                    {
                        _selectedItem.IsSelected = true;
                    }
                }
            }
        }


        // Methods

        private void PagemarkCollection_PagemarkChanged(object sender, PagemarkCollectionChangedEventArgs e)
        {
            if (_toast != null)
            {
                _toast.Cancel();
                _toast = null;
            }
        }

        public void Decide(TreeListNode<IPagemarkEntry> item)
        {
            switch (item.Value)
            {
                case Pagemark pagemark:
                    bool isJumped = BookOperation.Current.JumpPagemarkInPlace(pagemark);
                    if (!isJumped)
                    {
                        var options = pagemark.EntryName != null ? BookLoadOption.IsPage : BookLoadOption.None;
                        BookHub.Current.RequestLoad(pagemark.Place, pagemark.EntryName, options, true);
                    }
                    break;
            }
        }

        public void Expand(TreeListNode<IPagemarkEntry> item, bool isExpanded)
        {
            if (item.CanExpand && item.IsExpanded != isExpanded)
            {
                item.IsExpanded = isExpanded;
            }
        }


        public bool Remove(TreeListNode<IPagemarkEntry> item)
        {
            var next = item.Next ?? item.Previous ?? item.Parent;

            var memento = new TreeListNodeMemento<IPagemarkEntry>(item);

            var isRemoved = PagemarkCollection.Current.Remove(item);
            if (isRemoved)
            {
                if (item.Value is PagemarkFolder)
                {
                    var count = item.Count(e => e.Value is Pagemark);
                    if (count > 0)
                    {
                        _toast = new Toast(string.Format(Properties.Resources.DialogPagemarkFolderDelete, count), Properties.Resources.WordRestore, () => PagemarkCollection.Current.Restore(memento));
                        ToastService.Current.Show(_toast);
                    }
                }

                if (next != null)
                {
                    SelectedItem = next;
                }
            }

            return isRemoved;
        }

        #region Pagemark Special

        public void SetSelectedItem(string place, string entryName)
        {
            var node = PagemarkCollection.Current.FindNode(place, entryName);
            if (node == null)
            {
                return;
            }

            node.ExpandParent();
            SelectedItem = node;

            SelectedItemChanged?.Invoke(this, null);
        }


        /// <summary>
        /// 指定のマーカーに移動。存在しなければ移動しない
        /// </summary>
        public void Jump(string place, string entryName)
        {
            var node = PagemarkCollection.Current.FindNode(place, entryName);
            if (node != null)
            {
                node.ExpandParent();
                SelectedItem = node;

                SelectedItemChanged?.Invoke(this, null);
            }
        }

        #endregion

        public void Move(DropInfo<TreeListNode<IPagemarkEntry>> dropInfo)
        {
            if (dropInfo == null) return;
            if (dropInfo.Data == dropInfo.DropTarget) return;

            var item = dropInfo.Data;
            var target = dropInfo.DropTarget;

            const double margine = 0.33;

            if (target.Value is PagemarkFolder folder)
            {
                if (dropInfo.Position < margine)
                {
                    PagemarkCollection.Current.Move(item, target, -1);
                }
                else if (dropInfo.Position > (1.0 - margine) && !target.IsExpanded)
                {
                    PagemarkCollection.Current.Move(item, target, +1);
                }
                else
                {
                    PagemarkCollection.Current.MoveToChild(item, target);
                }
            }
            else
            {
                if (target.Next == null && dropInfo.Position > (1.0 - margine))
                {
                    PagemarkCollection.Current.Move(item, target, +1);
                }
                else if (item.CompareOrder(item, target))
                {
                    PagemarkCollection.Current.Move(item, target, +1);
                }
                else
                {
                    PagemarkCollection.Current.Move(item, target, -1);
                }
            }
        }

        internal void NewFolder()
        {
            var node = new TreeListNode<IPagemarkEntry>(new PagemarkFolder() { Name = Properties.Resources.WordNewFolder });
            PagemarkCollection.Current.AddFirst(node);
            SelectedItem = node;
            Changed?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, node));
        }




        // ページマークを戻る
        public void PrevPagemark()
        {
            if (BookHub.Current.IsLoading) return;

            var node = GetNeighborPagemark(SelectedItem, -1);
            if (node != null)
            {
                SelectedItem = node;

                if (node.Value is Pagemark)
                {
                    Decide(node);
                }

                SelectedItemChanged?.Invoke(this, null);
            }
            else
            {
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, Properties.Resources.NotifyPagemarkPrevFailed);
            }
        }

        // ページマークを進む
        public void NextPagemark()
        {
            if (BookHub.Current.IsLoading) return;

            var node = GetNeighborPagemark(SelectedItem, +1);
            if (node != null)
            {
                SelectedItem = node;

                if (node.Value is Pagemark)
                {
                    Decide(node);
                }

                SelectedItemChanged?.Invoke(this, null);
            }
            else
            {
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, Properties.Resources.NotifyPagemarkNextFailed);
            }
        }

        private TreeListNode<IPagemarkEntry> GetNeighborPagemark(TreeListNode<IPagemarkEntry> item, int direction)
        {
            if (direction == 0) throw new ArgumentOutOfRangeException(nameof(direction));

            if (item == null)
            {
                var pagemarks = PagemarkCollection.Current.Items.GetExpandedCollection().Where(e => e.Value is Pagemark).ToList();
                return direction >= 0 ? pagemarks.FirstOrDefault() : pagemarks.LastOrDefault();
            }
            else
            {
                var pagemarks = PagemarkCollection.Current.Items.GetExpandedCollection().Where(e => e.Value is Pagemark || e == item).ToList();
                if (pagemarks.Count <= 0)
                {
                    return null;
                }
                int index = pagemarks.IndexOf(item);
                if (index < 0)
                {
                    return null;
                }
                return pagemarks.ElementAtOrDefault(index + direction);
            }
        }

        public int IndexOfSelectedItem()
        {
            return IndexOfExpanded(SelectedItem);
        }

        public int IndexOfExpanded(TreeListNode<IPagemarkEntry> item)
        {
            if (item == null)
            {
                return -1;
            }

            return PagemarkCollection.Current.Items.GetExpandedCollection().IndexOf(item);
        }
    }
}
