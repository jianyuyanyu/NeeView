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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NeeView
{
    /// <summary>
    /// ConflictDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ConflictDialog : Window
    {
        //
        public ConflictDialog(ConflictDialogContext context)
        {
            InitializeComponent();

            this.DataContext = new ConflictDialogVM(context);
        }

        //
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    /// <summary>
    /// 競合しているコマンド情報
    /// </summary>
    public class ConflictItem
    {
        public CommandType Command { get; set; }
        public bool IsChecked { get; set; }

        //
        public string Name => Command.ToDispString();

        //
        public ConflictItem(CommandType commandType, bool isChecked)
        {
            Command = commandType;
            IsChecked = isChecked;
        }
    }

    /// <summary>
    /// ConflictDialog ViewModel
    /// </summary>
    public class ConflictDialogVM : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        private ConflictDialogContext _Context;

        // window title
        public string Title => $"競合の解消 - {_Context.Command.ToDispString()}";

        public string Gesture => _Context.Gesture;

        public List<ConflictItem> Conflicts => _Context.Conflicts;

        //
        public ConflictDialogVM(ConflictDialogContext context)
        {
            _Context = context;
        }
    }

    /// <summary>
    /// ConflictDialog Model
    /// </summary>
    public class ConflictDialogContext
    {
        public CommandType Command { get; set; }
        public string Gesture { get; set; }
        public List<ConflictItem> Conflicts { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="command">currrent command</param>
        /// <param name="gesture">conflict gesture</param>
        /// <param name="commands">conflict commands</param>
        public ConflictDialogContext(string gesture, List<CommandType> commands, CommandType command)
        {
            Command = command;
            Gesture = gesture;

            Conflicts = commands
                .Select(e => new ConflictItem(e, e == command))
                .ToList();
        }
    }
}
