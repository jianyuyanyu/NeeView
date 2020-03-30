﻿using NeeLaboratory.ComponentModel;
using NeeView.Windows.Property;
using System;
using System.Text.Json.Serialization;

namespace NeeView
{
    public class BookSettingConfig : BindableBase, ICloneable, IEquatable<BookSettingConfig>
    {
        private PageMode _pageMode = PageMode.SinglePage;
        private PageReadOrder _bookReadOrder = PageReadOrder.RightToLeft;
        private bool _isSupportedDividePage;
        private bool _isSupportedSingleFirstPage;
        private bool _isSupportedSingleLastPage;
        private bool _isSupportedWidePage = true;
        private bool _isRecursiveFolder;
        private PageSortMode _sortMode = PageSortMode.FileName;


        // ページ
        [JsonIgnore, PropertyMapIgnore]
        [PropertyMember("@ParamBookPage")]
        public string Page
        {
            get { return null; }
            set { }
        }

        // 1ページ表示 or 2ページ表示
        [PropertyMember("@ParamBookPageMode")]
        public PageMode PageMode
        {
            get { return _pageMode; }
            set { SetProperty(ref _pageMode, value); }
        }

        // 右開き or 左開き
        [PropertyMember("@ParamBookBookReadOrder")]
        public PageReadOrder BookReadOrder
        {
            get { return _bookReadOrder; }
            set { SetProperty(ref _bookReadOrder, value); }
        }

        // 横長ページ分割 (1ページモード)
        [PropertyMember("@ParamBookIsSupportedDividePage")]
        public bool IsSupportedDividePage
        {
            get { return _isSupportedDividePage; }
            set { SetProperty(ref _isSupportedDividePage, value); }
        }

        // 最初のページを単独表示 
        [PropertyMember("@ParamBookIsSupportedSingleFirstPage")]
        public bool IsSupportedSingleFirstPage
        {
            get { return _isSupportedSingleFirstPage; }
            set { SetProperty(ref _isSupportedSingleFirstPage, value); }
        }

        // 最後のページを単独表示
        [PropertyMember("@ParamBookIsSupportedSingleLastPage")]
        public bool IsSupportedSingleLastPage
        {
            get { return _isSupportedSingleLastPage; }
            set { SetProperty(ref _isSupportedSingleLastPage, value); }
        }

        // 横長ページを2ページ分とみなす(2ページモード)
        [PropertyMember("@ParamBookIsSupportedWidePage")]
        public bool IsSupportedWidePage
        {
            get { return _isSupportedWidePage; }
            set { SetProperty(ref _isSupportedWidePage, value); }
        }

        // フォルダーの再帰
        [PropertyMember("@ParamBookIsRecursiveFolder", Tips = "@ParamBookIsRecursiveFolderTips")]
        public bool IsRecursiveFolder
        {
            get { return _isRecursiveFolder; }
            set { SetProperty(ref _isRecursiveFolder, value); }
        }

        // ページ並び順
        [PropertyMember("@ParamBookSortMode")]
        public PageSortMode SortMode
        {
            get { return _sortMode; }
            set { SetProperty(ref _sortMode, value); }
        }


        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool Equals(BookSettingConfig other)
        {
            return other != null &&
                this.Page == other.Page &&
                this.PageMode == other.PageMode &&
                this.BookReadOrder == other.BookReadOrder &&
                this.IsSupportedDividePage == other.IsSupportedDividePage &&
                this.IsSupportedSingleFirstPage == other.IsSupportedSingleFirstPage &&
                this.IsSupportedSingleLastPage == other.IsSupportedSingleLastPage &&
                this.IsSupportedWidePage == other.IsSupportedWidePage &&
                this.IsRecursiveFolder == other.IsRecursiveFolder &&
                this.SortMode == other.SortMode;
        }
    }

}