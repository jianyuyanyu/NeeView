﻿using NeeLaboratory.ComponentModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace NeeView.Windows
{
    public class WindowChromeAccessor : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddPropertyChanged(string propertyName, PropertyChangedEventHandler handler)
        {
            PropertyChanged += (s, e) => { if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName) handler?.Invoke(s, e); };
        }

        #endregion


        private Window _window;
        private WindowChrome _windowChrome;
        private bool _isEnabled;
        private bool _isSuspended;
        private double _maximizeWindowGapWidth = 8.0;


        public WindowChromeAccessor(Window window)
        {
            _window = window;

            _windowChrome = new WindowChrome();
            _windowChrome.CornerRadius = new CornerRadius();
            _windowChrome.UseAeroCaptionButtons = false;
            _windowChrome.CaptionHeight = 0;
            _windowChrome.GlassFrameThickness = new Thickness(1);

            _window.StateChanged += Window_StateChanged;
        }


        public Window Window => _window;

        public WindowChrome WindowChrome => _windowChrome;


        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    Update();
                }
            }
        }

        public bool IsSuspended
        {
            get { return _isSuspended; }
            set
            {
                if (SetProperty(ref _isSuspended, value))
                {
                    Update();
                }
            }
        }

        public bool IsActive => _isEnabled && !_isSuspended;



        public double CaptionHeight
        {
            get { return _window.WindowState == WindowState.Maximized ? 24.0 : 28.0; }
        }

        public double MaximizeWindowGapWidth
        {
            get { return _maximizeWindowGapWidth; }
            set
            {
                if (SetProperty(ref _maximizeWindowGapWidth, Math.Max(value, 0.0)))
                {
                    UpdateWindowBorderThickness();
                }
            }
        }


        private void Window_StateChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(CaptionHeight));
            UpdateWindowBorderThickness();
        }

        private void Update()
        {
            WindowChrome.SetWindowChrome(_window, IsActive ? _windowChrome : null);
            UpdateWindowBorderThickness();
            AttachWindowChromeExceptionGuard();
        }

        public virtual void UpdateWindowBorderThickness()
        {
            var dpi = (_window is IHasDpiScale dpiProvider) ? dpiProvider.GetDpiScale() : new DpiScale(1.0, 1.0);

            if (IsActive && _window.WindowState == WindowState.Maximized)
            {
                var x = MaximizeWindowGapWidth / dpi.DpiScaleX;
                var y = MaximizeWindowGapWidth / dpi.DpiScaleY;
                _window.BorderBrush = Brushes.Black;
                _window.BorderThickness = new Thickness(x, y, x, y);
            }
            else
            {
                _window.BorderThickness = default;
            }
        }

        #region Hotfix: Overflow exception in WindowChrome

        // https://developercommunity.visualstudio.com/content/problem/167357/overflow-exception-in-windowchrome.html?childToView=1209945#comment-1209945 

        private HwndSource GetHwndSource()
        {
            return (HwndSource)PresentationSource.FromVisual(_window);
        }

        private void AttachWindowChromeExceptionGuard()
        {
            HwndSource hwnd = GetHwndSource();
            if (hwnd == null) return;
            Debug.WriteLine($"SetHook {hwnd.Handle}");
            hwnd.RemoveHook(HookProc);
            hwnd.AddHook(HookProc);
        }

        private IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0084 /*WM_NCHITTEST*/ )
            {
                // This prevents a crash in WindowChromeWorker._HandleNCHitTest
                try
                {
                    var x = lParam.ToInt32();
                    ////DebugInfo.Current?.SetMessage($"WM_NCHITTEST.LPARAM: {x:#,0}");
                    ////Debug.WriteLine($"{x:#,0}");
                }
                catch (OverflowException)
                {
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        #endregion
    }

}
