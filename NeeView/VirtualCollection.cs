﻿using NeeView.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace NeeView
{
    /// <summary>
    /// 仮想パネル管理を行う項目
    /// </summary>
    public interface IVirtualItem
    {
        /// <summary>
        /// 実体化解除までのカウント
        /// </summary>
        int DetachCount { get; set; }

        /// <summary>
        /// 実体化された時に呼ばれる
        /// </summary>
        void Attached();

        /// <summary>
        /// 実体化を解除された時に呼ばれる
        /// </summary>
        void Detached();
    }

    /// <summary>
    /// 仮想パネルの実項目管理用
    /// </summary>
    /// <remarks>
    /// TContainerのDataContextはIHasValue&lt;TValue&gt;継承でなければならず、TValueはIVirtualItem継承である必要がある。監視はこのValueに対して行われる。
    /// TContainer.DataContext : IHasValue&lt;TValue&gt;
    ///     + Value: IVirtualItem
    /// </remarks>
    /// <typeparam name="TContainer">TreeViewItem,ListBoxItem等のコンテナ</typeparam>
    /// <typeparam name="TValue">Value型</typeparam>
    public class VirtualCollection<TContainer, TValue> : IDisposable
        where TContainer : Control
    {
        private ItemsControl _itemsControl;
        private List<IVirtualItem> _items;
        private DispatcherTimer _timer;
        public bool _darty;


        public VirtualCollection(ItemsControl itemsControl)
        {
            _itemsControl = itemsControl;

            _items = new List<IVirtualItem>();

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();
        }


        /// <summary>
        /// 更新要求を遅延させて実行
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_darty)
            {
                _darty = CleanUp();
            }
        }

        public void Attach(IVirtualItem item)
        {
            if (!_items.Contains(item))
            {
                _items.Add(item);
                item.DetachCount = 0;
                item.Attached();
                ////Debug.WriteLine($"Attached: {item}");
            }
        }

        public void Detach(IVirtualItem item)
        {
            if (_items.Contains(item))
            {
                _items.Remove(item);
                item.Detached();
                ////Debug.WriteLine($"Detached: {item}");
            }
        }

        public void Refresh()
        {
            _darty = true;
        }

        /// <summary>
        /// 実体化されていない項目をDetachする。
        /// 実体化が間に合っていない可能性を考慮して２回目判定を行っている。
        /// </summary>
        private bool CleanUp()
        {
            var nodes = CollectVisualChildren<TContainer>(_itemsControl).Select(e => e.DataContext);
            var values = nodes.OfType<IHasValue<TValue>>();
            var items = values.Select(e => e.Value).OfType<IVirtualItem>();

            var intersect = _items.Intersect(items);
            var removes = _items.Except(intersect);

            ////Debug.WriteLine($"CleanUp: {removes.Count()}/{items.Count()}/{values.Count()}/{nodes.Count()}");

            if (!removes.Any())
            {
                return false;
            }

            foreach (var item in removes)
            {
                item.DetachCount++;
            }

            var detaches = removes.Where(e => e.DetachCount > 1);
            foreach (var item in detaches)
            {
                ////Debug.WriteLine($"CleanUp.Detatched: {item}");
                item.Detached();
            }

            var rest = removes.Except(detaches);
            _items = intersect.Concat(rest).ToList();

            return true;
        }

        private IEnumerable<T> CollectVisualChildren<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        yield return correctlyTyped;
                    }

                    foreach (var descendent in CollectVisualChildren<T>(child))
                    {
                        yield return descendent;
                    }
                }
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timer.Stop();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

}
