﻿using NeeLaboratory.ComponentModel;
using NeeView.Windows.Property;

namespace NeeView
{
    public class AutoHideConfig : BindableBase
    {
        private double _autoHideDelayTime = 1.0;
        private double _autoHideDelayVisibleTime = 0.0;
        private AutoHideFocusLockMode _autoHideFocusLockMode = AutoHideFocusLockMode.LogicalTextBoxFocusLock;
        private bool _isAutoHideKeyDownDelay = true;
        private double _autoHideHitTestMargin = 32.0;

        // パネルやメニューが自動的に消えるまでの時間(秒)
        [PropertyMember]
        public double AutoHideDelayTime
        {
            get { return _autoHideDelayTime; }
            set { SetProperty(ref _autoHideDelayTime, value); }
        }

        // パネルやメニューが自動的に消えるまでの時間(秒)
        [PropertyMember]
        public double AutoHideDelayVisibleTime
        {
            get { return _autoHideDelayVisibleTime; }
            set { SetProperty(ref _autoHideDelayVisibleTime, value); }
        }

        // パネル自動非表示のフォーカス挙動モード
        [PropertyMember]
        public AutoHideFocusLockMode AutoHideFocusLockMode
        {
            get { return _autoHideFocusLockMode; }
            set { SetProperty(ref _autoHideFocusLockMode, value); }
        }

        // パネル自動非表示のキー入力遅延
        [PropertyMember]
        public bool IsAutoHideKeyDownDelay
        {
            get { return _isAutoHideKeyDownDelay; }
            set { SetProperty(ref _isAutoHideKeyDownDelay, value); }
        }

        // パネル自動非表示の表示判定マージン
        [PropertyMember]
        public double AutoHideHitTestMargin
        {
            get { return _autoHideHitTestMargin; }
            set { SetProperty(ref _autoHideHitTestMargin, value); }
        }
    }
}