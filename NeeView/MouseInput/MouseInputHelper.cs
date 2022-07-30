﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace NeeView
{
    /// <summary>
    /// MouseInput Helper
    /// </summary>
    public static class MouseInputHelper
    {
        #region MouseCapture Helper
        // MouseCaptureのラップ。開発用

        [Conditional("DEBUG")]
        public static void DumpMouseCaptureState()
        {
            Debug.WriteLine($"> Mouse captured element: {FixedElementName(Mouse.Captured)}");
        }

        public static void CaptureMouse(object sender, IInputElement element)
        {
            //var id = _mouseCaptureSerial++;
            //Debug.WriteLine($"> {id}.MouseCapture: {FixedElementName(element)} by {sender}");
            element.CaptureMouse();
            //Debug.WriteLine($"> {id}.MouseCapture: done.");
        }

        public static void ReleaseMouseCapture(object sender, IInputElement element)
        {
            //var id = _mouseCaptureSerial++;
            //Debug.WriteLine($"> {id}.ReleaseMouseCapture: {FixedElementName(element)} by {sender}");
            //Debug.Assert(Mouse.Captured == element, "WARNING!! It's not caputured element.");
            element.ReleaseMouseCapture();
            //Debug.WriteLine($"> {id}.ReleaseMouseCapture: done.");
        }

        private static string FixedElementName(IInputElement element)
        {
            if (element is FrameworkElement framweorkElement)
            {
                return framweorkElement.ToString() + (framweorkElement.Name != null ? $" ({framweorkElement.Name})" : "");
            }
            else
            {
                return element?.ToString();
            }
        }

        #endregion MouseCapture Helper

        /// <summary>
        /// マウスボタン入力イベントをブロック。コマンド制御用。
        /// </summary>
        /// <param name="element"></param>
        public static void AddMouseButtonBlock(FrameworkElement element)
        {
            element.MouseDown += OnMouseButtonChanged;
            element.MouseUp += OnMouseButtonChanged;
            element.MouseWheel += (s, e) => e.Handled = true;

            void OnMouseButtonChanged(object sender, MouseButtonEventArgs e)
            {
                e.Handled = e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right;
            }
        }
    }
}
