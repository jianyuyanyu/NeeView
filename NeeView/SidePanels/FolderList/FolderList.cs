﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using NeeView.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NeeView
{
   //
    public class SelectedChangedEventArgs : EventArgs
    {
        public bool IsFocus { get; set; }
    }

    //
    public class FolderList : BindableBase
    {
        public static FolderList Current { get; private set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bookHub"></param>
        /// <param name="folderPanel"></param>
        public FolderList(BookHub bookHub, FolderPanelModel folderPanel)
        {
            Current = this;

            this.FolderPanel = folderPanel;
            _bookHub = bookHub;

            _bookHub.FolderListSync += (s, e) => SyncWeak(e);
            _bookHub.HistoryChanged += (s, e) => RefleshIcon(e.Key);
            _bookHub.BookmarkChanged += (s, e) => RefleshIcon(e.Key);
        }


        #region events

        public event EventHandler PlaceChanged;

        // 項目のフォーカス制御に使用する
        public event EventHandler<SelectedChangedEventArgs> SelectedChanged;

        #endregion


        //
        private BookHub _bookHub;

        //
        public FolderPanelModel FolderPanel { get; private set; }


        /// <summary>
        /// PanelListItemStyle property.
        /// </summary>
        public PanelListItemStyle PanelListItemStyle
        {
            get { return _panelListItemStyle; }
            set { if (_panelListItemStyle != value) { _panelListItemStyle = value; RaisePropertyChanged(); } }
        }

        //
        private PanelListItemStyle _panelListItemStyle;


        /// <summary>
        /// フォルダーアイコン表示位置
        /// </summary>
        public FolderIconLayout FolderIconLayout
        {
            get { return _folderIconLayout; }
            set { if (_folderIconLayout != value) { _folderIconLayout = value; RaisePropertyChanged(); } }
        }

        private FolderIconLayout _folderIconLayout = FolderIconLayout.Default;


        /// <summary>
        /// IsVisibleHistoryMark property.
        /// </summary>
        public bool IsVisibleHistoryMark
        {
            get { return _isVisibleHistoryMark; }
            set { if (_isVisibleHistoryMark != value) { _isVisibleHistoryMark = value; RaisePropertyChanged(); } }
        }

        private bool _isVisibleHistoryMark = true;


        /// <summary>
        /// IsVisibleBookmarkMark property.
        /// </summary>
        public bool IsVisibleBookmarkMark
        {
            get { return _isVisibleBookmarkMark; }
            set { if (_isVisibleBookmarkMark != value) { _isVisibleBookmarkMark = value; RaisePropertyChanged(); } }
        }

        private bool _isVisibleBookmarkMark = true;


        /// </summary>
        private string _home;
        public string Home
        {
            get { return _home; }
            set { if (_home != value) { _home = value; RaisePropertyChanged(); } }
        }

        /// <summary>
        /// 補正されたHOME取得
        /// </summary>
        /// <returns></returns>
        public string GetFixedHome()
        {
            if (Directory.Exists(_home)) return _home;

            var myPicture = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
            if (Directory.Exists(myPicture)) return myPicture;

            return Environment.CurrentDirectory;
        }



        /// <summary>
        /// フォルダーコレクション
        /// </summary>
        public FolderCollection FolderCollection
        {
            get { return _folderCollection; }
            set
            {
                if (_folderCollection != value)
                {
                    _folderCollection?.Dispose();
                    _folderCollection = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(FolderOrder));
                }
            }
        }

        //
        private FolderCollection _folderCollection;


        /// <summary>
        /// SelectIndex property.
        /// </summary>
        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = NVUtility.Clamp(value, 0, this.FolderCollection.Items.Count - 1);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// ふさわしい選択項目インデックスを取得
        /// </summary>
        /// <param name="path">選択したいパス</param>
        /// <returns></returns>
        internal int FixedIndexOfPath(string path)
        {
            var index = this.FolderCollection.IndexOfPath(path);
            return index < 0 ? 0 : index;
        }


        /// <summary>
        /// 現在のフォルダー
        /// </summary>
        private string _place => FolderCollection?.Place;

        /// <summary>
        /// そのフォルダーで最後に選択されていた項目の記憶
        /// </summary>
        private Dictionary<string, string> _lastPlaceDictionary = new Dictionary<string, string>();

        /// <summary>
        /// フォルダー履歴
        /// </summary>
        private History<string> _history = new History<string>();
        public History<string> History => _history;

        /// <summary>
        /// 更新フラグ
        /// </summary>
        private bool _isDarty;



        /// <summary>
        /// フォルダー状態保存
        /// </summary>
        /// <param name="folder"></param>
        private void SavePlace(FolderItem folder)
        {
            if (folder == null || folder.ParentPath == null) return;
            _lastPlaceDictionary[folder.ParentPath] = folder.Path;
        }



        /// <summary>
        /// 選択項目にフォーカス取得
        /// イベントでViewへ通知
        /// </summary>
        /// <param name="isFocus"></param>
        public void FocusSelectedItem(bool isFocus)
        {
            SelectedChanged?.Invoke(this, new SelectedChangedEventArgs() { IsFocus = isFocus });
        }


        /// <summary>
        /// 場所の初期化。
        /// nullを指定した場合、HOMEフォルダに移動。
        /// </summary>
        /// <param name="place"></param>
        public void ResetPlace(string place)
        {
            SetPlace(place ?? GetFixedHome(), null, false);
        }

        //
        public void SetPlace(string place, string select, bool isFocus)
        {
            var oprions = (isFocus ? FolderSetPlaceOption.IsFocus : FolderSetPlaceOption.None) | FolderSetPlaceOption.IsUpdateHistory;
            SetPlace(place, select, oprions);
        }

        /// <summary>
        /// フォルダーリスト更新
        /// </summary>
        /// <param name="place">フォルダーパス</param>
        /// <param name="select">初期選択項目</param>
        public void SetPlace(string place, string select, FolderSetPlaceOption options)
        {
            // 現在フォルダーの情報を記憶
            SavePlace(GetFolderItem(0));

            // 初期項目
            if (select == null && place != null)
            {
                _lastPlaceDictionary.TryGetValue(place, out select);
            }

            if (options.HasFlag(FolderSetPlaceOption.IsTopSelect))
            {
                select = null;
            }

            // 更新が必要であれば、新しいFolderListViewを作成する
            if (CheckFolderListUpdateneNcessary(place))
            {
                _isDarty = false;

                // FolderCollection 更新
                var collection = CreateFolderCollection(place);
                collection.ParameterChanged += (s, e) => App.Current?.Dispatcher.BeginInvoke((Action)(delegate () { Reflesh(true, false); }));
                collection.Deleting += FolderCollection_Deleting;
                this.FolderCollection = collection;
                this.SelectedIndex = FixedIndexOfPath(select);

                FocusSelectedItem(options.HasFlag(FolderSetPlaceOption.IsFocus));

                // 最終フォルダー更新
                BookHistory.Current.LastFolder = _place;

                // 履歴追加
                if (options.HasFlag(FolderSetPlaceOption.IsUpdateHistory))
                {
                    if (place != _history.GetCurrent())
                    {
                        _history.Add(place);
                    }
                }
            }
            else
            {
                // 選択項目のみ変更
                this.SelectedIndex = FixedIndexOfPath(select);
            }

            // 変更通知
            PlaceChanged?.Invoke(this, null);
        }

        /// <summary>
        /// リストの更新必要性チェック
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CheckFolderListUpdateneNcessary(string place)
        {
            return (_isDarty || this.FolderCollection == null || place != this.FolderCollection.Place);
        }

        /// <summary>
        /// FolderCollection 作成
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        private FolderCollection CreateFolderCollection(string place)
        {
            try
            {
                var collection = new FolderCollection(place);
                collection.Initialize();
                return collection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                // 救済措置。取得に失敗した時はカレントディレクトリに移動
                var collection = new FolderCollection(Environment.CurrentDirectory);
                collection.Initialize();
                return collection;
            }
        }

        /// <summary>
        /// フォルダーリスト項目変更前処理
        /// 項目が削除される前に有効な選択項目に変更する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderCollection_Deleting(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType != System.IO.WatcherChangeTypes.Deleted) return;

            var index = this.FolderCollection.IndexOfPath(e.FullPath);
            if (SelectedIndex != index) return;

            if (SelectedIndex < this.FolderCollection.Items.Count - 1)
            {
                SelectedIndex++;
            }
            else if (SelectedIndex > 0)
            {
                SelectedIndex--;
            }
        }


        /// <summary>
        /// フォルダーリスト更新
        /// </summary>
        /// <param name="force">必要が無い場合も更新する</param>
        /// <param name="isFocus">フォーカスを取得する</param>
        public void Reflesh(bool force, bool isFocus)
        {
            if (this.FolderCollection == null) return;

            _isDarty = force || this.FolderCollection.IsDarty();

            var options = (isFocus ? FolderSetPlaceOption.IsFocus : FolderSetPlaceOption.None) | FolderSetPlaceOption.IsUpdateHistory;
            SetPlace(_place, null, options);
        }



        /// <summary>
        /// 選択項目を基準とした項目取得
        /// </summary>
        /// <param name="offset">選択項目から前後した項目を指定</param>
        /// <returns></returns>
        internal FolderItem GetFolderItem(int offset)
        {
            if (this.FolderCollection?.Items == null) return null;

            int index = this.SelectedIndex;
            if (index < 0) return null;

            int next = (this.FolderCollection.FolderParameter.FolderOrder == FolderOrder.Random)
                ? (index + this.FolderCollection.Items.Count + offset) % this.FolderCollection.Items.Count
                : index + offset;

            if (next < 0 || next >= this.FolderCollection.Items.Count) return null;

            return this.FolderCollection[next];
        }


        /// <summary>
        /// 現在開いているフォルダーで更新(弱)
        /// e.isKeepPlaceが有効の場合、フォルダーは移動せず現在選択項目のみの移動を試みる
        /// </summary>
        /// <param name="e"></param>
        public void SyncWeak(FolderListSyncArguments e)
        {
            if (e != null && e.isKeepPlace)
            {
                if (this.FolderCollection == null || this.FolderCollection.Contains(e.Path)) return;
            }

            var options = (e.IsFocus ? FolderSetPlaceOption.IsFocus : FolderSetPlaceOption.None) | FolderSetPlaceOption.IsUpdateHistory;
            SetPlace(System.IO.Path.GetDirectoryName(e.Path), e.Path, options);
        }

        /// <summary>
        /// フォルダーアイコンの表示更新
        /// </summary>
        /// <param name="path">更新するパス。nullならば全て更新</param>
        public void RefleshIcon(string path)
        {
            this.FolderCollection?.RefleshIcon(path);
        }


        // サムネイル要求
        public void RequestThumbnail(int start, int count, int margin, int direction)
        {
            if (this.PanelListItemStyle.HasThumbnail())
            {
                ThumbnailManager.Current.RequestThumbnail(FolderCollection.Items, QueueElementPriority.FolderThumbnail, start, count, margin, direction);
            }
        }


        // ブックの読み込み
        public void LoadBook(string path)
        {
            BookLoadOption option = BookLoadOption.SkipSamePlace | (this.FolderCollection.FolderParameter.IsFolderRecursive ? BookLoadOption.DefaultRecursive : BookLoadOption.None);
            LoadBook(path, option);
        }

        // ブックの読み込み
        public void LoadBook(string path, BookLoadOption option)
        {
            _bookHub.RequestLoad(path, null, option, false);
        }


        // 現在の場所のフォルダーの並び順
        public FolderOrder FolderOrder
        {
            get { return GetFolderOrder(); }
        }

        /// <summary>
        /// フォルダーの並びを設定
        /// </summary>
        public void SetFolderOrder(FolderOrder folderOrder)
        {
            if (FolderCollection == null) return;
            this.FolderCollection.FolderParameter.FolderOrder = folderOrder;
            RaisePropertyChanged(nameof(FolderOrder));
        }

        /// <summary>
        /// フォルダーの並びを取得
        /// </summary>
        public FolderOrder GetFolderOrder()
        {
            if (this.FolderCollection == null) return default(FolderOrder);
            return this.FolderCollection.FolderParameter.FolderOrder;
        }


        /// <summary>
        /// フォルダーの並びを順番に切り替える
        /// </summary>
        public void ToggleFolderOrder()
        {
            if (this.FolderCollection == null) return;
            this.FolderCollection.FolderParameter.FolderOrder.GetToggle();
            RaisePropertyChanged(nameof(FolderOrder));
        }

        // 次のフォルダーに移動
        public void NextFolder(BookLoadOption option = BookLoadOption.None)
        {
            if (_bookHub.IsBusy()) return; // 相対移動の場合はキャンセルしない
            var result = MoveFolder(+1, option);
            if (result != true)
            {
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, "次のブックはありません");
            }
        }

        // 前のフォルダーに移動
        public void PrevFolder(BookLoadOption option = BookLoadOption.None)
        {
            if (_bookHub.IsBusy()) return; // 相対移動の場合はキャンセルしない
            var result = MoveFolder(-1, option);
            if (result != true)
            {
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, "前のブックはありません");
            }
        }


        /// <summary>
        /// コマンドの「前のフォルダーに移動」「次のフォルダーへ移動」に対応
        /// </summary>
        public bool MoveFolder(int direction, BookLoadOption options)
        {
            var item = this.GetFolderItem(direction);
            if (item != null)
            {
                SetPlace(_place, item.Path, FolderSetPlaceOption.IsUpdateHistory);
                _bookHub.RequestLoad(item.TargetPath, null, options, false);
                return true;
            }

            return false;
        }



        #region Commands


        public void SetHome_Executed()
        {
            if (_bookHub == null) return;
            this.Home = _place;
        }

        //
        public void MoveToHome_Executed()
        {
            if (_bookHub == null) return;

            var place = GetFixedHome();
            SetPlace(place, null, FolderSetPlaceOption.IsFocus | FolderSetPlaceOption.IsUpdateHistory | FolderSetPlaceOption.IsTopSelect);
        }


        //
        public void MoveTo_Executed(string path)
        {
            this.SetPlace(path, null, FolderSetPlaceOption.IsFocus | FolderSetPlaceOption.IsUpdateHistory);
        }

        //
        public bool MoveToPrevious_CanExecutre()
        {
            return _history.CanPrevious();
        }

        //
        public void MoveToPrevious_Executed()
        {
            if (!_history.CanPrevious()) return;

            var place = _history.GetPrevious();
            SetPlace(place, null, FolderSetPlaceOption.IsFocus);
            _history.Move(-1);
        }

        //
        public bool MoveToNext_CanExecute()
        {
            return _history.CanNext();
        }

        //
        public void MoveToNext_Executed()
        {
            if (!_history.CanNext()) return;

            var place = _history.GetNext();
            SetPlace(place, null, FolderSetPlaceOption.IsFocus);
            _history.Move(+1);
        }

        //
        public void MoveToHistory_Executed(KeyValuePair<int, string> item)
        {
            var place = _history.GetHistory(item.Key);
            SetPlace(place, null, FolderSetPlaceOption.IsFocus);
            _history.SetCurrent(item.Key + 1);
        }

        //
        public bool MoveToParent_CanExecute()
        {
            return (_place != null);
        }

        //
        public void MoveToParent_Execute()
        {
            if (_place == null) return;
            var parent = System.IO.Path.GetDirectoryName(_place);
            SetPlace(parent, _place, FolderSetPlaceOption.IsFocus | FolderSetPlaceOption.IsUpdateHistory);
        }

        //
        public void Sync_Executed()
        {
            string place = _bookHub?.Book?.Place;

            if (place != null)
            {
                _isDarty = true; // 強制更新
                SetPlace(System.IO.Path.GetDirectoryName(place), place, FolderSetPlaceOption.IsFocus | FolderSetPlaceOption.IsUpdateHistory);

                FocusSelectedItem(true);
            }
            else if (_place != null)
            {
                _isDarty = true; // 強制更新
                SetPlace(_place, null, FolderSetPlaceOption.IsFocus);

                FocusSelectedItem(true);
            }
        }

        //
        public void ToggleFolderRecursive_Executed()
        {
            this.FolderCollection.FolderParameter.IsFolderRecursive = !this.FolderCollection.FolderParameter.IsFolderRecursive;
        }

        #endregion



        #region Memento
        [DataContract]
        public class Memento
        {
            [DataMember]
            public PanelListItemStyle PanelListItemStyle { get; set; }

            [DataMember]
            public FolderIconLayout FolderIconLayout { get; set; }

            [DataMember]
            public bool IsVisibleHistoryMark { get; set; }

            [DataMember]
            public bool IsVisibleBookmarkMark { get; set; }

            [DataMember]
            public string Home { get; set; }

        }

        //
        public Memento CreateMemento()
        {
            var memento = new Memento();
            memento.PanelListItemStyle = this.PanelListItemStyle;
            memento.FolderIconLayout = this.FolderIconLayout;
            memento.IsVisibleHistoryMark = this.IsVisibleHistoryMark;
            memento.IsVisibleBookmarkMark = this.IsVisibleBookmarkMark;
            memento.Home = this.Home;
            return memento;
        }

        //
        public void Restore(Memento memento)
        {
            if (memento == null) return;

            this.PanelListItemStyle = memento.PanelListItemStyle;
            this.FolderIconLayout = memento.FolderIconLayout;
            this.IsVisibleHistoryMark = memento.IsVisibleHistoryMark;
            this.IsVisibleBookmarkMark = memento.IsVisibleBookmarkMark;
            this.Home = memento.Home;

            // Preference反映
            ///RaisePropertyChanged(nameof(FolderIconLayout));
        }

        #endregion
    }




    /// <summary>
    /// 旧フォルダーリスト設定。
    /// 互換性のために残してあります
    /// </summary>
    [DataContract]
    public class FolderListSetting
    {
        [DataMember]
        public bool IsVisibleHistoryMark { get; set; }

        [DataMember]
        public bool IsVisibleBookmarkMark { get; set; }
    }
}
