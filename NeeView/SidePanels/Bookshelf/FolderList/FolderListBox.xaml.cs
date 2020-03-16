﻿using NeeLaboratory.Windows.Input;
using NeeLaboratory.Windows.Media;
using NeeView.Collections;
using NeeView.Collections.Generic;
using NeeView.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeeView
{
    /// <summary>
    /// FolderListBox.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderListBox : UserControl, IPageListPanel, IDisposable
    {
        #region Fields

        public static string DragDropFormat = $"{Environment.ProcessId}.FolderListBox";

        private FolderListBoxViewModel _vm;
        private ListBoxThumbnailLoader _thumbnailLoader;
        private bool _storeFocus;
        private PageThumbnailJobClient _jobClient;

        #endregion

        #region Constructors

        // static construcotr
        static FolderListBox()
        {
            InitialieCommandStatic();
        }

        //
        public FolderListBox()
        {
            InitializeComponent();
        }

        //
        public FolderListBox(FolderListBoxViewModel vm) : this()
        {
            _vm = vm;
            this.DataContext = vm;

            InitializeCommand();

            // タッチスクロール操作の終端挙動抑制
            this.ListBox.ManipulationBoundaryFeedback += SidePanel.Current.ScrollViewer_ManipulationBoundaryFeedback;

            this.ListBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ListBox_ScrollChanged));


            this.Loaded += FolderListBox_Loaded;
            this.Unloaded += FolderListBox_Unloaded;

            if (_vm.FolderCollection is BookmarkFolderCollection)
            {
                var menu = new ContextMenu();
                menu.Items.Add(new MenuItem() { Header = Properties.Resources.FolderTreeMenuAddBookmark, Command = AddBookmarkCommand });
                menu.Items.Add(new MenuItem() { Header = Properties.Resources.WordNewFolder, Command = NewFolderCommand });
                this.ListBox.ContextMenu = menu;
            }
        }

        private void FolderListBox_Loaded(object sender, RoutedEventArgs e)
        {
            _jobClient = new PageThumbnailJobClient("FolderList", JobCategories.BookThumbnailCategory);
            _thumbnailLoader = new ListBoxThumbnailLoader(this, _jobClient);

            _vm.Loaded();
            _vm.SelectedChanging += SelectedChanging;
            _vm.SelectedChanged += SelectedChanged;
        }

        private void FolderListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            _jobClient?.Dispose();

            _vm.Unloaded();
            _vm.SelectedChanging -= SelectedChanging;
            _vm.SelectedChanged -= SelectedChanged;
        }

        #endregion

        #region Properties

        // フォーカス可能フラグ
        public bool IsFocusEnabled { get; set; } = true;

        #endregion

        #region IDisposable Support
        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_jobClient != null)
                    {
                        _jobClient.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region IPanelListBox Support

        //
        public ListBox PageCollectionListBox => this.ListBox;

        // サムネイルが表示されている？
        public bool IsThumbnailVisibled => _vm.IsThumbnailVisibled;

        public IEnumerable<IHasPage> CollectPageList(IEnumerable<object> objs) => objs.OfType<IHasPage>();

        #endregion

        #region Commands

        public static readonly RoutedCommand LoadWithRecursiveCommand = new RoutedCommand("LoadWithRecursiveCommand", typeof(FolderListBox));
        public static readonly RoutedCommand OpenCommand = new RoutedCommand("OpenCommand", typeof(FolderListBox));
        public static readonly RoutedCommand OpenExplorerCommand = new RoutedCommand("OpenExplorerCommand", typeof(FolderListBox));
        public static readonly RoutedCommand CopyCommand = new RoutedCommand("CopyCommand", typeof(FolderListBox));
        public static readonly RoutedCommand RemoveCommand = new RoutedCommand("RemoveCommand", typeof(FolderListBox));
        public static readonly RoutedCommand RenameCommand = new RoutedCommand("RenameCommand", typeof(FolderListBox));
        public static readonly RoutedCommand RemoveHistoryCommand = new RoutedCommand("RemoveHistoryCommand", typeof(FolderListBox));
        public static readonly RoutedCommand ToggleBookmarkCommand = new RoutedCommand("ToggleBookmarkCommand", typeof(FolderListBox));

        private static void InitialieCommandStatic()
        {
            CopyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            RemoveCommand.InputGestures.Add(new KeyGesture(Key.Delete));
            RenameCommand.InputGestures.Add(new KeyGesture(Key.F2));
            ToggleBookmarkCommand.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control));
        }

        private void InitializeCommand()
        {
            this.ListBox.CommandBindings.Add(new CommandBinding(LoadWithRecursiveCommand, LoadWithRecursive_Executed, LoadWithRecursive_CanExecute));
            this.ListBox.CommandBindings.Add(new CommandBinding(OpenCommand, Open_Executed));
            this.ListBox.CommandBindings.Add(new CommandBinding(OpenExplorerCommand, OpenExplorer_Executed));
            this.ListBox.CommandBindings.Add(new CommandBinding(CopyCommand, Copy_Executed, Copy_CanExecute));
            this.ListBox.CommandBindings.Add(new CommandBinding(RemoveCommand, Remove_Executed, Remove_CanExecute));
            this.ListBox.CommandBindings.Add(new CommandBinding(RenameCommand, Rename_Executed, Rename_CanExecute));
            this.ListBox.CommandBindings.Add(new CommandBinding(RemoveHistoryCommand, RemoveHistory_Executed, RemoveHistory_CanExecute));
            this.ListBox.CommandBindings.Add(new CommandBinding(ToggleBookmarkCommand, ToggleBookmark_Executed, ToggleBookmark_CanExecute));
        }

        /// <summary>
        /// ブックマーク登録/解除可能？
        /// </summary>
        private void ToggleBookmark_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = item != null && item.IsFileSystem() && !item.EntityPath.SimplePath.StartsWith(Temporary.Current.TempDirectory);
        }

        /// <summary>
        /// ブックマーク登録/解除
        /// </summary>
        private void ToggleBookmark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item != null)
            {
                if (BookmarkCollection.Current.Contains(item.EntityPath.SimplePath))
                {
                    BookmarkCollectionService.Remove(item.EntityPath);
                }
                else
                {
                    BookmarkCollectionService.Add(item.EntityPath);
                }
            }
        }

        /// <summary>
        /// 履歴から削除できる？
        /// </summary>
        private void RemoveHistory_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = item != null && BookHistoryCollection.Current.Contains(item.TargetPath.SimplePath);
        }

        /// <summary>
        /// 履歴から削除
        /// </summary>
        private void RemoveHistory_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item != null)
            {
                BookHistoryCollection.Current.Remove(item.TargetPath.SimplePath);
            }

        }

        /// <summary>
        /// サブフォルダーを読み込む？
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadWithRecursive_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;

            e.CanExecute = item == null || item.Attributes.AnyFlag(FolderItemAttribute.Drive | FolderItemAttribute.Empty)
                ? false
                : Config.Current.System.ArchiveRecursiveMode == ArchiveEntryCollectionMode.IncludeSubArchives
                    ? item.Attributes.HasFlag(FolderItemAttribute.Directory)
                    : ArchiverManager.Current.GetSupportedType(item.TargetPath.SimplePath).IsRecursiveSupported();
        }


        /// <summary>
        /// サブフォルダーを読み込む
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadWithRecursive_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;

            // サブフォルダー読み込み状態を反転する
            var option = item.IsRecursived ? BookLoadOption.NotRecursive : BookLoadOption.Recursive;
            _vm.Model.LoadBook(item, option);
        }

        /// <summary>
        /// ファイル系コマンド実行可能判定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = (item != null && item.IsEditable && FileIOProfile.Current.IsEnabled);
        }

        /// <summary>
        /// コピーコマンド実行可能判定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = (item != null && item.IsEditable);
        }

        /// <summary>
        /// コピーコマンド実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item != null)
            {
                FileIO.Current.CopyToClipboard(item);
            }
        }


        public void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = item != null && item.CanRemove();
        }


        /// <summary>
        /// 削除コマンド実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item == null)
            {
                return;
            }

            await _vm.RemoveAsync(item);
        }


        public void Rename_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            e.CanExecute = CanRenameExecute(item);
        }

        private bool CanRenameExecute(FolderItem item)
        {
            if (item == null || !item.IsEditable)
            {
                return false;
            }
            if (_vm.FolderCollection is PlaylistFolderCollection)
            {
                return false;
            }
            else if (item.IsFileSystem())
            {
                if (item.TargetPath.SimplePath.StartsWith(Temporary.Current.TempDirectory))
                {
                    return false;
                }
                else
                {
                    return FileIOProfile.Current.IsEnabled;
                }
            }
            else if (item.Attributes.HasFlag(FolderItemAttribute.Bookmark | FolderItemAttribute.Directory))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 名前変更コマンド実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Rename_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var listView = sender as ListBox;

            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item == null) return;

            if (CanRenameExecute(item))
            {
                listView.UpdateLayout();
                var listViewItem = VisualTreeUtility.GetListBoxItemFromItem(listView, item);
                var textBlock = VisualTreeUtility.FindVisualChild<TextBlock>(listViewItem, "FileNameTextBlock");

                // 
                if (textBlock != null)
                {
                    var rename = new RenameControl();
                    rename.Target = textBlock;
                    rename.IsSeleftFileNameBody = !item.IsDirectory;
                    rename.IsInvalidFileNameChars = item.IsFileSystem();
                    rename.Closing += async (s, ev) =>
                    {
                        if (item.Source is TreeListNode<IBookmarkEntry> bookmarkNode)
                        {
                            BookmarkCollectionService.Rename(bookmarkNode, ev.NewValue);
                        }
                        else if (ev.OldValue != ev.NewValue)
                        {
                            var newName = item.IsHideExtension() ? ev.NewValue + System.IO.Path.GetExtension(item.Name) : ev.NewValue;
                            //Debug.WriteLine($"{ev.OldValue} => {newName}");
                            var src = item.TargetPath;
                            var dst = await FileIO.Current.RenameAsync(item, newName);
                            if (dst != null)
                            {
                                _vm.FolderCollection?.RequestRename(src, new QueryPath(dst));
                            }
                        }
                    };
                    rename.Closed += (s, ev) =>
                    {
                        listViewItem.Focus();
                        if (ev.MoveRename != 0)
                        {
                            RenameNext(ev.MoveRename);
                        }
                    };
                    rename.Close += (s, ev) =>
                    {
                        _vm.IsRenaming = false;
                    };

                    _vm.IsRenaming = true;
                    ((MainWindow)Application.Current.MainWindow).RenameManager.Open(rename);
                }
            }
        }


        /// <summary>
        /// 項目を移動して名前変更処理を続行する
        /// </summary>
        /// <param name="delta"></param>
        private void RenameNext(int delta)
        {
            if (this.ListBox.SelectedIndex < 0) return;

            // 選択項目を1つ移動
            this.ListBox.SelectedIndex = (this.ListBox.SelectedIndex + this.ListBox.Items.Count + delta) % this.ListBox.Items.Count;
            this.ListBox.UpdateLayout();

            // ブック切り替え
            var item = this.ListBox.SelectedItem as FolderItem;
            if (item != null)
            {
                _vm.Model.LoadBook(item);
            }

            // リネーム発動
            Rename_Executed(this.ListBox, null);
        }

        public void Rename()
        {
            Rename_Executed(this.ListBox, null);
        }

        /// <summary>
        /// エクスプローラーで開くコマンド実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenExplorer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item != null)
            {
                var path = item.TargetPath.SimplePath;
                path = item.Attributes.AnyFlag(FolderItemAttribute.Bookmark | FolderItemAttribute.ArchiveEntry | FolderItemAttribute.Empty) ? ArchiverManager.Current.GetExistPathName(path) : path;
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
        }

        public void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as FolderItem;
            if (item != null)
            {
                _vm.MoveToSafety(item);
            }
        }

        private RelayCommand _NewFolderCommand;
        public RelayCommand NewFolderCommand
        {
            get { return _NewFolderCommand = _NewFolderCommand ?? new RelayCommand(NewFolderCommand_Executed); }
        }

        private void NewFolderCommand_Executed()
        {
            _vm.Model.NewFolder();
        }

        private RelayCommand _AddBookmarkCommand;
        public RelayCommand AddBookmarkCommand
        {
            get { return _AddBookmarkCommand = _AddBookmarkCommand ?? new RelayCommand(AddBookmarkCommand_Executed); }
        }

        private void AddBookmarkCommand_Executed()
        {
            _vm.Model.AddBookmark();
        }

        #endregion

        #region DragDrop

        private void DragStartBehavior_DragBegin(object sender, Windows.DragStartEventArgs e)
        {
            var data = e.Data.GetData(DragDropFormat) as ListBoxItem;
            if (data == null)
            {
                return;
            }

            var item = data.Content as FolderItem;
            if (item == null)
            {
                return;
            }

            if (item.Attributes.HasFlag(FolderItemAttribute.Empty))
            {
                e.Cancel = true;
                return;
            }


            if (item.Attributes.AnyFlag(FolderItemAttribute.Bookmark))
            {
                e.Data.SetData(item.Source);
                e.Data.SetData(item.TargetPath);
                e.AllowedEffects |= DragDropEffects.Move;
                return;
            }

            if (item.IsFileSystem())
            {
                try
                {
                    e.Data.SetFileDropList(new System.Collections.Specialized.StringCollection() { item.TargetPath.SimplePath });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    e.Data.SetData(item.TargetPath);
                }
                return;
            }
        }

        private void FolderList_DragEnter(object sender, DragEventArgs e)
        {
            FolderList_DragDrop(sender, e, false);
            DragDropHelper.AutoScroll(sender, e);
        }

        private void FolderList_PreviewDragOver(object sender, DragEventArgs e)
        {
            FolderList_DragDrop(sender, e, false);
            DragDropHelper.AutoScroll(sender, e);
        }

        private void FolderList_Drop(object sender, DragEventArgs e)
        {
            FolderList_DragDrop(sender, e, true);
        }

        private void FolderList_DragDrop(object sender, DragEventArgs e, bool isDrop)
        {
            var listBoxItem = PointToViewItem(this.ListBox, e.GetPosition(this.ListBox));

            var dragData = e.Data.GetData<ListBoxItem>(DragDropFormat);
            if (dragData != null)
            {
                if (listBoxItem == dragData)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }

            // bookmark
            if (_vm.FolderCollection is BookmarkFolderCollection bookmarkFolderCollection)
            {
                TreeListNode<IBookmarkEntry> bookmarkNode = null;

                if (listBoxItem?.Content is FolderItem target && target.Attributes.HasFlag(FolderItemAttribute.Bookmark | FolderItemAttribute.Directory))
                {
                    bookmarkNode = target.Source as TreeListNode<IBookmarkEntry>;
                }
                else
                {
                    bookmarkNode = bookmarkFolderCollection.BookmarkPlace;
                }

                if (bookmarkNode != null)
                {
                    DropToBookmark(sender, e, isDrop, bookmarkNode, e.Data.GetData<TreeListNode<IBookmarkEntry>>());
                    if (e.Handled) return;

                    DropToBookmark(sender, e, isDrop, bookmarkNode, e.Data.GetData<QueryPath>());
                    if (e.Handled) return;

                    DropToBookmark(sender, e, isDrop, bookmarkNode, e.Data.GetFileDrop());
                    if (e.Handled) return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void DropToBookmark(object sender, DragEventArgs e, bool isDrop, TreeListNode<IBookmarkEntry> node, TreeListNode<IBookmarkEntry> bookmarkEntry)
        {
            if (bookmarkEntry == null)
            {
                return;
            }

            if (!node.Children.Contains(bookmarkEntry) && !node.ParentContains(bookmarkEntry) && node != bookmarkEntry)
            {
                if (isDrop)
                {
                    _vm.Model.SelectBookmark(node, true);
                    BookmarkCollection.Current.MoveToChild(bookmarkEntry, node);
                }
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void DropToBookmark(object sender, DragEventArgs e, bool isDrop, TreeListNode<IBookmarkEntry> node, QueryPath query)
        {
            if (query == null)
            {
                return;
            }

            if (node.Value is BookmarkFolder && query.Search == null && (query.Scheme == QueryScheme.File || query.IsRoot(QueryScheme.Pagemark)))
            {
                if (isDrop)
                {
                    var bookmark = BookmarkCollectionService.AddToChild(node, query);
                    _vm.Model.SelectBookmark(bookmark, true);
                }
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void DropToBookmark(object sender, DragEventArgs e, bool isDrop, TreeListNode<IBookmarkEntry> node, string[] fileNames)
        {
            if (fileNames == null)
            {
                return;
            }
            if ((e.AllowedEffects & DragDropEffects.Copy) != DragDropEffects.Copy)
            {
                return;
            }

            bool isDropped = false;
            foreach (var fileName in fileNames)
            {
                if (ArchiverManager.Current.IsSupported(fileName, true, true) || System.IO.Directory.Exists(fileName))
                {
                    if (isDrop)
                    {
                        var bookmark = BookmarkCollectionService.AddToChild(node, new QueryPath(fileName));
                        _vm.Model.SelectBookmark(bookmark, true);
                    }
                    isDropped = true;
                }
            }
            if (isDropped)
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private ListBoxItem PointToViewItem(ListBox listBox, Point point)
        {
            var element = VisualTreeUtility.HitTest<ListBoxItem>(listBox, point);

            // NOTE: リストアイテム間に隙間がある場合があるので、Y座標をずらして再検証する
            if (element == null)
            {
                element = VisualTreeUtility.HitTest<ListBoxItem>(listBox, new Point(point.X, point.Y + 1));
            }

            return element;
        }

        #endregion

        #region Methods

        /// <summary>
        /// フォーカス取得
        /// </summary>
        /// <param name="isFocus"></param>
        public void FocusSelectedItem(bool isFocus)
        {
            if (this.ListBox.SelectedIndex < 0) this.ListBox.SelectedIndex = 0;
            if (this.ListBox.SelectedIndex < 0) return;

            // 選択項目が表示されるようにスクロール
            this.ListBox.ScrollIntoView(this.ListBox.SelectedItem);

            if (this.ListBox.IsLoaded && ((isFocus && this.IsFocusEnabled) || _vm.Model.IsFocusAtOnce))
            {
                _vm.Model.IsFocusAtOnce = false;
                ListBoxItem lbi = (ListBoxItem)(this.ListBox.ItemContainerGenerator.ContainerFromIndex(this.ListBox.SelectedIndex));
                lbi?.Focus();
            }
        }

        //
        public void SelectedChanging(object sender, SelectedChangedEventArgs e)
        {
            StoreFocus();
        }

        //
        public void SelectedChanged(object sender, SelectedChangedEventArgs e)
        {
            if (e.IsFocus)
            {
                FocusSelectedItem(true);
            }
            else
            {
                RestoreFocus();
            }

            _thumbnailLoader.Load();

            if (e.IsNewFolder)
            {
                Rename();
            }
        }

        /// <summary>
        /// 選択項目フォーカス状態を取得
        /// リスト項目変更前処理。
        /// </summary>
        public void StoreFocus()
        {
            var index = this.ListBox.SelectedIndex;

            ListBoxItem lbi = index >= 0 ? (ListBoxItem)(this.ListBox.ItemContainerGenerator.ContainerFromIndex(index)) : null;
            _storeFocus = lbi != null ? lbi.IsFocused : false;
        }

        /// <summary>
        /// 選択項目フォーカス反映
        /// リスト変更後処理。
        /// </summary>
        /// <param name="isFocused"></param>
        public void RestoreFocus()
        {
            if (_storeFocus)
            {
                this.ListBox.ScrollIntoView(this.ListBox.SelectedItem);

                var index = this.ListBox.SelectedIndex;
                var lbi = index >= 0 ? (ListBoxItem)(this.ListBox.ItemContainerGenerator.ContainerFromIndex(index)) : null;
                var isSuccess = lbi?.Focus();
            }
        }

        /// <summary>
        /// スクロール変更イベント処理
        /// </summary>
        private void ListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // リネームキャンセル
            ((MainWindow)App.Current.MainWindow).RenameManager.Stop();
        }

        private void FolderList_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void FolderList_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                _vm.IsVisibleChanged(true);
                // NOTE: ListBoxItemの表示を確定？
                await Task.Yield();
                FocusSelectedItem(false);
            }
        }

        private void FolderList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                Key key = e.Key == Key.System ? e.SystemKey : e.Key;

                if (key == Key.Home)
                {
                    _vm.MoveToHome();
                    e.Handled = true;
                }
                else if (key == Key.Up)
                {
                    _vm.MoveToUp();
                    e.Handled = true;
                }
                else if (key == Key.Down)
                {
                    var item = (sender as ListBox)?.SelectedItem as FolderItem;
                    if (item != null)
                    {
                        _vm.MoveToSafety(item);
                        e.Handled = true;
                    }
                }
                else if (key == Key.Left)
                {
                    _vm.MoveToPrevious();
                    e.Handled = true;
                }
                else if (key == Key.Right)
                {
                    _vm.MoveToNext();
                    e.Handled = true;
                }
            }
        }

        private void FolderList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isLRKeyEnabled = _vm.IsLRKeyEnabled();
            if ((isLRKeyEnabled && e.Key == Key.Left) || e.Key == Key.Back) // ←, Backspace
            {
                _vm.MoveToUp();
                e.Handled = true;
            }
        }

        private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && listBox.IsLoaded)
            {
                // 選択項目が表示されるようにスクロール
                listBox.ScrollIntoView(listBox.SelectedItem);
            }
        }

        //
        private void FolderListItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = (sender as ListBoxItem)?.Content as FolderItem;
            if (item != null && !item.IsEmpty())
            {
                _vm.Model.LoadBook(item);
            }
        }

        //
        private void FolderListItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = (sender as ListBoxItem)?.Content as FolderItem;
            _vm.MoveToSafety(item);

            e.Handled = true;
        }

        //
        private void FolderListItem_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool isLRKeyEnabled = _vm.IsLRKeyEnabled();
            var item = (sender as ListBoxItem)?.Content as FolderItem;

            if (e.Key == Key.Return)
            {
                _vm.Model.LoadBook(item);
                e.Handled = true;
            }
            else if (isLRKeyEnabled && e.Key == Key.Right) // →
            {
                _vm.MoveToSafety(item);
                e.Handled = true;
            }
            else if ((isLRKeyEnabled && e.Key == Key.Left) || e.Key == Key.Back) // ←, Backspace
            {
                if (item != null)
                {
                    _vm.MoveToUp();
                }
                e.Handled = true;
            }
        }


        //
        private void FolderListItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var folderInfo = (sender as ListBoxItem)?.Content as FolderItem;
            if (folderInfo == null) return;

            // 一時的にドラッグ禁止
            ////_vm.Drag_MouseDown(sender, e, folderInfo);
        }

        //
        private void FolderListItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 一時的にドラッグ禁止
            ////_vm.Drag_MouseUp(sender, e);
        }

        //
        private void FolderListItem_MouseMove(object sender, MouseEventArgs e)
        {
            // 一時的にドラッグ禁止
            ////_vm.Drag_MouseMove(sender, e);
        }


        /// <summary>
        /// コンテキストメニュー開始前イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderListItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var container = sender as ListBoxItem;
            if (container == null)
            {
                return;
            }

            var item = container.Content as FolderItem;
            if (item == null)
            {
                return;
            }

            // サブフォルダー読み込みの状態を更新
            var isDefaultRecursive = _vm.FolderCollection != null ? _vm.FolderCollection.FolderParameter.IsFolderRecursive : false;
            item.UpdateIsRecursived(isDefaultRecursive);

            // コンテキストメニュー生成

            var contextMenu = container.ContextMenu;
            if (contextMenu == null)
            {
                return;
            }

            contextMenu.Items.Clear();


            if (item.Attributes.HasFlag(FolderItemAttribute.System))
            {
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuOpen, Command = OpenCommand });
            }
            else if (item.Attributes.HasFlag(FolderItemAttribute.Bookmark))
            {
                if (item.IsDirectory)
                {
                    contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuDelete, Command = RemoveCommand });
                    contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuRename, Command = RenameCommand });
                }
                else
                {
                    contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuDeleteBookmark, Command = RemoveCommand });
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuExplorer, Command = OpenExplorerCommand });
                    contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuCopy, Command = CopyCommand });
                    ////contextMenu.Items.Add(new Separator());
                    ////contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuDelete, Command = RemoveCommand });
                    ////contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuRename, Command = RenameCommand });
                }
            }
            else if (item.Attributes.HasFlag(FolderItemAttribute.Empty))
            {
                bool canExplorer = !(_vm.FolderCollection is BookmarkFolderCollection);
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuExplorer, Command = OpenExplorerCommand, IsEnabled = canExplorer });
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuCopy, Command = CopyCommand, IsEnabled = false });
            }
            else if (item.IsFileSystem())
            {
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuSubfolder, Command = LoadWithRecursiveCommand, IsChecked = item.IsRecursived });
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.WordBookmark, Command = ToggleBookmarkCommand, IsChecked = BookmarkCollection.Current.Contains(item.EntityPath.SimplePath) });
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuDeleteHistory, Command = RemoveHistoryCommand });
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuExplorer, Command = OpenExplorerCommand });
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuCopy, Command = CopyCommand });
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuDelete, Command = RemoveCommand });
                contextMenu.Items.Add(new MenuItem() { Header = Properties.Resources.BookshelfItemMenuRename, Command = RenameCommand });
            }
        }

        public void Refresh()
        {
            this.ListBox.Items.Refresh();
        }


        #endregion
    }

    public class FolderItemToNoteConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is FolderItem item && values[1] is FolderOrder order)
            {
                return item.GetNote(order);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
