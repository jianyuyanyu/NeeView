﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using NeeLaboratory.Windows.Input;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Linq;
using System.Diagnostics;
using NeeLaboratory.ComponentModel;
using System.Threading;
using NeeView.Properties;
using System.Threading.Tasks;

namespace NeeView
{
    /// <summary>
    /// 
    /// </summary>
    public class HistoryListViewModel : BindableBase
    {
        //
        private CancellationTokenSource _removeUnlinkedCommandCancellationToken;

        #region Property: Items
        private ObservableCollection<BookHistory> _items;
        public ObservableCollection<BookHistory> Items
        {
            get { return _items; }
            set { _items = value; RaisePropertyChanged(); }
        }
        #endregion


        #region Property: SelectedItem
        private BookHistory _selectedItem;
        public BookHistory SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; RaisePropertyChanged(); }
        }
        #endregion


        #region Property: Visibility
        private Visibility _visibility = Visibility.Hidden;
        public Visibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; RaisePropertyChanged(); }
        }
        #endregion


        #region MoreMenu

        /// <summary>
        /// MoreMenu property.
        /// </summary>
        public ContextMenu MoreMenu
        {
            get { return _MoreMenu; }
            set { if (_MoreMenu != value) { _MoreMenu = value; RaisePropertyChanged(); } }
        }

        //
        private ContextMenu _MoreMenu;


        //
        private void InitializeMoreMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(CreateListItemStyleMenuItem(Properties.Resources.WordStyleList, PanelListItemStyle.Normal));
            menu.Items.Add(CreateListItemStyleMenuItem(Properties.Resources.WordStyleContent, PanelListItemStyle.Content));
            menu.Items.Add(CreateListItemStyleMenuItem(Properties.Resources.WordStyleBanner, PanelListItemStyle.Banner));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateCommandMenuItem(Properties.Resources.HistoryMenuDeleteInvalid, RemoveUnlinkedCommand));
            menu.Items.Add(CreateCommandMenuItem(Properties.Resources.HistoryMenuDeleteAll, RemoveAllCommand));
            this.MoreMenu = menu;
        }

        //
        private MenuItem CreateCommandMenuItem(string header, ICommand command)
        {
            var item = new MenuItem();
            item.Header = header;
            item.Command = command;
            return item;
        }


        //
        private MenuItem CreateCommandMenuItem(string header, string command, object source)
        {
            var item = new MenuItem();
            item.Header = header;
            item.Command = RoutedCommandTable.Current.Commands[command];
            item.CommandParameter = MenuCommandTag.Tag; // コマンドがメニューからであることをパラメータで伝えてみる
            var binding = CommandTable.Current.GetElement(command).CreateIsCheckedBinding();
            if (binding != null)
            {
                binding.Source = source;
                item.SetBinding(MenuItem.IsCheckedProperty, binding);
            }

            return item;
        }

        //
        private MenuItem CreateListItemStyleMenuItem(string header, PanelListItemStyle style)
        {
            var item = new MenuItem();
            item.Header = header;
            item.Command = SetListItemStyle;
            item.CommandParameter = style;
            var binding = new Binding(nameof(HistoryConfig.PanelListItemStyle))
            {
                Converter = _PanelListItemStyleToBooleanConverter,
                ConverterParameter = style,
                Source = Config.Current.History
            };
            item.SetBinding(MenuItem.IsCheckedProperty, binding);

            return item;
        }


        private PanelListItemStyleToBooleanConverter _PanelListItemStyleToBooleanConverter = new PanelListItemStyleToBooleanConverter();


        /// <summary>
        /// SetListItemStyle command.
        /// </summary>
        public RelayCommand<PanelListItemStyle> SetListItemStyle
        {
            get { return _SetListItemStyle = _SetListItemStyle ?? new RelayCommand<PanelListItemStyle>(SetListItemStyle_Executed); }
        }

        //
        private RelayCommand<PanelListItemStyle> _SetListItemStyle;

        //
        private void SetListItemStyle_Executed(PanelListItemStyle style)
        {
            Config.Current.History.PanelListItemStyle = style;
        }

        #endregion


        private bool _isDarty;

        /// <summary>
        /// Model property.
        /// </summary>
        public HistoryList Model
        {
            get { return _model; }
            set { if (_model != value) { _model = value; RaisePropertyChanged(); } }
        }

        //
        private HistoryList _model;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="model"></param>
        public HistoryListViewModel(HistoryList model)
        {
            _model = model;
            
            Config.Current.History.AddPropertyChanged(nameof(HistoryConfig.PanelListItemStyle), (s, e) => UpdateListBoxContent());

            _isDarty = true;

            BookHub.Current.HistoryChanged += BookHub_HistoryChanged;
            BookHub.Current.HistoryListSync += BookHub_HistoryListSync;

            InitializeMoreMenu();

            UpdateListBoxContent();
            UpdateItems();
        }


        //
        private void BookHub_HistoryListSync(object sender, BookHubPathEventArgs e)
        {
            this.ListBoxContent.StoreFocus();
            SelectedItem = BookHistoryCollection.Current.Find(e.Path);
            this.ListBoxContent.RestoreFocus();
        }

        //
        private void BookHub_HistoryChanged(object sender, BookMementoCollectionChangedArgs e)
        {
            _isDarty = _isDarty || e.HistoryChangedType != BookMementoCollectionChangedType.Update;
            if (_isDarty && Visibility == Visibility.Visible)
            {
                UpdateItems();
            }
        }

        //
        public void UpdateItems()
        {
            if (_isDarty)
            {
                _isDarty = false;

                AppDispatcher.Invoke(() => this.ListBoxContent.StoreFocus());

                var item = SelectedItem;
                Items = new ObservableCollection<BookHistory>(BookHistoryCollection.Current.Items);
                SelectedItem = Items.Count > 0 ? item : null;

                AppDispatcher.Invoke(() => this.ListBoxContent.RestoreFocus());
            }
        }

        //
        public void Load(string path)
        {
            if (path == null) return;
            BookHub.Current?.RequestLoad(path, null, BookLoadOption.KeepHistoryOrder | BookLoadOption.SkipSamePlace | BookLoadOption.IsBook, true);
        }


        // となりを取得
        public BookHistory GetNeighbor(BookHistory item)
        {
            if (Items == null || Items.Count <= 0) return null;

            int index = Items.IndexOf(item);
            if (index < 0) return Items[0];

            if (index + 1 < Items.Count)
            {
                return Items[index + 1];
            }
            else if (index > 0)
            {
                return Items[index - 1];
            }
            else
            {
                return item;
            }
        }

        //
        public void Remove(BookHistory item)
        {
            if (item == null) return;

            // 位置ずらし
            this.ListBoxContent.StoreFocus();
            SelectedItem = GetNeighbor(item);
            this.ListBoxContent.RestoreFocus();

            // 削除
            BookHistoryCollection.Current.Remove(item.Place);
        }

        /// <summary>
        /// RemoveAllCommand command
        /// </summary>
        public RelayCommand RemoveAllCommand
        {
            get { return _removeAllCommand = _removeAllCommand ?? new RelayCommand(RemoveAll_Executed); }
        }

        private RelayCommand _removeAllCommand;

        private void RemoveAll_Executed()
        {
            if (BookHistoryCollection.Current.Items.Any())
            {
                var dialog = new MessageDialog(Resources.DialogHistoryDeleteAll, Resources.DialogHistoryDeleteAllTitle);
                dialog.Commands.Add(UICommands.Delete);
                dialog.Commands.Add(UICommands.Cancel);
                var answer = dialog.ShowDialog();
                if (answer != UICommands.Delete) return;
            }

            BookHistoryCollection.Current.Clear();
        }


        /// <summary>
        /// RemoveUnlinkedCommand command.
        /// </summary>
        public RelayCommand RemoveUnlinkedCommand
        {
            get { return _removeUnlinkedCommand = _removeUnlinkedCommand ?? new RelayCommand(RemoveUnlinkedCommand_Executed); }
        }

        //
        private RelayCommand _removeUnlinkedCommand;

        //
        private async void RemoveUnlinkedCommand_Executed()
        {
            // 直前の命令はキャンセル
            _removeUnlinkedCommandCancellationToken?.Cancel();
            _removeUnlinkedCommandCancellationToken = new CancellationTokenSource();
            await BookHistoryCollection.Current.RemoveUnlinkedAsync(_removeUnlinkedCommandCancellationToken.Token);
        }



        /// <summary>
        /// ListBoxContent property.
        /// </summary>
        public HistoryListBox ListBoxContent
        {
            get { return _listBoxContent; }
            set { if (_listBoxContent != value) { _listBoxContent = value; RaisePropertyChanged(); } }
        }

        //
        private HistoryListBox _listBoxContent;

        //
        private void UpdateListBoxContent()
        {
            ListBoxContent = new HistoryListBox(this);
        }

    }
}
