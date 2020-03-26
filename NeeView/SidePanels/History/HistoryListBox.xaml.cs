﻿using System;
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
    /// HistoryListBox.xaml の相互作用ロジック
    /// </summary>
    public partial class HistoryListBox : UserControl, IPageListPanel, IDisposable
    {
        #region Fields

        private HistoryListViewModel _vm;
        private ListBoxThumbnailLoader _thumbnailLoader;
        private bool _storeFocus;
        private PageThumbnailJobClient _jobClient;
        private bool _focusRequest;

        #endregion

        #region Constructors

        static HistoryListBox()
        {
            InitializeCommandStatic();
        }

        public HistoryListBox()
        {
            InitializeComponent();
        }

        public HistoryListBox(HistoryListViewModel vm) : this()
        {
            _vm = vm;
            this.DataContext = vm;

            InitializeCommand();

            // タッチスクロール操作の終端挙動抑制
            this.ListBox.ManipulationBoundaryFeedback += SidePanel.Current.ScrollViewer_ManipulationBoundaryFeedback;

            this.Loaded += HistoryListBox_Loaded;
            this.Unloaded += HistoryListBox_Unloaded;
        }

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

        #region IPageListBox support

        public ListBox PageCollectionListBox => this.ListBox;

        public bool IsThumbnailVisibled => _vm.Model.IsThumbnailVisibled;

        public IEnumerable<IHasPage> CollectPageList(IEnumerable<object> objs) => objs.OfType<IHasPage>();

        #endregion

        #region Commands

        public static readonly RoutedCommand RemoveCommand = new RoutedCommand("RemoveCommand", typeof(HistoryListBox));

        public static void InitializeCommandStatic()
        {
            RemoveCommand.InputGestures.Add(new KeyGesture(Key.Delete));
        }

        public void InitializeCommand()
        {
            this.ListBox.CommandBindings.Add(new CommandBinding(RemoveCommand, Remove_Exec));
        }

        public void Remove_Exec(object sender, ExecutedRoutedEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as BookHistory;
            if (item != null)
            {
                _vm.Remove(item);
            }
        }

        #endregion

        #region Methods

        private void HistoryListBox_Loaded(object sender, RoutedEventArgs e)
        {
            _jobClient = new PageThumbnailJobClient("HistoryList", JobCategories.BookThumbnailCategory);
            _thumbnailLoader = new ListBoxThumbnailLoader(this, _jobClient);
        }

        private void HistoryListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            _jobClient?.Dispose();
        }

        /// <summary>
        /// 選択項目フォーカス状態を取得
        /// リスト項目変更前処理。
        /// </summary>
        /// <returns></returns>
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
                lbi?.Focus();
            }
        }

        // フォーカス
        public bool FocusSelectedItem(bool focus)
        {
            if (this.ListBox.SelectedIndex < 0) return false;

            this.ListBox.ScrollIntoView(this.ListBox.SelectedItem);

            if (focus)
            {
                ListBoxItem lbi = (ListBoxItem)(this.ListBox.ItemContainerGenerator.ContainerFromIndex(this.ListBox.SelectedIndex));
                return lbi?.Focus() ?? false;
            }
            else
            {
                return false;
            }
        }

        public void Refresh()
        {
            this.ListBox.Items.Refresh();
        }

        public void FocusAtOnce()
        {
            var focused = FocusSelectedItem(true);
            if (!focused)
            {
                _focusRequest = true;
            }
        }

        #endregion

        #region Event Methods

        // 履歴項目決定


        private void HistoryListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = ((sender as ListBoxItem)?.Content as BookHistory);
            _vm.Load(item?.Place);
        }

        // 履歴項目決定(キー)
        private void HistoryListItem_KeyDown(object sender, KeyEventArgs e)
        {
            var item = ((sender as ListBoxItem)?.Content as BookHistory);

            if (e.Key == Key.Return)
            {
                _vm.Load(item?.Place);
                e.Handled = true;
            }
        }

        // リストのキ入力
        private void HistoryListBox_KeyDown(object sender, KeyEventArgs e)
        {
            bool isLRKeyEnabled = Config.Current.Panels.IsLeftRightKeyEnabled;

            // このパネルで使用するキーのイベントを止める
            if (e.Key == Key.Up || e.Key == Key.Down || (isLRKeyEnabled && (e.Key == Key.Left || e.Key == Key.Right)) || e.Key == Key.Return || e.Key == Key.Delete)
            {
                e.Handled = true;
            }
        }

        // 表示/非表示イベント
        private async void HistoryListBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Hidden;

            if (_vm.Visibility == Visibility.Visible)
            {
                _vm.UpdateItems();
                this.ListBox.UpdateLayout();
                
                await Task.Yield();
                
                if (this.ListBox.SelectedIndex < 0) this.ListBox.SelectedIndex = 0;
                FocusSelectedItem(_focusRequest);
                _focusRequest = false;
            }
        }

        // 選択項目が表示されるようにスクロールする
        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ListBox.SelectedItem == null || this.ListBox.SelectedIndex < 0) return;

            // スクロール
            this.ListBox.ScrollIntoView(this.ListBox.SelectedItem);
        }

        // リスト全体が変化したときにサムネイルを更新する
        private void HistoryListBox_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            AppDispatcher.BeginInvoke(() => _thumbnailLoader?.Load());
        }

        #endregion

    }

    public class ArchiveEntryToDecoratePlaceNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ArchiveEntry entry)
            {
                var directory = entry.RootArchiver?.SystemPath ?? LoosePath.GetDirectoryName(entry.SystemPath);
                return SidePanelProfile.Current.GetDecoratePlaceName(directory);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
