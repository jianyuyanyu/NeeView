﻿using System;

namespace NeeView
{
    public class BookmarkFolderTreeAccessor
    {
        private readonly BookmarkFolderTreeModel _model;

        public BookmarkFolderTreeAccessor(BookmarkFolderTreeModel model)
        {
            _model = model;

            BookmarkNode = new BookmarkFolderNodeAccessor(_model, _model.RootBookmarkFolder ?? throw new InvalidOperationException());
        }


        [WordNodeMember(IsAutoCollect = false)]
        public BookmarkFolderNodeAccessor BookmarkNode { get; }

        [WordNodeMember]
        public NodeAccessor? SelectedItem
        {
            get { return _model.SelectedItem is not null ? FolderNodeAccessorFactory.Create(_model, _model.SelectedItem) : null; }
            set { AppDispatcher.Invoke(() => _model.SetSelectedItem(value?.Node)); }
        }


        internal WordNode CreateWordNode(string name)
        {
            var node = WordNodeHelper.CreateClassWordNode(name, this.GetType());
            node.Children?.Add(BookmarkNode.CreateWordNode(nameof(BookmarkNode)));
            return node;
        }
    }

}
