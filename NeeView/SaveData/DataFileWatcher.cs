﻿using System;
using System.IO;
using NeeLaboratory.Generators;

namespace NeeView
{
    /// <summary>
    /// データファイルの監視
    /// </summary>
    /// <remarks>
    /// パスの指し示すデータファイルの更新が必要かのみをチェックするので、Changed もしくは Deleted イベントのみを発行する。
    /// 直接の Changed イベントだけを監視しないのは、File.Replace() による置き換えにも対応させるため。
    /// </remarks>
    public partial class DataFileWatcher : IDisposable
    {
        private FileSystemWatcher? _watcher;
        private string? _path;
        private string? _name;
        private bool _disposedValue;

        public DataFileWatcher(string path)
        {
            Start(path);
        }


        [Subscribable]
        public event FileSystemEventHandler? Changed;

        [Subscribable]
        public event FileSystemEventHandler? Deleted;


        public void Start(string path)
        {
            if (_disposedValue) return;

            if (path == _path) return;

            Stop();


            if (string.IsNullOrEmpty(path) || !FileIO.ExistsPath(path))
            {
                return;
            }

            _path = path;
            _name = Path.GetFileName(_path);
            if (string.IsNullOrEmpty(_name))
            {
                return;
            }

            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(_path) ?? "";
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.IncludeSubdirectories = false;
            _watcher.Created += Watcher_Created;
            _watcher.Changed += Watcher_Changed;
            _watcher.Deleted += Watcher_Deleted;
            _watcher.Renamed += Watcher_Renamed;

            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher?.Dispose();
            _watcher = null;
            _path = null;
            _name = null;
        }


        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.Name != _name) return;

            Changed?.Invoke(sender, e);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name != _name) return;

            Changed?.Invoke(sender, e);
        }


        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (e.Name != _name) return;

            Deleted?.Invoke(sender, e);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (e.Name != _name && e.OldName != _name) return;

            if (e.Name == _name)
            {
                Changed?.Invoke(sender, e);
            }
            else if (e.OldName == _name)
            {
                Deleted?.Invoke(sender, e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
