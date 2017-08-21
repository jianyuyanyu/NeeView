﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView
{
    /// <summary>
    /// ロードオプションフラグ
    /// </summary>
    [Flags]
    public enum BookLoadOption
    {
        None = 0,
        Recursive = (1 << 0), // 再帰
        SupportAllFile = (1 << 1), // すべてのファイルをページとみなす
        FirstPage = (1 << 2), // 初期ページを先頭ページにする
        LastPage = (1 << 3), // 初期ページを最終ページにする
        ReLoad = (1 << 4), // 再読み込みフラグ(BookHubで使用)
        KeepHistoryOrder = (1 << 5), // 履歴の順番を変更しない
        SelectFoderListMaybe = (1 << 6), // 可能ならばフォルダーリストで選択する
        SelectHistoryMaybe = (1 << 7), // 可能ならば履歴リストで選択する
        SkipSamePlace = (1 << 8), // 同じフォルダーならば読み込まない
        AutoRecursive = (1 << 9), // 自動再帰
        Resume = (1 << 10), // 履歴情報から全て復元
        DefaultRecursive = (1 << 11), // 再帰、ただし履歴が優先
    };

    /// <summary>
    /// 
    /// </summary>
    public class PagemarkChangedEventArgs : EventArgs
    {
        public Page Page { get; set; }
        public bool IsMarked { get; set; }
    }

    public class PageChangedEventArgs : EventArgs
    {
        public object Sender { get; set; }
        public int Index { get; set; }
    }


    /// <summary>
    /// 本
    /// </summary>
    public class Book : IDisposable
    {
        // Log用
        public static TraceSource Log = Logger.CreateLogger(nameof(Book));
        public static int _serial = 0;

        // Log用 シリアル番号
        public int Serial { get; private set; }

        // 環境
        ////private BookProfile _environment { get; } = BookProfile.Current;

        // テンポラリコンテンツ用ゴミ箱
        public TrashBox _trashBox { get; private set; } = new TrashBox();


        // 現在ページ変更(ページ番号)
        // タイトル、スライダーの更新を要求
        public event EventHandler<PageChangedEventArgs> PageChanged;

        // 表示コンテンツ変更
        // 表示の更新を要求
        public event EventHandler<ViewPageCollection> ViewContentsChanged;

        // ページ終端を超えて移動しようとした
        // 次の本への移動を要求
        public event EventHandler<int> PageTerminated;

        // 再読み込みを要求
        public event EventHandler DartyBook;

        // ソートされた
        public event EventHandler PagesSorted;

        // サムネイル更新
        public event EventHandler<Page> ThumbnailChanged;

        // 最初のコンテンツ表示フラグ
        public ManualResetEventSlim ContentLoaded = new ManualResetEventSlim();

        // Disposed
        private volatile bool _isDisposed;

        // 先読み許可フラグ
        private bool AllowPreLoad
        {
            get
            {
                switch (BookProfile.Current.PreLoadMode)
                {
                    default:
                    case PreLoadMode.None:
                        return false;
                    case PreLoadMode.AutoPreLoad:
                        return _canPreLoad;
                    case PreLoadMode.PreLoad:
                        return true;
                    case PreLoadMode.PreLoadNoUnload:
                        return true;
                }
            }
        }

        // 開放許可フラグ
        private bool AllowUnload
        {
            get { return BookProfile.Current.PreLoadMode != PreLoadMode.PreLoadNoUnload; }
        }

        // 先読み可能フラグ
        private bool _canPreLoad = true;

        // 先読み解除フラグ
        private int _canPreLoadCount;


        // ファイル削除された
        public event EventHandler<Page> PageRemoved;


        // 横長ページを分割する
        private bool _isSupportedDividePage;
        public bool IsSupportedDividePage
        {
            get { return _isSupportedDividePage; }
            set
            {
                if (_isSupportedDividePage != value)
                {
                    _isSupportedDividePage = value;
                    RequestReflesh(this, false);
                }
            }
        }

        // 最初のページは単独表示
        private bool _isSupportedSingleFirstPage;
        public bool IsSupportedSingleFirstPage
        {
            get { return _isSupportedSingleFirstPage; }
            set
            {
                if (_isSupportedSingleFirstPage != value)
                {
                    _isSupportedSingleFirstPage = value;
                    RequestReflesh(this, false);
                }
            }
        }

        // 最後のページは単独表示
        private bool _isSupportedSingleLastPage;
        public bool IsSupportedSingleLastPage
        {
            get { return _isSupportedSingleLastPage; }
            set
            {
                if (_isSupportedSingleLastPage != value)
                {
                    _isSupportedSingleLastPage = value;
                    RequestReflesh(this, false);
                }
            }
        }

        // 横長ページは２ページとみなす
        private bool _isSupportedWidePage = true;
        public bool IsSupportedWidePage
        {
            get { return _isSupportedWidePage; }
            set
            {
                if (_isSupportedWidePage != value)
                {
                    _isSupportedWidePage = value;
                    RequestReflesh(this, false);
                }
            }
        }


        // 右開き、左開き
        private PageReadOrder _bookReadOrder = PageReadOrder.RightToLeft;
        public PageReadOrder BookReadOrder
        {
            get { return _bookReadOrder; }
            set
            {
                if (_bookReadOrder != value)
                {
                    _bookReadOrder = value;
                    RequestReflesh(this, false);
                }
            }
        }

        // サブフォルダー読み込み
        private bool _isRecursiveFolder;
        public bool IsRecursiveFolder
        {
            get { return _isRecursiveFolder; }
            set
            {
                if (_isRecursiveFolder != value)
                {
                    _isRecursiveFolder = value;
                    DartyBook?.Invoke(this, null);
                }
            }
        }

        // 単ページ/見開き
        private PageMode _pageMode = PageMode.SinglePage;
        public PageMode PageMode
        {
            get { return _pageMode; }
            set
            {
                if (_pageMode != value)
                {
                    _pageMode = value;
                    RequestReflesh(this, false);
                }
            }
        }

        // ページ列
        private PageSortMode _sortMode = PageSortMode.FileName;
        public PageSortMode SortMode
        {
            get { return _sortMode; }
            set
            {
                if (_sortMode != value)
                {
                    _sortMode = value;
                    RequestSort(this);
                }
            }
        }

        // ページ列を設定
        // プロパティと異なり、ランダムソートの場合はソートを再実行する
        public void SetSortMode(PageSortMode mode)
        {
            if (_sortMode != mode || mode == PageSortMode.Random)
            {
                _sortMode = mode;
                RequestSort(this);
            }
        }


        // この本の場所
        // nullの場合、この本は無効
        public string Place { get; private set; }

        // 開始ページ
        public string StartEntry { get; private set; }

        // アーカイバーコレクション
        // Dispose処理のために保持
        private List<Archiver> _archivers = new List<Archiver>();

        // ページ コレクション
        public List<Page> Pages { get; private set; } = new List<Page>();

        // 表示されるページ番号(スライダー用)
        public int DisplayIndex { get; set; }

        // 表示ページコンテキスト
        private volatile ViewPageCollection _viewPageCollection = new ViewPageCollection();

        // 先読みページコンテキスト
        private volatile ViewPageCollection _nextPageCollection = new ViewPageCollection();

        // 表示ページ番号
        public int GetViewPageindex() => _viewPageCollection.Range.Position.Index;

        // 表示ページ
        public Page GetViewPage() => GetPage(_viewPageCollection.Range.Position.Index);

        // 表示ページ群
        public List<Page> GetViewPages() => _viewPageCollection.Collection.Select(e => e.Page).ToList();

        // ページ
        public Page GetPage(int index) => Pages.Count > 0 ? Pages[ClampPageNumber(index)] : null;

        //
        public Page GetPage(string name) => Pages.FirstOrDefault(e => e.FullPath == name);

        // ページ番号
        public int GetIndex(Page page) => Pages.IndexOf(page);

        // 先頭ページの場所
        private PagePosition _firstPosition => new PagePosition(0, 0);

        // 最終ページの場所
        private PagePosition _lastPosition => Pages.Count > 0 ? new PagePosition(Pages.Count - 1, 1) : _firstPosition;

        // リソースを保持しておくページ
        private List<Page> _keepPages = new List<Page>();

        //
        private int _keepPageNextSize => PageMode == PageMode.SinglePage ? 1 : 3;
        private int _keepPagePrevSize => PageMode == PageMode.SinglePage ? 1 : 2;

        // マーカー
        public List<Page> Markers = new List<Page>();


        // ページサムネイル寿命管理
        private class PageThumbnailPool : ThumbnailPool
        {
            public override int Limit => ThumbnailProfile.Current.PageCapacity;
        }

        // サムネイル寿命管理
        private PageThumbnailPool _thumbnaulPool = new PageThumbnailPool();


        // 本の読み込み
        #region LoadBook

        // 読み込み対象外サブフォルダー数。リカーシブ確認に使用します。
        public int SubFolderCount { get; private set; }


        /// <summary>
        /// constructor
        /// </summary>
        public Book()
        {
            Serial = ++_serial;
        }


        /// <summary>
        /// フォルダーの読込
        /// </summary>
        /// <param name="path"></param>
        /// <param name="start"></param>
        /// <param name="option"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task LoadAsync(string path, string start, BookLoadOption option, CancellationToken token)
        {
            try
            {
                Log.TraceEvent(TraceEventType.Information, Serial, $"Load: {path}");
                Log.Flush();

                await LoadCoreAsync(path, start, option, token);
            }
            catch (Exception e)
            {
                Log.TraceEvent(TraceEventType.Warning, Serial, $"Load Failed: {e.Message}");
                Log.Flush();

                Dispose();
                throw;
            }
        }

        // 本読み込み
        public async Task LoadCoreAsync(string path, string start, BookLoadOption option, CancellationToken token)
        {
            Debug.Assert(Place == null);

            // リカーシブフラグ
            if (IsRecursiveFolder)
            {
                option |= BookLoadOption.Recursive;
            }

            // アーカイバーの選択
            Archiver archiver = ArchiverManager.Current.CreateArchiver(path, null, true);
            if (archiver.IsFileSystem)
            {
                // 入力ファイルを最初のページにする
                if (path != archiver.GetPlace())
                {
                    start = Path.GetFileName(path);
                }
            }
            else
            {
                // 圧縮ファイルは再帰させる
                option |= BookLoadOption.Recursive;
            }

            PagePosition position = _firstPosition;
            int direction = 1;

            _trashBox.Add(archiver);

            this.Pages = await ReadArchiveAsync2(archiver, option, token);


            // Pages initialize
            var prefix = GetPagesPrefix();
            foreach (var page in Pages)
            {
                page.Prefix = prefix;
                page.Loaded += Page_Loaded;
                page.Thumbnail.Touched += Thumbnail_Touched;
            }

            // 初期ソート
            Sort();

            // スタートページ取得
            if ((option & BookLoadOption.FirstPage) == BookLoadOption.FirstPage)
            {
                position = _firstPosition;
                direction = 1;
            }
            else if ((option & BookLoadOption.LastPage) == BookLoadOption.LastPage)
            {
                position = _lastPosition;
                direction = -1;
            }
            else
            {
                int index = (start != null) ? Pages.FindIndex(e => e.FullPath == start) : 0;
                position = index >= 0 ? new PagePosition(index, 0) : _firstPosition;
                direction = 1;
            }

            // 開始ページ記憶
            StartEntry = Pages.Count > 0 ? Pages[position.Index].FullPath : null;

            // 有効化
            Place = archiver.Path;

            // 初期ページ設定
            RequestSetPosition(this, position, direction, true);
        }

        /// <summary>
        /// アーカイブファイルロック解除
        /// </summary>
        /// <returns></returns>
        public void Unlock()
        {
            var archivers = this.Pages.Select(e => e.Entry.Archiver).Distinct().Where(e => e != null);
            foreach (var archiver in archivers)
            {
                archiver.Unlock();
            }
        }

        /// <summary>
        /// サムネイル参照イベント処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Thumbnail_Touched(object sender, EventArgs e)
        {
            var thumb = (Thumbnail)sender;
            _thumbnaulPool.Add(thumb);
        }


        /// <summary>
        /// ページ収集
        /// </summary>
        /// <param name="archiver"></param>
        /// <param name="option"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<List<Page>> ReadArchiveAsync2(Archiver archiver, BookLoadOption option, CancellationToken token)
        {
            try
            {
                var collection = new EntryCollection(archiver, option.HasFlag(BookLoadOption.Recursive), option.HasFlag(BookLoadOption.SupportAllFile));
                _trashBox.Add(collection);

                await collection.CollectAsync(token);

                SubFolderCount = collection.SkippedArchiveCount;

                return collection.Collection.Select(e => CreatePage(e)).ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }


        /// <summary>
        /// ページ作成
        /// </summary>
        /// <param name="entry">ファイルエントリ</param>
        /// <returns></returns>
        private Page CreatePage(ArchiveEntry entry)
        {
            Page page;

            if (entry.IsImage())
            {
                if (entry.Archiver is PdfArchiver)
                {
                    page = new PdfPage(entry);
                }
                else if (BookProfile.Current.IsEnableAnimatedGif && LoosePath.GetExtension(entry.EntryName) == ".gif")
                {
                    page = new AnimatedPage(entry);
                }
                else
                {
                    page = new BitmapPage(entry);
                }
            }
            else
            {
                var type = entry.IsDirectory ? ArchiverType.FolderArchive : ArchiverManager.Current.GetSupportedType(entry.EntryName);
                switch (type)
                {
                    case ArchiverType.None:
                        page = new FilePage(entry, FilePageIcon.File);
                        break;
                    case ArchiverType.FolderArchive:
                        page = new FilePage(entry, FilePageIcon.Folder);
                        break;
                    default:
                        page = new FilePage(entry, FilePageIcon.Archive);
                        break;
                }
            }

            //
            page.Thumbnail.Changed += (s, e) =>
            {
                ThumbnailChanged?.Invoke(this, page);
            };

            return page;
        }



        // 名前の最長一致文字列取得
        private string GetPagesPrefix()
        {
            if (Pages == null || Pages.Count == 0) return "";

            string s = Pages[0].FullPath;
            foreach (var page in Pages)
            {
                s = GetStartsWith(s, page.FullPath);
                if (string.IsNullOrEmpty(s)) break;
            }

            // １ディレクトリだけの場合に表示が消えないようにする
            if (Pages.Count == 1)
            {
                s = s.TrimEnd('\\', '/');
            }

            // 最初の区切り記号
            for (int i = s.Length - 1; i >= 0; --i)
            {
                if (s[i] == '\\' || s[i] == '/')
                {
                    return s.Substring(0, i + 1);
                }
            }

            // ヘッダとして認識できなかった
            return "";
        }

        //
        private string GetStartsWith(string s0, string s1)
        {
            if (s0 == null || s1 == null) return "";

            if (s0.Length > s1.Length)
            {
                var temp = s0;
                s0 = s1;
                s1 = temp;
            }

            for (int i = 0; i < s0.Length; ++i)
            {
                char a0 = s0[i];
                char a1 = s1[i];
                if (s0[i] != s1[i])
                {
                    return i > 0 ? s0.Substring(0, i) : "";
                }
            }

            return s0;
        }

        // コマンドエンジン
        private BookCommandEngine _commandEngine = new BookCommandEngine();

        // 開始
        // ページ設定を行うとコンテンツ読み込みが始まるため、ロードと分離した
        public void Start()
        {
            Debug.Assert(Place != null);
            _commandEngine.StartEngine();
        }

        #endregion


        // 廃棄処理
        public async Task DisposeAsync()
        {
            var command = RequestDispose(this);
            if (command == null) return;

            await command.WaitAsync();
        }

        // 前のページに戻る
        public void PrevPage(int step = 0)
        {
            var s = (step == 0) ? PageMode.Size() : step;
            RequestMovePosition(this, -s);
        }

        // 次のページへ進む
        public void NextPage(int step = 0)
        {
            var s = (step == 0) ? PageMode.Size() : step;
            RequestMovePosition(this, +s);
        }

        // 最初のページに移動
        public void FirstPage()
        {
            RequestSetPosition(this, _firstPosition, 1, true);
        }

        // 最後のページに移動
        public void LastPage()
        {
            RequestSetPosition(this, _lastPosition, -1, true);
        }

        // 指定ページに移動
        public void JumpPage(Page page)
        {
            int index = Pages.IndexOf(page);
            if (index >= 0)
            {
                var position = new PagePosition(index, 0);
                RequestSetPosition(this, position, 1, false);
            }
        }



        // ページ指定移動
        public void RequestSetPosition(object sender, PagePosition position, int direction, bool isPreLoad)
        {
            Debug.Assert(direction == 1 || direction == -1);

            if (Place == null) return;

            DisplayIndex = position.Index;

            var command = new BookCommandSetPage(sender, this, new BookCommandSetPageArgs()
            {
                Position = position,
                Direction = direction,
                Size = PageMode.Size(),
                IsPreLoad = isPreLoad,
            });
            _commandEngine.Enqueue(command);
        }

        // ページ相対移動
        public void RequestMovePosition(object sender, int step)
        {
            if (Place == null) return;

            var command = new BookCommandMovePage(sender, this, new BookCommandMovePageArgs()
            {
                Step = step,
            });

            _commandEngine.Enqueue(command);
        }

        // リフレッシュ
        public void RequestReflesh(object sender, bool isClear)
        {
            if (Place == null) return;

            var command = new BookCommandReflesh(sender, this, new BookCommandRefleshArgs()
            {
                IsClear = isClear,
            });
            _commandEngine.Enqueue(command);
        }

        // ソート
        public void RequestSort(object sender)
        {
            if (Place == null) return;

            var command = new BookCommandSort(sender, this, new BookCommandSortArgs());
            _commandEngine.Enqueue(command);
        }

        // ページ削除
        public void RequestRemove(object sender, Page page)
        {
            if (Place == null) return;

            var command = new BookCommandRemove(sender, this, new BookCommandRemoveArgs()
            {
                Page = page,
            });
            _commandEngine.Enqueue(command);
        }


        // 終了処理
        private BookCommand RequestDispose(object sender)
        {
            if (Place == null) return null;

            var command = new BookCommandDispose(sender, this, new BookCommandDisposeArgs());
            _commandEngine.Enqueue(command);

            return command;
        }



        #region Marker

        /// <summary>
        /// マーカー判定
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public bool IsMarked(Page page)
        {
            return Markers.Contains(page);
        }

        /// <summary>
        /// マーカー群設定
        /// </summary>
        /// <param name="pageNames"></param>
        public void SetMarkers(IEnumerable<string> pageNames)
        {
            this.Markers = pageNames.Select(e => Pages.FirstOrDefault(page => page.FullPath == e)).Where(e => e != null).ToList();
        }

        /// <summary>
        /// マーカー移動可能判定
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        public bool CanJumpToMarker(int direction, bool isLoop)
        {
            if (Place == null) return false;
            if (Markers == null || Markers.Count == 0) return false;

            if (isLoop) return true;

            var list = Markers.OrderBy(e => e.Index).ToList();
            var index = GetViewPageindex();

            return direction > 0
                ? list.Last().Index > index
                : list.First().Index < index;
        }

        /// <summary>
        /// マーカーに移動
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="isLoop"></param>
        /// <returns></returns>
        public bool RequestJumpToMarker(object sender, int direction, bool isLoop, bool isIncludeTerminal)
        {
            Debug.Assert(direction == 1 || direction == -1);

            if (Place == null) return false;
            if (Pages == null || Pages.Count < 2) return false;

            var list = Markers != null ? Markers.OrderBy(e => e.Index).ToList() : new List<Page>();

            if (isIncludeTerminal)
            {
                if (list.FirstOrDefault() != Pages.First())
                {
                    list.Insert(0, Pages.First());
                }
                if (list.LastOrDefault() != Pages.Last())
                {
                    list.Add(Pages.Last());
                }
            }

            if (list.Count == 0) return false;

            var index = GetViewPageindex();

            var target =
                direction > 0
                ? list.FirstOrDefault(e => e.Index > index) ?? (isLoop ? list.First() : null)
                : list.LastOrDefault(e => e.Index < index) ?? (isLoop ? list.Last() : null);

            if (target == null) return false;

            RequestSetPosition(sender, new PagePosition(target.Index, 0), direction, false);
            return true;
        }

        #endregion


        // コマンド処理
        internal async Task Dispose_Executed(BookCommandDisposeArgs param, CancellationToken token)
        {
            Dispose();
            await Task.Yield();
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;

            // さまざまなイベント停止
            this.DartyBook = null;
            this.PageChanged = null;
            this.PageRemoved = null;
            this.PagesSorted = null;
            this.PageTerminated = null;
            this.ThumbnailChanged = null;
            this.ViewContentsChanged = null;

            _viewPageCollection = new ViewPageCollection();

            Pages?.ForEach(e => e?.Dispose());
            _archivers?.ForEach(e => e.Dispose());
            _trashBox?.CleanUp();

            _commandEngine.StopEngine();

            MemoryControl.Current.GarbageCollect();

            ////Debug.WriteLine("Book: Disposed.");
        }

        internal async Task Remove_Executed(BookCommandRemoveArgs param, CancellationToken token)
        {
            Remove(param.Page);
            await Task.Yield();
        }

        internal async Task Sort_Executed(BookCommandSortArgs param, CancellationToken token)
        {
            Sort();
            RequestSetPosition(this, _firstPosition, 1, true);
            await Task.Yield();
        }

        internal async Task Reflesh_Executed(BookCommandRefleshArgs param, CancellationToken token)
        {
            Reflesh(param.IsClear);
            await Task.Yield();
        }

        internal async Task SetPage_Executed(object sender, BookCommandSetPageArgs param, CancellationToken token)
        {
            var source = new PageDirectionalRange(param.Position, param.Direction, param.Size);
            await UpdateViewPageAsync(source, param.IsPreLoad, token);
        }


        internal async Task MovePage_Executed(BookCommandMovePageArgs param, CancellationToken token)
        {
            var viewRange = _viewPageCollection.Range;

            var direction = param.Step < 0 ? -1 : 1;

            var pos = Math.Abs(param.Step) == PageMode.Size() ? viewRange.Next(direction) : viewRange.Move(param.Step);
            if (pos < _firstPosition && !viewRange.IsContains(_firstPosition))
            {
                pos = new PagePosition(0, direction < 0 ? 1 : 0);
            }
            else if (pos > _lastPosition && !viewRange.IsContains(_lastPosition))
            {
                pos = new PagePosition(Pages.Count - 1, direction < 0 ? 1 : 0);
            }

            var range = new PageDirectionalRange(pos, direction, PageMode.Size());

            var isPreLoad = Math.Abs(param.Step) <= PageMode.Size();

            await UpdateViewPageAsync(range, isPreLoad, token);
        }


        // ページ番号のクランプ
        private int ClampPageNumber(int index)
        {
            if (index > Pages.Count - 1) index = Pages.Count - 1;
            if (index < 0) index = 0;
            return index;
        }

        // ページ場所の有効判定
        private bool IsValidPosition(PagePosition position)
        {
            return (_firstPosition <= position && position <= _lastPosition);
        }

        // 表示ページ更新
        private async Task UpdateViewPageAsync(PageDirectionalRange source, bool isPreLoad, CancellationToken token)
        {
            // ページ終端を越えたか判定
            if (source.Position < _firstPosition)
            {
                App.Current?.Dispatcher.Invoke(() => PageTerminated?.Invoke(this, -1));
                return;
            }
            else if (source.Position > _lastPosition)
            {
                App.Current?.Dispatcher.Invoke(() => PageTerminated?.Invoke(this, +1));
                return;
            }

            // ページ数０の場合は表示コンテンツなし
            if (Pages.Count == 0)
            {
                App.Current?.Dispatcher.Invoke(() => ViewContentsChanged?.Invoke(this, null));
                return;
            }

            // 先読みページコンテンツ無効
            _nextPageCollection = new ViewPageCollection();

            // view pages
            var viewPages = new List<Page>();
            for (int i = 0; i < PageMode.Size(); ++i)
            {
                var page = Pages[ClampPageNumber(source.Position.Index + source.Direction * i)];
                if (!viewPages.Contains(page))
                {
                    viewPages.Add(page);
                }
            }

            // cleanup pages
            _keepPages.AddRange(viewPages.Where(e => !_keepPages.Contains(e)));
            CleanupPages(source);

            // start load
            var tlist = new List<Task>();
            foreach (var page in viewPages)
            {
                tlist.Add(page.LoadAsync(QueueElementPriority.Top));
            }

            // pre load
            if (isPreLoad) PreLoad(source);

            // wait load
            if (BookProfile.Current.CanPrioritizePageMove())
            {
                await Task.Run(() => Task.WaitAll(tlist.ToArray(), 100, token));
            }
            else
            {
                await Task.WhenAll(tlist.ToArray());
            }
            // task cancel?
            token.ThrowIfCancellationRequested();

            // update contents
            _viewPageRange = source;
            UpdateViewContents();
            UpdateNextContents();

            // ページ破棄
            if (!AllowPreLoad) ClearAllPages(viewPages);
        }

        /// <summary>
        /// 要求中の表示範囲
        /// </summary>
        volatile PageDirectionalRange _viewPageRange;

        /// <summary>
        /// ページロード完了イベント処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!BookProfile.Current.CanPrioritizePageMove()) return;

            // 非同期なので一旦退避
            var now = _viewPageCollection;
            var next = _nextPageCollection;

            if (now?.Collection == null) return;

            var page = (Page)sender;

            // 現在表示に含まれているページ？
            if (page.IsContentAlived && now.Collection.Any(item => !item.IsValid && item.Page == page))
            {
                // 再更新
                UpdateViewContents();
            }

            UpdateNextContents();
        }

        /// <summary>
        /// 表示コンテンツ更新
        /// </summary>
        public void UpdateViewContents()
        {
            if (_isDisposed) return;

            // update contents
            var viewContent = CreateViewPageContext(_viewPageRange);
            if (viewContent == null) return;

            _viewPageCollection = viewContent;
            ////Debug.WriteLine($"now: {_viewPageCollection.Range}");

            // notice ViewContentsChanged
            App.Current?.Dispatcher.Invoke(() => ViewContentsChanged?.Invoke(this, _viewPageCollection));

            // change page
            DisplayIndex = viewContent.Range.Position.Index;

            // notice PropertyChanged
            var args = new PageChangedEventArgs() { Index = viewContent.Range.Position.Index };
            PageChanged?.Invoke(this, args);

            // コンテンツ準備完了
            ContentLoaded.Set();
        }


        /// <summary>
        /// 先読みコンテンツ更新
        /// </summary>
        public void UpdateNextContents()
        {
            if (_isDisposed) return;

            // 表示コンテンツ確定？
            if (!_viewPageCollection.IsValid) return;

            // 既に先読みコンテンツは確定している？
            if (_nextPageCollection.IsValid) return;

            // 先読みコンテンツ領域計算
            var position = _viewPageCollection.Range.Next();
            var direction = _viewPageCollection.Range.Direction;
            var range = new PageDirectionalRange(position, direction, PageMode.Size());

            // create contents
            _nextPageCollection = CreateViewPageContext(range);
            if (_nextPageCollection == null) return;
            if (!_nextPageCollection.IsValid) return;

            Debug.WriteLine($"next: {_nextPageCollection.Range}");
            // TODO: フィルター有効であれば表示サイズを計算してビットマップのリサイズを行う
        }

        //
        private bool IsWide(Page page)
        {
            return page.Width > page.Height * BookProfile.Current.WideRatio;
        }


        // 見開きモードでも単独表示するべきか判定
        private bool IsSoloPage(int index)
        {
            if (IsSupportedWidePage && IsWide(Pages[index])) return true;
            if (IsSupportedSingleFirstPage && index == 0) return true;
            if (IsSupportedSingleLastPage && index == Pages.Count - 1) return true;
            return false;
        }

        // 分割モード有効判定
        private bool IsEnableDividePage(int index)
        {
            return (PageMode == PageMode.SinglePage && IsSupportedDividePage && IsWide(Pages[index]));
        }

        // 表示コンテンツソースと、それに対応したコンテキスト作成
        private ViewPageCollection CreateViewPageContext(PageDirectionalRange source)
        {
            var infos = new List<PagePart>();

            {
                PagePosition position = source.Position;

                for (int id = 0; id < PageMode.Size(); ++id)
                {
                    if (!IsValidPosition(position) || Pages[position.Index] == null) break;

                    int size = 2;
                    if (IsEnableDividePage(position.Index))
                    {
                        size = 1;
                    }
                    else
                    {
                        position.Part = 0;
                    }

                    infos.Add(new PagePart(position, size, this.BookReadOrder));

                    position = position + ((source.Direction > 0) ? size : -1);
                }
            }

            // 見開き補正
            if (PageMode == PageMode.WidePage && infos.Count >= 2)
            {
                if (IsSoloPage(infos[0].Position.Index) || IsSoloPage(infos[1].Position.Index))
                {
                    infos = infos.GetRange(0, 1);
                }
            }

            // コンテンツソース作成
            var contentsSource = new List<ViewPage>();
            foreach (var v in infos)
            {
                var page = Pages[v.Position.Index];
                contentsSource.Add(new ViewPage(page, v));
            }



            // 並び順補正
            if (source.Direction < 0 && infos.Count >= 2)
            {
                contentsSource.Reverse();
                infos.Reverse();
            }

            // 左開き
            if (BookReadOrder == PageReadOrder.LeftToRight)
            {
                contentsSource.Reverse();
            }

            // 単一ソースならコンテンツは１つにまとめる
            if (infos.Count == 2 && infos[0].Position.Index == infos[1].Position.Index)
            {
                var position = new PagePosition(infos[0].Position.Index, 0);
                contentsSource.Clear();
                contentsSource.Add(new ViewPage(Pages[position.Index], new PagePart(position, 2, BookReadOrder)));
            }

            // 先読み可能判定
            UpdatePreLoadStatus(contentsSource);

            // 新しいコンテキスト
            var context = new ViewPageCollection(new PageDirectionalRange(infos, source.Direction), contentsSource);
            return context;
        }

        // 先読み判定更新
        private void UpdatePreLoadStatus(List<ViewPage> contentsSource)
        {
            if (BookProfile.Current.PreLoadMode != PreLoadMode.AutoPreLoad) return;

            UpdatePreLoadStatus(contentsSource.Select(e => e.Page));
        }

        /// <summary>
        /// 先読み自動判定
        /// </summary>
        /// <param name="page"></param>
        private void UpdatePreLoadStatus(IEnumerable<Page> pages)
        {
            if (BookProfile.Current.PreLoadMode != PreLoadMode.AutoPreLoad) return;

            // 集計
            double size = 0;
            foreach (var page in pages)
            {
                if (!page.IsContentInfoAlive) return;
                size += page.Content.Size.Width * page.Content.Size.Height;
            }

            // 判定
            if (size > BookProfile.Current.PreLoadLimitSize)
            {
                //Debug.WriteLine("PreLoad: Disabled");
                _canPreLoadCount = 0;
                _canPreLoad = false;
            }
            else
            {
                _canPreLoadCount++;
                if (!_canPreLoad && _canPreLoadCount > 3) // 一定回数連続で規定サイズ以下なら先読み有効
                {
                    //Debug.WriteLine("PreLoad: Enabled");
                    _canPreLoad = true;
                }
            }
        }


        // 不要ページコンテンツの削除を行う
        private void CleanupPages(PageDirectionalRange source)
        {
            // コンテンツを保持するページ収集
            var keepPages = new List<Page>();
            int prevSize = source.Direction < 0 ? _keepPageNextSize : _keepPagePrevSize;
            int nextSize = source.Direction < 0 ? _keepPagePrevSize : _keepPageNextSize;
            for (int offset = -prevSize; offset <= nextSize; ++offset)
            {
                int index = source.Position.Index + offset;
                if (0 <= index && index < Pages.Count)
                {
                    keepPages.Add(Pages[index]);
                }
            }

            // 不要コンテンツ破棄
            ClearAllPages(keepPages);
        }



        // 全ページコンテンツの削除を行う
        private void ClearAllPages(List<Page> keeps)
        {
            if (AllowUnload)
            {
                foreach (var page in _keepPages.Where(e => !keeps.Contains(e)))
                {
                    page.Unload();
                }
            }

            // 保持ページ更新
            _keepPages = keeps;
        }


        // 先読み
        private void PreLoad(PageDirectionalRange source)
        {
            if (!AllowPreLoad) return;

            var preLoadPages = new List<Page>();

            for (int offset = 0; offset <= _keepPageNextSize; offset++)
            {
                int index = source.Position.Index + (source.Direction < 0 ? -offset : offset);
                if (0 <= index && index < Pages.Count)
                {
                    Debug.Assert(_keepPages.Contains(Pages[index])); // 念のため
                    Pages[index].Load(QueueElementPriority.Default, PageJobOption.WeakPriority);

                    if (!_keepPages.Contains(Pages[index]))
                    {
                        _keepPages.Add(Pages[index]);
                    }
                }
            }
        }

        // ページの並び替え
        private void Sort()
        {
            if (Pages.Count <= 0) return;

            switch (SortMode)
            {
                case PageSortMode.FileName:
                    Pages.Sort((a, b) => CompareFileNameOrder(a, b, Win32Api.StrCmpLogicalW));
                    break;
                case PageSortMode.FileNameDescending:
                    Pages.Sort((a, b) => CompareFileNameOrder(b, a, Win32Api.StrCmpLogicalW));
                    break;
                case PageSortMode.TimeStamp:
                    Pages.Sort((a, b) => CompareDateTimeOrder(a, b, Win32Api.StrCmpLogicalW));
                    break;
                case PageSortMode.TimeStampDescending:
                    Pages.Sort((a, b) => CompareDateTimeOrder(b, a, Win32Api.StrCmpLogicalW));
                    break;
                case PageSortMode.Random:
                    var random = new Random();
                    Pages = Pages.OrderBy(e => random.Next()).ToList();
                    break;
                default:
                    throw new NotImplementedException();
            }

            // ページ ナンバリング
            PagesNumbering();

            PagesSorted?.Invoke(this, null);
        }


        /// <summary>
        /// ページ番号設定
        /// </summary>
        private void PagesNumbering()
        {
            for (int i = 0; i < Pages.Count; ++i) Pages[i].Index = i;
        }


        // ファイル名, 日付, ID の順で比較
        private static int CompareFileNameOrder(Page p1, Page p2, Func<string, string, int> compare)
        {
            if (p1.FullPath != p2.FullPath)
                return CompareFileName(p1.FullPath, p2.FullPath, compare);
            else if (p1.Entry.LastWriteTime != p2.Entry.LastWriteTime)
                return CompareDateTime(p1.Entry.LastWriteTime, p2.Entry.LastWriteTime);
            else
                return p1.Entry.Id - p2.Entry.Id;
        }

        // 日付, ファイル名, ID の順で比較
        private static int CompareDateTimeOrder(Page p1, Page p2, Func<string, string, int> compare)
        {
            if (p1.Entry.LastWriteTime != p2.Entry.LastWriteTime)
                return CompareDateTime(p1.Entry.LastWriteTime, p2.Entry.LastWriteTime);
            else if (p1.FullPath != p2.FullPath)
                return CompareFileName(p1.FullPath, p2.FullPath, compare);
            else
                return p1.Entry.Id - p2.Entry.Id;
        }

        // ファイル名比較. ディレクトリを優先する
        private static int CompareFileName(string s1, string s2, Func<string, string, int> compare)
        {
            string d1 = LoosePath.GetDirectoryName(s1);
            string d2 = LoosePath.GetDirectoryName(s2);

            if (d1 == d2)
                return compare(s1, s2);
            else
                return compare(d1, d2);
        }

        // 日付比較。null対応
        private static int CompareDateTime(DateTime? _t1, DateTime? _t2)
        {
            DateTime t1 = _t1 ?? DateTime.MinValue;
            DateTime t2 = _t2 ?? DateTime.MinValue;
            return (t1.Ticks - t2.Ticks < 0) ? -1 : 1;
        }



        // ページの削除
        private void Remove(Page page)
        {
            if (Pages.Count <= 0) return;

            int index = Pages.IndexOf(page);
            if (index < 0) return;

            Pages.RemoveAt(index);

            PagesNumbering();

            PageRemoved?.Invoke(this, page);

            index = ClampPageNumber(index);
            RequestSetPosition(this, new PagePosition(index, 0), 1, true);
        }


        // 表示の再構築
        private void Reflesh(bool clear)
        {
            if (Place == null) return;

            if (clear)
            {
                _keepPages.ForEach(e => e?.Unload());
            }

            RequestSetPosition(this, _viewPageCollection.Range.Position, 1, true);
        }



        #region Memento

        /// <summary>
        /// 保存設定
        /// </summary>
        [DataContract]
        public class Memento
        {
            // フォルダーの場所
            [DataMember(EmitDefaultValue = false)]
            public string Place { get; set; }

            // 名前
            public string Name => Place.EndsWith(@":\") ? Place : System.IO.Path.GetFileName(Place);

            // 現在ページ
            [DataMember(EmitDefaultValue = false)]
            public string Page { get; set; }

            // 1ページ表示 or 2ページ表示
            [DataMember(Name = "PageModeV2")]
            public PageMode PageMode { get; set; }

            // 右開き or 左開き
            [DataMember]
            public PageReadOrder BookReadOrder { get; set; }

            // 横長ページ分割 (1ページモード)
            [DataMember]
            public bool IsSupportedDividePage { get; set; }

            // 最初のページを単独表示 
            [DataMember]
            public bool IsSupportedSingleFirstPage { get; set; }

            // 最後のページを単独表示
            [DataMember]
            public bool IsSupportedSingleLastPage { get; set; }

            // 横長ページを2ページ分とみなす(2ページモード)
            [DataMember]
            public bool IsSupportedWidePage { get; set; } = true;

            // フォルダーの再帰
            [DataMember]
            public bool IsRecursiveFolder { get; set; }

            // ページ並び順
            [DataMember]
            public PageSortMode SortMode { get; set; }

            // 最終アクセス日
            [DataMember(Order = 12, EmitDefaultValue = false)]
            public DateTime LastAccessTime { get; set; }


            //
            public Memento Clone()
            {
                return (Memento)this.MemberwiseClone();
            }


            /// <summary>
            /// 項目のフィルタリング。フラグの立っている項目を上書き
            /// </summary>
            /// <param name="filter">フィルタービット列</param>
            /// <param name="overwrite">上書き既定値</param>
            public void Write(BookMementoFilter filter, Memento overwrite)
            {
                // 現在ページ
                if (filter.Flags[BookMementoBit.Page])
                {
                    this.Page = overwrite.Page;
                }
                // 1ページ表示 or 2ページ表示
                if (filter.Flags[BookMementoBit.PageMode])
                {
                    this.PageMode = overwrite.PageMode;
                }
                // 右開き or 左開き
                if (filter.Flags[BookMementoBit.BookReadOrder])
                {
                    this.BookReadOrder = overwrite.BookReadOrder;
                }
                // 横長ページ分割 (1ページモード)
                if (filter.Flags[BookMementoBit.IsSupportedDividePage])
                {
                    this.IsSupportedDividePage = overwrite.IsSupportedDividePage;
                }
                // 最初のページを単独表示 
                if (filter.Flags[BookMementoBit.IsSupportedSingleFirstPage])
                {
                    this.IsSupportedSingleFirstPage = overwrite.IsSupportedSingleFirstPage;
                }
                // 最後のページを単独表示
                if (filter.Flags[BookMementoBit.IsSupportedSingleLastPage])
                {
                    this.IsSupportedSingleLastPage = overwrite.IsSupportedSingleLastPage;
                }
                // 横長ページを2ページ分とみなす(2ページモード)
                if (filter.Flags[BookMementoBit.IsSupportedWidePage])
                {
                    this.IsSupportedWidePage = overwrite.IsSupportedWidePage;
                }
                // フォルダーの再帰
                if (filter.Flags[BookMementoBit.IsRecursiveFolder])
                {
                    this.IsRecursiveFolder = overwrite.IsRecursiveFolder;
                }
                // ページ並び順
                if (filter.Flags[BookMementoBit.SortMode])
                {
                    this.SortMode = overwrite.SortMode;
                }
            }


            // 保存用バリデート
            // このmementoは履歴とデフォルト設定の２つに使われるが、デフォルト設定には本の場所やページ等は不要
            public void ValidateForDefault()
            {
                Place = null;
                Page = null;
                LastAccessTime = default(DateTime);
            }

            // バリデートされたクローン
            public Memento ValidatedClone()
            {
                var clone = this.Clone();
                clone.ValidateForDefault();
                return clone;
            }
        }

        // 重複チェック用
        public class MementoPlaceCompare : IEqualityComparer<Memento>
        {
            public bool Equals(Memento m1, Memento m2)
            {
                if (m1 == null && m2 == null)
                    return true;
                else if (m1 == null | m2 == null)
                    return false;
                else if (m1.Place == m2.Place)
                    return true;
                else
                    return false;
            }

            public int GetHashCode(Memento m)
            {
                return m.Place.GetHashCode();
            }
        }


        // bookの設定を取得する
        public Memento CreateMemento()
        {
            var memento = new Memento();

            memento.Place = Place;
            memento.Page = SortMode != PageSortMode.Random ? GetViewPage()?.FullPath : null;

            memento.PageMode = PageMode;
            memento.BookReadOrder = BookReadOrder;
            memento.IsSupportedDividePage = IsSupportedDividePage;
            memento.IsSupportedSingleFirstPage = IsSupportedSingleFirstPage;
            memento.IsSupportedSingleLastPage = IsSupportedSingleLastPage;
            memento.IsSupportedWidePage = IsSupportedWidePage;
            memento.IsRecursiveFolder = IsRecursiveFolder;
            memento.SortMode = SortMode;
            //memento.LastAccessTime = DateTime.Now;

            return memento;
        }

        // bookに設定を反映させる
        public void Restore(Memento memento)
        {
            if (memento == null) return;

            PageMode = memento.PageMode;
            BookReadOrder = memento.BookReadOrder;
            IsSupportedDividePage = memento.IsSupportedDividePage;
            IsSupportedSingleFirstPage = memento.IsSupportedSingleFirstPage;
            IsSupportedSingleLastPage = memento.IsSupportedSingleLastPage;
            IsSupportedWidePage = memento.IsSupportedWidePage;
            IsRecursiveFolder = memento.IsRecursiveFolder;
            SortMode = memento.SortMode;
        }
    }

    #endregion


    /// <summary>
    /// Book設定項目番号
    /// </summary>
    public enum BookMementoBit
    {
        // 現在ページ
        Page,

        // 1ページ表示 or 2ページ表示
        PageMode,

        // 右開き or 左開き
        BookReadOrder,

        // 横長ページ分割 (1ページモード)
        IsSupportedDividePage,

        // 最初のページを単独表示 
        IsSupportedSingleFirstPage,

        // 最後のページを単独表示
        IsSupportedSingleLastPage,

        // 横長ページを2ページ分とみなす(2ページモード)
        IsSupportedWidePage,

        // フォルダーの再帰
        IsRecursiveFolder,

        // ページ並び順
        SortMode,
    };



    /// <summary>
    /// Book設定フィルタ
    /// </summary>
    [DataContract]
    public class BookMementoFilter
    {
        [DataMember]
        public Dictionary<BookMementoBit, bool> Flags { get; set; }

        //
        public BookMementoFilter(bool def = false)
        {
            Flags = Enum.GetValues(typeof(BookMementoBit)).OfType<BookMementoBit>().ToDictionary(e => e, e => def);
        }

        /// <summary>
        /// デシリアライズ終端処理
        /// </summary>
        /// <param name="c"></param>
        [OnDeserialized]
        private void Deserialized(StreamingContext c)
        {
            // 項目数の保証
            foreach (BookMementoBit key in Enum.GetValues(typeof(BookMementoBit)))
            {
                if (!Flags.ContainsKey(key)) Flags.Add(key, true);
            }
        }
    }

}
