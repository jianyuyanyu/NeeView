﻿using NeeView.Windows;
using System;

namespace NeeView
{
    public class MainViewLocker : IDisposable
    {
        private readonly MainViewLockerMediator _mediator;
        private readonly IMainViewWindow _window;
        private readonly MainViewLockerKey _key;
        private bool _disposedValue;

        public MainViewLocker(MainViewLockerMediator mediator, IMainViewWindow window)
        {
            _mediator = mediator;
            _window = window;
            _key = _mediator.CreateKey();

            AttachWindowStateManager();
        }


        #region IDisposable support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    DetachWindowStateManager();
                    Deactivate();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable support


        private void AttachWindowStateManager()
        {
            _window.WindowStateManager.StateEditing += WindowStateManager_StateEditing;
            _window.WindowStateManager.StateEdited += WindowStateManager_StateEdited;
        }

        private void DetachWindowStateManager()
        {
            _window.WindowStateManager.StateEditing -= WindowStateManager_StateEditing;
            _window.WindowStateManager.StateEdited -= WindowStateManager_StateEdited;
        }

        private void WindowStateManager_StateEditing(object? sender, WindowStateExChangedEventArgs e)
        {
            Lock();
        }

        private void WindowStateManager_StateEdited(object? sender, WindowStateExChangedEventArgs e)
        {
            double delay = e.NewState == WindowStateEx.FullScreen ? 1.0 : 0.0;
            Unlock(delay);
        }


        public void Activate()
        {
            _mediator.Activate(_key);
        }

        public void Deactivate()
        {
            _mediator.Deactivate(_key);
        }

        public void Lock()
        {
            _mediator.Lock(_key);
        }

        public void Unlock(double delayMilliseconds)
        {
            _mediator.Unlock(_key, delayMilliseconds);
        }
    }


}
