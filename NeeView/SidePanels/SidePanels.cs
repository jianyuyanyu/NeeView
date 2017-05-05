﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NeeView
{
    /// <summary>
    /// NeeView用 サイドパネル管理
    /// </summary>
    public class SidePanels : SidePanelFrameModel
    {
        // 各種類のパネルインスタンス
        public FolderPanel FolderListPanel { get; private set; }
        public HistoryPanel HistoryPanel { get; private set; }
        public FileInformationPanel FileInfoPanel { get; private set; }
        public ImageEffectPanel ImageEffectPanel { get; private set; }
        public BookmarkPanel BookmarkPanel { get; private set; }
        public PagemarkPanel PagemarkPanel { get; private set; }


        /// <summary>
        /// サイドパネル初期化
        /// TODO: 生成順。モデルはビュー生成の前に準備されているべき
        /// </summary>
        /// <param name="control"></param>
        public SidePanels(Models models)
        {
            var leftPanels = new List<IPanel>();
            var rightPanels = new List<IPanel>();

            // フォルダーリスト
            this.FolderListPanel = new FolderPanel(models.FolderPanelModel, models.FolderList, models.PageList);
            leftPanels.Add(this.FolderListPanel);

            // 履歴
            this.HistoryPanel = new HistoryPanel(models.HistoryList);
            leftPanels.Add(this.HistoryPanel);

            // ファイル情報
            this.FileInfoPanel = new FileInformationPanel(models.FileInformation);
            rightPanels.Add(this.FileInfoPanel);

            // エフェクト
            this.ImageEffectPanel = new ImageEffectPanel(models.ImageEffecct);
            rightPanels.Add(this.ImageEffectPanel);

            // ブックマーク
            this.BookmarkPanel = new BookmarkPanel(models.BookmarkList);
            leftPanels.Add(this.BookmarkPanel);

            // ページマーク
            this.PagemarkPanel = new PagemarkPanel(models.PagemarkList);
            leftPanels.Add(this.PagemarkPanel);

            //
            this.InitializePanels(leftPanels, rightPanels);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="vm"></param>
        public void Initialize(MainWindowVM vm)
        {
            // フォルダーリスト
            Models.Current.FolderList.SetPlace(ModelContext.BookHistory.LastFolder ?? vm.BookHub.GetFixedHome(), null, false); // ##
        }

        /// <summary>
        /// 指定したパネルが表示されているか判定
        /// </summary>
        /// <returns></returns>
        public bool IsVisiblePanel(IPanel panel)
        {
            return this.Left.IsVisiblePanel(panel) || this.Right.IsVisiblePanel(panel);
        }

        /// <summary>
        /// 指定したパネルが選択されているか判定
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        public bool IsSelectedPanel(IPanel panel)
        {
            return this.Left.SelectedPanel == panel || this.Right.SelectedPanel == panel;
        }

        /// <summary>
        /// パネル選択状態を設定
        /// </summary>
        /// <param name="panel">パネル</param>
        /// <param name="isSelected">選択</param>
        public void SetSelectedPanel(IPanel panel, bool isSelected)
        {
            if (this.Left.Contains(panel))
            {
                this.Left.SetSelectedPanel(panel, isSelected);
            }
            if (this.Right.Contains(panel))
            {
                this.Right.SetSelectedPanel(panel, isSelected);
            }
        }

        /// <summary>
        /// パネル選択状態をトグル。
        /// 非表示状態の場合は切り替えよりも表示させることを優先する
        /// </summary>
        /// <param name="panel">パネル</param>
        /// <param name="force">表示状態にかかわらず切り替える</param>
        public void ToggleSelectedPanel(IPanel panel, bool force)
        {
            if (this.Left.Contains(panel))
            {
                this.Left.ToggleSelectedPanel(panel, force);
            }
            if (this.Right.Contains(panel))
            {
                this.Right.ToggleSelectedPanel(panel, force);
            }
        }

        /// <summary>
        /// パネル表示トグル
        /// </summary>
        /// <param name="code"></param>
        public void ToggleVisiblePanel(IPanel panel)
        {
            this.Left.Toggle(panel);
            this.Right.Toggle(panel);
        }
    }
}
