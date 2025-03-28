﻿using System;
using System.Linq;

namespace NeeView.PageFrames
{
    public class PageFrameContainerSelectedPageWatcher : IDisposable
    {
        private readonly PageFrameBox _box;
        private readonly Book _book;
        private bool _disposedValue;

        public PageFrameContainerSelectedPageWatcher(PageFrameBox box, Book book)
        {
            _box = box;
            _book = book;

            _box.ViewContentChanged += Box_ViewContentChanged;
        }

        private void Box_ViewContentChanged(object? sender, FrameViewContentChangedEventArgs e)
        {
            if (e.Action < ViewContentChangedAction.ContentLoading) return;
            var viewPages = e.ViewContents.Select(e => e.Page).Distinct().ToList();
            _book.Pages.SetViewPageFlag(viewPages);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _box.ViewContentChanged -= Box_ViewContentChanged;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
