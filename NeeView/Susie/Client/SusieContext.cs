﻿using NeeLaboratory.ComponentModel;
using NeeView.Windows.Controls;
using NeeView.Windows.Property;
using NeeView.Susie;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NeeView.Susie.Client;

namespace NeeView
{
    public class SusieContext : BindableBase
    {
        static SusieContext() => Current = new SusieContext();
        public static SusieContext Current { get; }

        private bool _isEnabled;
        private bool _isFirstOrderSusieImage;
        private bool _isFirstOrderSusieArchive;


        private SusiePluginClient _client;
        private SusiePluginServerSetting _serverSetting;


        private SusieContext()
        {
            _serverSetting = new SusiePluginServerSetting();
        }


        #region Properties

        // 直接参照はよろしくない
        [Obsolete]
        public SusiePluginClient Client => _client;

        /// <summary>
        /// Susie 有効/無効設定
        /// </summary>
        [PropertyMember("@ParamSusieIsEnabled")]
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    UpdateSusiePluginCollection();
                }
            }
        }

        // Susie プラグインフォルダー
        [PropertyPath("@ParamSusiePluginPath", FileDialogType = FileDialogType.Directory)]
        public string SusiePluginPath
        {
            get { return _serverSetting.PluginFolder; }
            set
            {
                if (_serverSetting.PluginFolder != value)
                {
                    _serverSetting.PluginFolder = value;
                    UpdateSusiePluginCollection();
                }
            }
        }

        // Susie 画像プラグイン 優先フラグ
        [PropertyMember("@ParamSusieIsFirstOrderSusieImage")]
        public bool IsFirstOrderSusieImage
        {
            get { return _isFirstOrderSusieImage; }
            set { if (_isFirstOrderSusieImage != value) { _isFirstOrderSusieImage = value; RaisePropertyChanged(); } }
        }

        // Susie 書庫プラグイン 優先フラグ
        [PropertyMember("@ParamSusieIsFirstOrderSusieArchive")]
        public bool IsFirstOrderSusieArchive
        {
            get { return _isFirstOrderSusieArchive; }
            set { if (_isFirstOrderSusieArchive != value) { _isFirstOrderSusieArchive = value; RaisePropertyChanged(); } }
        }

        // Susie プラグインキャッシュ有効フラグ
        [PropertyMember("@ParamSusieIsPluginCacheEnabled", Tips = "@ParamSusieIsPluginCacheEnabledTips")]
        public bool IsPluginCacheEnabled
        {
            get { return _serverSetting.IsPluginCacheEnabled; }
            set
            {
                if (_serverSetting.IsPluginCacheEnabled != value)
                {
                    _serverSetting.IsPluginCacheEnabled = value;
                    Debug.WriteLine($"TODO: キャッシュ有効/無効の反映。キャッシュの仕組み自体見直しても良いね");
                }
            }
        }

        /// <summary>
        /// 対応画像ファイル拡張子
        /// </summary>
        public FileTypeCollection ImageExtensions = new FileTypeCollection();

        /// <summary>
        /// 対応圧縮ファイル拡張子
        /// </summary>
        public FileTypeCollection ArchiveExtensions = new FileTypeCollection();

        #endregion

        #region Methods

        // PluginCollectionのOpen/Close
        private void UpdateSusiePluginCollection()
        {
            if (_isEnabled && Directory.Exists(SusiePluginPath))
            {
                OpenSusiePluginCollection();
            }
            else
            {
                CloseSusiePluginCollection();
            }
        }

        private void OpenSusiePluginCollection()
        {
            CloseSusiePluginCollection();

            _client = new SusiePluginClient();
            _client.SetServerSetting(_serverSetting);

            UpdateImageExtensions();
            UpdateArchiveExtensions();
        }

        private void CloseSusiePluginCollection()
        {
            if (_client == null) return;

            _serverSetting = _client.GetServerSetting();
            _client.Dispose();
            _client = null;

            UpdateImageExtensions();
            UpdateArchiveExtensions();
        }

        // 最新のプラグイン設定を取得
        private SusiePluginServerSetting GetLatestServerSettings()
        {
            return _client?.GetServerSetting() ?? _serverSetting;
        }

        // Susie画像プラグインのサポート拡張子を更新
        public void UpdateImageExtensions()
        {
            var extensions = _client.GetPlugins(null)
                .Where(e => e.PluginType == SusiePluginType.Image && e.IsEnabled)
                .SelectMany(e => e.Extensions);

            ImageExtensions.Restore(extensions);

            Debug.WriteLine("SusieIN Support: " + string.Join(" ", this.ImageExtensions));
        }

        // Susies書庫プラグインのサポート拡張子を更新
        public void UpdateArchiveExtensions()
        {
            var extensions = _client.GetPlugins(null)
                .Where(e => e.PluginType == SusiePluginType.Archive && e.IsEnabled)
                .SelectMany(e => e.Extensions);

            ArchiveExtensions.Restore(extensions);

            Debug.WriteLine("SusieAM Support: " + string.Join(" ", this.ArchiveExtensions));
        }

        #endregion

        #region Memento

        [DataContract]
        public class Memento
        {
            [DataMember]
            public int _Version { get; set; } = Config.Current.ProductVersionNumber;

            [DataMember]
            public bool IsEnableSusie { get; set; }

            [DataMember]
            public bool IsFirstOrderSusieImage { get; set; }

            [DataMember]
            public bool IsFirstOrderSusieArchive { get; set; }

            [DataMember]
            public SusiePluginServerSetting SusiePluginServerSetting { get; set; }


            #region Obsolete

            [Obsolete, DataMember]
            public string SusiePluginPath { get; set; }

            [Obsolete, DataMember(Name = "SpiFiles", EmitDefaultValue = false)]
            public Dictionary<string, bool> SpiFilesV1 { get; set; } // ver 33.0

            [Obsolete, DataMember(Name = "SpiFilesV2", EmitDefaultValue = false)]
            public Dictionary<string, SusiePluginSetting> SpiFilesV2 { get; set; } // ver 34.0 

            [Obsolete, DataMember(Name = "SusiePlugins", EmitDefaultValue = false)]
            public Dictionary<string, SusiePlugin.Memento> SpiFilesV3 { get; set; } // ver 35.0

            [Obsolete, DataMember, DefaultValue(true)]
            public bool IsPluginCacheEnabled { get; set; }

            #endregion Obsolete




            [OnDeserializing]
            private void Deserializing(StreamingContext c)
            {
                this.InitializePropertyDefaultValues();
            }

            [OnDeserialized]
            private void Deserialized(StreamingContext c)
            {
#pragma warning disable CS0612
                if (_Version < Config.GenerateProductVersionNumber(33, 0, 0) && SpiFilesV1 != null)
                {
                    SpiFilesV2 = SpiFilesV1.ToDictionary(e => e.Key, e => new SusiePluginSetting(e.Value, false));
                }

                if (_Version < Config.GenerateProductVersionNumber(34, 0, 0) && SpiFilesV2 != null)
                {
                    SpiFilesV3 = SpiFilesV2
                        .ToDictionary(e => e.Key, e => e.Value.ToPluginMemento());
                }

                if (_Version < Config.GenerateProductVersionNumber(35, 0, 0) && SpiFilesV3 != null)
                {
                    SusiePluginServerSetting = new SusiePluginServerSetting();
                    SusiePluginServerSetting.IsPluginCacheEnabled = IsPluginCacheEnabled;
                    SusiePluginServerSetting.PluginFolder = SusiePluginPath;
                    SusiePluginServerSetting.PluginSettings = SpiFilesV3
                        .Select(e => e.Value.ToSusiePluginSetting(LoosePath.GetFileName(e.Key)))
                        .ToList();
                }
#pragma warning restore CS0612
            }
        }

        public Memento CreateMemento()
        {
            var memento = new Memento();
            memento.IsEnableSusie = this.IsEnabled;
            memento.IsFirstOrderSusieImage = this.IsFirstOrderSusieImage;
            memento.IsFirstOrderSusieArchive = this.IsFirstOrderSusieArchive;
            memento.SusiePluginServerSetting = GetLatestServerSettings();
            return memento;
        }

        public void Restore(Memento memento)
        {
            if (memento == null) return;

            this.IsFirstOrderSusieImage = memento.IsFirstOrderSusieImage;
            this.IsFirstOrderSusieArchive = memento.IsFirstOrderSusieArchive;
            _serverSetting = memento.SusiePluginServerSetting;
            this.IsEnabled = memento.IsEnableSusie;
        }

        #endregion
    }
}
