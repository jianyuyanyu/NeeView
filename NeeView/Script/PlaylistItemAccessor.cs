﻿namespace NeeView
{
    public record class PlaylistItemAccessor
    {
        private readonly PlaylistItem _source;

        public PlaylistItemAccessor(PlaylistItem source)
        {
            _source = source;
        }

        internal PlaylistItem Source => _source;


        [WordNodeMember]
        public string Name => _source.Name;

        [WordNodeMember]
        public string Path => _source.Path;


        [WordNodeMember]
        public void Open()
        {
            BookHub.Current.RequestLoad(this, _source.Path, null, BookLoadOption.None, true);
        }
    }

}
