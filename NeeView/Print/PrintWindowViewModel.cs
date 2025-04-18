﻿using NeeLaboratory.ComponentModel;
using NeeLaboratory.Windows.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Documents;

namespace NeeView
{
    public class PrintWindowCloseEventArgs : EventArgs
    {
        public bool? Result { get; set; }
    }


    public class PrintWindowViewModel : BindableBase
    {
        /// <summary>
        /// 設定保存
        /// </summary>
        private static PrintModel.Memento? _memento;

        private readonly PrintModel _model;
        private FrameworkElement? _mainContent;
        private List<FixedPage> _pageCollection = new();



        public PrintWindowViewModel(PrintContext context)
        {
            _model = new PrintModel(context);
            _model.Restore(_memento);

            _model.PropertyChanged += PrintService_PropertyChanged;
            _model.Margin.PropertyChanged += PrintService_PropertyChanged;

            UpdatePreview();
        }


        public event EventHandler<PrintWindowCloseEventArgs>? Close;


        public PrintModel Model => _model;

        public FrameworkElement? MainContent
        {
            get { return _mainContent; }
            set { if (_mainContent != value) { _mainContent = value; RaisePropertyChanged(); } }
        }

        public List<FixedPage> PageCollection
        {
            get { return _pageCollection; }
            set { if (_pageCollection != value) { _pageCollection = value; RaisePropertyChanged(); } }
        }


        /// <summary>
        /// 終了処理
        /// </summary>
        public void Closed()
        {
            _memento = _model.CreateMemento();
        }

        /// <summary>
        /// パラメータ変更イベント処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdatePreview();
        }

        /// <summary>
        /// プレビュー更新
        /// </summary>
        [MemberNotNull(nameof(PageCollection))]
        private void UpdatePreview()
        {
            PageCollection = _model.CreatePageCollection();
        }

        /// <summary>
        /// PrintCommand command.
        /// </summary>
        private RelayCommand? _PrintCommand;
        public RelayCommand PrintCommand
        {
            get { return _PrintCommand = _PrintCommand ?? new RelayCommand(PrintCommand_Executed); }
        }

        private void PrintCommand_Executed()
        {
            _model.Print();
            Close?.Invoke(this, new PrintWindowCloseEventArgs() { Result = true });
        }


        /// <summary>
        /// CancelCommand command.
        /// </summary>
        private RelayCommand? _CancelCommand;
        public RelayCommand CancelCommand
        {
            get { return _CancelCommand = _CancelCommand ?? new RelayCommand(CancelCommand_Executed); }
        }

        private void CancelCommand_Executed()
        {
            Close?.Invoke(this, new PrintWindowCloseEventArgs() { Result = false });
        }


        /// <summary>
        /// PrintDialogCommand command.
        /// </summary>
        private RelayCommand? _PrintDialogCommand;
        public RelayCommand PrintDialogCommand
        {
            get { return _PrintDialogCommand = _PrintDialogCommand ?? new RelayCommand(PrintDialogCommand_Executed); }
        }

        private void PrintDialogCommand_Executed()
        {
            _model.ShowPrintDialog();
            UpdatePreview();
        }
    }
}
