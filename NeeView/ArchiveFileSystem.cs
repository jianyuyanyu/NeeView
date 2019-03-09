﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView
{
    /// <summary>
    /// アーカイブ内パスを含むパス記述に対応した処理
    /// </summary>
    public static class ArchiveFileSystem
    {
        /// <summary>
        /// パスからArcvhiveEntryを作成
        /// </summary>
        public static async Task<ArchiveEntry> CreateArchiveEntry_New(string path, bool allowPreExtract, CancellationToken token)
        {
            var query = new QueryPath(path);

            if (File.Exists(path) || Directory.Exists(path))
            {
                return new ArchiveEntry(path);
            }

            else if (query.Scheme == QueryScheme.Pagemark)
            {
                if (query.Path == null)
                {
                    return new ArchiveEntry(query.FullPath);
                }
                else
                {
                    var archiver = await ArchiverManager.Current.CreateArchiverAsync(new ArchiveEntry(QueryScheme.Pagemark.ToSchemeString()), false, token);
                    var entries = await archiver.GetEntriesAsync(token);
                    var entry = entries.FirstOrDefault(e => e.EntryName == query.FileName);
                    if (entry != null)
                    {
                        return entry;
                    }
                }
            }

            else
            {
                try
                {
                    var parts = LoosePath.Split(path);
                    string archivePath = null;

                    foreach (var part in parts)
                    {
                        archivePath = LoosePath.Combine(archivePath, part);

                        if (File.Exists(archivePath))
                        {
                            var archiver = await ArchiverManager.Current.CreateArchiverAsync(new ArchiveEntry(archivePath), allowPreExtract, token);
                            var entries = await archiver.GetEntriesAsync(token);

                            var entryName = path.Substring(archivePath.Length).TrimStart(LoosePath.Separator);
                            var entry = entries.FirstOrDefault(e => e.EntryName == entryName);
                            if (entry != null)
                            {
                                return entry;
                            }
                            else
                            {
                                return await CreateInnerArchiveEntry_New(archiver, entryName, allowPreExtract, token);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new FileNotFoundException(string.Format(Properties.Resources.ExceptionFileNotFound, path), ex);
                }
            }

            throw new FileNotFoundException(string.Format(Properties.Resources.ExceptionFileNotFound, path));
        }

        /// <summary>
        /// アーカイブ内のエントリーを返す。
        /// 入れ子になったアーカイブの場合、再帰処理する。
        /// </summary>
        private static async Task<ArchiveEntry> CreateInnerArchiveEntry_New(Archiver archiver, string entryName, bool allowPreExtract, CancellationToken token)
        {
            var entries = await archiver.GetEntriesAsync(token);

            var entry = entries.FirstOrDefault(e => e.EntryName == entryName);
            if (entry != null) return entry;

            var parts = LoosePath.Split(entryName);
            string archivePath = null;

            foreach (var part in parts)
            {
                archivePath = LoosePath.Combine(archivePath, part);

                entry = entries.FirstOrDefault(e => e.EntryName == archivePath && e.IsArchive());
                if (entry != null)
                {
                    var subArchiver = await ArchiverManager.Current.CreateArchiverAsync(entry, allowPreExtract, token);
                    var subEntryName = entryName.Substring(archivePath.Length).TrimStart(LoosePath.Separator);
                    return await CreateInnerArchiveEntry_New(subArchiver, subEntryName, allowPreExtract, token);
                }
            }

            throw new FileNotFoundException();
        }


        /// <summary>
        /// パスからArcvhiveEntryを作成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Obsolete]
        public static async Task<ArchiveEntry> CreateArchiveEntry(string path, CancellationToken token)
        {
            // システムパスはそのまま
            if (File.Exists(path) || Directory.Exists(path))
            {
                return new ArchiveEntry(path);
            }
            // アーカイブパスはそのエントリーを返す
            else
            {
                try
                {
                    var archivePath = ArchiverManager.Current.GetExistPathName(path) ?? throw new FileNotFoundException();
                    var entryName = path.Substring(archivePath.Length).TrimStart(LoosePath.Separator);
                    var archiver = await ArchiverManager.Current.CreateArchiverAsync(new ArchiveEntry(archivePath), false, token);
                    ////Debug.WriteLine($"Create Archiver: {archiver.FullPath}");
                    var entries = await archiver.GetEntriesAsync(token);
                    var entry = entries.FirstOrDefault(e => e.EntryName == entryName);
                    if (entry != null)
                    {
                        return entry;
                    }
                    else
                    {
                        return await CreateInnerArchiveEntry(archiver, entryName, token);
                    }
                }
                catch (FileNotFoundException)
                {
                    throw new FileNotFoundException(string.Format(Properties.Resources.ExceptionFileNotFound, path));
                }
            }
        }

        /// <summary>
        /// アーカイブ内のエントリーを返す。
        /// 入れ子になったアーカイブの場合、再帰処理する。
        /// </summary>
        /// <param name="archiver"></param>
        /// <param name="entryName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Obsolete]
        private static async Task<ArchiveEntry> CreateInnerArchiveEntry(Archiver archiver, string entryName, CancellationToken token)
        {
            var entries = await archiver.GetEntriesAsync(token);

            var entry = entries.GetEntry(entryName);
            if (entry != null) return entry;

            // 書庫内書庫の検証
            var path = entryName;

            while (true)
            {
                path = LoosePath.GetDirectoryName(path);
                if (string.IsNullOrEmpty(path)) throw new FileNotFoundException();

                entry = entries.GetEntry(path);
                if (entry != null)
                {
                    var subArchiver = await ArchiverManager.Current.CreateArchiverAsync(entry, false, token);
                    ////Debug.WriteLine($"Create Archiver: {subArchiver.FullPath}");
                    var subEntryName = entryName.Substring(entry.RawEntryName.Length + 1);
                    return await CreateInnerArchiveEntry(subArchiver, subEntryName, token);
                }
            }
        }

        /// <summary>
        /// アーカイブパスの存在チェック
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<bool> ExistsAsync(string path, CancellationToken token)
        {
            try
            {
                var entry = await CreateArchiveEntry(path, token);
                return entry != null;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }


        /// <summary>
        /// 実在するディレクトリまで遡る
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetExistDirectoryName(string path)
        {
            if (Directory.Exists(path))
            {
                return path;
            }

            while (!string.IsNullOrEmpty(path))
            {
                path = LoosePath.GetDirectoryName(path);
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// 実在するエントリーまで遡る
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetExistEntryName(string path)
        {
            if (Directory.Exists(path) || File.Exists(path))
            {
                return path;
            }

            while (!string.IsNullOrEmpty(path))
            {
                path = LoosePath.GetDirectoryName(path);
                if (Directory.Exists(path) || File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

    }

}
