﻿using NeeLaboratory.ComponentModel;
using NeeView.Windows.Controls;
using NeeView.Windows.Property;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NeeView
{
    public partial class App : Application, INotifyPropertyChanged
    {
        // ここでのパラメータは値の保持のみを行う。機能は提供しない。

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

        #region Fields

        private bool _isNetworkEnalbe = true;
        private bool _isSettingBackup;
        private bool _isSaveWindowPlacement;
        private double _autoHideDelayTime = 1.0;
        private string _temporaryDirectory;
        private string _cacheDirectory;
        private bool _isSaveHistory;
        private string _historyFilePath;

        #endregion

        #region Properties

        // 適用した設定データのバージョン
        public int SettingVersion { get; set; }

        // 多重起動を許可する
        [PropertyMember("@ParamIsMultiBootEnabled")]
        public bool IsMultiBootEnabled { get; set; }

        // フルスクリーン状態を復元する
        [PropertyMember("@ParamIsSaveFullScreen")]
        public bool IsSaveFullScreen { get; set; }

        // ウィンドウ座標を復元する
        [PropertyMember("@ParamIsSaveWindowPlacement")]
        public bool IsSaveWindowPlacement
        {
            get { return _isSaveWindowPlacement; }
            set { if (_isSaveWindowPlacement != value) { _isSaveWindowPlacement = value; RaisePropertyChanged(); } }
        }

        // ネットワークアクセス許可
        [PropertyMember("@ParamIsNetworkEnabled", Tips = "@ParamIsNetworkEnabledTips")]
        public bool IsNetworkEnabled
        {
            get { return _isNetworkEnalbe || Config.Current.IsAppxPackage; } // Appxは強制ON
            set { if (_isNetworkEnalbe != value) { _isNetworkEnalbe = value; RaisePropertyChanged(); } }
        }

        // 画像のDPI非対応
        [PropertyMember("@ParamIsIgnoreImageDpi", Tips = "@ParamIsIgnoreImageDpiTips")]
        public bool IsIgnoreImageDpi { get; set; } = true;

        // 複数ウィンドウの座標復元
        [PropertyMember("@ParamIsRestoreSecondWindow", Tips = "@ParamIsRestoreSecondWindowTips")]
        public bool IsRestoreSecondWindow { get; set; } = true;

        [Obsolete]
        public bool IsDisableSave { get; set; }

        // 履歴データの保存
        [PropertyMember("@ParamIsSaveHistory")]
        public bool IsSaveHistory
        {
            get { return _isSaveHistory; }
            set { SetProperty(ref _isSaveHistory, value); }
        }

        // 履歴データの保存場所
        [PropertyPath("@ParamHistoryFilePath", FileDialogType = FileDialogType.SaveFile, Filter = "XML|*.xml", Note = SaveData.HistoryFileName)]
        public string HistoryFilePath
        {
            get => _historyFilePath;
            set => _historyFilePath = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // ブックマークの保存
        [PropertyMember("@ParamIsSaveBookmark")]
        public bool IsSaveBookmark { get; set; } = true;

        // ページマークの保存
        [PropertyMember("@ParamIsSavePagemark")]
        public bool IsSavePagemark { get; set; } = true;

        // パネルやメニューが自動的に消えるまでの時間(秒)
        [PropertyMember("@ParamAutoHideDelayTime")]
        public double AutoHideDelayTime
        {
            get { return _autoHideDelayTime; }
            set { if (_autoHideDelayTime != value) { _autoHideDelayTime = value; RaisePropertyChanged(); } }
        }

        // ウィンドウクローム枠
        [PropertyMember("@ParamWindowChromeFrame")]
        public WindowChromeFrame WindowChromeFrame { get; set; } = WindowChromeFrame.Line;

        // 前回開いていたブックを開く
        [PropertyMember("@ParamIsOpenLastBook")]
        public bool IsOpenLastBook { get; set; }

        // ダウンロードファイル置き場
        [DefaultValue("")]
        [PropertyPath("@ParamDownloadPath", Tips = "@ParamDownloadPathTips", FileDialogType = FileDialogType.Directory)]
        public string DownloadPath { get; set; } = "";

        [PropertyMember("@ParamIsSettingBackup", Tips = "@ParamIsSettingBackupTips")]
        public bool IsSettingBackup
        {
            get { return _isSettingBackup || Config.Current.IsAppxPackage; }  // Appxは強制ON
            set { _isSettingBackup = value; }
        }

        // 言語
        [PropertyMember("@ParamLanguage", Tips = "@ParamLanguageTips")]
        public Language Language { get; set; } = LanguageExtensions.GetLanguage(CultureInfo.CurrentCulture.Name);

        // スプラッシュスクリーン
        [PropertyMember("@ParamIsSplashScreenEnabled")]
        public bool IsSplashScreenEnabled { get; set; } = true;

        // 設定データの同期
        [PropertyMember("@ParamIsSyncUserSetting", Tips = "@ParamIsSyncUserSettingTips")]
        public bool IsSyncUserSetting { get; set; } = true;

        // テンポラリーフォルダーの場所
        [PropertyPath("@ParamTemporaryDirectory", Tips = "@ParamTemporaryDirectoryTips", FileDialogType = FileDialogType.Directory)]
        public string TemporaryDirectory
        {
            get => _temporaryDirectory;
            set => _temporaryDirectory = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // サムネイルキャッシュの場所
        [PropertyPath("@ParamCacheDirectory", Tips = "@ParamCacheDirectoryTips", FileDialogType = FileDialogType.Directory)]
        public string CacheDirectory
        {
            get => _cacheDirectory;
            set => _cacheDirectory = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // サムネイルキャッシュの場所 (変更前)
        public string CacheDirectoryOld { get; set; }

        #endregion

        #region Memento
        [DataContract]
        public class Memento
        {
            [DataMember]
            public int _Version { get; set; } = Config.Current.ProductVersionNumber;

            [DataMember, DefaultValue(false)]
            public bool IsMultiBootEnabled { get; set; }

            [DataMember, DefaultValue(false)]
            public bool IsSaveFullScreen { get; set; }

            [DataMember, DefaultValue(false)]
            public bool IsSaveWindowPlacement { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsNetworkEnabled { get; set; }

            [Obsolete]
            [DataMember(EmitDefaultValue = false)]
            public bool IsDisableSave { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsSaveHistory { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string HistoryFilePath { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsSaveBookmark { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsSavePagemark { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsIgnoreImageDpi { get; set; }

            [Obsolete]
            [DataMember(EmitDefaultValue = false), DefaultValue(false)]
            public bool IsIgnoreWindowDpi { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsRestoreSecondWindow { get; set; }

            [DataMember, DefaultValue(WindowChromeFrame.Line)]
            public WindowChromeFrame WindowChromeFrame { get; set; }

            [DataMember, DefaultValue(1.0)]
            public double AutoHideDelayTime { get; set; }

            [DataMember, DefaultValue(false)]
            public bool IsOpenLastBook { get; set; }

            [DataMember, DefaultValue("")]
            public string DownloadPath { get; set; }

            [DataMember, DefaultValue(false)]
            public bool IsSettingBackup { get; set; }

            [DataMember]
            public Language Language { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsSplashScreenEnabled { get; set; }

            [DataMember, DefaultValue(true)]
            public bool IsSyncUserSetting { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string TemporaryDirectory { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string CacheDirectory { get; set; }

            [DataMember(EmitDefaultValue = false)]
            public string CacheDirectoryOld { get; set; }


            [OnDeserializing]
            private void Deserializing(StreamingContext c)
            {
                this.InitializePropertyDefaultValues();

                this.Language = LanguageExtensions.GetLanguage(CultureInfo.CurrentCulture.Name);
            }

#pragma warning disable CS0612

            [OnDeserialized]
            private void Deserialized(StreamingContext c)
            {
                // before ver.30
                if (_Version < Config.GenerateProductVersionNumber(30, 0, 0))
                {
                    if (IsDisableSave)
                    {
                        IsSaveHistory = false;
                        IsSaveBookmark = false;
                        IsSavePagemark = false;
                    }
                }
            }

#pragma warning restore CS0612
        }

        //
        public Memento CreateMemento()
        {
            var memento = new Memento();
            memento.IsMultiBootEnabled = this.IsMultiBootEnabled;
            memento.IsSaveFullScreen = this.IsSaveFullScreen;
            memento.IsSaveWindowPlacement = this.IsSaveWindowPlacement;
            memento.IsNetworkEnabled = this.IsNetworkEnabled;
            memento.IsIgnoreImageDpi = this.IsIgnoreImageDpi;
            memento.IsSaveHistory = this.IsSaveHistory;
            memento.HistoryFilePath = this.HistoryFilePath;
            memento.IsSaveBookmark = this.IsSaveBookmark;
            memento.IsSavePagemark = this.IsSavePagemark;
            memento.AutoHideDelayTime = this.AutoHideDelayTime;
            memento.WindowChromeFrame = this.WindowChromeFrame;
            memento.IsOpenLastBook = this.IsOpenLastBook;
            memento.DownloadPath = this.DownloadPath;
            memento.IsRestoreSecondWindow = this.IsRestoreSecondWindow;
            memento.IsSettingBackup = this.IsSettingBackup;
            memento.Language = this.Language;
            memento.IsSplashScreenEnabled = this.IsSplashScreenEnabled;
            memento.IsSyncUserSetting = this.IsSyncUserSetting;
            memento.TemporaryDirectory = this.TemporaryDirectory;
            memento.CacheDirectory = this.CacheDirectory;
            memento.CacheDirectoryOld = this.CacheDirectoryOld;
            return memento;
        }

        // 起動直後の１回だけの設定反映
        public void RestoreOnce(Memento memento)
        {
            if (memento == null) return;

            this.CacheDirectoryOld = memento.CacheDirectoryOld;
        }

        // 通常の設定反映
        public void Restore(Memento memento)
        {
            if (memento == null) return;

            this.SettingVersion = memento._Version;

            this.IsMultiBootEnabled = memento.IsMultiBootEnabled;
            this.IsSaveFullScreen = memento.IsSaveFullScreen;
            this.IsSaveWindowPlacement = memento.IsSaveWindowPlacement;
            this.IsNetworkEnabled = memento.IsNetworkEnabled;
            this.IsIgnoreImageDpi = memento.IsIgnoreImageDpi;
            this.IsSaveHistory = memento.IsSaveHistory;
            this.HistoryFilePath = memento.HistoryFilePath;
            this.IsSaveBookmark = memento.IsSaveBookmark;
            this.IsSavePagemark = memento.IsSavePagemark;
            this.AutoHideDelayTime = memento.AutoHideDelayTime;
            this.WindowChromeFrame = memento.WindowChromeFrame;
            this.IsOpenLastBook = memento.IsOpenLastBook;
            this.DownloadPath = memento.DownloadPath;
            this.IsRestoreSecondWindow = memento.IsRestoreSecondWindow;
            this.IsSettingBackup = memento.IsSettingBackup;
            this.Language = memento.Language;
            this.IsSplashScreenEnabled = memento.IsSplashScreenEnabled;
            this.IsSyncUserSetting = memento.IsSyncUserSetting;
            this.TemporaryDirectory = memento.TemporaryDirectory;
            this.CacheDirectory = memento.CacheDirectory;
            ////this.CacheDirectoryOld = memento.CacheDirectoryOld; // RestoreOnce()で反映
        }

#pragma warning disable CS0612

        public void RestoreCompatible(UserSetting setting)
        {
            // compatible before ver.23
            if (setting._Version < Config.GenerateProductVersionNumber(1, 23, 0))
            {
                if (setting.ViewMemento != null)
                {
                    this.IsMultiBootEnabled = !setting.ViewMemento.IsDisableMultiBoot;
                    this.IsSaveFullScreen = setting.ViewMemento.IsSaveFullScreen;
                    this.IsSaveWindowPlacement = setting.ViewMemento.IsSaveWindowPlacement;
                }
            }

            // Preferenceの復元 (APP)
            if (setting.PreferenceMemento != null)
            {
                var preference = new Preference();
                preference.Restore(setting.PreferenceMemento);
                preference.RestoreCompatibleApp();
            }
        }

#pragma warning restore CS0612

        #endregion

    }
}
