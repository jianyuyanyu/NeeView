﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NeeView
{
    public class MouseGestureEventArgs
    {
        public MouseGestureSequence MouseGestureSequence { get; set; }
        public bool Handled { get; set; }

        public MouseGestureEventArgs(MouseGestureSequence sequence)
        {
            MouseGestureSequence = sequence;
        }
    }

    /// <summary>
    /// マウスジェスチャー管理
    /// </summary>
    public class MouseGestureManager : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }
        #endregion

        // ジェスチャーシーケンスとコマンドの対応表
        public MouseGestureCommandCollection CommandCollection { get; private set; }

        // マウスジェスチャーコントローラー
        public MouseGestureController Controller { get; private set; }

        #region Property: GestureText
        private string _gestureText;
        public string GestureText
        {
            get { return _gestureText; }
            set { _gestureText = value; RaisePropertyChanged(); }
        }
        #endregion

        // クリックイベントハンドラ
        public event EventHandler<MouseButtonEventArgs> MouseClickEventHandler;



        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sender">マウスジェスチャーを受け付けるコントロール</param>
        public MouseGestureManager(FrameworkElement sender)
        {
            CommandCollection = new MouseGestureCommandCollection();

            Controller = new MouseGestureController(sender);

            Controller.MouseGestureUpdateEventHandler +=
                (s, e) => GestureText = e.ToString();
            Controller.MouseGestureExecuteEventHandler +=
                (s, e) =>
                {
                    var command = CommandCollection.GetCommand(e.MouseGestureSequence);
                    if (command == _contextMenuCommand)
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        if (command != null && command.CanExecute(null, null))
                        {
                            command.Execute(null, null);
                        }
                        e.Handled = true;
                    }
                };
            Controller.MouseClickEventHandler +=
                (s, e) => MouseClickEventHandler?.Invoke(s, e);
        }

        // クリックイベントハンドル初期化
        public void ClearClickEventHandler()
        {
            MouseClickEventHandler = null;
        }

        //
        private RoutedUICommand _contextMenuCommand = new RoutedUICommand("コンテキストメニュー", "OpenContextMenu", typeof(MainWindow));

        // コンテキストメニュー起動用ジェスチャー登録
        public void AddOpenContextMenuGesture(string gesture)
        {
            if (string.IsNullOrWhiteSpace(gesture)) return;
            CommandCollection.Add(gesture, _contextMenuCommand);
        }

        // 現在のジェスチャーシーケンスでのコマンド名取得
        public string GetGestureCommandName()
        {
            var command = CommandCollection.GetCommand(Controller.Gesture);
            return command?.Text;
        }

        // 現在のジェスチャーシーケンス表示文字列取得
        public string GetGestureString()
        {
            return Controller.Gesture.ToDispString();
        }

        // 現在のジェスチャー表示文字列取得
        public string GetGestureText()
        {
            string commandName = GetGestureCommandName();
            return ((commandName != null) ? commandName + "\n" : "") + Controller.Gesture.ToDispString();
        }
    }
}
