﻿using NeeLaboratory.ComponentModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace NeeView
{
    public class MouseHorizontalWheelSource : IDisposable, INotifyMouseHorizontalWheelChanged
    {
        private readonly INotifyMouseHorizontalWheelChanged _source;
        private readonly FrameworkElement _target;
        private bool _disposedValue;


        public MouseHorizontalWheelSource(FrameworkElement target, INotifyMouseHorizontalWheelChanged source)
        {
            _source = source;
            _target = target;

            _source.MouseHorizontalWheelChanged += Source_MouseHorizontalWheel;
        }


        public event MouseWheelEventHandler? MouseHorizontalWheelChanged;

        public IDisposable SubscribeMouseHorizontalWheelChanged(MouseWheelEventHandler handler)
        {
            MouseHorizontalWheelChanged += handler;
            return new AnonymousDisposable(() => MouseHorizontalWheelChanged -= handler);
        }



        private void Source_MouseHorizontalWheel(object? sender, MouseWheelEventArgs e)
        {
            if (_target.IsMouseOver)
            {
                MouseHorizontalWheelChanged?.Invoke(_target, e);
            }
        }


        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _source.MouseHorizontalWheelChanged -= Source_MouseHorizontalWheel;
                    this.MouseHorizontalWheelChanged = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }



}
