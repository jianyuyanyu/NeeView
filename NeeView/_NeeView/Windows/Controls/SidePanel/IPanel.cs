﻿using System.Windows;
using System.Windows.Media;

namespace NeeView.Windows.Controls
{
    /// <summary>
    /// パネル定義
    /// </summary>
    public interface IPanel
    {
        /// <summary>
        /// 識別名
        /// </summary>
        string TypeCode { get; }

        /// <summary>
        /// アイコン
        /// </summary>
        ImageSource Icon { get; }

        /// <summary>
        /// アイコンレイアウト
        /// </summary>
        Thickness IconMargin { get; }

        /// <summary>
        /// アイコン説明
        /// </summary>
        string IconTips { get; }

        /// <summary>
        /// パネル実体
        /// </summary>
        FrameworkElement View { get; }
    }
}
